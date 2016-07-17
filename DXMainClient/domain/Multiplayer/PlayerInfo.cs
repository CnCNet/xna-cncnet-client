using System;

namespace DTAClient.domain.Multiplayer
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
        public virtual string IPAddress { get { return ipAddress; } set { ipAddress = value; } }
        public int Port { get; set; }
        public bool Verified { get; set; }

        public int Index { get; set; }

        /// <summary>
        /// Returns the "reversed" AI level ("how it was in Tiberian Sun UI") of the AI.
        /// 2 = Hard, 1 = Medium, 0 = Easy.
        /// </summary>
        public int ReversedAILevel
        {
            get { return Math.Abs(AILevel - 2); }
        }

        /// <summary>
        /// The AI level of the AI for the [HouseHandicaps] section in spawn.ini.
        /// 2 = Easy, 1 = Medium, 0 = Hard.
        /// </summary>
        public int AILevel { get; set; }
    }
}
