using System;
using Rampastring.Tools;

namespace ClientCore
{
    public static class LoadingScreenController
    {
        public static string GetLoadScreenName(string sideId)
        {
            int resHeight = UserINISettings.Instance.IngameScreenHeight;
            int randomInt = new Random().Next(1, 1 + ClientConfiguration.Instance.LoadingScreenCount);
            string resolutionText;

            if (resHeight < 480)
                resolutionText = "400";
            else if (resHeight < 600)
                resolutionText = "480";
            else
                resolutionText = "600";

            return SafePath.CombineFilePath(
                ProgramConstants.BASE_RESOURCE_PATH,
                FormattableString.Invariant($"l{resolutionText}s{sideId}{randomInt}.pcx")).Replace('\\', '/');
        }
    }
}