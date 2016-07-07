using ClientCore;
using DTAClient.domain.Multiplayer;
using Rampastring.Tools;
using System;
using System.Net;

namespace DTAClient.domain.LAN
{
    class LANGame : GenericHostedGame
    {
        public IPEndPoint EndPoint { get; set; }
        public string LoadedGameID { get; set; }
        public TimeSpan TimeWithoutRefresh { get; set; }

        public bool SetDataFromStringArray(GameCollection gc, string[] parameters)
        {
            if (parameters.Length < 10)
            {
                Logger.Log("Ignoring LAN GAME message because of an incorrect number of parameters.");
                return false;
            }

            if (parameters[0] != ProgramConstants.LAN_PROTOCOL_REVISION)
                return false;

            GameVersion = parameters[1];
            Incompatible = GameVersion != ProgramConstants.GAME_VERSION;
            Game = gc.GameList.Find(g => g.InternalName.ToUpper() == parameters[2]);
            RoomName = parameters[3];
            Map = parameters[4];
            GameMode = parameters[5];
            LoadedGameID = parameters[6];
            string[] players = parameters[7].Split(',');
            Players = players;
            if (players.Length == 0)
                return false;
            HostName = players[0];
            if (parameters[8].Length != 2)
                return false;
            Locked = Conversions.IntFromString(parameters[8], 1) > 0;
            IsLoadedGame = Conversions.IntFromString(parameters[9], 0) > 0;
            LastRefreshTime = DateTime.Now;
            TimeWithoutRefresh = TimeSpan.Zero;

            return true;
        }
    }
}
