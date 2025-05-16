#nullable enable
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer
{
    public interface IGameModeMap
    {
        List<int> AllowedStartingLocations { get; }
        int CoopDifficultyLevel { get; }
        CoopMapInfo? CoopInfo { get; }
        bool EnforceMaxPlayers { get; }
        bool ForceNoTeams { get; }
        bool ForceRandomStartLocations { get; }
        bool HumanPlayersOnly { get; }
        bool IsCoop { get; }
        int MaxPlayers { get; }
        int MinPlayers { get; }
        bool MultiplayerOnly { get; }
    }
}