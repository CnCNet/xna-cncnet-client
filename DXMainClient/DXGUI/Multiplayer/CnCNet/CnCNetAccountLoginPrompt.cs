using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Net;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class CnCNetAccountLoginPrompt : XNAWindow
    {
        public event Action<object> ConnectAsGuest;
        public event Action<object> ConnectWithAccount;
    
        private XNALabel lblWindowTitle;
        private XNALabel lblWindowDescription;
        private XNAButton btnConnectAsGuest;

        public CnCNetAccountLoginPrompt(WindowManager windowManager) : base(windowManager)
        {
        }
        
        public override void Initialize()
        {
            Name = "CnCNetAccountLoginPrompt";
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");
            ClientRectangle = new Rectangle(0, 0, 400, 150);

            lblWindowTitle = new XNALabel(WindowManager);
            lblWindowTitle.Name = "lblWindowTitle";
            lblWindowTitle.FontIndex = 1;
            lblWindowTitle.Text = "CONNECT TO CNCNET";

            AddChild(lblWindowTitle);
            lblWindowTitle.CenterOnParent();
            lblWindowTitle.ClientRectangle = new Rectangle(
                lblWindowTitle.X, 12,
                lblWindowTitle.Width,
                lblWindowTitle.Height);

            lblWindowDescription = new XNALabel(WindowManager);
            lblWindowDescription.Name = "lblWindowTitle";
            lblWindowDescription.FontIndex = 1;
            lblWindowDescription.Text = "Choose how you would like to connect to CnCNet";

            AddChild(lblWindowDescription);
            lblWindowDescription.CenterOnParent();
            lblWindowDescription.ClientRectangle = new Rectangle(
                lblWindowDescription.X, 50,
                lblWindowDescription.Width,
                lblWindowDescription.Height);

            btnConnectAsGuest = new XNAClientButton(WindowManager);
            btnConnectAsGuest.Name = "btnConnectAsGuest";
            btnConnectAsGuest.ClientRectangle = new Rectangle(12,ClientRectangle.Bottom - 35, 133, 23);
            btnConnectAsGuest.Text = "As a Guest";
            btnConnectAsGuest.LeftClick += BtnConnectAsGuest_LeftClick;

            var btnLoginWithAccount = new XNAClientButton(WindowManager);
            btnLoginWithAccount.Name = "btnLoginWithAccount";
            btnLoginWithAccount.ClientRectangle = new Rectangle(Width - 140,
                btnConnectAsGuest.Y, 133, 23);
            btnLoginWithAccount.Text = "With my Account";
            btnLoginWithAccount.LeftClick += BtnLoginWithAcccount_LeftClick;

            AddChild(btnConnectAsGuest);
            AddChild(btnLoginWithAccount);

            base.Initialize();

            CenterOnParent();

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
        }

        private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (Enabled && e.PressedKey == Keys.Enter)
                BtnConnectAsGuest_LeftClick(this, EventArgs.Empty);
        }

        private void BtnLoginWithAcccount_LeftClick(object sender, EventArgs e)
        {
            ConnectWithAccount?.Invoke(this);
            Disable();
        }

        private void BtnConnectAsGuest_LeftClick(object sender, EventArgs e)
        {
            ConnectAsGuest?.Invoke(this);
            Disable();
        }
    }
}
