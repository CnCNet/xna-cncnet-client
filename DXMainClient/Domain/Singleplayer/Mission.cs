using System;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;

using Rampastring.Tools;


namespace DTAClient.Domain.Singleplayer
{
    /// <summary>
    /// A Tiberian Sun mission listed in Battle(E).ini.
    /// </summary>
    public class Mission
    {
        public Mission(IniSection section, int index)
        {
            CampaignID = index;
            Name = section.SectionName;
            CD = section.GetIntValue(nameof(CD), 0);
            Side = section.GetIntValue(nameof(Side), 0);
            Scenario = section.GetStringValue(nameof(Scenario), string.Empty);
            UntranslatedGUIName = section.GetStringValue( "Description", "Undefined mission");
            GUIName = UntranslatedGUIName
                .L10N($"INI:Missions:{section.SectionName}:Description");

            IconPath = section.GetStringValue("SideName", string.Empty);
            GUIDescription = section.GetStringValue("LongDescription", string.Empty)
                .FromIniString()
                .L10N($"INI:Missions:{section.SectionName}:LongDescription");
            FinalMovie = section.GetStringValue(nameof(FinalMovie), "none");
            RequiredAddon = section.GetBooleanValue(nameof(RequiredAddon),
               ClientConfiguration.Instance.ClientGameType == ClientType.YR ||
               ClientConfiguration.Instance.ClientGameType == ClientType.Ares ?
                true :  // In case of YR this toggles Ra2Mode instead which should not be default
                false
            );
            Enabled = section.GetBooleanValue(nameof(Enabled), true);
            BuildOffAlly = section.GetBooleanValue(nameof(BuildOffAlly), false);
            PlayerAlwaysOnNormalDifficulty = section.GetBooleanValue(nameof(PlayerAlwaysOnNormalDifficulty), false);
        }

        public string Name { get; }
        public int CD { get; }
        public int CampaignID { get; }
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
