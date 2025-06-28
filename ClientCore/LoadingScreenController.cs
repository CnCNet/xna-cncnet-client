using System;
using Rampastring.Tools;

namespace ClientCore
{
    public static class LoadingScreenController
    {
        private static readonly Random _random = new Random();
        
        public static string GetLoadScreenName(string sideId)
        {
            int resHeight = UserINISettings.Instance.IngameScreenHeight;
            int randomInt = _random.Next(1, 1 + ClientConfiguration.Instance.LoadingScreenCount);
            
            string resolutionText = resHeight switch
            {
                < 480 => "400",
                < 600 => "480",
                _ => "600"
            };

            return SafePath.CombineFilePath(
                ProgramConstants.BASE_RESOURCE_PATH,
                FormattableString.Invariant($"l{resolutionText}s{sideId}{randomInt}.pcx")).Replace('\\', '/');
        }
    }
}