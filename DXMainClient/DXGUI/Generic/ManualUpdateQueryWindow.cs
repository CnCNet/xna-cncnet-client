using System;
using System.Diagnostics;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// A window that redirects users to manually download an update.
    /// </summary>
    public class ManualUpdateQueryWindow : XNAWindow
    {
        public delegate void ClosedEventHandler(object sender, EventArgs e);
        public event ClosedEventHandler Closed;

        public ManualUpdateQueryWindow(WindowManager windowManager) : base(windowManager) { }

        private XNALabel lblDescription;

        private string downloadUrl;
        private string descriptionText;

        public override void Initialize()
        {
            Name = "ManualUpdateQueryWindow";
            ClientRectangle = new Rectangle(0, 0, 251, 140);
            BackgroundTexture = AssetLoader.LoadTexture("updatequerybg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblDescription.Text = ("Version {0} is available." + Environment.NewLine + Environment.NewLine +
                "Manual download and installation is" + Environment.NewLine + "required.").L10N("UI:Main:ManualDownloadAvailable");

            var btnDownload = new XNAClientButton(WindowManager);
            btnDownload.Name = "btnDownload";
            btnDownload.ClientRectangle = new Rectangle(12, 110, 110, 23);
            btnDownload.Text = "View Downloads".L10N("UI:Main:ButtonViewDownloads");
            btnDownload.LeftClick += BtnDownload_LeftClick;

            var btnClose = new XNAClientButton(WindowManager);
            btnClose.Name = "btnClose";
            btnClose.ClientRectangle = new Rectangle(147, 110, 92, 23);
            btnClose.Text = "Close".L10N("UI:Main:ButtonClose");
            btnClose.LeftClick += BtnClose_LeftClick;

            AddChild(lblDescription);
            AddChild(btnDownload);
            AddChild(btnClose);

            base.Initialize();

            descriptionText = lblDescription.Text.Replace("@", Environment.NewLine);

            CenterOnParent();
        }

        private void BtnDownload_LeftClick(object sender, EventArgs e)
            => Process.Start(downloadUrl);

        private void BtnClose_LeftClick(object sender, EventArgs e)
            => Closed?.Invoke(this, e);

        public void SetInfo(string version, string downloadUrl)
        {
            this.downloadUrl = downloadUrl;
            lblDescription.Text = string.Format(descriptionText, version);
        }
    }
}