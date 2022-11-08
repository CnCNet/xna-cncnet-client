using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;

public class QmSettingsService
{
    private static QmSettingsService Instance;
    private static readonly string SettingsFile = ClientConfiguration.Instance.QuickMatchPath;

    private const string BasicSectionKey = "Basic";
    private const string SoundsSectionKey = "Sounds";
    private const string HeaderLogosSectionKey = "HeaderLogos";

    private const string BaseUrlKey = "BaseUrl";
    private const string LoginUrlKey = "LoginUrl";
    private const string RefreshUrlKey = "RefreshUrl";
    private const string ServerStatusUrlKey = "ServerStatusUrl";
    private const string GetUserAccountsUrlKey = "GetUserAccountsUrl";
    private const string GetLaddersUrlKey = "GetLaddersUrl";
    private const string GetLadderMapsUrlKey = "GetLadderMapsUrl";

    private const string MatchFoundSoundFileKey = "MatchFoundSoundFile";
    private const string AllowedLaddersKey = "AllowedLadders";

    private QmSettings qmSettings;

    private QmSettingsService()
    {
    }

    public static QmSettingsService GetInstance() => Instance ??= new QmSettingsService();

    public QmSettings GetSettings() => qmSettings ??= LoadSettings();

    private static QmSettings LoadSettings()
    {
        var settings = new QmSettings();
        if (!File.Exists(SettingsFile))
            SaveSettings(settings); // init the settings file

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

        settings.BaseUrl = basicSection.GetStringValue(BaseUrlKey, QmSettings.DefaultBaseUrl);
        settings.LoginUrl = basicSection.GetStringValue(LoginUrlKey, QmSettings.DefaultLoginUrl);
        settings.RefreshUrl = basicSection.GetStringValue(RefreshUrlKey, QmSettings.DefaultRefreshUrl);
        settings.ServerStatusUrl = basicSection.GetStringValue(ServerStatusUrlKey, QmSettings.DefaultServerStatusUrl);
        settings.GetUserAccountsUrl = basicSection.GetStringValue(GetUserAccountsUrlKey, QmSettings.DefaultGetUserAccountsUrl);
        settings.GetLaddersUrl = basicSection.GetStringValue(GetLaddersUrlKey, QmSettings.DefaultGetLaddersUrl);
        settings.GetLadderMapsUrlFormat = basicSection.GetStringValue(GetLadderMapsUrlKey, QmSettings.DefaultGetLadderMapsUrl);
        settings.MatchFoundWaitSeconds = basicSection.GetIntValue(GetLadderMapsUrlKey, QmSettings.DefaultMatchFoundWaitSeconds);
        settings.AllowedLadders = basicSection.GetStringValue(AllowedLaddersKey, string.Empty).Split(',').ToList();
    }

    private static void LoadSoundSettings(IniFile iniFile, QmSettings settings)
    {
        IniSection soundsSection = iniFile.GetSection(SoundsSectionKey);
        if (soundsSection == null)
            return;

        string matchFoundSoundFile = soundsSection.GetStringValue(MatchFoundSoundFileKey, null);
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

    public static void SaveSettings(QmSettings settings)
    {
        var iniFile = new IniFile();
        var basicSection = new IniSection(BasicSectionKey);
        basicSection.AddKey(BaseUrlKey, settings.BaseUrl);
        basicSection.AddKey(LoginUrlKey, settings.LoginUrl);
        basicSection.AddKey(RefreshUrlKey, settings.RefreshUrl);
        basicSection.AddKey(ServerStatusUrlKey, settings.ServerStatusUrl);
        basicSection.AddKey(GetUserAccountsUrlKey, settings.GetUserAccountsUrl);
        basicSection.AddKey(GetLaddersUrlKey, settings.GetLaddersUrl);
        basicSection.AddKey(GetLadderMapsUrlKey, settings.GetLadderMapsUrlFormat);

        iniFile.AddSection(basicSection);
        iniFile.WriteIniFile(SettingsFile);
    }
}