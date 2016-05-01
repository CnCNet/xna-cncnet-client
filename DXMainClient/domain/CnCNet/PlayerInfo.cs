using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.CnCNet
{
    /// <summary>
    /// A player in the game lobby.
    /// </summary>
    public class PlayerInfo
    {
        public PlayerInfo() { }

        public PlayerInfo(string name)
        {
            Name = name;
        }

        public PlayerInfo(string name, int sideId, int startingLocation, int colorId, int teamId)
        {
            Name = name;
            SideId = sideId;
            StartingLocation = startingLocation;
            ColorId = colorId;
            TeamId = teamId;
        }

        public string Name { get; set; }
        public int SideId { get; set; }
        public int StartingLocation { get; set; }
        public int ColorId { get; set; }
        public int TeamId { get; set; }
        public bool Ready { get; set; }
        public bool IsAI { get; set; }
        public bool IsInGame { get; set; }
        string ipAddress = "0.0.0.0";
        public string IPAddress { get { return ipAddress; } set { ipAddress = value; } }
        public int Port { get; set; }
        public bool Verified { get; set; }
    }
}
