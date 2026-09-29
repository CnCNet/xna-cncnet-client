#nullable enable

namespace DTAClient.Domain;

/// <summary>A human player from a replay's embedded spawn.ini.</summary>
public sealed class ReplayPlayer
{
    public ReplayPlayer(int spawnIniIndex, string name, int sideIndex, bool isSpectator)
    {
        SpawnIniIndex = spawnIniIndex;
        Name = name;
        SideIndex = sideIndex;
        IsSpectator = isSpectator;
    }

    /// <summary>Player slot used by the spawner's ReplayViewPlayer setting.</summary>
    public int SpawnIniIndex { get; }

    public string Name { get; }

    /// <summary>Side index used for the loading screen, or -1 when absent.</summary>
    public int SideIndex { get; }

    public bool IsSpectator { get; }

    /// <summary>Whether this player created the recording.</summary>
    public bool IsRecorder => SpawnIniIndex == 0;
}