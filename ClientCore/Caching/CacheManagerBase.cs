#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

using Rampastring.Tools;

namespace ClientCore.Caching;

/// <summary>
/// Thread-safe manager for caching outputs with LRU eviction policy.
/// Processes computation requests sequentially to limit CPU usage to a single thread.
/// Cached outputs are ref-counted: <see cref="GetDisposeAction"/> is invoked when all
/// callers have released their leases and the cache itself has evicted the entry.
/// Override <see cref="GetDisposeAction"/> in a subclass to add disposal behaviour
/// (see <see cref="DisposableCacheManagerBase{TInput, TOutput}"/>).
/// </summary>
public abstract class CacheManagerBase<TInput, TOutput> : ICacheManager<TInput, TOutput>
    where TInput : notnull
{
    public abstract string Name { get; }

    private const int WorkerThreadShutdownTimeoutMs = 2000;

    private readonly int capacity;
    private readonly object cacheLock = new();
    private readonly Dictionary<TInput, CacheEntry> cache = new();
    private readonly LinkedList<TInput> lruList = new();
    private readonly HashSet<TInput> requestQueue = new();
    private readonly object queueLock = new();
    private readonly Thread? workerThread;
    private volatile bool isDisposed = false;

    public int Count
    {
        get
        {
            lock (cacheLock)
            {
                return cache.Count;
            }
        }
    }

    /// <summary>
    /// Represents a cached entry with its ref-counted output and position in the LRU list.
    /// <see cref="RefCounted"/> is null when the output was computed but its value was null
    /// (e.g. a map with a hidden preview). The null result is still cached to avoid recomputation.
    /// </summary>
    private sealed class CacheEntry
    {
        public RefCountedValue<TOutput>? RefCounted { get; }
        public LinkedListNode<TInput> LruNode { get; set; }

        public CacheEntry(RefCountedValue<TOutput>? refCounted, LinkedListNode<TInput> lruNode)
        {
            RefCounted = refCounted;
            LruNode = lruNode;
        }
    }

    /// <summary>
    /// Initializes a new instance of the CacheManagerBase and starts the worker thread immediately.
    /// </summary>
    /// <param name="capacity">Maximum number of outputs to keep in cache. Must be positive.</param>
    public CacheManagerBase(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive.", nameof(capacity));

        this.capacity = capacity;

        workerThread = new Thread(ProcessRequests)
        {
            IsBackground = true,
            Name = $"{Name}-Worker"
        };
        workerThread.Start();
    }

    /// <summary>
    /// Attempts to acquire a lease for the cached output of the specified input.
    /// Updates LRU order if found. The lease is acquired inside <see cref="cacheLock"/>
    /// so the ref count is incremented before any concurrent eviction can decrement it.
    /// </summary>
    private bool TryGetLease(TInput input, out CacheLease<TOutput>? lease)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        lock (cacheLock)
        {
            if (cache.TryGetValue(input, out CacheEntry? entry))
            {
                // Move to front of LRU list (most recently used)
                lruList.Remove(entry.LruNode);
                entry.LruNode = lruList.AddFirst(input);

                // Acquire inside the lock: ref count is incremented while the cache still
                // holds its own reference, guaranteeing the value cannot be disposed yet.
                lease = entry.RefCounted?.AcquireLease();

                return true;
            }

            lease = null;
            return false;
        }
    }

    public bool Request(TInput input, out CacheLease<TOutput>? lease, bool syncLoadOnCacheMiss = false, bool addToQueue = true)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (isDisposed)
            throw new ObjectDisposedException(nameof(CacheManagerBase<TInput, TOutput>));

        // Check if already cached
        if (TryGetLease(input, out CacheLease<TOutput>? cachedLease))
        {
            lease = cachedLease;
            return true;
        }

        // If not cached and sync load is allowed, attempt to load immediately (may be CPU-intensive)
        if (syncLoadOnCacheMiss)
        {
            TOutput? output = ComputeOutputForInput(input);

            // Add to cache even if the output is null
            lease = AddToCache(input, output);
            return true;
        }

        // Queue for processing (HashSet prevents duplicates)
        if (addToQueue)
        {
            lock (queueLock)
            {
                if (requestQueue.Add(input))
                {
                    // Signal worker thread that new work is available
                    Monitor.Pulse(queueLock);
                }
            }
        }

        lease = null;
        return false;
    }

    /// <summary>
    /// Adds an output to the cache and returns a lease for it.
    /// If the input is already cached, disposes the duplicate output, updates LRU order,
    /// and returns a new lease for the existing entry.
    /// Note: If the input is already cached, the provided <paramref name="output"/> is disposed.
    /// </summary>
    private CacheLease<TOutput>? AddToCache(TInput input, TOutput? output)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        lock (cacheLock)
        {
            // If disposal happened while output was being computed outside the lock,
            // don't reintroduce a new cache-owned reference.
            if (isDisposed)
            {
                GetDisposeAction(output)?.Invoke();
                return null;
            }

            if (cache.TryGetValue(input, out CacheEntry? existingEntry))
            {
                // Already cached: discard the duplicate output and return a lease for the existing entry.
                // Invoked inside the lock to prevent a race where another thread could
                // access the output after it has already been disposed.
                GetDisposeAction(output)?.Invoke();
                lruList.Remove(existingEntry.LruNode);
                existingEntry.LruNode = lruList.AddFirst(input);
                return existingEntry.RefCounted?.AcquireLease();
            }

            // Evict if at capacity
            if (cache.Count >= capacity)
                EvictLeastRecentlyUsed();

            // Add new entry; RefCounted is null when output itself is null
            RefCountedValue<TOutput>? refCounted = output != null
                ? new RefCountedValue<TOutput>(output, GetDisposeAction(output))
                : null;
            LinkedListNode<TInput> node = lruList.AddFirst(input);
            cache[input] = new CacheEntry(refCounted, node);

            // Acquire lease inside the lock so the caller's ref is counted before any eviction
            return refCounted?.AcquireLease();
        }
    }

    public void Clear()
    {
        lock (cacheLock)
        {
            // Release the cache's own reference to every entry.
            // Entries still held by caller leases are disposed when those leases are released.
            foreach (CacheEntry entry in cache.Values)
                entry.RefCounted?.Release();

            cache.Clear();
            lruList.Clear();
        }
    }

    /// <summary>
    /// Computes the output for a given input. May be called on the worker thread or the calling
    /// thread and may be CPU-intensive.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The output.</returns>
    protected abstract TOutput? ComputeOutputForInput(TInput input);

    /// <summary>
    /// Returns an <see cref="Action"/> that should be invoked when the ref count of a cached
    /// <paramref name="value"/> entry reaches zero (i.e. the entry has been evicted from the
    /// cache and all caller leases have been released).
    /// Returns <c>null</c> by default; override in a subclass to perform cleanup such as
    /// calling <see cref="IDisposable.Dispose"/> on the value.
    /// </summary>
    /// <param name="value">The value whose lifetime is ending.</param>
    protected virtual Action? GetDisposeAction(TOutput? value) => null;

    /// <summary>
    /// Worker thread that processes computation requests sequentially.
    /// </summary>
    private void ProcessRequests()
    {
        while (!isDisposed)
        {
            TInput? input = default;
            bool inputFound = false;

            lock (queueLock)
            {
                // Wait for work or disposal
                while (requestQueue.Count == 0 && !isDisposed)
                {
                    Monitor.Wait(queueLock);
                }

                // Exit if disposed
                if (isDisposed)
                    break;

                // Get first item from HashSet
                using var enumerator = requestQueue.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    inputFound = true;
                    input = enumerator.Current;
                    requestQueue.Remove(input);
                }
            }

            // If no input, loop back to wait
            if (!inputFound)
                continue;

            try
            {
                // Check if already cached (might have been computed by another request)
                if (TryGetLease(input!, out CacheLease<TOutput>? existingLease))
                {
                    // Release immediately; this thread does not use the value
                    existingLease?.Dispose();
                    continue;
                }

                // Get the output for the input. This is the CPU-intensive operation.
                TOutput? output = ComputeOutputForInput(input!);

                // Add to cache even if the output is null; release the worker's lease right away
                using CacheLease<TOutput>? workerLease = AddToCache(input!, output);
            }
            catch (Exception ex)
            {
                Logger.Log($"{Name}: Failed to get the output for input '{input}'. Error: {ex.ToString()}");
            }
        }
    }

    /// <summary>
    /// Evicts the least recently used entry from the cache and releases the cache's reference.
    /// Must be called while holding <see cref="cacheLock"/>.
    /// </summary>
    private void EvictLeastRecentlyUsed()
    {
        if (lruList.Last == null)
            return;

        TInput lruInput = lruList.Last.Value;
        lruList.RemoveLast();

        if (cache.TryGetValue(lruInput, out CacheEntry? entry))
        {
            cache.Remove(lruInput);

            // Release the cache's reference. If no caller leases exist, the value is disposed now.
            // If a caller still holds a lease, the value is disposed when that lease is released.
            entry.RefCounted?.Release();
        }
    }

    /// <summary>
    /// Disposes the cache manager. Releases the cache's reference to all entries.
    /// Values still leased by callers are disposed when those leases are released.
    /// </summary>
    public void Dispose()
    {
        if (isDisposed)
            return;

        isDisposed = true;

        // Signal worker thread to stop
        lock (queueLock)
        {
            Monitor.Pulse(queueLock);
        }

        // Wait for worker thread to finish
        if (workerThread != null && workerThread.IsAlive)
        {
            if (!workerThread.Join(WorkerThreadShutdownTimeoutMs))
            {
                // Log warning if thread doesn't terminate gracefully
                Logger.Log($"{Name}: Worker thread did not terminate within timeout period.");
            }
        }

        // Release cache's references to all entries
        Clear();
    }
}
