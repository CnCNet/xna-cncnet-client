using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    internal class PasswordRequestWindow : XNAWindow
    {
        public PasswordRequestWindow(WindowManager windowManager, PrivateMessagingWindow privateMessagingWindow) : base(windowManager)
        {
            this.privateMessagingWindow = privateMessagingWindow;
        }

        public event EventHandler<PasswordEventArgs> PasswordEntered;

        private XNATextBox tbPassword;

        private HostedCnCNetGame hostedGame;

        private PrivateMessagingWindow privateMessagingWindow;
        private bool pmWindowWasEnabled { get; set; }

        public override void Initialize()
        {
            Name = "PasswordRequestWindow";
            BackgroundTexture = AssetLoader.LoadTexture("passwordquerybg.png");

            var lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblDescription.Text = "Please enter the password for the game and click OK.".L10N("UI:Main:EnterPasswordAndHitOK");

            ClientRectangle = new Rectangle(0, 0, lblDescription.Width + 24, 110);

            tbPassword = new XNATextBox(WindowManager);
            tbPassword.Name = "tbPassword";
            tbPassword.ClientRectangle = new Rectangle(lblDescription.X,
                lblDescription.Bottom + 12, Width - 24, 21);

            var btnOK = new XNAClientButton(WindowManager);
            btnOK.Name = "btnOK";
            btnOK.ClientRectangle = new Rectangle(lblDescription.X,
                ClientRectangle.Bottom - 35, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnOK.Text = "OK".L10N("UI:Main:ButtonOK");
            btnOK.LeftClick += BtnOK_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 104,
                btnOK.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lblDescription);
            AddChild(tbPassword);
            AddChild(btnOK);
            AddChild(btnCancel);

            base.Initialize();

            CenterOnParent();

            EnabledChanged += PasswordRequestWindow_EnabledChanged;
            tbPassword.EnterPressed += TextBoxPassword_EnterPressed;
        }

        private void TextBoxPassword_EnterPressed(object sender, EventArgs eventArgs)
        {
            BtnOK_LeftClick(this, eventArgs);
        }

        private void PasswordRequestWindow_EnabledChanged(object sender, EventArgs e)
        {
            if (Enabled)
            {
                WindowManager.SelectedControl = tbPassword;
                if (!privateMessagingWindow.Enabled) return;
                pmWindowWasEnabled = true;
                privateMessagingWindow.Disable();
            } 
            else if(pmWindowWasEnabled)
            {
                privateMessagingWindow.Enable();
            }
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbPassword.Text))
                return;

            pmWindowWasEnabled = false;
            Disable();

            PasswordEntered?.Invoke(this, new PasswordEventArgs(tbPassword.Text, hostedGame));
            tbPassword.Text = string.Empty;
        }

        public void SetHostedGame(HostedCnCNetGame hostedGame)
        {
            this.hostedGame = hostedGame;
        }
    }

    public class PasswordEventArgs : EventArgs
    {
        public PasswordEventArgs(string password, HostedCnCNetGame hostedGame)
        {
            Password = password;
            HostedGame = hostedGame;
        }

        /// <summary>
        /// The password input by the user.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// The game that the user is attempting to join.
        /// </summary>
        public HostedCnCNetGame HostedGame { get; private set; }
    }
}
