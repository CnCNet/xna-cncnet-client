#nullable enable
using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// Thread-safe manager for caching map preview images with LRU eviction policy.
/// Processes image extraction requests sequentially to limit CPU usage to a single thread.
/// Note: this manager assumes the `Image` objects are managed, so it never disposes them directly.
/// </summary>
public class MapPreviewCacheManager : CacheManagerBase<Map, Image>, IMapPreviewCacheManager
{
    public MapPreviewCacheManager(int capacity) : base(capacity) { }

    public override string Name => nameof(MapPreviewCacheManager);

    protected override Image? ComputeOutputForInput(Map map)
    {
        if (!map.IsNonImmediatePreviewImageAvailable())
            return null;

        Image? image = map.GetNonImmediatePreviewImage();
        return image;
    }
}
