#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.Matchmaking
{
    public class MatchmakingModeInfo
    {
        public string Id { get; set; } = string.Empty;
        public string UIName { get; set; } = string.Empty;
        public int PlayerCount { get; set; } = 2;
        public bool AssignTeams { get; set; }
        public List<string> AlliedColors { get; } = new List<string>();
        public List<string> SovietColors { get; } = new List<string>();
        public Dictionary<string, bool> ForceCheckboxes { get; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ForceDropdowns { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public List<string> Maps { get; } = new List<string>();
        public List<string> MapDisplayNames { get; } = new List<string>();

        public string Description
        {
            get
            {
                if (PlayerCount == 2)
                    return "1 vs 1 (2 Players) - 1 Allies vs 1 Soviet (Random Countries)";
                if (PlayerCount == 4)
                    return "2 vs 2 (4 Players) - Each Team: 1 Allies + 1 Soviet";
                if (PlayerCount == 6)
                    return "3 vs 3 (6 Players) - Team A: 3 Allies vs Team B: 3 Soviets";
                if (PlayerCount == 8)
                {
                    if (Id.Equals("4v4", StringComparison.OrdinalIgnoreCase))
                        return "4 vs 4 (8 Players) - Each Team: 2 Allies + 2 Soviet";
                    return "2v2v2v2 (8 Players) - 4 Teams: Each 1 Allies + 1 Soviet";
                }

                return $"{UIName} ({PlayerCount} Players)";
            }
        }
    }

    public class MatchmakingConfig
    {
        private static MatchmakingConfig? instance;
        public static MatchmakingConfig Instance => instance ??= new MatchmakingConfig();

        public bool Enabled { get; private set; }
        public string ApiUrl { get; private set; } = "http://localhost:3000";
        public string LadderBase { get; private set; } = "sim-ra2";
        public bool Casual { get; private set; } = true;
        public int DefaultSide { get; private set; }

        public List<MatchmakingModeInfo> Modes { get; } = new List<MatchmakingModeInfo>();
        public MatchmakingModeInfo? CurrentMode { get; set; }

        public MatchmakingConfig()
        {
            Initialize();
        }

        public void Initialize()
        {
            Modes.Clear();

            string iniPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "Matchmaking.ini");
            FileInfo fileInfo = SafePath.GetFile(iniPath);

            if (!fileInfo.Exists)
            {
                Enabled = false;
                Logger.Log($"[MatchmakingConfig] Config not found at {iniPath}. Matchmaking disabled.");
                return;
            }

            IniFile ini = new IniFile(iniPath);

            Enabled = ini.GetBooleanValue("Matchmaking", "Enabled", true);
            ApiUrl = ini.GetStringValue("Matchmaking", "ApiUrl", "http://localhost:3000");
            LadderBase = ini.GetStringValue("Matchmaking", "Ladder", "sim-ra2");
            Casual = ini.GetBooleanValue("Matchmaking", "Casual", true);
            DefaultSide = ini.GetIntValue("Matchmaking", "DefaultSide", 0);

            // Read [MatchmakingModes]
            IniSection? modesSection = ini.GetSection("MatchmakingModes");
            if (modesSection != null)
            {
                foreach (var kvp in modesSection.Keys)
                {
                    string modeId = kvp.Value.Trim();
                    if (string.IsNullOrEmpty(modeId))
                        continue;

                    IniSection? modeSec = ini.GetSection(modeId);
                    if (modeSec == null)
                        continue;

                    var modeInfo = new MatchmakingModeInfo
                    {
                        Id = modeId,
                        UIName = modeSec.GetStringValue("UIName", modeId),
                        PlayerCount = modeSec.GetIntValue("PlayerCount", 2),
                        AssignTeams = modeSec.GetBooleanValue("AssignTeams", false)
                    };

                    string alliedColors = modeSec.GetStringValue("AlliedColors", string.Empty);
                    if (!string.IsNullOrEmpty(alliedColors))
                    {
                        foreach (string c in alliedColors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            modeInfo.AlliedColors.Add(c.Trim());
                    }

                    string sovietColors = modeSec.GetStringValue("SovietColors", string.Empty);
                    if (!string.IsNullOrEmpty(sovietColors))
                    {
                        foreach (string c in sovietColors.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            modeInfo.SovietColors.Add(c.Trim());
                    }

                    // Read [Mode_ForceCheckboxes]
                    IniSection? chkSec = ini.GetSection($"{modeId}_ForceCheckboxes");
                    if (chkSec != null)
                    {
                        foreach (var opt in chkSec.Keys)
                        {
                            if (bool.TryParse(opt.Value, out bool val))
                                modeInfo.ForceCheckboxes[opt.Key] = val;
                        }
                    }

                    // Read [Mode_ForceDropdowns]
                    IniSection? drpSec = ini.GetSection($"{modeId}_ForceDropdowns");
                    if (drpSec != null)
                    {
                        foreach (var opt in drpSec.Keys)
                        {
                            modeInfo.ForceDropdowns[opt.Key] = opt.Value;
                        }
                    }

                    Modes.Add(modeInfo);
                }
            }

            // Load maps from MatchmakingMaps.ini if present
            string mapsIniPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "MatchmakingMaps.ini");
            FileInfo mapsFileInfo = SafePath.GetFile(mapsIniPath);
            if (mapsFileInfo.Exists)
            {
                IniFile mapsIni = new IniFile(mapsIniPath);
                foreach (var mode in Modes)
                {
                    IniSection? sec = mapsIni.GetSection(mode.UIName);
                    if (sec != null)
                    {
                        foreach (var kvp in sec.Keys)
                        {
                            mode.Maps.Add(kvp.Value);

                            string raw = kvp.Value;
                            int semi = raw.IndexOf(';');
                            if (semi >= 0 && semi < raw.Length - 1)
                            {
                                string name = raw.Substring(semi + 1).Trim();
                                if (!string.IsNullOrEmpty(name))
                                    mode.MapDisplayNames.Add(name);
                            }
                        }
                    }
                }
            }

            if (Modes.Count > 0)
            {
                CurrentMode = Modes[0];
            }

            Logger.Log($"[MatchmakingConfig] Loaded {Modes.Count} matchmaking modes. Active mode: {CurrentMode?.UIName ?? "None"}");
        }

        public string GetLadderForCurrentMode()
        {
            if (CurrentMode == null || CurrentMode.Id.Equals("1v1", StringComparison.OrdinalIgnoreCase))
            {
                return LadderBase;
            }

            return $"{LadderBase}-{CurrentMode.Id.ToLowerInvariant()}";
        }

        public string GetLadderForMode(MatchmakingModeInfo mode)
        {
            if (mode == null || mode.Id.Equals("1v1", StringComparison.OrdinalIgnoreCase))
            {
                return LadderBase;
            }

            return $"{LadderBase}-{mode.Id.ToLowerInvariant()}";
        }
    }
}
