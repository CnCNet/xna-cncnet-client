using ClientCore;

using Microsoft.Extensions.Options;

using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;

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
        private const string CAMPAIGN_INI = "INI/Campaign.ini";
        private const string VARS_SECTION = "CampaignVariables";

        private CampaignHandler()
        {
            ReadBattleIni("INI/Battle.ini");
            ReadBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);

            //CareerHandler.ReadCareerData(Missions, Variables);

            ValidateConfiguration();
        }

        /// <summary>
        /// Singleton pattern. Only one instance of this class can exist.
        /// </summary>
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

        public List<Mission> Missions = new List<Mission>();
        public Dictionary<string,int> Variables = new Dictionary<string,int>();

        /// <summary>
        /// Reads all the missions defined in the specified battle ini file.
        /// Missions must have only one section, no overriding or piecemeal entries.
        /// </summary>
        private void ReadBattleIni(string path)
        {
            string iniPath = ProgramConstants.GamePath + path;

            if (!File.Exists(iniPath))
            {
                Logger.Log("File " + path + " not found. Ignoring.");
                return;
            }

            var battleIni = new IniFile(iniPath);
            List<string> battleKeys = battleIni.GetSectionKeys("Battles");

            if (battleKeys == null)
                return; // File exists but [Battles] doesn't

            foreach (string battleEntry in battleKeys)
            {
                string battleSection = battleIni.GetStringValue("Battles", battleEntry, "NOT FOUND");

                if (!battleIni.SectionExists(battleSection))
                    continue;


                if (Missions.Exists(m => m.InternalName == battleSection))
                    throw new CampaignConfigException($"Multiple entries found for mission name: " + battleSection);

                Mission mission = new Mission(battleIni.GetSection(battleSection));
                Missions.Add(mission);
            }

        }

        /// <summary>
        /// Checks all the mission definitions to make sure that the associated
        /// map files exist and all mission unlocks point to defined missions.
        /// </summary>
        private void ValidateConfiguration()
        {
            foreach (var mission in Missions)
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

                foreach (var binding in mission.MissionUnlocks)
                {
                    if(!Missions.Exists(m => m.InternalName == binding.Key))
                    {
                        Logger.Log("Mission " + mission.InternalName + " has mission unlock defined for non-existant mission: " + binding.Key);
                        mission.MissionUnlocks.Remove(binding);
                        continue;
                    }
                }
            }
        }
    }
}
