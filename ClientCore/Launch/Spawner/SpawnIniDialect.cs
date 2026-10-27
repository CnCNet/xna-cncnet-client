#nullable enable

namespace ClientCore.Launch.Spawner;

/// <summary>
/// The ways spawn.ini's structure differs between games. Mod-defined settings never
/// belong here; they are passed through as option layers.
/// </summary>
public sealed class SpawnIniDialect
{
    private readonly int allianceHouseIdOffset;
    private readonly AddonFlag addonFlag;

    private enum AddonFlag
    {
        None,

        /// <summary>Ra2Mode, which is true when the expansion is NOT used.</summary>
        Ra2Mode,

        /// <summary>Firestorm, which is true when the expansion is used.</summary>
        Firestorm,
    }

    private SpawnIniDialect(int allianceHouseIdOffset, AddonFlag addonFlag)
    {
        this.allianceHouseIdOffset = allianceHouseIdOffset;
        this.addonFlag = addonFlag;
    }

    public static SpawnIniDialect ForClientType(ClientType clientType) => clientType switch
    {
        // Red Alert's multiplayer house IDs are shifted by +12 compared with the other games.
        ClientType.RA => new SpawnIniDialect(11, AddonFlag.None),
        ClientType.YR or ClientType.Ares => new SpawnIniDialect(-1, AddonFlag.Ra2Mode),
        ClientType.TS => new SpawnIniDialect(-1, AddonFlag.Firestorm),
        _ => new SpawnIniDialect(-1, AddonFlag.None),
    };

    /// <summary>Converts a 1-based MultiN house number into the house ID alliance keys use.</summary>
    public int GetAllianceHouseId(int multiId) => multiId + allianceHouseIdOffset;

    /// <summary>
    /// Gets the [Settings] key and value that tell the game whether to use its expansion.
    /// Returns false for games that have no such key.
    /// </summary>
    public bool TryGetAddonSetting(bool requiresAddon, out string key, out string value)
    {
        switch (addonFlag)
        {
            case AddonFlag.Ra2Mode:
                key = "Ra2Mode";
                value = SpawnOptionLayer.FormatBoolean(!requiresAddon);
                return true;
            case AddonFlag.Firestorm:
                key = "Firestorm";
                value = SpawnOptionLayer.FormatBoolean(requiresAddon);
                return true;
            default:
                key = string.Empty;
                value = string.Empty;
                return false;
        }
    }
}
