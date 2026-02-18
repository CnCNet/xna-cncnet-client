using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientGUI;
using ClientCore.Extensions;
using ClientCore.Enums;
using ClientGUI.Settings;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Generic.OptionPanels
{
    class GameOptionsPanel : XNAOptionsPanel
    {

        private const string TEXT_BACKGROUND_COLOR_TRANSPARENT = "0";
        private const string TEXT_BACKGROUND_COLOR_BLACK = "12";
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
        private XNAClientCheckBox chkAltToUndeploy;
        private XNAClientCheckBox chkBlackChatBackground;
        private XNAClientCheckBox chkShowHiddenObjects;

        private XNAControl topBar;

        private XNATextBox tbPlayerName;

        private HotkeyConfigurationWindow hotkeyConfigWindow;

        public override void Initialize()
        {
            base.Initialize();

            Name = "GameOptionsPanel";

            var lblScrollRate = new XNALabel(WindowManager);
            lblScrollRate.Name = nameof(lblScrollRate);
            lblScrollRate.ClientRectangle = new Rectangle(12,
                14, 0, 0);
            lblScrollRate.Text = "Scroll Rate:".L10N("Client:DTAConfig:ScrollRate");

            lblScrollRateValue = new XNALabel(WindowManager);
            lblScrollRateValue.Name = nameof(lblScrollRateValue);
            lblScrollRateValue.FontIndex = 1;
            lblScrollRateValue.Text = "0";
            lblScrollRateValue.ClientRectangle = new Rectangle(
                Width - lblScrollRateValue.Width - 12,
                lblScrollRate.Y, 0, 0);

            trbScrollRate = new XNATrackbar(WindowManager);
            trbScrollRate.Name = nameof(trbScrollRate);
            trbScrollRate.ClientRectangle = new Rectangle(
                lblScrollRate.Right + 32,
                lblScrollRate.Y - 2,
                lblScrollRateValue.X - lblScrollRate.Right - 47,
                22);
            trbScrollRate.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            trbScrollRate.MinValue = 0;
            trbScrollRate.MaxValue = MAX_SCROLL_RATE;
            trbScrollRate.ValueChanged += TrbScrollRate_ValueChanged;

            chkScrollCoasting = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ScrollMethod", true, "0", "1");
            chkScrollCoasting.Name = nameof(chkScrollCoasting);
            chkScrollCoasting.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                trbScrollRate.Bottom + 20, 0, 0);
            chkScrollCoasting.Text = "Scroll Coasting".L10N("Client:DTAConfig:ScrollCoasting");

            chkTargetLines = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "UnitActionLines");
            chkTargetLines.Name = nameof(chkTargetLines);
            chkTargetLines.ClientRectangle = new Rectangle(
                lblScrollRate.X,
                chkScrollCoasting.Bottom + 24, 0, 0);
            chkTargetLines.Text = "Target Lines".L10N("Client:DTAConfig:TargetLines");

            chkTooltips = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ToolTips");
            chkTooltips.Name = nameof(chkTooltips);
            chkTooltips.Text = "Tooltips".L10N("Client:DTAConfig:Tooltips");

            var lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = nameof(lblPlayerName);
            lblPlayerName.Text = "Player Name*:".L10N("Client:DTAConfig:PlayerName");

            if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
            {
                chkTooltips.ClientRectangle = new Rectangle(
                    lblScrollRate.X,
                    chkTargetLines.Bottom + 24, 0, 0);
            }
            else
            {
                chkShowHiddenObjects = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ShowHidden");
                chkShowHiddenObjects.Name = nameof(chkShowHiddenObjects);
                chkShowHiddenObjects.ClientRectangle = new Rectangle(
                    lblScrollRate.X,
                    chkTargetLines.Bottom + 24, 0, 0);
                chkShowHiddenObjects.Text = "Show Hidden Objects".L10N("Client:DTAConfig:YRShowHidden");

                chkTooltips.ClientRectangle = new Rectangle(
                    lblScrollRate.X,
                    chkShowHiddenObjects.Bottom + 24, 0, 0);

                lblPlayerName.ClientRectangle = new Rectangle(
                    lblScrollRate.X,
                    chkTooltips.Bottom + 30, 0, 0);

                AddChild(chkShowHiddenObjects);
            }

            if (ClientConfiguration.Instance.ClientGameType == ClientType.TS)
            {
                chkBlackChatBackground = new SettingCheckBox(WindowManager, false, UserINISettings.OPTIONS, "TextBackgroundColor", true, TEXT_BACKGROUND_COLOR_BLACK, TEXT_BACKGROUND_COLOR_TRANSPARENT);
                chkBlackChatBackground.Name = nameof(chkBlackChatBackground);
                chkBlackChatBackground.ClientRectangle = new Rectangle(
                    chkScrollCoasting.X,
                    chkTooltips.Bottom + 24, 0, 0);
                chkBlackChatBackground.Text = "Use black background for in-game chat messages".L10N("Client:DTAConfig:TSUseBlackBackgroundChat");

                AddChild(chkBlackChatBackground);

                chkAltToUndeploy = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "MoveToUndeploy");
                chkAltToUndeploy.Name = nameof(chkAltToUndeploy);
                chkAltToUndeploy.ClientRectangle = new Rectangle(
                    chkScrollCoasting.X,
                    chkBlackChatBackground.Bottom + 24, 0, 0);
                chkAltToUndeploy.Text = "Undeploy units by holding Alt key instead of a regular move command".L10N("Client:DTAConfig:TSUndeployAltKey");

                AddChild(chkAltToUndeploy);

                lblPlayerName.ClientRectangle = new Rectangle(
                    lblScrollRate.X,
                    chkAltToUndeploy.Bottom + 30, 0, 0);
            }

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = nameof(tbPlayerName);
            tbPlayerName.MaximumTextLength = ClientConfiguration.Instance.MaxNameLength;
            tbPlayerName.ClientRectangle = new Rectangle(trbScrollRate.X,
                lblPlayerName.Y - 2, 200, 19);
            tbPlayerName.Text = ProgramConstants.PLAYERNAME;

            var lblNotice = new XNALabel(WindowManager);
            lblNotice.Name = nameof(lblNotice);
            lblNotice.ClientRectangle = new Rectangle(lblPlayerName.X,
                lblPlayerName.Bottom + 30, 0, 0);
            lblNotice.Text = ("* If you are currently connected to CnCNet, you need to log out and reconnect\nfor your new name to be applied.").L10N("Client:DTAConfig:ReconnectAfterRename");

            hotkeyConfigWindow = new HotkeyConfigurationWindow(WindowManager);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, hotkeyConfigWindow);
            hotkeyConfigWindow.Disable();

            var btnConfigureHotkeys = new XNAClientButton(WindowManager);
            btnConfigureHotkeys.Name = nameof(btnConfigureHotkeys);
            btnConfigureHotkeys.ClientRectangle = new Rectangle(lblPlayerName.X, lblNotice.Bottom + 36, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnConfigureHotkeys.Text = "Configure Hotkeys".L10N("Client:DTAConfig:ConfigureHotkeys");
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
