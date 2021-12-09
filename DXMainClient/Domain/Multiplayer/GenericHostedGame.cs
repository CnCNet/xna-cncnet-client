using ClientCore.CnCNet5;
using System;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// A base class for hosted games.
    /// CnCNet and LAN games derive from this.
    /// </summary>
    public abstract class GenericHostedGame: IEquatable<GenericHostedGame>
    {
        public string RoomName { get; set; }
        public bool Incompatible { get; set; }
        public bool Locked { get; set; }
        public bool IsLoadedGame { get; set; }
        public bool Passworded { get; set; }
        public CnCNetGame Game { get; set; }
        public string GameMode { get; set; }
        public string Map { get; set; }
        public string GameVersion { get; set; }
        public string HostName { get; set; }
        public string[] Players { get; set; }

        int _maxPlayers = 8;

        public int MaxPlayers
        {
            get { return _maxPlayers; }
            set { _maxPlayers = value; }
        }

        public abstract int Ping
        {
            get;
        }

        public DateTime LastRefreshTime { get; set; }
        
        public virtual bool Equals(GenericHostedGame other) => 
            string.Equals(RoomName, other?.RoomName, StringComparison.InvariantCultureIgnoreCase);
    }
}
