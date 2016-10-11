using System;

namespace ClientCore
{
    public static class LoadingScreenController
    {
        public static string GetLoadScreenName(int sideId)
        {
            int resHeight = UserINISettings.Instance.IngameScreenHeight;

            string loadingScreenName = ProgramConstants.BASE_RESOURCE_PATH + "l";
            if (resHeight < 480)
                loadingScreenName = loadingScreenName + "400";
            else if (resHeight < 600)
                loadingScreenName = loadingScreenName + "480";
            else
                loadingScreenName = loadingScreenName + "600";

            loadingScreenName = loadingScreenName + "s" + sideId;

            Random random = new Random();
            int randomInt = random.Next(1, 1 + ClientConfiguration.Instance.LoadingScreenCount);

            loadingScreenName = loadingScreenName + Convert.ToString(randomInt);
            loadingScreenName = loadingScreenName + ".pcx";

            return loadingScreenName;
        }
    }
}
