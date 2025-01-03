using System;
using System.Collections.Generic;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;

using Rampastring.Tools;


namespace DTAClient.Domain.Singleplayer
{
    /// <summary>
    /// A Tiberian Sun mission listed in Battle(E).ini.
    /// </summary>
    public class Mission
    {
        private const int CompletionStateCount = 3;

        public Mission(IniSection section)
        {
            InternalName = section.SectionName;
            CD = section.GetIntValue(nameof(CD), 0);
            Side = section.GetIntValue(nameof(Side), 0);
            Scenario = section.GetStringValue(nameof(Scenario), string.Empty);
            UntranslatedGUIName = section.GetStringValue("Description", "Undefined mission");
            GUIName = UntranslatedGUIName
                .L10N($"INI:Missions:{section.SectionName}:Description");

            IconPath = section.GetStringValue("SideName", string.Empty);
            GUIDescription = section.GetStringValue("LongDescription", string.Empty)
                .FromIniString()
                .L10N($"INI:Missions:{section.SectionName}:LongDescription");
            FinalMovie = section.GetStringValue(nameof(FinalMovie), "none");
            RequiredAddon = section.GetBooleanValue(nameof(RequiredAddon),
#if YR || ARES
                true  // In case of YR this toggles Ra2Mode instead which should not be default
#else
                false
#endif
            );
            Enabled = section.GetBooleanValue(nameof(Enabled), true);
            BuildOffAlly = section.GetBooleanValue(nameof(BuildOffAlly), false);
            PlayerAlwaysOnNormalDifficulty = section.GetBooleanValue(nameof(PlayerAlwaysOnNormalDifficulty), false);

            HomeCell = section.GetStringValue("HomeCell", string.Empty);

            ConfigurableVariables = new List<string>();
            for(int i = 0; i < 10; i++)
            {
                string str = section.GetStringValue("ConfigureVariable" + i, string.Empty);
                if (str == string.Empty)
                    break;
                ConfigurableVariables.Add(str);
            }

            LocalBindings = ParseVariables(section.GetStringValue("BindLocals", string.Empty));
            GlobalBindings = ParseVariables(section.GetStringValue("BindGlobals", string.Empty));
            LocalUpdates = ParseVariables(section.GetStringValue("LocalUpdates", string.Empty));
            GlobalUpdates = ParseVariables(section.GetStringValue("GlobalUpdates", string.Empty));
        }

        public string InternalName { get; }
        public int CD { get; private set; }
        public int CampaignID { get; } = -1;
        public int Side { get; private set; }
        public string Scenario { get; private set; }
        public string GUIName { get; private set; }
        public string UntranslatedGUIName { get; private set; }
        public string IconPath { get; private set; }
        public string GUIDescription { get; private set; }
        public string FinalMovie { get; private set; }
        public bool RequiredAddon { get; private set; }
        public bool Enabled { get; private set; }
        public bool BuildOffAlly { get; private set; }
        public bool PlayerAlwaysOnNormalDifficulty { get; }
        public bool RequiresUnlocking { get; private set; }
        public bool IsUnlocked { get; set; }
        public CompletionState Rank { get; set; }
        public string HomeCell { get; }
        public List<string> ConfigurableVariables { get; }
        public Dictionary<string, int> LocalBindings { get; }
        public Dictionary<string, int> GlobalBindings { get; }
        public Dictionary<string, int> LocalUpdates { get; }
        public Dictionary<string, int> GlobalUpdates { get; }

        private Dictionary<string, int> ParseVariables(string str)
        {
            if (str == string.Empty)
                return new Dictionary<string, int>();

            Dictionary<string, int> binds = new Dictionary<string, int>();

            string[] vars = str.Split(',');

            foreach (string var in vars)
            {
                string[] parts = var.Split(':');
                if(parts.Length == 2)
                {
                    binds.Add(parts[0], int.Parse(parts[1]));
                }
                else
                {
                    Logger.Log(InternalName + " failed trying to parse client variable: " + var);
                }
            }

            return binds;
        }
    }
}
