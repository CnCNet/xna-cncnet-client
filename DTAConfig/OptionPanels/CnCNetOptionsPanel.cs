using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using ClientCore.Enums;
using Localization;

namespace DTAConfig.OptionPanels
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
            chkPingUnofficialTunnels.Text = "Ping unofficial CnCNet tunnels".L10N("UI:DTAConfig:PingUnofficial");

            AddChild(chkPingUnofficialTunnels);

            chkWriteInstallPathToRegistry = new XNAClientCheckBox(WindowManager);
            chkWriteInstallPathToRegistry.Name = nameof(chkWriteInstallPathToRegistry);
            chkWriteInstallPathToRegistry.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.X,
                chkPingUnofficialTunnels.Bottom + 12, 0, 0);
            chkWriteInstallPathToRegistry.Text = ("Write game installation path to Windows" + Environment.NewLine +
                "Registry (makes it possible to join" + Environment.NewLine +
                 "other games' game rooms on CnCNet)").L10N("UI:DTAConfig:WriteGameRegistry");

            AddChild(chkWriteInstallPathToRegistry);

            chkPlaySoundOnGameHosted = new XNAClientCheckBox(WindowManager);
            chkPlaySoundOnGameHosted.Name = nameof(chkPlaySoundOnGameHosted);
            chkPlaySoundOnGameHosted.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.X,
                chkWriteInstallPathToRegistry.Bottom + 12, 0, 0);
            chkPlaySoundOnGameHosted.Text = "Play sound when a game is hosted".L10N("UI:DTAConfig:PlaySoundGameHosted");

            AddChild(chkPlaySoundOnGameHosted);

            chkNotifyOnUserListChange = new XNAClientCheckBox(WindowManager);
            chkNotifyOnUserListChange.Name = nameof(chkNotifyOnUserListChange);
            chkNotifyOnUserListChange.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.X,
                chkPlaySoundOnGameHosted.Bottom + 12, 0, 0);
            chkNotifyOnUserListChange.Text = ("Show player join / quit messages" + Environment.NewLine +
                "on CnCNet lobby").L10N("UI:DTAConfig:ShowPlayerJoinQuit");

            AddChild(chkNotifyOnUserListChange);

            chkDisablePrivateMessagePopup = new XNAClientCheckBox(WindowManager);
            chkDisablePrivateMessagePopup.Name = nameof(chkDisablePrivateMessagePopup);
            chkDisablePrivateMessagePopup.ClientRectangle = new Rectangle(
                chkNotifyOnUserListChange.X,
                chkNotifyOnUserListChange.Bottom + 8, 0, 0);
            chkDisablePrivateMessagePopup.Text = "Disable Popups from Private Messages".L10N("UI:DTAConfig:DisablePMPopup");

            AddChild(chkDisablePrivateMessagePopup);

            InitAllowPrivateMessagesFromDropdown();
            
            // RIGHT COLUMN

            chkSkipLoginWindow = new XNAClientCheckBox(WindowManager);
            chkSkipLoginWindow.Name = nameof(chkSkipLoginWindow);
            chkSkipLoginWindow.ClientRectangle = new Rectangle(
                276,
                12, 0, 0);
            chkSkipLoginWindow.Text = "Skip login dialog".L10N("UI:DTAConfig:SkipLoginDialog");
            chkSkipLoginWindow.CheckedChanged += ChkSkipLoginWindow_CheckedChanged;

            AddChild(chkSkipLoginWindow);

            chkPersistentMode = new XNAClientCheckBox(WindowManager);
            chkPersistentMode.Name = nameof(chkPersistentMode);
            chkPersistentMode.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.X,
                chkSkipLoginWindow.Bottom + 12, 0, 0);
            chkPersistentMode.Text = "Stay connected outside of the CnCNet lobby".L10N("UI:DTAConfig:StayConnect");
            chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;

            AddChild(chkPersistentMode);

            chkConnectOnStartup = new XNAClientCheckBox(WindowManager);
            chkConnectOnStartup.Name = nameof(chkConnectOnStartup);
            chkConnectOnStartup.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.X,
                chkPersistentMode.Bottom + 12, 0, 0);
            chkConnectOnStartup.Text = "Connect automatically on client startup".L10N("UI:DTAConfig:ConnectOnStart");
            chkConnectOnStartup.AllowChecking = false;

            AddChild(chkConnectOnStartup);

            chkDiscordIntegration = new XNAClientCheckBox(WindowManager);
            chkDiscordIntegration.Name = nameof(chkDiscordIntegration);
            chkDiscordIntegration.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.X,
                chkConnectOnStartup.Bottom + 12, 0, 0);
            chkDiscordIntegration.Text = "Show detailed game info in Discord status".L10N("UI:DTAConfig:DiscordStatus");
            
            if (String.IsNullOrEmpty(ClientConfiguration.Instance.DiscordAppId))
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
            chkAllowGameInvitesFromFriendsOnly.Text = "Only receive game invitations from friends".L10N("UI:DTAConfig:FriendsOnly");

            AddChild(chkAllowGameInvitesFromFriendsOnly);
        }

        private void InitAllowPrivateMessagesFromDropdown()
        {
            XNALabel lblAllPrivateMessagesFrom = new XNALabel(WindowManager);
            lblAllPrivateMessagesFrom.Name = nameof(lblAllPrivateMessagesFrom);
            lblAllPrivateMessagesFrom.Text = "Allow Private Messages From:".L10N("UI:DTAConfig:AllowPMFrom");
            lblAllPrivateMessagesFrom.ClientRectangle = new Rectangle(
                chkDisablePrivateMessagePopup.X,
                chkDisablePrivateMessagePopup.Bottom + 12, 165, 0);

            AddChild(lblAllPrivateMessagesFrom);

            ddAllowPrivateMessagesFrom = new XNAClientDropDown(WindowManager);
            ddAllowPrivateMessagesFrom.Name = nameof(ddAllowPrivateMessagesFrom);
            ddAllowPrivateMessagesFrom.ClientRectangle = new Rectangle(
                lblAllPrivateMessagesFrom.Right,
                lblAllPrivateMessagesFrom.Y - 2, 65, 0);

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "All".L10N("UI:DTAConfig:PMAll"),
                Tag =  AllowPrivateMessagesFromEnum.All
            });

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "Friends".L10N("UI:DTAConfig:PMFriends"),
                Tag =  AllowPrivateMessagesFromEnum.Friends
            });

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "None".L10N("UI:DTAConfig:PMNone"),
                Tag =  AllowPrivateMessagesFromEnum.None
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
            lblFollowedGames.Text = "Show game rooms from the following games:".L10N("UI:DTAConfig:ShowRoomFromGame");

            gameListPanel.AddChild(lblFollowedGames);

            int chkCount = 0;
            int chkCountPerColumn = 4;
            int nextColumnXOffset = 0;
            int columnXOffset = 0;
            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported || string.IsNullOrEmpty(game.GameBroadcastChannel))
                    continue;

                if (chkCount == chkCountPerColumn)
                {
                    chkCount = 0;
                    columnXOffset += nextColumnXOffset + 6;
                    nextColumnXOffset = 0;
                }

                var panel = new XNAPanel(WindowManager);
                panel.Name = "panel" + game.InternalName;
                panel.ClientRectangle = new Rectangle(lblFollowedGames.X + columnXOffset,
                    lblFollowedGames.Bottom + 12 + chkCount * 22, 16, 16);
                panel.DrawBorders = false;
                panel.BackgroundTexture = game.Texture;

                var chkBox = new XNAClientCheckBox(WindowManager);
                chkBox.Name = game.InternalName.ToUpper();
                chkBox.ClientRectangle = new Rectangle(
                    panel.Right + 6,
                    panel.Y, 0, 0);
                chkBox.Text = game.UIName;

                chkCount++;

                gameListPanel.AddChild(panel);
                gameListPanel.AddChild(chkBox);
                followedGameChks.Add(chkBox);

                if (chkBox.Right > nextColumnXOffset)
                    nextColumnXOffset = chkBox.Right;
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

            chkDiscordIntegration.Checked = !String.IsNullOrEmpty(ClientConfiguration.Instance.DiscordAppId)
                && IniSettings.DiscordIntegration;

            chkAllowGameInvitesFromFriendsOnly.Checked = IniSettings.AllowGameInvitesFromFriendsOnly;

            string localGame = ClientConfiguration.Instance.LocalGame;

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

            if (!String.IsNullOrEmpty(ClientConfiguration.Instance.DiscordAppId))
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
