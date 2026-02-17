#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DTAClient.Domain.Multiplayer;
using Rampastring.Tools;
using SixLabors.ImageSharp;

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// Thread-safe manager for caching map preview images with LRU eviction policy.
/// Processes image extraction requests sequentially to limit CPU usage to a single thread.
/// 
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a cache manager with capacity of 50 images
/// var cacheManager = new MapTextureCacheManager(capacity: 50);
/// 
/// // Synchronous check - returns immediately if cached
/// if (cacheManager.TryGetImage(map, out Image? image))
/// {
///     // IMPORTANT: Convert to texture immediately or clone the image
///     // The cache owns the image and will dispose it when evicted
///     var texture = AssetLoader.TextureFromImage(image);
/// }
/// 
/// // Asynchronous request - queues for extraction if not cached
/// cacheManager.RequestImage(map, loadedImage =>
/// {
///     if (loadedImage != null)
///     {
///         // IMPORTANT: Convert to texture immediately or clone the image
///         // The cache owns the image and will dispose it when evicted
///         var texture = AssetLoader.TextureFromImage(loadedImage);
///     }
/// });
/// 
/// // Manually add a pre-extracted image
/// Image preloadedImage = MapPreviewExtractor.ExtractMapPreview(mapIni);
/// cacheManager.AddToCache(map, preloadedImage);
/// 
/// // Clean up
/// cacheManager.Dispose();
/// </code>
/// </para>
/// 
/// <para>
/// <b>Thread Safety:</b><br/>
/// - All public methods are thread-safe<br/>
/// - TryGetImage can be called from any thread<br/>
/// - RequestImage callbacks are invoked on the worker thread<br/>
/// - The worker thread processes one extraction at a time to limit CPU usage
/// </para>
/// 
/// <para>
/// <b>Memory Management:</b><br/>
/// - When cache reaches capacity, least recently used images are evicted and disposed<br/>
/// - The cache owns cached images and disposes them on eviction<br/>
/// - Callers should convert Image to Texture2D immediately or clone if keeping reference<br/>
/// - Use Dispose(disposeImages: true) to dispose all cached images on shutdown
/// </para>
/// </summary>
public class MapTextureCacheManager : IDisposable
{
    private const int WorkerThreadShutdownTimeoutMs = 2000;

    private readonly int capacity;
    private readonly object cacheLock = new();
    private readonly Dictionary<Map, CacheEntry> cache = new();
    private readonly LinkedList<Map> lruList = new();
    private readonly ConcurrentQueue<ImageRequest> requestQueue = new();
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
    /// Represents an image extraction request with completion notification.
    /// </summary>
    private class ImageRequest
    {
        public Map Map { get; }
        public Action<Image?>? Callback { get; }

        public ImageRequest(Map map, Action<Image?>? callback)
        {
            Map = map;
            Callback = callback;
        }
    }

    /// <summary>
    /// Initializes a new instance of the MapTextureCacheManager.
    /// </summary>
    /// <param name="capacity">Maximum number of images to keep in cache. Must be positive.</param>
    /// <param name="startWorker">Whether to start the worker thread immediately. Default is true.</param>
    public MapTextureCacheManager(int capacity, bool startWorker = true)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive.", nameof(capacity));

        this.capacity = capacity;

        if (startWorker)
        {
            workerThread = new Thread(ProcessRequests)
            {
                IsBackground = true,
                Name = "MapTextureCacheWorker"
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
    public bool TryGetImage(Map map, out Image? image)
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
    /// If the image is already cached, the callback is invoked immediately.
    /// Otherwise, the request is queued for processing on the worker thread.
    /// </summary>
    /// <param name="map">The map to extract the image for.</param>
    /// <param name="callback">Optional callback to invoke when the image is ready.</param>
    public void RequestImage(Map map, Action<Image?>? callback = null)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        if (isDisposed)
            throw new ObjectDisposedException(nameof(MapTextureCacheManager));

        // Check if already cached
        if (TryGetImage(map, out Image? cachedImage))
        {
            callback?.Invoke(cachedImage);
            return;
        }

        // Queue for processing
        requestQueue.Enqueue(new ImageRequest(map, callback));
        requestEvent.Set();
    }

    /// <summary>
    /// Manually adds an image to the cache.
    /// Useful for pre-loading or when image is obtained from other sources.
    /// </summary>
    /// <param name="map">The map associated with the image.</param>
    /// <param name="image">The image to cache.</param>
    public void AddToCache(Map map, Image image)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        lock (cacheLock)
        {
            // If already cached, update LRU order
            if (cache.TryGetValue(map, out CacheEntry? existingEntry))
            {
                lruList.Remove(existingEntry.LruNode);
                existingEntry.LruNode = lruList.AddFirst(map);
                return;
            }

            // Evict if at capacity
            if (cache.Count >= capacity)
            {
                EvictLeastRecentlyUsed();
            }

            // Add new entry
            LinkedListNode<Map> node = lruList.AddFirst(map);
            cache[map] = new CacheEntry(image, node);
        }
    }

    /// <summary>
    /// Clears all cached images.
    /// </summary>
    /// <param name="disposeImages">Whether to dispose the images when clearing. Default is false.</param>
    public void Clear(bool disposeImages = false)
    {
        lock (cacheLock)
        {
            if (disposeImages)
            {
                // Dispose images if requested
                foreach (var entry in cache.Values)
                {
                    entry.Image.Dispose();
                }
            }

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
            // Wait for a request or disposal (no timeout - rely on requestEvent.Set() in Dispose)
            requestEvent.WaitOne();

            while (requestQueue.TryDequeue(out ImageRequest? request))
            {
                if (isDisposed)
                    break;

                try
                {
                    // Check if already cached (might have been extracted by another request)
                    if (TryGetImage(request.Map, out Image? cachedImage))
                    {
                        request.Callback?.Invoke(cachedImage);
                        continue;
                    }

                    // Extract the preview image (this is the CPU-intensive operation)
                    Image? image = MapPreviewExtractor.ExtractMapPreview(
                        request.Map.GetCustomMapIniFile(loadPreviewTextureSection: true));

                    if (image != null)
                    {
                        AddToCache(request.Map, image);
                    }

                    // Notify callback
                    request.Callback?.Invoke(image);
                }
                catch (Exception ex)
                {
                    // Log the error for debugging purposes with map identifier
                    string mapIdentifier = request.Map.Name ?? request.Map.BaseFilePath ?? "Unknown";
                    Logger.Log($"MapTextureCacheManager: Failed to extract preview image for map '{mapIdentifier}'. Error: {ex.Message}");
                    
                    // Notify callback with null
                    request.Callback?.Invoke(null);
                }
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
            // Dispose the image to free memory since it's being evicted
            entry.Image.Dispose();
            cache.Remove(lruMap);
        }
    }

    /// <summary>
    /// Disposes the cache manager and releases all resources.
    /// </summary>
    /// <param name="disposeImages">Whether to dispose cached images. Default is false.</param>
    public void Dispose(bool disposeImages = false)
    {
        if (isDisposed)
            return;

        isDisposed = true;

        // Signal worker thread to stop
        requestEvent.Set();

        // Wait for worker thread to finish
        if (workerThread != null && workerThread.IsAlive)
        {
            if (!workerThread.Join(WorkerThreadShutdownTimeoutMs))
            {
                // Log warning if thread doesn't terminate gracefully
                Logger.Log("MapTextureCacheManager: Worker thread did not terminate within timeout period.");
            }
        }

        // Clear cache
        Clear(disposeImages);

        // Dispose synchronization primitives
        requestEvent.Dispose();
    }

    /// <summary>
    /// Disposes the cache manager and releases all resources.
    /// </summary>
    void IDisposable.Dispose()
    {
        Dispose(disposeImages: false);
    }
}
