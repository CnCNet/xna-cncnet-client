using ClientGUI;
using DTAClient.domain;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DTAClient.DXGUI
{
    public class UpdateQueryWindow : DXWindow
    {
        public delegate void UpdateAcceptedEventHandler(object sender, EventArgs e);
        public event UpdateAcceptedEventHandler UpdateAccepted;

        public delegate void UpdateDeclinedEventHandler(object sender, EventArgs e);
        public event UpdateDeclinedEventHandler UpdateDeclined;

        public UpdateQueryWindow(Game game) : base(game)
        {

        }

        DXLabel lblDescription;
        DXLabel lblUpdateSize;

        public override void Initialize()
        {
            Name = "UpdateQueryWindow";
            ClientRectangle = new Rectangle(0, 0, 251, 140);
            BackgroundTexture = AssetLoader.LoadTexture("updatequerybg.png");

            lblDescription = new DXLabel(Game);
            lblDescription.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblDescription.Text = String.Empty;
            lblDescription.Name = "lblDescription";

            DXLabel lblChangelogLink = new DXLabel(Game);
            lblChangelogLink.ClientRectangle = new Rectangle(12, 50, 0, 0);
            lblChangelogLink.Text = "Click here to view the changelog";
            lblChangelogLink.RemapColor = Color.Goldenrod;
            lblChangelogLink.Name = "lblChangelogLink";
            lblChangelogLink.LeftClick += LblChangelogLink_LeftClick;

            lblUpdateSize = new DXLabel(Game);
            lblUpdateSize.ClientRectangle = new Rectangle(12, 80, 0, 0);
            lblUpdateSize.Text = String.Empty;
            lblUpdateSize.Name = "lblUpdateSize";

            DXButton btnYes = new DXButton(Game);
            btnYes.ClientRectangle = new Rectangle(12, 110, 75, 23);
            btnYes.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnYes.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnYes.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnYes.Text = "Yes";
            btnYes.FontIndex = 1;
            btnYes.LeftClick += BtnYes_LeftClick;

            DXButton btnNo = new DXButton(Game);
            btnNo.ClientRectangle = new Rectangle(164, 110, 75, 23);
            btnNo.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnNo.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnNo.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnNo.Text = "No";
            btnNo.FontIndex = 1;
            btnNo.LeftClick += BtnNo_LeftClick;

            AddChild(lblDescription);
            AddChild(lblChangelogLink);
            AddChild(lblUpdateSize);
            AddChild(btnYes);
            AddChild(btnNo);

            base.Initialize();

            CenterOnParent();
        }

        private void LblChangelogLink_LeftClick(object sender, EventArgs e)
        {
            Process.Start(MainClientConstants.CHANGELOG_URL);
        }

        private void BtnYes_LeftClick(object sender, EventArgs e)
        {
            if (UpdateAccepted != null)
                UpdateAccepted(this, e);
        }

        private void BtnNo_LeftClick(object sender, EventArgs e)
        {
            if (UpdateDeclined != null)
                UpdateDeclined(this, e);
        }

        public void SetInfo(string version, int updateSize)
        {
            lblDescription.Text = string.Format("Version {0} is available for download." + Environment.NewLine + "Do you wish to install it?", version);
            lblUpdateSize.Text = string.Format("The size of the update is {0} MB.", updateSize / 1000);
        }
    }
}
