using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities = Rampastring.Tools.Utilities;

namespace DTAClient.domain.CnCNet
{
    /// <summary>
    /// A multiplayer game mode.
    /// </summary>
    public class GameMode
    {
        const string BASE_INI_PATH = "INI\\Map Code\\";
        const string FORCED_OPTIONS_SECTION = "ForcedOptions";
        const string SPAWN_INI_OPTIONS_SECTION = "ForcedSpawnIniOptions";

        public string Name { get; set; }

        public List<Map> Maps = new List<Map>();

        public List<KeyValuePair<string, bool>> ForcedCheckBoxValues = new List<KeyValuePair<string, bool>>();
        public List<KeyValuePair<string, int>> ForcedComboBoxValues = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, string>> ForcedSpawnIniOptions = new List<KeyValuePair<string, string>>();

        public int CoopDifficultyLevel { get; set; }

        public void Initialize()
        {
            IniFile forcedOptionsIni = new IniFile(ProgramConstants.GamePath + BASE_INI_PATH + Name + "_ForcedOptions.ini");

            ParseForcedOptions(forcedOptionsIni);

            ParseSpawnIniOptions(forcedOptionsIni);
        }

        private void ParseForcedOptions(IniFile forcedOptionsIni)
        {
            List<string> keys = forcedOptionsIni.GetSectionKeys(FORCED_OPTIONS_SECTION);

            if (keys == null)
                return;

            foreach (string key in keys)
            {
                string value = forcedOptionsIni.GetStringValue(FORCED_OPTIONS_SECTION, key, String.Empty);

                int intValue = 0;
                if (Int32.TryParse(value, out intValue))
                {
                    ForcedComboBoxValues.Add(new KeyValuePair<string, int>(key, intValue));
                }
                else
                {
                    ForcedCheckBoxValues.Add(new KeyValuePair<string, bool>(key, Utilities.BooleanFromString(value, false)));
                }
            }
        }

        private void ParseSpawnIniOptions(IniFile forcedOptionsIni)
        {
            List<string> spawnIniKeys = forcedOptionsIni.GetSectionKeys(SPAWN_INI_OPTIONS_SECTION);

            if (spawnIniKeys == null)
                return;

            foreach (string key in spawnIniKeys)
            {
                ForcedSpawnIniOptions.Add(new KeyValuePair<string, string>(key, forcedOptionsIni.GetStringValue(SPAWN_INI_OPTIONS_SECTION, key, String.Empty)));
            }
        }

        public IniFile GetMapRulesIniFile()
        {
            return new IniFile(ProgramConstants.GamePath + BASE_INI_PATH + Name + ".ini");
        }
    }
}
