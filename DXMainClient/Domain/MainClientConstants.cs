using ClientCore;
using System.IO;
using System.Windows.Forms;

namespace DTAClient.Domain
{
    public static class MainClientConstants
    {
        public const string NEW_VERSION = "version";

#if DEBUG
        public static String gamepath = Application.StartupPath + "\\";
#else
        public static string gamepath = Directory.GetParent(Application.StartupPath).FullName + "\\";
#endif

        public static string GAME_NAME_LONG = "Tiberian Sun";
        public static string GAME_NAME_SHORT = "TS";
        public static string CNCNET_LIVE_STATUS_ID = "cncnet5_ts";

        public static string CHANGELOG_URL = "http://rampastring.cncnet.org/TSupdates/changelog.txt";
        public static string CREDITS_URL = "http://rampastring.cncnet.org/TS/Credits.txt";

        public static string SUPPORT_URL = "http://www.moddb.com/members/rampastring";
        public static string SUPPORT_URL_SHORT = "www.moddb.com/members/rampastring";

        public static int MAP_CELL_SIZE_X = 48;
        public static int MAP_CELL_SIZE_Y = 24;

        public static OSVersion OSId = OSVersion.UNKNOWN;

        public static bool AutoRunCnCNetClient = false;

        /// <summary>
        /// Initializes download mirrors and the custom component list.
        /// </summary>
        public static void Initialize()
        {
            var clientConfiguration = ClientConfiguration.Instance;

            OSId = clientConfiguration.GetOperatingSystemVersion();

            GAME_NAME_SHORT = clientConfiguration.LocalGame;
            GAME_NAME_LONG = clientConfiguration.LongGameName;

            SUPPORT_URL = clientConfiguration.LongSupportURL;
            SUPPORT_URL_SHORT = clientConfiguration.ShortSupportURL;

            CNCNET_LIVE_STATUS_ID = clientConfiguration.CnCNetLiveStatusIdentifier;

            CHANGELOG_URL = clientConfiguration.ChangelogURL;
            CREDITS_URL = clientConfiguration.CreditsURL;

            MAP_CELL_SIZE_X = clientConfiguration.MapCellSizeX;
            MAP_CELL_SIZE_Y = clientConfiguration.MapCellSizeY;
        }
    }
}
