using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;

public class QmUserSettingsService
{
    private QmUserSettings qmUserSettings;

    private static QmUserSettingsService instance;

    private QmUserSettingsService()
    {
            
    }

    public static QmUserSettingsService GetInstance() => instance ??= new QmUserSettingsService();

    public QmUserSettings GetSettings() => qmUserSettings ??= QmUserSettings.Load();

    public void SaveSettings() => qmUserSettings.Save();

    public void ClearAuthData() => qmUserSettings.ClearAuthData();
}