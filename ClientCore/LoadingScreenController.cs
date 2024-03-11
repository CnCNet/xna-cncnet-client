using System;

using ClientCore.Settings;

using Rampastring.Tools;

namespace ClientCore;

public static class LoadingScreenController
{
    public static string GetLoadScreenName(string sideId)
    {
        int resHeight = UserINISettings.Instance.IngameScreenHeight;
        int randomInt = new Random().Next(1, 1 + ClientConfiguration.Instance.LoadingScreenCount);
        string resolutionText = resHeight < 480 ? "400" : resHeight < 600 ? "480" : "600";
        return SafePath.CombineFilePath(
            ProgramConstants.BASE_RESOURCE_PATH,
            FormattableString.Invariant($"l{resolutionText}s{sideId}{randomInt}.pcx")).Replace('\\', '/');
    }
}