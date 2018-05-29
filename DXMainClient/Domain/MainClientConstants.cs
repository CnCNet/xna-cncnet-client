using ClientCore;
using System;
using System.IO;
using System.Windows.Forms;

namespace DTAClient.Domain
{
    public static class MainClientConstants
    {
        public const string CNCNET_TUNNEL_LIST_URL = "http://cncnet.org/master-list";

#if DEBUG
        public static string gamepath = Application.StartupPath + "\\";
#else
        public static string gamepath = Directory.GetParent(Application.StartupPath).FullName + "\\";
#endif

        public static string GAME_NAME_LONG = "CnCNet Client";
        public static string GAME_NAME_SHORT = "CnCNet";

        public static string CREDITS_URL = "http://rampastring.cncnet.org/TS/Credits.txt";

        public static string SUPPORT_URL_SHORT = "www.cncnet.org";

        public static int MAP_CELL_SIZE_X = 48;
        public static int MAP_CELL_SIZE_Y = 24;

        public static OSVersion OSId = OSVersion.UNKNOWN;

        public static void Initialize()
        {
            var clientConfiguration = ClientConfiguration.Instance;

            OSId = clientConfiguration.GetOperatingSystemVersion();

            GAME_NAME_SHORT = clientConfiguration.LocalGame;
            GAME_NAME_LONG = clientConfiguration.LongGameName;

            SUPPORT_URL_SHORT = clientConfiguration.ShortSupportURL;

            CREDITS_URL = clientConfiguration.CreditsURL;

            MAP_CELL_SIZE_X = clientConfiguration.MapCellSizeX;
            MAP_CELL_SIZE_Y = clientConfiguration.MapCellSizeY;
        }
    }
}
