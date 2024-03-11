using System;
using System.Collections.Generic;
using System.Linq;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// A single game option preset.
/// </summary>
public class GameOptionPreset
{
    public GameOptionPreset(string profileName)
    {
        ProfileName = profileName;

        if (ProfileName.Contains('[') || ProfileName.Contains(']'))
        {
            throw new ArgumentException("Game option preset name cannot contain the [] characters.");
        }
    }

    /// <summary>
    /// Checks if a specific name is valid for the name of a game option preset.
    /// Returns null if the name is valid, an error message otherwise.
    /// </summary>
    public static string IsNameValid(string name)
    {
        return name.Contains('[') || name.Contains(']') ? "Game option preset name cannot contain the [] characters." : null;
    }

    public string ProfileName { get; }

    private readonly Dictionary<string, bool> checkBoxValues = [];
    private readonly Dictionary<string, int> dropDownValues = [];

    private void AddValues<T>(IniSection section, string keyName, Dictionary<string, T> dictionary, Converter<string, T> converter)
    {
        string[] valueStrings = section.GetStringValue(keyName,
            string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string value in valueStrings)
        {
            string[] splitValue = value.Split(':');
            if (splitValue.Length != 2)
            {
                Logger.Log($"Failed to parse game option preset value ({ProfileName}, {keyName})");
                continue;
            }

            dictionary.Add(splitValue[0], converter(splitValue[1]));
        }
    }

    public void AddCheckBoxValue(string checkBoxName, bool value)
    {
        checkBoxValues.Add(checkBoxName, value);
    }

    public void AddDropDownValue(string dropDownValue, int value)
    {
        dropDownValues.Add(dropDownValue, value);
    }

    public Dictionary<string, bool> GetCheckBoxValues()
    {
        return new Dictionary<string, bool>(checkBoxValues);
    }

    public Dictionary<string, int> GetDropDownValues()
    {
        return new Dictionary<string, int>(dropDownValues);
    }

    public void Read(IniSection section)
    {
        // Syntax example:
        // CheckBoxValues=chkCrates:1,chkShortGame:1,chkFastResourceGrowth:0,.... (0 = unchecked, 1 = checked)
        // DropDownValues=ddTechLevel:7,ddStartingCredits:5,... (the number is the selected option index)

        AddValues(section, "CheckBoxValues", checkBoxValues, s => s == "1");
        AddValues(section, "DropDownValues", dropDownValues, s => Conversions.IntFromString(s, 0));
    }

    public void Write(IniSection section)
    {
        section.SetStringValue("CheckBoxValues", string.Join(",",
            checkBoxValues.Select(s => $"{s.Key}:{(s.Value ? "1" : "0")}")));
        section.SetStringValue("DropDownValues", string.Join(",",
            dropDownValues.Select(s => $"{s.Key}:{s.Value}")));
    }
}

/// <summary>
/// Handles game option presets.
/// </summary>
public class GameOptionPresets
{
    private const string IniFileName = "GameOptionsPresets.ini";
    private const string PresetDefinitionsSectionName = "Presets";

    private GameOptionPresets() { }

    private static GameOptionPresets _instance;
    public static GameOptionPresets Instance
    {
        get
        {
            _instance ??= new GameOptionPresets();

            return _instance;
        }
    }

    private IniFile gameOptionPresetsIni;
    private Dictionary<string, GameOptionPreset> presets;

    public GameOptionPreset GetPreset(string name)
    {
        LoadIniIfNotInitialized();

        return presets.TryGetValue(name, out GameOptionPreset value) ? value : null;
    }

    public List<string> GetPresetNames()
    {
        LoadIniIfNotInitialized();

        return presets.Keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToList();
    }

    public void AddPreset(GameOptionPreset preset)
    {
        LoadIniIfNotInitialized();

        presets[preset.ProfileName] = preset;
        WriteIni();
    }

    public void DeletePreset(string name)
    {
        LoadIniIfNotInitialized();

        if (!presets.ContainsKey(name))
        {
            return;
        }

        _ = presets.Remove(name);
        WriteIni();
    }

    private void LoadIniIfNotInitialized()
    {
        if (gameOptionPresetsIni == null)
        {
            LoadIni();
        }
    }

    private void LoadIni()
    {
        gameOptionPresetsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, IniFileName));
        presets = [];

        IniSection presetsDefinitions = gameOptionPresetsIni.GetSection(PresetDefinitionsSectionName);
        if (presetsDefinitions == null)
        {
            return;
        }

        foreach (KeyValuePair<string, string> kvp in presetsDefinitions.Keys)
        {
            if (!presets.ContainsKey(kvp.Value))
            {
                IniSection presetSection = gameOptionPresetsIni.GetSection(kvp.Value);
                if (presetSection == null)
                {
                    continue;
                }

                GameOptionPreset preset = new(kvp.Value);
                preset.Read(presetSection);
                presets[kvp.Value] = preset;
            }
        }
    }

    private void WriteIni()
    {
        gameOptionPresetsIni = new IniFile();
        int i = 0;
        IniSection definitionsSection = new(PresetDefinitionsSectionName);
        gameOptionPresetsIni.AddSection(definitionsSection);
        foreach (KeyValuePair<string, GameOptionPreset> kvp in presets)
        {
            definitionsSection.SetStringValue(i.ToString(), kvp.Value.ProfileName);
            IniSection presetSection = new(kvp.Value.ProfileName);
            kvp.Value.Write(presetSection);
            gameOptionPresetsIni.AddSection(presetSection);
            i++;
        }

        gameOptionPresetsIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, IniFileName));
    }
}