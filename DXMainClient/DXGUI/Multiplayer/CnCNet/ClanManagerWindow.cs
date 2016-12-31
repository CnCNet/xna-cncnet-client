using ClientGUI;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Linq;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using DTAClient.Online;
using DTAClient.Online.Services;
using Microsoft.Xna.Framework.Graphics;
using DTAClient.Properties;
using System.IO;
using ClientCore;
using Rampastring.Tools;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework.Audio;
using ClientCore.CnCNet5;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    internal class ClanManagerWindow : XNAWindow, ISwitchable
    {
        XNALabel lblClanManager;

        XNAClientTabControl tabClanManager;
        XNAPanel[] tabClanManagerArray;
        ServLoginWindow winLogin;
        DarkeningPanel panLogin;
        ClanManageTab clanManageTab;
        ClanInvitesTab clanInvitesTab;
        CnCNetManager cm;

        public ClanManagerWindow(WindowManager windowManager,
            CnCNetManager cm, GameCollection gameCollection) : base(windowManager)
        {
            //this.gameCollection = gameCollection;
            //windowManager.GameClose += WindowManager_GameClosing;
            this.cm = cm;
            cm.CncServ.AuthResponse += AuthenticationResponse;
        }

        public override void Initialize()
        {
            Name = "ClanManagerWindow";
            ClientRectangle = new Rectangle(0,0,600,600);
            BackgroundTexture = AssetLoader.LoadTextureUncached("privatemessagebg.png");

            lblClanManager = new XNALabel(WindowManager);
            lblClanManager.Name = "lblClanManager";
            lblClanManager.FontIndex = 1;
            lblClanManager.Text = "CLAN MANAGER";

            AddChild(lblClanManager);
            lblClanManager.CenterOnParent();
            lblClanManager.ClientRectangle = new Rectangle(
                lblClanManager.ClientRectangle.X, 12,
                lblClanManager.ClientRectangle.Width,
                lblClanManager.ClientRectangle.Height);

            tabClanManager = new XNAClientTabControl(WindowManager);
            tabClanManager.Name = "tabClanManager";
            tabClanManager.ClientRectangle = new Rectangle(60, 50, 0, 0);
            tabClanManager.SoundOnClick = AssetLoader.LoadSound("button.wav");
            tabClanManager.FontIndex = 1;
            tabClanManager.AddTab("Manage",160);
            tabClanManager.AddTab("Search",160);
            tabClanManager.AddTab("Invitations", 160);
            tabClanManager.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            var tabRect = new Rectangle(12, tabClanManager.ClientRectangle.Bottom + 24,
                ClientRectangle.Width - 24,
                ClientRectangle.Height - tabClanManager.ClientRectangle.Bottom - 48);


            clanManageTab = new ClanManageTab(WindowManager, cm, tabRect);
            clanInvitesTab = new ClanInvitesTab(WindowManager, cm, tabRect);

            tabClanManagerArray = new XNAPanel[3]
                {clanManageTab, clanInvitesTab, clanInvitesTab};


            AddChild(tabClanManager);
            AddChild(clanInvitesTab);
            AddChild(clanManageTab);
            base.Initialize();

            clanInvitesTab.Disable();
            tabClanManager.SelectedTab = 0;
            CenterOnParent();

            winLogin = new ServLoginWindow(WindowManager, cm);
            winLogin.Disable();
            var panLogin = new DarkeningPanel(WindowManager);
            panLogin.Alpha = 0.0f;

            AddChild(panLogin);
            panLogin.AddChild(winLogin);

            winLogin.Disable();
        }

        public void SwitchOn()
        {
            tabClanManager.SelectedTab = 0;
            WindowManager.SelectedControl = null;
            if (!cm.IsConnected)
            {
                Enable();
            }
            if (cm.CncServ.IsAuthenticated)
            {
                Enable();
            }
            else {
                Enable();
                winLogin.Enable();
            }
        }
        public void SwitchOff()
        {
            Disable();
        }

        public string GetSwitchName()
        {
            return "Clan Manager";
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            Array.ForEach(tabClanManagerArray, x => x.Disable());
            tabClanManagerArray[tabClanManager.SelectedTab].Enable();
        }
        private void AuthenticationResponse(object s, CnCNetServAuthEventArgs e)
        {
            if (cm.CncServ.IsAuthenticated)
            {
                clanManageTab.Refresh();
                clanInvitesTab.Refresh();
            }
        }
    }

    public class ServLoginWindow : XNAWindow
    {
        XNATextBox tbUserName;
        XNATextBox tbPassword;
        XNAClientButton btnConnect;
        CnCNetManager cm;
        Timer loginButtonTimer;

        public ServLoginWindow (WindowManager windowManager, CnCNetManager cm)
        : base(windowManager)
        {
            this.cm = cm;
            cm.CncServ.AuthResponse += AuthenticationResponse;
            loginButtonTimer = new Timer(5000);
            loginButtonTimer.Elapsed += LoginTimedOut;
        }

        public override void Initialize()
        {
            Name = "ServLoginWindow";
            ClientRectangle = new Rectangle(0,0,300,220);
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");
            tbUserName = new XNATextBox(WindowManager);
            tbUserName.Name = "tbUserName";
            CenterOnParent();
            tbUserName.ClientRectangle =
                new Rectangle(ClientRectangle.Width - 132, 50, 120, 19);
            tbUserName.MaximumTextLength = 13;

            tbPassword = new XNATextBox(WindowManager);
            tbPassword.Name = "tbPassword";
            tbPassword.ClientRectangle =
                new Rectangle(ClientRectangle.Width - 132, 70, 120, 19);
            tbPassword.MaximumTextLength = 20;

            btnConnect = new XNAClientButton(WindowManager);
            btnConnect.Name = "tbConnect";
            btnConnect.ClientRectangle =
                new Rectangle(12, ClientRectangle.Height - 35, 110, 23);
            btnConnect.Text = "Login";

            AddChild(tbUserName);
            AddChild(tbPassword);
            AddChild(btnConnect);
            btnConnect.LeftClick += BtnConnect_LeftClick;
        }
        private void BtnConnect_LeftClick(object s, EventArgs e)
        {
            cm.CncServ.UserName = tbUserName.Text;
            cm.CncServ.Password = tbPassword.Text;
            cm.CncServ.ConnectOnce = true;
            cm.CncServ.Authenticate();
            btnConnect.Disable();
            loginButtonTimer.Start();
        }

        private void LoginTimedOut(object s, EventArgs e)
        {
            btnConnect.Enable();
            tbPassword.Text = "";
            loginButtonTimer.Stop();
        }

        private void AuthenticationResponse(object s, CnCNetServAuthEventArgs e)
        {
            Console.WriteLine("result = {0}, uname = {1}, cname = {2}, failmsg = {3}",
                              e.Result, e.UserName, e.ClanName, e.FailMessage);
            if (cm.CncServ.IsAuthenticated)
                Disable();
            else
                btnConnect.Enable();
        }

    }
}
