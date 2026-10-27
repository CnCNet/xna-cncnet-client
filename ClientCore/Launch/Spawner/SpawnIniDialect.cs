#nullable enable

namespace ClientCore.Launch.Spawner;

/// <summary>
/// The ways spawn.ini's structure differs between games. Mod-defined settings never
/// belong here; they are passed through as option layers.
/// </summary>
public sealed class SpawnIniDialect
{
    private readonly int allianceHouseIdOffset;

    private SpawnIniDialect(int allianceHouseIdOffset)
    {
        this.allianceHouseIdOffset = allianceHouseIdOffset;
    }

    public static SpawnIniDialect ForClientType(ClientType clientType) => clientType switch
    {
        // Red Alert's multiplayer house IDs are shifted by +12 compared with the other games.
        ClientType.RA => new SpawnIniDialect(11),
        _ => new SpawnIniDialect(-1),
    };

    /// <summary>Converts a 1-based MultiN house number into the house ID alliance keys use.</summary>
    public int GetAllianceHouseId(int multiId) => multiId + allianceHouseIdOffset;
}
