using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ClientCore.LAN
{
    /// <summary>
    /// A LAN game.
    /// </summary>
    public class LANGame
    {
        public string Revision { get; set; }
        public string GameIdentifier { get; set; }
        public string Version { get; set; }
        public int MaxPlayers { get; set; }
        public string RoomName { get; set; }
        public bool Passworded { get; set; }
        public bool Started { get; set; }
        public bool IsLoadedGame { get; set; }
        public bool Closed { get; set; }
        public string MatchID { get; set; }
        public List<string> Players = new List<string>();
        public string Host { get; set; }
        public string MapName { get; set; }
        public string GameMode { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public DateTime LastRefreshTime { get; set; }
    }
}
