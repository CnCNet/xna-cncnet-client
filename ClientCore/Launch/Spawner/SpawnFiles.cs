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

    /// <summary>
    /// Replaces spawnmap.ini with a placeholder map, for launches where the game
    /// loads its map from somewhere else, such as a saved game.
    /// </summary>
    public static void WriteStubSpawnMap()
    {
        FileInfo spawnMapFile = SpawnMapFile;

        if (spawnMapFile.Exists)
            spawnMapFile.Delete();

        using var spawnMapStreamWriter = new StreamWriter(spawnMapFile.FullName);
        spawnMapStreamWriter.WriteLine("[Map]");
        spawnMapStreamWriter.WriteLine("Size=0,0,50,50");
        spawnMapStreamWriter.WriteLine("LocalSize=0,0,50,50");
        spawnMapStreamWriter.WriteLine();
    }
}
