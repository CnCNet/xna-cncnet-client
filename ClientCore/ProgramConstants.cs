using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using Rampastring.Tools;
using ClientCore.Extensions;

namespace ClientCore
{
    /// <summary>
    /// Contains various static variables and constants that the client uses for operation.
    /// </summary>
    public static class ProgramConstants
    {
        public static readonly string StartupExecutable = Assembly.GetEntryAssembly().Location;

        public static readonly string StartupPath = SafePath.CombineDirectoryPath(new FileInfo(StartupExecutable).Directory.FullName);

        public static readonly string GamePath = SafePath.CombineDirectoryPath(GetGamePath(StartupPath));

        public static string ClientUserFilesPath => SafePath.CombineDirectoryPath(GamePath, "Client");

        public static event EventHandler PlayerNameChanged;

        public const string QRES_EXECUTABLE = "qres.dat";

        public const string CNCNET_PROTOCOL_REVISION = "R12";
        public const string LAN_PROTOCOL_REVISION = "RL7";
        public const int LAN_PORT = 1234;
        public const int LAN_INGAME_PORT = 1234;
        public const int LAN_LOBBY_PORT = 1232;
        public const int LAN_GAME_LOBBY_PORT = 1233;
        public const char LAN_DATA_SEPARATOR = (char)01;
        public const char LAN_MESSAGE_SEPARATOR = (char)02;

        public const string SPAWNMAP_INI = "spawnmap.ini";
        public const string SPAWNER_SETTINGS = "spawn.ini";
        public const string SAVED_GAME_SPAWN_INI = "Saved Games/spawnSG.ini";

        /// <summary>
        /// The locale code that corresponds to the language the hardcoded client strings are in.
        /// </summary>
        public const string HARDCODED_LOCALE_CODE = "en";

        /// <summary>
        /// Used to denote <see cref="Environment.NewLine"/> in the INI files.
        /// </summary>
        /// <remarks>
        /// Historically Westwood used '@' for this purpose, so we keep it for compatibility.
        /// </remarks>
        public const string INI_NEWLINE_PATTERN = "@";

        public const int GAME_ID_MAX_LENGTH = 4;

        public static readonly Encoding LAN_ENCODING = Encoding.UTF8;

#if NETFRAMEWORK
        private static bool? isMono;

        /// <summary>
        /// Gets a value whether or not the application is running under Mono. Uses lazy loading and caching.
        /// </summary>
        public static bool ISMONO => isMono ??= Type.GetType("Mono.Runtime") != null;
#endif

        public static string GAME_VERSION = "Undefined";
        private static string PlayerName = "No name";

        public static string PLAYERNAME
        {
            get { return PlayerName; }
            set
            {
                string oldPlayerName = PlayerName;
                PlayerName = value;
                if (oldPlayerName != PlayerName)
                    PlayerNameChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static string BASE_RESOURCE_PATH = "Resources";
        public static string RESOURCES_DIR = BASE_RESOURCE_PATH;

        public static int LOG_LEVEL = 1;

        public static bool IsInGame { get; set; }

        public static string GetResourcePath()
        {
            return SafePath.CombineDirectoryPath(GamePath, RESOURCES_DIR);
        }

        public static string GetBaseResourcePath()
        {
            return SafePath.CombineDirectoryPath(GamePath, BASE_RESOURCE_PATH);
        }

        public const string GAME_INVITE_CTCP_COMMAND = "INVITE";
        public const string GAME_INVITATION_FAILED_CTCP_COMMAND = "INVITATION_FAILED";

        public static string GetAILevelName(int aiLevel)
        {
            if (aiLevel > -1 && aiLevel < AI_PLAYER_NAMES.Count)
                return AI_PLAYER_NAMES[aiLevel];

            return "";
        }

        public static readonly List<string> TEAMS = new List<string> { "A", "B", "C", "D" };

        // Static fields might be initialized before the translation file is loaded. Change to readonly properties here.
        public static List<string> AI_PLAYER_NAMES => new List<string> { "Easy AI".L10N("Client:Main:EasyAIName"), "Medium AI".L10N("Client:Main:MediumAIName"), "Hard AI".L10N("Client:Main:HardAIName") };

        public static string LogFileName { get; set; }

        /// <summary>
        /// This method finds the "Resources" directory by traversing the directory tree upwards from the startup path.
        /// </summary>
        /// <remarks>
        /// This method is needed by both ClientCore and DXMainClient. However, since it is usually called at the very beginning,
        /// where DXMainClient could not refer to ClientCore, this method is copied to both projects.
        /// Remember to keep <see cref="ClientCore.ProgramConstants.SearchResourcesDir"/> and <see cref="DTAClient.Program.SearchResourcesDir"/> consistent if you have modified its source codes.
        /// </remarks>
        private static string SearchResourcesDir(string startupPath)
        {
            DirectoryInfo currentDir = new(startupPath);
            for (int i = 0; i < 3; i++)
            {
                // Determine if currentDir is the "Resources" folder
                if (currentDir.Name.ToLowerInvariant() == "Resources".ToLowerInvariant())
                    return currentDir.FullName;

                // Additional check. This makes developers to debug the client inside Visual Studio a little bit easier.
                DirectoryInfo resourcesDir = currentDir.GetDirectories("Resources", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (resourcesDir is not null)
                    return resourcesDir.FullName;

                currentDir = currentDir.Parent;
            }

            throw new Exception("Could not find Resources directory.");
        }

        private static string GetGamePath(string startupPath)
        {
            string resourceDir = SearchResourcesDir(startupPath);
            return new DirectoryInfo(resourceDir).Parent.FullName;
        }
    }
}