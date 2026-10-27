#nullable enable

using System.Collections.Generic;

namespace ClientCore.Launch;

/// <summary>A request to start a campaign or custom mission.</summary>
public sealed class MissionLaunchRequest : LaunchRequest
{
    /// <summary>
    /// The scenario the game loads: spawnmap.ini when the client provides the map,
    /// otherwise the mission's own scenario file.
    /// </summary>
    public string Scenario { get; init; } = string.Empty;

    public int CampaignID { get; init; }

    public int GameSpeed { get; init; }

    /// <summary>Whether the mission needs the game's expansion.</summary>
    public bool RequiresAddon { get; init; }

    public string CustomLoadScreen { get; init; } = string.Empty;

    public string UIMapName { get; init; } = string.Empty;

    public bool SidebarHack { get; init; }

    public int Side { get; init; }

    public bool BuildOffAlly { get; init; }

    public int HumanDifficulty { get; init; }

    public int ComputerDifficulty { get; init; }

    /// <summary>The custom mission's ID, or null for a regular campaign mission.</summary>
    public int? CustomMissionID { get; init; }

    /// <summary>The mission's own settings for the game, or null when it has none.</summary>
    public MissionSection? MissionSection { get; init; }

    /// <summary>Mod-defined settings, applied in order.</summary>
    public IReadOnlyList<SpawnOptionLayer> OptionLayers { get; init; } = [];

    /// <summary>Replay recording settings, or null when the game does not support replays.</summary>
    public ReplayRecording? Recording { get; init; }
}

/// <summary>A request to load a single-player saved game.</summary>
public sealed class SavedMissionLaunchRequest : LaunchRequest
{
    /// <summary>The saved game's file name.</summary>
    public string SaveGameName { get; init; } = string.Empty;

    public bool SidebarHack { get; init; }

    public string CustomLoadScreen { get; init; } = string.Empty;

    public int GameSpeed { get; init; }

    /// <summary>The custom mission the game was saved in, or null.</summary>
    public int? CustomMissionID { get; init; }

    /// <summary>The custom mission's own settings for the game, or null when it has none.</summary>
    public MissionSection? MissionSection { get; init; }

    /// <summary>Mod-defined settings, applied in order.</summary>
    public IReadOnlyList<SpawnOptionLayer> OptionLayers { get; init; } = [];
}

/// <summary>
/// A mission's own settings for the game, such as its loading screen,
/// written to a spawn.ini section named after the mission's scenario.
/// </summary>
public sealed record MissionSection(string Name, IReadOnlyList<SpawnOption> Settings);
