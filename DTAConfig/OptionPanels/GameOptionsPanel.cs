using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using ClientCore.Extensions;
using DTAConfig.Settings;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAConfig.OptionPanels
{
    class GameOptionsPanel : XNAOptionsPanel
    {
        private const int PADDING_X = 12;
        private const int PADDING_Y = 14;
        private const int TRACKBAR_X_PADDING = 32;
        private const int TRACKBAR_Y_PADDING = 16;
        private const int TRACKBAR_Y_OFFSET = 2; //trackbars sit slightly higher than their labels.
        private const int TRACKBAR_HEIGHT = 22;
        private const int TEXTBOX_Y_OFFSET = 2;
        private const int TEXTBOX_HEIGHT = 19;
        private const int CHECKBOX_SPACING = 4;
        private const int GROUP_SPACING = 22;

#if TS
        private const string TEXT_BACKGROUND_COLOR_TRANSPARENT = "0";
        private const string TEXT_BACKGROUND_COLOR_BLACK = "12";
#endif
        private const int MAX_SCROLL_RATE = 6;

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
#if TS
        private XNAClientCheckBox chkAltToUndeploy;
        private XNAClientCheckBox chkBlackChatBackground;
#else
        private XNAClientCheckBox chkShowHiddenObjects;
        private XNAClientCheckBox chkDisableInGameChat;
#endif

        private XNAControl topBar;

        private XNATextBox tbPlayerName;

        private HotkeyConfigurationWindow hotkeyConfigWindow;

        public override void Initialize()
        {
            base.Initialize();

            Name = "GameOptionsPanel";

            var lblScrollRate = new XNALabel(WindowManager);
            lblScrollRate.Name = nameof(lblScrollRate);
            lblScrollRate.ClientRectangle = new Rectangle(PADDING_X,
                PADDING_Y, 0, 0);
            lblScrollRate.Text = "Scroll Rate:".L10N("Client:DTAConfig:ScrollRate");

            lblScrollRateValue = new XNALabel(WindowManager);
            lblScrollRateValue.Name = nameof(lblScrollRateValue);
            lblScrollRateValue.FontIndex = 1;
            lblScrollRateValue.Text = "0";
            lblScrollRateValue.ClientRectangle = new Rectangle(
                Width - lblScrollRateValue.Width - PADDING_X,
                lblScrollRate.Y, 0, 0);

            trbScrollRate = new XNATrackbar(WindowManager);
            trbScrollRate.Name = nameof(trbScrollRate);
            trbScrollRate.ClientRectangle = new Rectangle(
                lblScrollRate.Right + TRACKBAR_X_PADDING,
                lblScrollRate.Y - TRACKBAR_Y_OFFSET,
                lblScrollRateValue.X - lblScrollRate.Right - TRACKBAR_X_PADDING - PADDING_X,
                TRACKBAR_HEIGHT);
            trbScrollRate.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            trbScrollRate.MinValue = 0;
            trbScrollRate.MaxValue = MAX_SCROLL_RATE;
            trbScrollRate.ValueChanged += TrbScrollRate_ValueChanged;

            chkScrollCoasting = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ScrollMethod", true, "0", "1");
            chkScrollCoasting.Name = nameof(chkScrollCoasting);
            chkScrollCoasting.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                trbScrollRate.Bottom + TRACKBAR_Y_PADDING, 0, 0);
            chkScrollCoasting.Text = "Scroll Coasting".L10N("Client:DTAConfig:ScrollCoasting");
            AddChild(chkScrollCoasting);

            chkTargetLines = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "UnitActionLines");
            chkTargetLines.Name = nameof(chkTargetLines);
            chkTargetLines.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkScrollCoasting.Bottom + CHECKBOX_SPACING, 0, 0);
            chkTargetLines.Text = "Target Lines".L10N("Client:DTAConfig:TargetLines");
            AddChild(chkTargetLines);

            chkTooltips = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ToolTips");
            chkTooltips.Name = nameof(chkTooltips);
            chkTooltips.Text = "Tooltips".L10N("Client:DTAConfig:Tooltips");

            var lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = nameof(lblPlayerName);
            lblPlayerName.Text = "Player Name*:".L10N("Client:DTAConfig:PlayerName");

#if TS
            chkTooltips.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkTargetLines.Bottom + CHECKBOX_SPACING, 0, 0);
            AddChild(chkTooltips);
#else
            chkDisableInGameChat = new SettingCheckBox(WindowManager, false, UserINISettings.OPTIONS, "DisableChat");
            chkDisableInGameChat.Name = nameof(chkDisableInGameChat);
            chkDisableInGameChat.Text = "Disable in-game chat".L10N("Client:DTAConfig:DisableInGameChat");

            chkShowHiddenObjects = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ShowHidden");
            chkShowHiddenObjects.Name = nameof(chkShowHiddenObjects);
            chkShowHiddenObjects.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkTargetLines.Bottom + CHECKBOX_SPACING, 0, 0);
            chkShowHiddenObjects.Text = "Show Hidden Objects".L10N("Client:DTAConfig:YRShowHidden");
            AddChild(chkShowHiddenObjects);

            chkTooltips.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkShowHiddenObjects.Bottom + CHECKBOX_SPACING, 0, 0);
            AddChild(chkTooltips);

            chkDisableInGameChat.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkTooltips.Bottom + CHECKBOX_SPACING, 0, 0);
            AddChild(chkDisableInGameChat);

            lblPlayerName.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkDisableInGameChat.Bottom + GROUP_SPACING, 0, 0);

#endif

#if TS
            chkBlackChatBackground = new SettingCheckBox(WindowManager, false, UserINISettings.OPTIONS, "TextBackgroundColor", true, TEXT_BACKGROUND_COLOR_BLACK, TEXT_BACKGROUND_COLOR_TRANSPARENT);
            chkBlackChatBackground.Name = nameof(chkBlackChatBackground);
            chkBlackChatBackground.ClientRectangle = new Rectangle(
                chkScrollCoasting.X,
                chkTooltips.Bottom + CHECKBOX_SPACING, 0, 0);
            chkBlackChatBackground.Text = "Use black background for in-game chat messages".L10N("Client:DTAConfig:TSUseBlackBackgroundChat");

            AddChild(chkBlackChatBackground);

            chkAltToUndeploy = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "MoveToUndeploy");
            chkAltToUndeploy.Name = nameof(chkAltToUndeploy);
            chkAltToUndeploy.ClientRectangle = new Rectangle(
                chkScrollCoasting.X,
                chkBlackChatBackground.Bottom + CHECKBOX_SPACING, 0, 0);
            chkAltToUndeploy.Text = "Undeploy units by holding Alt key instead of a regular move command".L10N("Client:DTAConfig:TSUndeployAltKey");

            AddChild(chkAltToUndeploy);

            lblPlayerName.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkAltToUndeploy.Bottom + CHECKBOX_SPACING, 0, 0);
#endif

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = nameof(tbPlayerName);
            tbPlayerName.MaximumTextLength = ClientConfiguration.Instance.MaxNameLength;
            tbPlayerName.ClientRectangle = new Rectangle(trbScrollRate.X,
                lblPlayerName.Y - TEXTBOX_Y_OFFSET, 200, TEXTBOX_HEIGHT);
            tbPlayerName.Text = ProgramConstants.PLAYERNAME;

            var lblNotice = new XNALabel(WindowManager);
            lblNotice.Name = nameof(lblNotice);
            lblNotice.ClientRectangle = new Rectangle(lblPlayerName.X,
                tbPlayerName.Bottom + CHECKBOX_SPACING, 0, 0);
            lblNotice.Text = ("* If you are currently connected to CnCNet, you need to log out and reconnect\nfor your new name to be applied.").L10N("Client:DTAConfig:ReconnectAfterRename");

            hotkeyConfigWindow = new HotkeyConfigurationWindow(WindowManager);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, hotkeyConfigWindow);
            hotkeyConfigWindow.Disable();

            var btnConfigureHotkeys = new XNAClientButton(WindowManager);
            btnConfigureHotkeys.Name = nameof(btnConfigureHotkeys);
            btnConfigureHotkeys.ClientRectangle = new Rectangle(lblPlayerName.X, lblNotice.Bottom + GROUP_SPACING, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnConfigureHotkeys.Text = "Configure Hotkeys".L10N("Client:DTAConfig:ConfigureHotkeys");
            btnConfigureHotkeys.LeftClick += BtnConfigureHotkeys_LeftClick;

            AddChild(lblScrollRate);
            AddChild(lblScrollRateValue);
            AddChild(trbScrollRate);
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
            base.Load();

            int scrollRate = ReverseScrollRate(IniSettings.ScrollRate);

            if (scrollRate >= trbScrollRate.MinValue && scrollRate <= trbScrollRate.MaxValue)
            {
                trbScrollRate.Value = scrollRate;
                lblScrollRateValue.Text = scrollRate.ToString();
            }

            tbPlayerName.Text = UserINISettings.Instance.PlayerName;
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.ScrollRate.Value = ReverseScrollRate(trbScrollRate.Value);

            string playerName = NameValidator.GetValidOfflineName(tbPlayerName.Text);

            if (playerName.Length > 0)
                IniSettings.PlayerName.Value = playerName;

            return restartRequired;
        }

        private int ReverseScrollRate(int scrollRate)
        {
            return Math.Abs(scrollRate - MAX_SCROLL_RATE);
        }
    }
}
