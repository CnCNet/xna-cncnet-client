using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace DTAClient.domain.CnCNet
{
    public class HostedGame
    {
        public HostedGame() { }

        public HostedGame(string channelName, string revision, string gameId, string gamever, int maxPlayers,
            string roomName, bool passworded, bool started,
            bool tunneled, 
            string[] players, string adminName, string mapName, string gameMode)
        {
            ChannelName = channelName;
            Revision = revision;
            GameIdentifier = gameId;
            Version = gamever;
            MaxPlayers = maxPlayers;
            RoomName = roomName;
            Passworded = passworded;
            Started = started;
            Tunneled = tunneled;
            foreach (string player in players)
            {
                Players.Add(player);
            }
            Admin = adminName;
            MapName = mapName;
            GameMode = gameMode;
        }

        public string ChannelName { get; set; }
        public string Revision { get; set; }
        public string GameIdentifier { get; set; }
        public string Version { get; set; }
        public int MaxPlayers { get; set; }
        public string RoomName { get; set; }
        public bool Passworded { get; set; }
        public bool TourneyReserved { get; set; }
        public bool Started { get; set; }
        public bool Tunneled { get; set; }
        public bool PureReserved { get; set; }
        public bool MatchmakingReserved { get; set; }
        public bool IsLoadedGame { get; set; }
        public bool IsLadder { get; set; }
        public string MatchID { get; set; }
        public List<string> Players = new List<string>();
        public string Admin { get; set; }
        public string MapName { get; set; }
        public string GameMode { get; set; }
        public CnCNetTunnel TunnelServer { get; set; }
        public DateTime LastRefreshTime { get; set; }
        public Texture2D GameTexture { get; set; }
        public bool IsIncompatible { get; set; }
        public bool IsLocked { get; set; }
    }
}
