using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using Updater;

namespace DTAConfig
{
    public class OptionsWindow : XNAWindow
    {
        public OptionsWindow(WindowManager windowManager, GameCollection gameCollection, XNAControl topBar) : base(windowManager)
        {
            this.gameCollection = gameCollection;
            this.topBar = topBar;
        }

        private XNAClientTabControl tabControl;

        private XNAOptionsPanel[] optionsPanels;
        private ComponentsPanel componentsPanel;

        private DisplayOptionsPanel displayOptionsPanel;
        private XNAControl topBar;

        private GameCollection gameCollection;

        public override void Initialize()
        {
            Name = "OptionsWindow";
            ClientRectangle = new Rectangle(0, 0, 576, 435);
            BackgroundTexture = AssetLoader.LoadTextureUncached("optionsbg.png");

            tabControl = new XNAClientTabControl(WindowManager);
            tabControl.Name = "tabControl";
            tabControl.ClientRectangle = new Rectangle(12, 12, 0, 23);
            tabControl.FontIndex = 1;
            tabControl.SoundOnClick = AssetLoader.LoadSound("button.wav");
            tabControl.AddTab("Display", 92);
            tabControl.AddTab("Audio", 92);
            tabControl.AddTab("Game", 92);
            tabControl.AddTab("CnCNet", 92);
            tabControl.AddTab("Updater", 92);
            tabControl.AddTab("Components", 92);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(ClientRectangle.Width - 104,
                ClientRectangle.Height - 35, 92, 23);
            btnCancel.Text = "Cancel";
            btnCancel.LeftClick += BtnBack_LeftClick;

            var btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = "btnSave";
            btnSave.ClientRectangle = new Rectangle(12, btnCancel.ClientRectangle.Y, 92, 23);
            btnSave.Text = "Save";
            btnSave.LeftClick += BtnSave_LeftClick;

            displayOptionsPanel = new DisplayOptionsPanel(WindowManager, UserINISettings.Instance);
            componentsPanel = new ComponentsPanel(WindowManager, UserINISettings.Instance);

            optionsPanels = new XNAOptionsPanel[]
            {
                displayOptionsPanel,
                new AudioOptionsPanel(WindowManager, UserINISettings.Instance),
                new GameOptionsPanel(WindowManager, UserINISettings.Instance, topBar),
                new CnCNetOptionsPanel(WindowManager, UserINISettings.Instance, gameCollection),
                new UpdaterOptionsPanel(WindowManager, UserINISettings.Instance),
                componentsPanel
            };

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

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (var panel in optionsPanels)
            {
                panel.Disable();
            }

            optionsPanels[tabControl.SelectedTab].Enable();
        }

        private void BtnBack_LeftClick(object sender, EventArgs e)
        {
            if (CustomComponent.IsDownloadInProgress())
            {
                var msgBox = new XNAMessageBox(WindowManager, "Downloads in progress",
                    "Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu." +
                    Environment.NewLine + Environment.NewLine +
                    "Are you sure you want to continue?", DXMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.NoClicked += ExitDownloadCancelConfirmation_NoClicked;
                msgBox.YesClicked += ExitDownloadCancelConfirmation_YesClicked;

                return;
            }

            Disable();
        }

        private void ExitDownloadCancelConfirmation_YesClicked(object sender, EventArgs e)
        {
            var msgBox = (XNAMessageBox)sender;
            msgBox.YesClicked -= ExitDownloadCancelConfirmation_YesClicked;
            msgBox.NoClicked -= SaveDownloadCancelConfirmation_NoClicked;

            componentsPanel.CancelAllDownloads();
            Disable();
        }

        private void ExitDownloadCancelConfirmation_NoClicked(object sender, EventArgs e)
        {
            var msgBox = (XNAMessageBox)sender;
            msgBox.YesClicked -= ExitDownloadCancelConfirmation_YesClicked;
            msgBox.NoClicked -= SaveDownloadCancelConfirmation_NoClicked;
        }

        private void BtnSave_LeftClick(object sender, EventArgs e)
        {
            if (CustomComponent.IsDownloadInProgress())
            {
                var msgBox = new XNAMessageBox(WindowManager, "Downloads in progress",
                    "Optional component downloads are in progress. The downloads will be cancelled if you exit the Options menu." +
                    Environment.NewLine + Environment.NewLine +
                    "Are you sure you want to continue?", DXMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.NoClicked += SaveDownloadCancelConfirmation_NoClicked;
                msgBox.YesClicked += SaveDownloadCancelConfirmation_YesClicked;

                return;
            }

            SaveSettings();
        }

        private void SaveDownloadCancelConfirmation_YesClicked(object sender, EventArgs e)
        {
            var msgBox = (XNAMessageBox)sender;
            msgBox.YesClicked -= SaveDownloadCancelConfirmation_YesClicked;
            msgBox.NoClicked -= SaveDownloadCancelConfirmation_NoClicked;

            componentsPanel.CancelAllDownloads();

            SaveSettings();
        }

        private void SaveDownloadCancelConfirmation_NoClicked(object sender, EventArgs e)
        {
            var msgBox = (XNAMessageBox)sender;
            msgBox.YesClicked -= SaveDownloadCancelConfirmation_YesClicked;
            msgBox.NoClicked -= SaveDownloadCancelConfirmation_NoClicked;
        }

        private void SaveSettings()
        {
            bool restartRequired = false;

            try
            {
                foreach (var panel in optionsPanels)
                {
                    restartRequired = panel.Save() || restartRequired;
                }

                UserINISettings.Instance.SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Log("Saving settings failed! Error message: " + ex.Message);
                XNAMessageBox.Show(WindowManager, "Saving Settings Failed",
                    "Saving settings failed! Error message: " + ex.Message);
            }

            Disable();

            if (restartRequired)
            {
                var msgBox = new XNAMessageBox(WindowManager, "Restart Required",
                    "The game needs to be restarted for some of the changes to take effect." +
                    Environment.NewLine + Environment.NewLine +
                    "Do you want to restart now?", DXMessageBoxButtons.YesNo);
                msgBox.Show();
                msgBox.YesClicked += RestartMsgBox_YesClicked;
                msgBox.NoClicked += RestartMsgBox_NoClicked;
            }
        }

        private void RestartMsgBox_NoClicked(object sender, EventArgs e)
        {
            var msgBox = (XNAMessageBox)sender;
            msgBox.YesClicked -= RestartMsgBox_YesClicked;
            msgBox.NoClicked -= RestartMsgBox_NoClicked;
        }

        private void RestartMsgBox_YesClicked(object sender, EventArgs e)
        {
            WindowManager.RestartGame();
        }

        public void RefreshSettings()
        {
            foreach (var panel in optionsPanels)
            {
                panel.Load();
                panel.Save();
            }

            UserINISettings.Instance.SaveSettings();
        }

        public void Open()
        {
            foreach (var panel in optionsPanels)
                panel.Load();

            componentsPanel.Open();

            Enable();
        }

        public void SwitchToCustomComponentsPanel()
        {
            foreach (var panel in optionsPanels)
            {
                panel.Disable();
            }

            tabControl.SelectedTab = 5;
        }

        public void InstallCustomComponent(int id)
        {
            componentsPanel.InstallComponent(id);
        }

        public void PostInit()
        {
#if !YR
            displayOptionsPanel.PostInit();
#endif
        }
    }
}
