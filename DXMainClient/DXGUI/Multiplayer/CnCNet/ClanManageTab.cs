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
        ClanManagerWindow cw;
        private static string loading = "Loading...";

        public ClanManageTab(WindowManager windowManager, CnCNetManager cm,
                             ClanManagerWindow cw, Rectangle location) :
        base(windowManager)
        {
            ClientRectangle = location;
            this.cm = cm;
            this.wm = windowManager;
            this.cw = cw;

            currentClanMembers = new List<ClanMember>(){};
            cm.CncServ.ClanServices.ReceivedClanMemberNext += DoNextClanMember;
            cm.CncServ.ClanServices.ReceivedClanMemberComplete +=
                DoCompleteClanMember;

            cm.CncServ.ClanServices.ReceivedChangeRoleResponse +=
                DoChangeRoleResponse;

            cm.CncServ.ClanServices.ReceivedRemoveMemberResponse +=
                DoRemoveMemberResponse;
            cw.ViewClanRequested += LoadSearchedClan;

        }

        private void TabMineOrOther_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabMineOrOther.SelectedTab == 1)
            {
                tbOtherClan.Visible = true;
                btnLoadOtherClan.Visible = true;
                SelectedClan = tbOtherClan.Text;
            }
            else
            {
                tbOtherClan.Visible = false;
                btnLoadOtherClan.Visible = false;
                SelectedClan = cm.CncServ.ClanName;
                Refresh();
            }
        }

        private void BtnLoadOtherClan_LeftClicked(object s, EventArgs e)
        {
            SelectedClan = tbOtherClan.Text;
            Refresh();
        }

        private void LoadSearchedClan(object s, SearchEventArgs e)
        {
            Console.WriteLine("Loading searched clan");
            tbOtherClan.Text = e.String;
            tabMineOrOther.SelectedTab = 1;
            BtnLoadOtherClan_LeftClicked(null, null);
        }
        private void DoChangeRoleResponse(object sender, ClanEventArgs e)
        {
            if (e.Result == "SUCCESS")
            {
                Console.WriteLine("SUCCESS {0} {1} {2}", e.ClanName,
                                  e.Member.Name, e.Member.Role);
                ClanMember m = currentClanMembers.Find(x => x.Name == e.Member.Name);
                if (m != null)
                    m.Role = e.Member.Role;
            }
            else
            {
                LbClanMembers_SelectedIndexChanged(null, EventArgs.Empty);
                messageBox = new XNAMessageBox(wm, e.FailMessage,
                    string.Format("Failed To Change Role For {0}", e.Member.Name),
                    DXMessageBoxButtons.OK);

                messageBox.Show();
                Console.WriteLine("FAIL {0} {1} {2}", e.ClanName, e.Member.Name,
                                  e.Member.Role);
            }
        }
        private void LbClanMembers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbClanMembers.SelectedIndex < 0)
            {
                btnRemoveMember.Visible = false;
                btnMemberRoleOperator.Visible = false;
                btnMemberRoleOwner.Visible = false;
                btnMemberRoleGamer.Visible = false;
                return;
            }
            if (lbClanMembers.Items[lbClanMembers.SelectedIndex].Text == loading)
            {
                lbClanMembers.SelectedIndex = -1;
                return;
            }

            ClanMember m = currentClanMembers[lbClanMembers.SelectedIndex];

            btnMemberRoleOwner.SelectedTab = -1;
            btnMemberRoleOperator.SelectedTab = -1;
            btnMemberRoleGamer.SelectedTab = -1;

            switch (m.Role)
            {
            case "Owner": btnMemberRoleOwner.SelectedTab = 0; break;
            case "Operator": btnMemberRoleOperator.SelectedTab = 0; break;
            case "Gamer": btnMemberRoleGamer.SelectedTab = 0; break;
            }

            btnMemberRoleOperator.Visible = true;
            btnMemberRoleOwner.Visible = true;
            btnMemberRoleGamer.Visible = true;
            btnRemoveMember.Visible = true;
        }

        private void Btn_RoleChange(XNAClientTabControl b)
        {
            ClanMember m = currentClanMembers[lbClanMembers.SelectedIndex];

            btnMemberRoleOwner.SelectedTab = -1;
            btnMemberRoleOperator.SelectedTab = -1;
            btnMemberRoleGamer.SelectedTab = -1;

            Console.WriteLine("Changing role to {0}",b.Text);
            switch (b.Text)
            {
            case "Owner":
                btnMemberRoleOwner.SelectedTab = 0;
                break;
            case "Operator": btnMemberRoleOperator.SelectedTab = 0; break;
            case "Gamer": btnMemberRoleGamer.SelectedTab = 0; break;
            }
            if (m.Role != b.Text)
                cm.CncServ.ClanServices.ChangeRole("1", SelectedClan, m.Name,
                                                   b.Text);

        }

        private int RemoveDistinguisher = 0;
        private void BtnRemoveMember_LeftClicked(object s, EventArgs e)
        {
            string name = currentClanMembers[lbClanMembers.SelectedIndex].Name;
            cm.CncServ.ClanServices.RemoveMember(
                RemoveDistinguisher.ToString(), name, SelectedClan);
        }

        private void DoRemoveMemberResponse(object s, ClanEventArgs m)
        {
            if (m.Result == "FAIL")
            {
                messageBox = new XNAMessageBox(wm, m.FailMessage,
                    string.Format("Failed Remove user {0}", m.Member.Name),
                    DXMessageBoxButtons.OK);
                messageBox.Show();
            }
            else Refresh();
        }

        private int MembersDistinguisher = 0;
        public void Refresh()
        {
            if (!cm.CncServ.IsAuthenticated)
                return;
            MembersDistinguisher++;
            currentClanMembers.Clear();
            lbClanMembers.Clear();
            lbClanMembers.SelectedIndex = -1;

            if (tabMineOrOther.SelectedTab == 1)
                SelectedClan = tbOtherClan.Text;
            else
                SelectedClan = cm.CncServ.ClanName;
            btnMemberRoleOwner.Visible = false;
            btnMemberRoleOperator.Visible = false;
            btnMemberRoleGamer.Visible = false;

            if (!string.IsNullOrEmpty(SelectedClan))
                lblManageClan.Text = "-= " + SelectedClan + " =-";
            else
                lblManageClan.Text = "-= NONE =-";
            lblManageClan.ClientRectangle =
                new Rectangle((ClientRectangle.Width / 2) -
                              (lblManageClan.ClientRectangle.Width / 2),
                              lblManageClan.ClientRectangle.Y,
                              lblManageClan.ClientRectangle.Width,
                              lblManageClan.ClientRectangle.Height);

            cm.CncServ.ClanServices.ListClanMembers(MembersDistinguisher.ToString(),
                                                    SelectedClan);
            lbClanMembers.AddItem(loading);
        }

        private void DoNextClanMember(object s, ClanEventArgs ec)
        {
            if (ec.Distinguisher == MembersDistinguisher.ToString())
            {
                currentClanMembers.Add(ec.Member);
                int last = lbClanMembers.Items.Count - 1;
                if (last >= 0 && lbClanMembers.Items[last].Text == loading)
                    lbClanMembers.Items.RemoveAt(last);
                lbClanMembers.AddItem(ec.Member.Name);
                lbClanMembers.AddItem(loading);
            }
        }
        private void DoCompleteClanMember(object s, ClanEventArgs e)
        {
            Console.WriteLine("DoCompleteClanMember");
            if (e.Distinguisher == MembersDistinguisher.ToString())
            {
                Console.WriteLine("matched");
                int last = lbClanMembers.Items.Count - 1;
                if (last >= 0 && lbClanMembers.Items[last].Text == loading)
                {
                    lbClanMembers.Items.RemoveAt(last);
                    Console.WriteLine("Deleted {0}", last);
                }
            }
        }
    }
}
