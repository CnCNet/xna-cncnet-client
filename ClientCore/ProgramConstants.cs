using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
#if WINFORMS
using System.Windows.Forms;
#endif
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

#if DEBUG
        public static readonly string GamePath = SafePath.CombineDirectoryPath(SafePath.GetDirectory(StartupPath).Parent.Parent.FullName);
#else
        public static readonly string GamePath = SafePath.CombineDirectoryPath(SafePath.GetDirectory(StartupPath).Parent.Parent.Parent.FullName);
#endif

        public static string ClientUserFilesPath => SafePath.CombineDirectoryPath(GamePath, "Client");
        public static string CREDITS_URL = string.Empty;
        public static bool USE_ISOMETRIC_CELLS = true;
        public static int TDRA_WAYPOINT_COEFFICIENT = 128;
        public static int MAP_CELL_SIZE_X = 48;
        public static int MAP_CELL_SIZE_Y = 24;
        public static OSVersion OSId = OSVersion.UNKNOWN;

        public static event EventHandler PlayerNameChanged;

        public const string QRES_EXECUTABLE = "qres.dat";

        public const string CNCNET_PROTOCOL_REVISION = "R11";
        public const string LAN_PROTOCOL_REVISION = "RL7";
        public const int LAN_INGAME_PORT = 1234;
        public const int LAN_LOBBY_PORT = 1232;
        public const int LAN_GAME_LOBBY_PORT = 1233;
        public const char LAN_DATA_SEPARATOR = (char)01;
        public const char LAN_MESSAGE_SEPARATOR = (char)02;

        public const string SPAWNMAP_INI = "spawnmap.ini";
        public const string SPAWNER_SETTINGS = "spawn.ini";
        public const string SAVED_GAME_SPAWN_INI = SAVED_GAMES_DIRECTORY + "/spawnSG.ini";
        public const string SAVED_GAMES_DIRECTORY = "Saved Games";

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

        public const string REPLAYS_DIRECTORY = "Replays";
        public const string CNCNET_TUNNEL_LIST_URL = "https://cncnet.org/api/v1/master-list";
        public const string CNCNET_DYNAMIC_TUNNELS = "DYNAMIC";
        public const int GAME_ID_MAX_LENGTH = 4;

        public static readonly Encoding LAN_ENCODING = Encoding.UTF8;

        public static string GAME_VERSION = "Undefined";
        public static string GAME_NAME_LONG = "CnCNet Client";
        public static string GAME_NAME_SHORT = "CnCNet";
        public static string SUPPORT_URL_SHORT = "www.cncnet.org";
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
        /// Gets or sets the action to perform to notify the user of an error.
        /// </summary>
        public static Action<string, string, bool> DisplayErrorAction { get; set; } = (title, error, exit) =>
        {
            Logger.Log(FormattableString.Invariant($"{(title is null ? null : title + Environment.NewLine + Environment.NewLine)}{error}"));
#if WINFORMS
            MessageBox.Show(error, title, MessageBoxButtons.OK);
#else
            ProcessLauncher.StartShellProcess(LogFileName);
#endif

            if (exit)
                Environment.Exit(1);
        };

        /// <summary>
        /// Logs all details of an exception to the logfile without further action.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        /// /// <param name="message">Optional message to accompany the error.</param>
        public static void LogException(Exception ex, string message = null)
        {
            LogExceptionRecursive(ex, message);
        }

        private static void LogExceptionRecursive(Exception ex, string message = null, bool innerException = false)
        {
            if (!innerException)
                Logger.Log(message);
            else
                Logger.Log("InnerException info:");

            Logger.Log("Type: " + ex.GetType());
            Logger.Log("Message: " + ex.Message);
            Logger.Log("Source: " + ex.Source);
            Logger.Log("TargetSite.Name: " + ex.TargetSite?.Name);
            Logger.Log("Stacktrace: " + ex.StackTrace);

            if (ex is AggregateException aggregateException)
            {
                foreach (Exception aggregateExceptionInnerException in aggregateException.InnerExceptions)
                {
                    LogExceptionRecursive(aggregateExceptionInnerException, null, true);
                }
            }
            else if (ex.InnerException is not null)
            {
                LogExceptionRecursive(ex.InnerException, null, true);
            }
        }

        /// <summary>
        /// Logs all details of an exception to the logfile, notifies the user, and exits the application.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        public static void HandleException(Exception ex)
        {
            LogExceptionRecursive(ex, "KABOOOOOOM!!! Info:");

            string errorLogPath = SafePath.CombineFilePath(ClientUserFilesPath, "ClientCrashLogs", FormattableString.Invariant($"ClientCrashLog{DateTime.Now.ToString("_yyyy_MM_dd_HH_mm")}.txt"));
            bool crashLogCopied = false;

            try
            {
                DirectoryInfo crashLogsDirectoryInfo = SafePath.GetDirectory(ClientUserFilesPath, "ClientCrashLogs");

                if (!crashLogsDirectoryInfo.Exists)
                    crashLogsDirectoryInfo.Create();

                File.Copy(SafePath.CombineFilePath(ClientUserFilesPath, "client.log"), errorLogPath, true);
                crashLogCopied = true;
            }
            catch
            {
            }

            string error = string.Format("{0} has crashed. Error message:".L10N("Client:Main:FatalErrorText1") + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine + (crashLogCopied ?
                "A crash log has been saved to the following file:".L10N("Client:Main:FatalErrorText2") + " " + Environment.NewLine + Environment.NewLine +
                errorLogPath + Environment.NewLine + Environment.NewLine : string.Empty) +
                (crashLogCopied ? "If the issue is repeatable, contact the {1} staff at {2} and provide the crash log file.".L10N("Client:Main:FatalErrorText3") :
                "If the issue is repeatable, contact the {1} staff at {2}.".L10N("Client:Main:FatalErrorText4")),
                GAME_NAME_LONG,
                GAME_NAME_SHORT,
                SUPPORT_URL_SHORT);

            DisplayErrorAction("KABOOOOOOOM".L10N("Client:Main:FatalErrorTitle"), error, true);
        }
    }
}