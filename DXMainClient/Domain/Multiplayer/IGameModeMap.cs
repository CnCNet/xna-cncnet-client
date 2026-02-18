#nullable enable
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer
{
    public interface IGameModeMap
    {
        public List<int> AllowedStartingLocations { get; }
        public int CoopDifficultyLevel { get; }
        public CoopMapInfo? CoopInfo { get; }
        public bool EnforceMaxPlayers { get; }
        public bool ForceNoTeams { get; }
        public bool ForceRandomStartLocations { get; }
        public bool HumanPlayersOnly { get; }
        public bool IsCoop { get; }
        public int MaxPlayers { get; }
        public int MinPlayers { get; }
        public bool MultiplayerOnly { get; }
    }
}