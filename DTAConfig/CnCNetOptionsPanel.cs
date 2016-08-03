using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientCore;
using ClientCore.CnCNet5;
using Rampastring.XNAUI;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig
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

        GameCollection gameCollection;

        List<XNAClientCheckBox> followedGameChks = new List<XNAClientCheckBox>();

        public override void Initialize()
        {
            base.Initialize();

            chkPingUnofficialTunnels = new XNAClientCheckBox(WindowManager);
            chkPingUnofficialTunnels.Name = "chkPingUnofficialTunnels";
            chkPingUnofficialTunnels.ClientRectangle = new Rectangle(12, 12, 0, 0);
            chkPingUnofficialTunnels.Text = "Ping unofficial CnCNet tunnels";

            AddChild(chkPingUnofficialTunnels);

            chkWriteInstallPathToRegistry = new XNAClientCheckBox(WindowManager);
            chkWriteInstallPathToRegistry.Name = "chkWriteInstallPathToRegistry";
            chkWriteInstallPathToRegistry.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.ClientRectangle.X,
                chkPingUnofficialTunnels.ClientRectangle.Bottom + 12, 0, 0);
            chkWriteInstallPathToRegistry.Text = "Write game installation path to Windows" + Environment.NewLine +
                "Registry (makes it possible to join" + Environment.NewLine +
                 "other games' game rooms on CnCNet)";

            AddChild(chkWriteInstallPathToRegistry);

            chkPlaySoundOnGameHosted = new XNAClientCheckBox(WindowManager);
            chkPlaySoundOnGameHosted.Name = "chkPlaySoundOnGameHosted";
            chkPlaySoundOnGameHosted.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.ClientRectangle.X,
                chkWriteInstallPathToRegistry.ClientRectangle.Bottom + 12, 0, 0);
            chkPlaySoundOnGameHosted.Text = "Play sound when a game is hosted";

            AddChild(chkPlaySoundOnGameHosted);

            chkNotifyOnUserListChange = new XNAClientCheckBox(WindowManager);
            chkNotifyOnUserListChange.Name = "chkNotifyOnUserListChange";
            chkNotifyOnUserListChange.ClientRectangle = new Rectangle(
                chkPingUnofficialTunnels.ClientRectangle.X,
                chkPlaySoundOnGameHosted.ClientRectangle.Bottom + 12, 0, 0);
            chkNotifyOnUserListChange.Text = "Show player join / quit messages" + Environment.NewLine +
                "on CnCNet lobby";

            AddChild(chkNotifyOnUserListChange);

            chkSkipLoginWindow = new XNAClientCheckBox(WindowManager);
            chkSkipLoginWindow.Name = "chkSkipLoginWindow";
            chkSkipLoginWindow.ClientRectangle = new Rectangle(
                276,
                12, 0, 0);
            chkSkipLoginWindow.Text = "Skip login dialog";
            chkSkipLoginWindow.CheckedChanged += ChkSkipLoginWindow_CheckedChanged;

            AddChild(chkSkipLoginWindow);

            chkPersistentMode = new XNAClientCheckBox(WindowManager);
            chkPersistentMode.Name = "chkPersistentMode";
            chkPersistentMode.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.ClientRectangle.X,
                chkSkipLoginWindow.ClientRectangle.Bottom + 12, 0, 0);
            chkPersistentMode.Text = "Stay connected outside of the CnCNet lobby";
            chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;

            AddChild(chkPersistentMode);

            chkConnectOnStartup = new XNAClientCheckBox(WindowManager);
            chkConnectOnStartup.Name = "chkConnectOnStartup";
            chkConnectOnStartup.ClientRectangle = new Rectangle(
                chkSkipLoginWindow.ClientRectangle.X,
                chkPersistentMode.ClientRectangle.Bottom + 12, 0, 0);
            chkConnectOnStartup.Text = "Connect automatically on client startup";
            chkConnectOnStartup.AllowChecking = false;

            AddChild(chkConnectOnStartup);

            var lblFollowedGames = new XNALabel(WindowManager);
            lblFollowedGames.Name = "lblFollowedGames";
            lblFollowedGames.ClientRectangle = new Rectangle(
                chkNotifyOnUserListChange.ClientRectangle.X,
                chkNotifyOnUserListChange.ClientRectangle.Bottom + 24, 0, 0);
            lblFollowedGames.Text = "Show game rooms from the following games:";

            AddChild(lblFollowedGames);

            int chkCount = 0;

            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported || string.IsNullOrEmpty(game.GameBroadcastChannel))
                    continue;

                var panel = new XNAPanel(WindowManager);
                panel.Name = "panel" + game.InternalName;
                panel.ClientRectangle = new Rectangle(chkPingUnofficialTunnels.ClientRectangle.X,
                    lblFollowedGames.ClientRectangle.Bottom + 12 + chkCount * 22, 16, 16);
                panel.DrawBorders = false;
                panel.BackgroundTexture = game.Texture;

                var chkBox = new XNAClientCheckBox(WindowManager);
                chkBox.Name = game.InternalName.ToUpper();
                chkBox.ClientRectangle = new Rectangle(
                    panel.ClientRectangle.Right + 6,
                    panel.ClientRectangle.Y, 0, 0);
                chkBox.Text = game.UIName;

                chkCount++;

                AddChild(panel);
                AddChild(chkBox);
                followedGameChks.Add(chkBox);
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
            chkPingUnofficialTunnels.Checked = IniSettings.PingUnofficialCnCNetTunnels;
            chkWriteInstallPathToRegistry.Checked = IniSettings.WritePathToRegistry;
            chkPlaySoundOnGameHosted.Checked = IniSettings.PlaySoundOnGameHosted;
            chkNotifyOnUserListChange.Checked = IniSettings.NotifyOnUserListChange;
            chkConnectOnStartup.Checked = IniSettings.AutomaticCnCNetLogin;
            chkSkipLoginWindow.Checked = IniSettings.SkipConnectDialog;
            chkPersistentMode.Checked = IniSettings.PersistentMode;

            string localGame = DomainController.Instance().GetDefaultGame();

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
            IniSettings.PingUnofficialCnCNetTunnels.Value = chkPingUnofficialTunnels.Checked;
            IniSettings.WritePathToRegistry.Value = chkWriteInstallPathToRegistry.Checked;
            IniSettings.PlaySoundOnGameHosted.Value = chkPlaySoundOnGameHosted.Checked;
            IniSettings.NotifyOnUserListChange.Value = chkNotifyOnUserListChange.Checked;
            IniSettings.AutomaticCnCNetLogin.Value = chkConnectOnStartup.Checked;
            IniSettings.SkipConnectDialog.Value = chkSkipLoginWindow.Checked;
            IniSettings.PersistentMode.Value = chkPersistentMode.Checked;

            foreach (var chkBox in followedGameChks)
            {
                IniSettings.SettingsIni.SetBooleanValue("Channels", chkBox.Name, chkBox.Checked);
            }

            return false;
        }
    }
}
