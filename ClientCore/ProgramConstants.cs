/// @author Rampastring
/// http://www.moddb.com/members/rampastring

using System.IO;
using System.Windows.Forms;

namespace ClientCore
{
    /// <summary>
    /// Contains various kinds of static variables and constants that the client uses for operation.
    /// </summary>
    public static class ProgramConstants
    {
        public static string GamePath = Path.GetDirectoryName(Application.ExecutablePath).TrimEnd('\\') + @"\";

        public const string QRES_EXECUTABLE             = "qres.dat";

        public const string CNCNET_PROTOCOL_REVISION = "R3";
        public const string LAN_PROTOCOL_REVISION = "RL1";
        public const int LAN_PORT = 1234;
        public const int LAN_INGAME_PORT = 1235;

        public static string GAME_VERSION = "1.15";
        public static string PLAYERNAME = "No name";
        public static int CNCNET_PORT = 8054;
        public static int CNCNET_PORT_INDEX = 0;
        public static string CNCNET_SERVER = "irc.gamesurge.net";
        public const string SPAWNMAP_INI = "spawnmap.ini";
        public const string SPAWNER_SETTINGS = "spawn.ini";
        public static string BASE_RESOURCE_PATH = "Resources\\";
        public static string RESOURCES_DIR = ProgramConstants.BASE_RESOURCE_PATH;

        public static int LOG_LEVEL = 1;

        public static bool CNCNET_AUTOLOGIN = false;

        public static bool IsInGame = false;

        public static string GetResourcePath()
        {
            return GamePath + RESOURCES_DIR;
        }

        public static string GetBaseResourcePath()
        {
            return GamePath + BASE_RESOURCE_PATH;
        }
    }
}
