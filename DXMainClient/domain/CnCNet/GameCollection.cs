using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework.Graphics;
using DTAClient.Properties;

namespace DTAClient.domain.CnCNet
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
        private bool[] GameFollowedArray;

        public void Initialize(GraphicsDevice gd)
        {
            Logger.Log("Initializing supported game list.");

            _gameList = new List<CnCNetGame>();

            CnCNetGame dta = new CnCNetGame();
            dta.ChatChannel = "#cncnet-dta";
            dta.ClientExecutableName = "DTA.exe";
            dta.ClientRunArguments = "-RUNCLIENT";
            dta.GameBroadcastChannel = "#cncnet-dta-games";
            dta.InternalName = "dta";
            dta.RegistryInstallPath = "HKCU\\Software\\TheDawnOfTheTiberiumAge";
            dta.UIName = "Dawn of the Tiberium Age";
            dta.Texture = AssetLoader.TextureFromImage(Resources.dtaicon);

            _gameList.Add(dta);

            CnCNetGame ti = new CnCNetGame();
            ti.ChatChannel = "#cncnet-ti";
            ti.ClientExecutableName = "TI_Launcher.exe";
            ti.ClientRunArguments = "-RUNCLIENT";
            ti.GameBroadcastChannel = "#cncnet-ti-games";
            ti.InternalName = "ti";
            ti.RegistryInstallPath = "HKCU\\Software\\TwistedInsurrection";
            ti.UIName = "Twisted Insurrection";
            ti.Texture = AssetLoader.TextureFromImage(Resources.tiicon);

            _gameList.Add(ti);

            CnCNetGame ts = new CnCNetGame();
            ts.ChatChannel = "#cncnet-ts";
            ts.ClientExecutableName = "TiberianSun.exe";
            ts.ClientRunArguments = "-RUNCLIENT";
            ts.GameBroadcastChannel = "#cncnet-ts-games";
            ts.InternalName = "ts";
            ts.RegistryInstallPath = "HKLM\\Software\\Westwood\\Tiberian Sun";
            ts.UIName = "Tiberian Sun";
            ts.Texture = AssetLoader.TextureFromImage(Resources.tsicon);

            _gameList.Add(ts);

            //CnCNetGame to = new CnCNetGame();
            //to.ChatChannel = "#to";
            //to.ClientExecutableName = "TiberianOdyssey.exe";
            //to.ClientRunArguments = "-RUNCLIENT";
            //to.GameBroadcastChannel = "#to-games";
            //to.InternalName = "to";
            //to.RegistryInstallPath = "HKCU\\Software\\TiberianOdyssey";
            //to.UIName = "Tiberian Odyssey";

            //GameList.Add(to);

            CnCNetGame yr = new CnCNetGame();
            yr.ChatChannel = "#cncnet-yr";
            yr.ClientExecutableName = "CnCNetClientYR.exe";
            yr.GameBroadcastChannel = "#cncnet-yr-games";
            yr.InternalName = "yr";
            yr.RegistryInstallPath = "HKLM\\Software\\Westwood\\Yuri's Revenge";
            yr.UIName = "Yuri's Revenge";
            yr.Texture = AssetLoader.TextureFromImage(Resources.yricon);

            _gameList.Add(yr);

            CnCNetGame mo = new CnCNetGame();
            mo.ChatChannel = "#cncnet-mo";
            mo.ClientExecutableName = "MOLauncher.exe";
            mo.GameBroadcastChannel = "#cncnet-mo-games";
            mo.InternalName = "mo";
            mo.RegistryInstallPath = "HKML\\Software\\MentalOmega";
            mo.UIName = "Mental Omega";
            mo.Texture = AssetLoader.TextureFromImage(Resources.moicon);

            _gameList.Add(mo);

            CnCNetGame generalChat = new CnCNetGame();
            generalChat.ChatChannel = "#cncnet";
            generalChat.InternalName = "cncnet";
            generalChat.UIName = "General CnCNet Chat";
            generalChat.AlwaysEnabled = true;
            generalChat.Texture = AssetLoader.TextureFromImage(Resources.cncneticon);

            _gameList.Add(generalChat);

            CnCNetGame td = new CnCNetGame();
            td.ChatChannel = "#cncnet-td";
            td.InternalName = "td";
            td.UIName = "Tiberian Dawn";
            td.Supported = false;
            td.Texture = AssetLoader.TextureFromImage(Resources.tdicon);

            _gameList.Add(td);

            CnCNetGame ra = new CnCNetGame();
            ra.ChatChannel = "#cncnet-ra";
            ra.InternalName = "ra";
            ra.UIName = "Red Alert";
            ra.Supported = false;
            ra.Texture = AssetLoader.TextureFromImage(Resources.raicon);

            _gameList.Add(ra);

            CnCNetGame d2 = new CnCNetGame();
            d2.ChatChannel = "#cncnet-d2";
            d2.InternalName = "d2";
            d2.UIName = "Dune 2000";
            d2.Supported = false;
            d2.Texture = AssetLoader.TextureFromImage(Resources.unknownicon);

            _gameList.Add(d2);

            GameFollowedArray = new bool[_gameList.Count];
        }

        /// <summary>
        /// Returns an array of game icons for the supported games.
        /// </summary>
        /// <returns>An array of icons for the supported games.</returns>
        public Image[] GetGameImages()
        {
            Image[] returnValue = new Image[_gameList.Count];

            for (int gId = 0; gId < _gameList.Count; gId++)
            {
                CnCNetGame game = _gameList[gId];

                Bitmap image = (Bitmap)(Resources.ResourceManager.GetObject(game.InternalName + "icon"));

                if (image == null)
                    image = (Bitmap)(Resources.ResourceManager.GetObject("unknownicon"));

                returnValue[gId] = image;
            }

            return returnValue;
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
        /// Gets the amount of CnCNet supported games.
        /// </summary>
        /// <returns>The amount of CnCNet games.</returns>
        public int GetGameCount()
        {
            return _gameList.Count;
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

        public string GetGameIdentifierFromGameBroadcastingChannel(string gameBroadcastingChannel)
        {
            return _gameList.Find(g => g.GameBroadcastChannel == gameBroadcastingChannel).InternalName;
        }

        /// <summary>
        /// Marks a certain game as followed.
        /// </summary>
        /// <param name="gameIndex">The index of the game to follow.</param>
        public void FollowGame(int gameIndex)
        {
            GameFollowedArray[gameIndex] = true;
        }

        /// <summary>
        /// Marks a certain game as unfollowed.
        /// </summary>
        /// <param name="gameIndex">The index of the game to unfollow.</param>
        public void UnfollowGame(int gameIndex)
        {
            GameFollowedArray[gameIndex] = false;
        }

        /// <summary>
        /// Returns true if a specific game is currently followed, otherwise false.
        /// </summary>
        /// <param name="gameIndex">The index of the game.</param>
        /// <returns>True if the game is currently followed, otherwise false.</returns>
        public bool IsGameFollowed(int gameIndex)
        {
            return GameFollowedArray[gameIndex];
        }

        /// <summary>
        /// Returns true if a specific game is supported by this client, otherwise false.
        /// </summary>
        /// <param name="gameIndex">The index of the game.</param>
        /// <returns>True if the game is supported, otherwise false.</returns>
        public bool IsGameSupported(int gameIndex)
        {
            return _gameList[gameIndex].Supported;
        }

        /// <summary>
        /// Returns the game broadcasting channel of the specific game.
        /// </summary>
        /// <param name="gameIndex">The index of the game.</param>
        /// <returns>The IRC channel name of the specific game's game broadcasting channel.</returns>
        public string GetGameBroadcastingChannelNameFromIndex(int gameIndex)
        {
            return _gameList[gameIndex].GameBroadcastChannel;
        }

        public string GetGameBroadcastingChannelNameFromIdentifier(string gameIdentifier)
        {
            return _gameList.Find(g => g.InternalName == gameIdentifier.ToLower()).GameBroadcastChannel;
        }

        /// <summary>
        /// Returns the chat channel name of the specific game.
        /// </summary>
        /// <param name="gameIndex">The index of the game.</param>
        /// <returns>The IRC channel name of the specific game's chat channel.</returns>
        public string GetGameChatChannelNameFromIndex(int gameIndex)
        {
            return _gameList[gameIndex].ChatChannel;
        }


        public string GetGameChatChannelNameFromIdentifier(string gameIdentifier)
        {
            return _gameList.Find(g => g.InternalName == gameIdentifier.ToLower()).ChatChannel;
        }


        /// <summary>
        /// Returns a CnCNet supported game from an index.
        /// </summary>
        /// <param name="gameIndex">The index of the game to return.</param>
        /// <returns>The game associated with the index.</returns>
        public CnCNetGame GetGameFromIndex(int gameIndex)
        {
            return _gameList[gameIndex];
        }

        /// <summary>
        /// Returns a game's index based on its chat channel name.
        /// </summary>
        /// <param name="chatChannel">The name of the game's chat channel.</param>
        /// <returns>The game's index.</returns>
        public int GetGameIndexFromChatChannelName(string chatChannel)
        {
            return _gameList.FindIndex(g => g.ChatChannel == chatChannel.ToLower());
        }

        /// <summary>
        /// Returns a list of internal game names for currently followed games.
        /// </summary>
        /// <returns>A list of internal game names for currently followed games.</returns>
        public List<string> GetInternalNamesOfFollowedGames()
        {
            List<string> internalNames = new List<string>();

            for (int i = 0; i < _gameList.Count; i++)
            {
                if (IsGameFollowed(i))
                    internalNames.Add(_gameList[i].InternalName);
            }

            return internalNames;
        }
    }
}
