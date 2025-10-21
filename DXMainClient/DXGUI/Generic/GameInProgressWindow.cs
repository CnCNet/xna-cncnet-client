using Rampastring.XNAUI.XNAControls;
using Rampastring.Tools;
using System;
using ClientCore;
using Rampastring.XNAUI;
using ClientGUI;
using System.IO;
using ClientCore.Extensions;
using ClientCore.Enums;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

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

        private List<string> debugSnapshotDirectories;
        private DateTime debugLogLastWriteTime;
        private bool deletingLogFilesFailed = false;

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
            explanation.Text = "A game is in progress.".L10N("Client:Main:GameInProgress");

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

            if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
            {
                try
                {
                    FileInfo debugLogFileInfo = SafePath.GetFile(ProgramConstants.GamePath, "debug", "debug.log");

                    if (debugLogFileInfo.Exists)
                        debugLogLastWriteTime = debugLogFileInfo.LastWriteTime;
                }
                catch { }
            }

        }

        private void SharedUILogic_GameProcessStarted()
        {
            if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
            {
                debugSnapshotDirectories = GetAllDebugSnapshotDirectories();
            }
            else
            {
                try
                {
                    SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "EXCEPT.TXT");

                    for (int i = 0; i < 8; i++)
                        SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "SYNC" + i + ".TXT");

                    deletingLogFilesFailed = false;
                }
                catch (Exception ex)
                {
                    Logger.Log("Exception when deleting error log files! Message: " + ex.ToString());
                    deletingLogFilesFailed = true;
                }
            }

            Visible = true;
            Enabled = true;
            WindowManager.Cursor.Visible = false;
            nativeCursorUsed = Game.IsMouseVisible;
            Game.IsMouseVisible = false;
            ProgramConstants.IsInGame = true;
            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / POWER_SAVING_FPS);
#if WINFORMS

            if (UserINISettings.Instance.MinimizeWindowsOnGameStart)
                WindowManager.MinimizeWindow();
#endif
        }

        private void SharedUILogic_GameProcessExited()
        {
            WindowManager.AddCallback(new Action(HandleGameProcessExited), null);
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

#if WINFORMS
            if (UserINISettings.Instance.MinimizeWindowsOnGameStart)
                WindowManager.MaximizeWindow();

#endif
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

            if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
            {
                Task.Run(ProcessScreenshots);

                // TODO: Ares debug log handling should be addressed in Ares DLL itself.
                // For now the following are handled here:
                // 1. Make a copy of syringe.log in debug snapshot directory on both crash and desync.
                // 2. Move SYNCX.txt from game directory to debug snapshot directory on desync.
                // 3. Make a debug snapshot directory & copy debug.log to it on desync even if full crash dump wasn't created.
                // 4. Handle the empty snapshot directories created on a crash if debug logging was disabled.

                string snapshotDirectory = GetNewestDebugSnapshotDirectory();
                bool snapshotCreated = snapshotDirectory != null;

                snapshotDirectory = snapshotDirectory ?? SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "debug", FormattableString.Invariant($"snapshot-{dtn.ToString("yyyyMMdd-HHmmss")}"));

                bool debugLogModified = false;
                FileInfo debugLogFileInfo = SafePath.GetFile(ProgramConstants.GamePath, "debug", "debug.log");
                DateTime lastWriteTime = new DateTime();

                if (debugLogFileInfo.Exists)
                    lastWriteTime = debugLogFileInfo.LastAccessTime;

                if (!lastWriteTime.Equals(debugLogLastWriteTime))
                {
                    debugLogModified = true;
                    debugLogLastWriteTime = lastWriteTime;
                }

                if (CopySyncErrorLogs(snapshotDirectory, null) || snapshotCreated)
                {
                    FileInfo snapShotDebugLogFileInfo = SafePath.GetFile(snapshotDirectory, "debug.log");

                    if (debugLogFileInfo.Exists && !snapShotDebugLogFileInfo.Exists && debugLogModified)
                        File.Copy(debugLogFileInfo.FullName, snapShotDebugLogFileInfo.FullName);

                    CopyErrorLog(snapshotDirectory, "syringe.log", null);
                }
            }
            else
            {
                if (deletingLogFilesFailed)
                    return;

                CopyErrorLog(SafePath.CombineDirectoryPath(ProgramConstants.ClientUserFilesPath, "GameCrashLogs"), "EXCEPT.TXT", dtn);
                CopySyncErrorLogs(SafePath.CombineDirectoryPath(ProgramConstants.ClientUserFilesPath, "SyncErrorLogs"), dtn);
            }
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
                FileInfo errorLogFileInfo = SafePath.GetFile(ProgramConstants.GamePath, filename);

                if (errorLogFileInfo.Exists)
                {
                    DirectoryInfo errorLogDirectoryInfo = SafePath.GetDirectory(directory);

                    if (!errorLogDirectoryInfo.Exists)
                        errorLogDirectoryInfo.Create();

                    Logger.Log("The game crashed! Copying " + filename + " file.");

                    string timeStamp = dateTime.HasValue ? dateTime.Value.ToString("_yyyy_MM_dd_HH_mm") : "";

                    string filenameCopy = Path.GetFileNameWithoutExtension(filename) +
                        timeStamp + Path.GetExtension(filename);

                    File.Copy(errorLogFileInfo.FullName, SafePath.CombineFilePath(directory, filenameCopy));
                    copied = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while checking for " + filename + " file. Message: " + ex.ToString());
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
                    FileInfo syncErrorLogFileInfo = SafePath.GetFile(ProgramConstants.GamePath, filename);

                    if (syncErrorLogFileInfo.Exists)
                    {
                        DirectoryInfo syncErrorLogDirectoryInfo = SafePath.GetDirectory(directory);

                        if (!syncErrorLogDirectoryInfo.Exists)
                            syncErrorLogDirectoryInfo.Create();

                        Logger.Log("There was a sync error! Copying file " + filename);

                        string timeStamp = dateTime.HasValue ? dateTime.Value.ToString("_yyyy_MM_dd_HH_mm") : "";

                        string filenameCopy = Path.GetFileNameWithoutExtension(filename) +
                            timeStamp + Path.GetExtension(filename);

                        File.Copy(syncErrorLogFileInfo.FullName, SafePath.CombineFilePath(directory, filenameCopy));
                        copied = true;
                        syncErrorLogFileInfo.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while checking for SYNCX.TXT files. Message: " + ex.ToString());
            }
            return copied;
        }

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
            var directories = new List<string>();

            try
            {
                directories.AddRange(Directory.GetDirectories(SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "debug"), "snapshot-*"));
            }
            catch { }

            return directories;
        }

        /// <summary>
        /// Converts BMP screenshots to PNG and copies them from game directory to Screenshots sub-directory.
        /// </summary>
        private void ProcessScreenshots()
        {
            IEnumerable<FileInfo> files = SafePath.GetDirectory(ProgramConstants.GamePath).EnumerateFiles("SCRN*.bmp");
            DirectoryInfo screenshotsDirectory = SafePath.GetDirectory(ProgramConstants.GamePath, "Screenshots");

            if (!screenshotsDirectory.Exists)
            {
                try
                {
                    screenshotsDirectory.Create();
                }
                catch (Exception ex)
                {
                    Logger.Log("ProcessScreenshots: An error occured trying to create Screenshots directory. Message: " + ex.ToString());
                    return;
                }
            }

            foreach (FileInfo file in files)
            {
                try
                {
                    using FileStream stream = file.OpenRead();
                    using var image = Image.Load(stream);
                    FileInfo newFile = SafePath.GetFile(screenshotsDirectory.FullName, FormattableString.Invariant($"{Path.GetFileNameWithoutExtension(file.FullName)}.png"));
                    using FileStream newFileStream = newFile.OpenWrite();

                    image.SaveAsPng(newFileStream);
                }
                catch (Exception ex)
                {
                    Logger.Log("ProcessScreenshots: Error occured when trying to save " + Path.GetFileNameWithoutExtension(file.FullName) + ".png. Message: " + ex.ToString());
                    continue;
                }

                Logger.Log("ProcessScreenshots: " + Path.GetFileNameWithoutExtension(file.FullName) + ".png has been saved to Screenshots directory.");
                file.Delete();
            }
        }
    }
}