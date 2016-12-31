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

        public ClanManageTab(WindowManager windowManager, CnCNetManager cm,
                             Rectangle location) : base(windowManager)
        {
            ClientRectangle = location;
            this.cm = cm;
            currentClanMembers = new List<ClanMember>(){};
            cm.CncServ.ClanServices.ReceivedNextClanMember += DoNextClanMember;
            cm.CncServ.ClanServices.ReceivedChangeRoleResponse +=
                DoChangeRoleResponse;
            cm.CncServ.ClanServices.ReceivedRemoveMemberResponse +=
                DoRemoveMemberResponse;
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
            Refresh();
        }

        private void TabMemberRole_SelectedTabChanged(object sender, EventArgs e)
        {
            ClanMember m = currentClanMembers[lbClanMembers.SelectedIndex];

            if (tabMemberRole.SelectedTab == 0)
            {
                if (m.Role != "Gamer")
                    cm.CncServ.ClanServices.ChangeRole("1", SelectedClan, m.Name,
                                                       "Gamer");
            }
            else if (tabMemberRole.SelectedTab == 1)
            {
                if (m.Role != "Operator")
                    cm.CncServ.ClanServices.ChangeRole("1", SelectedClan, m.Name,
                                                       "Operator");
            }
            else if (tabMemberRole.SelectedTab == 2)
            {
                if (m.Role != "Owner")
                    cm.CncServ.ClanServices.ChangeRole("1", SelectedClan, m.Name,
                                                       "Owner");
            }
        }

        private void DoChangeRoleResponse(object sender, ClanMemberEventArgs e)
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
                Console.WriteLine("FAIL {0} {1} {2}", e.ClanName, e.Member.Name,
                                  e.Member.Role);
            }
        }
        private void LbClanMembers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbClanMembers.SelectedIndex < 0)
            {
                tabMemberRole.Visible = false;
                return;
            }
            ClanMember m = currentClanMembers[lbClanMembers.SelectedIndex];

            if (m.Role == "Owner")
                tabMemberRole.SelectedTab = 2;
            if (m.Role == "Operator")
                tabMemberRole.SelectedTab = 1;
            if (m.Role == "Gamer")
                tabMemberRole.SelectedTab = 0;
        }

        private void LbClanMembers_HoveredIndexChanged(object sender, EventArgs e)
        {
            /* Tried to make some fancy hover thing, but it didn't work.
            if (lbClanMembers.SelectedIndex >= 0)
                return;

            if (lbClanMembers.HoveredIndex < 0 ||
                 lbClanMembers.HoveredIndex > lbClanMembers.Items.Count)

            {
                tabMemberRole.Visible = false;
                btnRemoveMember.Visible = false;
                return;
            }
            tabMemberRole.ClientRectangle =
                new Rectangle(lbClanMembers.ClientRectangle.Right + 6,
                              ((lbClanMembers.HoveredIndex - lbClanMembers.TopIndex)
                               * lbClanMembers.LineHeight) + lbClanMembers.ClientRectangle.Y,
                              140, 0);
            tabMemberRole.Visible = true;
            btnRemoveMember.ClientRectangle =
                new Rectangle(tabMemberRole.ClientRectangle.Right + 12,
                              tabMemberRole.ClientRectangle.Y,
                              92, 23);
            btnRemoveMember.Visible = true;
            */
        }

        private int RemoveDistinguisher = 0;
        private void BtnRemoveMember_LeftClicked(object s, EventArgs e)
        {
            string name = currentClanMembers[lbClanMembers.SelectedIndex].Name;
            cm.CncServ.ClanServices.RemoveMember(
                RemoveDistinguisher.ToString(), name, SelectedClan);
        }

        private void DoRemoveMemberResponse(object s, ClanMemberEventArgs m)
        {
            if (RemoveDistinguisher.ToString() == m.Distinguisher)
                Refresh();
        }

        private int MembersDistinguisher = 0;
        public void Refresh()
        {
            MembersDistinguisher++;
            currentClanMembers.Clear();
            lbClanMembers.Clear();
            if (tabMineOrOther.SelectedTab == 1)
                SelectedClan = tbOtherClan.Text;
            else
                SelectedClan = cm.CncServ.ClanName;

            cm.CncServ.ClanServices.ListClanMembers(MembersDistinguisher.ToString(),
                                                    SelectedClan);

        }

        private void DoNextClanMember(object s, ClanMemberEventArgs ec)
        {
            if (ec.Distinguisher == MembersDistinguisher.ToString())
            {
                currentClanMembers.Add(ec.Member);
                lbClanMembers.AddItem(ec.Member.Name);
            }
        }
    }
}
