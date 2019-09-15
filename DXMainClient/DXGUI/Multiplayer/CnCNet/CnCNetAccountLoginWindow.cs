using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Diagnostics;
using System.Net;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class CnCNetAccountLoginWindow : XNAWindow
    {
        public event Action<object> Cancel;
        public event Action<bool> LoginSuccess;

        private XNALabel lblPlayerEmail;
        private XNATextBox tbPlayerEmail;
        private XNALabel lblPlayerPassword;
        private XNAPasswordBox tbPlayerPassword;
        private XNALabel lblLoginWindowTitle;

        public CnCNetAccountLoginWindow(WindowManager windowManager) : base(windowManager)
        {
        }
        
        public override void Initialize()
        {
            Name = "CnCNetAccountLoginWindow";
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");
            ClientRectangle = new Rectangle(0, 0, 350, 150);

            lblLoginWindowTitle = new XNALabel(WindowManager);
            lblLoginWindowTitle.Name = "lblWindowTitle";
            lblLoginWindowTitle.FontIndex = 1;
            lblLoginWindowTitle.Text = "LOGIN TO CNCNET";

            AddChild(lblLoginWindowTitle);
            lblLoginWindowTitle.CenterOnParent();
            lblLoginWindowTitle.ClientRectangle = new Rectangle(
                lblLoginWindowTitle.X, 12,
                lblLoginWindowTitle.Width,
                lblLoginWindowTitle.Height);

            var btnLogin = new XNAClientButton(WindowManager);
            btnLogin.Name = "btnLogin";
            btnLogin.ClientRectangle = new Rectangle(12, ClientRectangle.Bottom - 35, 92, 23);
            btnLogin.Text = "Login";
            btnLogin.LeftClick += BtnLogin_LeftClick;

            var btnRegister = new XNAClientButton(WindowManager);
            btnRegister.Name = "btnRegister";
            btnRegister.ClientRectangle = new Rectangle(btnLogin.Width + 25, ClientRectangle.Bottom - 35, 92, 23);
            btnRegister.Text = "Register";
            btnRegister.LeftClick += BtnRegister_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 104,
                btnLogin.Y, 92, 23);
            btnCancel.Text = "Cancel";
            btnCancel.LeftClick += BtnCancel_LeftClick;

            tbPlayerEmail = new XNATextBox(WindowManager);
            tbPlayerEmail.Name = "tbPlayerEmail";
            tbPlayerEmail.Text = "email";
            tbPlayerEmail.ClientRectangle = new Rectangle(100, 50, 200, 19);
            tbPlayerEmail.Text = "";

            lblPlayerEmail = new XNALabel(WindowManager);
            lblPlayerEmail.Name = "lblPlayerEmail";
            lblPlayerEmail.FontIndex = 1;
            lblPlayerEmail.Text = "Email:";
            lblPlayerEmail.ClientRectangle = new Rectangle(12, tbPlayerEmail.ClientRectangle.Y + 1,
                lblPlayerEmail.ClientRectangle.Width, lblPlayerEmail.ClientRectangle.Height);

            lblPlayerPassword = new XNALabel(WindowManager);
            lblPlayerPassword.Name = "lblPlayerPassword";
            lblPlayerPassword.FontIndex = 1;
            lblPlayerPassword.Text = "Password:";
            lblPlayerPassword.ClientRectangle = new Rectangle(
                12, tbPlayerEmail.ClientRectangle.Y + 25,
                lblPlayerPassword.ClientRectangle.Width,
                lblPlayerPassword.ClientRectangle.Height
            );

            tbPlayerPassword = new XNAPasswordBox(WindowManager);
            tbPlayerPassword.Name = "tbPlayerPassword";
            tbPlayerPassword.Text = "Password";
            tbPlayerPassword.ClientRectangle = new Rectangle(100, lblPlayerPassword.ClientRectangle.Y, 200, 19);
            tbPlayerPassword.Text = "";

            AddChild(tbPlayerEmail);
            AddChild(tbPlayerPassword);
            AddChild(lblPlayerEmail);
            AddChild(lblPlayerPassword);
            AddChild(btnLogin);
            AddChild(btnCancel);
            AddChild(btnRegister);

            base.Initialize();

            CenterOnParent();

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
        }

        private void BtnRegister_LeftClick(object sender, EventArgs e)
        {
            Process.Start(CnCNetAuthApi.API_REGISTER_URL);
        }

        private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (Enabled && e.PressedKey == Keys.Enter)
                BtnLogin_LeftClick(this, EventArgs.Empty);
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Cancel?.Invoke(this);
            Disable();
        }

        private void BtnLogin_LeftClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbPlayerEmail.Text))
                return;

            Login();
        }

        private void Login()
        {
            bool success = CnCNetAuthApi.Instance.Login(tbPlayerEmail.Text, tbPlayerPassword.Password);
            if (success)
            {
                LoginSuccess?.Invoke(true);
            }
            else
            {
                // Handle error message
            }
        }
    }
}
