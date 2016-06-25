using Microsoft.Xna.Framework;
using Rampastring.XNAUI.XNAControls;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        const double FPS = 120.0;
        const double POWER_SAVING_FPS = 5.0;

        public GameInProgressWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        bool initialized = false;

        public override void Initialize()
        {
            if (initialized)
                throw new Exception("GameInProgressWindow cannot be initialized twice!");

            initialized = true;

            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
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

            SharedUILogic.GameProcessStarted += SharedUILogic_GameProcessStarted;
            SharedUILogic.GameProcessExited += SharedUILogic_GameProcessExited;

            explanation.CenterOnParent();

            window.CenterOnParent();

            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / FPS);

            Visible = false;
            Enabled = false;
        }

        private void SharedUILogic_GameProcessStarted()
        {
            File.Delete(ProgramConstants.GamePath + "EXCEPT.TXT");

            for (int i = 0; i < 8; i++)
                File.Delete(ProgramConstants.GamePath + "SYNC" + i + ".TXT");

            Visible = true;
            Enabled = true;
            WindowManager.Cursor.Visible = false;
            ProgramConstants.IsInGame = true;
            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / POWER_SAVING_FPS);
            WindowManager.MinimizeWindow();
        }

        private void SharedUILogic_GameProcessExited()
        {
            AddCallback(new Action(HandleGameProcessExited), null);
        }

        private void HandleGameProcessExited()
        {
            try
            {
                if (!Directory.Exists(ProgramConstants.GamePath + "Client\\ErrorLogs"))
                    Directory.CreateDirectory(ProgramConstants.GamePath + "Client\\ErrorLogs");

                DateTime dtn = DateTime.Now;

                if (File.Exists(ProgramConstants.GamePath + "EXCEPT.TXT"))
                {
                    Logger.Log("The game crashed! Copying EXCEPT.TXT file.");

                    File.Copy(ProgramConstants.GamePath + "EXCEPT.TXT",
                        string.Format(ProgramConstants.GamePath + "Client\\ErrorLogs\\EXCEPT_{0}_{1}_{2}_{3}_{4}.TXT",
                        dtn.Day, dtn.Month, dtn.Year, dtn.Hour, dtn.Minute));
                }

                for (int i = 0; i < 8; i++)
                {
                    string syncFileName = "SYNC" + i + ".TXT";

                    if (File.Exists(ProgramConstants.GamePath + syncFileName))
                    {
                        Logger.Log("There was a sync error! Copying file " + syncFileName);

                        File.Copy(ProgramConstants.GamePath + syncFileName,
                            string.Format(ProgramConstants.GamePath + "Client\\ErrorLogs\\" + syncFileName + "_{0}_{1}_{2}_{3}_{4}.TXT",
                            dtn.Day, dtn.Month, dtn.Year, dtn.Hour, dtn.Minute));
                        File.Delete(ProgramConstants.GamePath + syncFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while checking for EXCEPT.TXT and SYNCX.TXT files. Message: " + ex.Message);
            }

            Visible = false;
            Enabled = false;
            WindowManager.Cursor.Visible = true;
            ProgramConstants.IsInGame = false;
            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / FPS);
            WindowManager.MaximizeWindow();
        }
    }
}
