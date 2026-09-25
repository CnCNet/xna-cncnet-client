#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;

using ClientGUI;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>Coordinates requests, cancellation and map lifetime; delegates rendering and disk storage.</summary>
    internal static class MapPreviewGenerationService
    {
        private static readonly SemaphoreSlim Queue = new(1, 1);
        private static readonly object Sync = new();
        private static readonly HashSet<string> Pending = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Map> Maps = new(StringComparer.OrdinalIgnoreCase);
        private static bool inGame;
        private static CancellationTokenSource? activeCancellation;
        private static readonly MapPreviewDiskCache Cache = new(CacheDirectory, new ExternalMapPreviewExtractor());
        private static string Root => ProgramConstants.GamePath;
        internal static string CacheDirectory => Path.Combine(Root, "Client", "MapPreviewCache");
        public static event Action<Map, bool, string?>? Completed;
        public static event Action<Map, string>? Progress;
        public static bool Configured => !string.IsNullOrWhiteSpace(ClientConfiguration.Instance.MapRendererPath)
            && !string.IsNullOrWhiteSpace(ClientConfiguration.Instance.MapRendererArguments);
        public static bool Enabled => Configured && UserINISettings.Instance.RenderMapPreviews.Value;
        public static bool Selected => Enabled && UserINISettings.Instance.ShowGeneratedMapPreviews.Value;

        static MapPreviewGenerationService()
        {
            GameProcessLogic.GameProcessStarting += () => { lock (Sync) { inGame = true; CancelRendering(); } };
            GameProcessLogic.GameProcessExited += () => { lock (Sync) inGame = false; };
            UserINISettings.Instance.SettingsSaved += (sender, args) => { if (!Selected) lock (Sync) CancelRendering(); };
        }

        private static void CancelRendering() => activeCancellation?.Cancel();

        private static bool StillPresent(Map map) => Maps.Values.Any(m => m.SHA1 == map.SHA1 && File.Exists(m.CompleteFilePath));

        public static void Register(IEnumerable<Map> maps)
        {
            lock (Sync)
            {
                Maps.Clear();
                foreach (var map in maps) Maps[map.CompleteFilePath] = map;
            }
            _ = Task.Run(PruneAsync);
        }

        internal static async Task PruneAsync()
        {
            await Queue.WaitAsync().ConfigureAwait(false);
            try
            {
                lock (Sync)
                {
                    Cache.Prune(Maps.Values.Where(m => File.Exists(m.CompleteFilePath)).Select(m => m.SHA1));
                }
            }
            catch (Exception e) { Logger.Log("Map preview cache pruning failed: " + e.Message); }
            finally { Queue.Release(); }
        }

        public static void Remove(Map map)
        {
            if (map == null) return;
            lock (Sync) Maps.Remove(map.CompleteFilePath);
            _ = Task.Run(PruneAsync);
        }

        public static string? CachedImage(Map map) => CachedSource(map)?.ImmediateImagePath;
        internal static MapPreviewSource? CachedSource(Map map) => Selected ? Cache.Read(map) : null;

        public static Task<bool> Request(Map? map, bool force = false)
        {
            if (map == null || !Selected || !MapPreviewDiskCache.ValidHash(map.SHA1)) return Task.FromResult(false);
            lock (Sync)
            {
                if (inGame || !Pending.Add(map.SHA1)) return Task.FromResult(false);
                Maps[map.CompleteFilePath] = map;
            }
            return Task.Run(async () =>
            {
                await Queue.WaitAsync().ConfigureAwait(false);
                string? error = null;
                bool changed = false;
                using var cancellation = new CancellationTokenSource();
                try
                {
                    lock (Sync)
                    {
                        if (inGame || !StillPresent(map) || !Selected) return false;
                        activeCancellation = cancellation;
                    }
                    var config = ClientConfiguration.Instance;
                    var options = new MapPreviewRenderOptions(Root, config.MapRendererPath,
                        config.MapRendererArguments, config.MapRendererVersion,
                        config.MapRendererWidth, config.MapRendererHeight, config.MapRendererTimeoutSeconds);
                    changed = await Cache.GenerateAsync(map, options, force,
                        () => Progress?.Invoke(map, "Generating hi-res map previewÃ¢â‚¬Â¦"),
                        publish =>
                        {
                            lock (Sync)
                            {
                                if (inGame || !Selected || !StillPresent(map) || cancellation.IsCancellationRequested) return false;
                                publish();
                                return true;
                            }
                        }, cancellation.Token).ConfigureAwait(false);
                    return changed;
                }
                catch (OperationCanceledException) { return false; }
                catch (Exception e)
                {
                    error = e.Message;
                    Logger.Log("Map preview generation failed for " + map.CompleteFilePath + ": " + e);
                    return false;
                }
                finally
                {
                    lock (Sync)
                    {
                        activeCancellation = null;
                        Pending.Remove(map.SHA1);
                    }
                    Queue.Release();
                    Progress?.Invoke(map, string.Empty);
                    Completed?.Invoke(map, changed, error);
                }
            });
        }
    }
}