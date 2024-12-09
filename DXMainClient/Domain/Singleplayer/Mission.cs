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

            MissionUnlocks = InitBindings(section, "Unlocks");
            PreGameBindings = InitBindings(section, "Uses");
            PostGameBindings = InitBindings(section, "Updates");
        }

        public string InternalName { get; }
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

        /// <summary>
        /// Does this mission need to be unlocked by playing another mission?
        /// </summary>
        public bool RequiresUnlocking { get; private set; }

        /// <summary>
        /// Is this mission currently unlocked?
        /// </summary>
        public bool IsUnlocked { get; set; }

        /// <summary>
        /// The best state in which the mission was last completed.
        /// </summary>
        public CompletionState Rank { get; set; }

        /// <summary>
        /// A set of bindings defining conditions which will unlock a given mission.
        /// </summary>
        public List<CampaignBinding> MissionUnlocks { get; private set; }

        /// <summary>
        /// A set of bindings used to set in-game variables and include ini files in mission maps.
        /// </summary>
        public List<CampaignBinding> PreGameBindings { get; private set; }

        /// <summary>
        /// A set of bindings used to update career variables after mission completion.
        /// </summary>
        public List<CampaignBinding> PostGameBindings { get; private set; }

        private List<CampaignBinding> InitBindings(IniSection section, string prefix)
        {
            List<CampaignBinding> bindings = new List<CampaignBinding>();
            foreach (KeyValuePair<string, string> kvp in section.Keys.Where(k => k.Key.StartsWith(prefix)))
            {
                string[] parts = kvp.Key.Split(".");
                if (parts.Length > 2)
                {
                    Logger.Log("Campaign binding key containing more than one /'./' will be skipped: " + kvp.Key);
                    continue;
                }
                CampaignBinding binding = new CampaignBinding(parts[1]);
                binding.Bind(kvp.Value);
                bindings.Add(binding);
            }
            return bindings;
        }
    }
}
