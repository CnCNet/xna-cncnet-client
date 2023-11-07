using ClientCore;

namespace DTAClient.Domain
{
    public static class MainClientConstants
    {
        public const string CNCNET_TUNNEL_LIST_URL = "http://cncnet.org/master-list";

        public static string GAME_NAME_LONG = "CnCNet Client";
        public static string GAME_NAME_SHORT = "CnCNet";

        public static string CREDITS_URL = "http://rampastring.cncnet.org/TS/Credits.txt";

        public static string SUPPORT_URL_SHORT = "www.cncnet.org";

        public static bool USE_ISOMETRIC_CELLS = true;
        public static int TDRA_WAYPOINT_COEFFICIENT = 128;
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

            USE_ISOMETRIC_CELLS = clientConfiguration.UseIsometricCells;
            TDRA_WAYPOINT_COEFFICIENT = clientConfiguration.WaypointCoefficient;
            MAP_CELL_SIZE_X = clientConfiguration.MapCellSizeX;
            MAP_CELL_SIZE_Y = clientConfiguration.MapCellSizeY;

            if (string.IsNullOrEmpty(GAME_NAME_SHORT))
                throw new ClientConfigurationException("LocalGame is set to an empty value.");

            if (GAME_NAME_SHORT.Length > ProgramConstants.GAME_ID_MAX_LENGTH)
            {
                throw new ClientConfigurationException("LocalGame is set to a value that exceeds length limit of " +
                    ProgramConstants.GAME_ID_MAX_LENGTH + " characters.");
            }
        }
    }
}
