using System;
using ClientCore;
using ClientCore.Extensions;
using ClientGUI;
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
            lblDescription.Name = nameof(lblDescription);
            lblDescription.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblDescription.Text = ("Version {0} is available.\n\nManual download and installation is\nrequired.").L10N("Client:Main:ManualDownloadAvailable");

            var btnDownload = new XNAClientButton(WindowManager);
            btnDownload.Name = nameof(btnDownload);
            btnDownload.ClientRectangle = new Rectangle(12, 110, 110, 23);
            btnDownload.Text = "View Downloads".L10N("Client:Main:ButtonViewDownloads");
            btnDownload.LeftClick += BtnDownload_LeftClick;

            var btnClose = new XNAClientButton(WindowManager);
            btnClose.Name = nameof(btnClose);
            btnClose.ClientRectangle = new Rectangle(147, 110, 92, 23);
            btnClose.Text = "Close".L10N("Client:Main:ButtonClose");
            btnClose.LeftClick += BtnClose_LeftClick;

            AddChild(lblDescription);
            AddChild(btnDownload);
            AddChild(btnClose);

            base.Initialize();

            // loaded from INI
            descriptionText = lblDescription.Text;

            CenterOnParent();
        }

        private void BtnDownload_LeftClick(object sender, EventArgs e)
            => ProcessLauncher.StartShellProcess(downloadUrl);

        private void BtnClose_LeftClick(object sender, EventArgs e)
            => Closed?.Invoke(this, e);

        public void SetInfo(string version, string downloadUrl)
        {
            this.downloadUrl = downloadUrl;
            lblDescription.Text = string.Format(descriptionText, version);
        }
    }
}