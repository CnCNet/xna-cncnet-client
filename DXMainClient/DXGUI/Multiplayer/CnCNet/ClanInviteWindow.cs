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
    public class ClanInviteWindow : XNAWindow
    {
        XNAMessageBox messageBox;
        XNALabel lblNewInvite;
        XNALabel lblTo;
        XNATextBox tbTo;
        XNALabel lblMessage;
        XNATextBox tbMessage;
        XNAClientButton btnSend;
        XNAClientButton btnCancel;

        WindowManager wm;
        CnCNetManager cm;

        public ClanInviteWindow(WindowManager wm, CnCNetManager cm) : base(wm)
        {
            this.wm = wm;
            this.cm = cm;
        }

        public override void Initialize()
        {
            Name = "NewInviteWindow";
            ClientRectangle = new Rectangle(0, 0, 400, 200);
            CenterOnParent();

            BackgroundTexture = AssetLoader.LoadTextureUncached("privatemessagebg.png");
            lblNewInvite = new XNALabel(wm);
            lblNewInvite.Name = "lblNewInvite";
            lblNewInvite.FontIndex = 1;
            lblNewInvite.Text = "NEW INVITATION";
            AddChild(lblNewInvite);
            lblNewInvite.CenterOnParent();
            lblNewInvite.ClientRectangle =
                new Rectangle(lblNewInvite.ClientRectangle.X, 12,
                              lblNewInvite.ClientRectangle.Width,
                              lblNewInvite.ClientRectangle.Height);

            lblTo = new XNALabel(wm);
            lblTo.Name = "lblTo";
            lblTo.FontIndex = 1;
            lblTo.ClientRectangle =
                new Rectangle(12, lblNewInvite.ClientRectangle.Bottom + 12, 0, 0);
            lblTo.Text = "To:";

            tbTo = new XNATextBox(wm);
            tbTo.Name = "tbTo";
            tbTo.ClientRectangle =
                new Rectangle(lblTo.ClientRectangle.Width + 6,
                              lblTo.ClientRectangle.Y,
                              ClientRectangle.Width - 12,
                              24);
            tbTo.MaximumTextLength = 14;

            lblMessage = new XNALabel(wm);
            lblMessage.Name = "lblMessage";
            lblMessage.FontIndex = 1;
            lblMessage.ClientRectangle =
                new Rectangle(12, tbTo.ClientRectangle.Bottom + 6, 0, 0);
            lblMessage.Text = "MESSAGE:";

            tbMessage = new XNATextBox(wm);
            tbMessage.Name = "tbMessage";
            tbMessage.ClientRectangle =
                new Rectangle(lblMessage.ClientRectangle.Right + 6,
                              lblMessage.ClientRectangle.Y,
                              ClientRectangle.Width -
                              lblMessage.ClientRectangle.Right - 18,
                              ClientRectangle.Height - lblMessage.ClientRectangle.Y - 48);

            lblTo.ClientRectangle =
                new Rectangle(tbMessage.ClientRectangle.X
                              - lblTo.ClientRectangle.Width - 6,
                              lblTo.ClientRectangle.Y,
                              lblTo.ClientRectangle.Width,
                              lblTo.ClientRectangle.Height);

            tbTo.ClientRectangle =
                new Rectangle(lblTo.ClientRectangle.Right + 6,
                              lblTo.ClientRectangle.Y,
                              ClientRectangle.Width - lblTo.ClientRectangle.Right - 18,
                              24);

            btnSend = new XNAClientButton(wm);
            btnSend.Name = "btnSend";
            btnSend.FontIndex = 1;
            btnSend.ClientRectangle =
                new Rectangle(12, tbMessage.ClientRectangle.Bottom + 12, 75, 23);
            btnSend.Text = "Send";
            btnSend.LeftClick += BtnSend_LeftClicked;

            btnCancel = new XNAClientButton(wm);
            btnCancel.Name = "btnCancel";
            btnCancel.FontIndex = 1;
            btnCancel.ClientRectangle =
                new Rectangle(btnSend.ClientRectangle.Right + 6,
                              btnSend.ClientRectangle.Y,
                              75, 23);
            btnCancel.Text = "Cancel";
            btnCancel.LeftClick += BtnCancel_LeftClicked;

            AddChild(lblTo);
            AddChild(tbTo);
            AddChild(lblMessage);
            AddChild(tbMessage);
            AddChild(btnSend);
            AddChild(btnCancel);
            base.Initialize();

        }

        private void BtnCancel_LeftClicked(object s, EventArgs e)
        {
            Disable();
        }

        private void BtnSend_LeftClicked(object s, EventArgs e)
        {
            if (tbTo.Text.Length > 0)
            {
                cm.CncServ.ClanServices.Invite("dis", tbTo.Text, cm.CncServ.ClanName,
                                               tbMessage.Text);
                tbTo.Text = "";
                tbMessage.Text = "";
                Disable();
            }
        }
        //private void InviteResponse(
    }
}
