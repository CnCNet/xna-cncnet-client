using System;
using System.Collections.Generic;
using System.Threading;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.Online;
using DTAClient.Online.EventArguments;

using DTAConfig;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic;

/// <summary>
/// A top bar that allows switching between various client windows.
/// </summary>
public class TopBar : XNAPanel
{
    /// <summary>
    /// The number of seconds that the top bar will stay down after it has
    /// lost input focus.
    /// </summary>
    private const double DOWN_TIME_WAIT_SECONDS = 1.0;
    private const double EVENT_DOWN_TIME_WAIT_SECONDS = 2.0;
    private const double STARTUP_DOWN_TIME_WAIT_SECONDS = 3.5;
    private const double DOWN_MOVEMENT_RATE = 1.7;
    private const double UP_MOVEMENT_RATE = 1.7;
    private const int APPEAR_CURSOR_THRESHOLD_Y = 8;

    private readonly string DEFAULT_PM_BTN_LABEL = "Private Messages (F4)".L10N("Client:Main:PMButtonF4");

    public TopBar(
        WindowManager windowManager,
        CnCNetManager connectionManager,
        PrivateMessageHandler privateMessageHandler
    ) : base(windowManager)
    {
        downTimeWaitTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS);
        this.connectionManager = connectionManager;
        this.privateMessageHandler = privateMessageHandler;
    }

    public SwitchType LastSwitchType { get; private set; }

    private readonly List<ISwitchable> primarySwitches = [];
    private ISwitchable cncnetLobbySwitch;
    private ISwitchable privateMessageSwitch;

    private OptionsWindow optionsWindow;

    private XNAClientButton btnMainButton;
    private XNAClientButton btnCnCNetLobby;
    private XNAClientButton btnPrivateMessages;
    private XNAClientButton btnOptions;
    private XNAClientButton btnLogout;
    private XNALabel lblTime;
    private XNALabel lblDate;
    private XNALabel lblCnCNetStatus;
    private XNALabel lblCnCNetPlayerCount;
    private XNALabel lblConnectionStatus;

    private readonly CnCNetManager connectionManager;
    private readonly PrivateMessageHandler privateMessageHandler;

    private CancellationTokenSource cncnetPlayerCountCancellationSource;
    private static readonly object locker = new();

    private TimeSpan downTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS - STARTUP_DOWN_TIME_WAIT_SECONDS);

    private readonly TimeSpan downTimeWaitTime;

    private bool isDown = true;

    private double locationY = -40.0;

    private bool lanMode;

    public EventHandler LogoutEvent;

    public void AddPrimarySwitchable(ISwitchable switchable)
    {
        primarySwitches.Add(switchable);
        btnMainButton.Text = switchable.GetSwitchName() + " (F2)";
    }

    public void RemovePrimarySwitchable(ISwitchable switchable)
    {
        _ = primarySwitches.Remove(switchable);
        btnMainButton.Text = primarySwitches[^1].GetSwitchName() + " (F2)";
    }

    public void SetSecondarySwitch(ISwitchable switchable)
    {
        cncnetLobbySwitch = switchable;
    }

    public void SetTertiarySwitch(ISwitchable switchable)
    {
        privateMessageSwitch = switchable;
    }

    public void SetOptionsWindow(OptionsWindow optionsWindow)
    {
        this.optionsWindow = optionsWindow;
        optionsWindow.EnabledChanged += OptionsWindow_EnabledChanged;
    }

    private void OptionsWindow_EnabledChanged(object sender, EventArgs e)
    {
        if (!lanMode)
        {
            SetSwitchButtonsClickable(!optionsWindow.Enabled);
        }

        SetOptionsButtonClickable(!optionsWindow.Enabled);

        optionsWindow?.ToggleMainMenuOnlyOptions(primarySwitches.Count == 1 && !lanMode);
    }

    public void Clean()
    {
        cncnetPlayerCountCancellationSource?.Cancel();
    }

    public override void Initialize()
    {
        Name = "TopBar";
        ClientRectangle = new Rectangle(0, -39, WindowManager.RenderResolutionX, 39);
        PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        BackgroundTexture = AssetLoader.CreateTexture(Color.Black, 1, 1);
        DrawBorders = false;

        btnMainButton = new XNAClientButton(WindowManager)
        {
            Name = "btnMainButton",
            ClientRectangle = new Rectangle(12, 9, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Main Menu (F2)".L10N("Client:Main:MainMenuF2")
        };
        btnMainButton.LeftClick += BtnMainButton_LeftClick;

        btnCnCNetLobby = new XNAClientButton(WindowManager)
        {
            Name = "btnCnCNetLobby",
            ClientRectangle = new Rectangle(184, 9, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT),
            Text = "CnCNet Lobby (F3)".L10N("Client:Main:LobbyF3")
        };
        btnCnCNetLobby.LeftClick += BtnCnCNetLobby_LeftClick;

        btnPrivateMessages = new XNAClientButton(WindowManager)
        {
            Name = "btnPrivateMessages",
            ClientRectangle = new Rectangle(356, 9, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT),
            Text = DEFAULT_PM_BTN_LABEL
        };
        btnPrivateMessages.LeftClick += BtnPrivateMessages_LeftClick;

        lblDate = new XNALabel(WindowManager)
        {
            Name = "lblDate",
            FontIndex = 1
        };
        lblDate.Text = Renderer.GetSafeString(DateTime.Now.ToShortDateString(), lblDate.FontIndex);
        lblDate.ClientRectangle = new Rectangle(Width -
            (int)Renderer.GetTextDimensions(lblDate.Text, lblDate.FontIndex).X - 12, 18,
            lblDate.Width, lblDate.Height);

        lblTime = new XNALabel(WindowManager)
        {
            Name = "lblTime",
            FontIndex = 1
        };
        lblTime.Text = Renderer.GetSafeString(new DateTime(1, 1, 1, 23, 59, 59).ToLongTimeString(), lblTime.FontIndex);
        lblTime.ClientRectangle = new Rectangle(Width -
            (int)Renderer.GetTextDimensions(lblTime.Text, lblTime.FontIndex).X - 12, 4,
            lblTime.Width, lblTime.Height);

        btnLogout = new XNAClientButton(WindowManager)
        {
            Name = "btnLogout",
            ClientRectangle = new Rectangle(lblDate.X - 87, 9, 75, 23),
            FontIndex = 1,
            Text = "Log Out".L10N("Client:Main:TopBarLogOut"),
            AllowClick = false
        };
        btnLogout.LeftClick += BtnLogout_LeftClick;

        btnOptions = new XNAClientButton(WindowManager)
        {
            Name = "btnOptions",
            ClientRectangle = new Rectangle(btnLogout.X - 122, 9, 110, 23),
            Text = "Options (F12)".L10N("Client:Main:OptionsF12")
        };
        btnOptions.LeftClick += BtnOptions_LeftClick;

        lblConnectionStatus = new XNALabel(WindowManager)
        {
            Name = "lblConnectionStatus",
            FontIndex = 1,
            Text = "OFFLINE".L10N("Client:Main:StatusOffline")
        };

        AddChild(btnMainButton);
        AddChild(btnCnCNetLobby);
        AddChild(btnPrivateMessages);
        AddChild(btnOptions);
        AddChild(lblTime);
        AddChild(lblDate);
        AddChild(btnLogout);
        AddChild(lblConnectionStatus);

        if (ClientConfiguration.Instance.DisplayPlayerCountInTopBar)
        {
            lblCnCNetStatus = new XNALabel(WindowManager)
            {
                Name = "lblCnCNetStatus",
                FontIndex = 1,
                Text = ClientConfiguration.Instance.LocalGame.ToUpper() + " " + "PLAYERS ONLINE:".L10N("Client:Main:OnlinePlayersNumber")
            };
            lblCnCNetPlayerCount = new XNALabel(WindowManager)
            {
                Name = "lblCnCNetPlayerCount",
                FontIndex = 1,
                Text = "-"
            };
            lblCnCNetPlayerCount.ClientRectangle = new Rectangle(btnOptions.X - 50, 11, lblCnCNetPlayerCount.Width, lblCnCNetPlayerCount.Height);
            lblCnCNetStatus.ClientRectangle = new Rectangle(lblCnCNetPlayerCount.X - lblCnCNetStatus.Width - 6, 11, lblCnCNetStatus.Width, lblCnCNetStatus.Height);
            AddChild(lblCnCNetStatus);
            AddChild(lblCnCNetPlayerCount);
            CnCNetPlayerCountTask.CnCNetGameCountUpdated += CnCNetInfoController_CnCNetGameCountUpdated;
            cncnetPlayerCountCancellationSource = new CancellationTokenSource();
            CnCNetPlayerCountTask.InitializeService(cncnetPlayerCountCancellationSource);
        }

        lblConnectionStatus.CenterOnParent();

        base.Initialize();

        Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
        connectionManager.Connected += ConnectionManager_Connected;
        connectionManager.Disconnected += ConnectionManager_Disconnected;
        connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
        connectionManager.WelcomeMessageReceived += ConnectionManager_WelcomeMessageReceived;
        connectionManager.AttemptedServerChanged += ConnectionManager_AttemptedServerChanged;
        connectionManager.ConnectAttemptFailed += ConnectionManager_ConnectAttemptFailed;

        privateMessageHandler.UnreadMessageCountUpdated += PrivateMessageHandler_UnreadMessageCountUpdated;
    }

    private void PrivateMessageHandler_UnreadMessageCountUpdated(object sender, UnreadMessageCountEventArgs args)
    {
        UpdatePrivateMessagesBtnLabel(args.UnreadMessageCount);
    }

    private void UpdatePrivateMessagesBtnLabel(int unreadMessageCount)
    {
        btnPrivateMessages.Text = DEFAULT_PM_BTN_LABEL;
        if (unreadMessageCount > 0)
        {
            // TODO need to make a wider button to accommodate count
            // btnPrivateMessages.Text += $" ({unreadMessageCount})";
        }
    }

    private void CnCNetInfoController_CnCNetGameCountUpdated(object sender, PlayerCountEventArgs e)
    {
        lock (locker)
        {
            lblCnCNetPlayerCount.Text = e.PlayerCount == -1 ? "N/A".L10N("Client:Main:N/A") : e.PlayerCount.ToString();
        }
    }

    private void ConnectionManager_ConnectionLost(object sender, Online.EventArguments.ConnectionLostEventArgs e)
    {
        if (!lanMode)
        {
            ConnectionEvent("OFFLINE".L10N("Client:Main:StatusOffline"));
        }
    }

    private void ConnectionManager_ConnectAttemptFailed(object sender, EventArgs e)
    {
        if (!lanMode)
        {
            ConnectionEvent("OFFLINE".L10N("Client:Main:StatusOffline"));
        }
    }

    private void ConnectionManager_AttemptedServerChanged(object sender, Online.EventArguments.AttemptedServerEventArgs e)
    {
        ConnectionEvent("CONNECTING...".L10N("Client:Main:StatusConnecting"));
        BringDown();
    }

    private void ConnectionManager_WelcomeMessageReceived(object sender, Online.EventArguments.ServerMessageEventArgs e)
    {
        ConnectionEvent("CONNECTED".L10N("Client:Main:StatusConnected"));
    }

    private void ConnectionManager_Disconnected(object sender, EventArgs e)
    {
        btnLogout.AllowClick = false;
        if (!lanMode)
        {
            ConnectionEvent("OFFLINE".L10N("Client:Main:StatusOffline"));
        }
    }

    private void ConnectionEvent(string text)
    {
        lblConnectionStatus.Text = text;
        lblConnectionStatus.CenterOnParent();
        isDown = true;
        downTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS - EVENT_DOWN_TIME_WAIT_SECONDS);
    }

    private void BtnLogout_LeftClick(object sender, EventArgs e)
    {
        connectionManager.Disconnect();
        LogoutEvent?.Invoke(this, null);
        SwitchToPrimary();
    }

    private void ConnectionManager_Connected(object sender, EventArgs e)
    {
        btnLogout.AllowClick = true;
    }

    public void SwitchToPrimary()
    {
        BtnMainButton_LeftClick(this, EventArgs.Empty);
    }

    public ISwitchable GetTopMostPrimarySwitchable()
    {
        return primarySwitches[^1];
    }

    public void SwitchToSecondary()
    {
        BtnCnCNetLobby_LeftClick(this, EventArgs.Empty);
    }

    private void BtnCnCNetLobby_LeftClick(object sender, EventArgs e)
    {
        LastSwitchType = SwitchType.SECONDARY;
        primarySwitches[^1].SwitchOff();
        cncnetLobbySwitch.SwitchOn();
        privateMessageSwitch.SwitchOff();

        // HACK warning
        // TODO: add a way for DarkeningPanel to skip transitions
        ((DarkeningPanel)((XNAControl)cncnetLobbySwitch).Parent).Alpha = 1.0f;
    }

    private void BtnMainButton_LeftClick(object sender, EventArgs e)
    {
        LastSwitchType = SwitchType.PRIMARY;
        cncnetLobbySwitch.SwitchOff();
        privateMessageSwitch.SwitchOff();
        primarySwitches[^1].SwitchOn();

        // HACK warning
        // TODO: add a way for DarkeningPanel to skip transitions
        if (((XNAControl)primarySwitches[^1]).Parent is DarkeningPanel darkeningPanel)
        {
            darkeningPanel.Alpha = 1.0f;
        }
    }

    private void BtnPrivateMessages_LeftClick(object sender, EventArgs e)
    {
        privateMessageSwitch.SwitchOn();
    }

    private void BtnOptions_LeftClick(object sender, EventArgs e)
    {
        privateMessageSwitch.SwitchOff();
        optionsWindow.Open();
    }

    private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
    {
        if (!Enabled || !WindowManager.HasFocus || ProgramConstants.IsInGame)
        {
            return;
        }

        switch (e.PressedKey)
        {
            case Keys.F1:
                BringDown();
                break;
            case Keys.F2 when btnMainButton.AllowClick:
                BtnMainButton_LeftClick(this, EventArgs.Empty);
                break;
            case Keys.F3 when btnCnCNetLobby.AllowClick:
                BtnCnCNetLobby_LeftClick(this, EventArgs.Empty);
                break;
            case Keys.F4 when btnPrivateMessages.AllowClick:
                BtnPrivateMessages_LeftClick(this, EventArgs.Empty);
                break;
            case Keys.F12 when btnOptions.AllowClick:
                BtnOptions_LeftClick(this, EventArgs.Empty);
                break;
        }
    }

    public override void OnMouseOnControl()
    {
        if (Cursor.Location.Y > -1 && !ProgramConstants.IsInGame)
        {
            BringDown();
        }

        base.OnMouseOnControl();
    }

    private void BringDown()
    {
        isDown = true;
        downTime = TimeSpan.Zero;
    }

    public void SetMainButtonText(string text)
    {
        btnMainButton.Text = text;
    }

    public void SetSwitchButtonsClickable(bool allowClick)
    {
        if (btnMainButton != null)
        {
            btnMainButton.AllowClick = allowClick;
        }

        if (btnCnCNetLobby != null)
        {
            btnCnCNetLobby.AllowClick = allowClick;
        }

        if (btnPrivateMessages != null)
        {
            btnPrivateMessages.AllowClick = allowClick;
        }
    }

    public void SetOptionsButtonClickable(bool allowClick)
    {
        if (btnOptions != null)
        {
            btnOptions.AllowClick = allowClick;
        }
    }

    public void SetLanMode(bool lanMode)
    {
        this.lanMode = lanMode;
        SetSwitchButtonsClickable(!lanMode);
        if (lanMode)
        {
            ConnectionEvent("LAN MODE".L10N("Client:Main:StatusLanMode"));
        }
        else
        {
            ConnectionEvent("OFFLINE".L10N("Client:Main:StatusOffline"));
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (Cursor.Location.Y < APPEAR_CURSOR_THRESHOLD_Y && Cursor.Location.Y > -1 && !ProgramConstants.IsInGame)
        {
            BringDown();
        }

        if (isDown)
        {
            if (locationY < 0)
            {
                locationY += DOWN_MOVEMENT_RATE * (gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);
                ClientRectangle = new Rectangle(X, (int)locationY,
                    Width, Height);
            }

            downTime += gameTime.ElapsedGameTime;

            isDown = downTime < downTimeWaitTime;
        }
        else
        {
            if (locationY > -Height - 1)
            {
                locationY -= UP_MOVEMENT_RATE * (gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);
                ClientRectangle = new Rectangle(X, (int)locationY,
                    Width, Height);
            }
            else
            {
                return; // Don't handle input when the cursor is above our game window
            }
        }

        DateTime dtn = DateTime.Now;

        lblTime.Text = Renderer.GetSafeString(dtn.ToLongTimeString(), lblTime.FontIndex);
        string dateText = Renderer.GetSafeString(dtn.ToShortDateString(), lblDate.FontIndex);
        if (lblDate.Text != dateText)
        {
            lblDate.Text = dateText;
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        Renderer.DrawRectangle(new Rectangle(X, ClientRectangle.Bottom - 2, Width, 1), UISettings.ActiveSettings.PanelBorderColor);
    }
}

public enum SwitchType
{
    PRIMARY,
    SECONDARY
}