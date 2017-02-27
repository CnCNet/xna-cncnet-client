using System.Collections.Generic;
using System.Drawing;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework.Graphics;
using ClientCore.Properties;
using System.Linq;

namespace ClientCore.CnCNet5
{
    /// <summary>
    /// A class for storing the collection of supported CnCNet games.
    /// </summary>
    public class GameCollection
    {
        public List<CnCNetGame> GameList
        {
            get { return _gameList; }
        }

        private List<CnCNetGame> _gameList;

        public void Initialize(GraphicsDevice gd)
        {
            _gameList = new CnCNetGame[]
            {
                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-dta",
                    ClientExecutableName = "DTA.exe",
                    GameBroadcastChannel = "#cncnet-dta-games",
                    InternalName = "dta",
                    RegistryInstallPath = "HKCU\\Software\\TheDawnOfTheTiberiumAge",
                    UIName = "Dawn of the Tiberium Age",
                    Texture = AssetLoader.TextureFromImage(Resources.dtaicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-ti",
                    ClientExecutableName = "TI_Launcher.exe",
                    GameBroadcastChannel = "#cncnet-ti-games",
                    InternalName = "ti",
                    RegistryInstallPath = "HKCU\\Software\\TwistedInsurrection",
                    UIName = "Twisted Insurrection",
                    Texture = AssetLoader.TextureFromImage(Resources.tiicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-ts",
                    ClientExecutableName = "TiberianSun.exe",
                    GameBroadcastChannel = "#cncnet-ts-games",
                    InternalName = "ts",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Tiberian Sun",
                    UIName = "Tiberian Sun",
                    Texture = AssetLoader.TextureFromImage(Resources.tsicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-mo",
                    ClientExecutableName = "MentalOmegaClient.exe",
                    GameBroadcastChannel = "#cncnet-mo-games",
                    InternalName = "mo",
                    RegistryInstallPath = "HKCU\\Software\\MentalOmega",
                    UIName = "Mental Omega",
                    Texture = AssetLoader.TextureFromImage(Resources.moicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-yr",
                    ClientExecutableName = "CnCNetClientYR.exe",
                    GameBroadcastChannel = "#cncnet-yr-games",
                    InternalName = "yr",
                    RegistryInstallPath = "HKLM\\Software\\Westwood\\Yuri's Revenge",
                    UIName = "Yuri's Revenge",
                    Texture = AssetLoader.TextureFromImage(Resources.yricon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet",
                    InternalName = "cncnet",
                    UIName = "General CnCNet Chat",
                    AlwaysEnabled = true,
                    Texture = AssetLoader.TextureFromImage(Resources.cncneticon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-td",
                    InternalName = "td",
                    UIName = "Tiberian Dawn",
                    Supported = false,
                    Texture = AssetLoader.TextureFromImage(Resources.tdicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-ra",
                    InternalName = "ra",
                    UIName = "Red Alert",
                    Supported = false,
                    Texture = AssetLoader.TextureFromImage(Resources.raicon)
                },

                new CnCNetGame()
                {
                    ChatChannel = "#cncnet-d2",
                    InternalName = "d2",
                    UIName = "Dune 2000",
                    Supported = false,
                    Texture = AssetLoader.TextureFromImage(Resources.unknownicon)
                }

            }.ToList();
        }

        /// <summary>
        /// Gets the index of a CnCNet supported game based on its internal name.
        /// </summary>
        /// <param name="gameName">The internal name (suffix) of the game.</param>
        /// <returns>The index of the specified CnCNet game. -1 if the game is unknown or not supported.</returns>
        public int GetGameIndexFromInternalName(string gameName)
        {
            for (int gId = 0; gId < _gameList.Count; gId++)
            {
                CnCNetGame game = _gameList[gId];

                if (gameName.ToLower() == game.InternalName)
                    return gId;
            }

            return -1;
        }

        /// <summary>
        /// Seeks the supported game list for a specific game's internal name and if found,
        /// returns the game's full name. Otherwise returns the internal name specified in the param.
        /// </summary>
        /// <param name="gameName">The internal name of the game to seek for.</param>
        /// <returns>The full name of a supported game based on its internal name.
        /// Returns the given parameter if the name isn't found in the supported game list.</returns>
        public string GetGameNameFromInternalName(string gameName)
        {
            CnCNetGame game = _gameList.Find(g => g.InternalName == gameName.ToLower());

            if (game == null)
                return gameName;

            return game.UIName;
        }

        /// <summary>
        /// Returns the full UI name of a game based on its index in the game list.
        /// </summary>
        /// <param name="gameIndex">The index of the CnCNet supported game.</param>
        /// <returns>The UI name of the game.</returns>
        public string GetFullGameNameFromIndex(int gameIndex)
        {
            return _gameList[gameIndex].UIName;
        }

        /// <summary>
        /// Returns the internal name of a game based on its index in the game list.
        /// </summary>
        /// <param name="gameIndex">The index of the CnCNet supported game.</param>
        /// <returns>The internal name (suffix) of the game.</returns>
        public string GetGameIdentifierFromIndex(int gameIndex)
        {
            return _gameList[gameIndex].InternalName;
        }

        public string GetGameBroadcastingChannelNameFromIdentifier(string gameIdentifier)
        {
            return _gameList.Find(g => g.InternalName == gameIdentifier.ToLower()).GameBroadcastChannel;
        }

        public string GetGameChatChannelNameFromIdentifier(string gameIdentifier)
        {
            return _gameList.Find(g => g.InternalName == gameIdentifier.ToLower()).ChatChannel;
        }
    }
}
