using ClientCore;
using ClientGUI;
using DTAConfig;
using DTAClient.domain;
using DTAClient.domain.CnCNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Updater;
using SkirmishLobby = DTAClient.DXGUI.Multiplayer.GameLobby.SkirmishLobby;
using DTAClient.DXGUI.Multiplayer;

namespace DTAClient.DXGUI.Generic
{
    class MainMenu : DXWindow
    {
        public MainMenu(WindowManager windowManager, SkirmishLobby skirmishLobby,
            CnCNetLobby cncnetLobby) : base(windowManager)
        {
            isYR = DomainController.Instance().GetDefaultGame().ToUpper() == "YR";
            this.skirmishLobby = skirmishLobby;
            this.cncnetLobby = cncnetLobby;
        }

        bool isYR = false;

        MainMenuDarkeningPanel innerPanel;
        DXPanel mmUIPanel;

        DXLabel lblCnCNetPlayerCount;
        DXLabel lblUpdateStatus;
        DXLabel lblVersion;

        SkirmishLobby skirmishLobby;
        CnCNetLobby cncnetLobby;

        bool updateInProgress = false;

        private static readonly object locker = new object();

        public override void Initialize()
        {
            SharedUILogic.GameProcessExited += SharedUILogic_GameProcessExited;

            Name = "MainMenu";
            BackgroundTexture = AssetLoader.LoadTexture("MainMenu\\mainmenubg.png");

            mmUIPanel = new DXPanel(WindowManager);
            mmUIPanel.Name = "MainMenuUIPanel";
            Texture2D texture = BackgroundTexture;
            mmUIPanel.ClientRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            ClientRectangle = new Rectangle((WindowManager.RenderResolutionX - ClientRectangle.Width) / 2,
                (WindowManager.RenderResolutionY - ClientRectangle.Height) / 2,
                mmUIPanel.ClientRectangle.Width, mmUIPanel.ClientRectangle.Height);

            DXButton btnNewCampaign = new DXButton(WindowManager);
            btnNewCampaign.Name = "btnNewCampaign";
            btnNewCampaign.IdleTexture = AssetLoader.LoadTexture("MainMenu\\campaign.png");
            btnNewCampaign.HoverTexture = AssetLoader.LoadTexture("MainMenu\\campaign_c.png");
            btnNewCampaign.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnNewCampaign.LeftClick += BtnNewCampaign_LeftClick;
            btnNewCampaign.HotKey = Keys.C;

            DXButton btnLoadGame = new DXButton(WindowManager);
            btnLoadGame.Name = "btnLoadGame";
            btnLoadGame.IdleTexture = AssetLoader.LoadTexture("MainMenu\\loadmission.png");
            btnLoadGame.HoverTexture = AssetLoader.LoadTexture("MainMenu\\loadmission_c.png");
            btnLoadGame.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnLoadGame.LeftClick += BtnLoadGame_LeftClick;
            btnLoadGame.HotKey = Keys.L;

            DXButton btnSkirmish = new DXButton(WindowManager);
            btnSkirmish.Name = "btnSkirmish";
            btnSkirmish.IdleTexture = AssetLoader.LoadTexture("MainMenu\\skirmish.png");
            btnSkirmish.HoverTexture = AssetLoader.LoadTexture("MainMenu\\skirmish_c.png");
            btnSkirmish.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnSkirmish.LeftClick += BtnSkirmish_LeftClick;
            btnSkirmish.HotKey = Keys.S;

            DXButton btnCnCNet = new DXButton(WindowManager);
            btnCnCNet.Name = "btnCnCNet";
            btnCnCNet.IdleTexture = AssetLoader.LoadTexture("MainMenu\\cncnet.png");
            btnCnCNet.HoverTexture = AssetLoader.LoadTexture("MainMenu\\cncnet_c.png");
            btnCnCNet.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnCnCNet.LeftClick += BtnCnCNet_LeftClick;
            btnCnCNet.HotKey = Keys.M;

            DXButton btnLan = new DXButton(WindowManager);
            btnLan.Name = "btnLan";
            btnLan.IdleTexture = AssetLoader.LoadTexture("MainMenu\\lan.png");
            btnLan.HoverTexture = AssetLoader.LoadTexture("MainMenu\\lan_c.png");
            btnLan.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnLan.LeftClick += BtnLan_LeftClick;
            btnLan.HotKey = Keys.N;

            DXButton btnOptions = new DXButton(WindowManager);
            btnOptions.Name = "btnOptions";
            btnOptions.IdleTexture = AssetLoader.LoadTexture("MainMenu\\options.png");
            btnOptions.HoverTexture = AssetLoader.LoadTexture("MainMenu\\options_c.png");
            btnOptions.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnOptions.LeftClick += BtnOptions_LeftClick;
            btnOptions.HotKey = Keys.O;

            DXButton btnMapEditor = new DXButton(WindowManager);
            btnMapEditor.Name = "btnMapEditor";
            btnMapEditor.IdleTexture = AssetLoader.LoadTexture("MainMenu\\mapeditor.png");
            btnMapEditor.HoverTexture = AssetLoader.LoadTexture("MainMenu\\mapeditor_c.png");
            btnMapEditor.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnMapEditor.LeftClick += BtnMapEditor_LeftClick;
            btnMapEditor.HotKey = Keys.E;

            DXButton btnStatistics = new DXButton(WindowManager);
            btnStatistics.Name = "btnStatistics";
            btnStatistics.IdleTexture = AssetLoader.LoadTexture("MainMenu\\statistics.png");
            btnStatistics.HoverTexture = AssetLoader.LoadTexture("MainMenu\\statistics_c.png");
            btnStatistics.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnStatistics.LeftClick += BtnStatistics_LeftClick;
            btnStatistics.HotKey = Keys.T;

            DXButton btnCredits = new DXButton(WindowManager);
            btnCredits.Name = "btnCredits";
            btnCredits.IdleTexture = AssetLoader.LoadTexture("MainMenu\\credits.png");
            btnCredits.HoverTexture = AssetLoader.LoadTexture("MainMenu\\credits_c.png");
            btnCredits.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnCredits.LeftClick += BtnCredits_LeftClick;
            btnCredits.HotKey = Keys.R;

            DXButton btnExtras = new DXButton(WindowManager);
            btnExtras.Name = "btnExtras";
            btnExtras.IdleTexture = AssetLoader.LoadTexture("MainMenu\\extras.png");
            btnExtras.HoverTexture = AssetLoader.LoadTexture("MainMenu\\extras_c.png");
            btnExtras.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnExtras.LeftClick += BtnExtras_LeftClick;
            btnExtras.HotKey = Keys.E;

            DXButton btnExit = new DXButton(WindowManager);
            btnExit.Name = "btnExit";
            btnExit.IdleTexture = AssetLoader.LoadTexture("MainMenu\\exitgame.png");
            btnExit.HoverTexture = AssetLoader.LoadTexture("MainMenu\\exitgame_c.png");
            btnExit.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnExit.LeftClick += BtnExit_LeftClick;
            btnExit.HotKey = Keys.Escape;

            DXLabel lblCnCNetStatus = new DXLabel(WindowManager);
            lblCnCNetStatus.Name = "lblCnCNetStatus";
            lblCnCNetStatus.Text = "DTA players on CnCNet:";
            lblCnCNetStatus.ClientRectangle = new Rectangle(12, 9, 0, 0);

            lblCnCNetPlayerCount = new DXLabel(WindowManager);
            lblCnCNetPlayerCount.Name = "lblCnCNetPlayerCount";
            lblCnCNetPlayerCount.Text = "-";

            lblVersion = new DXLabel(WindowManager);
            lblVersion.Name = "lblVersion";
            lblVersion.Text = CUpdater.GameVersion;

            lblUpdateStatus = new DXLabel(WindowManager);
            lblUpdateStatus.Name = "lblUpdateStatus";
            lblUpdateStatus.LeftClick += LblUpdateStatus_LeftClick;
            lblUpdateStatus.ClientRectangle = new Rectangle(0, 0, 160, 20);

            AddChild(mmUIPanel);
            mmUIPanel.AddChild(btnNewCampaign);
            mmUIPanel.AddChild(btnLoadGame);
            mmUIPanel.AddChild(btnSkirmish);
            mmUIPanel.AddChild(btnCnCNet);
            mmUIPanel.AddChild(btnLan);
            mmUIPanel.AddChild(btnOptions);
            mmUIPanel.AddChild(btnMapEditor);
            mmUIPanel.AddChild(btnStatistics);
            mmUIPanel.AddChild(btnCredits);
            mmUIPanel.AddChild(btnExtras);
            mmUIPanel.AddChild(btnExit);
            mmUIPanel.AddChild(lblCnCNetStatus);
            mmUIPanel.AddChild(lblCnCNetPlayerCount);

            if (!MCDomainController.Instance.GetModModeStatus())
            {
                mmUIPanel.AddChild(lblVersion);
                mmUIPanel.AddChild(lblUpdateStatus);

                CUpdater.FileIdentifiersUpdated += CUpdater_FileIdentifiersUpdated;
            }

            innerPanel = new MainMenuDarkeningPanel(WindowManager);
            innerPanel.ClientRectangle = new Rectangle(0, 0, 
                ClientRectangle.Width,
                ClientRectangle.Height);
            AddChild(innerPanel);
            innerPanel.Hide();

            base.Initialize(); // Read control attributes from INI

            innerPanel.UpdateQueryWindow.UpdateDeclined += UpdateQueryWindow_UpdateDeclined;
            innerPanel.UpdateQueryWindow.UpdateAccepted += UpdateQueryWindow_UpdateAccepted;

            innerPanel.UpdateWindow.UpdateCompleted += UpdateWindow_UpdateCompleted;
            innerPanel.UpdateWindow.UpdateCancelled += UpdateWindow_UpdateCancelled;
            innerPanel.UpdateWindow.UpdateFailed += UpdateWindow_UpdateFailed;

            this.ClientRectangle = new Rectangle((WindowManager.RenderResolutionX - ClientRectangle.Width) / 2,
                (WindowManager.RenderResolutionY - ClientRectangle.Height) / 2,
                ClientRectangle.Width, ClientRectangle.Height);
            innerPanel.ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);

            CnCNetInfoController.CnCNetGameCountUpdated += CnCNetInfoController_CnCNetGameCountUpdated;
            CnCNetInfoController.InitializeService();

            WindowManager.GameFormClosing += Instance_GameFormClosing;

            skirmishLobby.VisibleChanged += SkirmishLobby_VisibleChanged;
            cncnetLobby.VisibleChanged += CncnetLobby_VisibleChanged;
        }

        private void CnCNetInfoController_CnCNetGameCountUpdated(object sender, PlayerCountEventArgs e)
        {
            lock (locker)
            {
                if (e.PlayerCount == -1)
                    lblCnCNetPlayerCount.Text = "N/A";
                else
                    lblCnCNetPlayerCount.Text = e.PlayerCount.ToString();
            }
        }

        private void SkirmishLobby_VisibleChanged(object sender, EventArgs e)
        {
            if (skirmishLobby.Visible)
                innerPanel.Show(null);
            else
                innerPanel.Hide();
        }

        private void CncnetLobby_VisibleChanged(object sender, EventArgs e)
        {
            if (cncnetLobby.Visible)
                innerPanel.Show(null);
            else
                innerPanel.Hide();
        }

        /// <summary>
        /// Attemps to "clean" the client session in a nice way if the user closes the game form. 
        /// </summary>
        private void Instance_GameFormClosing(object sender, System.Windows.Forms.FormClosingEventArgs eventArgs)
        {
            CnCNetInfoController.DisableService();
            if (updateInProgress)
                CUpdater.TerminateUpdate = true;
        }

        public void PostInit()
        {
            if (!MCDomainController.Instance.GetModModeStatus())
            {
                if (MCDomainController.Instance.GetAutomaticUpdateStatus())
                {
                    lblUpdateStatus.Text = "Checking for updates...";
                    lblUpdateStatus.Enabled = false;
                    CUpdater.CheckForUpdates();
                }
                else
                {
                    lblUpdateStatus.Text = "Click here to check for updates.";
                }
            }
        }

        #region Updating / versioning system

        private void MessageBox_YesClicked(object sender, EventArgs e)
        {
            CUpdater.CheckForUpdates();
            lblUpdateStatus.Text = "Checking for updates...";
            innerPanel.Hide();
        }

        private void MessageBox_NoClicked(object sender, EventArgs e)
        {
            innerPanel.Hide();
        }

        private void UpdateWindow_UpdateFailed(object sender, UpdateFailureEventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = "Updating failed!";
            updateInProgress = false;

            innerPanel.Show(null); // Darkening
            DXMessageBox msgBox = new DXMessageBox(Game, WindowManager, "Update failed", 
                string.Format("An error occured while updating. Returned error was: {0}" +
                Environment.NewLine + Environment.NewLine +
                "If you are connected to the Internet and your firewall isn't blocking" + Environment.NewLine +
                "{1}, and the issue is reproducible, contact us at " + Environment.NewLine + 
                "{2} for support.",
                e.Reason, CUpdater.CURRENT_LAUNCHER_NAME, MainClientConstants.SUPPORT_URL_SHORT), DXMessageBoxButtons.OK);
            msgBox.OKClicked += MsgBox_OKClicked;
            msgBox.Show();
        }

        private void MsgBox_OKClicked(object sender, EventArgs e)
        {
            innerPanel.Hide();
        }

        private void UpdateWindow_UpdateCancelled(object sender, EventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = "The update was cancelled.";
            updateInProgress = false;
        }

        private void UpdateWindow_UpdateCompleted(object sender, EventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = MainClientConstants.GAME_NAME_SHORT + " has been succesfully updated to v. " + CUpdater.GameVersion;
            lblVersion.Text = CUpdater.GameVersion;
            updateInProgress = false;
            lblUpdateStatus.Enabled = true;
        }

        private void LblUpdateStatus_LeftClick(object sender, EventArgs e)
        {
            Logger.Log(CUpdater.DTAVersionState.ToString());

            if (CUpdater.DTAVersionState == VersionState.OUTDATED || 
                CUpdater.DTAVersionState == VersionState.MISMATCHED ||
                CUpdater.DTAVersionState == VersionState.UNKNOWN)
            {
                CUpdater.CheckForUpdates();
                lblUpdateStatus.Enabled = false;
                lblUpdateStatus.Text = "Checking for updates...";
            }
        }

        /// <summary>
        /// Used for displaying the result of an update check in the UI.
        /// </summary>
        private void CUpdater_FileIdentifiersUpdated()
        {
            if (updateInProgress)
            {
                return;
            }

            if (CUpdater.DTAVersionState == VersionState.UPTODATE)
            {
                lblUpdateStatus.Text = MainClientConstants.GAME_NAME_SHORT + " is up to date.";
                lblUpdateStatus.Enabled = true;
            }
            else if (CUpdater.DTAVersionState == VersionState.OUTDATED)
            {
                lblUpdateStatus.Text = "An update is available.";
                innerPanel.UpdateQueryWindow.SetInfo(CUpdater.ServerGameVersion, CUpdater.UpdateSizeInKb);
                innerPanel.Show(innerPanel.UpdateQueryWindow);
            }
            else if (CUpdater.DTAVersionState == VersionState.UNKNOWN)
            {
                lblUpdateStatus.Text = "Checking for updates failed!";
                lblUpdateStatus.Enabled = true;
            }
        }

        private void UpdateQueryWindow_UpdateDeclined(object sender, EventArgs e)
        {
            UpdateQueryWindow uqw = (UpdateQueryWindow)sender;
            innerPanel.Hide();
            lblUpdateStatus.Text = "Click here to download the update.";
            lblUpdateStatus.Enabled = true;
        }

        private void UpdateQueryWindow_UpdateAccepted(object sender, EventArgs e)
        {
            innerPanel.Hide();
            innerPanel.UpdateWindow.SetData(CUpdater.ServerGameVersion);
            innerPanel.Show(innerPanel.UpdateWindow);
            lblUpdateStatus.Text = "Updating...";
            updateInProgress = true;
            CUpdater.StartAsyncUpdate();
        }

        #endregion

        private void BtnOptions_LeftClick(object sender, EventArgs e)
        {
            innerPanel.InstantShow();
            Game.RunOneFrame(); // Hacky, remove after the options menu has been rewritten to utilize MonoGame
            OptionsForm of = new OptionsForm();
            of.ShowDialog();
            of.Dispose();
            DomainController.Instance().ReloadSettings();
            int themeId = DomainController.Instance().GetSelectedThemeId();
            string resDir = "Resources\\" + DomainController.Instance().GetThemeInfoFromIndex(themeId)[1];

            if (ProgramConstants.RESOURCES_DIR != resDir)
            {
                ProgramConstants.RESOURCES_DIR = resDir;
                DomainController.Instance().ReloadSettings();
                SetAttributesFromIni();
            }

            MCDomainController.Instance.ReloadSettings();
            innerPanel.Hide();
        }

        public void Enable()
        {
            mmUIPanel.Enabled = true;
        }

        private void BtnNewCampaign_LeftClick(object sender, EventArgs e)
        {
            innerPanel.Show(innerPanel.CampaignSelector);
        }

        private void BtnLoadGame_LeftClick(object sender, EventArgs e)
        {
            innerPanel.Show(innerPanel.GameLoadingWindow);
            mmUIPanel.Enabled = false;
        }

        private void BtnLan_LeftClick(object sender, EventArgs e)
        {
            StartCnCNetClient("-LAN");
        }

        private void BtnCnCNet_LeftClick(object sender, EventArgs e)
        {
            cncnetLobby.Visible = true;
            cncnetLobby.Enabled = true;
        }

        private void BtnSkirmish_LeftClick(object sender, EventArgs e)
        {
            skirmishLobby.Visible = true;
            skirmishLobby.Enabled = true;
        }

        private void BtnMapEditor_LeftClick(object sender, EventArgs e)
        {
            Process.Start(ProgramConstants.GamePath + MCDomainController.Instance.GetMapEditorExePath());
        }

        private void BtnStatistics_LeftClick(object sender, EventArgs e)
        {
            mmUIPanel.Enabled = false;
            innerPanel.Show(innerPanel.StatisticsWindow);
        }

        private void BtnCredits_LeftClick(object sender, EventArgs e)
        {
            Process.Start(MainClientConstants.CREDITS_URL);
        }

        private void BtnExtras_LeftClick(object sender, EventArgs e)
        {
            innerPanel.Show(innerPanel.ExtrasWindow);
        }

        private void BtnExit_LeftClick(object sender, EventArgs e)
        {
            Logger.Log("Exiting.");

            CnCNetInfoController.DisableService();

            if (isYR)
                File.Delete(ProgramConstants.GamePath + "ddraw.dll");

            Game.Exit();
        }

        private void StartCnCNetClient(string commandLine)
        {
            SaveSettings();

            ProcessStartInfo startInfo = new ProcessStartInfo(MainClientConstants.gamepath + "cncnetclient.dat");
            startInfo.Arguments = "\"-RESDIR=" + ProgramConstants.RESOURCES_DIR.Remove(ProgramConstants.RESOURCES_DIR.Length - 1) + "\"";
            startInfo.Arguments = startInfo.Arguments + " -VER" + CUpdater.GameVersion;
            if (!String.IsNullOrEmpty(commandLine))
                startInfo.Arguments = startInfo.Arguments + " " + commandLine;
            startInfo.UseShellExecute = false;

            WindowManager.HideWindow();

            Process clientProcess = new Process();
            clientProcess.StartInfo = startInfo;

            clientProcess.Start();

            clientProcess.WaitForExit();

            MCDomainController.Instance.ReloadSettings();

            WindowManager.ShowWindow();
        }

        private void SaveSettings()
        {
            OptionsForm of = new OptionsForm();
            of.UpdateSettings();
        }

        private void SharedUILogic_GameProcessExited()
        {
            AddCallback(new Action(HandleGameProcessExited), null);
        }

        private void HandleGameProcessExited()
        {
            innerPanel.GameLoadingWindow.ListSaves();
            innerPanel.Hide();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            lock (locker)
            {
                base.Draw(gameTime);
            }
        }
    }
}
