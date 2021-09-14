using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using ClientCore;
using Newtonsoft.Json;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    public class MapLoader
    {
        public const string MAP_FILE_EXTENSION = ".map";
        private const string CUSTOM_MAPS_DIRECTORY = "Maps/Custom";
        private static readonly string CUSTOM_MAPS_CACHE = ProgramConstants.ClientUserFilesPath + "custom_map_cache";
        private const string MultiMapsSection = "MultiMaps";
        private const string GameModesSection = "GameModes";
        private const string GameModeAliasesSection = "GameModeAliases";
        private const int CurrentCustomMapCacheVersion = 1;

        /// <summary>
        /// List of game modes.
        /// </summary>
        public List<GameMode> GameModes = new List<GameMode>();

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

        /// <summary>
        /// List of gamemodes allowed to be used on custom maps in order for them to display in map list.
        /// </summary>
        private string[] AllowedGameModes = ClientConfiguration.Instance.AllowedCustomGameModes.Split(',');

        /// <summary>
        /// Loads multiplayer map info asynchonously.
        /// </summary>
        public void LoadMapsAsync()
        {
            Thread thread = new Thread(LoadMaps);
            thread.Start();
        }

        /// <summary>
        /// Load maps based on INI info as well as those in the custom maps directory.
        /// </summary>
        public void LoadMaps()
        {
            Logger.Log("Loading maps.");

            IniFile mpMapsIni = new IniFile(ProgramConstants.GamePath + ClientConfiguration.Instance.MPMapsIniPath);

            LoadGameModes(mpMapsIni);
            LoadGameModeAliases(mpMapsIni);
            LoadMultiMaps(mpMapsIni);
            LoadCustomMaps();

            GameModes.RemoveAll(g => g.Maps.Count < 1);

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
                string mapFilePath = mpMapsIni.GetStringValue(MultiMapsSection, key, string.Empty);

                if (!File.Exists(ProgramConstants.GamePath + mapFilePath + MAP_FILE_EXTENSION))
                {
                    Logger.Log("Map " + mapFilePath + " doesn't exist!");
                    continue;
                }

                Map map = new Map(mapFilePath);

                if (!map.SetInfoFromMpMapsINI(mpMapsIni))
                    continue;

                maps.Add(map);
            }

            foreach (Map map in maps)
            {
                AddMapToGameModes(map, false);
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
                        new char[] {','}, StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }

        private void LoadCustomMaps()
        {
            if (!Directory.Exists(ProgramConstants.GamePath + CUSTOM_MAPS_DIRECTORY))
            {
                Logger.Log("Custom maps directory does not exist!");
                return;
            }
            
            string[] mapFiles = Directory.GetFiles(ProgramConstants.GamePath + CUSTOM_MAPS_DIRECTORY, "*.map");
            ConcurrentDictionary<string, Map> customMapCache = LoadCustomMapCache();
            var localMapSHAs = new List<string>();

            var tasks = new List<Task>();

            foreach (string mapFile in mapFiles)
            {
                // this must be Task.Factory.StartNew for XNA/.Net 4.0 compatibility
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    string baseFilePath = mapFile.Substring(ProgramConstants.GamePath.Length);
                    baseFilePath = baseFilePath.Substring(0, baseFilePath.Length - 4);

                    Map map = new Map(baseFilePath, mapFile);
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
            var jsonData = JsonConvert.SerializeObject(customMapCache);
            
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

                var customMapCache = JsonConvert.DeserializeObject<CustomMapCache>(jsonData);

                var customMaps = customMapCache?.Version == CurrentCustomMapCacheVersion && customMapCache.Maps != null
                    ? customMapCache.Maps : new ConcurrentDictionary<string, Map>();

                foreach (var customMap in customMaps.Values)
                    customMap.CalculateSHA();

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
        /// <returns>The map if loading it was succesful, otherwise false.</returns>
        public Map LoadCustomMap(string mapPath, out string resultMessage)
        {
            if (!File.Exists(ProgramConstants.GamePath + mapPath + MAP_FILE_EXTENSION))
            {
                Logger.Log("LoadCustomMap: Map " + mapPath + " not found!");
                resultMessage = $"Map file {mapPath}{MAP_FILE_EXTENSION} doesn't exist!";

                return null;
            }

            Logger.Log("LoadCustomMap: Loading custom map " + mapPath);
            var iniPath = ProgramConstants.GamePath + mapPath + MAP_FILE_EXTENSION;
            Map map = new Map(mapPath, iniPath);

            if (map.SetInfoFromCustomMap())
            {
                foreach (GameMode gm in GameModes)
                {
                    if (gm.Maps.Find(m => m.SHA1 == map.SHA1) != null)
                    {
                        Logger.Log("LoadCustomMap: Custom map " + mapPath + " is already loaded!");
                        resultMessage = $"Map {mapPath} is already loaded.";

                        return null;
                    }
                }

                Logger.Log("LoadCustomMap: Map " + mapPath + " added succesfully.");

                AddMapToGameModes(map, true);

                resultMessage = $"Map {mapPath} loaded succesfully.";

                return map;
            }

            Logger.Log("LoadCustomMap: Loading map " + mapPath + " failed!");
            resultMessage = $"Loading map {mapPath} failed!";

            return null;
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
                        Logger.Log("AddMapToGameModes: Added map " + map.Name + " to game mode " + gm.Name);
                }
            }
        }
    }
}
