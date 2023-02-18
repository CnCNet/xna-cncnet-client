using System;
using System.IO;
using System.Threading.Tasks;

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
        public int Losses { get; set; }
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

        public async ValueTask WriteAsync(Stream stream)
        {
            await stream.WriteIntAsync(Economy).ConfigureAwait(false);
            // 1 byte for IsAI
            await stream.WriteBoolAsync(IsAI).ConfigureAwait(false);
            // 1 byte for IsLocalPlayer
            await stream.WriteBoolAsync(IsLocalPlayer).ConfigureAwait(false);
            // 4 bytes for kills
            await stream.WriteAsync(BitConverter.GetBytes(Kills), 0, 4).ConfigureAwait(false);
            // 4 bytes for losses
            await stream.WriteAsync(BitConverter.GetBytes(Losses), 0, 4).ConfigureAwait(false);
            // Name takes 32 bytes
            await stream.WriteStringAsync(Name, 32).ConfigureAwait(false);
            // 1 byte for SawEnd
            await stream.WriteBoolAsync(SawEnd).ConfigureAwait(false);
            // 4 bytes for Score
            await stream.WriteIntAsync(Score).ConfigureAwait(false);
            // 1 byte for Side
            await stream.WriteAsync(new[] { Convert.ToByte(Side) }, 0, 1).ConfigureAwait(false);
            // 1 byte for Team
            await stream.WriteAsync(new[] { Convert.ToByte(Team) }, 0, 1).ConfigureAwait(false);
            // 1 byte color Color
            await stream.WriteAsync(new[] { Convert.ToByte(Color) }, 0, 1).ConfigureAwait(false);
            // 1 byte for WasSpectator
            await stream.WriteBoolAsync(WasSpectator).ConfigureAwait(false);
            // 1 byte for Won
            await stream.WriteBoolAsync(Won).ConfigureAwait(false);
            // 1 byte for AI level
            await stream.WriteAsync(new[] { Convert.ToByte(AILevel) }, 0, 1).ConfigureAwait(false);
        }
    }
}