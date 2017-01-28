using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DTAClient.Domain.Multiplayer
{
    public class MapLoader
    {
        private const string MAP_FILE_EXTENSION = ".map";
        private const string CUSTOM_MAPS_DIRECTORY = "Maps\\Custom";

        public List<GameMode> GameModes = new List<GameMode>();

        /// <summary>
        /// An event that is fired when the maps have been loaded.
        /// </summary>
        public event EventHandler MapLoadingComplete;

        /// <summary>
        /// Loads multiplayer map info asynchonously.
        /// </summary>
        public void LoadMapsAsync()
        {
            Thread thread = new Thread(LoadMaps);
            thread.Start();
        }

        public void LoadMaps()
        {
            Logger.Log("Loading maps.");

            IniFile mpMapsIni = new IniFile(ProgramConstants.GamePath + ClientConfiguration.Instance.MPMapsIniPath);

            List<string> keys = mpMapsIni.GetSectionKeys("MultiMaps");

            if (keys == null)
            {
                Logger.Log("Loading multiplayer map list failed!!!");
                return;
            }

            List<Map> maps = new List<Map>();

            foreach (string key in keys)
            {
                string mapFilePath = mpMapsIni.GetStringValue("MultiMaps", key, String.Empty);

                if (!File.Exists(ProgramConstants.GamePath + mapFilePath + MAP_FILE_EXTENSION))
                {
                    Logger.Log("Map " + mapFilePath + " doesn't exist!");
                    continue;
                }

                Map map = new Map(mapFilePath, true);

                if (!map.SetInfoFromINI(mpMapsIni))
                    continue;

                maps.Add(map);
            }

            if (!Directory.Exists(ProgramConstants.GamePath + CUSTOM_MAPS_DIRECTORY))
            {
                Logger.Log("Custom maps directory does not exist!");
            }
            else
            {
                string[] files = Directory.GetFiles(ProgramConstants.GamePath + CUSTOM_MAPS_DIRECTORY, "*.map");

                foreach (string file in files)
                {
                    string baseFilePath = file.Substring(ProgramConstants.GamePath.Length);
                    baseFilePath = baseFilePath.Substring(0, baseFilePath.Length - 4);

                    Map map = new Map(baseFilePath, false);
                    if (map.SetInfoFromMap(file))
                        maps.Add(map);
                }
            }

            foreach (Map map in maps)
            {
                foreach (string gameMode in map.GameModes)
                {
                    GameMode gm = GameModes.Find(g => g.Name == gameMode);

                    if (gm == null)
                    {
                        gm = new GameMode();
                        gm.Name = gameMode.Replace(";", string.Empty);
                        gm.Initialize();
                        GameModes.Add(gm);
                    }

                    gm.Maps.Add(map);
                }
            }

            MapLoadingComplete?.Invoke(this, EventArgs.Empty);
        }
    }
}
