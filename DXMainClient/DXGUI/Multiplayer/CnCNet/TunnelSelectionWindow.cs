using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A window for selecting a CnCNet tunnel server and tunnel mode.
    /// </summary>
    class TunnelSelectionWindow : XNAWindow
    {
        public TunnelSelectionWindow(WindowManager windowManager, TunnelHandler tunnelHandler) : base(windowManager)
        {
            this.tunnelHandler = tunnelHandler;
        }

        public event EventHandler<TunnelSelectedEventArgs> TunnelSelected;

        private readonly TunnelHandler tunnelHandler;
        private TunnelListBox lbTunnelList;
        private XNAPanel pnlTunnelListDisabledOverlay;
        private XNALabel lblDescription;
        private XNADropDown ddMode;
        private XNAClientButton btnApply;

        private CnCNetTunnel originalTunnel;
        private TunnelMode originalMode;

        public override void Initialize()
        {
            if (Initialized)
                return;

            Name = "TunnelSelectionWindow";

            BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = nameof(lblDescription);
            lblDescription.Text = "Line 1" + Environment.NewLine + "Line 2";
            lblDescription.X = UIDesignConstants.EMPTY_SPACE_SIDES + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            lblDescription.Y = UIDesignConstants.EMPTY_SPACE_TOP + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            AddChild(lblDescription);

            ddMode = new XNADropDown(WindowManager);
            ddMode.Name = nameof(ddMode);
            ddMode.X = lblDescription.X;
            ddMode.Y = lblDescription.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            ddMode.Width = 220;
            ddMode.Height = UIDesignConstants.BUTTON_HEIGHT;
            ddMode.AddItem(new XNADropDownItem { Text = "Dynamic (V3)".L10N("Client:Main:TunnelSelModeDynamic"), Tag = TunnelMode.V3Dynamic });
            ddMode.AddItem(new XNADropDownItem { Text = "Static (V3)".L10N("Client:Main:TunnelSelModeStatic"), Tag = TunnelMode.V3Static });
            ddMode.AddItem(new XNADropDownItem { Text = "Legacy (V2)".L10N("Client:Main:TunnelSelModeLegacy"), Tag = TunnelMode.V2Legacy });
            ddMode.SelectedIndexChanged += DdMode_SelectedIndexChanged;
            AddChild(ddMode);

            lbTunnelList = new TunnelListBox(WindowManager, tunnelHandler);
            lbTunnelList.Name = nameof(lbTunnelList);
            lbTunnelList.X = UIDesignConstants.EMPTY_SPACE_SIDES + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            lbTunnelList.Y = ddMode.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN;
            AddChild(lbTunnelList);
            lbTunnelList.SelectedIndexChanged += LbTunnelList_SelectedIndexChanged;

            pnlTunnelListDisabledOverlay = new XNAPanel(WindowManager);
            pnlTunnelListDisabledOverlay.Name = nameof(pnlTunnelListDisabledOverlay);
            pnlTunnelListDisabledOverlay.ClientRectangle = lbTunnelList.ClientRectangle;
            pnlTunnelListDisabledOverlay.DrawBorders = false;
            pnlTunnelListDisabledOverlay.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            AddChild(pnlTunnelListDisabledOverlay);

            btnApply = new XNAClientButton(WindowManager);
            btnApply.Name = nameof(btnApply);
            btnApply.Width = UIDesignConstants.BUTTON_WIDTH_92;
            btnApply.Height = UIDesignConstants.BUTTON_HEIGHT;
            btnApply.Text = "Apply".L10N("Client:Main:ButtonApply");
            btnApply.X = UIDesignConstants.EMPTY_SPACE_SIDES + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;
            btnApply.Y = lbTunnelList.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN * 3;
            AddChild(btnApply);
            btnApply.LeftClick += BtnApply_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Width = UIDesignConstants.BUTTON_WIDTH_92;
            btnCancel.Height = UIDesignConstants.BUTTON_HEIGHT;
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.Y = btnApply.Y;
            AddChild(btnCancel);
            btnCancel.LeftClick += BtnCancel_LeftClick;

            Width = lbTunnelList.Right + UIDesignConstants.CONTROL_HORIZONTAL_MARGIN + UIDesignConstants.EMPTY_SPACE_SIDES;
            Height = btnApply.Bottom + UIDesignConstants.CONTROL_VERTICAL_MARGIN + UIDesignConstants.EMPTY_SPACE_BOTTOM;
            btnCancel.X = Width - btnCancel.Width - UIDesignConstants.EMPTY_SPACE_SIDES - UIDesignConstants.CONTROL_HORIZONTAL_MARGIN;

            base.Initialize();
        }

        private TunnelMode GetSelectedMode() => (TunnelMode)(ddMode.SelectedItem?.Tag ?? TunnelMode.V3Dynamic);

        private void DdMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var mode = GetSelectedMode();
            bool isDynamic = mode == TunnelMode.V3Dynamic;

            lbTunnelList.Enabled = !isDynamic;
            pnlTunnelListDisabledOverlay.Visible = isDynamic;
            lbTunnelList.TargetVersion = mode == TunnelMode.V2Legacy ? 2 : 3;

            UpdateApplyButton();
        }

        private void UpdateApplyButton()
        {
            var mode = GetSelectedMode();
            if (mode == TunnelMode.V3Dynamic)
            {
                btnApply.AllowClick = originalMode != TunnelMode.V3Dynamic;
            }
            else
            {
                bool modeChanged = mode != originalMode;
                bool tunnelChanged = originalTunnel == null ||
                    !lbTunnelList.IsTunnelSelected(originalTunnel.Address, originalTunnel.Port);
                btnApply.AllowClick = lbTunnelList.IsValidIndexSelected() && (modeChanged || tunnelChanged);
            }
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            Disable();

            var mode = GetSelectedMode();
            CnCNetTunnel tunnel = (mode == TunnelMode.V3Dynamic) ? null : lbTunnelList.GetSelectedTunnel();

            if (mode != TunnelMode.V3Dynamic && tunnel == null)
                return;

            TunnelSelected?.Invoke(this, new TunnelSelectedEventArgs(tunnel, mode));
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e) => Disable();

        private void LbTunnelList_SelectedIndexChanged(object sender, EventArgs e) => UpdateApplyButton();

        /// <summary>
        /// Opens the window with the given description, pre-selecting the current tunnel and mode.
        /// </summary>
        public void Open(string description, CnCNetTunnel currentTunnel = null, TunnelMode currentMode = TunnelMode.V3Dynamic)
        {
            lblDescription.Text = description;
            originalTunnel = currentTunnel;
            originalMode = currentMode;

            ddMode.SelectedIndex = Math.Max(0, ddMode.Items.FindIndex(i => (TunnelMode)i.Tag == currentMode));

            DdMode_SelectedIndexChanged(this, EventArgs.Empty);

            if (GetSelectedMode() != TunnelMode.V3Dynamic && currentTunnel != null)
                lbTunnelList.SelectTunnel(currentTunnel.Address, currentTunnel.Port);
            else
                lbTunnelList.SelectedIndex = -1;

            if (lbTunnelList.SelectedIndex > -1)
            {
                lbTunnelList.SetTopIndex(0);

                int diff = lbTunnelList.SelectedIndex - lbTunnelList.LastIndex;
                if (diff > 0)
                    lbTunnelList.TopIndex = Math.Min(lbTunnelList.TopIndex + diff, lbTunnelList.ItemCount - 1);
            }

            btnApply.AllowClick = false;
            Enable();
        }
    }

}
