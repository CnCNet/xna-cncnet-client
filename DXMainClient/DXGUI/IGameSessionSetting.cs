using DTAClient.Domain.Multiplayer;

using Rampastring.Tools;

namespace DTAClient.DXGUI;

// TODO split the logic between campaign/mp and clean up
public interface IGameSessionSetting
{
    /// <summary>Gets the name of this setting.</summary>
    string Name { get; }

    /// <summary>Indicates whether this setting can affect spawn.ini.</summary>
    bool AffectsSpawnIni { get; }

    /// <summary>Indicates whether this setting can affect map code.</summary>
    bool AffectsMapCode { get; }

    /// <summary>Indicates whether this setting in its current state allows the game to be scored.</summary>
    bool AllowScoring { get; }

    /// <summary>Indicates whether this setting should be broadcast to the lobby.</summary>
    bool BroadcastToLobby { get; }

    /// <summary>
    /// Gets or sets the value of this setting.
    /// For checkboxes: 0 = unchecked/off, 1 = checked/on.
    /// For dropdowns: the selected index.
    /// </summary>
    int Value { get; set; }

    /// <summary>Gets the spawn.ini [Settings] key and value this setting currently writes.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>False if this setting writes nothing to spawn.ini.</returns>
    bool TryGetSpawnIniOption(out string key, out string value);

    /// <summary>Applies the associated code to the map INI file.</summary>
    /// <param name="mapIni">The map INI file.</param>
    /// <param name="gameMode">Currently selected gamemode, if applicable.</param>
    void ApplyMapCode(IniFile mapIni, GameMode gameMode);
}