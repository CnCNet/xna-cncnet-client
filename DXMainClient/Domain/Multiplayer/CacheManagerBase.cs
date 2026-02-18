#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// Thread-safe manager for caching outputs with LRU eviction policy.
/// Processes computation requests sequentially to limit CPU usage to a single thread.
/// Note: this manager assumes the `TOutput` objects are managed, so it never disposes them directly.
/// </summary>
public abstract class CacheManagerBase<TInput, TOutput> : ICacheManager<TInput, TOutput> where TInput : notnull
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

    public int Count => cache.Count;

    /// <summary>
    /// Represents a cached TOutput entry with its position in the LRU list.
    /// </summary>
    private class CacheEntry
    {
        public TOutput? Output { get; }
        public LinkedListNode<TInput> LruNode { get; set; }

        public CacheEntry(TOutput? output, LinkedListNode<TInput> lruNode)
        {
            Output = output;
            LruNode = lruNode;
        }
    }

    /// <summary>
    /// Initializes a new instance of the InputPreviewCacheManager and start the worker thread immediately.
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
    /// Attempts to get a cached output for the specified input.
    /// Updates LRU order if found.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="output">The cached output if found.</param>
    /// <returns>True if the output was found in cache; otherwise false.</returns>
    private bool TryGet(TInput input, out TOutput? output)
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
                output = entry.Output;
                return true;
            }

            output = default;
            return false;
        }
    }

    public bool Request(TInput input, out TOutput? output, bool syncLoadOnCacheMiss = false, bool addToQueue = true)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (isDisposed)
            throw new ObjectDisposedException(nameof(CacheManagerBase<TInput, TOutput>));

        // Check if already cached
        if (TryGet(input, out TOutput? cachedOutput))
        {
            output = cachedOutput;

            return true;
        }

        // If not cached and sync load is allowed, attempt to load immediately (may be CPU-intensive)
        if (syncLoadOnCacheMiss)
        {
            output = ComputeOutputForInput(input);

            // Add to cache even if the output is null
            AddToCache(input, output);

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

        output = default;

        return false;
    }

    /// <summary>
    /// Manually adds an output to the cache.
    /// Useful for pre-loading or when output is obtained from other sources.
    /// Note: If the input is already cached, this method updates LRU order but does NOT replace the cached output.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="output">The output.</param>
    /// <returns>True if the output was added to cache; false if output was already cached.</returns>
    private bool AddToCache(TInput input, TOutput? output)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        lock (cacheLock)
        {
            // If already cached, update LRU order but don't replace
            if (cache.TryGetValue(input, out CacheEntry? existingEntry))
            {
                lruList.Remove(existingEntry.LruNode);
                existingEntry.LruNode = lruList.AddFirst(input);
                return false;
            }

            // Evict if at capacity
            if (cache.Count >= capacity)
                EvictLeastRecentlyUsed();

            // Add new entry
            LinkedListNode<TInput> node = lruList.AddFirst(input);
            cache[input] = new CacheEntry(output, node);
            return true;
        }
    }

    public void Clear()
    {
        lock (cacheLock)
        {
            cache.Clear();
            lruList.Clear();
        }
    }

    /// <summary>
    /// Computes the output for a given input. This method may or might not be called by the worker thread and may be CPU-intensive.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The output.</returns>
    protected abstract TOutput? ComputeOutputForInput(TInput input);

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
                if (TryGet(input!, out _))
                    continue;

                // Get the output for the input. This is the CPU-intensive operation.
                TOutput? output = ComputeOutputForInput(input!);

                // Add to cache even if the output is null
                AddToCache(input!, output);
            }
            catch (Exception ex)
            {
                Logger.Log($"{Name}: Failed to get the output for input '{input}'. Error: {ex.ToString()}");
            }
        }
    }

    /// <summary>
    /// Evicts the least recently used output from the cache.
    /// Must be called within cacheLock.
    /// </summary>
    private void EvictLeastRecentlyUsed()
    {
        if (lruList.Last == null)
            return;

        TInput lruInput = lruList.Last.Value;
        lruList.RemoveLast();

        if (cache.TryGetValue(lruInput, out CacheEntry? entry))
            cache.Remove(lruInput);
    }

    /// <summary>
    /// Disposes the cache manager. Does not dispose cached outputs directly; left to garbage collector.
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

        // Clear cache
        Clear();
    }

}
