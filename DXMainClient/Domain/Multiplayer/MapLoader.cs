using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using ClientCore;
using ClientCore.Extensions;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    public enum MapChangeType
    {
        Added,
        Updated,
        Removed
    }

    public class MapLoader
    {
        private const string CUSTOM_MAPS_DIRECTORY = "Maps/Custom";

        private const int CurrentCustomMapCacheVersion = 4;
        private static readonly string CUSTOM_MAPS_CACHE = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "custom_map_cache_v4");
        private static readonly IReadOnlyList<string> LEGACY_CUSTOM_MAP_CACHE_FILES = [
            SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "custom_map_cache"),
            SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "custom_map_cache_v2"),
            SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "custom_map_cache_v3"),
        ];

        private const string MultiMapsSection = "MultiMaps";
        private const string GameModesSection = "GameModes";
        private const string GameModeAliasesSection = "GameModeAliases";
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };
        private MapFileWatcher mapFileWatcher;
        private readonly object mapModificationLock = new object();
        private const int _mapChangeRetryCount = 3;

        /// <summary>
        /// List of game modes.
        /// </summary>
        public List<GameMode> GameModes = new List<GameMode>();

        public GameModeMapCollection GameModeMaps;

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

        /// <summary>
        /// Sets up file watching for maps.
        /// </summary>
        public void Initialize()
        {
            if (mapFileWatcher != null)
                return;

            string customMapsPath = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, CUSTOM_MAPS_DIRECTORY);

            mapFileWatcher = new MapFileWatcher(customMapsPath, ClientConfiguration.Instance.MapFileExtension);
            mapFileWatcher.MapFileChanged += OnMapFileChanged;
            mapFileWatcher.StartWatching();
        }

        /// <summary>
        /// Loads multiplayer map info asynchronously.
        /// </summary>
        public Task LoadMapsAsync() => Task.Run(LoadMaps);

        /// <summary>
        /// Load maps based on INI info as well as those in the custom maps directory.
        /// </summary>
        public void LoadMaps()
        {
            string mpMapsPath = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MPMapsIniPath);

            Logger.Log($"Loading maps from {mpMapsPath}.");

            IniFile mpMapsIni = new IniFile(mpMapsPath);

            LoadGameModes(mpMapsIni);
            LoadGameModeAliases(mpMapsIni);
            LoadMultiMaps(mpMapsIni);
            LoadCustomMaps();

            GameModes.RemoveAll(g => g.Maps.Count < 1);
            GameModeMaps = new GameModeMapCollection(GameModes);

            // Clean up any name-based favorite entries after migration (legacy: changed from name to sha1)
            CleanupMigratedFavorites();

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
                        if (map.SetInfoFromCustomMap())
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
                    lock (mapModificationLock)
                    {
                        if (IsMapAlreadyLoaded(map.SHA1))
                            return;

                        AddMapToGameModes(map, true);
                        UpdateGameModeMaps();

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
                        if (newMap.SetInfoFromCustomMap())
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
                    lock (mapModificationLock)
                    {
                        string oldSHA1 = FindMapSHA1ByFilePath(baseFilePath);

                        if (!string.IsNullOrEmpty(oldSHA1))
                        {
                            if (oldSHA1 != newMap.SHA1)
                            {
                                // SHA1 changed, remove old and add new
                                RemoveMapBySHA1(oldSHA1);
                                AddMapToGameModes(newMap, true);
                                UpdateGameModeMaps();

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
                            AddMapToGameModes(newMap, true);
                            UpdateGameModeMaps();
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

                lock (mapModificationLock)
                {
                    string mapSHA1 = FindMapSHA1ByFilePath(baseFilePath);

                    if (!string.IsNullOrEmpty(mapSHA1))
                    {
                        var removedMap = FindMapBySHA1(mapSHA1);
                        RemoveMapBySHA1(mapSHA1);
                        UpdateGameModeMaps();

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

        private bool IsMapAlreadyLoaded(string sha1)
            => GameModes.SelectMany(gm => gm.Maps).Any(map => map.SHA1 == sha1);

        private Map FindMapBySHA1(string sha1)
            => GameModes.SelectMany(gm => gm.Maps).FirstOrDefault(map => map.SHA1 == sha1);

        private string FindMapSHA1ByFilePath(string baseFilePath)
            => GameModes.SelectMany(gm => gm.Maps)
                .Where(map => !map.Official && map.BaseFilePath.Equals(baseFilePath, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault()?.SHA1;

        private void RemoveMapBySHA1(string sha1)
        {
            foreach (var gameMode in GameModes)
                gameMode.Maps.RemoveAll(map => map.SHA1 == sha1);
        }

        private void UpdateGameModeMaps()
        {
            GameModes.RemoveAll(g => g.Maps.Count < 1);
            GameModeMaps = new GameModeMapCollection(GameModes);
        }

        private void LoadMultiMaps(IniFile mpMapsIni)
        {
            List<string> keys = mpMapsIni.GetSectionKeys(MultiMapsSection);

            if (keys == null)
            {
                Logger.Log("Loading multiplayer map list failed!!!");
                return;
            }

            List<Map> maps = new List<Map>();

            foreach (string key in keys)
            {
                string mapFilePathValue = mpMapsIni.GetStringValue(MultiMapsSection, key, string.Empty);
                string mapFilePath = SafePath.CombineFilePath(mapFilePathValue);
                FileInfo mapFile = SafePath.GetFile(ProgramConstants.GamePath, FormattableString.Invariant($"{mapFilePath}.{ClientConfiguration.Instance.MapFileExtension}"));

                if (!mapFile.Exists)
                {
                    Logger.Log("Map " + mapFile.FullName + " doesn't exist!");
                    continue;
                }

                var map = new Map(mapFilePathValue, false);

                if (!map.SetInfoFromMpMapsINI(mpMapsIni))
                    continue;

                maps.Add(map);
            }

            foreach (Map map in maps)
            {
                AddMapToGameModes(map, false);
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
                        GameModes.Add(gm);
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

        private void LoadCustomMaps()
        {
            DirectoryInfo customMapsDirectory = SafePath.GetDirectory(ProgramConstants.GamePath, CUSTOM_MAPS_DIRECTORY);

            if (!customMapsDirectory.Exists)
            {
                Logger.Log($"Custom maps directory {customMapsDirectory} does not exist!");
                return;
            }

            IEnumerable<FileInfo> mapFiles = customMapsDirectory.EnumerateFiles($"*.{ClientConfiguration.Instance.MapFileExtension}");
            CustomMapCache customMapCache = LoadCustomMapCache();
            var localMapPaths = new ConcurrentBag<string>();

            Task[] tasks = mapFiles.Select(mapFile => Task.Run(() =>
            {
                string baseFilePath = mapFile.FullName.Substring(ProgramConstants.GamePath.Length);
                baseFilePath = baseFilePath.Substring(0, baseFilePath.Length - 4);

                string normalizedPath = baseFilePath
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .Replace(Path.AltDirectorySeparatorChar, '/');

                localMapPaths.Add(normalizedPath);

                if (customMapCache.Items.TryGetValue(normalizedPath, out var cachedItem) && !cachedItem.IsOutdated())
                {
                    // Use cached map
                    return;
                }

                // Not in cache or outdated
                var map = new Map(normalizedPath, true);
                if (map.SetInfoFromCustomMap())
                    customMapCache.Items[normalizedPath] = new CustomMapCache.Item(map);
            })).ToArray();

            while (!Task.WaitAll(tasks, millisecondsTimeout: 1000))
            {
                string message = "Waiting for the custom map loading task to complete. Remaining files: " + tasks.Count(t => !t.IsCompleted) + ". Total: " + tasks.Length;
                Debug.WriteLine(message);
                Logger.Log(message);
            }

            // remove cached maps that no longer exist locally
            foreach (var missingPath in customMapCache.Items.Keys.Where(cachedPath => !localMapPaths.Contains(cachedPath)))
            {
                customMapCache.Items.TryRemove(missingPath, out _);
            }

            // save cache
            CacheCustomMaps(customMapCache);

            foreach (Map map in customMapCache.Items.Values.Select(item => item.Map))
            {
                AddMapToGameModes(map, false);
            }
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
            Debug.Assert(!mapPath.EndsWith($".{ClientConfiguration.Instance.MapFileExtension}", StringComparison.InvariantCultureIgnoreCase), $"Unexpected map path {mapPath}. It should end with the map extension.");

            if (mapPath != mapPath.ToWin32FileName())
            {
                Logger.Log("LoadCustomMap: Map " + FormattableString.Invariant($"{mapPath}.{ClientConfiguration.Instance.MapFileExtension}") + " contains WIN32API reserved characters!");
                resultMessage = $"Map file {FormattableString.Invariant($"{mapPath}.{ClientConfiguration.Instance.MapFileExtension}")} doesn't exist!";
                return null;
            }

            string customMapFilePath = SafePath.CombineFilePath(ProgramConstants.GamePath, FormattableString.Invariant($"{mapPath}.{ClientConfiguration.Instance.MapFileExtension}"));
            FileInfo customMapFile = SafePath.GetFile(customMapFilePath);

            if (!customMapFile.Exists)
            {
                Logger.Log("LoadCustomMap: Map " + customMapFile.FullName + " not found!");
                resultMessage = $"Map file {customMapFile.Name} doesn't exist!";

                return null;
            }

            Logger.Log("LoadCustomMap: Loading custom map " + customMapFile.FullName);

            var map = new Map(mapPath, true);

            if (map.SetInfoFromCustomMap())
            {
                foreach (GameMode gm in GameModes)
                {
                    if (gm.Maps.Find(m => m.SHA1 == map.SHA1) != null)
                    {
                        Logger.Log("LoadCustomMap: Custom map " + customMapFile.FullName + " is already loaded!");
                        resultMessage = $"Map {map.Name} is already loaded.";

                        return null;
                    }
                }

                Logger.Log("LoadCustomMap: Map " + customMapFile.FullName + " added successfully.");

                AddMapToGameModes(map, true);
                var gameModes = GameModes.Where(gm => gm.Maps.Contains(map));
                GameModeMaps.AddRange(gameModes.Select(gm => new GameModeMap(gm, map, false)));

                resultMessage = $"Map {map.Name} loaded successfully.";

                return map;
            }

            Logger.Log("LoadCustomMap: Loading map " + customMapFile.FullName + " failed!");
            resultMessage = $"Loading map {Path.GetFileNameWithoutExtension(customMapFile.Name)} failed!";

            return null;
        }

        public void DeleteCustomMap(GameModeMap gameModeMap)
        {
            Logger.Log("Deleting map " + gameModeMap.Map.UntranslatedName);
            File.Delete(gameModeMap.Map.CompleteFilePath);
            foreach (GameMode gameMode in GameModeMaps.GameModes)
            {
                gameMode.Maps.Remove(gameModeMap.Map);
            }

            GameModeMaps.Remove(gameModeMap);
        }

        /// <summary>
        /// Adds map to all eligible game modes.
        /// </summary>
        /// <param name="map">Map to add.</param>
        /// <param name="enableLogging">If set to true, a message for each game mode the map is added to is output to the log file.</param>
        private void AddMapToGameModes(Map map, bool enableLogging)
        {
            foreach (string gameMode in map.GameModes)
            {
                if (!GameModeAliases.TryGetValue(gameMode, out string[] gameModeAliases))
                    gameModeAliases = new string[] { gameMode };

                foreach (string gameModeAlias in gameModeAliases)
                {
                    if (!map.Official && !(AllowedGameModes.Contains(gameMode) || AllowedGameModes.Contains(gameModeAlias)))
                        continue;

                    GameMode gm = GameModes.Find(g => g.Name == gameModeAlias);
                    if (gm == null)
                    {
                        gm = new GameMode(gameModeAlias);
                        GameModes.Add(gm);
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
    }
}
