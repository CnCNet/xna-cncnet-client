using System;
using System.IO;

#if WINFORMS
using System.Windows.Forms;
#endif
using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain
{
    public static class MainClientConstants
    {
        public static string GAME_NAME_LONG = "CnCNet Client";
        public static string GAME_NAME_SHORT = "CnCNet";
        public static string SUPPORT_URL_SHORT = "www.cncnet.org";
        public static bool USE_ISOMETRIC_CELLS = true;
        public static int TDRA_WAYPOINT_COEFFICIENT = 128;
        public static int MAP_CELL_SIZE_X = 48;
        public static int MAP_CELL_SIZE_Y = 24;

        public static OSVersion OSId = OSVersion.UNKNOWN;

        // TODO: remove this variable after `Logger.Initialized` property is implemented by upstream
        public static bool LoggerInitialized { get; set; } = false;

        private static Action<string, string, bool> displayErrorAction = null;
        /// <summary>
        /// Gets or sets the action to perform to notify the user of an error.
        /// </summary>
        public static Action<string, string, bool> DisplayErrorAction
        {
            get => displayErrorAction ??= DefaultDisplayErrorAction;
            set => displayErrorAction = value;
        }

        /// <summary>
        /// Show an error in console as well as a Win32 MessageBox. For non-Windows platforms, this launches a text file in a GUI editor.
        /// This action handles errors when XNA windows are not initialized yet.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="error">The error.</param>
        /// <param name="exit">Whether the client exits.</param>
        public static void DefaultDisplayErrorAction(string title, string error, bool exit)
        {
            Console.WriteLine(title);
            Console.WriteLine();
            Console.WriteLine(error);

            if (LoggerInitialized)
                Logger.Log(FormattableString.Invariant($"{(title is null ? null : title + Environment.NewLine + Environment.NewLine)}{error}"));

#if WINFORMS
            MessageBox.Show(error, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
            if (LoggerInitialized)
                ProcessLauncher.StartShellProcess(ProgramConstants.LogFileName);
            else
            {
                string tempfile = SafePath.CombineFilePath(Path.GetTempPath(), "xna-cncnet-client-error.log");
                using (StreamWriter writer = new StreamWriter(tempfile))
                {
                    writer.WriteLine(title);
                    writer.WriteLine();
                    writer.WriteLine(error);
                }
                ProcessLauncher.StartShellProcess(tempfile);
            }
#endif

            if (exit)
                Environment.Exit(1);
        }

        public static void Initialize()
        {
            var clientConfiguration = ClientConfiguration.Instance;

            OSId = clientConfiguration.GetOperatingSystemVersion();

            GAME_NAME_SHORT = clientConfiguration.LocalGame;
            GAME_NAME_LONG = clientConfiguration.LongGameName;
            SUPPORT_URL_SHORT = clientConfiguration.ShortSupportURL;
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
