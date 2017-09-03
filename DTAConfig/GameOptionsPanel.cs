using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAConfig
{
    class GameOptionsPanel : XNAOptionsPanel
    {
        private const int TEXT_BACKGROUND_COLOR_TRANSPARENT = 0;
        private const int TEXT_BACKGROUND_COLOR_BLACK = 12;

        public GameOptionsPanel(WindowManager windowManager, UserINISettings iniSettings, XNAControl topBar)
            : base(windowManager, iniSettings)
        {
            this.topBar = topBar;
        }

        private XNALabel lblScrollRateValue;

        private XNATrackbar trbScrollRate;
        private XNAClientCheckBox chkTargetLines;
        private XNAClientCheckBox chkScrollCoasting;
        private XNAClientCheckBox chkTooltips;
#if YR
        private XNAClientCheckBox chkShowHiddenObjects;
#else
        private XNAClientCheckBox chkAltToUndeploy;
        private XNAClientCheckBox chkBlackChatBackground;
#endif
        private XNAControl topBar;

        private XNATextBox tbPlayerName;

        private HotkeyConfigurationWindow hotkeyConfigWindow;

        public override void Initialize()
        {
            base.Initialize();

            Name = "GameOptionsPanel";

            var lblScrollRate = new XNALabel(WindowManager);
            lblScrollRate.Name = "lblScrollRate";
            lblScrollRate.ClientRectangle = new Rectangle(12,
                14, 0, 0);
            lblScrollRate.Text = "Scroll Rate:";

            lblScrollRateValue = new XNALabel(WindowManager);
            lblScrollRateValue.Name = "lblScrollRateValue";
            lblScrollRateValue.FontIndex = 1;
            lblScrollRateValue.Text = "3";
            lblScrollRateValue.ClientRectangle = new Rectangle(
                ClientRectangle.Width - lblScrollRateValue.ClientRectangle.Width - 12,
                lblScrollRate.ClientRectangle.Y, 0, 0);

            trbScrollRate = new XNATrackbar(WindowManager);
            trbScrollRate.Name = "trbClientVolume";
            trbScrollRate.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.Right + 32,
                lblScrollRate.ClientRectangle.Y - 2,
                lblScrollRateValue.ClientRectangle.X - lblScrollRate.ClientRectangle.Right - 47,
                22);
            trbScrollRate.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            trbScrollRate.MinValue = 0;
            trbScrollRate.MaxValue = 6;
            trbScrollRate.ValueChanged += TrbScrollRate_ValueChanged;

            chkScrollCoasting = new XNAClientCheckBox(WindowManager);
            chkScrollCoasting.Name = "chkScrollCoasting";
            chkScrollCoasting.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                trbScrollRate.ClientRectangle.Bottom + 20, 0, 0);
            chkScrollCoasting.Text = "Scroll Coasting";

            chkTargetLines = new XNAClientCheckBox(WindowManager);
            chkTargetLines.Name = "chkTargetLines";
            chkTargetLines.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkScrollCoasting.ClientRectangle.Bottom + 24, 0, 0);
            chkTargetLines.Text = "Target Lines";

            chkTooltips = new XNAClientCheckBox(WindowManager);
            chkTooltips.Name = "chkTooltips";
            chkTooltips.Text = "Tooltips";

            var lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = "lblPlayerName";
            lblPlayerName.Text = "Player Name*:";

#if YR
            chkShowHiddenObjects = new XNAClientCheckBox(WindowManager);
            chkShowHiddenObjects.Name = "chkShowHiddenObjects";
            chkShowHiddenObjects.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkTargetLines.ClientRectangle.Bottom + 24, 0, 0);
            chkShowHiddenObjects.Text = "Show Hidden Objects";

            chkTooltips.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkShowHiddenObjects.ClientRectangle.Bottom + 24, 0, 0);

            lblPlayerName.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkTooltips.ClientRectangle.Bottom + 30, 0, 0);

            AddChild(chkShowHiddenObjects);
#else
            chkTooltips.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkTargetLines.ClientRectangle.Bottom + 24, 0, 0);

            chkAltToUndeploy = new XNAClientCheckBox(WindowManager);
            chkAltToUndeploy.Name = "chkAltToUndeploy";
            chkAltToUndeploy.ClientRectangle = new Rectangle(
                chkScrollCoasting.ClientRectangle.X,
                chkTooltips.ClientRectangle.Bottom + 24, 0, 0);
            chkAltToUndeploy.Text = "Undeploy units by holding Alt key instead of a regular move command";

            chkBlackChatBackground = new XNAClientCheckBox(WindowManager);
            chkBlackChatBackground.Name = "chkBlackChatBackground";
            chkBlackChatBackground.ClientRectangle = new Rectangle(
                chkScrollCoasting.ClientRectangle.X,
                chkAltToUndeploy.ClientRectangle.Bottom + 24, 0, 0);
            chkBlackChatBackground.Text = "Use black background for in-game chat messages";

            lblPlayerName.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkBlackChatBackground.ClientRectangle.Bottom + 30, 0, 0);

            AddChild(chkAltToUndeploy);
            AddChild(chkBlackChatBackground);
#endif 

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = "tbPlayerName";
            tbPlayerName.MaximumTextLength = ClientConfiguration.Instance.MaxNameLength;
            tbPlayerName.ClientRectangle = new Rectangle(trbScrollRate.ClientRectangle.X,
                lblPlayerName.ClientRectangle.Y - 2, 200, 19);
            tbPlayerName.Text = ProgramConstants.PLAYERNAME;

            var lblNotice = new XNALabel(WindowManager);
            lblNotice.Name = "lblNotice";
            lblNotice.ClientRectangle = new Rectangle(lblPlayerName.ClientRectangle.X,
                lblPlayerName.ClientRectangle.Bottom + 30, 0, 0);
            lblNotice.Text = "* If you are currently connected to CnCNet, you need to log out and reconnect" +
                Environment.NewLine + "for your new name to be applied.";

            hotkeyConfigWindow = new HotkeyConfigurationWindow(WindowManager);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, hotkeyConfigWindow);
            hotkeyConfigWindow.Disable();

            var btnConfigureHotkeys = new XNAClientButton(WindowManager);
            btnConfigureHotkeys.Name = "btnConfigureHotkeys";
            btnConfigureHotkeys.ClientRectangle = new Rectangle(lblPlayerName.X, lblNotice.Bottom + 36, 160, 23);
            btnConfigureHotkeys.Text = "Configure Hotkeys";
            btnConfigureHotkeys.LeftClick += BtnConfigureHotkeys_LeftClick;

            AddChild(lblScrollRate);
            AddChild(lblScrollRateValue);
            AddChild(trbScrollRate);
            AddChild(chkScrollCoasting);
            AddChild(chkTargetLines);
            AddChild(chkTooltips);
            AddChild(lblPlayerName);
            AddChild(tbPlayerName);
            AddChild(lblNotice);
            AddChild(btnConfigureHotkeys);
        }

        private void BtnConfigureHotkeys_LeftClick(object sender, EventArgs e)
        {
            hotkeyConfigWindow.Enable();

            if (topBar.Enabled)
            {
                topBar.Disable();
                hotkeyConfigWindow.EnabledChanged += HotkeyConfigWindow_EnabledChanged;
            }
        }

        private void HotkeyConfigWindow_EnabledChanged(object sender, EventArgs e)
        {
            hotkeyConfigWindow.EnabledChanged -= HotkeyConfigWindow_EnabledChanged;
            topBar.Enable();
        }

        private void TrbScrollRate_ValueChanged(object sender, EventArgs e)
        {
            lblScrollRateValue.Text = trbScrollRate.Value.ToString();
        }

        public override void Load()
        {
            int scrollRate = ReverseScrollRate(IniSettings.ScrollRate);

            if (scrollRate >= trbScrollRate.MinValue && scrollRate <= trbScrollRate.MaxValue)
            {
                trbScrollRate.Value = scrollRate;
                lblScrollRateValue.Text = scrollRate.ToString();
            }

            chkScrollCoasting.Checked = !Convert.ToBoolean(IniSettings.ScrollCoasting);
            chkTargetLines.Checked = IniSettings.TargetLines;
            chkTooltips.Checked = IniSettings.Tooltips;
#if YR
            chkShowHiddenObjects.Checked = IniSettings.ShowHiddenObjects;
#else
            chkAltToUndeploy.Checked = !IniSettings.MoveToUndeploy;
            chkBlackChatBackground.Checked = IniSettings.TextBackgroundColor == TEXT_BACKGROUND_COLOR_BLACK;
#endif
            tbPlayerName.Text = UserINISettings.Instance.PlayerName;
        }

        public override bool Save()
        {
            IniSettings.ScrollRate.Value = ReverseScrollRate(trbScrollRate.Value);

            IniSettings.ScrollCoasting.Value = Convert.ToInt32(!chkScrollCoasting.Checked);
            IniSettings.TargetLines.Value = chkTargetLines.Checked;
            IniSettings.Tooltips.Value = chkTooltips.Checked;
#if YR
            IniSettings.ShowHiddenObjects.Value = chkShowHiddenObjects.Checked;
#else
            IniSettings.MoveToUndeploy.Value = !chkAltToUndeploy.Checked;

            if (chkBlackChatBackground.Checked)
                IniSettings.TextBackgroundColor.Value = TEXT_BACKGROUND_COLOR_BLACK;
            else
                IniSettings.TextBackgroundColor.Value = TEXT_BACKGROUND_COLOR_TRANSPARENT;
#endif

            string playerName = tbPlayerName.Text;
            playerName = playerName.Replace(",", string.Empty);
            playerName = Renderer.GetSafeString(playerName, 0);
            playerName.Trim();

            if (playerName.Length > 0)
                IniSettings.PlayerName.Value = tbPlayerName.Text;

            return false;
        }

        private int ReverseScrollRate(int scrollRate)
        {
            return Math.Abs(scrollRate - 6);
        }
    }
}
