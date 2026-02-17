#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DTAClient.Domain.Multiplayer;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// Thread-safe manager for caching map preview textures with LRU eviction policy.
/// Processes texture extraction requests sequentially to limit CPU usage to a single thread.
/// 
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create a cache manager with capacity of 50 textures
/// var cacheManager = new MapTextureCacheManager(capacity: 50);
/// 
/// // Synchronous check - returns immediately if cached
/// if (cacheManager.TryGetTexture(map, out Texture2D? texture))
/// {
///     // Use the cached texture
///     DrawTexture(texture);
/// }
/// 
/// // Asynchronous request - queues for loading if not cached
/// cacheManager.RequestTexture(map, loadedTexture =>
/// {
///     if (loadedTexture != null)
///     {
///         // Texture loaded, update UI on appropriate thread
///         DrawTexture(loadedTexture);
///     }
/// });
/// 
/// // Manually add a pre-loaded texture
/// Texture2D preloadedTexture = LoadTextureFromFile("preview.png");
/// cacheManager.AddToCache(map, preloadedTexture);
/// 
/// // Clean up
/// cacheManager.Dispose();
/// </code>
/// </para>
/// 
/// <para>
/// <b>Thread Safety:</b><br/>
/// - All public methods are thread-safe<br/>
/// - TryGetTexture can be called from any thread<br/>
/// - RequestTexture callbacks are invoked on the worker thread<br/>
/// - The worker thread processes one texture at a time to limit CPU usage
/// </para>
/// 
/// <para>
/// <b>Memory Management:</b><br/>
/// - When cache reaches capacity, least recently used textures are evicted<br/>
/// - Evicted textures are NOT disposed automatically<br/>
/// - Use Dispose(disposeTextures: true) to dispose all cached textures<br/>
/// - Caller is responsible for texture lifetime management
/// </para>
/// </summary>
public class MapTextureCacheManager : IDisposable
{
    private const int WorkerThreadShutdownTimeoutMs = 2000;

    private readonly int capacity;
    private readonly object cacheLock = new();
    private readonly Dictionary<Map, CacheEntry> cache = new();
    private readonly LinkedList<Map> lruList = new();
    private readonly ConcurrentQueue<TextureRequest> requestQueue = new();
    private readonly Thread? workerThread;
    private readonly AutoResetEvent requestEvent = new(false);
    private volatile bool isDisposed = false;

    /// <summary>
    /// Represents a cached texture entry with its position in the LRU list.
    /// </summary>
    private class CacheEntry
    {
        public Texture2D Texture { get; }
        public LinkedListNode<Map> LruNode { get; set; }

        public CacheEntry(Texture2D texture, LinkedListNode<Map> lruNode)
        {
            Texture = texture;
            LruNode = lruNode;
        }
    }

    /// <summary>
    /// Represents a texture loading request with completion notification.
    /// </summary>
    private class TextureRequest
    {
        public Map Map { get; }
        public Action<Texture2D?>? Callback { get; }

        public TextureRequest(Map map, Action<Texture2D?>? callback)
        {
            Map = map;
            Callback = callback;
        }
    }

    /// <summary>
    /// Initializes a new instance of the MapTextureCacheManager.
    /// </summary>
    /// <param name="capacity">Maximum number of textures to keep in cache. Must be positive.</param>
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
    /// Attempts to get a cached texture for the specified map.
    /// Updates LRU order if found.
    /// </summary>
    /// <param name="map">The map to get the texture for.</param>
    /// <param name="texture">The cached texture if found; otherwise null.</param>
    /// <returns>True if the texture was found in cache; otherwise false.</returns>
    public bool TryGetTexture(Map map, out Texture2D? texture)
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
                texture = entry.Texture;
                return true;
            }

            texture = null;
            return false;
        }
    }

    /// <summary>
    /// Requests a texture to be loaded for the specified map.
    /// If the texture is already cached, the callback is invoked immediately.
    /// Otherwise, the request is queued for processing on the worker thread.
    /// </summary>
    /// <param name="map">The map to load the texture for.</param>
    /// <param name="callback">Optional callback to invoke when the texture is ready.</param>
    public void RequestTexture(Map map, Action<Texture2D?>? callback = null)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        if (isDisposed)
            throw new ObjectDisposedException(nameof(MapTextureCacheManager));

        // Check if already cached
        if (TryGetTexture(map, out Texture2D? cachedTexture))
        {
            callback?.Invoke(cachedTexture);
            return;
        }

        // Queue for processing
        requestQueue.Enqueue(new TextureRequest(map, callback));
        requestEvent.Set();
    }

    /// <summary>
    /// Manually adds a texture to the cache.
    /// Useful for pre-loading or when texture is obtained from other sources.
    /// </summary>
    /// <param name="map">The map associated with the texture.</param>
    /// <param name="texture">The texture to cache.</param>
    public void AddToCache(Map map, Texture2D texture)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));
        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

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
            cache[map] = new CacheEntry(texture, node);
        }
    }

    /// <summary>
    /// Clears all cached textures.
    /// </summary>
    /// <param name="disposeTextures">Whether to dispose the textures when clearing. Default is false.</param>
    public void Clear(bool disposeTextures = false)
    {
        lock (cacheLock)
        {
            if (disposeTextures)
            {
                // Dispose textures if requested
                foreach (var entry in cache.Values)
                {
                    entry.Texture?.Dispose();
                }
            }

            cache.Clear();
            lruList.Clear();
        }
    }

    /// <summary>
    /// Worker thread that processes texture loading requests sequentially.
    /// </summary>
    private void ProcessRequests()
    {
        while (!isDisposed)
        {
            // Wait for a request or disposal
            requestEvent.WaitOne(1000); // Timeout to periodically check disposal

            while (requestQueue.TryDequeue(out TextureRequest? request))
            {
                if (isDisposed)
                    break;

                try
                {
                    // Check if already cached (might have been loaded by another request)
                    if (TryGetTexture(request.Map, out Texture2D? cachedTexture))
                    {
                        request.Callback?.Invoke(cachedTexture);
                        continue;
                    }

                    // Load the texture (this is the CPU-intensive operation)
                    Texture2D? texture = request.Map.LoadPreviewTexture();

                    if (texture != null)
                    {
                        AddToCache(request.Map, texture);
                    }

                    // Notify callback
                    request.Callback?.Invoke(texture);
                }
                catch (Exception ex)
                {
                    // Log the error for debugging purposes
                    Logger.Log($"MapTextureCacheManager: Failed to load texture for map. Error: {ex.Message}");
                    
                    // Notify callback with null
                    request.Callback?.Invoke(null);
                }
            }
        }
    }

    /// <summary>
    /// Evicts the least recently used texture from the cache.
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
            // Note: We don't dispose the texture here as it might still be in use elsewhere.
            // The caller is responsible for managing texture lifetime if needed.
            cache.Remove(lruMap);
        }
    }

    /// <summary>
    /// Disposes the cache manager and releases all resources.
    /// </summary>
    /// <param name="disposeTextures">Whether to dispose cached textures. Default is false.</param>
    public void Dispose(bool disposeTextures = false)
    {
        if (isDisposed)
            return;

        isDisposed = true;

        // Signal worker thread to stop
        requestEvent.Set();

        // Wait for worker thread to finish
        workerThread?.Join(2000);

        // Clear cache
        Clear(disposeTextures);

        // Dispose synchronization primitives
        requestEvent.Dispose();
    }

    /// <summary>
    /// Disposes the cache manager and releases all resources.
    /// </summary>
    void IDisposable.Dispose()
    {
        Dispose(disposeTextures: false);
    }
}
