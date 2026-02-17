#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using DTAClient.Domain.Multiplayer;

using Rampastring.Tools;

using SixLabors.ImageSharp;

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// Thread-safe manager for caching map preview images with LRU eviction policy.
/// Processes image extraction requests sequentially to limit CPU usage to a single thread.
/// Note: this manager assumes the `Image` objects are managed, so it never disposes them directly.
/// </summary>
public class MapPreviewCacheManager : IDisposable, IMapPreviewCacheManager
{
    private const int WorkerThreadShutdownTimeoutMs = 2000;

    private readonly int capacity;
    private readonly object cacheLock = new();
    private readonly Dictionary<Map, CacheEntry> cache = new();
    private readonly LinkedList<Map> lruList = new();
    private readonly HashSet<Map> requestQueue = new();
    private readonly object queueLock = new();
    private readonly Thread? workerThread;
    private readonly AutoResetEvent requestEvent = new(false);
    private volatile bool isDisposed = false;

    /// <summary>
    /// Represents a cached image entry with its position in the LRU list.
    /// </summary>
    private class CacheEntry
    {
        public Image Image { get; }
        public LinkedListNode<Map> LruNode { get; set; }

        public CacheEntry(Image image, LinkedListNode<Map> lruNode)
        {
            Image = image;
            LruNode = lruNode;
        }
    }

    /// <summary>
    /// Initializes a new instance of the MapPreviewCacheManager.
    /// </summary>
    /// <param name="capacity">Maximum number of images to keep in cache. Must be positive.</param>
    /// <param name="startWorker">Whether to start the worker thread immediately. Default is true.</param>
    public MapPreviewCacheManager(int capacity, bool startWorker = true)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive.", nameof(capacity));

        this.capacity = capacity;

        if (startWorker)
        {
            workerThread = new Thread(ProcessRequests)
            {
                IsBackground = true,
                Name = nameof(MapPreviewCacheManager) + "Worker"
            };
            workerThread.Start();
        }
    }

    /// <summary>
    /// Attempts to get a cached image for the specified map.
    /// Updates LRU order if found.
    /// </summary>
    /// <param name="map">The map to get the image for.</param>
    /// <param name="image">The cached image if found; otherwise null.</param>
    /// <returns>True if the image was found in cache; otherwise false.</returns>
    private bool TryGetImage(Map map, out Image? image)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        lock (cacheLock)
        {
            if (cache.TryGetValue(map, out CacheEntry? entry))
            {
                // Move to front of LRU list (most recently used)
                lruList.Remove(entry.LruNode);
                entry.LruNode = lruList.AddFirst(map);
                image = entry.Image;
                return true;
            }

            image = null;
            return false;
        }
    }

    /// <summary>
    /// Requests an image to be extracted for the specified map.
    /// </summary>
    /// <param name="map">The map to extract the image for.</param>
    /// <returns>The cached image if already available; otherwise null. The image will be extracted and cached asynchronously.</returns>
    public Image? RequestImage(Map map)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        if (isDisposed)
            throw new ObjectDisposedException(nameof(MapPreviewCacheManager));

        // Check if already cached
        if (TryGetImage(map, out Image? cachedImage))
            return cachedImage;

        // Queue for processing (HashSet prevents duplicates)
        lock (queueLock)
        {
            if (requestQueue.Add(map))
            {
                // Signal worker thread that new work is available
                Monitor.Pulse(queueLock);
            }
        }

        return null;
    }

    /// <summary>
    /// Manually adds an image to the cache.
    /// Useful for pre-loading or when image is obtained from other sources.
    /// Note: If the map is already cached, this method updates LRU order but does NOT replace the cached image.
    /// </summary>
    /// <param name="map">The map associated with the image.</param>
    /// <param name="image">The image to cache.</param>
    /// <returns>True if the image was added to cache; false if map was already cached.</returns>
    private bool AddToCache(Map map, Image image)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        lock (cacheLock)
        {
            // If already cached, update LRU order but don't replace
            if (cache.TryGetValue(map, out CacheEntry? existingEntry))
            {
                lruList.Remove(existingEntry.LruNode);
                existingEntry.LruNode = lruList.AddFirst(map);
                return false;
            }

            // Evict if at capacity
            if (cache.Count >= capacity)
                EvictLeastRecentlyUsed();

            // Add new entry
            LinkedListNode<Map> node = lruList.AddFirst(map);
            cache[map] = new CacheEntry(image, node);
            return true;
        }
    }

    /// <summary>
    /// Clears all cached images.
    /// </summary>
    public void Clear()
    {
        lock (cacheLock)
        {
            cache.Clear();
            lruList.Clear();
        }
    }

    /// <summary>
    /// Worker thread that processes image extraction requests sequentially.
    /// </summary>
    private void ProcessRequests()
    {
        while (!isDisposed)
        {
            Map? map = null;

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

                // Recheck queue after wake (defensive)
                if (requestQueue.Count > 0)
                {
                    // Get first item from HashSet
                    using var enumerator = requestQueue.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        map = enumerator.Current;
                        requestQueue.Remove(map);
                    }
                }
            }

            // If no map, loop back to wait
            if (map == null)
                continue;

            try
            {
                // Check if already cached (might have been extracted by another request)
                if (TryGetImage(map, out Image? cachedImage))
                    continue;

                if (!map.IsNonImmediatePreviewImageAvailable())
                    continue;

                // Load the full map ini and extract the preview image. This operation is CPU-intensive.
                Image? image = map.GetNonImmediatePreviewImage();

                if (image != null)
                    AddToCache(map, image);
            }
            catch (Exception ex)
            {
                // Log the error for debugging purposes with map identifier
                string mapIdentifier = map.Name ?? map.BaseFilePath ?? "Unknown";
                Logger.Log($"MapPreviewCacheManager: Failed to extract preview image for map '{mapIdentifier}'. Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Evicts the least recently used image from the cache.
    /// Must be called within cacheLock.
    /// </summary>
    private void EvictLeastRecentlyUsed()
    {
        if (lruList.Last == null)
            return;

        Map lruMap = lruList.Last.Value;
        lruList.RemoveLast();

        if (cache.TryGetValue(lruMap, out CacheEntry? entry))
        {
            // Remove from cache but does not call image.Dispose() since we assume images are managed and will be collected by GC
            cache.Remove(lruMap);
        }
    }

    /// <summary>
    /// Disposes the cache manager. Does not dispose cached images directly; left to garbage collector.
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
                Logger.Log("MapPreviewCacheManager: Worker thread did not terminate within timeout period.");
            }
        }

        // Clear cache
        Clear();

        // Dispose synchronization primitives
        requestEvent.Dispose();
    }
}
