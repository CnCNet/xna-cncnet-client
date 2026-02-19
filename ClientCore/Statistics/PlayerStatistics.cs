using System;
using System.IO;

namespace ClientCore.Statistics
{
    public class PlayerStatistics
    {
        public PlayerStatistics() { }

        public PlayerStatistics(string name, bool isLocal, bool isAi, bool isSpectator, 
            int side, int team, int color, int aiLevel)
        {
            Name = name;
            IsLocalPlayer = isLocal;
            IsAI = isAi;
            WasSpectator = isSpectator;
            Side = side;
            Team = team;
            Color = color;
            AILevel = aiLevel;
        }

        public string Name { get; set; }
        public int Kills { get; set; }
        public int Losses {get; set;}
        public int Economy { get; set; }
        public int Score { get; set; }
        public int Side { get; set; }
        public int Team { get; set; }
        public int AILevel { get; set; }
        public bool SawEnd { get; set; }
        public bool WasSpectator { get; set; }
        public bool Won { get; set; }
        public bool IsLocalPlayer { get; set; }
        public bool IsAI { get; set; }
        public int Color { get; set; } = 255;

        public void Write(Stream stream)
        {
            stream.WriteInt(Economy);
            // 1 byte for IsAI
            stream.WriteBool(IsAI);
            // 1 byte for IsLocalPlayer
            stream.WriteBool(IsLocalPlayer);
            // 4 bytes for kills
            stream.Write(BitConverter.GetBytes(Kills), 0, 4);
            // 4 bytes for losses
            stream.Write(BitConverter.GetBytes(Losses), 0, 4);
            // Name takes 32 bytes
            stream.WriteString(Name, 32);
            // 1 byte for SawEnd
            stream.WriteBool(SawEnd);
            // 4 bytes for Score
            stream.WriteInt(Score);
            // 1 byte for Side
            stream.WriteByte(Convert.ToByte(Side));
            // 1 byte for Team
            stream.WriteByte(Convert.ToByte(Team));
            // 1 byte color Color
            stream.WriteByte(Convert.ToByte(Color));
            // 1 byte for WasSpectator
            stream.WriteBool(WasSpectator);
            // 1 byte for Won
            stream.WriteBool(Won);
            // 1 byte for AI level
            stream.WriteByte(Convert.ToByte(AILevel));
        }
    }
}
