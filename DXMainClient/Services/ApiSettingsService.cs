using System.IO;
using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet;
using Rampastring.Tools;

namespace DTAClient.Services;

public class ApiSettingsService
{
    private static readonly string SettingsFile = ClientConfiguration.Instance.ApiIniPath;
    private ApiSettings apiSettings;

    public static ApiSettingsService Instance { get; set; }

    private const string UrlsSectionKey = "URLs";
    private const string BaseUrlKey = "Base";
    private const string LoginUrlKey = "Login";
    private const string RefreshUrlKey = "Refresh";
    private const string ServerStatusUrlKey = "ServerStatus";
    private const string GetUserAccountsUrlKey = "GetUserAccounts";
    private const string GetLaddersUrlKey = "GetLadders";
    private const string GetLadderMapsUrlKey = "GetLadderMaps";

    public static ApiSettingsService GetInstance() => Instance ??= new ApiSettingsService();

    public ApiSettings GetSettings() => apiSettings ??= LoadSettings();

    private static ApiSettings LoadSettings()
    {
        if (!File.Exists(SettingsFile))
        {
            Logger.Log($"API settings INI not found: {SettingsFile}");
            return null;
        }

        var settings = new ApiSettings();
        var iniFile = new IniFile(SettingsFile);
        LoadUrls(iniFile, settings);

        return settings;
    }

    private static void LoadUrls(IniFile iniFile, ApiSettings settings)
    {
        IniSection urlsSection = iniFile.GetSection(UrlsSectionKey);
        if (urlsSection == null)
            return;

        settings.BaseUrl = urlsSection.GetStringValue(BaseUrlKey, ApiSettings.DefaultBaseUrl);
        settings.LoginUrl = urlsSection.GetStringValue(LoginUrlKey, ApiSettings.DefaultLoginUrl);
        settings.RefreshUrl = urlsSection.GetStringValue(RefreshUrlKey, ApiSettings.DefaultRefreshUrl);
        settings.ServerStatusUrl = urlsSection.GetStringValue(ServerStatusUrlKey, ApiSettings.DefaultServerStatusUrl);
        settings.GetUserAccountsUrl = urlsSection.GetStringValue(GetUserAccountsUrlKey, ApiSettings.DefaultGetUserAccountsUrl);
        settings.GetLaddersUrl = urlsSection.GetStringValue(GetLaddersUrlKey, ApiSettings.DefaultGetLaddersUrl);
        settings.GetLadderMapsUrlFormat = urlsSection.GetStringValue(GetLadderMapsUrlKey, ApiSettings.DefaultGetLadderMapsUrl);
    }
}