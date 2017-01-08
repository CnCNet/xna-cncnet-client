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
    public class ClanSearchWindow : XNAWindow
    {
        XNAMessageBox messageBox;
        XNALabel lblSearchTitle;
        XNATextBox tbSearchBox;
        XNAClientButton btnSearch;
        XNAListBox lbResults;
        XNAClientButton btnView;
        XNAClientButton btnClose;

        int searchDistinguisher;
        string loading = "Loading...";

        public event EventHandler<SearchEventArgs> ViewClanRequested;

        WindowManager wm;
        CnCNetManager cm;

        public ClanSearchWindow(WindowManager wm, CnCNetManager cm) : base(wm)
        {
            this.wm = wm;
            this.cm = cm;
            cm.CncServ.ClanServices.ReceivedSearchClanFail += DoFail;
            cm.CncServ.ClanServices.ReceivedSearchClanNext += DoNext;
            cm.CncServ.ClanServices.ReceivedSearchClanComplete += DoComplete;

        }

        public override void Initialize()
        {
            Name = "ClanSearchWindow";
            ClientRectangle = new Rectangle(0,0, 250, 400);
            CenterOnParent();

            BackgroundTexture = AssetLoader.LoadTextureUncached("privatemessagebg.png");

            lblSearchTitle = new XNALabel(wm);
            lblSearchTitle.Name = "lblSearchTitle";
            lblSearchTitle.FontIndex = 1;
            lblSearchTitle.Text = "SEARCH FOR CLAN";
            AddChild(lblSearchTitle);
            lblSearchTitle.CenterOnParent();
            lblSearchTitle.ClientRectangle =
                new Rectangle(lblSearchTitle.ClientRectangle.X, 12,
                              lblSearchTitle.ClientRectangle.Width,
                              lblSearchTitle.ClientRectangle.Height);

            tbSearchBox = new XNATextBox(wm);
            tbSearchBox.Name = "tbSearchBox";
            tbSearchBox.ClientRectangle =
                new Rectangle(12,
                              lblSearchTitle.ClientRectangle.Bottom + 16,
                              145, 23);
            tbSearchBox.FontIndex = 1;

            btnSearch = new XNAClientButton(wm);
            btnSearch.Name = "btnSearch";
            btnSearch.ClientRectangle =
                new Rectangle(tbSearchBox.ClientRectangle.Right + 6,
                              tbSearchBox.ClientRectangle.Y,
                              75, 23);
            btnSearch.FontIndex = 1;
            btnSearch.Text = "Search";
            btnSearch.LeftClick += BtnSearch_LeftClicked;


            lbResults = new XNAListBox(wm);
            lbResults.Name = "lbResults";
            lbResults.ClientRectangle =
                new Rectangle(12,
                              tbSearchBox.ClientRectangle.Bottom + 12,
                              145,
                              ClientRectangle.Height -
                               tbSearchBox.ClientRectangle.Bottom - 24);
            lbResults.FontIndex = 1;
            lbResults.BackgroundTexture =
                AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbResults.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            btnClose = new XNAClientButton(wm);
            btnClose.Name = "btnClose";
            btnClose.ClientRectangle =
                new Rectangle(lbResults.ClientRectangle.Right + 6,
                              lbResults.ClientRectangle.Bottom - 23,
                              75, 23);
            btnClose.Text = "Close";
            btnClose.LeftClick += BtnClose_LeftClicked;

            btnView = new XNAClientButton(wm);
            btnView.Name = "btnView";
            btnView.ClientRectangle =
                new Rectangle(btnClose.ClientRectangle.X,
                              btnClose.ClientRectangle.Y - 29,
                              75, 23);
            btnView.Text = "View";
            btnView.LeftClick += BtnView_LeftClicked;

            AddChild(tbSearchBox);
            AddChild(btnSearch);
            AddChild(lbResults);
            AddChild(btnView);
            AddChild(btnClose);
            base.Initialize();
        }
        private void BtnClose_LeftClicked(object s, EventArgs e)
        {
            Disable();
        }

        private void BtnView_LeftClicked(object s, EventArgs e)
        {
            if (lbResults.SelectedIndex >= 0)
            {
                string str = lbResults.Items[lbResults.SelectedIndex].Text;
                if (!String.IsNullOrEmpty(str))
                    ViewClanRequested?.Invoke(this, new SearchEventArgs(str));
                Disable();
            }
        }

        private void BtnSearch_LeftClicked(object s, EventArgs e)
        {
            lbResults.Clear();
            if (!String.IsNullOrEmpty(tbSearchBox.Text))
            {
                if (!tbSearchBox.Text.Contains("%"))
                    tbSearchBox.Text = "%"+ tbSearchBox.Text +"%";
                searchDistinguisher++;
                cm.CncServ.ClanServices.SearchClan(searchDistinguisher.ToString(),
                                                   tbSearchBox.Text);
            }
            else
            {
                cm.CncServ.ClanServices.SearchClan(searchDistinguisher.ToString(),
                                                   "%");
            }
            lbResults.SelectedIndex = -1;
            lbResults.AddItem(loading);
        }

        private void DoFail(object s, ClanEventArgs c)
        {
            messageBox = new XNAMessageBox(wm, c.FailMessage, "Search failed.",
                                           DXMessageBoxButtons.OK);
            messageBox.Show();
            if (c.Distinguisher == searchDistinguisher.ToString())
            {
                int last = lbResults.Items.Count - 1;
                if (last >= 0 && lbResults.Items[last].Text == loading)
                {
                    lbResults.Items.RemoveAt(last);
                }
            }
        }

        private void DoNext(object s, ClanEventArgs c)
        {
            if (c.Distinguisher == searchDistinguisher.ToString())
            {
                int last = lbResults.Items.Count - 1;
                if (last >= 0 && lbResults.Items[last].Text == loading)
                {
                    lbResults.Items.RemoveAt(last);
                }
                lbResults.AddItem(c.ClanName);
                lbResults.AddItem(loading);
            }
        }

        private void DoComplete(object s, ClanEventArgs c)
        {
            if (c.Distinguisher == searchDistinguisher.ToString())
            {
                int last = lbResults.Items.Count - 1;
                if (last >= 0 && lbResults.Items[last].Text == loading)
                {
                    lbResults.Items.RemoveAt(last);
                }
            }
        }
    }
    public class SearchEventArgs : EventArgs
    {
        public string String;
        public SearchEventArgs(string s)
        {
            String = s;
        }
    }
}
