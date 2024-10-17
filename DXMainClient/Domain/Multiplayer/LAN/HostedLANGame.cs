using ClientCore;
using ClientCore.CnCNet5;
using DTAClient.Domain.Multiplayer;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DTAClient.Domain.LAN
{
    class HostedLANGame : GenericHostedGame
    {
        public IPEndPoint EndPoint { get; set; }
        public string LoadedGameID { get; set; }

        public TimeSpan TimeWithoutRefresh { get; set; }

        public override int Ping
        {
            get
            {
                return -1;
            }
        }

        public bool SetDataFromStringArray(GameCollection gc, string[] parameters)
        {
            if (parameters.Length != 9)
            {
                Logger.Log("Ignoring LAN GAME message because of an incorrect number of parameters.");
                return false;
            }

            if (parameters[0] != ProgramConstants.LAN_PROTOCOL_REVISION)
                return false;

            GameVersion = parameters[1];
            Incompatible = GameVersion != ProgramConstants.GAME_VERSION;
            Game = gc.GameList.Find(g => g.InternalName.ToUpperInvariant() == parameters[2]);
            if (Game == null)
                return false;
            Map = parameters[3];
            GameMode = parameters[4];
            LoadedGameID = parameters[5];

            List<PlayerInfo> playerInfos = new List<PlayerInfo>();

            List<string> playerNames = parameters[6].Split(',').ToList();
            foreach (string playerName in playerNames)
            {
                playerInfos.Add(new PlayerInfo(playerName));
            }

            Players = playerInfos;
            if (playerInfos.Count == 0)
                return false;

            HostUserName = playerInfos[0].Name;
            Locked = Conversions.IntFromString(parameters[7], 1) > 0;
            IsLoadedGame = Conversions.IntFromString(parameters[8], 0) > 0;
            LastRefreshTime = DateTime.Now;
            TimeWithoutRefresh = TimeSpan.Zero;
            RoomName = HostUserName + "'s Game";

            return true;
        }
    }
}
