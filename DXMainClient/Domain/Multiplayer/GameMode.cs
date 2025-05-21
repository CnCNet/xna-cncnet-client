using ClientCore;
using ClientCore.Extensions;

using Rampastring.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// A multiplayer game mode.
    /// </summary>
    public class GameMode : GameModeMapBase
    {
        public GameMode(string name)
        {
            Name = name;
            Initialize();
        }

        private const string BASE_INI_PATH = "INI/Map Code/";
        private const string SPAWN_INI_OPTIONS_SECTION = "ForcedSpawnIniOptions";

        /// <summary>
        /// The internal (INI) name of the game mode.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The user-interface name of the game mode.
        /// </summary>
        public string UIName { get; private set; }

        /// <summary>
        /// The original user-interface name of the game mode before translation.
        /// </summary>
        public string UntranslatedUIName { get; private set; }

        /// <summary>
        /// List of side indices players cannot select in this game mode.
        /// </summary>
        public List<int> DisallowedPlayerSides = new List<int>();

        /// <summary>
        /// List of side indices human players cannot select in this game mode.
        /// </summary>
        public List<int> DisallowedHumanPlayerSides = new List<int>();

        /// <summary>
        /// List of side indices computer players cannot select in this game mode.
        /// </summary>
        public List<int> DisallowedComputerPlayerSides = new List<int>();

        /// </summary>
        /// Override for minimum amount of players needed to play any map in this game mode.
        /// Priority sequences: GameMode.MinPlayersOverride, Map.MinPlayers, GameMode.MinPlayers.
        /// </summary>
        public int? MinPlayersOverride { get; private set; }

        public int? MaxPlayersOverride { get; private set; }

        private string mapCodeININame;
        private List<string> randomizedMapCodeININames;
        private int randomizedMapCodesCount;

        private string forcedOptionsSection;

        public List<Map> Maps = new List<Map>();

        public List<KeyValuePair<string, bool>> ForcedCheckBoxValues = new List<KeyValuePair<string, bool>>();
        public List<KeyValuePair<string, int>> ForcedDropDownValues = new List<KeyValuePair<string, int>>();

        private List<KeyValuePair<string, string>> ForcedSpawnIniOptions = new List<KeyValuePair<string, string>>();

        public void Initialize()
        {
            IniFile forcedOptionsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MPMapsIniPath));
            IniSection section = forcedOptionsIni.GetSection(Name);

            UntranslatedUIName = section.GetStringValue("UIName", Name);
            UIName = UntranslatedUIName.L10N($"INI:GameModes:{Name}:UIName");

            InitializeBaseSettingsFromIniSection(forcedOptionsIni.GetSection(Name));

            MinPlayersOverride = section.GetIntValueOrNull("MinPlayersOverride");
            MaxPlayersOverride = section.GetIntValueOrNull("MaxPlayersOverride");

            forcedOptionsSection = section.GetStringValue("ForcedOptions", string.Empty);
            mapCodeININame = section.GetStringValue("MapCodeININame", Name + ".ini");
            randomizedMapCodeININames = section.GetStringValue("RandomizedMapCodeININames", string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            randomizedMapCodesCount = section.GetIntValue("RandomizedMapCodesCount", 1);

            DisallowedPlayerSides = section.GetListValue("DisallowedPlayerSides", ',', int.Parse);
            DisallowedHumanPlayerSides = section.GetListValue("DisallowedHumanPlayerSides", ',', int.Parse);
            DisallowedComputerPlayerSides = section.GetListValue("DisallowedComputerPlayerSides", ',', int.Parse);

            ParseForcedOptions(forcedOptionsIni);

            ParseSpawnIniOptions(forcedOptionsIni);
        }

        private void ParseForcedOptions(IniFile forcedOptionsIni)
        {
            if (string.IsNullOrEmpty(forcedOptionsSection))
                return;

            List<string> keys = forcedOptionsIni.GetSectionKeys(forcedOptionsSection);

            if (keys == null)
                return;

            foreach (string key in keys)
            {
                string value = forcedOptionsIni.GetStringValue(forcedOptionsSection, key, string.Empty);

                int intValue = 0;
                if (int.TryParse(value, out intValue))
                {
                    ForcedDropDownValues.Add(new KeyValuePair<string, int>(key, intValue));
                }
                else
                {
                    ForcedCheckBoxValues.Add(new KeyValuePair<string, bool>(key, Conversions.BooleanFromString(value, false)));
                }
            }
        }

        private void ParseSpawnIniOptions(IniFile forcedOptionsIni)
        {
            string section = forcedOptionsIni.GetStringValue(Name, "ForcedSpawnIniOptions", Name + SPAWN_INI_OPTIONS_SECTION);

            List<string> spawnIniKeys = forcedOptionsIni.GetSectionKeys(section);

            if (spawnIniKeys == null)
                return;

            foreach (string key in spawnIniKeys)
            {
                ForcedSpawnIniOptions.Add(new KeyValuePair<string, string>(key,
                    forcedOptionsIni.GetStringValue(section, key, string.Empty)));
            }
        }

        public void ApplySpawnIniCode(IniFile spawnIni)
        {
            foreach (KeyValuePair<string, string> key in ForcedSpawnIniOptions)
                spawnIni.SetStringValue("Settings", key.Key, key.Value);
        }

        public List<IniFile> GetMapRulesIniFiles(Random random)
        {
            var mapRules = new List<IniFile>() { new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, BASE_INI_PATH, mapCodeININame)) };
            if (randomizedMapCodeININames.Count == 0)
                return mapRules;

            Dictionary<string, int> randomOrder = new();
            foreach (string name in randomizedMapCodeININames)
            {
                randomOrder[name] = random.Next();
            }

            mapRules.AddRange(
                from iniName in randomizedMapCodeININames.OrderBy(x => randomOrder[x]).Take(randomizedMapCodesCount)
                select new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, BASE_INI_PATH, iniName)));

            return mapRules;
        }

        protected bool Equals(GameMode other) => string.Equals(Name, other?.Name, StringComparison.InvariantCultureIgnoreCase);

        public override int GetHashCode() => (Name != null ? Name.GetHashCode() : 0);
    }
}
