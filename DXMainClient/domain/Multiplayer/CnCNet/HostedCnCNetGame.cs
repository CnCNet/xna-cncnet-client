using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace DTAClient.domain.Multiplayer.CnCNet
{
    public class HostedCnCNetGame : GenericHostedGame
    {
        public HostedCnCNetGame() { }

        public HostedCnCNetGame(string channelName, string revision, string gamever, int maxPlayers,
            string roomName, bool passworded, bool started,
            bool tunneled, 
            string[] players, string adminName, string mapName, string gameMode)
        {
            ChannelName = channelName;
            Revision = revision;
            GameVersion = gamever;
            MaxPlayers = maxPlayers;
            RoomName = roomName;
            Passworded = passworded;
            Started = started;
            Tunneled = tunneled;
            Players = players;
            HostName = adminName;
            Map = mapName;
            GameMode = gameMode;
        }

        public string ChannelName { get; set; }
        public string Revision { get; set; }
        public bool Started { get; set; }
        public bool Tunneled { get; set; }
        public bool IsLadder { get; set; }
        public string MatchID { get; set; }
        public CnCNetTunnel TunnelServer { get; set; }
    }
}
