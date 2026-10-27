#nullable enable

using Rampastring.Tools;

namespace ClientCore.Launch;

/// <summary>
/// A request to launch the game. Each subclass is one kind of launch and carries
/// the resolved data that kind needs; nothing in a request depends on how the
/// game is told about it.
/// </summary>
public abstract class LaunchRequest
{
    /// <summary>The map the game starts on, or null when this launch provides no map of its own.</summary>
    public IniFile? SpawnMap { get; init; }
}
