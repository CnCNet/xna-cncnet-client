using ClientCore;
using ClientCore.CnCNet5;
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
    public class CnCNetAccountManagerWindow : XNAWindow
    {
        public event Action<object> Logout;
        public event Action<object> Connect;

        private XNALabel lblAccountManager;
        private XNADropDown ddAccounts;
        private XNALabel lblAccounts;
        private XNAButton btnConnect;

        public CnCNetAccountManagerWindow(WindowManager windowManager) : base(windowManager)
        {
        }
        
        public override void Initialize()
        {
            Name = "CnCNetAccountManagerWindow";
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");
            ClientRectangle = new Rectangle(0, 0, 400, 250);

            lblAccountManager = new XNALabel(WindowManager);
            lblAccountManager.Name = "lblAccountManager";
            lblAccountManager.FontIndex = 1;
            lblAccountManager.Text = "YOUR NICKNAMES";

            lblAccountManager.ClientRectangle = new Rectangle(
                12, 12,
                lblAccountManager.Width,
                lblAccountManager.Height);

            AddChild(lblAccountManager);

            btnConnect = new XNAClientButton(WindowManager);
            btnConnect.Name = "btnConnect";
            btnConnect.ClientRectangle = new Rectangle(12,ClientRectangle.Bottom - 35, 92, 23);
            btnConnect.Text = "Connect";
            btnConnect.LeftClick += BtnConnect_LeftClick;

            var btnLogout = new XNAClientButton(WindowManager);
            btnLogout.Name = "btnLogout";
            btnLogout.ClientRectangle = new Rectangle(Width - 104,
                btnConnect.Y, 92, 23);
            btnLogout.Text = "Logout";
            btnLogout.LeftClick += BtnLogout_LeftClick;

            ddAccounts = new XNADropDown(WindowManager);
            ddAccounts.Name = "ddAccounts";
            ddAccounts.Text = "Accounts";
            ddAccounts.ClientRectangle = new Rectangle(100, ClientRectangle.Y + 50, 200, 19);

            lblAccounts = new XNALabel(WindowManager);
            lblAccounts.Name = "lblAccounts";
            lblAccounts.FontIndex = 1;
            lblAccounts.Text = "Nicknames:";
            lblAccounts.ClientRectangle = new Rectangle(12, ddAccounts.ClientRectangle.Y + 1,
            lblAccounts.ClientRectangle.Width, lblAccounts.ClientRectangle.Height);

            AddChild(ddAccounts);
            AddChild(lblAccounts);
            AddChild(btnConnect);
            AddChild(btnLogout);

            base.Initialize();

            CenterOnParent();

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;

            CnCNetAuthApi.Instance.AccountUpdated += CnCNetAuthApi_AccountUpdated;
        }

        private void CnCNetAuthApi_AccountUpdated(object obj)
        {
            PopulateAccountList();
        }

        private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (Enabled && e.PressedKey == Keys.Enter)
                BtnConnect_LeftClick(this, EventArgs.Empty);
        }

        private void BtnLogout_LeftClick(object sender, EventArgs e)
        {
            CnCNetAuthApi.Instance.Logout();
            Disable();
        }

        private void BtnConnect_LeftClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddAccounts.SelectedItem.Text))
                return;

            string nickname = ddAccounts.SelectedItem.Text;

            string errorMessage = NameValidator.IsNameValid(nickname);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                XNAMessageBox.Show(WindowManager, "Invalid Player Name", errorMessage);
                return;
            }

            ProgramConstants.PLAYERNAME = nickname;
            UserINISettings.Instance.PlayerName.Value = ProgramConstants.PLAYERNAME;
            UserINISettings.Instance.SaveSettings();

            Connect?.Invoke(this);
        }

        private void PopulateAccountList()
        {
            for (int i = 0; i < CnCNetAuthApi.Instance.Accounts.Count; i++)
            {
                AuthPlayer player = CnCNetAuthApi.Instance.Accounts[i];

                XNADropDownItem ddItem = new XNADropDownItem();
                ddItem.Text = player.username;

                ddAccounts.AddItem(ddItem);
            }
            ddAccounts.SelectedIndex = 0;
        }
    }
}
