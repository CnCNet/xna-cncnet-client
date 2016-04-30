using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace DTAClient.domain.CnCNet
{
    public class MapLoader
    {
        const string MAP_FILE_EXTENSION = ".map";

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

        private void LoadMaps()
        {
            Logger.Log("Loading maps.");

            IniFile mpMapsIni = new IniFile(ProgramConstants.GamePath + DomainController.Instance().GetMPMapsIniPath());

            List<string> keys = mpMapsIni.GetSectionKeys("MultiMaps");

            if (keys == null)
                throw new Exception("Loading MPMaps.ini failed!");

            List<Map> maps = new List<Map>();

            foreach (string key in keys)
            {
                string mapFilePath = mpMapsIni.GetStringValue("MultiMaps", key, String.Empty);

                if (!File.Exists(ProgramConstants.GamePath + mapFilePath + MAP_FILE_EXTENSION))
                {
                    Logger.Log("Map " + mapFilePath + " doesn't exist!");
                    continue;
                }

                Map map = new Map(mapFilePath);

                if (!map.SetInfoFromINI(mpMapsIni))
                    continue;

                maps.Add(map);
            }

            foreach (Map map in maps)
            {
                foreach (string gameMode in map.GameModes)
                {
                    GameMode gm = GameModes.Find(g => g.Name == gameMode);

                    if (gm == null)
                    {
                        gm = new GameMode();
                        gm.Name = gameMode;
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
