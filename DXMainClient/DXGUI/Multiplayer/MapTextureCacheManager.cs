using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DTAClient.Domain.Multiplayer;

using SixLabors.ImageSharp;

namespace DTAClient.DXGUI.Multiplayer
{
    public class MapTextureCacheManager : IDisposable
    {
        public const int MaxCacheSize = 100;
        public const int SleepIntervalMS = 100;

        private readonly ConcurrentDictionary<Map, Image> mapTextures = [];

        private readonly ConcurrentDictionary<Map, byte> missedMaps = [];

        private readonly CancellationTokenSource cancellationTokenSource = new();

        public MapTextureCacheManager() =>
            Task.Run(() => MapTextureLoadingService(cancellationTokenSource.Token));

        public void Dispose() =>
            cancellationTokenSource?.Cancel();

        public Image GetMapTextureIfAvailable(Map map)
        {
            if (mapTextures.TryGetValue(map, out Image image))
                return image;

            if (missedMaps.Count < MaxCacheSize)
                missedMaps.TryAdd(map, 0);

            return null;
        }

        private async Task MapTextureLoadingService(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Clear cache if it's too big
                if (mapTextures.Count > MaxCacheSize)
                    mapTextures.Clear();

                if (!missedMaps.IsEmpty)
                {
                    var missedMapCopy = missedMaps.ToArray();
                    foreach ((Map missedMap, _) in missedMapCopy)
                    {
                        if (mapTextures.Count > MaxCacheSize)
                            break;

                        missedMaps.TryRemove(missedMap, out _);

                        if (mapTextures.ContainsKey(missedMap))
                            continue;

                        Image image = await Task.Run(missedMap.ExtractMapPreview);
                        mapTextures.TryAdd(missedMap, image);
                    }

                }

                await Task.Delay(SleepIntervalMS);
            }
        }

    }
}
