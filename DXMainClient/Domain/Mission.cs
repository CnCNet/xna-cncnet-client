using Rampastring.Tools;
using System;

namespace DTAClient.Domain
{
    /// <summary>
    /// A Tiberian Sun mission listed in Battle(E).ini.
    /// </summary>
    public class Mission
    {
        public Mission(IniFile iniFile, string sectionName)
        {
            CD = iniFile.GetIntValue(sectionName, nameof(CD), 0);
            Side = iniFile.GetIntValue(sectionName, nameof(Side), 0);
            Scenario = iniFile.GetStringValue(sectionName, nameof(Scenario), string.Empty);
            GUIName = iniFile.GetStringValue(sectionName, "Description", "Undefined mission");
            IconPath = iniFile.GetStringValue(sectionName, "SideName", string.Empty);
            GUIDescription = iniFile.GetStringValue(sectionName, "LongDescription", string.Empty);
            FinalMovie = iniFile.GetStringValue(sectionName, nameof(FinalMovie), "none");
            RequiredAddon = iniFile.GetBooleanValue(sectionName, nameof(RequiredAddon), false);
            Enabled = iniFile.GetBooleanValue(sectionName, nameof(Enabled), true);
            BuildOffAlly = iniFile.GetBooleanValue(sectionName, nameof(BuildOffAlly), false);
            PlayerAlwaysOnNormalDifficulty = iniFile.GetBooleanValue(sectionName, nameof(PlayerAlwaysOnNormalDifficulty), false);

            GUIDescription = GUIDescription.Replace("@", Environment.NewLine);
        }

        public int CD { get; }
        public int Side { get; }
        public string Scenario { get; }
        public string GUIName { get; }
        public string IconPath { get; }
        public string GUIDescription { get; }
        public string FinalMovie { get; }
        public bool RequiredAddon { get; }
        public bool Enabled { get; }
        public bool BuildOffAlly { get; }
        public bool PlayerAlwaysOnNormalDifficulty { get; }
    }
}
