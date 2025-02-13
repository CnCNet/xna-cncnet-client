using ClientCore.Extensions;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore.Enums;

namespace DTAConfig.OptionPanels
{
    class CnCNetOptionsPanel : XNAOptionsPanel
    {
        private const int PADDING_X = 12;
        private const int PADDING_Y = 14;
        private const int DD_Y_OFFSET = 2;
        private const int DD_HEIGHT = 22;
        private const int DD_PM_WIDTH = 65;
        private const int CHECKBOX_SPACING = 4;
        private const int GROUP_SPACING_X = 22;
        private const int LABEL_WIDTH = 200;
        private const int LABEL_HEIGHT = 20;

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
            int halfWidth = (Width - (2 * PADDING_X) - GROUP_SPACING_X) / 2;

            // LEFT COLUMN

            chkPingUnofficialTunnels = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkPingUnofficialTunnels),
                ClientRectangle = new Rectangle(PADDING_X, PADDING_Y, 0, 0),
                Text = "Ping unofficial CnCNet tunnels".L10N("Client:DTAConfig:PingUnofficial")
            };
            AddChild(chkPingUnofficialTunnels);

            chkWriteInstallPathToRegistry = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkWriteInstallPathToRegistry),
                ClientRectangle = new Rectangle(PADDING_X, chkPingUnofficialTunnels.Bottom + CHECKBOX_SPACING, 0, 0),
                Text = ("Write game installation path to Windows\n" +
                        "Registry (makes it possible to join\n" +
                        "other games' game rooms on CnCNet)").L10N("Client:DTAConfig:WriteGameRegistry")
            };
            AddChild(chkWriteInstallPathToRegistry);

            chkPlaySoundOnGameHosted = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkPlaySoundOnGameHosted),
                ClientRectangle = new Rectangle(PADDING_X, chkWriteInstallPathToRegistry.Bottom + CHECKBOX_SPACING, 0, 0),
                Text = "Play sound when a game is hosted".L10N("Client:DTAConfig:PlaySoundGameHosted")
            };
            AddChild(chkPlaySoundOnGameHosted);

            chkNotifyOnUserListChange = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkNotifyOnUserListChange),
                ClientRectangle = new Rectangle(PADDING_X, chkPlaySoundOnGameHosted.Bottom + CHECKBOX_SPACING, 0, 0),
                Text = ("Show player join / quit messages\n" +
                        "on CnCNet lobby").L10N("Client:DTAConfig:ShowPlayerJoinQuit")
            };
            AddChild(chkNotifyOnUserListChange);

            chkDisablePrivateMessagePopup = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkDisablePrivateMessagePopup),
                ClientRectangle = new Rectangle(PADDING_X, chkNotifyOnUserListChange.Bottom + CHECKBOX_SPACING, 0, 0),
                Text = "Disable popups from Private Messages".L10N("Client:DTAConfig:DisablePMPopup")
            };
            AddChild(chkDisablePrivateMessagePopup);

            InitAllowPrivateMessagesFromDropdown(PADDING_X);

            // RIGHT COLUMN

            int rightColumnX = PADDING_X + halfWidth + GROUP_SPACING_X;

            chkSkipLoginWindow = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkSkipLoginWindow),
                ClientRectangle = new Rectangle(rightColumnX, PADDING_Y, 0, 0),
                Text = "Skip login dialog".L10N("Client:DTAConfig:SkipLoginDialog")
            };
            chkSkipLoginWindow.CheckedChanged += ChkSkipLoginWindow_CheckedChanged;
            AddChild(chkSkipLoginWindow);

            chkPersistentMode = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkPersistentMode),
                ClientRectangle = new Rectangle(rightColumnX, chkSkipLoginWindow.Bottom + CHECKBOX_SPACING, 0, 0),
                Text = "Stay connected outside of the CnCNet lobby".L10N("Client:DTAConfig:StayConnect"),
            };
            chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;
            AddChild(chkPersistentMode);

            chkConnectOnStartup = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkConnectOnStartup),
                ClientRectangle = new Rectangle(rightColumnX, chkPersistentMode.Bottom + CHECKBOX_SPACING, 0, 0),
                Text = "Connect automatically on client startup".L10N("Client:DTAConfig:ConnectOnStart"),
                AllowChecking = false
            };

            AddChild(chkConnectOnStartup);

            chkDiscordIntegration = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkDiscordIntegration),
                ClientRectangle = new Rectangle(rightColumnX, chkConnectOnStartup.Bottom + CHECKBOX_SPACING, 0, 0),
                Text = "Show detailed game info in Discord status".L10N("Client:DTAConfig:DiscordStatus")
            };
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

            chkAllowGameInvitesFromFriendsOnly = new XNAClientCheckBox(WindowManager)
            {
                Name = nameof(chkAllowGameInvitesFromFriendsOnly),
                ClientRectangle = new Rectangle(rightColumnX, chkDiscordIntegration.Bottom + CHECKBOX_SPACING, 0, 0),
                Text = "Only receive game invitations from friends".L10N("Client:DTAConfig:FriendsOnly")
            };

            AddChild(chkAllowGameInvitesFromFriendsOnly);
        }

        private void InitAllowPrivateMessagesFromDropdown(int xPosition)
        {
            XNALabel lblAllPrivateMessagesFrom = new XNALabel(WindowManager)
            {
                Name = nameof(lblAllPrivateMessagesFrom),
                Text = "Allow Private Messages from:".L10N("Client:DTAConfig:AllowPMFrom"),
                ClientRectangle = new Rectangle(xPosition, chkDisablePrivateMessagePopup.Bottom + CHECKBOX_SPACING, LABEL_WIDTH, LABEL_HEIGHT)
            };

            AddChild(lblAllPrivateMessagesFrom);

            ddAllowPrivateMessagesFrom = new XNAClientDropDown(WindowManager)
            {
                Name = nameof(ddAllowPrivateMessagesFrom),
                ClientRectangle = new Rectangle(xPosition + LABEL_WIDTH, lblAllPrivateMessagesFrom.Y - DD_Y_OFFSET, DD_PM_WIDTH, DD_HEIGHT)
            };

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "All".L10N("Client:DTAConfig:PMAll"),
                Tag = AllowPrivateMessagesFromEnum.All
            });

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "Friends".L10N("Client:DTAConfig:PMFriends"),
                Tag = AllowPrivateMessagesFromEnum.Friends
            });

            ddAllowPrivateMessagesFrom.AddItem(new XNADropDownItem()
            {
                Text = "None".L10N("Client:DTAConfig:PMNone"),
                Tag = AllowPrivateMessagesFromEnum.None
            });

            AddChild(ddAllowPrivateMessagesFrom);
        }

        private void InitGameListPanel()
        {
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

            XNAPanel gameListPanel = new XNAPanel(WindowManager);
            gameListPanel.DrawBorders = false;
            gameListPanel.Name = nameof(gameListPanel);

            var lblFollowedGames = new XNALabel(WindowManager);
            lblFollowedGames.Name = nameof(lblFollowedGames);
            lblFollowedGames.ClientRectangle = new Rectangle(PADDING_X, PADDING_Y, 0, 0);
            lblFollowedGames.Text = "Show game rooms from the following games:".L10N("Client:DTAConfig:ShowRoomFromGame");

            gameListPanel.AddChild(lblFollowedGames);

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

            int startY = lblFollowedGames.Bottom + PADDING_X;

            // Reposition each game panel and then add them to the overall list panel
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

            // Calculate panel height based on content
            int contentHeight = PADDING_Y +
                               lblFollowedGames.Height +
                               PADDING_X +
                               (Math.Min(supportedGames.Count(), maxGamesPerColumn) * rowBuffer) -
                               (rowBuffer - gameIconWidth) +
                               PADDING_Y;

            gameListPanel.ClientRectangle = new Rectangle(0, Height - contentHeight, Width, contentHeight);

            AddChild(gameListPanel);
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
