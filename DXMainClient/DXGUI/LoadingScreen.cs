using ClientCore;
using ClientGUI;
using DTAClient.domain;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Updater;

namespace DTAClient.DXGUI
{
    public class LoadingScreen : DXWindow
    {
        public LoadingScreen(Game game, WindowManager windowManager) : base(game, windowManager)
        {

        }

        private static readonly object locker = new object();

        DXProgressBar progressBar;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";

            BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

            progressBar = new DXProgressBar(Game, WindowManager);
            progressBar.Name = "progressBar";
            progressBar.Maximum = 100;
            progressBar.ClientRectangle = new Rectangle(50, 430, 700, 35);
            progressBar.BorderColor = UISettings.WindowBorderColor;

            CenterOnParent();

            AddChild(progressBar);

            base.Initialize();
        }

        private void CUpdater_OnLocalFileVersionsChecked()
        {
            MainMenu mm = new MainMenu(Game, WindowManager);
            CUpdater.OnLocalFileVersionsChecked -= CUpdater_OnLocalFileVersionsChecked;
            WindowManager.Instance.AddAndInitializeControl(mm);
            WindowManager.Instance.RemoveControl(this);
            mm.PostInit();
        }

        private void CUpdater_LocalFileCheckProgressChanged(int checkedFileCount, int totalFileCount)
        {
            lock (locker)
            {
                progressBar.Maximum = totalFileCount;
                progressBar.Value = checkedFileCount;
            }
        }

        public void Start()
        {
            if (File.Exists(ProgramConstants.gamepath + MainClientConstants.NEW_VERSION) && !MCDomainController.Instance.GetModModeStatus())
            {
                CUpdater.LocalFileCheckProgressChanged += CUpdater_LocalFileCheckProgressChanged;
                CUpdater.OnLocalFileVersionsChecked += CUpdater_OnLocalFileVersionsChecked;

                Thread thread = new Thread(CUpdater.CheckLocalFileVersions);
                thread.Start();
            }
            else
            {
                CUpdater_OnLocalFileVersionsChecked();
            }
        }

        public override void Update(GameTime gameTime)
        {
            lock (locker)
                base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            lock (locker)
                base.Draw(gameTime);
        }
    }
}
