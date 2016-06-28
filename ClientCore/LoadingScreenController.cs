using System;
using System.Collections.Generic;
using System.Text;

namespace ClientCore
{
    public static class LoadingScreenController
    {
        public static string GetLoadScreenName(int sideId)
        {
            bool success;
            string resolution = DomainController.Instance().GetCurrentGameRes(DomainController.Instance().GetWindowedStatus(), out success);
            string[] resArray = resolution.Split('x');

            int resHeight = Convert.ToInt32(resArray[1]);

            string loadingScreenName = ProgramConstants.BASE_RESOURCE_PATH + "l";
            if (resHeight < 480)
                loadingScreenName = loadingScreenName + "400";
            else if (resHeight < 600)
                loadingScreenName = loadingScreenName + "480";
            else
                loadingScreenName = loadingScreenName + "600";

            loadingScreenName = loadingScreenName + "s" + sideId;

            Random random = new Random();
            int randomInt = random.Next(1, 1 + DomainController.Instance().GetLoadScreenCount());

            loadingScreenName = loadingScreenName + Convert.ToString(randomInt);
            loadingScreenName = loadingScreenName + ".pcx";

            return loadingScreenName;
        }
    }
}
