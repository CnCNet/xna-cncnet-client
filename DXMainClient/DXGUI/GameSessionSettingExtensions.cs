#nullable enable

using Rampastring.Tools;

namespace DTAClient.DXGUI;

// TODO remove once the campaign launch builds a launch request
internal static class GameSessionSettingExtensions
{
    /// <summary>Writes the setting's spawn.ini option, if it has one, to [Settings].</summary>
    public static void ApplySpawnIniCode(this IGameSessionSetting setting, IniFile spawnIni)
    {
        if (setting.TryGetSpawnIniOption(out string key, out string value))
            spawnIni.SetStringValue("Settings", key, value);
    }
}
