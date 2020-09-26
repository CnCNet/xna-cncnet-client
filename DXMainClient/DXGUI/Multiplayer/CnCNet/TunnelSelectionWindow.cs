using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
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
    /// A window for selecting a CnCNet tunnel server.
    /// </summary>
    class TunnelSelectionWindow : XNAWindow
    {
        public TunnelSelectionWindow(WindowManager windowManager, TunnelHandler tunnelHandler) : base(windowManager)
        {
            this.tunnelHandler = tunnelHandler;
        }

        public event EventHandler<TunnelEventArgs> TunnelSelected;

        private readonly TunnelHandler tunnelHandler;
        private TunnelListBox lbTunnelList;
        private XNALabel lblDescription;

        public override void Initialize()
        {
            if (Initialized)
                return;

            Name = "TunnelSelectionWindow";

            BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.Text = "Line 1" + Environment.NewLine + "Line 2";
            lblDescription.X = UIDesignConstants.EMPTY_SPACE_SIDES;
            lblDescription.Y = UIDesignConstants.EMPTY_SPACE_TOP;
            AddChild(lblDescription);

            lbTunnelList = new TunnelListBox(WindowManager, tunnelHandler);
            lbTunnelList.Name = "lbTunnelList";
            lbTunnelList.Y = lblDescription.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            lbTunnelList.X = UIDesignConstants.EMPTY_SPACE_SIDES;
            AddChild(lbTunnelList);

            var btnApply = new XNAClientButton(WindowManager);
            btnApply.Name = "btnApply";
            btnApply.Width = UIDesignConstants.BUTTON_WIDTH_92;
            btnApply.Height = UIDesignConstants.BUTTON_HEIGHT;
            btnApply.Text = "Apply";
            btnApply.Y = lbTunnelList.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            AddChild(btnApply);
            btnApply.LeftClick += BtnApply_LeftClick;

            Width = lbTunnelList.Right + UIDesignConstants.EMPTY_SPACE_SIDES;
            Height = btnApply.Bottom + UIDesignConstants.EMPTY_SPACE_BOTTOM;
            btnApply.CenterOnParentHorizontally();

            base.Initialize();
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            Disable();
            if (!lbTunnelList.IsValidIndexSelected())
                return;

            CnCNetTunnel tunnel = tunnelHandler.Tunnels[lbTunnelList.SelectedIndex];
            TunnelSelected?.Invoke(this, new TunnelEventArgs(tunnel));
        }

        /// <summary>
        /// Sets the window's description and selects the tunnel server
        /// with the given address.
        /// </summary>
        /// <param name="description">The window description.</param>
        /// <param name="tunnelAddress">The address of the tunnel server to select.</param>
        public void Open(string description, string tunnelAddress = null)
        {
            lblDescription.Text = description;

            if (!string.IsNullOrWhiteSpace(tunnelAddress))
                lbTunnelList.SelectTunnel(tunnelAddress);
            else
                lbTunnelList.SelectedIndex = -1;

            Enable();
        }
    }

    class TunnelEventArgs : EventArgs
    {
        public TunnelEventArgs(CnCNetTunnel tunnel)
        {
            Tunnel = tunnel;
        }

        public CnCNetTunnel Tunnel { get; }
    }
}
