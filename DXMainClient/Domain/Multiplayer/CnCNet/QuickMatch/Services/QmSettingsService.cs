using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;

public class QmSettingsService
{
    private static readonly string SettingsFile = ClientConfiguration.Instance.QuickMatchIniPath;
    private static QmSettingsService _instance;

    private const string BasicSectionKey = "Basic";
    private const string SoundsSectionKey = "Sounds";
    private const string HeaderLogosSectionKey = "HeaderLogos";

    private const string MatchFoundSoundFileKey = "MatchFoundSoundFile";
    private const string AllowedLaddersKey = "AllowedLadders";

    private QmSettings qmSettings;

    private QmSettingsService()
    {
    }

    public static QmSettingsService GetInstance() => _instance ??= new QmSettingsService();

    public QmSettings GetSettings() => qmSettings ??= LoadSettings();

    private static QmSettings LoadSettings()
    {
        if (!File.Exists(SettingsFile))
        {
            Logger.Log($"No QuickMatch settings INI not found: {SettingsFile}");
            return null;
        }

        var settings = new QmSettings();
        var iniFile = new IniFile(SettingsFile);
        LoadBasicSettings(iniFile, settings);
        LoadSoundSettings(iniFile, settings);
        LoadHeaderLogoSettings(iniFile, settings);

        return settings;
    }

    private static void LoadBasicSettings(IniFile iniFile, QmSettings settings)
    {
        IniSection basicSection = iniFile.GetSection(BasicSectionKey);
        if (basicSection == null)
            return;

        settings.MatchFoundWaitSeconds = QmSettings.DefaultMatchFoundWaitSeconds;
        settings.AllowedLadders = basicSection.GetStringValue(AllowedLaddersKey, string.Empty).Split(',').ToList();
    }

    private static void LoadSoundSettings(IniFile iniFile, QmSettings settings)
    {
        IniSection soundsSection = iniFile.GetSection(SoundsSectionKey);

        string matchFoundSoundFile = soundsSection?.GetStringValue(MatchFoundSoundFileKey, null);
        if (matchFoundSoundFile == null)
            return;

        matchFoundSoundFile = SafePath.CombineFilePath("Resources", matchFoundSoundFile);
        if (File.Exists(matchFoundSoundFile))
            settings.MatchFoundSoundFile = matchFoundSoundFile;
    }

    private static void LoadHeaderLogoSettings(IniFile iniFile, QmSettings settings)
    {
        IniSection headerLogosSection = iniFile.GetSection(HeaderLogosSectionKey);
        if (headerLogosSection == null)
            return;

        foreach (KeyValuePair<string, string> keyValuePair in headerLogosSection.Keys.Where(keyValuePair => AssetLoader.AssetExists(keyValuePair.Value)))
            settings.HeaderLogos.Add(keyValuePair.Key, AssetLoader.LoadTexture(keyValuePair.Value));
    }
}