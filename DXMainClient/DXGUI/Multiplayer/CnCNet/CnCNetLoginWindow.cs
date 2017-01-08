using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    class CnCNetLoginWindow : XNAWindow
    {
        public CnCNetLoginWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        XNALabel lblConnectToCnCNet;
        XNATextBox tbPlayerName;
        XNALabel lblPlayerName;
        XNAPasswordBox tbPassword;
        XNALabel lbPassword;
        XNAClientCheckBox chkAnonymous;
        XNAClientCheckBox chkRememberMe;
        XNAClientCheckBox chkPersistentMode;
        XNAClientCheckBox chkAutoConnect;
        XNAClientButton btnConnect;
        XNAClientButton btnCancel;

        public event EventHandler Cancelled;
        public event EventHandler Connect;

        public override void Initialize()
        {
            Name = "CnCNetLoginWindow";
            ClientRectangle = new Rectangle(0, 0, 300, 282);
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");

            lblConnectToCnCNet = new XNALabel(WindowManager);
            lblConnectToCnCNet.Name = "lblConnectToCnCNet";
            lblConnectToCnCNet.FontIndex = 1;
            lblConnectToCnCNet.Text = "CONNECT TO CNCNET";

            AddChild(lblConnectToCnCNet);
            lblConnectToCnCNet.CenterOnParent();
            lblConnectToCnCNet.ClientRectangle = new Rectangle(
                lblConnectToCnCNet.ClientRectangle.X, 12,
                lblConnectToCnCNet.ClientRectangle.Width,
                lblConnectToCnCNet.ClientRectangle.Height);

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = "tbPlayerName";
            tbPlayerName.ClientRectangle = new Rectangle(ClientRectangle.Width - 132, 50, 120, 19);
            tbPlayerName.MaximumTextLength = 16;
            string defgame = ClientConfiguration.Instance.LocalGame;
            if (defgame == "YR" || defgame == "MO")
                tbPlayerName.MaximumTextLength = 12; // YR can't handle names longer than 12 chars

            lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = "lblPlayerName";
            lblPlayerName.FontIndex = 1;
            lblPlayerName.Text = "PLAYER NAME:";
            lblPlayerName.ClientRectangle = new Rectangle(12, tbPlayerName.ClientRectangle.Y + 1,
                lblPlayerName.ClientRectangle.Width, lblPlayerName.ClientRectangle.Height);

            tbPassword = new XNAPasswordBox(WindowManager);
            tbPassword.Name = "tbPassword";
            tbPassword.MaximumTextLength = 16;
            tbPassword.ClientRectangle =
                new Rectangle(tbPlayerName.ClientRectangle.X,
                              tbPlayerName.ClientRectangle.Bottom + 6,
                              120, 19);

            lbPassword = new XNALabel(WindowManager);
            lbPassword.Name = "lbPassword";
            lbPassword.FontIndex = 1;
            lbPassword.Text = "PASSWORD:";
            lbPassword.ClientRectangle =
                new Rectangle(12,
                              tbPassword.ClientRectangle.Y + 1,
                              lbPassword.ClientRectangle.Width, lbPassword.ClientRectangle.Height);

            chkAnonymous = new XNAClientCheckBox(WindowManager);
            chkAnonymous.Name = "chkAnonymous";
            chkAnonymous.ClientRectangle = new Rectangle(12, tbPassword.ClientRectangle.Bottom + 12, 0, 0);
            chkAnonymous.Text = "Connect Anonymously";
            chkAnonymous.TextPadding = 7;
            chkAnonymous.CheckedChanged += ChkAnonymous_CheckedChanged;

            chkRememberMe = new XNAClientCheckBox(WindowManager);
            chkRememberMe.Name = "chkRememberMe";
            chkRememberMe.ClientRectangle = new Rectangle(12, chkAnonymous.ClientRectangle.Bottom + 30, 0, 0);
            chkRememberMe.Text = "Remember me";
            chkRememberMe.TextPadding = 7;
            chkRememberMe.CheckedChanged += ChkRememberMe_CheckedChanged;

            chkPersistentMode = new XNAClientCheckBox(WindowManager);
            chkPersistentMode.Name = "chkPersistentMode";
            chkPersistentMode.ClientRectangle = new Rectangle(12, chkRememberMe.ClientRectangle.Bottom + 30, 0, 0);
            chkPersistentMode.Text = "Stay connected outside of the CnCNet lobby";
            chkPersistentMode.TextPadding = chkRememberMe.TextPadding;
            chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;

            chkAutoConnect = new XNAClientCheckBox(WindowManager);
            chkAutoConnect.Name = "chkAutoConnect";
            chkAutoConnect.ClientRectangle = new Rectangle(12, chkPersistentMode.ClientRectangle.Bottom + 30, 0, 0);
            chkAutoConnect.Text = "Connect automatically on client startup";
            chkAutoConnect.TextPadding = chkRememberMe.TextPadding;
            chkAutoConnect.AllowChecking = false;

            btnConnect = new XNAClientButton(WindowManager);
            btnConnect.Name = "btnConnect";
            btnConnect.ClientRectangle = new Rectangle(12, ClientRectangle.Height - 35, 110, 23);
            btnConnect.Text = "Connect";
            btnConnect.LeftClick += BtnConnect_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(ClientRectangle.Width - 122, btnConnect.ClientRectangle.Y, 110, 23);
            btnCancel.Text = "Cancel";
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(tbPlayerName);
            AddChild(lblPlayerName);
            AddChild(lbPassword);
            AddChild(tbPassword);
            AddChild(chkAnonymous);
            AddChild(chkRememberMe);
            AddChild(chkPersistentMode);
            AddChild(chkAutoConnect);
            AddChild(btnConnect);
            AddChild(btnCancel);

            base.Initialize();

            CenterOnParent();

            UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;
        }

        private void Instance_SettingsSaved(object sender, EventArgs e)
        {
            tbPlayerName.Text = UserINISettings.Instance.PlayerName;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void ChkAnonymous_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAnonymous.Checked)
            {
                lbPassword.Visible = false;
                tbPassword.Visible = false;
            }
            else
            {
                lbPassword.Visible = true;
                tbPassword.Visible = true;
            }
        }
        private void ChkRememberMe_CheckedChanged(object sender, EventArgs e)
        {
            CheckAutoConnectAllowance();
        }

        private void ChkPersistentMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckAutoConnectAllowance();
        }

        private void CheckAutoConnectAllowance()
        {
            chkAutoConnect.AllowChecking = chkPersistentMode.Checked && chkRememberMe.Checked;
            if (!chkAutoConnect.AllowChecking)
                chkAutoConnect.Checked = false;
        }

        private void BtnConnect_LeftClick(object sender, EventArgs e)
        {
            string errorMessage = NameValidator.IsNameValid(tbPlayerName.Text);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                XNAMessageBox.Show(WindowManager, "Invalid Player Name", errorMessage);
                return;
            }

            ProgramConstants.PLAYERNAME = tbPlayerName.Text;
            ProgramConstants.PASSWORD = tbPassword.Password;
            ProgramConstants.AUTHENTICATE = !chkAnonymous.Checked;

            UserINISettings.Instance.SkipConnectDialog.Value = chkRememberMe.Checked;
            UserINISettings.Instance.PersistentMode.Value = chkPersistentMode.Checked;
            UserINISettings.Instance.AutomaticCnCNetLogin.Value = chkAutoConnect.Checked;
            UserINISettings.Instance.PlayerName.Value = ProgramConstants.PLAYERNAME;

            UserINISettings.Instance.SaveSettings();

            Connect?.Invoke(this, EventArgs.Empty);
        }

        public void LoadSettings()
        {
            chkAutoConnect.Checked = UserINISettings.Instance.AutomaticCnCNetLogin;
            chkPersistentMode.Checked = UserINISettings.Instance.PersistentMode;
            chkRememberMe.Checked = UserINISettings.Instance.SkipConnectDialog;

            tbPlayerName.Text = ProgramConstants.PLAYERNAME;

            if (chkRememberMe.Checked)
                BtnConnect_LeftClick(this, EventArgs.Empty);
        }
    }
}
