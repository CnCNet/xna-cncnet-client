using System;
using System.Collections.Generic;
using DTAClient.Domain.Multiplayer.CnCNet;

namespace DTAClient.Online.EventArguments
{
    public class ChannelGameEventArgs : ChannelCTCPEventArgs
    {

        public ChannelGameEventArgs(string userName, string message, ChannelUser channelUser) : base(userName, message, channelUser)
        {
        }

        public string MatchID;
        public bool IsLoadedGame { get; set; }
        public DateTime LastRefreshTime { get; set; }
        public bool IsLadder { get; set; }
        public bool Locked { get; set; }
        public int TunnelPort { get; set; }
        public string TunnelAddress { get; set; }
        public string GameVersion { get; set; }
        public int MaxPlayers { get; set; }
        public string GameRoomChannelName { get; set; }
        public string GameRoomDisplayName { get; set; }
        public string[] PlayerNames { get; set; }
        public string MapName { get; set; }
        public string GameMode { get; set; }
        public bool IsClosed { get; set; }
        public bool IsCustomPassword { get; set; }
        public string Revision { get; set; }
    }
}