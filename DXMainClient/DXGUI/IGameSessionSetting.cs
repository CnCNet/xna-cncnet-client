using DTAClient.Domain.Multiplayer;

using Rampastring.Tools;

namespace DTAClient.DXGUI;

// TODO split the logic between campaign/mp and clean up
public interface IGameSessionSetting
{
    /// <summary>Indicates whether this setting can affect spawn.ini.</summary>
    bool AffectsSpawnIni { get; }
    
    /// <summary>Indicates whether this setting can affect map code.</summary>
    bool AffectsMapCode { get; }
    
    /// <summary>Indicates whether this setting in its current state allows the game to be scored.</summary>
    bool AllowScoring { get; }
    
    /// <summary>Applies the associated code to the spawn.ini file.</summary>
    /// <param name="spawnIni">The spawn.ini file.</param>
    void ApplySpawnIniCode(IniFile spawnIni);
    
    /// <summary>Applies the associated code to the map INI file.</summary>
    /// <param name="mapIni">The map INI file.</param>
    /// <param name="gameMode">Currently selected gamemode, if applicable.</param>
    void ApplyMapCode(IniFile mapIni, GameMode gameMode);
}