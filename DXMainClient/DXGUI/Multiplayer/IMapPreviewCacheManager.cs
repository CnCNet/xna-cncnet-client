#nullable enable
using System;

using DTAClient.Domain.Multiplayer;

using SixLabors.ImageSharp;

namespace DTAClient.DXGUI.Multiplayer
{
    public interface IMapPreviewCacheManager : IDisposable
    {
        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Clears all cached images. The manager does not call Dispose() on the images; it assumes they are managed and will be collected by the garbage collector.
        /// </summary>
        public void Clear();

        /// <summary>
        /// Requests an image to be extracted for the specified map.
        /// </summary>
        /// <param name="map">The map to extract the image for.</param>
        /// <param name="syncLoadOnCacheMiss">If true, the method will attempt to load the image immediately if it's not cached, which may be CPU-intensive. If false, the map will be queued for asynchronous processing if <see cref="addToQueue"/> holds.</param>
        /// <param name="addToQueue">This parameter is ignored if <see cref="syncLoadOnCacheMiss"/> is true. Otherwise, if true, the map will be added to the processing queue if not already cached; if false, the method will simply return null on cache miss without queuing.</param>
        /// <param name="image">The cached image if found or loaded.</param>
        /// <returns>True if the image was found in cache or loaded synchronously; false if the image is not available yet.</returns>
        public bool RequestImage(Map map, out Image? image, bool syncLoadOnCacheMiss = false, bool addToQueue = true);
    }
}