using ClientCore.Extensions;
using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore.Enums;
using ClientGUI;
using DTAClient.DXGUI.Generic.OptionPanels;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using ClientUpdater;
using DTAClient.Domain;

namespace DTAClient.DXGUI.Generic
{
    public class OptionsWindow : XNAWindow
    {
        public OptionsWindow(WindowManager windowManager, GameCollection gameCollection, DirectDrawWrapperManager directDrawWrapperManager) : base(windowManager)
        {
            this.gameCollection = gameCollection;
            this.directDrawWrapperManager = directDrawWrapperManager;
        }

        public event EventHandler OnForceUpdate;

        private XNAClientTabControl tabControl;

        private XNAOptionsPanel[] optionsPanels;
        private ComponentsPanel componentsPanel;

        private DisplayOptionsPanel displayOptionsPanel;
        private XNAControl topBar;

        private readonly GameCollection gameCollection;
        private readonly DirectDrawWrapperManager directDrawWrapperManager;

        public override void Initialize()
        {
            Name = "OptionsWindow";
            ClientRectangle = new Rectangle(0, 0, 576, 475);
            BackgroundTexture = AssetLoader.LoadTextureUncached("optionsbg.png");

            tabControl = new XNAClientTabControl(WindowManager);
            tabControl.Name = "tabControl";
            tabControl.ClientRectangle = new Rectangle(12, 12, 0, 23);
            tabControl.FontIndex = 1;
            tabControl.ClickSound = new EnhancedSoundEffect("button.wav");
            tabControl.AddTab("Display".L10N("Client:DTAConfig:TabDisplay"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Audio".L10N("Client:DTAConfig:TabAudio"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Game".L10N("Client:DTAConfig:TabGame"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("CnCNet".L10N("Client:DTAConfig:TabCnCNet"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Updater".L10N("Client:DTAConfig:TabUpdater"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.AddTab("Components".L10N("Client:DTAConfig:TabComponents"), UIDesignConstants.BUTTON_WIDTH_92);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 104,
                Height - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("Client:DTAConfig:ButtonCancel");
            btnCancel.LeftClick += BtnBack_LeftClick;

            var btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = "btnSave";
            btnSave.ClientRectangle = new Rectangle(12, btnCancel.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnSave.Text = "Save".L10N("Client:DTAConfig:ButtonSave");
            btnSave.LeftClick += BtnSave_LeftClick;

            displayOptionsPanel = new DisplayOptionsPanel(WindowManager, UserINISettings.Instance, directDrawWrapperManager);
            componentsPanel = new ComponentsPanel(WindowManager, UserINISettings.Instance);
            var updaterOptionsPanel = new UpdaterOptionsPanel(WindowManager, UserINISettings.Instance);
            updaterOptionsPanel.OnForceUpdate += (s, e) => { Disable(); OnForceUpdate?.Invoke(this, EventArgs.Empty); };

            optionsPanels = new XNAOptionsPanel[]
            {
                displayOptionsPanel,
                new AudioOptionsPanel(WindowManager, UserINISettings.Instance),
                new GameOptionsPanel(WindowManager, UserINISettings.Instance, topBar),
                new CnCNetOptionsPanel(WindowManager, UserINISettings.Instance, gameCollection),
                updaterOptionsPanel,
                componentsPanel
            };

            if (ClientConfiguration.Instance.ModMode || Updater.UpdateMirrors == null || Updater.UpdateMirrors.Count < 1)
            {
                tabControl.MakeUnselectable(4);
                tabControl.MakeUnselectable(5);
            }
            else if (Updater.CustomComponents == null || Updater.CustomComponents.Count < 1)
                tabControl.MakeUnselectable(5);

            foreach (var panel in optionsPanels)
            {
                AddChild(panel);
                panel.Load();
                panel.Disable();
            }

            optionsPanels[0].Enable();

            AddChild(tabControl);
            AddChild(btnCancel);
            AddChild(btnSave);

            base.Initialize();

            CenterOnParent();
        }

        public void SetTopBar(XNAControl topBar) => this.topBar = topBar;

        /// <summary>
        /// Parses extra options defined by the modder
        /// from an INI file. Called from XNAWindow.SetAttributesFromINI.
        /// </summary>
        /// <param name="iniFile">The INI file.</param>
        protected override void GetINIAttributes(IniFile iniFile)
        {
            base.GetINIAttributes(iniFile);

            foreach (var panel in optionsPanels)
                panel.ParseUserOptions(iniFile);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (var panel in optionsPanels)
                panel.Disable();

            optionsPanels[tabControl.SelectedTab].Enable();
            optionsPanels[tabControl.SelectedTab].RefreshPanel();
        }

        private void BtnBack_LeftClick(object sender, EventArgs e)
        {
            if (Updater.IsComponentDownloadInProgress())
            {
                var msgBox = new XNAMessageBox(WindowManager, "Downloads in progress".L10N("Client:DTAConfig:DownloadingTitle"),
                    ("Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu.\n\n" +
                    "Are you sure you want to continue?").L10N("Client:DTAConfig:DownloadingText"), XNAMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.YesClickedAction = ExitDownloadCancelConfirmation_YesClicked;

                return;
            }

            WindowManager.SoundPlayer.SetVolume(Convert.ToSingle(UserINISettings.Instance.ClientVolume));
            Disable();
        }

        private void ExitDownloadCancelConfirmation_YesClicked(XNAMessageBox messageBox)
        {
            componentsPanel.CancelAllDownloads();
            WindowManager.SoundPlayer.SetVolume(Convert.ToSingle(UserINISettings.Instance.ClientVolume));
            Disable();
        }

        private void BtnSave_LeftClick(object sender, EventArgs e)
        {
            if (Updater.IsComponentDownloadInProgress())
            {
                XNAMessageBox msgBox = new XNAMessageBox(WindowManager, "Downloads in progress".L10N("Client:DTAConfig:DownloadingTitle"),
                      ("Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu.\n\n" +
                      "Are you sure you want to continue?").L10N("Client:DTAConfig:DownloadingText"), XNAMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.YesClickedAction = SaveDownloadCancelConfirmation_YesClicked;

                return;
            }

            SaveSettings();
        }

        private void SaveDownloadCancelConfirmation_YesClicked(XNAMessageBox messageBox)
        {
            componentsPanel.CancelAllDownloads();

            SaveSettings();
        }

        private void SaveSettings()
        {
            if (RefreshOptionPanels())
                return;

            bool restartRequired = false;

            try
            {
                foreach (var panel in optionsPanels)
                    restartRequired = panel.Save() || restartRequired;

                UserINISettings.Instance.SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Log("Saving settings failed! Error message: " + ex.ToString());
                XNAMessageBox.Show(WindowManager, "Saving Settings Failed".L10N("Client:DTAConfig:SaveSettingFailTitle"),
                    "Saving settings failed! Error message:".L10N("Client:DTAConfig:SaveSettingFailText") + " " + ex.Message);
            }

            Disable();

            if (restartRequired)
            {
                var msgBox = new XNAMessageBox(WindowManager, "Restart Required".L10N("Client:DTAConfig:RestartClientTitle"),
                    ("The client needs to be restarted for some of the changes to take effect.\n\n" +
                    "Do you want to restart now?").L10N("Client:DTAConfig:RestartClientText"), XNAMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.YesClickedAction = RestartMsgBox_YesClicked;
            }
        }

        private void RestartMsgBox_YesClicked(XNAMessageBox messageBox) => WindowManager.RestartGame();

        /// <summary>
        /// Refreshes the option panels to account for possible
        /// changes that could affect theirs functionality.
        /// Shows the popup to inform the user if needed.
        /// </summary>
        /// <returns>A bool that determines whether the 
        /// settings values were changed.</returns>
        private bool RefreshOptionPanels()
        {
            bool optionValuesChanged = false;

            foreach (var panel in optionsPanels)
                optionValuesChanged = panel.RefreshPanel() || optionValuesChanged;

            if (optionValuesChanged)
            {
                XNAMessageBox.Show(WindowManager, "Setting Value(s) Changed".L10N("Client:DTAConfig:SettingChangedTitle"),
                    ("One or more setting values are\n" +
                    "no longer available and were changed.\n\n" +
                    "You may want to verify the new setting\n" +
                    "values in client's options window.").L10N("Client:DTAConfig:SettingChangedText"));

                return true;
            }

            return false;
        }

        public void RefreshSettings()
        {
            foreach (var panel in optionsPanels)
                panel.Load();

            RefreshOptionPanels();

            foreach (var panel in optionsPanels)
                panel.Save();

            UserINISettings.Instance.SaveSettings();
        }

        public void Open()
        {
            foreach (var panel in optionsPanels)
                panel.Load();

            RefreshOptionPanels();

            componentsPanel.Open();

            Enable();
        }

        public void ToggleMainMenuOnlyOptions(bool enable)
        {
            foreach (var panel in optionsPanels)
            {
                panel.ToggleMainMenuOnlyOptions(enable);
            }
        }

        public void SwitchToCustomComponentsPanel()
        {
            foreach (var panel in optionsPanels)
                panel.Disable();

            tabControl.SelectedTab = 5;
        }

        public void InstallCustomComponent(int id) => componentsPanel.InstallComponent(id);

        public void PostInit()
        {
            if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
                displayOptionsPanel.PostInit();
        }
    }
}
