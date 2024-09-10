using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    public class MapLoader
    {
        public const string MAP_FILE_EXTENSION = ".map";
        private const string CUSTOM_MAPS_DIRECTORY = "Maps/Custom";
        private static readonly string CUSTOM_MAPS_CACHE = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "custom_map_cache");
        private const string MultiMapsSection = "MultiMaps";
        private const string GameModesSection = "GameModes";
        private const string GameModeAliasesSection = "GameModeAliases";
        private const int CurrentCustomMapCacheVersion = 1;
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };

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
        /// Loads multiplayer map info asynchonously.
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

            MapLoadingComplete?.Invoke(this, EventArgs.Empty);
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
                FileInfo mapFile = SafePath.GetFile(ProgramConstants.GamePath, FormattableString.Invariant($"{mapFilePath}{MAP_FILE_EXTENSION}"));

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

            IEnumerable<FileInfo> mapFiles = customMapsDirectory.EnumerateFiles($"*{MAP_FILE_EXTENSION}");
            ConcurrentDictionary<string, Map> customMapCache = LoadCustomMapCache();
            var localMapSHAs = new List<string>();

            var tasks = new List<Task>();

            foreach (FileInfo mapFile in mapFiles)
            {
                // this must be Task.Factory.StartNew for XNA/.Net 4.0 compatibility
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    string baseFilePath = mapFile.FullName.Substring(ProgramConstants.GamePath.Length);
                    baseFilePath = baseFilePath.Substring(0, baseFilePath.Length - 4);

                    var map = new Map(baseFilePath
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(Path.AltDirectorySeparatorChar, '/'), true);
                    map.CalculateSHA();
                    localMapSHAs.Add(map.SHA1);
                    if (!customMapCache.ContainsKey(map.SHA1) && map.SetInfoFromCustomMap())
                        customMapCache.TryAdd(map.SHA1, map);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // remove cached maps that no longer exist locally
            foreach (var missingSHA in customMapCache.Keys.Where(cachedSHA => !localMapSHAs.Contains(cachedSHA)))
            {
                customMapCache.TryRemove(missingSHA, out _);
            }

            // save cache
            CacheCustomMaps(customMapCache);

            foreach (Map map in customMapCache.Values)
            {
                AddMapToGameModes(map, false);
            }
        }

        /// <summary>
        /// Save cache of custom maps.
        /// </summary>
        /// <param name="customMaps">Custom maps to cache</param>
        private void CacheCustomMaps(ConcurrentDictionary<string, Map> customMaps)
        {
            var customMapCache = new CustomMapCache
            {
                Maps = customMaps,
                Version = CurrentCustomMapCacheVersion
            };
            var jsonData = JsonSerializer.Serialize(customMapCache, jsonSerializerOptions);

            File.WriteAllText(CUSTOM_MAPS_CACHE, jsonData);
        }

        /// <summary>
        /// Load previously cached custom maps
        /// </summary>
        /// <returns></returns>
        private ConcurrentDictionary<string, Map> LoadCustomMapCache()
        {
            try
            {
                var jsonData = File.ReadAllText(CUSTOM_MAPS_CACHE);

                var customMapCache = JsonSerializer.Deserialize<CustomMapCache>(jsonData, jsonSerializerOptions);

                var customMaps = customMapCache?.Version == CurrentCustomMapCacheVersion && customMapCache.Maps != null
                    ? customMapCache.Maps : new ConcurrentDictionary<string, Map>();

                foreach (var customMap in customMaps.Values)
                    customMap.AfterDeserialize();

                return customMaps;
            }
            catch (Exception)
            {
                return new ConcurrentDictionary<string, Map>();
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
            string customMapFilePath = SafePath.CombineFilePath(ProgramConstants.GamePath, FormattableString.Invariant($"{mapPath}{MAP_FILE_EXTENSION}"));
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
    }
}
