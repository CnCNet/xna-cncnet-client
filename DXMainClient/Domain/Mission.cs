using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ClientCore;
using ClientCore.Extensions;
using Rampastring.Tools;

namespace DTAClient.Domain
{
    /// <summary>
    /// A Tiberian Sun mission listed in Battle(E).ini.
    /// </summary>
    public class Mission
    {
        public Mission(IniSection missionSection, string missionCodeName)
        {
            if (missionSection == null)
                throw new ArgumentNullException(nameof(missionSection));

            CD = missionSection.GetIntValue(nameof(CD), 0);
            Side = missionSection.GetIntValue(nameof(Side), 0);
            Scenario = missionSection.GetStringValue(nameof(Scenario), string.Empty);
            UntranslatedGUIName = missionSection.GetStringValue("Description", "Undefined mission");
            GUIName = UntranslatedGUIName
                .L10N($"INI:Missions:{missionCodeName}:Description");

            IconPath = missionSection.GetStringValue("SideName", string.Empty);
            GUIDescription = missionSection.GetStringValue("LongDescription", string.Empty)
                .FromIniString()
                .L10N($"INI:Missions:{missionCodeName}:LongDescription");
            FinalMovie = missionSection.GetStringValue(nameof(FinalMovie), "none");
            RequiredAddon = missionSection.GetBooleanValue(nameof(RequiredAddon),
#if YR || ARES
                true // In case of YR this toggles Ra2Mode instead which should not be default
#else
                false
#endif
            );
            Enabled = missionSection.GetBooleanValue(nameof(Enabled), true);
            BuildOffAlly = missionSection.GetBooleanValue(nameof(BuildOffAlly), false);
            PlayerAlwaysOnNormalDifficulty = missionSection.GetBooleanValue(nameof(PlayerAlwaysOnNormalDifficulty), false);
            Tags = missionSection.GetStringValue(nameof(Tags), string.Empty).Split(',');

            CodeName = missionCodeName;
            CustomMissionID = ComputeCustomMissionID(missionCodeName);
        }

        public static Mission NewCustomMission(IniSection missionSection, string missionCodeName, string scenario, IniSection? missionMdIniSection)
        {
            var mission = new Mission(missionSection, missionCodeName)
            {
                IsCustomMission = true,
                Scenario = scenario,
                CustomMission_MissionMdIniSection = missionMdIniSection,
                Tags = ["CUSTOM"],
            };
            return mission;
        }

        private static int ComputeCustomMissionID(string missionCodeName)
        {
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
#pragma warning disable CA1850 // Prefer static 'HashData' method over 'ComputeHash'
            using var sha1 = SHA1.Create();
            byte[] digest = sha1.ComputeHash(Encoding.UTF8.GetBytes(missionCodeName));
            return BitConverter.ToInt32(digest, 0);
#pragma warning restore CA1850 // Prefer static 'HashData' method over 'ComputeHash'
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
        }

        public string CodeName { get; private set; }
        public int CampaignID { get; } = -1;
        public int CustomMissionID { get; private set; }

        public int CD { get; private set; }
        public int Side { get; private set; }

        /// <summary>
        /// Refers to the map file. Must be a relative path to the game folder.
        /// </summary>
        public string Scenario { get; private set; }
        public string GUIName { get; private set; }
        public string UntranslatedGUIName { get; private set; }
        public string IconPath { get; private set; }
        public string GUIDescription { get; private set; }
        public string FinalMovie { get; private set; }
        public bool RequiredAddon { get; private set; }
        public bool Enabled { get; set; }
        public bool BuildOffAlly { get; private set; }
        public bool PlayerAlwaysOnNormalDifficulty { get; private set; }
        public IReadOnlyCollection<string> Tags { get; private set; }

        /// <summary>
        /// This property is not set through the ini file.
        /// For a user custom mission, "scenario" will be assumed as the filename of a map file, with the suffix ".map" (case-insensitive).
        /// The map file is assumed to be placed at ClientConfiguration.CustomMissionPath.
        /// When launching a user custom mission, all supplemental files, i.e., files with the same filename (excepts for the suffix), will be temporarily copied into game folder.
        /// </summary>
        public bool IsCustomMission { get; private set; }

        public IniSection? CustomMission_MissionMdIniSection { get; private set; }
    }
}