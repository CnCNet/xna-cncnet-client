using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    public class TeamStartMappingUserPresets
    {
        private const string IniFileName = "AutoAllyUserPresets.ini";
        private static readonly string FullIniPath = ProgramConstants.ClientUserFilesPath + IniFileName;
        private const string PresetDefinitionsSectionName = "Presets";
        private const string DefaultPresetKey = "$$Default";

        private IniFile teamStartMappingsPresetsIni;
        private Dictionary<string, List<TeamStartMappingPreset>> TeamStartMappingPresets;

        private TeamStartMappingUserPresets()
        {
        }

        private static TeamStartMappingUserPresets _instance;

        public static TeamStartMappingUserPresets Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TeamStartMappingUserPresets();

                return _instance;
            }
        }

        private void LoadIniIfNotInitialized()
        {
            if (teamStartMappingsPresetsIni == null)
                Load();
        }

        private void Load()
        {
            TeamStartMappingPresets = new Dictionary<string, List<TeamStartMappingPreset>>();
            if (!File.Exists(FullIniPath))
                return;

            teamStartMappingsPresetsIni = new IniFile(FullIniPath);

            var presetsSection = teamStartMappingsPresetsIni.GetSection(PresetDefinitionsSectionName);
            var mapNames = presetsSection.Keys.Select(key => key.Value);

            foreach (string mapName in mapNames)
            {
                var mapSection = teamStartMappingsPresetsIni.GetSection(mapName);
                if (mapSection == null)
                    continue;

                string defaultPresetName = null;
                if (mapSection.KeyExists(DefaultPresetKey))
                    defaultPresetName = mapSection.GetStringValue(DefaultPresetKey, null);

                foreach (var mapSectionKey in mapSection.Keys.Where(k => k.Key != DefaultPresetKey))
                {
                    try
                    {
                        AddOrUpdate(mapName, new TeamStartMappingPreset
                        {
                            Name = mapSectionKey.Key,
                            TeamStartMappings = TeamStartMapping.FromListString(mapSectionKey.Value),
                            IsDefaultForMap = string.Equals(mapSectionKey.Key, defaultPresetName),
                            IsUserDefined = true
                        });
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Unable to read user defined team/start mapping: {mapName}, {mapSectionKey.Key}, {mapSectionKey.Value}\n{e.Message}");
                    }
                }
            }
        }

        private void Save()
        {
            teamStartMappingsPresetsIni = new IniFile();
            var presetsSection = new IniSection(PresetDefinitionsSectionName);
            teamStartMappingsPresetsIni.AddSection(presetsSection);
            List<string> mapNames = TeamStartMappingPresets.Keys.ToList();
            for (int i = 0; i < mapNames.Count; i++)
            {
                string mapName = mapNames[i];
                presetsSection.AddKey(i.ToString(), mapName);
                var mapPresets = TeamStartMappingPresets[mapName];
                var defaultPreset = mapPresets.FirstOrDefault(p => p.IsDefaultForMap);

                var mapSection = new IniSection(mapName);
                if (defaultPreset != null)
                    mapSection.AddKey(DefaultPresetKey, defaultPreset.Name);
                
                foreach (var teamStartMappingPreset in mapPresets)
                    mapSection.AddKey(teamStartMappingPreset.Name, TeamStartMapping.ToListString(teamStartMappingPreset.TeamStartMappings));

                teamStartMappingsPresetsIni.AddSection(mapSection);
            }

            teamStartMappingsPresetsIni.WriteIniFile(FullIniPath);
        }

        public void AddOrUpdate(Map map, TeamStartMappingPreset preset)
        {
            LoadIniIfNotInitialized();
            AddOrUpdate(map.IniSafeName, preset);
            Save();
        }

        private void AddOrUpdate(string mapName, TeamStartMappingPreset preset)
        {
            preset.IsUserDefined = true;
            if (!TeamStartMappingPresets.ContainsKey(mapName))
                TeamStartMappingPresets.Add(mapName, new List<TeamStartMappingPreset>());

            var existingPreset = TeamStartMappingPresets[mapName].FirstOrDefault(p => p.Name == preset.Name);

            if (existingPreset == null)
                TeamStartMappingPresets[mapName].Add(preset);
            else
                existingPreset.TeamStartMappings = preset.TeamStartMappings;
        }

        public void DeletePreset(Map map, TeamStartMappingPreset preset)
        {
            LoadIniIfNotInitialized();
            if (preset == null)
                return;

            string iniSafeName = map.IniSafeName;
            if (!TeamStartMappingPresets.ContainsKey(iniSafeName))
                return;

            TeamStartMappingPresets[iniSafeName].Remove(preset);

            Save();
        }

        public List<TeamStartMappingPreset> GetPresets(Map map)
        {
            LoadIniIfNotInitialized();
            string iniSafeName = map.IniSafeName;

            return TeamStartMappingPresets.ContainsKey(iniSafeName) ? TeamStartMappingPresets[iniSafeName] : new List<TeamStartMappingPreset>();
        }
    }
}
