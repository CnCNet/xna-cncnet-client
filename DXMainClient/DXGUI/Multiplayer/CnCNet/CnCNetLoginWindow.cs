using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using DTAClient.DXGUI.Multiplayer.CnCNet.Api;
using System.Net;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    class CnCNetLoginWindow : XNAWindow
    {
        private WindowManager wm;

        public CnCNetLoginWindow(WindowManager windowManager) : base(windowManager)
        {
            this.wm = windowManager;
        }

        XNALabel lblConnectToCnCNet;
        XNATextBox tbPlayerName;
        XNALabel lblPlayerName;
        XNALabel lblEmail;
        XNATextBox tbEmail;
        XNAPasswordBox tbPassword;
        XNALabel lblPassword;
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
            ClientRectangle = new Rectangle(0, 0, 300, 300);
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
            lblPlayerName.Text = "Nickname:";
            lblPlayerName.ClientRectangle = new Rectangle(12, tbPlayerName.ClientRectangle.Y + 1,
                lblPlayerName.ClientRectangle.Width, lblPlayerName.ClientRectangle.Height);

            tbEmail = new XNATextBox(WindowManager);
            tbEmail.Name = "tbEmail";
            tbEmail.Text = "email";
            tbEmail.ClientRectangle =
                new Rectangle(tbPlayerName.ClientRectangle.X,
                              tbPlayerName.ClientRectangle.Bottom + 6,
                              120, 19);

            lblEmail = new XNALabel(WindowManager);
            lblEmail.Name = "lblPlayerEmail";
            lblEmail.FontIndex = 1;
            lblEmail.Text = "Email:";
            lblEmail.ClientRectangle = new Rectangle(12, tbEmail.ClientRectangle.Y + 1,
                lblEmail.ClientRectangle.Width, lblEmail.ClientRectangle.Height);

            tbPassword = new XNAPasswordBox(WindowManager);
            tbPassword.Name = "tbPassword";
            tbPassword.MaximumTextLength = 16;
            tbPassword.ClientRectangle =
                new Rectangle(tbEmail.ClientRectangle.X,
                              tbEmail.ClientRectangle.Bottom + 6,
                              120, 19);

            lblPassword = new XNALabel(WindowManager);
            lblPassword.Name = "lbPassword";
            lblPassword.FontIndex = 1;
            lblPassword.Text = "Password:";
            lblPassword.ClientRectangle =
                new Rectangle(12,
                              tbPassword.ClientRectangle.Y + 1,
                              lblPassword.ClientRectangle.Width, lblPassword.ClientRectangle.Height);

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
            AddChild(tbEmail);
            AddChild(lblEmail);
            AddChild(lblPassword);
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
                lblPassword.Visible = false;
                tbPassword.Visible = false;
                tbEmail.Visible = false;
                lblEmail.Visible = false;
            }
            else
            {
                lblPassword.Visible = true;
                tbPassword.Visible = true;
                tbEmail.Visible = true;
                lblEmail.Visible = true;
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
            ProgramConstants.PLAYER_EMAIL = tbEmail.Text;
            ProgramConstants.PASSWORD = tbPassword.Password;
            ProgramConstants.AUTHENTICATE = !chkAnonymous.Checked;

            UserINISettings.Instance.SkipConnectDialog.Value = chkRememberMe.Checked;
            UserINISettings.Instance.PersistentMode.Value = chkPersistentMode.Checked;
            UserINISettings.Instance.AutomaticCnCNetLogin.Value = chkAutoConnect.Checked;
            UserINISettings.Instance.PlayerName.Value = ProgramConstants.PLAYERNAME;

            UserINISettings.Instance.SaveSettings();

            if(ProgramConstants.AUTHENTICATE)
            {
                NetworkCredential credentials = new NetworkCredential(ProgramConstants.PLAYERNAME, ProgramConstants.PASSWORD);
                Auth auth = new Auth(credentials, ProgramConstants.PLAYERNAME, "derp@cncnet.org");

                string response = auth.Login();
                if(response.Length == 0)
                {
                    string success = "You have logged in as " + auth.account.Username + "\n" + "Clan: " + auth.account.Clan
                        + "\n" + "Email: " + auth.account.Email + "";

                    var msgBox = new XNAMessageBox(wm, "Welcome!", success, DXMessageBoxButtons.OK);
                    wm.AddAndInitializeControl(msgBox);

                    Connect?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    var msgBox = new XNAMessageBox(wm, "Error", response, DXMessageBoxButtons.OK);
                    wm.AddAndInitializeControl(msgBox);
                }
            }
            else
            {
                Connect?.Invoke(this, EventArgs.Empty);
            }
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
