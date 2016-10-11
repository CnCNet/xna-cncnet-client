using ClientCore;
using System;
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

        public static OSVersion OSId = OSVersion.UNKNOWN;

        public static bool AutoRunCnCNetClient = false;

        /// <summary>
        /// Initializes download mirrors and the custom component list.
        /// </summary>
        public static void Initialize()
        {
            OSId = ClientConfiguration.Instance.GetOperatingSystemVersion();

            GAME_NAME_SHORT = ClientConfiguration.Instance.LocalGame;
            GAME_NAME_LONG = ClientConfiguration.Instance.LongGameName;

            SUPPORT_URL = ClientConfiguration.Instance.LongSupportURL;
            SUPPORT_URL_SHORT = ClientConfiguration.Instance.ShortSupportURL;

            CNCNET_LIVE_STATUS_ID = ClientConfiguration.Instance.CnCNetLiveStatusIdentifier;

            CHANGELOG_URL = ClientConfiguration.Instance.ChangelogURL;
            CREDITS_URL = ClientConfiguration.Instance.CreditsURL;
        }
    }
}
