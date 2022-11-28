using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;

public class QmUserSettingsService
{
    private QmUserSettings qmUserSettings;

    public QmUserSettings GetSettings() => qmUserSettings ??= QmUserSettings.Load();

    public void SaveSettings() => qmUserSettings.Save();

    public void ClearAuthData() => qmUserSettings.ClearAuthData();
}