using ClientCore;
using ClientCore.Statistics;

using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DTAClient.Domain.Singleplayer
{
    public class CampaignConfigException : Exception
    {
        public CampaignConfigException(string message) : base(message) { }
    }

    /// <summary>
    /// Primary container and controller for campaigns and campaign career variables
    /// </summary>
    public class CampaignHandler
    {

        private CampaignHandler()
        {
            ReadBattleIni("INI/Battle.ini");
            ReadBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);

            CareerHandler.ReadCareerData(Missions, Variables);

            ValidateConfiguration();
        }

        public List<Mission> Missions = new List<Mission>();
        public Dictionary<string, int> Variables = new Dictionary<string, int>();

        private static Regex GameVariableFormat = new Regex(@"^[lg]\d+");

        private static string[] DifficultyIniPaths = new string[]
        {
            "INI/Map Code/Difficulty Easy.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Hard.ini"
        };


        private static CampaignHandler _instance;
        public static CampaignHandler Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CampaignHandler();
                return _instance;
            }
        }

        /// <summary>
        /// Reads all the missions defined in the specified battle ini file.
        /// Missions must have only one section, no overriding or piecemeal entries.
        /// </summary>
        private bool ReadBattleIni(string path)
        {
            Logger.Log("Attempting to parse " + path + " to populate mission list.");

            FileInfo iniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);

            if (!iniFileInfo.Exists)
            {
                Logger.Log("File " + path + " not found. Ignoring.");
                return false;
            }
            
            var battleIni = new IniFile(iniFileInfo.FullName);
            List<string> battleKeys = battleIni.GetSectionKeys("Battles");

            if (battleKeys == null)
                return false; // File exists but [Battles] doesn't

            foreach (string battleEntry in battleKeys)
            {
                string battleSection = battleIni.GetStringValue("Battles", battleEntry, "NOT FOUND");

                if (!battleIni.SectionExists(battleSection))
                    continue;

                // Mission mission = Missions.Find(m => m.InternalName == battleSection);
                // TODO Update duplicate

                Mission mission = new Mission(battleIni.GetSection(battleSection));
                Missions.Add(mission);

            }
            Logger.Log("Finished parsing " + path + ".");
            return true;
        }

        /// <summary>
        /// Checks all the mission definitions to make sure that the associated
        /// map files exist and all mission unlocks point to defined missions.
        /// </summary>
        private void ValidateConfiguration()
        {
            foreach (var mission in Missions.ToList())
            {
                string root = ProgramConstants.GamePath;

                // If defined, check to make sure mission file exists
                if(mission.Scenario != string.Empty &&
                    !File.Exists(root + mission.Scenario))
                {
                    Logger.Log("Map file for mission " + mission.InternalName + " not found. Entry will be discarded.");
                    Missions.Remove(mission);
                    continue;
                }

                // Make sure every variable mentioned is defined in the variables dictionary
                // Could probably get mod makers to define a list instead of this mess?
                foreach(var kvp in mission.LocalBindings)
                {
                        if (!Variables.ContainsKey(kvp.Key))
                        Variables.Add(kvp.Key, 0);
                }
                foreach (var kvp in mission.GlobalBindings)
                {
                    if (!Variables.ContainsKey(kvp.Key))
                        Variables.Add(kvp.Key, 0);
                }
                foreach (var kvp in mission.LocalUpdates)
                {
                    if (!Variables.ContainsKey(kvp.Key))
                        Variables.Add(kvp.Key, 0);
                }
                foreach (var kvp in mission.GlobalUpdates)
                {
                    if (!Variables.ContainsKey(kvp.Key))
                        Variables.Add(kvp.Key, 0);
                }
            }
        }

        public void CampaignPostGame(Mission mission)
        {
            Dictionary<string, int> gameVars = new Dictionary<string, int>();

            void VariablesFromIni(string ini)
            {
                FileInfo iniInfo = SafePath.GetFile(ProgramConstants.GamePath, ini);
                if (!iniInfo.Exists)
                    return;

                IniSection section = new IniFile(iniInfo.FullName).GetSection("spawnmap.ini");
                if (section.Keys.Count == 0)
                    return;

                for (int i = 0; i < section.Keys.Count; i++)
                {
                    gameVars.Add(ini[0] + i.ToString(), int.Parse(section.Keys[i].Value));
                }
            }

            VariablesFromIni("globals.ini");
            VariablesFromIni("locals.ini");

            // TODO Updates
        }

        public void WriteFilesForMission(Mission mission, int difficulty)
        {
            bool copyMapsToSpawnmapINI = ClientConfiguration.Instance.CopyMissionsToSpawnmapINI;

            Logger.Log("About to write spawn.ini.");
            using (var spawnStreamWriter = new StreamWriter(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawn.ini")))
            {
                spawnStreamWriter.WriteLine("; Generated by DTA Client");
                spawnStreamWriter.WriteLine("[Settings]");
                if (copyMapsToSpawnmapINI)
                    spawnStreamWriter.WriteLine("Scenario=spawnmap.ini");
                else
                    spawnStreamWriter.WriteLine("Scenario=" + mission.Scenario);

                // No one wants to play missions on Fastest, so we'll change it to Faster
                if (UserINISettings.Instance.GameSpeed == 0)
                    UserINISettings.Instance.GameSpeed.Value = 1;

                spawnStreamWriter.WriteLine("CampaignID=" + mission.CampaignID);
                spawnStreamWriter.WriteLine("GameSpeed=" + UserINISettings.Instance.GameSpeed);
#if YR || ARES
                spawnStreamWriter.WriteLine("Ra2Mode=" + !mission.RequiredAddon);
#else
                spawnStreamWriter.WriteLine("Firestorm=" + mission.RequiredAddon);
#endif
                spawnStreamWriter.WriteLine("CustomLoadScreen=" + LoadingScreenController.GetLoadScreenName(mission.Side.ToString()));
                spawnStreamWriter.WriteLine("IsSinglePlayer=Yes");
                spawnStreamWriter.WriteLine("SidebarHack=" + ClientConfiguration.Instance.SidebarHack);
                spawnStreamWriter.WriteLine("Side=" + mission.Side);
                spawnStreamWriter.WriteLine("BuildOffAlly=" + mission.BuildOffAlly);

                UserINISettings.Instance.Difficulty.Value = difficulty;

                spawnStreamWriter.WriteLine("DifficultyModeHuman=" + (mission.PlayerAlwaysOnNormalDifficulty ? "1" : difficulty.ToString()));
                spawnStreamWriter.WriteLine("DifficultyModeComputer=" + GetComputerDifficulty(difficulty));

                spawnStreamWriter.WriteLine();
                spawnStreamWriter.WriteLine();
                spawnStreamWriter.WriteLine();
            }

            var difficultyIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[difficulty]));

            if (copyMapsToSpawnmapINI)
            {
                var mapIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, mission.Scenario));
                IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
                mapIni = AppendVariableBinding(mapIni, mission);
                mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));
            }

            UserINISettings.Instance.Difficulty.Value = difficulty;
            UserINISettings.Instance.SaveSettings();
        }
        private int GetComputerDifficulty(int selected) =>
            Math.Abs(selected - 2);

        private IniFile AppendVariableBinding(IniFile src, Mission mission)
        {
            IniFile mapIni = src;

            // For locals, we just put the variable in the [VariableNames] section and set the default value
            foreach (var local in mission.LocalBindings)
                mapIni.SetStringValue("VariableNames", local.Value.ToString(), local.Key + "," + Variables[local.Key]);

            if (mission.HomeCell != string.Empty)
            {
                // Syntax for homecell change should be either "Variable:Waypoint" or "Variable|Value:Waypoint,Value:Waypoint,..."

                int index = mission.HomeCell.IndexOf('|');

                if (index != -1)
                {
                    string variable = mission.HomeCell.Substring(0, index);
                    string[] values = mission.HomeCell.Substring(index + 1).Split(',');

                    foreach (var v in values)
                    {
                        string[] parts = v.Split(':');

                        if (parts[0] == Variables[variable].ToString())
                        {
                            if (parts.Length == 2)
                                mapIni.SetStringValue("Basic", "HomeCell", parts[1]);
                            else
                                Logger.Log("Incorrect syntax for HomeCell flag on mission " + mission.InternalName + ": " + v);
                        }
                    }
                }
                else
                {
                    string[] parts = mission.HomeCell.Split(':');

                    if (Variables[parts[0]] > 0)
                    {
                        if (parts.Length == 2)
                            mapIni.SetStringValue("Basic", "HomeCell", parts[1]);
                        else
                            Logger.Log("Incorrect syntax for HomeCell flag on mission " + mission.InternalName + ": " + mission.HomeCell);
                    }
                }
            }

            // For globals, we need to make a trigger to set all the globals properly
            if (mission.GlobalBindings.Count > 0)
            {
                string action = "";
                int count = 0;

                foreach (var global in mission.GlobalBindings)
                {
                    if (Variables[global.Key] > 0)
                    {
                        action = action + ",28,0," + global.Value.ToString() + ",0,0,0,0,A";
                        count++;
                    }
                }

                // If none of the variables need to be set, skip to return the map ini before we write a nonsense trigger
                if (count < 1)
                    return mapIni;


                IniFile bindings = new IniFile();

                bindings.SetStringValue("Triggers", "XNACLIENT", "Neutral,<none>,Bind Client Variables,0,1,1,1,0");
                bindings.SetStringValue("Events", "XNACLIENT", "1,13,0,0");
                bindings.SetStringValue("Tags", "XNACLIENTTag", "0,Used by XNA Client,XNACLIENT");
                bindings.SetStringValue("Actions", "XNACLIENT", count + action);

                // Consolidating in this order should place our trigger as first in the list
                IniFile.ConsolidateIniFiles(bindings, mapIni);
                return bindings;
            }

            return mapIni;
        }
    }
}
