using ClientGUI;
using Localization;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A panel that is used to verify and display map sharing status.
    /// </summary>
    class MapSharingConfirmationPanel : XNAPanel
    {
        public MapSharingConfirmationPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        private readonly string MapSharingRequestText = 
            ("The game host has selected a map that" + Environment.NewLine +
            "doens't exist on your local installation.").L10N("UI:Main:MapSharingRequestText");

        private readonly string MapSharingDownloadText =
            "Downloading map...".L10N("UI:Main:MapSharingDownloadText");

        private readonly string MapSharingFailedText =
            ("Downloading map failed. The game host" + Environment.NewLine +
            "needs to change the map or you will be" + Environment.NewLine +
            "unable to participate in the match.").L10N("UI:Main:MapSharingFailedText");

        public event EventHandler MapDownloadConfirmed;

        private XNALabel lblDescription;
        private XNAClientButton btnDownload;

        public override void Initialize()
        {
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;

            Name = nameof(MapSharingConfirmationPanel);
            BackgroundTexture = AssetLoader.LoadTexture("msgboxform.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = nameof(lblDescription);
            lblDescription.X = UIDesignConstants.EMPTY_SPACE_SIDES;
            lblDescription.Y = UIDesignConstants.EMPTY_SPACE_TOP;
            lblDescription.Text = MapSharingRequestText;
            AddChild(lblDescription);

            Width = lblDescription.Right + UIDesignConstants.EMPTY_SPACE_SIDES;

            btnDownload = new XNAClientButton(WindowManager);
            btnDownload.Name = nameof(btnDownload);
            btnDownload.Width = UIDesignConstants.BUTTON_WIDTH_92;
            btnDownload.Y = lblDescription.Bottom + UIDesignConstants.EMPTY_SPACE_TOP * 2;
            btnDownload.Text = "Download".L10N("UI:Main:ButtonDownload");
            btnDownload.LeftClick += (s, e) => MapDownloadConfirmed?.Invoke(this, EventArgs.Empty);
            AddChild(btnDownload);
            btnDownload.CenterOnParentHorizontally();

            Height = btnDownload.Bottom + UIDesignConstants.EMPTY_SPACE_BOTTOM;

            base.Initialize();

            CenterOnParent();

            Disable();
        }

        public void ShowForMapDownload()
        {
            lblDescription.Text = MapSharingRequestText;
            btnDownload.AllowClick = true;
            Enable();
        }

        public void SetDownloadingStatus()
        {
            lblDescription.Text = MapSharingDownloadText;
            btnDownload.AllowClick = false;
        }

        public void SetFailedStatus()
        {
            lblDescription.Text = MapSharingFailedText;
            btnDownload.AllowClick = false;
        }
    }
}
