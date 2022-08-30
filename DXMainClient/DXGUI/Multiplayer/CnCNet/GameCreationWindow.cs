using ClientCore;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
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
        private XNATextBox tbPassword;

        private XNALabel lblRoomName;
        private XNALabel lblMaxPlayers;
        private XNALabel lblPassword;

        private XNALabel lblTunnelServer;
        private TunnelListBox lbTunnelList;

        private XNAClientButton btnCreateGame;
        private XNAClientButton btnCancel;
        private XNAClientButton btnLoadMPGame;
        private XNAClientButton btnDisplayAdvancedOptions;

        private TunnelHandler tunnelHandler;

        public override void Initialize()
        {
            lbTunnelList = new TunnelListBox(WindowManager, tunnelHandler);
            lbTunnelList.Name = nameof(lbTunnelList);

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
            tbGameName.Text = string.Format("{0}'s Game".L10N("UI:Main:GameOfPlayer"), ProgramConstants.PLAYERNAME);

            lblRoomName = new XNALabel(WindowManager);
            lblRoomName.Name = nameof(lblRoomName);
            lblRoomName.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, tbGameName.Y + 1, 0, 0);
            lblRoomName.Text = "Game room name:".L10N("UI:Main:GameRoomName");

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
            lblMaxPlayers.Text = "Maximum number of players:".L10N("UI:Main:GameMaxPlayerCount");

            tbPassword = new XNATextBox(WindowManager);
            tbPassword.Name = nameof(tbPassword);
            tbPassword.MaximumTextLength = 20;
            tbPassword.ClientRectangle = new Rectangle(tbGameName.X, ddMaxPlayers.Bottom + 20,
                tbGameName.Width, 21);

            lblPassword = new XNALabel(WindowManager);
            lblPassword.Name = nameof(lblPassword);
            lblPassword.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, tbPassword.Y + 1, 0, 0);
            lblPassword.Text = "Password (leave blank for none):".L10N("UI:Main:PasswordTextBlankForNone");

            btnDisplayAdvancedOptions = new XNAClientButton(WindowManager);
            btnDisplayAdvancedOptions.Name = nameof(btnDisplayAdvancedOptions);
            btnDisplayAdvancedOptions.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, lblPassword.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 3, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnDisplayAdvancedOptions.Text = "Advanced Options".L10N("UI:Main:AdvancedOptions");
            btnDisplayAdvancedOptions.LeftClick += BtnDisplayAdvancedOptions_LeftClick;

            lblTunnelServer = new XNALabel(WindowManager);
            lblTunnelServer.Name = nameof(lblTunnelServer);
            lblTunnelServer.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, lblPassword.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 4, 0, 0);
            lblTunnelServer.Text = "Tunnel server:".L10N("UI:Main:TunnelServer");
            lblTunnelServer.Enabled = false;
            lblTunnelServer.Visible = false;

            lbTunnelList.X = UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            lbTunnelList.Y = lblTunnelServer.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            lbTunnelList.Disable();
            lbTunnelList.ListRefreshed += LbTunnelList_ListRefreshed;

            btnCreateGame = new XNAClientButton(WindowManager);
            btnCreateGame.Name = nameof(btnCreateGame);
            btnCreateGame.ClientRectangle = new Rectangle(UIDesignConstants.EMPTY_SPACE_SIDES +
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, btnDisplayAdvancedOptions.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 3,
                UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCreateGame.Text = "Create Game".L10N("UI:Main:CreateGame");
            btnCreateGame.LeftClick += BtnCreateGame_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.ClientRectangle = new Rectangle(Width - UIDesignConstants.BUTTON_WIDTH_133 - UIDesignConstants.EMPTY_SPACE_SIDES -
                UIDesignConstants.CONTROL_HORIZONTAL_MARGIN, btnCreateGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            int btnLoadMPGameX = btnCreateGame.Right + (btnCancel.X - btnCreateGame.Right) / 2 - UIDesignConstants.BUTTON_WIDTH_133 / 2;

            btnLoadMPGame = new XNAClientButton(WindowManager);
            btnLoadMPGame.Name = nameof(btnLoadMPGame);
            btnLoadMPGame.ClientRectangle = new Rectangle(btnLoadMPGameX, btnCreateGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLoadMPGame.Text = "Load Game".L10N("UI:Main:LoadGame");
            btnLoadMPGame.LeftClick += BtnLoadMPGame_LeftClick;

            AddChild(tbGameName);
            AddChild(lblRoomName);
            AddChild(ddMaxPlayers);
            AddChild(lblMaxPlayers);
            AddChild(tbPassword);
            AddChild(lblPassword);
            AddChild(btnDisplayAdvancedOptions);
            AddChild(lblTunnelServer);
            AddChild(lbTunnelList);
            AddChild(btnCreateGame);
            if (!ClientConfiguration.Instance.DisableMultiplayerGameLoading)
                AddChild(btnLoadMPGame);
            AddChild(btnCancel);

            base.Initialize();

            Height = btnCreateGame.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN + UIDesignConstants.EMPTY_SPACE_BOTTOM;

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
            tbGameName.Text = string.Format("{0}'s Game".L10N("UI:Main:GameOfPlayer"), UserINISettings.Instance.PlayerName.Value);
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void BtnLoadMPGame_LeftClick(object sender, EventArgs e)
        {
            string gameName = tbGameName.Text.Replace(";", string.Empty);

            if (string.IsNullOrEmpty(gameName))
                return;

            if (!lbTunnelList.IsValidIndexSelected())
            {
                return;
            }

            IniFile spawnSGIni = new IniFile(ProgramConstants.GamePath +
                ProgramConstants.SAVED_GAME_SPAWN_INI);

            string password = Utilities.CalculateSHA1ForString(
                spawnSGIni.GetStringValue("Settings", "GameID", string.Empty)).Substring(0, 10);

            GameCreationEventArgs ea = new GameCreationEventArgs(gameName,
                spawnSGIni.GetIntValue("Settings", "PlayerCount", 2), password,
                tunnelHandler.Tunnels[lbTunnelList.SelectedIndex]);

            LoadedGameCreated?.Invoke(this, ea);
        }

        private void BtnCreateGame_LeftClick(object sender, EventArgs e)
        {
            string gameName = tbGameName.Text.Replace(";", string.Empty);

            if (string.IsNullOrEmpty(gameName))
            {
                return;
            }

            if (new ProfanityFilter().IsOffensive(gameName))
            {
                XNAMessageBox.Show(WindowManager, "Offensive game name".L10N("UI:Main:GameNameOffensiveTitle"),
                    "Please enter a less offensive game name.".L10N("UI:Main:GameNameOffensiveText"));
                return;
            }

            if (!lbTunnelList.IsValidIndexSelected())
            {
                return;
            }

            GameCreated?.Invoke(this, new GameCreationEventArgs(gameName,
                int.Parse(ddMaxPlayers.SelectedItem.Text), tbPassword.Text,
                tunnelHandler.Tunnels[lbTunnelList.SelectedIndex]));
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
            lbTunnelList.Enable();
            btnDisplayAdvancedOptions.Disable();

            SetAttributesFromIni();

            CenterOnParent();
        }

        public void Refresh()
        {
            btnLoadMPGame.AllowClick = AllowLoadingGame();
        }

        private bool AllowLoadingGame()
        {
            if (!File.Exists(ProgramConstants.GamePath + ProgramConstants.SAVED_GAME_SPAWN_INI))
                return false;

            IniFile iniFile = new IniFile(ProgramConstants.GamePath +
                ProgramConstants.SAVED_GAME_SPAWN_INI);

            if (iniFile.GetStringValue("Settings", "Name", string.Empty) != ProgramConstants.PLAYERNAME)
                return false;

            if (!iniFile.GetBooleanValue("Settings", "Host", false))
                return false;

            return true;
        }
    }
}
