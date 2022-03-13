using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Diagnostics;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// A window that asks the user whether they want to update their game.
    /// </summary>
    public class UpdateQueryWindow : XNAWindow
    {
        public delegate void UpdateAcceptedEventHandler(object sender, EventArgs e);
        public event UpdateAcceptedEventHandler UpdateAccepted;

        public delegate void UpdateDeclinedEventHandler(object sender, EventArgs e);
        public event UpdateDeclinedEventHandler UpdateDeclined;

        public UpdateQueryWindow(WindowManager windowManager) : base(windowManager) { }

        private XNALabel lblDescription;
        private XNALabel lblUpdateSize;

        private string changelogUrl;

        public override void Initialize()
        {
            changelogUrl = ClientConfiguration.Instance.ChangelogURL;

            Name = "UpdateQueryWindow";
            ClientRectangle = new Rectangle(0, 0, 251, 140);
            BackgroundTexture = AssetLoader.LoadTexture("updatequerybg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblDescription.Text = String.Empty;
            lblDescription.Name = nameof(lblDescription);

            var lblChangelogLink = new XNALinkLabel(WindowManager);
            lblChangelogLink.ClientRectangle = new Rectangle(12, 50, 0, 0);
            lblChangelogLink.Text = "View Changelog".L10N("UI:Main:ViewChangeLog");
            lblChangelogLink.IdleColor = Color.Goldenrod;
            lblChangelogLink.Name = nameof(lblChangelogLink);
            lblChangelogLink.LeftClick += LblChangelogLink_LeftClick;

            lblUpdateSize = new XNALabel(WindowManager);
            lblUpdateSize.ClientRectangle = new Rectangle(12, 80, 0, 0);
            lblUpdateSize.Text = String.Empty;
            lblUpdateSize.Name = nameof(lblUpdateSize);

            var btnYes = new XNAClientButton(WindowManager);
            btnYes.ClientRectangle = new Rectangle(12, 110, 75, 23);
            btnYes.Text = "Yes".L10N("UI:Main:ButtonYes");
            btnYes.LeftClick += BtnYes_LeftClick;
            btnYes.Name = nameof(btnYes);

            var btnNo = new XNAClientButton(WindowManager);
            btnNo.ClientRectangle = new Rectangle(164, 110, 75, 23);
            btnNo.Text = "No".L10N("UI:Main:ButtonNo");
            btnNo.LeftClick += BtnNo_LeftClick;
            btnNo.Name = nameof(btnNo);

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
            Process.Start(changelogUrl);
        }

        private void BtnYes_LeftClick(object sender, EventArgs e)
        {
            UpdateAccepted?.Invoke(this, e);
        }

        private void BtnNo_LeftClick(object sender, EventArgs e)
        {
            UpdateDeclined?.Invoke(this, e);
        }

        public void SetInfo(string version, int updateSize)
        {
            lblDescription.Text = string.Format(("Version {0} is available for download." + Environment.NewLine + "Do you wish to install it?").L10N("UI:Main:VersionAvailable"), version);
            if (updateSize >= 1000)
                lblUpdateSize.Text = string.Format("The size of the update is {0} MB.".L10N("UI:Main:UpdateSizeMB"), updateSize / 1000);
            else
                lblUpdateSize.Text = string.Format("The size of the update is {0} KB.".L10N("UI:Main:UpdateSizeKB"), updateSize);
        }
    }
}
