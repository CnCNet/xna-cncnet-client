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
    public class NewClanWindow
    {

        XNALabel lblCreateClan;
        XNALabel lblClanName;
        XNAClientButton btnCreateClan;
        XNAClientButton btnCancel;

        WindowManager wm;
        CnCNetManager cm;

        public NewClanWindow(WindowManager wm, CnCNetManager cm) : base(wm)
        {
            this.cm = cm;
            this.wm = wm;
        }
    }

    public override void Initialize()
    {
        Name = "NewClanWindow";
        ClientRectangle = new Rectangle(0,0,200,100);

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
            new Rectangle(12, lblCreateClan.ClientRectangle.Bottom + 12, 0,0)
        lblClanName.Text = "NAME:";

        tbClanName = new XNATextBox(wm);
        tbClanName.Name = "tbClanName";
        tbClanName.FontIndex = 1;
        tbClanName.ClientRectangle =
            new Rectangle(tbClanName.ClientRectangle.Right + 12,
                          tbClanName.ClientRectangle.Y,
                          120, 24);
        tbClanName.MaximumTextLength = 14;

        btnCreateClan = new XNAClientButton(wm);
        btnCreateClan.Name = "btnCreateClan";
        btnCreateClan.FontIndex = 1;
        btnCreateClan.ClientRectangle =
            new Rectangle(12, lblClanNam.ClientRectangle.Bottom + 6, 75, 23);
        btnCreateClan.Text = "Create";
        //btnCreateClan.LeftClick += BtnCreateClan_LeftClicked;

        btnCancel = new XNAClientButton(wm);
        btnCancel.Name = "btnCancel";
        btnCancel.FontIndex = 1;
        btnCancel.ClientRectangle =
            new Rectangle(btnCreateClan.ClientRectangle.Right + 6,
                          btnCreateClan.ClientRectangle.Y,
                          75, 23);

    }
}
