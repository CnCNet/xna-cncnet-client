using Microsoft.Xna.Framework;
using Rampastring.XNAUI.XNAControls;
using Rampastring.Tools;
using System;
using ClientCore;
using Rampastring.XNAUI;
using ClientGUI;
using System.IO;

namespace DTAClient.DXGUI
{
    /// <summary>
    /// Displays a dialog in the client when a game is in progress.
    /// Also enables power-saving (lowers FPS) while a game is in progress,
    /// and performs various operations on game start and exit.
    /// </summary>
    public class GameInProgressWindow : XNAPanel
    {
        private const double FPS = 60.0;
        private const double POWER_SAVING_FPS = 5.0;

        public GameInProgressWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        private bool initialized = false;
        private bool deletingLogFilesFailed = false;
        private bool nativeCursorUsed = false;

        public override void Initialize()
        {
            if (initialized)
                throw new Exception("GameInProgressWindow cannot be initialized twice!");

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
            explanation.Text = "A game is in progress.";

            AddChild(window);

            window.AddChild(explanation);

            base.Initialize();

            GameProcessLogic.GameProcessStarted += SharedUILogic_GameProcessStarted;
            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;

            explanation.CenterOnParent();

            window.CenterOnParent();

            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / FPS);

            Visible = false;
            Enabled = false;
        }

        private void SharedUILogic_GameProcessStarted()
        {
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
            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / FPS);
            if (UserINISettings.Instance.MinimizeWindowsOnGameStart)
                WindowManager.MaximizeWindow();

            UserINISettings.Instance.ReloadSettings();

            if (deletingLogFilesFailed)
                return;

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

            try
            {
                if (!Directory.Exists(ProgramConstants.GamePath + "Client/ErrorLogs"))
                    Directory.CreateDirectory(ProgramConstants.GamePath + "Client/ErrorLogs");

                DateTime dtn = DateTime.Now;

                if (File.Exists(ProgramConstants.GamePath + "EXCEPT.TXT"))
                {
                    Logger.Log("The game crashed! Copying EXCEPT.TXT file.");

                    File.Copy(ProgramConstants.GamePath + "EXCEPT.TXT",
                        string.Format(ProgramConstants.GamePath + "Client/ErrorLogs/EXCEPT_{0}_{1}_{2}_{3}_{4}.TXT",
                        dtn.Day, dtn.Month, dtn.Year, dtn.Hour, dtn.Minute));
                }

                for (int i = 0; i < 8; i++)
                {
                    string syncFileName = "SYNC" + i + ".TXT";

                    if (File.Exists(ProgramConstants.GamePath + syncFileName))
                    {
                        Logger.Log("There was a sync error! Copying file " + syncFileName);

                        File.Copy(ProgramConstants.GamePath + syncFileName,
                            string.Format(ProgramConstants.GamePath + "Client/ErrorLogs/" + syncFileName + "_{0}_{1}_{2}_{3}_{4}.TXT",
                            dtn.Day, dtn.Month, dtn.Year, dtn.Hour, dtn.Minute));
                        File.Delete(ProgramConstants.GamePath + syncFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while checking for EXCEPT.TXT and SYNCX.TXT files. Message: " + ex.Message);
            }
        }
    }
}
