using Microsoft.Xna.Framework;
using Rampastring.XNAUI.XNAControls;
using Rampastring.Tools;
using System;
using ClientCore;
using Rampastring.XNAUI;
using ClientGUI;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Localization;

namespace DTAClient.DXGUI
{
    /// <summary>
    /// Displays a dialog in the client when a game is in progress.
    /// Also enables power-saving (lowers FPS) while a game is in progress,
    /// and performs various operations on game start and exit.
    /// </summary>
    public class GameInProgressWindow : XNAPanel
    {
        private const double POWER_SAVING_FPS = 5.0;

        public GameInProgressWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        private bool initialized = false;
        private bool nativeCursorUsed = false;

#if ARES
        private List<string> debugSnapshotDirectories;
        private DateTime debugLogLastWriteTime;
#else
        private bool deletingLogFilesFailed = false;
#endif

        public override void Initialize()
        {
            if (initialized)
                throw new InvalidOperationException("GameInProgressWindow cannot be initialized twice!");

            initialized = true;

            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            DrawBorders = false;
            ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);

            XNAWindow window = new XNAWindow(WindowManager);

            window.Name = "GameInProgressWindow";
            window.BackgroundTexture = AssetLoader.LoadTexture("gameinprogresswindowbg.png");
            window.ClientRectangle = new Rectangle(0, 0, 200, 100);

            XNALabel explanation = new XNALabel(WindowManager);
            explanation.Text = "A game is in progress.".L10N("UI:Main:GameInProgress");

            AddChild(window);

            window.AddChild(explanation);

            base.Initialize();

            GameProcessLogic.GameProcessStarted += SharedUILogic_GameProcessStarted;
            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;

            explanation.CenterOnParent();

            window.CenterOnParent();

            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / UserINISettings.Instance.ClientFPS);

            Visible = false;
            Enabled = false;

#if ARES
            try
            {
                if (File.Exists(ProgramConstants.GamePath + "debug/debug.log"))
                    debugLogLastWriteTime = File.GetLastWriteTimeUtc(ProgramConstants.GamePath + "debug/debug.log");
            }
            catch { }
#endif
        }

        private void SharedUILogic_GameProcessStarted()
        {

#if ARES
            debugSnapshotDirectories = GetAllDebugSnapshotDirectories();
#else
            try
            {
                File.Delete(ProgramConstants.GamePath + "EXCEPT.TXT");

                for (int i = 0; i < 8; i++)
                    File.Delete(ProgramConstants.GamePath + "SYNC" + i + ".TXT");

                deletingLogFilesFailed = false;
            }
            catch (Exception ex)
            {
                Logger.Log("Exception when deleting error log files! Message: " + ex.Message);
                deletingLogFilesFailed = true;
            }
#endif

            Visible = true;
            Enabled = true;
            WindowManager.Cursor.Visible = false;
            nativeCursorUsed = Game.IsMouseVisible;
            Game.IsMouseVisible = false;
            ProgramConstants.IsInGame = true;
            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / POWER_SAVING_FPS);
            if (UserINISettings.Instance.MinimizeWindowsOnGameStart)
                WindowManager.MinimizeWindow();

        }

        private void SharedUILogic_GameProcessExited()
        {
            AddCallback(new Action(HandleGameProcessExited), null);
        }

        private void HandleGameProcessExited()
        {
            Visible = false;
            Enabled = false;
            if (nativeCursorUsed)
                Game.IsMouseVisible = true;
            else
                WindowManager.Cursor.Visible = true;
            ProgramConstants.IsInGame = false;
            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / UserINISettings.Instance.ClientFPS);
            if (UserINISettings.Instance.MinimizeWindowsOnGameStart)
                WindowManager.MaximizeWindow();

            UserINISettings.Instance.ReloadSettings();

            if (UserINISettings.Instance.BorderlessWindowedClient)
            {
                // Hack: Re-set graphics mode
                // Windows resizes our window if we're in fullscreen mode and
                // the in-game resolution is lower than the user's desktop resolution.
                // After the game exits, Windows doesn't properly re-size our window
                // back to cover the entire screen, which causes graphics to get
                // stretched and also messes up input handling since the window manager
                // still thinks it's using the original resolution.
                // Re-setting the graphics mode fixes it.
                GameClass.SetGraphicsMode(WindowManager);
            }

            DateTime dtn = DateTime.Now;

#if ARES
            Task.Factory.StartNew(ProcessScreenshots);

            // TODO: Ares debug log handling should be addressed in Ares DLL itself.
            // For now the following are handled here:
            // 1. Make a copy of syringe.log in debug snapshot directory on both crash and desync.
            // 2. Move SYNCX.txt from game directory to debug snapshot directory on desync.
            // 3. Make a debug snapshot directory & copy debug.log to it on desync even if full crash dump wasn't created.
            // 4. Handle the empty snapshot directories created on a crash if debug logging was disabled.

            string snapshotDirectory = GetNewestDebugSnapshotDirectory();
            bool snapshotCreated = snapshotDirectory != null;

            snapshotDirectory = snapshotDirectory ?? ProgramConstants.GamePath + "debug/snapshot-" +
                dtn.ToString("yyyyMMdd-HHmmss");

            bool debugLogModified = false;
            string debugLogPath = ProgramConstants.GamePath + "debug/debug.log";
            DateTime lastWriteTime = new DateTime();

            if (File.Exists(debugLogPath))
                lastWriteTime = File.GetLastWriteTimeUtc(debugLogPath);

            if (!lastWriteTime.Equals(debugLogLastWriteTime))
            {
                debugLogModified = true;
                debugLogLastWriteTime = lastWriteTime;
            }

            if (CopySyncErrorLogs(snapshotDirectory, null) || snapshotCreated)
            {
                if (File.Exists(debugLogPath) && !File.Exists(snapshotDirectory + "/debug.log") && debugLogModified)
                    File.Copy(debugLogPath, snapshotDirectory + "/debug.log");

                CopyErrorLog(snapshotDirectory, "syringe.log", null);
            }
#else
            if (deletingLogFilesFailed)
                return;

            CopyErrorLog(ProgramConstants.ClientUserFilesPath + "GameCrashLogs", "EXCEPT.TXT", dtn);
            CopySyncErrorLogs(ProgramConstants.ClientUserFilesPath + "SyncErrorLogs", dtn);
#endif
        }

        /// <summary>
        /// Attempts to copy a general error log from game directory to another directory.
        /// </summary>
        /// <param name="directory">Directory to copy error log to.</param>
        /// <param name="filename">Filename of the error log.</param>
        /// <param name="dateTime">Time to to apply as a timestamp to filename. Set to null to not apply a timestamp.</param>
        /// <returns>True if error log was copied, false otherwise.</returns>
        private bool CopyErrorLog(string directory, string filename, DateTime? dateTime)
        {
            bool copied = false;

            try
            {
                if (File.Exists(ProgramConstants.GamePath + filename))
                {
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    Logger.Log("The game crashed! Copying " + filename + " file.");

                    string timeStamp = dateTime.HasValue ? dateTime.Value.ToString("_yyyy_MM_dd_HH_mm") : "";

                    string filenameCopy = Path.GetFileNameWithoutExtension(filename) +
                        timeStamp + Path.GetExtension(filename);

                    File.Copy(ProgramConstants.GamePath + filename, directory + "/" + filenameCopy);
                    copied = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while checking for " + filename + " file. Message: " + ex.Message);
            }
            return copied;
        }

        /// <summary>
        /// Attempts to copy sync error logs from game directory to another directory.
        /// </summary>
        /// <param name="directory">Directory to copy sync error logs to.</param>
        /// <param name="dateTime">Time to to apply as a timestamp to filename. Set to null to not apply a timestamp.</param>
        /// <returns>True if any sync logs were copied, false otherwise.</returns>
        private bool CopySyncErrorLogs(string directory, DateTime? dateTime)
        {
            bool copied = false;

            try
            {
                for (int i = 0; i < 8; i++)
                {
                    string filename = "SYNC" + i + ".TXT";

                    if (File.Exists(ProgramConstants.GamePath + filename))
                    {
                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        Logger.Log("There was a sync error! Copying file " + filename);

                        string timeStamp = dateTime.HasValue ? dateTime.Value.ToString("_yyyy_MM_dd_HH_mm") : "";

                        string filenameCopy = Path.GetFileNameWithoutExtension(filename) +
                            timeStamp + Path.GetExtension(filename);

                        File.Copy(ProgramConstants.GamePath + filename, directory + "/" + filenameCopy);
                        copied = true;
                        File.Delete(ProgramConstants.GamePath + filename);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while checking for SYNCX.TXT files. Message: " + ex.Message);
            }
            return copied;
        }

#if ARES
        /// <summary>
        /// Returns the first debug snapshot directory found in Ares debug log directory that was created after last game launch and isn't empty.
        /// Additionally any empty snapshot directories encountered are deleted.
        /// </summary>
        /// <returns>Full path of the debug snapshot directory. If one isn't found, null is returned.</returns>
        private string GetNewestDebugSnapshotDirectory()
        {
            string snapshotDirectory = null;

            if (debugSnapshotDirectories != null)
            {
                var newDirectories = GetAllDebugSnapshotDirectories().Except(debugSnapshotDirectories);

                foreach (string directory in newDirectories)
                {
                    if (Directory.EnumerateFileSystemEntries(directory).Any())
                        snapshotDirectory = directory;
                    else
                    {
                        try
                        {
                            Directory.Delete(directory);
                        }
                        catch { }
                    }
                }
            }

            return snapshotDirectory;
        }

        /// <summary>
        /// Returns list of all debug snapshot directories in Ares debug logs directory.
        /// </summary>
        /// <returns>List of all debug snapshot directories in Ares debug logs directory. Empty list if none are found or an error was encountered.</returns>
        private List<string> GetAllDebugSnapshotDirectories()
        {
            List<string> directories = new List<string>();

            try
            {
                directories.AddRange(Directory.GetDirectories(ProgramConstants.GamePath + "debug", "snapshot-*"));
            }
            catch { }

            return directories;
        }

        /// <summary>
        /// Converts BMP screenshots to PNG and copies them from game directory to Screenshots sub-directory.
        /// </summary>
        private void ProcessScreenshots()
        {
            string[] filenames = Directory.GetFiles(ProgramConstants.GamePath, "SCRN*.bmp");
            string screenshotsDirectory = ProgramConstants.GamePath + "Screenshots";

            if (!Directory.Exists(screenshotsDirectory))
            {
                try
                {
                    Directory.CreateDirectory(screenshotsDirectory);
                }
                catch (Exception ex)
                {
                    Logger.Log("ProcessScreenshots: An error occured trying to create Screenshots directory. Message: " + ex.Message);
                    return;
                }
            }

            foreach (string filename in filenames)
            {
                try
                {
                    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(filename);
                    bitmap.Save(screenshotsDirectory + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename) +
                        ".png", System.Drawing.Imaging.ImageFormat.Png);
                    bitmap.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Log("ProcessScreenshots: Error occured when trying to save " + Path.GetFileNameWithoutExtension(filename) + ".png. Message: " + ex.Message);
                    continue;
                }

                Logger.Log("ProcessScreenshots: " + Path.GetFileNameWithoutExtension(filename) + ".png has been saved to Screenshots directory.");
                File.Delete(filename);
            }
        }
#endif
    }
}
