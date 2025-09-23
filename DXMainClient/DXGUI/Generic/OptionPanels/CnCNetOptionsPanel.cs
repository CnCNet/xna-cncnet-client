using ClientCore.Extensions;
using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore.Enums;

namespace DTAClient.DXGUI.Generic.OptionPanels
{
    class CnCNetOptionsPanel : XNAOptionsPanel
    {
        public CnCNetOptionsPanel(WindowManager windowManager, UserINISettings iniSettings,
            GameCollection gameCollection)
            : base(windowManager, iniSettings)
        {
            this.gameCollection = gameCollection;
        }

        XNAClientCheckBox chkPingUnofficialTunnels;
        XNAClientCheckBox chkWriteInstallPathToRegistry;
        XNAClientCheckBox chkPlaySoundOnGameHosted;

        XNAClientCheckBox chkNotifyOnUserListChange;

        XNAClientCheckBox chkSkipLoginWindow;
        XNAClientCheckBox chkPersistentMode;
        XNAClientCheckBox chkConnectOnStartup;
        XNAClientCheckBox chkDiscordIntegration;
        XNAClientCheckBox chkSteamIntegration;
        XNAClientCheckBox chkAllowGameInvitesFromFriendsOnly;
        XNAClientCheckBox chkDisablePrivateMessagePopup;

        XNAClientDropDown ddAllowPrivateMessagesFrom;

        GameCollection gameCollection;

        List<XNAClientCheckBox> followedGameChks = new List<XNAClientCheckBox>();

        public override void Initialize()
        {
            base.Initialize();
            Name = "CnCNetOptionsPanel";

            InitOptions();
            InitGameListPanel();
        }

        private void InitOptions()
        {
            // LEFT COLUMN

            chkPingUnofficialTunnels = new XNAClientCheckBox(WindowManager);
            chkPingUnofficialTunnels.Name = nameof(chkPingUnofficialTunnels);
            chkPingUnofficialTunnels.ClientRectangle = new Rectangle(12, 12, 0, 0);
            chkPingUnofficialTunnels.Text = "Ping unofficial CnCNet tunnels".L10N("Client:DTAConfig:PingUnofficial");

            AddChild(chkPingUnofficialTunnels);

            chkWriteInstallPathToRegistry = new XNAClientCheckBox(WindowManager);
            chkWriteInstallPathToRegistry.Name = nameof(chkWriteInstallPathToRegistry);
            chkWriteInstallPathToRegistry.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.X,
                chkPingUnofficialTunnels.Bottom + 12, 0, 0);
            chkWriteInstallPathToRegistry.Text = ("Write game installation path to Windows\n" +
                "Registry (makes it possible to join\n" +
                 "other games' game rooms on CnCNet)").L10N("Client:DTAConfig:WriteGameRegistry");

            AddChild(chkWriteInstallPathToRegistry);

            chkPlaySoundOnGameHosted = new XNAClientCheckBox(WindowManager);
            chkPlaySoundOnGameHosted.Name = nameof(chkPlaySoundOnGameHosted);
            chkPlaySoundOnGameHosted.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.X,
                chkWriteInstallPathToRegistry.Bottom + 12, 0, 0);
            chkPlaySoundOnGameHosted.Text = "Play sound when a game is hosted".L10N("Client:DTAConfig:PlaySoundGameHosted");

            AddChild(chkPlaySoundOnGameHosted);

            chkNotifyOnUserListChange = new XNAClientCheckBox(WindowManager);
            chkNotifyOnUserListChange.Name = nameof(chkNotifyOnUserListChange);
            chkNotifyOnUserListChange.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.X,
                chkPlaySoundOnGameHosted.Bottom + 12, 0, 0);
            chkNotifyOnUserListChange.Text = ("Show player join / quit messages\n" +
                "on CnCNet lobby").L10N("Client:DTAConfig:ShowPlayerJoinQuit");

            AddChild(chkNotifyOnUserListChange);

            chkDisablePrivateMessagePopup = new XNAClientCheckBox(WindowManager);
            chkDisablePrivateMessagePopup.Name = nameof(chkDisablePrivateMessagePopup);
            chkDisablePrivateMessagePopup.ClientRectangle = new Rectangle(
                chkNotifyOnUserListChange.X,
                chkNotifyOnUserListChange.Bottom + 8, 0, 0);
            chkDisablePrivateMessagePopup.Text = "Disable Popups from Private Messages".L10N("Client:DTAConfig:DisablePMPopup");

            AddChild(chkDisablePrivateMessagePopup);

            InitAllowPrivateMessagesFromDropdown();

            // RIGHT COLUMN

            chkSkipLoginWindow = new XNAClientCheckBox(WindowManager);
            chkSkipLoginWindow.Name = nameof(chkSkipLoginWindow);
            chkSkipLoginWindow.ClientRectangle = new Rectangle(
                276,
                12, 0, 0);
            chkSkipLoginWindow.Text = "Skip login dialog".L10N("Client:DTAConfig:SkipLoginDialog");
            chkSkipLoginWindow.CheckedChanged += ChkSkipLoginWindow_CheckedChanged;

            AddChild(chkSkipLoginWindow);

            chkPersistentMode = new XNAClientCheckBox(WindowManager);
            chkPersistentMode.Name = nameof(chkPersistentMode);
            chkPersistentMode.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.X,
                chkSkipLoginWindow.Bottom + 12, 0, 0);
            chkPersistentMode.Text = "Stay connected outside of the CnCNet lobby".L10N("Client:DTAConfig:StayConnect");
            chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;

            AddChild(chkPersistentMode);

            chkConnectOnStartup = new XNAClientCheckBox(WindowManager);
            chkConnectOnStartup.Name = nameof(chkConnectOnStartup);
            chkConnectOnStartup.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.X,
                chkPersistentMode.Bottom + 12, 0, 0);
            chkConnectOnStartup.Text = "Connect automatically on client startup".L10N("Client:DTAConfig:ConnectOnStart");
            chkConnectOnStartup.AllowChecking = false;

            AddChild(chkConnectOnStartup);

            chkDiscordIntegration = new XNAClientCheckBox(WindowManager);
            chkDiscordIntegration.Name = nameof(chkDiscordIntegration);
            chkDiscordIntegration.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.X,
                chkConnectOnStartup.Bottom + 12, 0, 0);
            chkDiscordIntegration.Text = "Show detailed game info in Discord status".L10N("Client:DTAConfig:DiscordStatus");

            if (ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled)
            {
                chkDiscordIntegration.AllowChecking = false;
                chkDiscordIntegration.Checked = false;
            }
            else
            {
                chkDiscordIntegration.AllowChecking = true;
            }

            AddChild(chkDiscordIntegration);

            chkAllowGameInvitesFromFriendsOnly = new XNAClientCheckBox(WindowManager);
            chkAllowGameInvitesFromFriendsOnly.Name = nameof(chkAllowGameInvitesFromFriendsOnly);
            chkAllowGameInvitesFromFriendsOnly.ClientRectangle = new Rectangle(
                chkDiscordIntegration.X,
                chkDiscordIntegration.Bottom + 12, 0, 0);
            chkAllowGameInvitesFromFriendsOnly.Text = "Only receive game invitations from friends".L10N("Client:DTAConfig:FriendsOnly");

            AddChild(chkAllowGameInvitesFromFriendsOnly);


            chkSteamIntegration = new XNAClientCheckBox(WindowManager);
            chkSteamIntegration.Name = nameof(chkSteamIntegration);
            chkSteamIntegration.ClientRectangle = new Rectangle(
                chkAllowGameInvitesFromFriendsOnly.X,
                chkAllowGameInvitesFromFriendsOnly.Bottom + 12, 0, 0);
            chkSteamIntegration.Text = "Show the game being played in Steam".L10N("Client:DTAConfig:SteamStatus");

            AddChild(chkSteamIntegration);
        }

        private void InitAllowPrivateMessagesFromDropdown()
        {
            XNALabel lblAllPrivateMessagesFrom = new XNALabel(WindowManager);
            lblAllPrivateMessagesFrom.Name = nameof(lblAllPrivateMessagesFrom);
            lblAllPrivateMessagesFrom.Text = "Allow Private Messages From:".L10N("Client:DTAConfig:AllowPMFrom");
            lblAllPrivateMessagesFrom.ClientRectangle = new Rectangle(
                chkDisablePrivateMessagePopup.X,
                chkDisablePrivateMessagePopup.Bottom + 12, 165, 0);

            AddChild(lblAllPrivateMessagesFrom);

            ddAllowPrivateMessagesFrom = new XNAClientDropDown(WindowManager);
            ddAllowPrivateMessagesFrom.Name = nameof(ddAllowPrivateMessagesFrom);
            ddAllowPrivateMessagesFrom.ClientRectangle = new Rectangle(
                lblAllPrivateMessagesFrom.Right,
                lblAllPrivateMessagesFrom.Y - 2, 110, 0);

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "All".L10N("Client:DTAConfig:PMAll"),
                Tag = AllowPrivateMessagesFromEnum.All,
            });

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "Current channel".L10N("Client:DTAConfig:PMCurrentChannel"),
                Tag = AllowPrivateMessagesFromEnum.CurrentChannel,
            });

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "Friends".L10N("Client:DTAConfig:PMFriends"),
                Tag = AllowPrivateMessagesFromEnum.Friends,
            });

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "None".L10N("Client:DTAConfig:PMNone"),
                Tag = AllowPrivateMessagesFromEnum.None,
            });

            AddChild(ddAllowPrivateMessagesFrom);
        }

        private void InitGameListPanel()
        {
            const int gameListPanelHeight = 185;
            XNAPanel gameListPanel = new XNAPanel(WindowManager);
            gameListPanel.DrawBorders = false;
            gameListPanel.Name = nameof(gameListPanel);
            gameListPanel.ClientRectangle = new Rectangle(0, Bottom - gameListPanelHeight, Width, gameListPanelHeight);

            AddChild(gameListPanel);

            var lblFollowedGames = new XNALabel(WindowManager);
            lblFollowedGames.Name = nameof(lblFollowedGames);
            lblFollowedGames.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblFollowedGames.Text = "Show game rooms from the following games:".L10N("Client:DTAConfig:ShowRoomFromGame");

            gameListPanel.AddChild(lblFollowedGames);

            // Max number of games per column
            const int maxGamesPerColumn = 4;
            // Spacing buffer between columns
            const int columnBuffer = 20;
            // Spacing buffer between rows
            const int rowBuffer = 22;
            // Render width of a game icon
            const int gameIconWidth = 16;
            // Spacing buffer between game icon and game check box
            const int gameIconBuffer = 6;

            // List of supported games
            IEnumerable<CnCNetGame> supportedGames = gameCollection.GameList
                .Where(game => game.Supported && !string.IsNullOrEmpty(game.GameBroadcastChannel));

            // Convert to a matrix of XNAPanels that contain the game icons and check boxes
            List<List<XNAPanel>> gamePanelMatrix = supportedGames
                .Select(game =>
                {
                    var gameIconPanel = new XNAPanel(WindowManager);
                    gameIconPanel.Name = "gameIcon" + game.InternalName.ToUpperInvariant();
                    gameIconPanel.ClientRectangle = new Rectangle(0, 0, gameIconWidth, gameIconWidth);
                    gameIconPanel.DrawBorders = false;
                    gameIconPanel.BackgroundTexture = game.Texture;

                    var gameChkBox = new XNAClientCheckBox(WindowManager);
                    gameChkBox.Name = game.InternalName.ToUpperInvariant();
                    gameChkBox.ClientRectangle = new Rectangle(gameIconPanel.Right + gameIconBuffer, 0, 0, 0);
                    gameChkBox.Text = game.UIName;

                    var gamePanel = new XNAPanel(WindowManager);
                    gamePanel.AddChild(gameIconPanel);
                    gamePanel.AddChild(gameChkBox);
                    gamePanel.Name = "gamePanel" + game.InternalName.ToUpperInvariant();
                    gamePanel.DrawBorders = false;
                    gamePanel.ClientRectangle = new Rectangle(lblFollowedGames.X, 0, gameIconPanel.Width + gameChkBox.Width + gameIconBuffer, gameIconPanel.Height);

                    followedGameChks.Add(gameChkBox);
                    return gamePanel;
                })
                .ToMatrix(maxGamesPerColumn);


            // Calculate max widths for each column
            List<int> columnWidths = gamePanelMatrix
                .Select(columnList => columnList.Max(gamePanel => gamePanel.Children.Last().Right + columnBuffer))
                .ToList();

            // Reposition each game panel and then add them to the overall list panel
            int startY = lblFollowedGames.Bottom + 12;
            for (int col = 0; col < gamePanelMatrix.Count; col++)
            {
                List<XNAPanel> gamePanelColumn = gamePanelMatrix[col];
                for (int row = 0; row < gamePanelColumn.Count; row++)
                {
                    int columnOffset = columnWidths.Take(col).Sum();
                    int rowOffset = startY + row * rowBuffer;
                    XNAPanel gamePanel = gamePanelColumn[row];
                    gamePanel.ClientRectangle = new Rectangle(gamePanel.X + columnOffset, rowOffset, gamePanel.Width, gamePanel.Height);
                    gameListPanel.AddChild(gamePanel);
                }
            }
        }

        private void ChkSkipLoginWindow_CheckedChanged(object sender, EventArgs e)
        {
            CheckConnectOnStartupAllowance();
        }

        private void ChkPersistentMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckConnectOnStartupAllowance();
        }

        private void CheckConnectOnStartupAllowance()
        {
            if (!chkSkipLoginWindow.Checked || !chkPersistentMode.Checked)
            {
                chkConnectOnStartup.AllowChecking = false;
                chkConnectOnStartup.Checked = false;
                return;
            }

            chkConnectOnStartup.AllowChecking = true;
        }

        public override void Load()
        {
            base.Load();

            chkPingUnofficialTunnels.Checked = IniSettings.PingUnofficialCnCNetTunnels;
            chkWriteInstallPathToRegistry.Checked = IniSettings.WritePathToRegistry;
            chkPlaySoundOnGameHosted.Checked = IniSettings.PlaySoundOnGameHosted;
            chkNotifyOnUserListChange.Checked = IniSettings.NotifyOnUserListChange;
            chkDisablePrivateMessagePopup.Checked = IniSettings.DisablePrivateMessagePopups;
            SetAllowPrivateMessagesFromState(IniSettings.AllowPrivateMessagesFromState);
            chkConnectOnStartup.Checked = IniSettings.AutomaticCnCNetLogin;
            chkSkipLoginWindow.Checked = IniSettings.SkipConnectDialog;
            chkPersistentMode.Checked = IniSettings.PersistentMode;
            chkSteamIntegration.Checked = IniSettings.SteamIntegration;

            chkDiscordIntegration.Checked = !ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled
                && IniSettings.DiscordIntegration;

            chkAllowGameInvitesFromFriendsOnly.Checked = IniSettings.AllowGameInvitesFromFriendsOnly;

            string localGame = ClientConfiguration.Instance.LocalGame.ToUpperInvariant();

            foreach (var chkBox in followedGameChks)
            {
                if (chkBox.Name == localGame)
                {
                    chkBox.AllowChecking = false;
                    chkBox.Checked = true;
                    IniSettings.SettingsIni.SetBooleanValue("Channels", localGame, true);
                    continue;
                }

                chkBox.Checked = IniSettings.IsGameFollowed(chkBox.Name);
            }
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.PingUnofficialCnCNetTunnels.Value = chkPingUnofficialTunnels.Checked;
            IniSettings.WritePathToRegistry.Value = chkWriteInstallPathToRegistry.Checked;
            IniSettings.PlaySoundOnGameHosted.Value = chkPlaySoundOnGameHosted.Checked;
            IniSettings.NotifyOnUserListChange.Value = chkNotifyOnUserListChange.Checked;
            IniSettings.DisablePrivateMessagePopups.Value = chkDisablePrivateMessagePopup.Checked;
            IniSettings.AllowPrivateMessagesFromState.Value = GetAllowPrivateMessagesFromState();
            IniSettings.AutomaticCnCNetLogin.Value = chkConnectOnStartup.Checked;
            IniSettings.SkipConnectDialog.Value = chkSkipLoginWindow.Checked;
            IniSettings.PersistentMode.Value = chkPersistentMode.Checked;
            IniSettings.SteamIntegration.Value = chkSteamIntegration.Checked;

            if (!ClientConfiguration.Instance.DiscordIntegrationGloballyDisabled)
            {
                IniSettings.DiscordIntegration.Value = chkDiscordIntegration.Checked;
            }

            IniSettings.AllowGameInvitesFromFriendsOnly.Value = chkAllowGameInvitesFromFriendsOnly.Checked;

            foreach (var chkBox in followedGameChks)
            {
                IniSettings.SettingsIni.SetBooleanValue("Channels", chkBox.Name, chkBox.Checked);
            }

            return restartRequired;
        }

        private void SetAllowPrivateMessagesFromState(int state)
        {
            var selectedIndex = ddAllowPrivateMessagesFrom.Items.FindIndex(i => (int)i.Tag == state);
            if (selectedIndex < 0)
                selectedIndex = ddAllowPrivateMessagesFrom.Items.FindIndex(i => (AllowPrivateMessagesFromEnum)i.Tag == AllowPrivateMessagesFromEnum.All);

            ddAllowPrivateMessagesFrom.SelectedIndex = selectedIndex;
        }

        private int GetAllowPrivateMessagesFromState()
        {
            return (int)(ddAllowPrivateMessagesFrom.SelectedItem?.Tag ?? AllowPrivateMessagesFromEnum.All);
        }
    }
}
