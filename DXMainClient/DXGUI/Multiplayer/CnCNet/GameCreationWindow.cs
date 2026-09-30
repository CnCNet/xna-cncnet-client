using ClientCore;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A window that allows the user to host a new game on CnCNet.
    /// </summary>
    class GameCreationWindow : XNAWindow
    {
        public GameCreationWindow(WindowManager windowManager, TunnelHandler tunnelHandler)
            : base(windowManager)
        {
            this.tunnelHandler = tunnelHandler;
        }

        public event EventHandler Cancelled;
        public event EventHandler<GameCreationEventArgs> GameCreated;
        public event EventHandler<GameCreationEventArgs> LoadedGameCreated;

        private XNATextBox tbGameName;
        private XNAClientDropDown ddMaxPlayers;
        private XNAClientDropDown ddSkillLevel;
        private XNATextBox tbPassword;

        private XNALabel lblRoomName;
        private XNALabel lblMaxPlayers;
        private XNALabel lblSkillLevel;
        private XNALabel lblPassword;

        private XNALabel lblTunnelServer;
        private XNADropDown ddTunnelMode;
        private TunnelListBox lbTunnelList;
        private XNAPanel pnlTunnelListDisabledOverlay;

        private XNAClientButton btnCreateGame;
        private XNAClientButton btnCancel;
        private XNAClientButton btnLoadMPGame;
        private XNAClientButton btnDisplayAdvancedOptions;

        private TunnelHandler tunnelHandler;

        private string[] SkillLevelOptions;

        public override void Initialize()
        {
            lbTunnelList = new TunnelListBox(WindowManager, tunnelHandler);
            lbTunnelList.Name = nameof(lbTunnelList);

            lbTunnelList.TargetVersion = (TunnelMode)UserINISettings.Instance.TunnelMode.Value == TunnelMode.V2Legacy ? 2 : 3;

            SkillLevelOptions = ClientConfiguration.Instance.GetSkillLevelOptions();

            Name = "GameCreationWindow";
            Width = lbTunnelList.Width + UIDesignConstants.EMPTY_SPACE_SIDES * 2 +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN * 2;
            BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");

            tbGameName = new XNATextBox(WindowManager);
            tbGameName.Name = nameof(tbGameName);
            tbGameName.MaximumTextLength = 23;
            tbGameName.ClientRectangle = new Rectangle(Width - 150 - UIDesignConstants.EMPTY_SPACE_SIDES -
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, UIDesignConstants.EMPTY_SPACE_TOP +
                UIDesignConstants.CONTROL_VERTICAL_MARGIN, 150, 21);
            tbGameName.Text = string.Format("{0}'s Game", ProgramConstants.PLAYERNAME);

            lblRoomName = new XNALabel(WindowManager);
            lblRoomName.Name = nameof(lblRoomName);
            lblRoomName.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, tbGameName.Y + 1, 0, 0);
            lblRoomName.Text = "Game room name:".L10N("Client:Main:GameRoomName");

            ddMaxPlayers = new XNAClientDropDown(WindowManager);
            ddMaxPlayers.Name = nameof(ddMaxPlayers);
            ddMaxPlayers.ClientRectangle = new Rectangle(tbGameName.X, tbGameName.Bottom + 20,
                tbGameName.Width, 21);
            for (int i = 8; i > 1; i--)
                ddMaxPlayers.AddItem(i.ToString());
            ddMaxPlayers.SelectedIndex = 0;

            lblMaxPlayers = new XNALabel(WindowManager);
            lblMaxPlayers.Name = nameof(lblMaxPlayers);
            lblMaxPlayers.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, ddMaxPlayers.Y + 1, 0, 0);
            lblMaxPlayers.Text = "Maximum number of players:".L10N("Client:Main:GameMaxPlayerCount");

            // Skill Level selector
            ddSkillLevel = new XNAClientDropDown(WindowManager);
            ddSkillLevel.Name = nameof(ddSkillLevel);
            ddSkillLevel.ClientRectangle = new Rectangle(tbGameName.X, ddMaxPlayers.Bottom + 20,
                tbGameName.Width, 21);

            for (int i = 0; i < SkillLevelOptions.Length; i++)
            {
                string skillLevel = SkillLevelOptions[i];
                string localizedSkillLevel = skillLevel.L10N($"INI:ClientDefinitions:SkillLevel:{i}");
                ddSkillLevel.AddItem(localizedSkillLevel);
            }

            ddSkillLevel.SelectedIndex = ClientConfiguration.Instance.DefaultSkillLevelIndex;

            lblSkillLevel = new XNALabel(WindowManager);
            lblSkillLevel.Name = nameof(lblSkillLevel);
            lblSkillLevel.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, ddSkillLevel.Y + 1, 0, 0);
            lblSkillLevel.Text = "Select preferred skill level of players:".L10N("Client:Main:SelectSkillLevel");

            tbPassword = new XNATextBox(WindowManager);
            tbPassword.Name = nameof(tbPassword);
            tbPassword.MaximumTextLength = 20;
            tbPassword.ClientRectangle = new Rectangle(tbGameName.X, ddSkillLevel.Bottom + 20,
                tbGameName.Width, 21);

            lblPassword = new XNALabel(WindowManager);
            lblPassword.Name = nameof(lblPassword);
            lblPassword.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, tbPassword.Y + 1, 0, 0);
            lblPassword.Text = "Password (leave blank for none):".L10N("Client:Main:PasswordTextBlankForNone");

            btnDisplayAdvancedOptions = new XNAClientButton(WindowManager);
            btnDisplayAdvancedOptions.Name = nameof(btnDisplayAdvancedOptions);
            btnDisplayAdvancedOptions.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, lblPassword.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 3, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnDisplayAdvancedOptions.Text = "Advanced Options".L10N("Client:Main:AdvancedOptions");
            btnDisplayAdvancedOptions.LeftClick += BtnDisplayAdvancedOptions_LeftClick;

            lblTunnelServer = new XNALabel(WindowManager);
            lblTunnelServer.Name = nameof(lblTunnelServer);
            lblTunnelServer.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, lblPassword.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 4, 0, 0);
            lblTunnelServer.Text = "Tunnel mode:".L10N("Client:Main:TunnelModeLabel");
            lblTunnelServer.Enabled = false;
            lblTunnelServer.Visible = false;

            ddTunnelMode = new XNADropDown(WindowManager);
            ddTunnelMode.Name = nameof(ddTunnelMode);
            ddTunnelMode.X = lblTunnelServer.X;
            ddTunnelMode.Y = lblTunnelServer.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            ddTunnelMode.Width = 220;
            ddTunnelMode.Height = UIDesignConstants.BUTTON_HEIGHT;
            ddTunnelMode.AddItem(new XNADropDownItem { Text = "Dynamic (V3)".L10N("Client:Main:TunnelSelModeDynamic"), Tag = TunnelMode.V3Dynamic });
            ddTunnelMode.AddItem(new XNADropDownItem { Text = "Static (V3)".L10N("Client:Main:TunnelSelModeStatic"), Tag = TunnelMode.V3Static });
            ddTunnelMode.AddItem(new XNADropDownItem { Text = "Legacy (V2)".L10N("Client:Main:TunnelSelModeLegacy"), Tag = TunnelMode.V2Legacy });
            ddTunnelMode.SelectedIndexChanged += DdTunnelMode_SelectedIndexChanged;

            lbTunnelList.X = UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            lbTunnelList.Y = ddTunnelMode.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            lbTunnelList.Disable();
            lbTunnelList.ListRefreshed += LbTunnelList_ListRefreshed;

            pnlTunnelListDisabledOverlay = new XNAPanel(WindowManager);
            pnlTunnelListDisabledOverlay.Name = nameof(pnlTunnelListDisabledOverlay);
            pnlTunnelListDisabledOverlay.ClientRectangle = lbTunnelList.ClientRectangle;
            pnlTunnelListDisabledOverlay.DrawBorders = false;
            pnlTunnelListDisabledOverlay.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            pnlTunnelListDisabledOverlay.Visible = false;

            btnCreateGame = new XNAClientButton(WindowManager);
            btnCreateGame.Name = nameof(btnCreateGame);
            btnCreateGame.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, btnDisplayAdvancedOptions.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 3,
                UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCreateGame.Text = "Create Game".L10N("Client:Main:CreateGame");
            btnCreateGame.LeftClick += BtnCreateGame_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.ClientRectangle = new Rectangle(Width - UIDesignConstants.BUTTON_WIDTH_133 - UIDesignConstants.EMPTY_SPACE_SIDES -
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, btnCreateGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            int btnLoadMPGameX = btnCreateGame.Right + (btnCancel.X - btnCreateGame.Right) / 2 - UIDesignConstants.BUTTON_WIDTH_133 / 2;

            btnLoadMPGame = new XNAClientButton(WindowManager);
            btnLoadMPGame.Name = nameof(btnLoadMPGame);
            btnLoadMPGame.ClientRectangle = new Rectangle(btnLoadMPGameX, btnCreateGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLoadMPGame.Text = "Load Game".L10N("Client:Main:LoadGame");
            btnLoadMPGame.LeftClick += BtnLoadMPGame_LeftClick;

            AddChild(tbGameName);
            AddChild(lblRoomName);
            AddChild(ddMaxPlayers);
            AddChild(lblMaxPlayers);
            AddChild(ddSkillLevel);
            AddChild(lblSkillLevel);
            AddChild(tbPassword);
            AddChild(lblPassword);
            AddChild(btnDisplayAdvancedOptions);
            AddChild(lblTunnelServer);
            AddChild(ddTunnelMode);
            AddChild(lbTunnelList);
            AddChild(pnlTunnelListDisabledOverlay);
            AddChild(btnCreateGame);
            if (!ClientConfiguration.Instance.DisableMultiplayerGameLoading)
                AddChild(btnLoadMPGame);
            AddChild(btnCancel);

            Height = btnCreateGame.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN + UIDesignConstants.EMPTY_SPACE_BOTTOM;

            base.Initialize();

            CenterOnParent();

            UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;

            if (UserINISettings.Instance.AlwaysDisplayTunnelList)
                BtnDisplayAdvancedOptions_LeftClick(this, EventArgs.Empty);
        }

        private void LbTunnelList_ListRefreshed(object sender, EventArgs e)
        {
            if (lbTunnelList.ItemCount == 0)
            {
                btnCreateGame.AllowClick = false;
                btnLoadMPGame.AllowClick = false;
            }
            else
            {
                btnCreateGame.AllowClick = true;
                btnLoadMPGame.AllowClick = AllowLoadingGame();
            }
        }

        private void Instance_SettingsSaved(object sender, EventArgs e)
        {
            tbGameName.Text = string.Format("{0}'s Game", UserINISettings.Instance.PlayerName.Value);
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void BtnLoadMPGame_LeftClick(object sender, EventArgs e)
        {
            string gameName = NameValidator.GetSanitizedGameName(tbGameName.Text);

            NameValidationError validationError = NameValidator.IsGameNameValid(gameName, out string errorMessage);
            if (validationError != NameValidationError.None)
            {
                XNAMessageBox.Show(WindowManager, "Invalid game name".L10N("Client:Main:InvalidGameName"),
                    errorMessage);
                return;
            }

            CnCNetTunnel selectedTunnel = null;
            if ((TunnelMode)UserINISettings.Instance.TunnelMode.Value != TunnelMode.V3Dynamic)
            {
                if (!lbTunnelList.IsValidIndexSelected())
                    return;
                selectedTunnel = lbTunnelList.GetSelectedTunnel();
            }

            IniFile spawnSGIni =
                new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));

            string password = Utilities.CalculateSHA1ForString(
                spawnSGIni.GetStringValue("Settings", "GameID", string.Empty)).Substring(0, 10);

            GameCreationEventArgs ea = new GameCreationEventArgs(gameName,
                spawnSGIni.GetIntValue("Settings", "PlayerCount", 2), password,
                selectedTunnel, ddSkillLevel.SelectedIndex);
            LoadedGameCreated?.Invoke(this, ea);
        }

        private void BtnCreateGame_LeftClick(object sender, EventArgs e)
        {
            string gameName = NameValidator.GetSanitizedGameName(tbGameName.Text);

            NameValidationError validationError = NameValidator.IsGameNameValid(gameName, out string errorMessage);
            if (validationError != NameValidationError.None)
            {
                XNAMessageBox.Show(WindowManager, "Invalid game name".L10N("Client:Main:InvalidGameName"),
                    errorMessage);
                return;
            }

            CnCNetTunnel selectedTunnel = null;
            if ((TunnelMode)UserINISettings.Instance.TunnelMode.Value != TunnelMode.V3Dynamic)
            {
                if (!lbTunnelList.IsValidIndexSelected())
                    return;
                selectedTunnel = lbTunnelList.GetSelectedTunnel();
            }

            GameCreated?.Invoke(this,
                new GameCreationEventArgs(gameName, int.Parse(ddMaxPlayers.SelectedItem.Text),
                    tbPassword.Text, selectedTunnel, ddSkillLevel.SelectedIndex)
            );
        }

        private void BtnDisplayAdvancedOptions_LeftClick(object sender, EventArgs e)
        {
            Name = "GameCreationWindow_Advanced";

            btnCreateGame.ClientRectangle = new Rectangle(btnCreateGame.X,
                lbTunnelList.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 3,
                btnCreateGame.Width, btnCreateGame.Height);

            btnCancel.ClientRectangle = new Rectangle(btnCancel.X,
                btnCreateGame.Y, btnCancel.Width, btnCancel.Height);

            btnLoadMPGame.ClientRectangle = new Rectangle(btnLoadMPGame.X,
                btnCreateGame.Y, btnLoadMPGame.Width, btnLoadMPGame.Height);

            Height = btnCreateGame.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN + UIDesignConstants.EMPTY_SPACE_BOTTOM;

            lblTunnelServer.Enable();
            ddTunnelMode.Enable();
            lbTunnelList.Visible = true;

            btnDisplayAdvancedOptions.Disable();

            SetAttributesFromIni();

            UpdateTunnelListState();

            CenterOnParent();
        }

        public void Refresh()
        {
            bool isAdvancedMode = Name == "GameCreationWindow_Advanced";

            lblTunnelServer.Visible = isAdvancedMode;
            ddTunnelMode.Visible = isAdvancedMode;
            lbTunnelList.Visible = isAdvancedMode;
            btnDisplayAdvancedOptions.Visible = !isAdvancedMode;

            var mode = (TunnelMode)UserINISettings.Instance.TunnelMode.Value;
            int selectedIndex = ddTunnelMode.Items.FindIndex(i => (TunnelMode)i.Tag == mode);
            if (selectedIndex == -1)
                selectedIndex = ddTunnelMode.Items.FindIndex(i => (TunnelMode)i.Tag == TunnelMode.V3Static);
            ddTunnelMode.SelectedIndex = selectedIndex;

            DdTunnelMode_SelectedIndexChanged(this, EventArgs.Empty);

            btnLoadMPGame.AllowClick = AllowLoadingGame();
        }

        private TunnelMode GetSelectedMode() => (TunnelMode)(ddTunnelMode.SelectedItem?.Tag ?? TunnelMode.V3Static);

        /// <summary>
        /// Applies the tunnel list's enabled state and the "disabled" dimming overlay for the
        /// currently selected tunnel mode. The overlay is only shown when the tunnel list itself
        /// is displayed, otherwise it would be drawn over the non-advanced window.
        /// </summary>
        private void UpdateTunnelListState()
        {
            bool isDynamic = GetSelectedMode() == TunnelMode.V3Dynamic;

            lbTunnelList.Enabled = !isDynamic;
            pnlTunnelListDisabledOverlay.Visible = isDynamic && lbTunnelList.Visible;
        }

        private void DdTunnelMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var mode = GetSelectedMode();

            UpdateTunnelListState();
            lbTunnelList.TargetVersion = mode == TunnelMode.V2Legacy ? 2 : 3;

            if ((TunnelMode)UserINISettings.Instance.TunnelMode.Value != mode)
            {
                UserINISettings.Instance.TunnelMode.Value = (int)mode;
                UserINISettings.Instance.SaveSettings();
            }

            LbTunnelList_ListRefreshed(this, EventArgs.Empty);
        }

        private bool AllowLoadingGame()
        {
            FileInfo savedGameSpawnIniFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI);

            if (!savedGameSpawnIniFile.Exists)
                return false;

            IniFile iniFile = new IniFile(savedGameSpawnIniFile.FullName);

            if (iniFile.GetStringValue("Settings", "Name", string.Empty) != ProgramConstants.PLAYERNAME)
                return false;

            if (!iniFile.GetBooleanValue("Settings", "Host", false))
                return false;

            return true;
        }
    }
}
