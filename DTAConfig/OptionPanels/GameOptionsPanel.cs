using System;

using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Extensions;
using ClientCore.Settings;

using ClientGUI;

using DTAConfig.Settings;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels;

internal class GameOptionsPanel : XNAOptionsPanel
{

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
#endif

    private readonly XNAControl topBar;

    private XNATextBox tbPlayerName;

    private HotkeyConfigurationWindow hotkeyConfigWindow;

    public override void Initialize()
    {
        base.Initialize();

        Name = "GameOptionsPanel";

        XNALabel lblScrollRate = new(WindowManager)
        {
            Name = "lblScrollRate",
            ClientRectangle = new Rectangle(12,
            14, 0, 0),
            Text = "Scroll Rate:".L10N("Client:DTAConfig:ScrollRate")
        };

        lblScrollRateValue = new XNALabel(WindowManager)
        {
            Name = "lblScrollRateValue",
            FontIndex = 1,
            Text = "0"
        };
        lblScrollRateValue.ClientRectangle = new Rectangle(
            Width - lblScrollRateValue.Width - 12,
            lblScrollRate.Y, 0, 0);

        trbScrollRate = new XNATrackbar(WindowManager)
        {
            Name = "trbClientVolume",
            ClientRectangle = new Rectangle(
            lblScrollRate.Right + 32,
            lblScrollRate.Y - 2,
            lblScrollRateValue.X - lblScrollRate.Right - 47,
            22),
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2),
            MinValue = 0,
            MaxValue = MAX_SCROLL_RATE
        };
        trbScrollRate.ValueChanged += TrbScrollRate_ValueChanged;

        chkScrollCoasting = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ScrollMethod", true, "0", "1")
        {
            Name = "chkScrollCoasting",
            ClientRectangle = new Rectangle(
            lblScrollRate.X,
            trbScrollRate.Bottom + 20, 0, 0),
            Text = "Scroll Coasting".L10N("Client:DTAConfig:ScrollCoasting")
        };

        chkTargetLines = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "UnitActionLines")
        {
            Name = "chkTargetLines",
            ClientRectangle = new Rectangle(
            lblScrollRate.X,
            chkScrollCoasting.Bottom + 24, 0, 0),
            Text = "Target Lines".L10N("Client:DTAConfig:TargetLines")
        };

        chkTooltips = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ToolTips")
        {
            Name = "chkTooltips",
            Text = "Tooltips".L10N("Client:DTAConfig:Tooltips")
        };

        XNALabel lblPlayerName = new(WindowManager)
        {
            Name = "lblPlayerName",
            Text = "Player Name*:".L10N("Client:DTAConfig:PlayerName")
        };

#if TS
        chkTooltips.ClientRectangle = new Rectangle(
            lblScrollRate.X,
            chkTargetLines.Bottom + 24, 0, 0);
#else
        chkShowHiddenObjects = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "ShowHidden")
        {
            Name = "chkShowHiddenObjects",
            ClientRectangle = new Rectangle(
            lblScrollRate.X,
            chkTargetLines.Bottom + 24, 0, 0),
            Text = "Show Hidden Objects".L10N("Client:DTAConfig:YRShowHidden")
        };

        chkTooltips.ClientRectangle = new Rectangle(
            lblScrollRate.X,
            chkShowHiddenObjects.Bottom + 24, 0, 0);

        lblPlayerName.ClientRectangle = new Rectangle(
            lblScrollRate.X,
            chkTooltips.Bottom + 30, 0, 0);

        AddChild(chkShowHiddenObjects);
#endif

#if TS
        chkBlackChatBackground = new SettingCheckBox(WindowManager, false, UserINISettings.OPTIONS, "TextBackgroundColor", true, TEXT_BACKGROUND_COLOR_BLACK, TEXT_BACKGROUND_COLOR_TRANSPARENT);
        chkBlackChatBackground.Name = "chkBlackChatBackground";
        chkBlackChatBackground.ClientRectangle = new Rectangle(
            chkScrollCoasting.X,
            chkTooltips.Bottom + 24, 0, 0);
        chkBlackChatBackground.Text = "Use black background for in-game chat messages".L10N("Client:DTAConfig:TSUseBlackBackgroundChat");

        AddChild(chkBlackChatBackground);

        chkAltToUndeploy = new SettingCheckBox(WindowManager, true, UserINISettings.OPTIONS, "MoveToUndeploy");
        chkAltToUndeploy.Name = "chkAltToUndeploy";
        chkAltToUndeploy.ClientRectangle = new Rectangle(
            chkScrollCoasting.X,
            chkBlackChatBackground.Bottom + 24, 0, 0);
        chkAltToUndeploy.Text = "Undeploy units by holding Alt key instead of a regular move command".L10N("Client:DTAConfig:TSUndeployAltKey");

        AddChild(chkAltToUndeploy);

        lblPlayerName.ClientRectangle = new Rectangle(
            lblScrollRate.X,
            chkAltToUndeploy.Bottom + 30, 0, 0);
#endif

        tbPlayerName = new XNATextBox(WindowManager)
        {
            Name = "tbPlayerName",
            MaximumTextLength = ClientConfiguration.Instance.MaxNameLength,
            ClientRectangle = new Rectangle(trbScrollRate.X,
            lblPlayerName.Y - 2, 200, 19),
            Text = ProgramConstants.PLAYERNAME
        };

        XNALabel lblNotice = new(WindowManager)
        {
            Name = "lblNotice",
            ClientRectangle = new Rectangle(lblPlayerName.X,
            lblPlayerName.Bottom + 30, 0, 0),
            Text = "* If you are currently connected to CnCNet, you need to log out and reconnect\nfor your new name to be applied.".L10N("Client:DTAConfig:ReconnectAfterRename")
        };

        hotkeyConfigWindow = new HotkeyConfigurationWindow(WindowManager);
        DarkeningPanel.AddAndInitializeWithControl(WindowManager, hotkeyConfigWindow);
        hotkeyConfigWindow.Disable();

        XNAClientButton btnConfigureHotkeys = new(WindowManager)
        {
            Name = "btnConfigureHotkeys",
            ClientRectangle = new Rectangle(lblPlayerName.X, lblNotice.Bottom + 36, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Configure Hotkeys".L10N("Client:DTAConfig:ConfigureHotkeys")
        };
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
        {
            IniSettings.PlayerName.Value = playerName;
        }

        return restartRequired;
    }

    private int ReverseScrollRate(int scrollRate)
    {
        return Math.Abs(scrollRate - MAX_SCROLL_RATE);
    }
}