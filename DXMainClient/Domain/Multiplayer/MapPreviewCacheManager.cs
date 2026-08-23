#nullable enable
using ClientCore.Caching;

using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// Thread-safe manager for caching map preview images with LRU eviction policy.
/// Processes image extraction requests sequentially to limit CPU usage to a single thread.
/// Images are disposed via ref-counting once evicted and all caller leases are released.
/// </summary>
public class MapPreviewCacheManager : DisposableCacheManagerBase<Map, Image>, IMapPreviewCacheManager
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
