using System;
using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;
using Rampastring.Tools;


namespace DTAClient.Domain
{
    /// <summary>
    /// A Tiberian Sun mission listed in Battle(E).ini.
    /// </summary>
    public class Mission
    {
        public Mission(IniFile iniFile, string sectionName, int index)
        {
            Index = index;
            CD = iniFile.GetIntValue(sectionName, nameof(CD), 0);
            Side = iniFile.GetIntValue(sectionName, nameof(Side), 0);
            Scenario = iniFile.GetStringValue(sectionName, nameof(Scenario), string.Empty);
            UntranslatedGUIName = iniFile.GetStringValue(sectionName, "Description", "Undefined mission");
            GUIName = UntranslatedGUIName
                .L10N($"INI:Missions:{sectionName}:Description");

            IconPath = iniFile.GetStringValue(sectionName, "SideName", string.Empty);
            GUIDescription = iniFile.GetStringValue(sectionName, "LongDescription", string.Empty)
                .FromIniString()
                .L10N($"INI:Missions:{sectionName}:LongDescription");
            FinalMovie = iniFile.GetStringValue(sectionName, nameof(FinalMovie), "none");
            RequiredAddon = iniFile.GetBooleanValue(sectionName, nameof(RequiredAddon),
               ClientConfiguration.Instance.ClientGameType == ClientType.YR ||
               ClientConfiguration.Instance.ClientGameType == ClientType.Ares ?
                true :  // In case of YR this toggles Ra2Mode instead which should not be default
                false
            );
            Enabled = iniFile.GetBooleanValue(sectionName, nameof(Enabled), true);
            BuildOffAlly = iniFile.GetBooleanValue(sectionName, nameof(BuildOffAlly), false);
            PlayerAlwaysOnNormalDifficulty = iniFile.GetBooleanValue(sectionName, nameof(PlayerAlwaysOnNormalDifficulty), false);
        }

        public int Index { get; }
        public int CD { get; }
        public int CampaignID { get; } = -1;
        public int Side { get; }
        public string Scenario { get; }
        public string GUIName { get; }
        public string UntranslatedGUIName { get; }
        public string IconPath { get; }
        public string GUIDescription { get; }
        public string FinalMovie { get; }
        public bool RequiredAddon { get; }
        public bool Enabled { get; }
        public bool BuildOffAlly { get; }
        public bool PlayerAlwaysOnNormalDifficulty { get; }
    }
}
