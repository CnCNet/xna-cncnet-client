#nullable enable

using System;
using System.Collections.Generic;

using Rampastring.Tools;

namespace ClientCore.Launch;

/// <summary>
/// A request to load a multiplayer saved game. The game is set up the way the match was
/// started, updated with the players' current network endpoints.
/// </summary>
public sealed class SavedMatchLaunchRequest : LaunchRequest
{
    /// <summary>The request the saved match was started with.</summary>
    public MatchLaunchRequest Match { get; init; } = new();

    /// <summary>The saved game's file name.</summary>
    public string SaveGameName { get; init; } = string.Empty;

    /// <summary>The local player's game port.</summary>
    public int LocalPort { get; init; }

    /// <summary>
    /// The players in the loading lobby, local player first. Recorded players are matched to them by name.
    /// </summary>
    public IReadOnlyList<PlayerEndpoint> Players { get; init; } = [];

    /// <summary>Where the game sends its traffic, or null to keep what the saved match says.</summary>
    public TunnelEndpoint? Tunnel { get; init; }
}

/// <summary>Where a player's game can be reached.</summary>
public sealed record PlayerEndpoint(string Name, string IpAddress, int Port);

/// <summary>A request to play back a replay.</summary>
public sealed class ReplayLaunchRequest : LaunchRequest
{
    /// <summary>The spawn.ini the replay was recorded with. It is not modified.</summary>
    public IniFile RecordedSpawnIni { get; init; } = new();

    /// <summary>The spawnmap.ini the replay was recorded with, or empty when the game loaded its own map.</summary>
    public byte[] RecordedSpawnMap { get; init; } = Array.Empty<byte>();

    /// <summary>The replay file's path, relative to the game directory.</summary>
    public string ReplayFile { get; init; } = string.Empty;

    /// <summary>The spawn.ini player slot to watch from: 0 for [Settings], N for [OtherN].</summary>
    public int ViewPlayer { get; init; }

    /// <summary>The loading screen to show, or null to keep the recorded one.</summary>
    public string? CustomLoadScreen { get; init; }

    /// <summary>Playback speed in frames per second.</summary>
    public int PlaybackSpeed { get; init; }

    public bool ShroudEnabled { get; init; }

    /// <summary>Whether the camera moves freely instead of following the recorded camera.</summary>
    public bool FreeCamera { get; init; }

    public bool ShowSelections { get; init; }

    /// <summary>Whether to watch as a spectator instead of a player.</summary>
    public bool Spectator { get; init; }

    public bool ShowChatAndBeacons { get; init; }

    public int KeyframeInterval { get; init; }

    public int KeyframeStorageLimitMB { get; init; }
}
