using Rampastring.Tools;
using System;

namespace DTAClient.Domain.Multiplayer
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
        public bool AutoReady { get; set; }
        public bool IsAI { get; set; }

        public bool IsInGame { get; set; }
        public virtual string IPAddress { get; set; } = "0.0.0.0";
        public int Port { get; set; }

        /// <summary>
        /// Whether the file hash information is received from the player, regardless of whether it is consistent with the one calculated by this client.
        /// </summary>
        public bool HashReceived { get; set; }

        public int Index { get; set; }

        public int Ping { get; set; } = -1;

        /// <summary>
        /// The difficulty level of an AI player for in-client purposes.
        /// Logical increasing scale, like in the vanilla Tiberian Sun UI.
        /// 2 = Hard, 1 = Medium, 0 = Easy.
        /// </summary>
        public int AILevel { get; set; }

        /// <summary>
        /// The AI level of the AI for the [HouseHandicaps] section in spawn.ini.
        /// 2 = Easy, 1 = Medium, 0 = Hard.
        /// </summary>
        public int HouseHandicapAILevel
        {
            get { return Math.Abs(AILevel - 2); }
        }

        public override string ToString()
        {
            var sb = new ExtendedStringBuilder(true, ',');
            sb.Append(Name);
            sb.Append(SideId);
            sb.Append(StartingLocation);
            sb.Append(ColorId);
            sb.Append(TeamId);
            sb.Append(AILevel);
            sb.Append(IsAI.ToString());
            sb.Append(Index);
            return sb.ToString();
        }

        /// <summary>
        /// Creates a PlayerInfo instance from a string in a format that matches the 
        /// string given by the ToString() method.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>A PlayerInfo instance, or null if the string format was invalid.</returns>
        public static PlayerInfo FromString(string str)
        {
            var values = str.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != 8)
                return null;

            var pInfo = new PlayerInfo();

            pInfo.Name = values[0];
            pInfo.SideId = Conversions.IntFromString(values[1], 0);
            pInfo.StartingLocation = Conversions.IntFromString(values[2], 0);
            pInfo.ColorId = Conversions.IntFromString(values[3], 0);
            pInfo.TeamId = Conversions.IntFromString(values[4], 0);
            pInfo.AILevel = Conversions.IntFromString(values[5], 0);
            pInfo.IsAI = Conversions.BooleanFromString(values[6], true);
            pInfo.Index = Conversions.IntFromString(values[7], 0);

            return pInfo;
        }
    }
}
