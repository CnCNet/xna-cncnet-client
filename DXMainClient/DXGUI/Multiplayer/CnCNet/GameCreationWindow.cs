using ClientCore;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
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
            Name = "GameCreationWindow";
            ClientRectangle = new Rectangle(0, 0, 490, 188);
            BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");

            btnCreateGame = new XNAClientButton(WindowManager);
            btnCreateGame.ClientRectangle = new Rectangle(12, 159, 133, 23);
            btnCreateGame.Text = "Create Game";
            btnCreateGame.LeftClick += BtnCreateGame_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.ClientRectangle = new Rectangle(345, btnCreateGame.Y, 133, 23);
            btnCancel.Text = "Cancel";
            btnCancel.LeftClick += BtnCancel_LeftClick;

            btnLoadMPGame = new XNAClientButton(WindowManager);
            btnLoadMPGame.ClientRectangle = new Rectangle(178, btnCreateGame.Y, 133, 23);
            btnLoadMPGame.Text = "Load Game";
            btnLoadMPGame.LeftClick += BtnLoadMPGame_LeftClick;

            btnDisplayAdvancedOptions = new XNAClientButton(WindowManager);
            btnDisplayAdvancedOptions.ClientRectangle = new Rectangle(12, 124, 160, 23);
            btnDisplayAdvancedOptions.Text = "Advanced Options";
            btnDisplayAdvancedOptions.LeftClick += BtnDisplayAdvancedOptions_LeftClick;

            tbGameName = new XNATextBox(WindowManager);
            tbGameName.MaximumTextLength = 23;
            tbGameName.ClientRectangle = new Rectangle(Width - 162, 12, 150, 21);
            tbGameName.Text = ProgramConstants.PLAYERNAME + "'s Game";

            lblRoomName = new XNALabel(WindowManager);
            lblRoomName.ClientRectangle = new Rectangle(12, tbGameName.Y + 1, 0, 0);
            lblRoomName.Text = "Game room name:";

            ddMaxPlayers = new XNAClientDropDown(WindowManager);
            ddMaxPlayers.ClientRectangle = new Rectangle(tbGameName.X, 53, 
                tbGameName.Width, 21);
            for (int i = 8; i > 1; i--)
                ddMaxPlayers.AddItem(i.ToString());
            ddMaxPlayers.SelectedIndex = 0;

            lblMaxPlayers = new XNALabel(WindowManager);
            lblMaxPlayers.ClientRectangle = new Rectangle(12, ddMaxPlayers.Y + 1, 0, 0);
            lblMaxPlayers.Text = "Maximum number of players:";

            tbPassword = new XNATextBox(WindowManager);
            tbPassword.MaximumTextLength = 20;
            tbPassword.ClientRectangle = new Rectangle(tbGameName.X, 94, 
                tbGameName.Width, 21);

            lblPassword = new XNALabel(WindowManager);
            lblPassword.ClientRectangle = new Rectangle(12, tbPassword.Y + 1, 0, 0);
            lblPassword.Text = "Password (leave blank for none):";

            lblTunnelServer = new XNALabel(WindowManager);
            lblTunnelServer.ClientRectangle = new Rectangle(12, 134, 0, 0);
            lblTunnelServer.Text = "Tunnel server:";
            lblTunnelServer.Enabled = false;
            lblTunnelServer.Visible = false;

            lbTunnelList = new TunnelListBox(WindowManager, tunnelHandler);
            lbTunnelList.X = 12;
            lbTunnelList.Y = 154;
            lbTunnelList.Disable();
            lbTunnelList.ListRefreshed += LbTunnelList_ListRefreshed;

            AddChild(btnCreateGame);
            AddChild(btnCancel);
            if (!ClientConfiguration.Instance.DisableMultiplayerGameLoading)
                AddChild(btnLoadMPGame);
            AddChild(btnDisplayAdvancedOptions);
            AddChild(tbGameName);
            AddChild(lblRoomName);
            AddChild(lblMaxPlayers);
            AddChild(tbPassword);
            AddChild(lblPassword);
            AddChild(lblTunnelServer);
            AddChild(lbTunnelList);
            AddChild(ddMaxPlayers);

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
            tbGameName.Text = UserINISettings.Instance.PlayerName.Value + "'s Game";
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
                XNAMessageBox.Show(WindowManager, "Offensive game name", 
                    "Please enter a less offensive game name.");
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

            ClientRectangle = new Rectangle(X, Y,
                Width, 420);

            btnCreateGame.ClientRectangle = new Rectangle(btnCreateGame.X,
                Height - 29, btnCreateGame.Width,
                btnCreateGame.Height);

            btnCancel.ClientRectangle = new Rectangle(btnCancel.X,
                Height - 29, btnCancel.Width,
                btnCancel.Height);

            btnLoadMPGame.ClientRectangle = new Rectangle(btnLoadMPGame.X,
                Height - 29, btnLoadMPGame.Width,
                btnLoadMPGame.Height);

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
