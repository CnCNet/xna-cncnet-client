using ClientGUI;
using System;
using System.Collections.Generic;
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
    partial class ClanManageTab : XNAPanel
    {
        XNALabel lblManageClan;
        XNAClientTabControl tabMineOrOther;
        XNATextBox tbOtherClan;
        XNAClientButton btnLoadOtherClan;
        XNALabel lblClanMembers;
        XNAListBox lbClanMembers;
        XNAClientTabControl tabMemberRole;
        XNAClientButton btnRemoveMember;
        XNAMessageBox messageBox;

        public string SelectedClan;
        List<ClanMember> currentClanMembers;
        CnCNetManager cm;
        WindowManager wm;

        public override void Initialize()
        {
            Name = "ClanManageTab";
            lblManageClan = new XNALabel(WindowManager);
            lblManageClan.Name = "lblManageClan";
            lblManageClan.FontIndex = 1;
            lblManageClan.Text = "MANAGE CLAN";
            AddChild(lblManageClan);
            lblManageClan.CenterOnParent();
            lblManageClan.ClientRectangle =
                new Rectangle(lblManageClan.ClientRectangle.X, 12,
                              lblManageClan.ClientRectangle.Width,
                              lblManageClan.ClientRectangle.Height);

            tabMineOrOther = new XNAClientTabControl(WindowManager);
            tabMineOrOther.Name = "tabMineOrOther";
            tabMineOrOther.ClientRectangle =
                new Rectangle(ClientRectangle.X + 6,
                              lblManageClan.ClientRectangle.Bottom + 12, 0, 0);
            tabMineOrOther.SoundOnClick = AssetLoader.LoadSound("button.wav");
            tabMineOrOther.FontIndex = 1;
            tabMineOrOther.AddTab("My Clan",160);
            tabMineOrOther.AddTab("Other Clan",160);
            tabMineOrOther.SelectedIndexChanged +=
                TabMineOrOther_SelectedIndexChanged;

            tbOtherClan = new XNATextBox(WindowManager);
            tbOtherClan.Name = "tbOtherClan";
            tbOtherClan.ClientRectangle =
                new Rectangle(tabMineOrOther.ClientRectangle.Right + 12,
                              tabMineOrOther.ClientRectangle.Y,
                              120,
                              24);
            tbOtherClan.MaximumTextLength = 14;
            tbOtherClan.Visible = false;

            btnLoadOtherClan = new XNAClientButton(WindowManager);
            btnLoadOtherClan.Name = "btnLoadOtherClan";
            btnLoadOtherClan.ClientRectangle =
                new Rectangle(tbOtherClan.ClientRectangle.Right + 12,
                              tbOtherClan.ClientRectangle.Y,
                              75,
                              23);
            btnLoadOtherClan.Text = "Load";
            btnLoadOtherClan.Visible = false;
            btnLoadOtherClan.LeftClick += BtnLoadOtherClan_LeftClicked;

            lblClanMembers = new XNALabel(WindowManager);
            lblClanMembers.Name = "lblMembers";
            lblClanMembers.ClientRectangle =
                new Rectangle(tabMineOrOther.ClientRectangle.X,
                              tabMineOrOther.ClientRectangle.Bottom + 24,
                              0, 0);
            lblClanMembers.FontIndex = 1;
            lblClanMembers.Text = "MEMBERS:";

            lbClanMembers = new XNAListBox(WindowManager);
            lbClanMembers.Name = "lbClanMembers";
            lbClanMembers.ClientRectangle =
                new Rectangle(tabMineOrOther.ClientRectangle.X,
                              lblClanMembers.ClientRectangle.Bottom + 12,
                              140,
                              ClientRectangle.Height -
                              lblClanMembers.ClientRectangle.Bottom - 48);
            lbClanMembers.FontIndex = 1;
            lbClanMembers.Enabled = true;
            lbClanMembers.BackgroundTexture =
                AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbClanMembers.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbClanMembers.SelectedIndexChanged += LbClanMembers_SelectedIndexChanged;
            lbClanMembers.HoveredIndexChanged += LbClanMembers_HoveredIndexChanged;

            tabMemberRole = new XNAClientTabControl(WindowManager);
            tabMemberRole.Name = "tabMemberRole";
            tabMemberRole.ClientRectangle =
                new Rectangle(lbClanMembers.ClientRectangle.Right + 24,
                              lbClanMembers.ClientRectangle.Bottom - 24,
                              0, 0);
            tabMemberRole.FontIndex = 1;
            tabMemberRole.SelectedIndexChanged += TabMemberRole_SelectedTabChanged;
            tabMemberRole.AddTab("Gamer", 92);
            tabMemberRole.AddTab("Operator", 92);
            tabMemberRole.AddTab("Owner", 92);

            btnRemoveMember = new XNAClientButton(WindowManager);
            btnRemoveMember.Name = "btnRemoveMember";
            btnRemoveMember.ClientRectangle =
                new Rectangle(tabMemberRole.ClientRectangle.Right + 12,
                              tabMemberRole.ClientRectangle.Y,
                              92, 23);
            btnRemoveMember.FontIndex = 1;
            btnRemoveMember.Text = "Remove";
            btnRemoveMember.LeftClick += BtnRemoveMember_LeftClicked;
            btnRemoveMember.Visible = false;

            AddChild(tabMineOrOther);
            AddChild(tbOtherClan);
            AddChild(btnLoadOtherClan);
            AddChild(lblClanMembers);
            AddChild(lbClanMembers);
            AddChild(tabMemberRole);
            AddChild(btnRemoveMember);
            base.Initialize();
        }
    }
}
