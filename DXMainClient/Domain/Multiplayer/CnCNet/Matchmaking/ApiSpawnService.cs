#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using DTAClient.Domain.Multiplayer;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.Matchmaking
{
    public class ApiSpawnService
    {
        private readonly MapLoader mapLoader;
        private readonly TunnelHandler? tunnelHandler;

        public ApiSpawnService(MapLoader mapLoader, TunnelHandler? tunnelHandler)
        {
            this.mapLoader = mapLoader;
            this.tunnelHandler = tunnelHandler;
        }

        public bool WriteSpawnFiles(QmMatchResponse spawnResponse)
        {
            if (spawnResponse.Spawn == null)
            {
                Logger.Log("[ApiSpawnService] Error: Spawn payload is null.");

                return false;
            }

            try
            {
                string spawnIniPath = SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);
                FileInfo spawnFileInfo = SafePath.GetFile(spawnIniPath);

                if (spawnFileInfo.Exists)
                {
                    spawnFileInfo.Delete();
                }

                IniFile spawnIni = new IniFile(spawnIniPath);

                foreach (KeyValuePair<string, Dictionary<string, object>> section in spawnResponse.Spawn)
                {
                    foreach (KeyValuePair<string, object> kvp in section.Value)
                    {
                        if (kvp.Value != null)
                        {
                            spawnIni.SetStringValue(section.Key, kvp.Key, kvp.Value.ToString() ?? string.Empty);
                        }
                    }
                }

                spawnIni.SetStringValue("Settings", "Scenario", ProgramConstants.SPAWNMAP_INI);
                spawnIni.SetStringValue("Settings", "QuickMatch", "Yes");

                int myIndex = spawnIni.GetIntValue("Settings", "MyIndex", 0);
                spawnIni.SetStringValue("Settings", "Host", myIndex == 0 ? "Yes" : "No");

                // Dynamically apply forced settings from active mode in Matchmaking.ini
                MatchmakingModeInfo? activeMode = MatchmakingConfig.Instance.CurrentMode;
                if (activeMode != null)
                {
                    foreach (var kvp in activeMode.ForceCheckboxes)
                    {
                        string settingName = kvp.Key.StartsWith("chk", StringComparison.OrdinalIgnoreCase)
                            ? kvp.Key.Substring(3)
                            : kvp.Key;
                        if (!spawnIni.KeyExists("Settings", settingName))
                            spawnIni.SetBooleanValue("Settings", settingName, kvp.Value);
                    }

                    foreach (var kvp in activeMode.ForceDropdowns)
                    {
                        string settingName = kvp.Key.StartsWith("cmb", StringComparison.OrdinalIgnoreCase)
                            ? kvp.Key.Substring(3)
                            : kvp.Key;
                        if (!spawnIni.KeyExists("Settings", settingName))
                            spawnIni.SetStringValue("Settings", settingName, kvp.Value);
                    }
                }

                int myPort = spawnIni.GetIntValue("Settings", "Port", 0);
                if (myPort <= 0)
                {
                    spawnIni.SetIntValue("Settings", "Port", 50000 + myIndex);
                }

                if (!spawnIni.SectionExists("Tunnel"))
                {
                    CnCNetTunnel? tunnel = tunnelHandler?.CurrentTunnel
                        ?? tunnelHandler?.Tunnels?.FirstOrDefault(t => t.Official && t.Port == 50000)
                        ?? tunnelHandler?.Tunnels?.FirstOrDefault(t => t.Port == 50000);

                    if (tunnel != null)
                    {
                        spawnIni.SetStringValue("Tunnel", "Ip", tunnel.Address);
                        spawnIni.SetIntValue("Tunnel", "Port", tunnel.Port);
                        Logger.Log($"[ApiSpawnService] Selected tunnel for match: {tunnel.Address}:{tunnel.Port} ({tunnel.Name})");
                    }
                    else
                    {
                        Logger.Log("[ApiSpawnService] WARNING: No V2 tunnel available for matchmaking!");
                    }
                }

                // Ensure other players have valid Host, Port, and non-empty Ip (0.0.0.0 routes via tunnel)
                for (int i = 1; i <= 8; i++)
                {
                    string otherSection = "Other" + i;
                    if (spawnIni.SectionExists(otherSection))
                    {
                        int otherIndex = spawnIni.GetIntValue(otherSection, "MyIndex", i);
                        spawnIni.SetStringValue(otherSection, "Host", otherIndex == 0 ? "Yes" : "No");

                        string otherIp = spawnIni.GetStringValue(otherSection, "Ip", string.Empty);
                        if (string.IsNullOrWhiteSpace(otherIp))
                        {
                            spawnIni.SetStringValue(otherSection, "Ip", "0.0.0.0");
                        }

                        int otherPort = spawnIni.GetIntValue(otherSection, "Port", 0);
                        if (otherPort <= 0)
                        {
                            spawnIni.SetIntValue(otherSection, "Port", 50000 + otherIndex);
                        }
                    }
                }

                spawnIni.WriteIniFile();
                Logger.Log($"[ApiSpawnService] Wrote {ProgramConstants.SPAWNER_SETTINGS} successfully.");

                string? mapHash = spawnIni.GetStringValue("Settings", "MapHash", null);
                string? uiMapName = spawnIni.GetStringValue("Settings", "UIMapName", null);

                WriteSpawnMapIni(mapHash, uiMapName, spawnResponse.SpawnMap);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[ApiSpawnService] Exception writing spawn files: {ex.Message}");

                return false;
            }
        }

        private void WriteSpawnMapIni(string? mapHash, string? uiMapName, Dictionary<string, Dictionary<string, object>>? extraSpawnMap)
        {
            Map? map = null;

            if (!string.IsNullOrEmpty(mapHash))
            {
                map = mapLoader.GameModes.SelectMany(gm => gm.Maps)
                    .FirstOrDefault(m => string.Equals(m.SHA1, mapHash, StringComparison.OrdinalIgnoreCase));
            }

            if (map == null && !string.IsNullOrEmpty(uiMapName))
            {
                map = mapLoader.GameModes.SelectMany(gm => gm.Maps)
                    .FirstOrDefault(m => string.Equals(m.Name, uiMapName, StringComparison.OrdinalIgnoreCase) ||
                                         m.Name.EndsWith(uiMapName, StringComparison.OrdinalIgnoreCase) ||
                                         m.Name.Contains(uiMapName));
            }

            if (map == null)
            {
                Logger.Log($"[ApiSpawnService] WARNING: Map not found locally for Hash='{mapHash}', Name='{uiMapName}'. Defaulting to first available map.");
                map = mapLoader.GameModes.SelectMany(gm => gm.Maps).FirstOrDefault();

                if (map == null)
                {
                    Logger.Log("[ApiSpawnService] ERROR: No maps loaded in map loader!");

                    return;
                }
            }

            string spawnMapPath = SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SPAWNMAP_INI);
            FileInfo spawnMapFile = SafePath.GetFile(spawnMapPath);

            if (spawnMapFile.Exists)
            {
                spawnMapFile.Delete();
            }

            IniFile mapIni = map.GetMapIni();

            if (extraSpawnMap != null)
            {
                foreach (KeyValuePair<string, Dictionary<string, object>> section in extraSpawnMap)
                {
                    foreach (KeyValuePair<string, object> kvp in section.Value)
                    {
                        if (kvp.Value != null)
                        {
                            mapIni.SetStringValue(section.Key, kvp.Key, kvp.Value.ToString() ?? string.Empty);
                        }
                    }
                }
            }

            mapIni.WriteIniFile(spawnMapPath);
            Logger.Log($"[ApiSpawnService] Wrote {ProgramConstants.SPAWNMAP_INI} successfully for map: {map.Name}.");
        }
    }
}
