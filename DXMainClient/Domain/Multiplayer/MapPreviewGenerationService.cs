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
        // The map hash each view currently shows. Renders no view wants are cancelled or skipped.
        private static readonly Dictionary<object, string> Wanted = new();
        private static bool inGame;
        private static CancellationTokenSource? activeCancellation;
        private static string? activeHash;
        private static readonly MapPreviewDiskCache Cache = new(CacheDirectory, new ExternalMapPreviewExtractor());
        private static string Root => ProgramConstants.GamePath;
        internal static string CacheDirectory => Path.Combine(Root, "Client", "MapPreviewCache");
        public static event Action<Map, bool, string?>? Completed;
        /// <summary>Raised with true when a map's render starts and false when it ends.</summary>
        public static event Action<Map, bool>? Progress;
        private static string RendererPath => ClientConfiguration.Instance.GetOperatingSystemVersion() == OSVersion.UNIX
            ? ClientConfiguration.Instance.UnixMapRendererPath
            : ClientConfiguration.Instance.MapRendererPath;
        public static bool Configured => !string.IsNullOrWhiteSpace(RendererPath)
            && !string.IsNullOrWhiteSpace(ClientConfiguration.Instance.MapRendererArguments);
        public static bool Enabled => Configured && UserINISettings.Instance.RenderMapPreviews.Value;
        public static bool Selected => Enabled && UserINISettings.Instance.ShowGeneratedMapPreviews.Value;

        /// <summary>Raised when <see cref="Enabled"/> or <see cref="Selected"/> changes, not on every settings save.</summary>
        public static event Action? ModeChanged;
        private static bool lastEnabled, lastSelected;

        static MapPreviewGenerationService()
        {
            lastEnabled = Enabled;
            lastSelected = Selected;
            GameProcessLogic.GameProcessStarting += () => { lock (Sync) { inGame = true; CancelRendering(); } };
            GameProcessLogic.GameProcessExited += () => { lock (Sync) inGame = false; };
            UserINISettings.Instance.SettingsSaved += (sender, args) => OnSettingsSaved();
        }

        private static void OnSettingsSaved()
        {
            bool enabled = Enabled, selected = Selected;
            lock (Sync)
            {
                if (enabled == lastEnabled && selected == lastSelected) return;
                lastEnabled = enabled;
                lastSelected = selected;
                if (!selected) CancelRendering();
            }
            ModeChanged?.Invoke();
        }

        private static void CancelRendering() => activeCancellation?.Cancel();

        private static bool IsWanted(string hash) => Wanted.Values.Any(h => string.Equals(h, hash, StringComparison.OrdinalIgnoreCase));

        private static void CancelUnwanted()
        {
            if (activeHash != null && !IsWanted(activeHash))
                CancelRendering();
        }

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
                // Holding Queue means no render can publish meanwhile, so file I/O can run outside Sync.
                List<Map> maps;
                lock (Sync) maps = Maps.Values.ToList();
                Cache.Prune(maps.Where(m => File.Exists(m.CompleteFilePath)).Select(m => m.SHA1));
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

        /// <summary>Records the map a view now shows and renders it if needed.
        /// A render that no view shows any more is cancelled, or skipped if still queued.</summary>
        public static Task<bool> Request(object view, Map? map, bool force = false)
        {
            if (map == null || !Selected || !MapPreviewDiskCache.ValidHash(map.SHA1))
            {
                lock (Sync)
                {
                    Wanted.Remove(view);
                    CancelUnwanted();
                }
                return Task.FromResult(false);
            }
            lock (Sync)
            {
                Wanted[view] = map.SHA1;
                CancelUnwanted();
            }
            return Enqueue(map, force);
        }

        private static Task<bool> Enqueue(Map map, bool force)
        {
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
                        if (inGame || !StillPresent(map) || !Selected || !IsWanted(map.SHA1)) return false;
                        activeCancellation = cancellation;
                        activeHash = map.SHA1;
                    }
                    var config = ClientConfiguration.Instance;
                    var options = new MapPreviewRenderOptions(Root, RendererPath,
                        config.MapRendererArguments, config.MapRendererVersion,
                        config.MapRendererWidth, config.MapRendererHeight, config.MapRendererTimeoutSeconds);
                    changed = await Cache.GenerateAsync(map, options, force,
                        () => Progress?.Invoke(map, true),
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
                    bool retry;
                    lock (Sync)
                    {
                        activeCancellation = null;
                        activeHash = null;
                        Pending.Remove(map.SHA1);
                        // A view may have switched back to this map while its cancelled render was stopping.
                        retry = cancellation.IsCancellationRequested && !inGame && Selected && IsWanted(map.SHA1);
                    }
                    Queue.Release();
                    Progress?.Invoke(map, false);
                    Completed?.Invoke(map, changed, error);
                    if (retry) _ = Enqueue(map, force);
                }
            });
        }
    }
}
