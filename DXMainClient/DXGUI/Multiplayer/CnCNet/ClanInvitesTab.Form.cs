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
        public override void Initialize()
        {
            Name = "ClanInvitesTab";

            lblIncoming = new XNALabel(WindowManager);
            lblIncoming.Name = "lblIncoming";
            lblIncoming.ClientRectangle =new Rectangle(24, 12, 0, 0);
            lblIncoming.FontIndex = 1;
            lblIncoming.Text = "INCOMING:";

            lbInInvites = new XNAListBox(WindowManager);
            lbInInvites.Name = "lbInInvites";
            lbInInvites.ClientRectangle =
                new Rectangle(lblIncoming.ClientRectangle.X,
                              lblIncoming.ClientRectangle.Bottom + 6,
                              ClientRectangle.Width - 48,
                              160)
;
            //lbInInvites.SelectedIndexChanged += LbUserList_SelectedIndexChanged;
            lbInInvites.BackgroundTexture =
                AssetLoader.CreateTexture(new Color(0,0,0,128),1,1);
            lbInInvites.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            btnInAccept = new XNAClientButton(WindowManager);
            btnInAccept.Name = "btnAccept";
            btnInAccept.ClientRectangle =
                new Rectangle(lbInInvites.ClientRectangle.X,
                              lbInInvites.ClientRectangle.Bottom + 12,
                              92, 23);
            btnInAccept.Text = "Accept";
            //btnInAccept.LeftClick += BtnIncAccept_LeftClick;

            btnInDecline = new XNAClientButton(WindowManager);
            btnInDecline.Name = "btnDecline";
            btnInDecline.ClientRectangle =
                new Rectangle(btnInAccept.ClientRectangle.Right + 12,
                              btnInAccept.ClientRectangle.Y,
                              92, 23);
            btnInDecline.Text = "Decline";
            //btnInDecline.LeftClick += BtnInDecline_LeftClick;

            lblOutgoing = new XNALabel(WindowManager);
            lblOutgoing.Name = "lblOutgoing";
            lblOutgoing.ClientRectangle =
                new Rectangle(lbInInvites.ClientRectangle.X,
                              btnInAccept.ClientRectangle.Bottom + 12, 0, 0);
            lblOutgoing.FontIndex = 1;
            lblOutgoing.Text = "OUTGOING:";

            lbOutInvites = new XNAListBox(WindowManager);
            lbOutInvites.Name = "lblOutInvites";
            lbOutInvites.ClientRectangle =
                new Rectangle(lblOutgoing.ClientRectangle.X,
                              lblOutgoing.ClientRectangle.Bottom + 6,
                              ClientRectangle.Width - 48,
                              160);
            //lbOutInvites.SelectedIndexChanged += LbUserList_SelectedIndexChanged;
            lbOutInvites.BackgroundTexture =
                AssetLoader.CreateTexture(new Color(0,0,0,128),1,1);
            lbOutInvites.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            btnOutDelete = new XNAClientButton(WindowManager);
            btnOutDelete.Name = "btnOutDelete";
            btnOutDelete.ClientRectangle =
                new Rectangle(lbOutInvites.ClientRectangle.X,
                              lbOutInvites.ClientRectangle.Bottom + 12,
                              92, 23);
            btnOutDelete.Text = "Delete";
            //btnOutDelete.LeftClick += BtnOutDelete_LeftClick;


            AddChild(lblIncoming);
            AddChild(lbInInvites);
            AddChild(btnInAccept);
            AddChild(btnInDecline);
            AddChild(lblOutgoing);
            AddChild(lbOutInvites);
            AddChild(btnOutDelete);
            //WindowManager.AddAndInitializeControl(notificationBox);
            base.Initialize();
        }
    }
}
