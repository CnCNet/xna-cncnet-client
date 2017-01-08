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
    partial class ClanInvitesTab : XNAPanel
    {
        XNALabel lblIncoming;
        XNALabel lblOutgoing;
        XNAListBox lbInInvites;
        XNAListBox lbOutInvites;
        XNAClientButton btnInAccept;
        XNAClientButton btnInDecline;
        XNAClientButton btnOutDelete;
        XNAMessageBox messageBox;
        List<InviteEventArgs> currentInInvites;
        List<InviteEventArgs> currentOutInvites;
        WindowManager wm;
        CnCNetManager cm;
        ClanManagerWindow cw;

        string loading = "Loading...";

        public ClanInvitesTab(WindowManager windowManager, CnCNetManager cm,
                              ClanManagerWindow cw, Rectangle location) :
        base(windowManager)
        {
            ClientRectangle = location;
            this.cm = cm;
            this.wm = windowManager;
            this.cw = cw;
            cm.CncServ.ClanServices.ReceivedListInvitesNext += NextListInvites;
            cm.CncServ.ClanServices.ReceivedListInvitesComplete +=
                CompleteListInvites;
            cm.CncServ.ClanServices.ReceivedListClanInvitesNext +=
                NextListClanInvites;
            cm.CncServ.ClanServices.ReceivedListClanInvitesComplete +=
                CompleteListClanInvites;
            cm.CncServ.ClanServices.ReceivedAcceptInviteResponse += AcceptInviteResp;
            currentInInvites = new List<InviteEventArgs>() {};
            currentOutInvites = new List<InviteEventArgs>() {};
            outInvitesDist = 0;
            inInvitesDist = 0;
        }

        int outInvitesDist;
        int inInvitesDist;
        public void Refresh()
        {
            if (!cm.CncServ.IsAuthenticated)
                return;
            lbInInvites.Clear();
            lbInInvites.SelectedIndex = -1;
            currentInInvites.Clear();

            lbOutInvites.Clear();
            lbOutInvites.SelectedIndex = -1;
            currentOutInvites.Clear();

            outInvitesDist++;
            inInvitesDist++;
            lbInInvites.AddItem(loading);
            lbOutInvites.AddItem(loading);
            if (cm.IsConnected && cm.CncServ.IsAuthenticated)
            {
                cm.CncServ.ClanServices.ListInvites(inInvitesDist.ToString());
                cm.CncServ.ClanServices.ListClanInvites(outInvitesDist.ToString(),
                                                        cm.CncServ.ClanName);
            }
        }

        private void NextListInvites(object s, InviteEventArgs i)
        {
            if (i.Distinguisher == inInvitesDist.ToString())
            {
                currentInInvites.Add(i);
                int last = lbInInvites.Items.Count -1;
                if (last >= 0 && lbInInvites.Items[last].Text == loading)
                {
                    lbInInvites.Items.RemoveAt(last);
                }
                lbInInvites.AddItem(string.Format("From: {0} -- {1}", i.ClanName,
                                                  i.Comment));
                lbInInvites.AddItem(loading);
            }
        }
        private void CompleteListInvites(object s, InviteEventArgs i)
        {
            if (i.Distinguisher == inInvitesDist.ToString())
            {
                int last = lbInInvites.Items.Count -1;
                if (last >= 0 && lbInInvites.Items[last].Text == loading)
                {
                    lbInInvites.Items.RemoveAt(last);
                }
            }
        }

        private void NextListClanInvites(object s, InviteEventArgs i)
        {
            if (i.Distinguisher == outInvitesDist.ToString())
            {
                currentOutInvites.Add(i);
                int last = lbOutInvites.Items.Count - 1;
                if (last >= 0 && lbOutInvites.Items[last].Text == loading)
                {
                    lbOutInvites.Items.RemoveAt(last);
                }
                lbOutInvites.AddItem(string.Format("To: {0} -- {1}", i.UserName,
                                                   i.Comment));
                lbOutInvites.AddItem(loading);
            }
        }
        private void CompleteListClanInvites(object s, InviteEventArgs i)
        {
            if (i.Distinguisher == outInvitesDist.ToString())
            {
                int last = lbOutInvites.Items.Count - 1;
                if (last >= 0 && lbOutInvites.Items[last].Text == loading)
                {
                    lbOutInvites.Items.RemoveAt(last);
                }
            }
        }

        private void BtnIncAccept_LeftClick(object s, EventArgs e)
        {
            if (lbInInvites.SelectedIndex < 0 ||
                lbInInvites.SelectedIndex > currentInInvites.Count)
                return;
            string id = currentInInvites[lbInInvites.SelectedIndex].ID;

            Console.WriteLine("Accepting invitation #{0}", id);

            cm.CncServ.ClanServices.AcceptInvite("dist", id);

            lbInInvites.Items.RemoveAt(lbInInvites.SelectedIndex);
            currentInInvites.RemoveAt(lbInInvites.SelectedIndex);
            lbInInvites.SelectedIndex = -1;
        }
        private void AcceptInviteResp(object s, InviteEventArgs i)
        {
            if (i.Result == "FAIL")
            {
                messageBox = new XNAMessageBox(wm, i.FailMessage,
                                               "Failed to accept invitation",
                                               DXMessageBoxButtons.OK);
                messageBox.Show();
                Refresh();
            }
            else if (i.Result == "SUCCESS")
            {
                cw.ClanManageTab.Refresh();
            }
        }
        private void BtnInDecline_LeftClick(object s, EventArgs e)
        {
            if (lbInInvites.SelectedIndex < 0 ||
                lbInInvites.SelectedIndex > currentInInvites.Count)
                return;
            string id = currentInInvites[lbInInvites.SelectedIndex].ID;
            cm.CncServ.ClanServices.DeclineInvite("dist", id);
            lbInInvites.Items.RemoveAt(lbInInvites.SelectedIndex);
            currentInInvites.RemoveAt(lbInInvites.SelectedIndex);
            lbInInvites.SelectedIndex = -1;
        }

        private void BtnOutDelete_LeftClick(object s, EventArgs e)
        {
            if (lbOutInvites.SelectedIndex < 0 ||
                lbOutInvites.SelectedIndex > currentOutInvites.Count)
                return;
            string id = currentOutInvites[lbOutInvites.SelectedIndex].ID;
            cm.CncServ.ClanServices.DeclineInvite("dist", id);
            lbOutInvites.Items.RemoveAt(lbOutInvites.SelectedIndex);
            currentOutInvites.RemoveAt(lbOutInvites.SelectedIndex);
            lbOutInvites.SelectedIndex = -1;
        }
    }
}
