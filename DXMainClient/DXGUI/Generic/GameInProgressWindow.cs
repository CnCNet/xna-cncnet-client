using Microsoft.Xna.Framework;
using Rampastring.XNAUI.DXControls;
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
    /// Also enables power-saving (lowers FPS) while a game is in progress.
    /// </summary>
    public class GameInProgressWindow : DXPanel
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

            DXWindow window = new DXWindow(WindowManager);

            window.Name = "GameInProgressWindow";
            window.BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            window.ClientRectangle = new Rectangle(0, 0, 200, 100);

            DXLabel explanation = new DXLabel(WindowManager);
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
            if (File.Exists(ProgramConstants.GamePath + "EXCEPT.TXT"))
            {
                if (!Directory.Exists(ProgramConstants.GamePath + "Client\\ErrorLogs"))
                    Directory.CreateDirectory(ProgramConstants.GamePath + "Client\\ErrorLogs");

                Logger.Log("The game crashed! Copying EXCEPT.TXT file.");

                DateTime dtn = DateTime.Now;

                File.Copy(ProgramConstants.GamePath + "EXCEPT.TXT",
                    string.Format(ProgramConstants.GamePath + "Client\\ErrorLogs\\EXCEPT_{0}_{1}_{2}_{3}_{4}.TXT",
                    dtn.Day, dtn.Month, dtn.Year, dtn.Hour, dtn.Minute));
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
