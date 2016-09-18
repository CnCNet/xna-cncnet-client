using System;
using System.IO;
using System.Windows.Forms;

namespace DTAClient.Domain
{
    public static class MainClientConstants
    {
        public static string GAME_SETTINGS = "SUN.ini";
        public const String NEW_VERSION = "version";

#if DEBUG
        public static String gamepath = Application.StartupPath + "\\";
#else
        public static String gamepath = Directory.GetParent(Application.StartupPath).FullName + "\\";
#endif

        public static String GAME_NAME_LONG = "Tiberian Sun";
        public static String GAME_NAME_SHORT = "TS";
        public static string CNCNET_LIVE_STATUS_ID = "cncnet5_ts";

        public const String UPDATE_EXECFILE = "updateexec";
        public static String FINALSUN_INI = "FinalTI\\FinalSun.ini";

        public static String CHANGELOG_URL = "http://rampastring.cncnet.org/TSupdates/changelog.txt";
        public static String CREDITS_URL = "http://rampastring.cncnet.org/TS/Credits.txt";

        public static String SUPPORT_URL = "http://www.moddb.com/members/rampastring";
        public static String SUPPORT_URL_SHORT = "www.moddb.com/members/rampastring";

        public static OSVersion OSId = OSVersion.UNKNOWN;

        public static bool IsAutomaticInstallation = false;
        public static bool AutoRunCnCNetClient = false;
        public static bool IgnoreVersionMismatch = false;

        /// <summary>
        /// Initializes download mirrors and the custom component list.
        /// </summary>
        public static void Initialize()
        {
            GAME_SETTINGS = ClientCore.DomainController.Instance().GetSettingsIniName();

            OSId = ClientCore.DomainController.Instance().GetOperatingSystemVersion();

            GAME_NAME_SHORT = MCDomainController.Instance.ShortGameName;
            GAME_NAME_LONG = MCDomainController.Instance.LongGameName;

            SUPPORT_URL = MCDomainController.Instance.LongSupportURL;
            SUPPORT_URL_SHORT = MCDomainController.Instance.ShortSupportURL;

            CNCNET_LIVE_STATUS_ID = MCDomainController.Instance.CnCNetLiveStatusIdentifier;

            CHANGELOG_URL = MCDomainController.Instance.ChangelogURL;
            CREDITS_URL = MCDomainController.Instance.CreditsURL;

            FINALSUN_INI = MCDomainController.Instance.FinalSunIniPath;
        }
    }
}
