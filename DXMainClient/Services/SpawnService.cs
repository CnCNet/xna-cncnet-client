using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ClientCore;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Rampastring.Tools;

namespace DTAClient.Services;

public class SpawnService
{
    private readonly MapLoader mapLoader;

    public SpawnService(
        MapLoader mapLoader
    )
    {
        this.mapLoader = mapLoader;
    }

    public void WriteSpawnInfo(QmSpawn spawn)
    {
        IniFile spawnIni = GetSpawnIniFile();

        AddSpawnSettingsSection(spawn, spawnIni);
        AddSpawnOtherSections(spawn, spawnIni);
        AddSpawnLocationsSection(spawn, spawnIni);
        AddSpawnTunnelSection(spawn, spawnIni);

        spawnIni.WriteIniFile();

        WriteSpawnMapIni(spawn.Settings.MapHash);
    }

    public void WriteSpawnMapIni(string mapHash)
    {
        Map map = mapLoader.GetMapForSHA(mapHash);
        IniFile mapIni = map.GetMapIni();
        mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SPAWNMAP_INI));
    }

    private static void AddSpawnSettingsSection(QmSpawn spawn, IniFile spawnIni)
    {
        var settings = new IniSection("Settings");
        settings.SetStringValue("Scenario", "spawnmap.ini");
        settings.SetStringValue("QuickMatch", "Yes");

        foreach (PropertyInfo prop in spawn.Settings.GetType().GetProperties())
            settings.SetStringValue(prop.Name, prop.GetValue(spawn.Settings).ToString());

        spawnIni.AddSection(settings);
    }

    private static void AddSpawnOtherSections(QmSpawn spawn, IniFile spawnIni)
    {
        for (int i = 0; i < spawn.Others.Count; i++)
        {
            // Headers for OTHER# sections are 1-based index
            var otherSection = new IniSection($"Other{i + 1}");
            QmSpawnOther other = spawn.Others[i];

            foreach (PropertyInfo otherProp in other.GetType().GetProperties())
                otherSection.SetStringValue(otherProp.Name, otherProp.GetValue(other).ToString());

            spawnIni.AddSection(otherSection);
        }
    }

    private static void AddSpawnLocationsSection(QmSpawn spawn, IniFile spawnIni)
    {
        var spawnLocationsSection = new IniSection("SpawnLocations");
        foreach (KeyValuePair<string, int> spawnLocation in spawn.SpawnLocations)
            spawnLocationsSection.SetStringValue(spawnLocation.Key, spawnLocation.Value.ToString());

        spawnIni.AddSection(spawnLocationsSection);
    }

    private static void AddSpawnTunnelSection(QmSpawn spawn, IniFile spawnIni)
    {
        var tunnel = new IniSection("Tunnel");
        tunnel.SetStringValue("Ip", "52.232.96.199");
        tunnel.SetIntValue("Port", 50001);
        spawnIni.AddSection(tunnel);
    }

    private static IniFile GetSpawnIniFile()
    {
        FileInfo spawnerMapSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);
        spawnerMapSettingsFile.Delete();
        return new IniFile(spawnerMapSettingsFile.FullName);
    }
}