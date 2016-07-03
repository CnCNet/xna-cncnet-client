/// @author Rami "Rampastring" Pasanen
/// http://www.moddb.com/members/rampastring
/// @version 16. 12. 2014

using System.Collections.Generic;

namespace ClientCore.CnCNet5
{
    /// <summary>
    /// A class for helping interaction between different parts of the client and
    /// for storing data used by multiple components of the client.
    /// </summary>
    public static class CnCNetData
    {
        public delegate void GameStartedEventHandler();
        public static event GameStartedEventHandler OnGameStarted;

        public static RConnectionBridge ConnectionBridge = new RConnectionBridge();

        public static List<Map> MapList = new List<Map>();
        public static List<string> GameTypes = new List<string>();

        public static int AmountOfOfficialMaps = 0;

        public static void DoGameStarted()
        {
            if (OnGameStarted != null)
                OnGameStarted();
        }

        public delegate void GameStoppedEventHandler();
        public static event GameStoppedEventHandler OnGameStopped;

        public static void DoGameStopped()
        {
            if (OnGameStopped != null)
                OnGameStopped();
        }

        public delegate void GameLobbyClosedEventHandler();
        public static event GameLobbyClosedEventHandler OnGameLobbyClosed;
        public delegate void GameLoadingLobbyClosedEventHandler();
        public static event GameLoadingLobbyClosedEventHandler OnGameLoadingLobbyClosed;

        public static void DoGameLobbyClosed()
        {
            if (OnGameLobbyClosed != null)
                OnGameLobbyClosed();
        }

        public static void DoGameLoadingLobbyClosed()
        {
            if (OnGameLoadingLobbyClosed != null)
                OnGameLoadingLobbyClosed();
        }

        public static List<string> players = new List<string>();
        public static bool isPMWindowOpen = false;
        public static bool IsGameLobbyOpen = false;
        public static bool IsGameLoadingLobbyOpen = false;
        public static List<PrivateMessageInfo> PMInfos = new List<PrivateMessageInfo>();
    }
}
