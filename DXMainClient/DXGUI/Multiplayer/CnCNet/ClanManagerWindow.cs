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
        DarkeningPanel panLogin;
        public ClanManageTab ClanManageTab;
        public ClanInvitesTab ClanInvitesTab;
        XNAPanel panBottomButtons;
        XNAClientButton btnNewClan;
        XNAClientButton btnNewInvite;
        XNAClientButton btnSearchClans;
        NewClanWindow winNewClan;
        DarkeningPanel panNewClan;
        public ClanSearchWindow ClanSearchWindow;
        DarkeningPanel panSearch;

        public ClanInviteWindow WinNewInvite;
        DarkeningPanel PanNewInvite;

        public event EventHandler<SearchEventArgs> ViewClanRequested;

        CnCNetManager cm;
        WindowManager wm;

        public ClanManagerWindow(WindowManager windowManager,
            CnCNetManager cm, GameCollection gameCollection) : base(windowManager)
        {
            //this.gameCollection = gameCollection;
            //windowManager.GameClose += WindowManager_GameClosing;
            this.cm = cm;
            this.wm = windowManager;
            cm.CncServ.AuthResponse += AuthenticationResponse;
            cm.CncServ.ClanServices.ReceivedCreateClanResponse += CreateClanResponse;
        }

        public override void Initialize()
        {
            Name = "ClanManagerWindow";
            ClientRectangle = new Rectangle(0,0,400,600);
            BackgroundTexture = AssetLoader.LoadTextureUncached("privatemessagebg.png");

            lblClanManager = new XNALabel(wm);
            lblClanManager.Name = "lblClanManager";
            lblClanManager.FontIndex = 1;
            lblClanManager.Text = "CLAN MANAGER";

            AddChild(lblClanManager);
            lblClanManager.CenterOnParent();
            lblClanManager.ClientRectangle = new Rectangle(
                lblClanManager.ClientRectangle.X, 12,
                lblClanManager.ClientRectangle.Width,
                lblClanManager.ClientRectangle.Height);

            tabClanManager = new XNAClientTabControl(wm);
            tabClanManager.Name = "tabClanManager";
            tabClanManager.SoundOnClick = AssetLoader.LoadSound("button.wav");
            tabClanManager.FontIndex = 1;
            tabClanManager.AddTab("Manage",160);
            tabClanManager.AddTab("Invitations", 160);
            AddChild(tabClanManager);
            tabClanManager.CenterOnParent();
            tabClanManager.ClientRectangle =
                new Rectangle(tabClanManager.ClientRectangle.X, 60,
                                    tabClanManager.ClientRectangle.Width,
                                    tabClanManager.ClientRectangle.Height);
            tabClanManager.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            var tabRect = new Rectangle(12, tabClanManager.ClientRectangle.Bottom + 24,
                ClientRectangle.Width - 24,
                ClientRectangle.Height - tabClanManager.ClientRectangle.Bottom - 70);


            ClanManageTab = new ClanManageTab(wm, cm, this, tabRect);
            ClanInvitesTab = new ClanInvitesTab(wm, cm, this, tabRect);

            tabClanManagerArray = new XNAPanel[]
                {ClanManageTab, ClanInvitesTab};

            btnNewClan = new XNAClientButton(wm);
            btnNewClan.Name = "bntNewClan";
            btnNewClan.ClientRectangle = new Rectangle(0,0,92,23);
            btnNewClan.FontIndex = 1;
            btnNewClan.Text = "New Clan";
            btnNewClan.LeftClick += BtnNewClan_LeftClicked;

            btnNewInvite = new XNAClientButton(wm);
            btnNewInvite.Name = "btnNewInvite";
            btnNewInvite.ClientRectangle =
                new Rectangle(btnNewClan.ClientRectangle.Right + 12, 0, 92, 23);
            btnNewInvite.FontIndex = 1;
            btnNewInvite.Text = "New Invite";
            btnNewInvite.LeftClick += BtnNewInvite_LeftClicked;

            btnSearchClans = new XNAClientButton(wm);
            btnSearchClans.Name = "btnSearchClan";
            btnSearchClans.ClientRectangle =
                new Rectangle(btnNewInvite.ClientRectangle.Right + 12, 0, 92, 23);
            btnSearchClans.FontIndex = 1;
            btnSearchClans.Text = "Search";
            btnSearchClans.LeftClick += BtnSearchClan_LeftClicked;

            panBottomButtons = new XNAPanel(wm);
            panBottomButtons.DrawBorders = false;
            panBottomButtons.AddChild(btnNewClan);
            panBottomButtons.AddChild(btnNewInvite);
            panBottomButtons.AddChild(btnSearchClans);
            panBottomButtons.ClientRectangle =
                new Rectangle(0,0, btnSearchClans.ClientRectangle.Right,
                              btnNewClan.ClientRectangle.Height);
            AddChild(panBottomButtons);
            panBottomButtons.CenterOnParent();
            panBottomButtons.ClientRectangle =
                new Rectangle(panBottomButtons.ClientRectangle.X,
                              ClientRectangle.Height - 34,
                              panBottomButtons.ClientRectangle.Width,
                              panBottomButtons.ClientRectangle.Height);

            ClanInvitesTab.Disable();
            tabClanManager.SelectedTab = 0;
            CenterOnParent();

            AddChild(ClanInvitesTab);
            AddChild(ClanManageTab);
            base.Initialize();

            winNewClan = new NewClanWindow(wm, cm);
            winNewClan.Name = "winNewClan";
            winNewClan.Disable();

            panNewClan = new DarkeningPanel(wm);
            panNewClan.Alpha = 0.0f;
            AddChild(panNewClan);
            panNewClan.AddChild(winNewClan);
            panNewClan.Disable();

            WinNewInvite = new ClanInviteWindow(wm, cm);
            WinNewInvite.Name = "WinNewInvite";
            WinNewInvite.Disable();

            PanNewInvite = new DarkeningPanel(wm);
            PanNewInvite.Alpha = 0.0f;
            AddChild(PanNewInvite);
            PanNewInvite.AddChild(WinNewInvite);
            PanNewInvite.Disable();

            ClanSearchWindow = new ClanSearchWindow(wm, cm);
            ClanSearchWindow.Name = "ClanSearchWindow";
            ClanSearchWindow.Disable();
            ClanSearchWindow.ViewClanRequested += LoadSearchedClan;

            panSearch = new DarkeningPanel(wm);
            panSearch.Alpha = 0.0f;
            AddChild(panSearch);
            panSearch.AddChild(ClanSearchWindow);
            panSearch.Disable();

        }

        public void SwitchOn()
        {
            tabClanManager.SelectedTab = 0;
            wm.SelectedControl = null;
            ClanManageTab.Refresh();
            ClanInvitesTab.Refresh();

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
                ClanManageTab.Refresh();
                ClanInvitesTab.Refresh();
            }
        }

        private void BtnNewClan_LeftClicked(object s, EventArgs e)
        {
            winNewClan.Enable();
        }

        private void CreateClanResponse(object s, ClanEventArgs e)
        {
            if (e.Result == "SUCCESS")
            {
                ClanManageTab.Refresh();
                ClanInvitesTab.Refresh();
            }
        }

        private void BtnNewInvite_LeftClicked(object s, EventArgs e)
        {
            WinNewInvite.Enable();
        }

        private void BtnSearchClan_LeftClicked(object s, EventArgs e)
        {
            ClanSearchWindow.Enable();
        }

        private void LoadSearchedClan(object s, SearchEventArgs e)
        {
            this.ViewClanRequested?.Invoke(this, e);
        }
    }
}
