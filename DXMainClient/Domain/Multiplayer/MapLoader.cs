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
        public const string MAP_CHAT_COMMAND_FILENAME_PREFIX = "user_chat_command_download";
        private const string CUSTOM_MAPS_DIRECTORY = "Maps/Custom";
        private static readonly string CUSTOM_MAPS_CACHE = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "custom_map_cache");
        private const string MultiMapsSection = "MultiMaps";
        private const string GameModesSection = "GameModes";
        private const string GameModeAliasesSection = "GameModeAliases";
        private const int CurrentCustomMapCacheVersion = 1;

        /// <summary>
        /// The relative path to the folder where custom maps are stored.
        /// This is the public version of CUSTOM_MAPS_DIRECTORY with a "/" added for convenience.
        /// </summary>
        public const string CustomMapsDirectory = CUSTOM_MAPS_DIRECTORY + "/";

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
        /// An event that will be fired when a new map is loaded while the client is already running.
        /// </summary>
        public static event EventHandler<MapLoaderEventArgs> GameModeMapsUpdated;

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

        private FileSystemWatcher customMapFileWatcher;

        /// <summary>
        /// Check to see if a map matching the SHA-1 ID is already loaded.
        /// </summary>
        /// <param name="sha1">The map ID to search the loaded maps for.</param>
        public bool IsMapAlreadyLoaded(string sha1) => GetLoadedMapBySha1(sha1) != null;

        /// <summary>
        /// Search the loaded maps for the SHA-1, return the map if a match is found.
        /// </summary>
        /// <param name="sha1">The map ID to search the loaded maps for.</param>
        /// <returns>The map matching the SHA-1 if one was found.</returns>
        public GameModeMap GetLoadedMapBySha1(string sha1) => GameModeMaps.Find(gmm => gmm.Map.SHA1 == sha1);

        /// <summary>
        /// Loads multiplayer map info asynchonously.
        /// </summary>
        public void LoadMapsAsync()
        {
            Thread thread = new Thread(LoadMaps);
            thread.Start();
        }

        /// <summary>
        /// Start the file watcher for the custom map directory.
        ///
        /// This will refresh the game modes and map lists when a change is detected. 
        /// </summary>
        public void StartCustomMapFileWatcher()
        {
            customMapFileWatcher = new FileSystemWatcher(SafePath.CombineDirectoryPath(ProgramConstants.GamePath, CustomMapsDirectory));

            customMapFileWatcher.Filter = $"*{MAP_FILE_EXTENSION}";

            customMapFileWatcher.Created += HandleCustomMapFolder_Created;
            customMapFileWatcher.Deleted += HandleCustomMapFolder_Deleted;
            customMapFileWatcher.Renamed += HandleCustomMapFolder_Renamed;
            customMapFileWatcher.Error += HandleCustomMapFolder_Error;

            customMapFileWatcher.IncludeSubdirectories = false;
            customMapFileWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Handle a file being moved / copied / created in the custom map directory.
        ///
        /// Adds the map to the GameModeMaps and updates the UI.
        /// </summary>
        /// <param name="sender">Sent by the file system watcher</param>
        /// <param name="e">Sent by the file system watcher</param>
        private void HandleCustomMapFolder_Created(object sender, FileSystemEventArgs e)
        {
            // Get the map filename without the extension.
            // The extension gets added in LoadCustomMap so we need to excise it to avoid "file.map.map".
            string name = Path.GetFileNameWithoutExtension(e.Name);

            string relativeMapPath = SafePath.CombineFilePath(CustomMapsDirectory, name);
            Logger.Log($"HandleCustomMapFolder_Created: Calling LoadCustomMap: mapPath={relativeMapPath}");
            Map map = LoadCustomMap(relativeMapPath, out string result);
            Logger.Log($"HandleCustomMapFolder_Created: Ended LoadCustomMap: mapPath={relativeMapPath}");

            if (map == null)
                Logger.Log($"Failed to load map file that was create / moved: mapPath={name}, reason={result}");
        }

        /// <summary>
        /// Handle a .map file being removed from the custom map directory.
        ///
        /// This function will attempt to remove the map from the client if it was deleted from the folder.
        /// </summary>
        /// <param name="sender">Sent by the file system watcher.</param>
        /// <param name="e">Sent by the file system watcher.</param>
        private void HandleCustomMapFolder_Deleted(object sender, FileSystemEventArgs e)
        {
            Logger.Log($"Map was deleted: map={e.Name}");
            // Use the filename without the extension so we can remove maps that had their extension changed.
            string name = Path.GetFileNameWithoutExtension(e.Name);
            // The way we're detecting the loaded map is hacky, but we don't
            // have the SHA-1 to work with.
            foreach (GameMode gameMode in GameModes)
            {
                gameMode.Maps.RemoveAll(map => Path.GetFileNameWithoutExtension(map.CompleteFilePath).EndsWith(name));
            }

            RemoveEmptyGameModesAndUpdateGameModeMaps();
            GameModeMapsUpdated?.Invoke(null, new MapLoaderEventArgs(null));
        }

        /// <summary>
        /// Handle a file being renamed in the custom map folder.
        ///
        /// If a file is renamed from "something.map" to "somethingelse.map" then there is a high likelyhood
        /// that nothing will change in the client because the map data was already loaded.
        ///
        /// This is mainly here because Final Alert 2 will often export as ".yrm" which requires a rename.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleCustomMapFolder_Renamed(object sender, RenamedEventArgs e)
        {
            string name = Path.GetFileNameWithoutExtension(e.Name);
            string relativeMapPath = SafePath.CombineFilePath(CustomMapsDirectory, name);
            bool oldPathIsMap = Path.GetExtension(e.OldName) == MAP_FILE_EXTENSION;
            bool newPathIsMap = Path.GetExtension(e.Name) == MAP_FILE_EXTENSION;

            // Check if the user is renaming a non ".map" file.
            // This is just for logging to help debug.
            if (!oldPathIsMap && newPathIsMap)
            {
                Logger.Log($"HandleCustomMapFolder_Renamed: Changed the file extension. User is likely renaming a '.yrm' from Final Alert 2: old={e.OldName}, new={e.Name}");
            }
            else if (oldPathIsMap && !newPathIsMap)
            {
                // A bit hacky, but this is a rare case.
                Logger.Log($"HandleCustomMapFolder_Renamed: Changed the file extension to no longer be '.map' for some reason, removing from map list: old={e.OldName}, new={e.Name}");
                HandleCustomMapFolder_Deleted(sender, e);
                return;
            }
            else if (!newPathIsMap)
            {
                Logger.Log($"HandleCustomMapFolder_Renamed: New extension is not '{MAP_FILE_EXTENSION}', moving on: file={e.Name}");
                return;
            }

            Map map = LoadCustomMap(relativeMapPath, out string result);

            if (map != null)
            {
                Logger.Log($"HandleCustomMapFolder_Renamed: Loaded renamed file as map: file={e.Name}, mapName={map.Name}");
                return;
            }

            Logger.Log($"Failed to load renamed map file. Map is likely already loaded, filepath: original={e.OldName}, new={e.Name}, reason={result}");
        }

        /// <summary>
        /// Handle errors in the filewatcher.
        /// </summary>
        private void HandleCustomMapFolder_Error(object sender, ErrorEventArgs e)
        {
            Exception exc = e.GetException();
            Logger.Log($"The custom map folder file watcher crashed: error={exc.Message}");
            Logger.Log("Stack Trace:");
            Logger.Log(exc.StackTrace);
        }

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

            RemoveEmptyGameModesAndUpdateGameModeMaps();

            MapLoadingComplete?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Remove any game modes that do not have any maps loaded and update `GameModeMaps` for the new `GameModes`.
        /// </summary>
        private void RemoveEmptyGameModesAndUpdateGameModeMaps()
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
                FileInfo mapFile = SafePath.GetFile(ProgramConstants.GamePath, FormattableString.Invariant($"{mapFilePath}{MAP_FILE_EXTENSION}"));

                if (!mapFile.Exists)
                {
                    Logger.Log("Map " + mapFile.FullName + " doesn't exist!");
                    continue;
                }

                Map map = new Map(mapFilePathValue);

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

                    Map map = new Map(baseFilePath
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(Path.AltDirectorySeparatorChar, '/'), mapFile.FullName);
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
        ///
        /// This should only be used after maps are loaded at startup. 
        /// </summary>
        /// <param name="mapPath">The path to the map file relative to the game directory. Don't include the file-extension.</param>
        /// <param name="resultMessage">When method returns, contains a message reporting whether or not loading the map failed and how.</param>
        /// <returns>The map if loading it was succesful, otherwise false.</returns>
        public Map LoadCustomMap(string mapPath, out string resultMessage)
        {
            // Create the full path to the map file.
            string customMapFilePath = SafePath.CombineFilePath(ProgramConstants.GamePath, FormattableString.Invariant($"{mapPath}{MAP_FILE_EXTENSION}"));
            FileInfo customMapFile = SafePath.GetFile(customMapFilePath);

            if (!customMapFile.Exists)
            {
                Logger.Log("LoadCustomMap: Map " + customMapFile.FullName + " not found!");
                resultMessage = $"Map file {customMapFile.Name} doesn't exist!";

                return null;
            }

            Logger.Log("LoadCustomMap: Loading custom map " + customMapFile.FullName);

            Map map = new Map(mapPath, customMapFilePath);

            // Make sure we can get the map info from the .map file.
            if (!map.SetInfoFromCustomMap())
            {
                Logger.Log("LoadCustomMap: Loading map " + customMapFile.FullName + " failed!");
                resultMessage = $"Loading map {customMapFile.FullName} failed!";

                return null;
            }

            // Make sure we don't accidentally load the same map twice.
            // This checks the SHA-1, so duplicate maps in two .map files with different filenames can still be detected.
            if (IsMapAlreadyLoaded(map.SHA1))
            {
                Logger.Log($"LoadCustomMap: Custom map {customMapFile.FullName} is already loaded! SHA1={map.SHA1}");
                resultMessage = $"Map {customMapFile.FullName} is already loaded.";

                return null;
            }


            AddMapToGameModes(map, true);
            var gameModes = GameModes.Where(gm => gm.Maps.Contains(map));
            GameModeMaps.AddRange(gameModes.Select(gm => new GameModeMap(gm, map, false)));

            // Notify the UI to update the gamemodes dropdown.
            GameModeMapsUpdated?.Invoke(null, new MapLoaderEventArgs(map));

            resultMessage = $"Map {customMapFile.FullName} loaded succesfully.";
            Logger.Log("LoadCustomMap: Map " + customMapFile.FullName + " added succesfully.");

            return map;
        }

        public void DeleteCustomMap(GameModeMap gameModeMap)
        {
            Logger.Log("Deleting map " + gameModeMap.Map.Name);
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
                        Logger.Log("AddMapToGameModes: Added map " + map.Name + " to game mode " + gm.Name);
                }
            }
        }
    }
}
