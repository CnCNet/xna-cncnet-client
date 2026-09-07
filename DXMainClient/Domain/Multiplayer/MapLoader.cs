using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using ClientCore;
using ClientCore.Caching;
using ClientCore.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI;

using Image = SixLabors.ImageSharp.Image;

namespace DTAClient.Domain.Multiplayer
{
    public enum MapChangeType
    {
        Added,
        Updated,
        Removed
    }

    public class MapLoader : IDisposable
    {
        private const string CUSTOM_MAPS_DIRECTORY = "Maps/Custom";

        private const int CurrentCustomMapCacheVersion = 5;

        private static string GetCustomMapCacheFileName(int version) => version == 1 ? "custom_map_cache" : $"custom_map_cache_v{version}";

        private static readonly string CUSTOM_MAPS_CACHE = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, GetCustomMapCacheFileName(CurrentCustomMapCacheVersion));
        private static readonly IReadOnlyList<string> LEGACY_CUSTOM_MAP_CACHE_FILES = Enumerable.Range(0, CurrentCustomMapCacheVersion)
            .Select(version => SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, GetCustomMapCacheFileName(version)))
            .ToList();

        private const int CurrentMapTileLevelCacheVersion = 1;
        private static readonly string MAP_TILE_LEVEL_CACHE = SafePath.CombineFilePath(
            ProgramConstants.ClientUserFilesPath, $"map_tile_levels_v{CurrentMapTileLevelCacheVersion}");

        private const string MultiMapsSection = "MultiMaps";
        private const string GameModesSection = "GameModes";
        private const string GameModeAliasesSection = "GameModeAliases";
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };
        private MapFileWatcher mapFileWatcher;
        private readonly object mapModificationLock = new object();
        private readonly object tileLevelCacheLock = new object();
        private const int _mapChangeRetryCount = 3;

        private bool _tileLevelSupportLoaded;
        private IReadOnlyList<AutoMapOverlayDefinition> _autoMapOverlayDefs = Array.Empty<AutoMapOverlayDefinition>();
        private MapTileLevelCache _mapTileLevelCache;

        // Mutable buffer used only during the initial map-loading pass. After
        // LoadMapsInternalAsync publishes the first snapshot, it is set to null
        // and every subsequent update goes through ReplaceGameModeSnapshot under
        // mapModificationLock.
        // TODO: Consider refactoring this MapLoader class into two classes, one for initial loading and one for runtime updates, to avoid having this mutable state that is only used during initialization.
        private List<GameMode> _initialGameModes = [];

        private sealed class Snapshot
        {
            public IReadOnlyList<GameMode> GameModes { get; }
            public IReadOnlyGameModeMapCollection GameModeMaps { get; }

            public Snapshot(IReadOnlyList<GameMode> gameModes, IReadOnlyGameModeMapCollection gameModeMaps)
            {
                GameModes = gameModes;
                GameModeMaps = gameModeMaps;
            }
        }

        private volatile Snapshot _snapshot = new Snapshot(Array.Empty<GameMode>(), new GameModeMapCollection(Array.Empty<GameMode>()));

        /// <summary>
        /// List of game modes.
        /// </summary>
        public IReadOnlyList<GameMode> GameModes => _snapshot.GameModes;

        public IReadOnlyGameModeMapCollection GameModeMaps => _snapshot.GameModeMaps;

        /// <summary>
        /// An event that is fired when the maps have been loaded.
        /// </summary>
        public event EventHandler MapLoadingComplete;

        /// <summary>
        /// Fired when a map file is added, updated, or removed.
        /// </summary>
        public event EventHandler<MapChangedEventArgs> MapChanged;

        /// <summary>
        /// A list of game mode aliases.
        /// Every game mode entry that exists in this dictionary will get
        /// replaced by the game mode entries of the value string array
        /// when map is added to game mode map lists.
        /// </summary>
        private Dictionary<string, string[]> GameModeAliases = new Dictionary<string, string[]>();

        private Dictionary<string, string> _translatedMapNames = new();

        /// <summary>
        /// A dictionary of translated map names. Used to look up the 
        /// translated name of a map without knowing the ID of the map.
        /// </summary>
        public IReadOnlyDictionary<string, string> TranslatedMapNames => _translatedMapNames;

        /// <summary>
        /// List of gamemodes allowed to be used on custom maps in order for them to display in map list.
        /// </summary>
        private string[] AllowedGameModes = ClientConfiguration.Instance.AllowedCustomGameModes.Split(',');

        public const int MapPreviewCacheCapacity = 100;

        private readonly IMapPreviewCacheManager mapPreviewCacheManager = new MapPreviewCacheManager(capacity: MapPreviewCacheCapacity);

        public MapLoader() { }

        public void Initialize()
        {
            MapLoadingComplete += (sender, args) => StartMapFileWatcher();
        }

        /// <summary>
        /// Sets up file watching for maps.
        /// </summary>
        public void StartMapFileWatcher()
        {
            if (mapFileWatcher != null)
                return;

            string customMapsPath = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, CUSTOM_MAPS_DIRECTORY);

            mapFileWatcher = new MapFileWatcher(customMapsPath, ClientConfiguration.Instance.MapFileExtension);
            mapFileWatcher.MapFileChanged += OnMapFileChanged;
            mapFileWatcher.StartWatching();
        }

        /// <summary>
        /// Asynchronously loads maps based on INI info as well as those in the custom maps directory.
        /// </summary>
        public Task LoadMapsAsync() => Task.Run(LoadMapsInternalAsync);

        private async Task LoadMapsInternalAsync()
        {
            Logger.Log("MapLoader: Map loading task started.");
            var stopwatch = Stopwatch.StartNew();

            string mpMapsPath = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MPMapsIniPath);

            Logger.Log($"MapLoader: Loading maps from {mpMapsPath}.");

            IniFile mpMapsIni = new IniFile(mpMapsPath);

            EnsureTileLevelSupportLoaded();

            LoadGameModes(mpMapsIni);
            LoadGameModeAliases(mpMapsIni);
            // LoadMultiMapsAsync and LoadCustomMapsAsync both modify the game mode map collection. We intend to keep the collection non-thread-safe for performance, so the two methods must not be called simultaneously.
            await LoadMultiMapsAsync(mpMapsIni, _mapTileLevelCache, _autoMapOverlayDefs);
            await LoadCustomMapsAsync(_mapTileLevelCache, _autoMapOverlayDefs);

            SaveTileLevelCache();

            Logger.Log("MapLoader: Post-processing game mode map collections.");
            PublishSnapshot(_initialGameModes);
            _initialGameModes = null;

            // Clean up any name-based favorite entries after migration (legacy: changed from name to sha1)
            CleanupMigratedFavorites();

            stopwatch.Stop();

            Logger.Log($"MapLoader: Map loading complete. Total time: {stopwatch.ElapsedMilliseconds} ms");
            MapLoadingComplete?.Invoke(this, EventArgs.Empty);
        }

        private async void OnMapFileChanged(object sender, MapFileEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    await HandleMapFileAdded(e.FilePath);
                    break;
                case WatcherChangeTypes.Changed:
                    await HandleMapFileChanged(e.FilePath);
                    break;
                case WatcherChangeTypes.Deleted:
                    await HandleMapFileDeleted(e.FilePath);
                    break;
            }
        }

        private async Task HandleMapFileAdded(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                string baseFilePath = GetBaseFilePathFromFullPath(filePath);
                if (string.IsNullOrEmpty(baseFilePath))
                    return;

                // If, for instance, the file was just extracted, the program that created it may still
                // have a lock on the file. Retry a couple of times.
                Map map = null;
                bool success = false;

                for (int attempt = 0; attempt < _mapChangeRetryCount; attempt++)
                {
                    try
                    {
                        map = new Map(baseFilePath, true);
                        if (map.InitializeFromCustomMap())
                        {
                            success = true;
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        if (attempt < _mapChangeRetryCount - 1)
                            await Task.Delay(100);
                        else
                            throw;
                    }
                }

                if (success && map != null)
                {
                    ApplyTileLevelDataToMap(map, _mapTileLevelCache, _autoMapOverlayDefs);
                    SaveTileLevelCache();

                    lock (mapModificationLock)
                    {
                        List<GameMode> gameModeSnapshot = CloneGameModeSnapshot();

                        if (IsMapAlreadyLoaded(map.SHA1, gameModeSnapshot))
                            return;

                        AddMapToGameModes(map, gameModeSnapshot, true);
                        ReplaceGameModeSnapshot(gameModeSnapshot);

                        Logger.Log($"MapLoader: Added new map {map.Name} from {filePath}");
                        MapChanged?.Invoke(this, new MapChangedEventArgs(map, MapChangeType.Added));
                    }
                }
                else
                {
                    Logger.Log($"MapLoader: Failed to load map info from {filePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"MapLoader: Error adding map from {filePath}: {ex.Message}");
            }
        }

        private async Task HandleMapFileChanged(string filePath)
        {
            try
            {
                string baseFilePath = GetBaseFilePathFromFullPath(filePath);
                if (string.IsNullOrEmpty(baseFilePath))
                    return;

                // If editing a map, the program that saved the new version may still
                // have a lock on the file. Retry a couple of times.
                Map newMap = null;
                bool success = false;

                for (int attempt = 0; attempt < _mapChangeRetryCount; attempt++)
                {
                    try
                    {
                        newMap = new Map(baseFilePath, true);
                        if (newMap.InitializeFromCustomMap())
                        {
                            success = true;
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        if (attempt < _mapChangeRetryCount - 1)
                            await Task.Delay(100);
                        else
                            throw;
                    }
                }

                if (success && newMap != null)
                {
                    ApplyTileLevelDataToMap(newMap, _mapTileLevelCache, _autoMapOverlayDefs);
                    SaveTileLevelCache();

                    lock (mapModificationLock)
                    {
                        List<GameMode> gameModeSnapshot = CloneGameModeSnapshot();
                        string oldSHA1 = FindMapSHA1ByFilePath(baseFilePath, gameModeSnapshot);

                        if (!string.IsNullOrEmpty(oldSHA1))
                        {
                            if (oldSHA1 != newMap.SHA1)
                            {
                                // SHA1 changed, remove old and add new
                                RemoveMapBySHA1(oldSHA1, gameModeSnapshot);
                                AddMapToGameModes(newMap, gameModeSnapshot, true);
                                ReplaceGameModeSnapshot(gameModeSnapshot);

                                Logger.Log($"MapLoader: Updated map {newMap.Name} from {filePath} (SHA1 changed: {oldSHA1} -> {newMap.SHA1})");
                                MapChanged?.Invoke(this, new MapChangedEventArgs(newMap, MapChangeType.Updated, oldSHA1));
                            }
                            else
                            {
                                Logger.Log($"MapLoader: Map file {filePath} changed but SHA1 remained the same ({newMap.SHA1})");
                            }
                        }
                        else
                        {
                            // Map not found, treat as new
                            Logger.Log($"MapLoader: Changed event for unknown map {filePath}, treating as new");
                            AddMapToGameModes(newMap, gameModeSnapshot, true);
                            ReplaceGameModeSnapshot(gameModeSnapshot);
                            MapChanged?.Invoke(this, new MapChangedEventArgs(newMap, MapChangeType.Added));
                        }
                    }
                }
                else
                {
                    Logger.Log($"MapLoader: Failed to reload map info from {filePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"MapLoader: Error updating map from {filePath}: {ex.Message}");
            }
        }

        private async Task HandleMapFileDeleted(string filePath)
        {
            try
            {
                string baseFilePath = GetBaseFilePathFromFullPath(filePath);
                if (string.IsNullOrEmpty(baseFilePath))
                    return;

                RemoveTileLevelCacheEntry(baseFilePath);

                lock (mapModificationLock)
                {
                    List<GameMode> gameModeSnapshot = CloneGameModeSnapshot();
                    string mapSHA1 = FindMapSHA1ByFilePath(baseFilePath, gameModeSnapshot);

                    if (!string.IsNullOrEmpty(mapSHA1))
                    {
                        var removedMap = FindMapBySHA1(mapSHA1, gameModeSnapshot);
                        RemoveMapBySHA1(mapSHA1, gameModeSnapshot);
                        ReplaceGameModeSnapshot(gameModeSnapshot);

                        Logger.Log($"MapLoader: Removed map from {filePath}");
                        if (removedMap != null)
                            MapChanged?.Invoke(this, new MapChangedEventArgs(removedMap, MapChangeType.Removed));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"MapLoader: Error removing map from {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts a full file path to the base file path used by the map system.
        /// C:\YR\Maps\Custom\abc123.map > Maps\Custom\abc123
        /// </summary>
        private string GetBaseFilePathFromFullPath(string fullPath)
        {
            try
            {
                string gamePathNormalized = Path.GetFullPath(ProgramConstants.GamePath);
                string fullPathNormalized = Path.GetFullPath(fullPath);

                if (!fullPathNormalized.StartsWith(gamePathNormalized, StringComparison.OrdinalIgnoreCase))
                    return null;

                string relativePath = fullPathNormalized.Substring(gamePathNormalized.Length);
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString())
                    || relativePath.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    relativePath = relativePath.Substring(1);
                }

                string baseFilePath = relativePath.Substring(0, relativePath.Length - Path.GetExtension(relativePath).Length);

                return baseFilePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
            }
            catch (Exception ex)
            {
                Logger.Log($"MapLoader: Error converting file path {fullPath}: {ex.Message}");
                return null;
            }
        }

        private static bool IsMapAlreadyLoaded(string sha1, IEnumerable<GameMode> gameModes)
            => gameModes.SelectMany(gm => gm.Maps).Any(map => map.SHA1 == sha1);

        private static Map FindMapBySHA1(string sha1, IEnumerable<GameMode> gameModes)
            => gameModes.SelectMany(gm => gm.Maps).FirstOrDefault(map => map.SHA1 == sha1);

        private static string FindMapSHA1ByFilePath(string baseFilePath, IEnumerable<GameMode> gameModes)
            => gameModes.SelectMany(gm => gm.Maps).FirstOrDefault(map => !map.Official && map.BaseFilePath.Equals(baseFilePath, StringComparison.OrdinalIgnoreCase))?.SHA1;

        private static void RemoveMapBySHA1(string sha1, IEnumerable<GameMode> gameModes)
        {
            foreach (var gameMode in gameModes)
                gameMode.Maps.RemoveAll(map => map.SHA1 == sha1);
        }

        private void ReplaceGameModeSnapshot(List<GameMode> gameModes, bool removeEmptyGameModes = true) =>
            PublishSnapshot(gameModes, removeEmptyGameModes);

        private void PublishSnapshot(List<GameMode> gameModes, bool removeEmptyGameModes = true)
        {
            if (removeEmptyGameModes)
                gameModes.RemoveAll(g => g.Maps.Count < 1);

            _snapshot = new Snapshot(gameModes, new GameModeMapCollection(gameModes));
        }

        private List<GameMode> CloneGameModeSnapshot() => GameModes.Select(gameMode => gameMode.Clone()).ToList();

        private async Task LoadMultiMapsAsync(IniFile mpMapsIni, MapTileLevelCache tileLevelCache,
            IReadOnlyList<AutoMapOverlayDefinition> autoOverlayDefs)
        {
            List<string> keys = mpMapsIni.GetSectionKeys(MultiMapsSection);

            if (keys == null)
            {
                Logger.Log("Loading multiplayer map list failed!!!");
                return;
            }

            Task<Map>[] tasks = keys.Select(key => Task.Run(() =>
            {
                try
                {
                    string mapFilePathValue = mpMapsIni.GetStringValue(MultiMapsSection, key, string.Empty);
                    string mapFilePath = SafePath.CombineFilePath(mapFilePathValue);
                    FileInfo mapFile = SafePath.GetFile(ProgramConstants.GamePath, FormattableString.Invariant($"{mapFilePath}.{ClientConfiguration.Instance.MapFileExtension}"));

                    if (!mapFile.Exists)
                    {
                        Logger.Log("Map " + mapFile.FullName + " doesn't exist!");
                        return null;
                    }

                    var map = new Map(mapFilePathValue, false);
                    if (!map.InitializeFromMpMapsINI(mpMapsIni))
                        return null;

                    ApplyTileLevelDataToMap(map, tileLevelCache, autoOverlayDefs);

                    return map;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error loading map for key {key}: {ex}");
                    return null;
                }
            })).ToArray();

            Task waitMultiMapsTask = Task.WhenAll(tasks);
            while (await Task.WhenAny(waitMultiMapsTask, Task.Delay(1000)) != waitMultiMapsTask)
            {
                string message = "MapLoader: Waiting for the multiplayer map loading task to complete. Remaining files: " + tasks.Count(t => !t.IsCompleted) + ". Total: " + tasks.Length;
                Debug.WriteLine(message);
                Logger.Log(message);
            }

            await waitMultiMapsTask;

            foreach (Map map in tasks.Select(t => t.Result).Where(m => m != null))
            {
                AddMapToGameModes(map, _initialGameModes, false);
                _translatedMapNames[map.UntranslatedName] = map.Name;
            }
        }

        private void LoadGameModes(IniFile mpMapsIni)
        {
            var gameModes = mpMapsIni.GetSectionKeys(GameModesSection);
            if (gameModes != null)
            {
                foreach (string key in gameModes)
                {
                    string gameModeName = mpMapsIni.GetStringValue(GameModesSection, key, string.Empty);
                    if (!string.IsNullOrEmpty(gameModeName))
                    {
                        GameMode gm = new GameMode(gameModeName);
                        _initialGameModes.Add(gm);
                    }
                }
            }
        }

        private void LoadGameModeAliases(IniFile mpMapsIni)
        {
            var gmAliases = mpMapsIni.GetSectionKeys(GameModeAliasesSection);

            if (gmAliases != null)
            {
                foreach (string key in gmAliases)
                {
                    GameModeAliases.Add(key, mpMapsIni.GetStringValue(GameModeAliasesSection, key, string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }

        private async Task LoadCustomMapsAsync(MapTileLevelCache tileLevelCache,
            IReadOnlyList<AutoMapOverlayDefinition> autoOverlayDefs)
        {
            DirectoryInfo customMapsDirectory = SafePath.GetDirectory(ProgramConstants.GamePath, CUSTOM_MAPS_DIRECTORY);

            if (!customMapsDirectory.Exists)
            {
                Logger.Log($"Custom maps directory {customMapsDirectory} does not exist!");
                return;
            }

            Logger.Log("MapLoader: Loading custom maps...");

            // Load custom map cache from file system
            Stopwatch stopwatch = Stopwatch.StartNew();

            IEnumerable<FileInfo> mapFiles = customMapsDirectory.EnumerateFiles($"*.{ClientConfiguration.Instance.MapFileExtension}");

            // Note: using synchronous file I/O here saves a noticeable amount of latency compared to async.
            CustomMapCache customMapCache = LoadCustomMapCache();

            stopwatch.Stop();
            Logger.Log(FormattableString.Invariant($"MapLoader: Loaded custom map cache from file system in {stopwatch.ElapsedMilliseconds} ms"));

            // Process uncached custom maps.
            stopwatch.Restart();

            List<string> localMapPaths;
            {
                int mapFileExtensionWithDotLength = $".{ClientConfiguration.Instance.MapFileExtension}".Length;

                Task<string>[] tasks = mapFiles.Select(mapFile => Task.Run(() =>
                {
                    string baseFilePath = mapFile.FullName.Substring(ProgramConstants.GamePath.Length);
                    baseFilePath = baseFilePath.Substring(0, baseFilePath.Length - mapFileExtensionWithDotLength);

                    string normalizedPath = baseFilePath
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(Path.AltDirectorySeparatorChar, '/');

                    Map map;
                    if (customMapCache.Items.TryGetValue(normalizedPath, out var cachedItem) && !cachedItem.IsOutdated())
                    {
                        map = cachedItem.Map;
                    }
                    else
                    {
                        // Not in cache or outdated
                        map = new Map(normalizedPath, true);
                        if (!map.InitializeFromCustomMap())
                            return normalizedPath;
                        customMapCache.Items[normalizedPath] = new CustomMapCache.Item(map);
                    }

                    ApplyTileLevelDataToMap(map, tileLevelCache, autoOverlayDefs);

                    return normalizedPath;
                })).ToArray();

                Task waitCustomMapsTask = Task.WhenAll(tasks);
                while (await Task.WhenAny(waitCustomMapsTask, Task.Delay(1000)) != waitCustomMapsTask)
                {
                    string message = "MapLoader: Waiting for the custom map loading task to complete. Remaining files: " + tasks.Count(t => !t.IsCompleted) + ". Total: " + tasks.Length;
                    Debug.WriteLine(message);
                    Logger.Log(message);
                }

                await waitCustomMapsTask;

                localMapPaths = tasks.Select(t => t.Result).ToList();
            }

            stopwatch.Stop();
            Logger.Log(FormattableString.Invariant($"MapLoader: Processed uncached custom maps in {stopwatch.ElapsedMilliseconds} ms"));

            // Remove cached maps that no longer exist locally
            stopwatch.Restart();

            HashSet<string> missingMapPaths;
            {
                HashSet<string> cachedMapPaths = customMapCache.Items.Keys.ToHashSet();
                cachedMapPaths.ExceptWith(localMapPaths);
                missingMapPaths = cachedMapPaths;
            }

            foreach (string missingPath in missingMapPaths)
                customMapCache.Items.TryRemove(missingPath, out _);

            stopwatch.Stop();
            Logger.Log(FormattableString.Invariant($"MapLoader: Removed outdated maps from cache in {stopwatch.ElapsedMilliseconds} ms"));

            // Save custom map cache
            stopwatch.Restart();
            CacheCustomMaps(customMapCache);
            stopwatch.Stop();
            Logger.Log(FormattableString.Invariant($"MapLoader: Saved custom map cache to disk in {stopwatch.ElapsedMilliseconds} ms"));

            foreach (Map map in customMapCache.Items.Values.Select(item => item.Map))
            {
                AddMapToGameModes(map, _initialGameModes, false);
            }

            Logger.Log("MapLoader: Custom maps loaded.");
        }

        /// <summary>
        /// Save cache of custom maps.
        /// </summary>
        /// <param name="customMapCache">Custom maps to cache</param>
        private void CacheCustomMaps(CustomMapCache customMapCache)
        {
            var jsonData = JsonSerializer.Serialize(customMapCache, jsonSerializerOptions);

            File.WriteAllText(CUSTOM_MAPS_CACHE, jsonData);
        }

        /// <summary>
        /// Load previously cached custom maps
        /// </summary>
        /// <returns></returns>
        private CustomMapCache LoadCustomMapCache()
        {
            // Delete any legacy cache files
            foreach (string legacyCacheFile in LEGACY_CUSTOM_MAP_CACHE_FILES.Where(File.Exists))
            {
                try
                {
                    File.Delete(legacyCacheFile);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to delete legacy custom map cache file {legacyCacheFile}: {ex.Message}");
                }
            }

            // Load current cache
            try
            {
                var jsonData = File.ReadAllText(CUSTOM_MAPS_CACHE);

                var customMapCache = JsonSerializer.Deserialize<CustomMapCache>(jsonData, jsonSerializerOptions);

                if (customMapCache?.Version != CurrentCustomMapCacheVersion)
                    return new CustomMapCache() { Version = CurrentCustomMapCacheVersion, Items = [] };

                foreach (CustomMapCache.Item customMap in customMapCache.Items.Values)
                    customMap.Map.AfterDeserialize(recalculateSHA: false);

                // Remove outdated items
                foreach (var mapPath in customMapCache.Items.Keys.ToList())
                {
                    if (customMapCache.Items[mapPath].IsOutdated())
                    {
                        customMapCache.Items.TryRemove(mapPath, out _);
                    }
                }

                return customMapCache;
            }
            catch (Exception)
            {
                return new CustomMapCache() { Version = CurrentCustomMapCacheVersion, Items = [] };
            }
        }

        /// <summary>
        /// Attempts to load a custom map.
        /// </summary>
        /// <param name="mapPath">The path to the map file relative to the game directory.</param>
        /// <param name="resultMessage">When method returns, contains a message reporting whether or not loading the map failed and how.</param>
        /// <returns>The map if loading it was successful, otherwise false.</returns>
        public Map LoadCustomMap(string mapPath, out string resultMessage)
        {
            Debug.Assert(!mapPath.EndsWith($".{ClientConfiguration.Instance.MapFileExtension}", StringComparison.InvariantCultureIgnoreCase), $"Unexpected map path {mapPath}. It should not end with the map extension.");

            if (mapPath != mapPath.ToWin32FileName())
            {
                Logger.Log("LoadCustomMap: Map " + FormattableString.Invariant($"{mapPath}.{ClientConfiguration.Instance.MapFileExtension}") + " contains WIN32API reserved characters!");

                // Return "map file does not exist" message to hide technical details towards users
                resultMessage = string.Format("Map file {0} doesn't exist!".L10N("Client:MapLoader:MapFileDoesNotExist"), FormattableString.Invariant($"{mapPath}.{ClientConfiguration.Instance.MapFileExtension}"));

                return null;
            }

            string customMapFilePath = SafePath.CombineFilePath(ProgramConstants.GamePath, FormattableString.Invariant($"{mapPath}.{ClientConfiguration.Instance.MapFileExtension}"));
            FileInfo customMapFile = SafePath.GetFile(customMapFilePath);

            if (!customMapFile.Exists)
            {
                Logger.Log("LoadCustomMap: Map " + customMapFile.FullName + " not found!");
                resultMessage = string.Format("Map file {0} doesn't exist!".L10N("Client:MapLoader:MapFileDoesNotExist"), customMapFile.Name);

                return null;
            }

            Logger.Log("LoadCustomMap: Loading custom map " + customMapFile.FullName);

            var map = new Map(mapPath, true);

            if (map.InitializeFromCustomMap())
            {
                EnsureTileLevelSupportLoaded();
                ApplyTileLevelDataToMap(map, _mapTileLevelCache, _autoMapOverlayDefs);
                SaveTileLevelCache();

                lock (mapModificationLock)
                {
                    List<GameMode> gameModeSnapshot = CloneGameModeSnapshot();

                    if (IsMapAlreadyLoaded(map.SHA1, gameModeSnapshot))
                    {
                        Logger.Log("LoadCustomMap: Custom map " + customMapFile.FullName + " is already loaded!");
                        resultMessage = string.Format("Map {0} is already loaded.".L10N("Client:MapLoader:MapAlreadyLoaded"), map.Name);

                        return null;
                    }

                    AddMapToGameModes(map, gameModeSnapshot, true);
                    ReplaceGameModeSnapshot(gameModeSnapshot);
                }

                Logger.Log("LoadCustomMap: Map " + customMapFile.FullName + " added successfully.");

                resultMessage = string.Format("Map {0} loaded successfully.".L10N("Client:MapLoader:MapLoadedSuccessfully"), map.Name);

                return map;
            }

            Logger.Log("LoadCustomMap: Loading map " + customMapFile.FullName + " failed!");
            resultMessage = string.Format("Loading map {0} failed!".L10N("Client:MapLoader:MapLoadingFailed"), Path.GetFileNameWithoutExtension(customMapFile.Name));

            return null;
        }

        public void DeleteCustomMap(GameModeMap gameModeMap)
        {
            Logger.Log("Deleting map " + gameModeMap.Map.UntranslatedName);
            File.Delete(gameModeMap.Map.CompleteFilePath);

            lock (mapModificationLock)
            {
                List<GameMode> gameModeSnapshot = CloneGameModeSnapshot();
                RemoveMapBySHA1(gameModeMap.Map.SHA1, gameModeSnapshot);
                ReplaceGameModeSnapshot(gameModeSnapshot);
            }
        }

        /// <summary>
        /// Adds map to all eligible game modes.
        /// </summary>
        /// <param name="map">Map to add.</param>
        /// <param name="gameModes">Game modes collection.</param>
        /// <param name="enableLogging">If set to true, a message for each game mode the map is added to is output to the log file.</param>
        private void AddMapToGameModes(Map map, List<GameMode> gameModes, bool enableLogging)
        {
            foreach (string gameMode in map.GameModes)
            {
                if (!GameModeAliases.TryGetValue(gameMode, out string[] gameModeAliases))
                    gameModeAliases = new string[] { gameMode };

                foreach (string gameModeAlias in gameModeAliases)
                {
                    if (!map.Official && !(AllowedGameModes.Contains(gameMode) || AllowedGameModes.Contains(gameModeAlias)))
                        continue;

                    GameMode gm = gameModes.FirstOrDefault(g => g.Name == gameModeAlias);
                    if (gm == null)
                    {
                        gm = new GameMode(gameModeAlias);
                        gameModes.Add(gm);
                    }

                    gm.Maps.Add(map);
                    if (enableLogging)
                        Logger.Log("AddMapToGameModes: Added map " + map.UntranslatedName + " to game mode " + gm.Name);
                }
            }
        }

        /// <summary>
        /// Removes any name-based favorite entries that have been successfully migrated to SHA1.
        /// This runs after all maps have been processed to ensure complete migration.
        /// </summary>
        private void CleanupMigratedFavorites()
        {
            var favoriteMaps = UserINISettings.Instance.FavoriteMaps;
            if (favoriteMaps == null || !favoriteMaps.Any())
                return;

            var entriesToRemove = new List<string>();

            foreach (string favoriteKey in favoriteMaps)
            {
                string[] parts = favoriteKey.Split(':');
                if (parts.Length != 2)
                    continue;

                string mapName = parts[0];
                string gameModeName = parts[1];

                // Check if there's a corresponding SHA1-based entry for any map with this name
                var gameMode = GameModes.FirstOrDefault(gm => gm.Name == gameModeName);
                if (gameMode != null)
                {
                    bool hasMigratedVersion = gameMode.Maps
                        .Where(m => m.UntranslatedName == mapName)
                        .Any(m => favoriteMaps.Contains($"{m.SHA1}:{gameModeName}"));

                    if (hasMigratedVersion)
                        entriesToRemove.Add(favoriteKey);
                }
            }

            // Remove the name-based entries
            if (entriesToRemove.Any())
            {
                foreach (string entry in entriesToRemove)
                    favoriteMaps.Remove(entry);

                UserINISettings.Instance.WriteFavoriteMaps();
            }
        }

        public void PrefetchCachedPreviewImageFromMap(Map map)
        {
            if (map?.IsNonImmediatePreviewImageAvailable() ?? false)
            {
                _ = mapPreviewCacheManager.Request(map, out CacheLease<Image>? lease, addToQueue: true);
                lease?.Dispose();
            }
        }

        public Texture2D GetPreviewTextureFromMap(Map map, bool syncLoadOnCacheMiss = false)
        {
            if (map?.IsImmediatePreviewImageAvailable() ?? false)
                return AssetLoader.LoadTextureUncached(map.PreviewPath);

            using var cacheLease = GetCachedPreviewImageFromMap(map, syncLoadOnCacheMiss);

            if (cacheLease != null)
                return AssetLoader.TextureFromImage(cacheLease.Value);
            else
                return null;
        }

        public CacheLease<Image> GetCachedPreviewImageFromMap(Map map, bool syncLoadOnCacheMiss = false)
        {
            if (map?.IsImmediatePreviewImageAvailable() ?? false)
            {
                Image image = map.GetImmediatePreviewImage();
                return CacheLease<Image>.CreateOwned(image, image.Dispose);
            }
            else if (map?.IsNonImmediatePreviewImageAvailable() ?? false)
            {
                if (mapPreviewCacheManager.Request(map, out CacheLease<Image> lease, syncComputeOnCacheMiss: syncLoadOnCacheMiss, addToQueue: true))
                    return lease;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        public Map FindMapByHash(string mapHash) => GameModeMaps?.FindMapByHash(mapHash);

        public void Dispose() => mapPreviewCacheManager?.Dispose();

        private void EnsureTileLevelSupportLoaded()
        {
            if (_tileLevelSupportLoaded)
                return;

            lock (tileLevelCacheLock)
            {
                if (_tileLevelSupportLoaded)
                    return;

                _autoMapOverlayDefs = LoadAutoMapOverlayDefinitions();

                _mapTileLevelCache = LoadMapTileLevelCache(_autoMapOverlayDefs);
                _tileLevelSupportLoaded = true;
            }
        }

        private void SaveTileLevelCache()
        {
            if (!_tileLevelSupportLoaded || _mapTileLevelCache == null)
                return;

            lock (tileLevelCacheLock)
            {
                SaveMapTileLevelCache(_mapTileLevelCache, _autoMapOverlayDefs);
            }
        }

        private void RemoveTileLevelCacheEntry(string baseFilePath)
        {
            if (!_tileLevelSupportLoaded || _mapTileLevelCache == null)
                return;

            string key = NormalizeMapBaseFilePath(baseFilePath);
            if (_mapTileLevelCache.Items.TryRemove(key, out _))
                SaveTileLevelCache();
        }

        /// <summary>
        /// Loads auto map overlay definitions from the [AutoMapOverlays] section
        /// of ClientDefinitions.ini.
        /// Format per entry: BuildingID,ImageFile,OwnerFilter,CellOffsetX,CellOffsetY[,Toggleable]
        /// </summary>
        private static IReadOnlyList<AutoMapOverlayDefinition> LoadAutoMapOverlayDefinitions()
        {
            IniSection section = ClientConfiguration.Instance.GetAutoMapOverlaysSection();
            if (section == null)
                return Array.Empty<AutoMapOverlayDefinition>();

            var defs = new List<AutoMapOverlayDefinition>();
            foreach (var kvp in section.Keys)
            {
                string[] parts = kvp.Value.Split(',');
                if (parts.Length < 5)
                {
                    Logger.Log($"MapLoader: Invalid AutoMapOverlay entry '{kvp.Value}' — expected at least 5 comma-separated fields.");
                    continue;
                }

                string buildingId = parts[0].Trim();
                string textureName = parts[1].Trim();
                string ownerFilter = parts[2].Trim();

                if (!int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int offsetX) ||
                    !int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int offsetY))
                {
                    Logger.Log($"MapLoader: Invalid cell offsets in AutoMapOverlay entry '{kvp.Value}'.");
                    continue;
                }

                bool toggleable = parts.Length > 5 && Conversions.BooleanFromString(parts[5].Trim(), false);
                defs.Add(new AutoMapOverlayDefinition(buildingId, textureName, ownerFilter, offsetX, offsetY, toggleable));
            }
            return defs;
        }

        /// <summary>
        /// Computes a short hash of the auto overlay config so the tile level cache can be
        /// invalidated when the building detection configuration changes.
        /// </summary>
        private static string ComputeAutoOverlayConfigHash(IReadOnlyList<AutoMapOverlayDefinition> defs)
        {
            if (defs.Count == 0)
                return "empty";

            string repr = string.Join("|", defs.Select(d =>
                $"{d.BuildingId},{d.TextureName},{d.OwnerFilter},{d.CellOffsetX},{d.CellOffsetY},{d.Toggleable}"));

            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(repr));
            return BitConverter.ToString(hash).Replace("-", string.Empty)[..16];
        }

        private MapTileLevelCache LoadMapTileLevelCache(IReadOnlyList<AutoMapOverlayDefinition> defs)
        {
            var emptyCache = new MapTileLevelCache
            {
                Version = CurrentMapTileLevelCacheVersion,
                ConfigHash = ComputeAutoOverlayConfigHash(defs),
                Items = []
            };

            try
            {
                if (!File.Exists(MAP_TILE_LEVEL_CACHE))
                    return emptyCache;

                string json = File.ReadAllText(MAP_TILE_LEVEL_CACHE);
                var cache = JsonSerializer.Deserialize<MapTileLevelCache>(json, jsonSerializerOptions);

                if (cache == null ||
                    cache.Version != CurrentMapTileLevelCacheVersion ||
                    cache.ConfigHash != emptyCache.ConfigHash)
                {
                    return emptyCache;
                }

                return cache;
            }
            catch (Exception ex)
            {
                Logger.Log($"MapLoader: Failed to load tile level cache: {ex.Message}");
                return emptyCache;
            }
        }

        private static string NormalizeMapBaseFilePath(string baseFilePath)
            => baseFilePath
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

        private void SaveMapTileLevelCache(MapTileLevelCache cache, IReadOnlyList<AutoMapOverlayDefinition> defs)
        {
            try
            {
                // Ensure config hash is current before saving
                cache.ConfigHash = ComputeAutoOverlayConfigHash(defs);
                string json = JsonSerializer.Serialize(cache, jsonSerializerOptions);
                File.WriteAllText(MAP_TILE_LEVEL_CACHE, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"MapLoader: Failed to save tile level cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks the tile level cache for this map. If absent or stale, decodes IsoMapPack5
        /// from the map file and runs building detection. Applies the result to the map object.
        /// </summary>
        private void ApplyTileLevelDataToMap(Map map, MapTileLevelCache tileLevelCache,
            IReadOnlyList<AutoMapOverlayDefinition> autoOverlayDefs)
        {
            if (!MainClientConstants.USE_ISOMETRIC_CELLS)
                return;

            string key = NormalizeMapBaseFilePath(map.BaseFilePath);

            if (tileLevelCache.Items.TryGetValue(key, out var cached) && !cached.IsOutdated(map.CompleteFilePath))
            {
                map.ApplyTileLevelData(cached.WaypointLevels, cached.BuildingOverlays);
                return;
            }

            var item = ComputeTileLevelItem(map, autoOverlayDefs);
            if (item != null)
            {
                tileLevelCache.Items[key] = item;
                map.ApplyTileLevelData(item.WaypointLevels, item.BuildingOverlays);
            }
        }

        /// <summary>
        /// Reads a map file in one pass and returns the raw base64 IsoMapPack5 string and the
        /// raw value lines from [Structures] (everything after the '=' on each line).
        /// </summary>
        private static (string isoBase64, List<string> structureValues) ReadMapSectionsFromFile(string filePath)
        {
            var isoSb = new StringBuilder(32 * 1024);
            var structureValues = new List<string>(128);

            bool inIso = false, inStructures = false;

            using var reader = new StreamReader(filePath,
                System.Text.Encoding.GetEncoding(1252),
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 65536);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == ';')
                    continue;

                if (line[0] == '[')
                {
                    inIso        = line.Equals("[IsoMapPack5]",  StringComparison.OrdinalIgnoreCase);
                    inStructures = line.Equals("[Structures]",   StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (inIso)
                {
                    int eq = line.IndexOf('=');
                    if (eq >= 0) isoSb.Append(line, eq + 1, line.Length - eq - 1);
                }
                else if (inStructures)
                {
                    int eq = line.IndexOf('=');
                    if (eq >= 0) structureValues.Add(line.Substring(eq + 1));
                }
            }

            return (isoSb.ToString(), structureValues);
        }

        /// <summary>
        /// Decodes IsoMapPack5 from the map file and computes tile level data:
        /// waypoint heights and auto-detected building overlays.
        /// Returns null on failure.
        /// </summary>
        private MapTileLevelCache.Item ComputeTileLevelItem(Map map,
            IReadOnlyList<AutoMapOverlayDefinition> defs)
        {
            try
            {
                var (isoBase64, structureValues) = ReadMapSectionsFromFile(map.CompleteFilePath);

                var tileLevels = IsoMapPackDecoder.Decode(isoBase64);

                // Waypoint levels
                int[] waypointLevels = null;
                if (map.waypoints.Count > 0)
                {
                    waypointLevels = new int[map.waypoints.Count];
                    for (int i = 0; i < map.waypoints.Count; i++)
                    {
                        var (wx, wy) = Map.ParseWaypointCellCoords(map.waypoints[i].Split(',')[0]);
                        waypointLevels[i] = tileLevels.TryGetValue(IsoMapPackDecoder.TileKey(wx, wy), out byte lv) ? lv : 0;
                    }
                }

                // Building overlays
                var buildingOverlays = DetectBuildingOverlays(structureValues, tileLevels, defs);

                var fi = new FileInfo(map.CompleteFilePath);
                return new MapTileLevelCache.Item(fi.Length, fi.LastWriteTimeUtc, waypointLevels, buildingOverlays);
            }
            catch (Exception ex)
            {
                Logger.Log($"MapLoader: Failed to decode tile levels for {map.BaseFilePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Scans the raw [Structures] value lines for buildings matching the auto-overlay definitions
        /// and returns ExtraMapPreviewTexture entries with the tile height level applied.
        /// </summary>
        private static List<ExtraMapPreviewTexture> DetectBuildingOverlays(
            List<string> structureValues,
            Dictionary<long, byte> tileLevels,
            IReadOnlyList<AutoMapOverlayDefinition> defs)
        {
            var result = new List<ExtraMapPreviewTexture>();
            if (structureValues.Count == 0 || defs.Count == 0)
                return result;

            // Split defs into exact-match dictionary and prefix-wildcard list.
            // A BuildingId ending with '*' matches any ID starting with that prefix.
            var exactDefs = new Dictionary<string, AutoMapOverlayDefinition>(StringComparer.OrdinalIgnoreCase);
            var wildcardDefs = new List<(string Prefix, AutoMapOverlayDefinition Def)>();
            foreach (var def in defs)
            {
                if (def.BuildingId.EndsWith("*"))
                    wildcardDefs.Add((def.BuildingId.TrimEnd('*'), def));
                else if (!exactDefs.ContainsKey(def.BuildingId))
                    exactDefs[def.BuildingId] = def;
            }

            foreach (string value in structureValues)
            {
                // Format: Owner,BuildingTypeID,Health,X,Y,...
                string[] parts = value.Split(',');
                if (parts.Length < 5)
                    continue;

                string owner      = parts[0].Trim();
                string buildingId = parts[1].Trim();

                AutoMapOverlayDefinition def = null;
                if (!exactDefs.TryGetValue(buildingId, out def))
                {
                    foreach (var (prefix, wildcardDef) in wildcardDefs)
                    {
                        if (buildingId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            def = wildcardDef;
                            break;
                        }
                    }
                }

                if (def == null)
                    continue;

                if (!string.IsNullOrEmpty(def.OwnerFilter) &&
                    !string.Equals(owner, def.OwnerFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ||
                    !int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                    continue;

                int cellX = x + def.CellOffsetX;
                int cellY = y + def.CellOffsetY;
                byte level = tileLevels.TryGetValue(IsoMapPackDecoder.TileKey(cellX, cellY), out byte lv) ? lv : (byte)0;

                result.Add(new ExtraMapPreviewTexture(def.TextureName, new Point(cellX, cellY), level, def.Toggleable));
            }

            return result;
        }
    }
}
