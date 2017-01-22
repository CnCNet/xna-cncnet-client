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
    public class NewClanWindow : XNAWindow
    {

        XNALabel lblCreateClan;
        XNALabel lblClanName;
        XNATextBox tbClanName;
        XNAClientButton btnCreateClan;
        XNAClientButton btnCancel;
        XNAMessageBox messageBox;
        WindowManager wm;
        CnCNetManager cm;

        public NewClanWindow(WindowManager wm, CnCNetManager cm) : base(wm)
        {
            this.cm = cm;
            this.wm = wm;
            cm.CncServ.ClanServices.ReceivedCreateClanResponse += CreateClanResponse;
        }

        public override void Initialize()
        {
            Name = "NewClanWindow";
            ClientRectangle = new Rectangle(0,0,200,100);
            BackgroundTexture = AssetLoader.LoadTextureUncached("privatemessagebg.png");
            lblCreateClan = new XNALabel(wm);
            lblCreateClan.Name = "lblCreateClan";
            lblCreateClan.FontIndex = 1;
            lblCreateClan.Text = "CREATE A NEW CLAN";
            AddChild(lblCreateClan);
            lblCreateClan.CenterOnParent();
            lblCreateClan.ClientRectangle =
                new Rectangle(lblCreateClan.ClientRectangle.X, 12,
                              lblCreateClan.ClientRectangle.Width,
                              lblCreateClan.ClientRectangle.Height);

            lblClanName = new XNALabel(wm);
            lblClanName.Name = "lblClanName";
            lblClanName.FontIndex = 1;
            lblClanName.ClientRectangle =
                new Rectangle(12, lblCreateClan.ClientRectangle.Bottom + 12, 0,0);
            lblClanName.Text = "NAME:";

            tbClanName = new XNATextBox(wm);
            tbClanName.Name = "tbClanName";
            tbClanName.FontIndex = 1;
            tbClanName.ClientRectangle =
                new Rectangle(lblClanName.ClientRectangle.Right + 12,
                              lblClanName.ClientRectangle.Y,
                              120, 24);
            tbClanName.MaximumTextLength = 14;

            btnCreateClan = new XNAClientButton(wm);
            btnCreateClan.Name = "btnCreateClan";
            btnCreateClan.FontIndex = 1;
            btnCreateClan.ClientRectangle =
                new Rectangle(12, lblClanName.ClientRectangle.Bottom + 12, 75, 23);
            btnCreateClan.Text = "Create";
            btnCreateClan.LeftClick += BtnCreateClan_LeftClicked;

            btnCancel = new XNAClientButton(wm);
            btnCancel.Name = "btnCancel";
            btnCancel.FontIndex = 1;
            btnCancel.ClientRectangle =
                new Rectangle(btnCreateClan.ClientRectangle.Right + 6,
                              btnCreateClan.ClientRectangle.Y,
                              75, 23);
            btnCancel.Text = "Cancel";
            btnCancel.LeftClick += BtnCancel_LeftClicked;

            AddChild(lblClanName);
            AddChild(tbClanName);
            AddChild(btnCreateClan);
            AddChild(btnCancel);
            CenterOnParent();
            base.Initialize();
        }

        private void BtnCancel_LeftClicked(object s, EventArgs e)
        {
            Disable();
        }

        private void BtnCreateClan_LeftClicked(object s, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tbClanName.Text)
                && tbClanName.Text.Length < 15)
            {
                cm.CncServ.ClanServices.CreateClan("abc", tbClanName.Text);
                Disable();
                tbClanName.Text = "";
            }
        }

        private void CreateClanResponse(object s, ClanEventArgs e)
        {
            if (e.Result == "FAIL")
            {
                messageBox = new XNAMessageBox(wm, "Unable to create clan",
                                               e.FailMessage,
                                               DXMessageBoxButtons.OK);
                messageBox.Show();
            }
        }
    }
}
