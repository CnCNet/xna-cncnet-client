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
            CD = iniFile.GetIntValue(sectionName, "CD", 0);
            Side = iniFile.GetIntValue(sectionName, "Side", 0);
            Scenario = iniFile.GetStringValue(sectionName, "Scenario", string.Empty);
            GUIName = iniFile.GetStringValue(sectionName, "Description", "Undefined mission");
            IconPath = iniFile.GetStringValue(sectionName, "SideName", string.Empty);
            GUIDescription = iniFile.GetStringValue(sectionName, "LongDescription", string.Empty);
            FinalMovie = iniFile.GetStringValue(sectionName, "FinalMovie", "none");
            RequiredAddon = iniFile.GetBooleanValue(sectionName, "RequiredAddon", false);
            Enabled = iniFile.GetBooleanValue(sectionName, "Enabled", true);
            BuildOffAlly = iniFile.GetBooleanValue(sectionName, "BuildOffAlly", false);

            GUIDescription = GUIDescription.Replace("@", Environment.NewLine);
        }

        public int CD { get; private set; }
        public int Side { get; private set; }
        public string Scenario { get; private set; }
        public string GUIName { get; private set; }
        public string IconPath { get; private set; }
        public string GUIDescription { get; private set; }
        public string FinalMovie { get; private set; }
        public bool RequiredAddon { get; private set; }
        public bool Enabled { get; private set; }
        public bool BuildOffAlly { get; private set; }
    }
}
