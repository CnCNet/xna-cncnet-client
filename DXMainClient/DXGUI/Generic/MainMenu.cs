using ClientCore;
using ClientGUI;
using DTAConfig;
using DTAClient.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Diagnostics;
using Updater;
using SkirmishLobby = DTAClient.DXGUI.Multiplayer.GameLobby.SkirmishLobby;
using DTAClient.Online;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer;
using Microsoft.Xna.Framework.Media;

namespace DTAClient.DXGUI.Generic
{
    class MainMenu : XNAWindow, ISwitchable
    {
        private const float MEDIA_PLAYER_VOLUME_FADE_STEP = 0.01f;
        private const float MEDIA_PLAYER_VOLUME_EXIT_FADE_STEP = 0.025f;
        private const double UPDATE_RE_CHECK_THRESHOLD = 30.0;

        public MainMenu(WindowManager windowManager, SkirmishLobby skirmishLobby,
            LANLobby lanLobby, TopBar topBar, OptionsWindow optionsWindow,
            CnCNetManager connectionManager) : base(windowManager)
        {
            this.skirmishLobby = skirmishLobby;
            this.lanLobby = lanLobby;
            this.topBar = topBar;
            this.connectionManager = connectionManager;
            this.optionsWindow = optionsWindow;
        }

        private MainMenuDarkeningPanel innerPanel;

        private XNALabel lblCnCNetPlayerCount;
        private XNALinkLabel lblUpdateStatus;
        private XNALabel lblVersion;

        private SkirmishLobby skirmishLobby;

        private LANLobby lanLobby;

        private CnCNetManager connectionManager;

        private OptionsWindow optionsWindow;

        private TopBar topBar;

        private bool updateInProgress = false;

        private DateTime lastUpdateCheckTime;

        private Song themeSong;

        private static readonly object locker = new object();

        private bool isMusicFading = false;

        private float musicVolume = 1.0f;

        public override void Initialize()
        {
            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;

            Name = "MainMenu";
            BackgroundTexture = AssetLoader.LoadTexture("MainMenu\\mainmenubg.png");
            ClientRectangle = new Rectangle(0, 0, BackgroundTexture.Width, BackgroundTexture.Height);

            WindowManager.CenterControlOnScreen(this);

            var btnNewCampaign = new XNAClientButton(WindowManager);
            btnNewCampaign.Name = "btnNewCampaign";
            btnNewCampaign.IdleTexture = AssetLoader.LoadTexture("MainMenu\\campaign.png");
            btnNewCampaign.HoverTexture = AssetLoader.LoadTexture("MainMenu\\campaign_c.png");
            btnNewCampaign.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnNewCampaign.LeftClick += BtnNewCampaign_LeftClick;
            btnNewCampaign.HotKey = Keys.C;

            var btnLoadGame = new XNAClientButton(WindowManager);
            btnLoadGame.Name = "btnLoadGame";
            btnLoadGame.IdleTexture = AssetLoader.LoadTexture("MainMenu\\loadmission.png");
            btnLoadGame.HoverTexture = AssetLoader.LoadTexture("MainMenu\\loadmission_c.png");
            btnLoadGame.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnLoadGame.LeftClick += BtnLoadGame_LeftClick;
            btnLoadGame.HotKey = Keys.L;

            var btnSkirmish = new XNAClientButton(WindowManager);
            btnSkirmish.Name = "btnSkirmish";
            btnSkirmish.IdleTexture = AssetLoader.LoadTexture("MainMenu\\skirmish.png");
            btnSkirmish.HoverTexture = AssetLoader.LoadTexture("MainMenu\\skirmish_c.png");
            btnSkirmish.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnSkirmish.LeftClick += BtnSkirmish_LeftClick;
            btnSkirmish.HotKey = Keys.S;

            var btnCnCNet = new XNAClientButton(WindowManager);
            btnCnCNet.Name = "btnCnCNet";
            btnCnCNet.IdleTexture = AssetLoader.LoadTexture("MainMenu\\cncnet.png");
            btnCnCNet.HoverTexture = AssetLoader.LoadTexture("MainMenu\\cncnet_c.png");
            btnCnCNet.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnCnCNet.LeftClick += BtnCnCNet_LeftClick;
            btnCnCNet.HotKey = Keys.M;

            var btnLan = new XNAClientButton(WindowManager);
            btnLan.Name = "btnLan";
            btnLan.IdleTexture = AssetLoader.LoadTexture("MainMenu\\lan.png");
            btnLan.HoverTexture = AssetLoader.LoadTexture("MainMenu\\lan_c.png");
            btnLan.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnLan.LeftClick += BtnLan_LeftClick;
            btnLan.HotKey = Keys.N;

            var btnOptions = new XNAClientButton(WindowManager);
            btnOptions.Name = "btnOptions";
            btnOptions.IdleTexture = AssetLoader.LoadTexture("MainMenu\\options.png");
            btnOptions.HoverTexture = AssetLoader.LoadTexture("MainMenu\\options_c.png");
            btnOptions.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnOptions.LeftClick += BtnOptions_LeftClick;
            btnOptions.HotKey = Keys.O;

            var btnMapEditor = new XNAClientButton(WindowManager);
            btnMapEditor.Name = "btnMapEditor";
            btnMapEditor.IdleTexture = AssetLoader.LoadTexture("MainMenu\\mapeditor.png");
            btnMapEditor.HoverTexture = AssetLoader.LoadTexture("MainMenu\\mapeditor_c.png");
            btnMapEditor.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnMapEditor.LeftClick += BtnMapEditor_LeftClick;
            btnMapEditor.HotKey = Keys.E;

            var btnStatistics = new XNAClientButton(WindowManager);
            btnStatistics.Name = "btnStatistics";
            btnStatistics.IdleTexture = AssetLoader.LoadTexture("MainMenu\\statistics.png");
            btnStatistics.HoverTexture = AssetLoader.LoadTexture("MainMenu\\statistics_c.png");
            btnStatistics.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnStatistics.LeftClick += BtnStatistics_LeftClick;
            btnStatistics.HotKey = Keys.T;

            var btnCredits = new XNAClientButton(WindowManager);
            btnCredits.Name = "btnCredits";
            btnCredits.IdleTexture = AssetLoader.LoadTexture("MainMenu\\credits.png");
            btnCredits.HoverTexture = AssetLoader.LoadTexture("MainMenu\\credits_c.png");
            btnCredits.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnCredits.LeftClick += BtnCredits_LeftClick;
            btnCredits.HotKey = Keys.R;

            var btnExtras = new XNAClientButton(WindowManager);
            btnExtras.Name = "btnExtras";
            btnExtras.IdleTexture = AssetLoader.LoadTexture("MainMenu\\extras.png");
            btnExtras.HoverTexture = AssetLoader.LoadTexture("MainMenu\\extras_c.png");
            btnExtras.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnExtras.LeftClick += BtnExtras_LeftClick;
            btnExtras.HotKey = Keys.E;

            var btnExit = new XNAClientButton(WindowManager);
            btnExit.Name = "btnExit";
            btnExit.IdleTexture = AssetLoader.LoadTexture("MainMenu\\exitgame.png");
            btnExit.HoverTexture = AssetLoader.LoadTexture("MainMenu\\exitgame_c.png");
            btnExit.HoverSoundEffect = AssetLoader.LoadSound("MainMenu\\button.wav");
            btnExit.LeftClick += BtnExit_LeftClick;

            XNALabel lblCnCNetStatus = new XNALabel(WindowManager);
            lblCnCNetStatus.Name = "lblCnCNetStatus";
            lblCnCNetStatus.Text = "DTA players on CnCNet:";
            lblCnCNetStatus.ClientRectangle = new Rectangle(12, 9, 0, 0);

            lblCnCNetPlayerCount = new XNALabel(WindowManager);
            lblCnCNetPlayerCount.Name = "lblCnCNetPlayerCount";
            lblCnCNetPlayerCount.Text = "-";

            lblVersion = new XNALabel(WindowManager);
            lblVersion.Name = "lblVersion";
            lblVersion.Text = CUpdater.GameVersion;

            lblUpdateStatus = new XNALinkLabel(WindowManager);
            lblUpdateStatus.Name = "lblUpdateStatus";
            lblUpdateStatus.LeftClick += LblUpdateStatus_LeftClick;
            lblUpdateStatus.ClientRectangle = new Rectangle(0, 0, 160, 20);

            AddChild(btnNewCampaign);
            AddChild(btnLoadGame);
            AddChild(btnSkirmish);
            AddChild(btnCnCNet);
            AddChild(btnLan);
            AddChild(btnOptions);
            AddChild(btnMapEditor);
            AddChild(btnStatistics);
            AddChild(btnCredits);
            AddChild(btnExtras);
            AddChild(btnExit);
            AddChild(lblCnCNetStatus);
            AddChild(lblCnCNetPlayerCount);

            if (!ClientConfiguration.Instance.ModMode)
            {
                AddChild(lblVersion);
                AddChild(lblUpdateStatus);

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

            WindowManager.GameClosing += WindowManager_GameClosing;

            skirmishLobby.Exited += SkirmishLobby_Exited;
            lanLobby.Exited += LanLobby_Exited;

            GameProcessLogic.GameProcessStarted += SharedUILogic_GameProcessStarted;
            GameProcessLogic.GameProcessStarting += SharedUILogic_GameProcessStarting;

            UserINISettings.Instance.SettingsSaved += SettingsSaved;

            CUpdater.Restart += CUpdater_Restart;
        }

        private void SharedUILogic_GameProcessStarting()
        {
            UserINISettings.Instance.ReloadSettings();

            try
            {
                optionsWindow.RefreshSettings();
            }
            catch (Exception ex)
            {
                Logger.Log("Refreshing settings failed! Exception message: " + ex.Message);
                // We don't want to show the dialog when starting a game
                //XNAMessageBox.Show(WindowManager, "Saving settings failed",
                //    "Saving settings failed! Error message: " + ex.Message);
            }
        }

        private void CUpdater_Restart(object sender, EventArgs e)
        {
            WindowManager.AddCallback(new Action(WindowManager.CloseGame), null);
        }

        private void SettingsSaved(object sender, EventArgs e)
        {
            musicVolume = (float)UserINISettings.Instance.ClientVolume;

            if (MainClientConstants.OSId != OSVersion.WINVISTA)
            {
                if (MediaPlayer.State == MediaState.Playing)
                {
                    if (!UserINISettings.Instance.PlayMainMenuMusic)
                        isMusicFading = true;
                }
                else if (topBar.GetTopMostPrimarySwitchable() == this &&
                    topBar.LastSwitchType == SwitchType.PRIMARY)
                {
                    PlayMusic();
                }
            }

            if (!connectionManager.IsConnected)
            {
                ProgramConstants.PLAYERNAME = UserINISettings.Instance.PlayerName;
                skirmishLobby.RefreshPlayerName();
            }
        }

        private void CheckIfFirstRun()
        {
            if (ClientConfiguration.Instance.LocalGame == "YR")
                return;

            if (UserINISettings.Instance.IsFirstRun)
            {
                UserINISettings.Instance.IsFirstRun.Value = false;
                UserINISettings.Instance.SaveSettings();

                var msgBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "Initial Installation",
                    string.Format("You have just installed {0}." + Environment.NewLine +
                    "It's highly recommended that you configure your settings before playing." +
                    Environment.NewLine + "Do you want to configure them now?", ClientConfiguration.Instance.LocalGame));
                msgBox.YesClicked += MsgBox_YesClicked;
                msgBox.NoClicked += MsgBox_NoClicked;
            }

            optionsWindow.PostInit();
        }

        private void MsgBox_NoClicked(object sender, EventArgs e)
        {
            var msgBox = (XNAMessageBox)sender;
            msgBox.YesClicked -= MsgBox_YesClicked;
            msgBox.NoClicked -= MsgBox_NoClicked;
        }

        private void MsgBox_YesClicked(object sender, EventArgs e)
        {
            var msgBox = (XNAMessageBox)sender;
            msgBox.YesClicked -= MsgBox_YesClicked;
            msgBox.NoClicked -= MsgBox_NoClicked;

            optionsWindow.Open();
        }

        private void SharedUILogic_GameProcessStarted()
        {
            MusicOff();
        }

        private void WindowManager_GameClosing(object sender, EventArgs e)
        {
            Clean();
        }

        private void SkirmishLobby_Exited(object sender, EventArgs e)
        {
            innerPanel.Hide();
            Visible = true;
            Enabled = true;

            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();
        }

        private void LanLobby_Exited(object sender, EventArgs e)
        {
            topBar.Enable();

            if (UserINISettings.Instance.AutomaticCnCNetLogin)
                connectionManager.Connect();

            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();
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

        /// <summary>
        /// Attemps to "clean" the client session in a nice way if the user closes the game.
        /// </summary>
        private void Clean()
        {
            CnCNetInfoController.DisableService();
            if (updateInProgress)
                CUpdater.TerminateUpdate = true;

            if (connectionManager.IsConnected)
                connectionManager.Disconnect();
        }

        public void PostInit()
        {
            themeSong = AssetLoader.LoadSong(ClientConfiguration.Instance.MainMenuMusicName);

            PlayMusic();

            if (!ClientConfiguration.Instance.ModMode)
            {
                if (UserINISettings.Instance.CheckForUpdates)
                {
                    CheckForUpdates();
                }
                else
                {
                    lblUpdateStatus.Text = "Click to check for updates.";
                }
            }

            CheckIfFirstRun();
        }

        #region Updating / versioning system

        private void UpdateWindow_UpdateFailed(object sender, UpdateFailureEventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = "Updating failed! Click to retry.";
            lblUpdateStatus.DrawUnderline = true;
            lblUpdateStatus.Enabled = true;
            updateInProgress = false;

            innerPanel.Show(null); // Darkening
            XNAMessageBox msgBox = new XNAMessageBox(WindowManager, "Update failed", 
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
            lblUpdateStatus.Text = "The update was cancelled. Click to retry.";
            lblUpdateStatus.DrawUnderline = true;
            lblUpdateStatus.Enabled = true;
            updateInProgress = false;
        }

        private void UpdateWindow_UpdateCompleted(object sender, EventArgs e)
        {
            innerPanel.Hide();
            lblUpdateStatus.Text = MainClientConstants.GAME_NAME_SHORT + " was succesfully updated to v." + CUpdater.GameVersion;
            lblVersion.Text = CUpdater.GameVersion;
            updateInProgress = false;
            lblUpdateStatus.Enabled = true;
            lblUpdateStatus.DrawUnderline = false;
        }

        private void LblUpdateStatus_LeftClick(object sender, EventArgs e)
        {
            Logger.Log(CUpdater.DTAVersionState.ToString());

            if (CUpdater.DTAVersionState == VersionState.OUTDATED || 
                CUpdater.DTAVersionState == VersionState.MISMATCHED ||
                CUpdater.DTAVersionState == VersionState.UNKNOWN ||
                CUpdater.DTAVersionState == VersionState.UPTODATE)
            {
                CheckForUpdates();
            }
        }

        /// <summary>
        /// Starts a check for updates.
        /// </summary>
        private void CheckForUpdates()
        {
            CUpdater.CheckForUpdates();
            lblUpdateStatus.Enabled = false;
            lblUpdateStatus.Text = "Checking for updates...";
            StatisticsSender.Instance.SendUpdate();
            lastUpdateCheckTime = DateTime.Now;
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
                lblUpdateStatus.DrawUnderline = false;
            }
            else if (CUpdater.DTAVersionState == VersionState.OUTDATED)
            {
                lblUpdateStatus.Text = "An update is available.";
                innerPanel.UpdateQueryWindow.SetInfo(CUpdater.ServerGameVersion, CUpdater.UpdateSizeInKb);
                innerPanel.Show(innerPanel.UpdateQueryWindow);
            }
            else if (CUpdater.DTAVersionState == VersionState.UNKNOWN)
            {
                lblUpdateStatus.Text = "Checking for updates failed! Click to retry.";
                lblUpdateStatus.Enabled = true;
                lblUpdateStatus.DrawUnderline = true;
            }
        }

        private void UpdateQueryWindow_UpdateDeclined(object sender, EventArgs e)
        {
            UpdateQueryWindow uqw = (UpdateQueryWindow)sender;
            innerPanel.Hide();
            lblUpdateStatus.Text = "An update is available, click to install.";
            lblUpdateStatus.Enabled = true;
            lblUpdateStatus.DrawUnderline = true;
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
            optionsWindow.Open();
        }

        private void BtnNewCampaign_LeftClick(object sender, EventArgs e)
        {
            innerPanel.Show(innerPanel.CampaignSelector);
        }

        private void BtnLoadGame_LeftClick(object sender, EventArgs e)
        {
            innerPanel.Show(innerPanel.GameLoadingWindow);
        }

        private void BtnLan_LeftClick(object sender, EventArgs e)
        {
            lanLobby.Open();

            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();

            topBar.Disable();
            if (connectionManager.IsConnected)
                connectionManager.Disconnect();
        }

        private void BtnCnCNet_LeftClick(object sender, EventArgs e)
        {
            topBar.SwitchToSecondary();
        }

        private void BtnSkirmish_LeftClick(object sender, EventArgs e)
        {
            skirmishLobby.Open();

            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();

            innerPanel.Show(null);
        }

        private void BtnMapEditor_LeftClick(object sender, EventArgs e)
        {
            Process.Start(ProgramConstants.GamePath + ClientConfiguration.Instance.MapEditorExePath);
        }

        private void BtnStatistics_LeftClick(object sender, EventArgs e)
        {
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
            FadeMusicExit();
            WindowManager.HideWindow();
        }

        private void SharedUILogic_GameProcessExited()
        {
            AddCallback(new Action(HandleGameProcessExited), null);
        }

        private void HandleGameProcessExited()
        {
            innerPanel.GameLoadingWindow.ListSaves();
            innerPanel.Hide();

            // If music is disabled on menus, check if the main menu is the top-most
            // window of the top bar and only play music if it is
            // LAN has the top bar disabled, so to detect the LAN game lobby
            // we'll check whether the top bar is enabled
            if (!UserINISettings.Instance.StopMusicOnMenu || 
                (topBar.Enabled && topBar.LastSwitchType == SwitchType.PRIMARY &&
                topBar.GetTopMostPrimarySwitchable() == this))
                PlayMusic();
        }

        public override void Update(GameTime gameTime)
        {
            if (isMusicFading)
                FadeMusic(gameTime);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            lock (locker)
            {
                base.Draw(gameTime);
            }
        }

        private void PlayMusic()
        {
            if (MainClientConstants.OSId == OSVersion.WINVISTA)
                return; // SharpDX fails at music playback on Vista

            if (themeSong != null && UserINISettings.Instance.PlayMainMenuMusic)
            {
                musicVolume = 1.0f;
                isMusicFading = false;
                MediaPlayer.Volume = AudioMaster.GetVolume();
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(themeSong);
            }
        }

        private void FadeMusic(GameTime gameTime)
        {
            if (!isMusicFading || themeSong == null)
                return;

            // Fade during 1 second
            float step = AudioMaster.GetVolume() * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (MediaPlayer.Volume > step)
                MediaPlayer.Volume -= step;
            else
            {
                MediaPlayer.Stop();
                isMusicFading = false;
            }
        }

        private void FadeMusicExit()
        {
            if (themeSong == null || MainClientConstants.OSId == OSVersion.WINVISTA)
            {
                Logger.Log("Exiting.");
                WindowManager.CloseGame();
                return;
            }

            if (MediaPlayer.Volume > MEDIA_PLAYER_VOLUME_EXIT_FADE_STEP * musicVolume)
            {
                MediaPlayer.Volume -= MEDIA_PLAYER_VOLUME_EXIT_FADE_STEP * musicVolume;
                AddCallback(new Action(FadeMusicExit), null);
            }
            else
            {
                MediaPlayer.Stop();
                Logger.Log("Exiting.");
                WindowManager.CloseGame();
            }
        }

        public void SwitchOn()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                PlayMusic();

            if (!ClientConfiguration.Instance.ModMode && UserINISettings.Instance.CheckForUpdates)
            {
                // Re-check for updates

                if ((DateTime.Now - lastUpdateCheckTime) > TimeSpan.FromSeconds(UPDATE_RE_CHECK_THRESHOLD))
                    CheckForUpdates();
            }
        }

        public void SwitchOff()
        {
            if (UserINISettings.Instance.StopMusicOnMenu)
                MusicOff();
        }

        private void MusicOff()
        {
            if (MainClientConstants.OSId != OSVersion.WINVISTA &&
                MediaPlayer.State == MediaState.Playing)
                isMusicFading = true;
        }

        public string GetSwitchName()
        {
            return "Main Menu";
        }
    }
}
