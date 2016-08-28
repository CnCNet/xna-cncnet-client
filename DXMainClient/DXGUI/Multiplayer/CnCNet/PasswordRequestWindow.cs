using ClientGUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.Domain.Multiplayer.CnCNet;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class PasswordRequestWindow : XNAWindow
    {
        public PasswordRequestWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        public event EventHandler<PasswordEventArgs> PasswordEntered;

        private XNATextBox tbPassword;

        private HostedCnCNetGame hostedGame;

        public override void Initialize()
        {
            Name = "PasswordRequestWindow";
            ClientRectangle = new Rectangle(0, 0, 150, 90);

            var lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblDescription.Text = "Please enter the password below and click OK.";

            tbPassword = new XNATextBox(WindowManager);
            tbPassword.Name = "tbPassword";
            tbPassword.ClientRectangle = new Rectangle(lblDescription.ClientRectangle.X,
                lblDescription.ClientRectangle.Bottom + 12, ClientRectangle.Width - 24, 21);

            var btnOK = new XNAClientButton(WindowManager);
            btnOK.Name = "btnOK";
            btnOK.ClientRectangle = new Rectangle(lblDescription.ClientRectangle.X,
                ClientRectangle.Bottom - 35, 92, 23);
            btnOK.LeftClick += BtnOK_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(ClientRectangle.Width - 104,
                btnOK.ClientRectangle.Y, 92, 23);
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lblDescription);
            AddChild(tbPassword);
            AddChild(btnOK);
            AddChild(btnCancel);

            base.Initialize();
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbPassword.Text))
                return;

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
