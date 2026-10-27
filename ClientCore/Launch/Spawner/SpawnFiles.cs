#nullable enable

using System.IO;

using Rampastring.Tools;

namespace ClientCore.Launch.Spawner;

/// <summary>Writes the files the spawner reads into the game directory.</summary>
public static class SpawnFiles
{
    public static FileInfo SpawnIniFile => SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

    public static FileInfo SpawnMapFile => SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNMAP_INI);

    /// <summary>Replaces spawn.ini.</summary>
    public static void WriteSpawnIni(IniFile spawnIni)
    {
        Logger.Log("Writing spawn.ini");
        spawnIni.WriteIniFile(SpawnIniFile.FullName);
    }

    /// <summary>Replaces spawnmap.ini. The map keeps its own encoding.</summary>
    public static void WriteSpawnMap(IniFile spawnMap)
    {
        Logger.Log("Writing spawnmap.ini");
        spawnMap.WriteIniFile(SpawnMapFile.FullName);
    }
}
