#nullable enable

using System.Collections.Generic;

namespace ClientCore.Launch;

/// <summary>
/// A request to start a new skirmish, LAN or CnCNet match. Sides, colors and starting
/// locations are already randomized, so every player's client builds the same request.
/// </summary>
public sealed class MatchLaunchRequest : LaunchRequest
{
    public string LocalPlayerName { get; init; } = string.Empty;

    /// <summary>Human players, including the local player and spectators, in lobby order.</summary>
    public IReadOnlyList<HumanHouse> Humans { get; init; } = [];

    /// <summary>AI players in lobby order.</summary>
    public IReadOnlyList<AIHouse> AIs { get; init; } = [];

    /// <summary>
    /// The game color index of every multiplayer color, sorted ascending. The game assigns
    /// human players to houses in this color order.
    /// </summary>
    public IReadOnlyList<int> ColorOrder { get; init; } = [];

    public int Seed { get; init; }

    public string UIGameMode { get; init; } = string.Empty;

    public string UIMapName { get; init; } = string.Empty;

    /// <summary>The official map's ID, used to translate the map name. Null for custom maps.</summary>
    public string? MapID { get; init; }

    public string CustomLoadScreen { get; init; } = string.Empty;

    public bool CoachMode { get; init; }

    /// <summary>Whether to turn off the game's automatic surrender, as co-op games do.</summary>
    public bool DisableAutoSurrender { get; init; }

    /// <summary>Network settings, or null for a skirmish.</summary>
    public MatchNetwork? Network { get; init; }

    /// <summary>Mod-defined settings, applied in order.</summary>
    public IReadOnlyList<SpawnOptionLayer> OptionLayers { get; init; } = [];

    public MatchMapSettings MapSettings { get; init; } = new();

    /// <summary>The houses a co-op map adds, or null when the map is not a co-op map.</summary>
    public CoopSetup? Coop { get; init; }

    public string MapSHA1 { get; init; } = string.Empty;

    /// <summary>The lobby's broadcast game option values, kept so a saved game can restore them.</summary>
    public string BroadcastedGameOptionValues { get; init; } = string.Empty;

    /// <summary>Replay recording settings, or null when the game does not support replays.</summary>
    public ReplayRecording? Recording { get; init; }
}

/// <summary>A human player's resolved house.</summary>
public sealed record HumanHouse
{
    public string Name { get; init; } = string.Empty;

    /// <summary>The side index the game's rules use.</summary>
    public int InternalSideIndex { get; init; }

    /// <summary>The game color index.</summary>
    public int ColorIndex { get; init; }

    /// <summary>The 0-based starting waypoint, -1 to let the game decide, or 90 for spectators.</summary>
    public int StartingWaypoint { get; init; } = -1;

    /// <summary>
    /// The waypoint given to a player who shares a starting location with another player,
    /// or null when the player does not share one.
    /// </summary>
    public int? StackedStartingWaypoint { get; init; }

    /// <summary>The alliance team, 1 to 4, or 0 for none.</summary>
    public int TeamId { get; init; }

    public bool IsSpectator { get; init; }

    public string IpAddress { get; init; } = string.Empty;

    public int Port { get; init; }
}

/// <summary>An AI player's resolved house.</summary>
public sealed record AIHouse
{
    /// <summary>The side index the game's rules use.</summary>
    public int InternalSideIndex { get; init; }

    /// <summary>The game color index.</summary>
    public int ColorIndex { get; init; }

    /// <summary>The 0-based starting waypoint, or -1 to let the game decide.</summary>
    public int StartingWaypoint { get; init; } = -1;

    /// <summary>
    /// The waypoint given to an AI that shares a starting location with another player,
    /// or null when it does not share one.
    /// </summary>
    public int? StackedStartingWaypoint { get; init; }

    /// <summary>The alliance team, 1 to 4, or 0 for none.</summary>
    public int TeamId { get; init; }

    /// <summary>The game's handicap level for this AI.</summary>
    public int HandicapLevel { get; init; }
}

/// <summary>Network settings for a LAN or CnCNet match.</summary>
public sealed record MatchNetwork
{
    public int FrameSendRate { get; init; }

    /// <summary>The MaxAhead value, or 0 to let the game calculate it.</summary>
    public int MaxAhead { get; init; }

    public int Protocol { get; init; }

    /// <summary>The session settings, or null when the local player is missing from the lobby.</summary>
    public MatchSession? Session { get; init; }
}

/// <summary>How the local player's game joins the match.</summary>
public abstract record MatchSession
{
    public int GameID { get; init; }

    public bool IsHost { get; init; }

    /// <summary>The local player's game port.</summary>
    public int Port { get; init; }
}

/// <summary>A match whose games talk to each other directly over the local network.</summary>
public sealed record LanMatchSession : MatchSession;

/// <summary>A match whose games talk through a CnCNet tunnel.</summary>
public sealed record TunnelMatchSession : MatchSession
{
    /// <summary>Where the game sends its traffic, or null when no tunnel is selected.</summary>
    public TunnelEndpoint? Tunnel { get; init; }
}

public sealed record TunnelEndpoint(string Address, int Port);

/// <summary>Settings a map forces regardless of the lobby's options.</summary>
public sealed record MatchMapSettings
{
    public int? Credits { get; init; }

    public int? UnitCount { get; init; }

    /// <summary>The color of the map's neutral house, or null to leave it unchanged.</summary>
    public int? NeutralHouseColor { get; init; }

    /// <summary>The color of the map's special house, or null to leave it unchanged.</summary>
    public int? SpecialHouseColor { get; init; }

    public bool? Bases { get; init; }
}

/// <summary>The extra houses of a co-op map.</summary>
public sealed record CoopSetup
{
    /// <summary>The handicap level the extra houses use.</summary>
    public int DifficultyLevel { get; init; }

    /// <summary>Houses allied with the players.</summary>
    public IReadOnlyList<CoopHouse> AllyHouses { get; init; } = [];

    /// <summary>Houses the players fight.</summary>
    public IReadOnlyList<CoopHouse> EnemyHouses { get; init; } = [];
}

/// <summary>A co-op map house. Its side is already resolved if the map asked for a random one.</summary>
public sealed record CoopHouse(int Side, int Color, int StartingLocation);

/// <summary>Replay recording settings for a launch.</summary>
public sealed record ReplayRecording
{
    public bool Enabled { get; init; }

    public string GamePackageVersion { get; init; } = string.Empty;

    /// <summary>The replay file's path, relative to the game directory.</summary>
    public string FileOut { get; init; } = string.Empty;

    /// <summary>"path|hash" entries for the game files the replay depends on, in order.</summary>
    public IReadOnlyList<string> FileHashes { get; init; } = [];
}
