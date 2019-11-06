using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.Input;
using Microsoft.Xna.Framework.Input;
using DTAClient.Online;
using ClientGUI;
using ClientCore;
using System.Threading;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAConfig;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// A top bar that allows switching between various client windows.
    /// </summary>
    public class TopBar : XNAPanel
    {
        /// <summary>
        /// The number of seconds that the top bar will stay down after it has
        /// lost input focus.
        /// </summary>
        const double DOWN_TIME_WAIT_SECONDS = 1.0;
        const double EVENT_DOWN_TIME_WAIT_SECONDS = 2.0;
        const double STARTUP_DOWN_TIME_WAIT_SECONDS = 3.5;

        const double DOWN_MOVEMENT_RATE = 1.7;
        const double UP_MOVEMENT_RATE = 1.7;
        const int APPEAR_CURSOR_THRESHOLD_Y = 8;

        public TopBar(WindowManager windowManager, CnCNetManager connectionManager) : base(windowManager)
        {
            downTimeWaitTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS);
            this.connectionManager = connectionManager;
        }

        public SwitchType LastSwitchType { get; private set; }

        List<ISwitchable> primarySwitches = new List<ISwitchable>();
        ISwitchable cncnetLobbySwitch;
        ISwitchable privateMessageSwitch;
        OptionsWindow optionsWindow;

        XNAClientButton btnMainButton;
        XNAClientButton btnCnCNetLobby;
        XNAClientButton btnPrivateMessages;
        XNAClientButton btnOptions;
        XNAClientButton btnLogout;
        XNALabel lblTime;
        XNALabel lblDate;
        XNALabel lblCnCNetStatus;
        XNALabel lblCnCNetPlayerCount;
        XNALabel lblConnectionStatus;

        CnCNetManager connectionManager;

        private CancellationTokenSource cncnetPlayerCountCancellationSource;
        private static readonly object locker = new object();

        TimeSpan downTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS - STARTUP_DOWN_TIME_WAIT_SECONDS);

        TimeSpan downTimeWaitTime;

        bool isDown = true;

        double locationY = -40.0;

        public void AddPrimarySwitchable(ISwitchable switchable)
        {
            primarySwitches.Add(switchable);
            btnMainButton.Text = switchable.GetSwitchName() + " (F2)";
        }

        public void RemovePrimarySwitchable(ISwitchable switchable)
        {
            primarySwitches.Remove(switchable);
            btnMainButton.Text = primarySwitches[primarySwitches.Count - 1].GetSwitchName() + " (F2)";
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
            if (!optionsWindow.Enabled)
                UnlockSwitchButtons();
            else
                LockSwitchButtons();
        }

        public void Clean()
        {
            if (cncnetPlayerCountCancellationSource != null) cncnetPlayerCountCancellationSource.Cancel();
        }

        public override void Initialize()
        {
            Name = "TopBar";
            ClientRectangle = new Rectangle(0, -39, WindowManager.RenderResolutionX, 39);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(Color.Black, 1, 1);
            DrawBorders = false;

            btnMainButton = new XNAClientButton(WindowManager);
            btnMainButton.Name = "btnMainButton";
            btnMainButton.ClientRectangle = new Rectangle(12, 9, 160, 23);
            btnMainButton.Text = "Main Menu (F2)";
            btnMainButton.LeftClick += BtnMainButton_LeftClick;

            btnCnCNetLobby = new XNAClientButton(WindowManager);
            btnCnCNetLobby.Name = "btnCnCNetLobby";
            btnCnCNetLobby.ClientRectangle = new Rectangle(184, 9, 160, 23);
            btnCnCNetLobby.Text = "CnCNet Lobby (F3)";
            btnCnCNetLobby.LeftClick += BtnCnCNetLobby_LeftClick;

            btnPrivateMessages = new XNAClientButton(WindowManager);
            btnPrivateMessages.Name = "btnPrivateMessages";
            btnPrivateMessages.ClientRectangle = new Rectangle(356, 9, 160, 23);
            btnPrivateMessages.Text = "Private Messages (F4)";
            btnPrivateMessages.LeftClick += BtnPrivateMessages_LeftClick;

            lblDate = new XNALabel(WindowManager);
            lblDate.Name = "lblDate";
            lblDate.FontIndex = 1;
            lblDate.Text = Renderer.GetSafeString(DateTime.Now.ToShortDateString(), lblDate.FontIndex);
            lblDate.ClientRectangle = new Rectangle(Width -
                (int)Renderer.GetTextDimensions(lblDate.Text, lblDate.FontIndex).X - 12, 18,
                lblDate.Width, lblDate.Height);

            lblTime = new XNALabel(WindowManager);
            lblTime.Name = "lblTime";
            lblTime.FontIndex = 1;
            lblTime.Text = "99:99:99";
            lblTime.ClientRectangle = new Rectangle(Width -
                (int)Renderer.GetTextDimensions(lblTime.Text, lblTime.FontIndex).X - 12, 4,
                lblTime.Width, lblTime.Height);

            btnLogout = new XNAClientButton(WindowManager);
            btnLogout.Name = "btnLogout";
            btnLogout.ClientRectangle = new Rectangle(lblDate.X - 87, 9, 75, 23);
            btnLogout.FontIndex = 1;
            btnLogout.Text = "Log Out";
            btnLogout.AllowClick = false;
            btnLogout.LeftClick += BtnLogout_LeftClick;

            btnOptions = new XNAClientButton(WindowManager);
            btnOptions.Name = "btnOptions";
            btnOptions.ClientRectangle = new Rectangle(btnLogout.X - 122, 9, 110, 23);
            btnOptions.Text = "Options (F12)";
            btnOptions.LeftClick += BtnOptions_LeftClick;

            lblConnectionStatus = new XNALabel(WindowManager);
            lblConnectionStatus.Name = "lblConnectionStatus";
            lblConnectionStatus.FontIndex = 1;
            lblConnectionStatus.Text = "OFFLINE";

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
                lblCnCNetStatus = new XNALabel(WindowManager);
                lblCnCNetStatus.Name = "lblCnCNetStatus";
                lblCnCNetStatus.FontIndex = 1;
                lblCnCNetStatus.Text = ClientConfiguration.Instance.LocalGame.ToUpper() + " PLAYERS ONLINE:";
                lblCnCNetPlayerCount = new XNALabel(WindowManager);
                lblCnCNetPlayerCount.Name = "lblCnCNetPlayerCount";
                lblCnCNetPlayerCount.FontIndex = 1;
                lblCnCNetPlayerCount.Text = "-";
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

        }

        private void CnCNetInfoController_CnCNetGameCountUpdated(object sender, PlayerCountEventArgs e)
        {
            lock (locker)
            {
                if (e.PlayerCount == -1)
                    lblCnCNetPlayerCount.Text = "N/A";
                else
                    lblCnCNetPlayerCount.Text = e.PlayerCount.ToString();
            }
        }

        private void ConnectionManager_ConnectionLost(object sender, Online.EventArguments.ConnectionLostEventArgs e)
        {
            ConnectionEvent("OFFLINE");
        }

        private void ConnectionManager_ConnectAttemptFailed(object sender, EventArgs e)
        {
            ConnectionEvent("OFFLINE");
        }

        private void ConnectionManager_AttemptedServerChanged(object sender, Online.EventArguments.AttemptedServerEventArgs e)
        {
            ConnectionEvent("CONNECTING...");
            BringDown();
        }

        private void ConnectionManager_WelcomeMessageReceived(object sender, Online.EventArguments.ServerMessageEventArgs e)
        {
            ConnectionEvent("CONNECTED");
        }

        private void ConnectionManager_Disconnected(object sender, EventArgs e)
        {
            btnLogout.AllowClick = false;
            ConnectionEvent("OFFLINE");
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
            return primarySwitches[primarySwitches.Count - 1];
        }

        public void SwitchToSecondary()
        {
            BtnCnCNetLobby_LeftClick(this, EventArgs.Empty);
        }

        private void BtnCnCNetLobby_LeftClick(object sender, EventArgs e)
        {
            LastSwitchType = SwitchType.SECONDARY;
            primarySwitches[primarySwitches.Count - 1].SwitchOff();
            cncnetLobbySwitch.SwitchOn();
            privateMessageSwitch.SwitchOff();
        }

        private void BtnMainButton_LeftClick(object sender, EventArgs e)
        {
            LastSwitchType = SwitchType.PRIMARY;
            cncnetLobbySwitch.SwitchOff();
            privateMessageSwitch.SwitchOff();
            primarySwitches[primarySwitches.Count - 1].SwitchOn();
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
                return;

            if (e.PressedKey == Keys.F1)
            {
                BringDown();
            }
            else if (e.PressedKey == Keys.F2 && btnMainButton.AllowClick)
            {
                BtnMainButton_LeftClick(this, EventArgs.Empty);
            }
            else if (e.PressedKey == Keys.F3 && btnCnCNetLobby.AllowClick)
            {
                BtnCnCNetLobby_LeftClick(this, EventArgs.Empty);
            }
            else if (e.PressedKey == Keys.F4 && btnPrivateMessages.AllowClick)
            {
                BtnPrivateMessages_LeftClick(this, EventArgs.Empty);
            }
            else if (e.PressedKey == Keys.F12 && btnOptions.AllowClick)
            {
                BtnOptions_LeftClick(this, EventArgs.Empty);
            }
        }

        public override void OnMouseOnControl(MouseEventArgs eventArgs)
        {
            if (Cursor.Location.Y > -1 && !ProgramConstants.IsInGame)
                BringDown();

            base.OnMouseOnControl(eventArgs);
        }

        void BringDown()
        {
            isDown = true;
            downTime = TimeSpan.Zero;
        }

        public void SetMainButtonText(string text)
        {
            btnMainButton.Text = text;
        }

        public void LockSwitchButtons()
        {
            if (btnMainButton != null && btnCnCNetLobby != null && btnPrivateMessages != null)
            {
                btnMainButton.AllowClick = false;
                btnCnCNetLobby.AllowClick = false;
                btnPrivateMessages.AllowClick = false;
            }
        }

        public void UnlockSwitchButtons()
        {
            if (btnMainButton != null && btnCnCNetLobby != null && btnPrivateMessages != null)
            {
                btnMainButton.AllowClick = true;
                btnCnCNetLobby.AllowClick = true;
                btnPrivateMessages.AllowClick = true;
            }
            
        }

        public override void Update(GameTime gameTime)
        {
            if (Cursor.Location.Y < APPEAR_CURSOR_THRESHOLD_Y && Cursor.Location.Y > -1
                && !ProgramConstants.IsInGame)
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
                    return; // Don't handle input when the cursor is above our game window
            }

            DateTime dtn = DateTime.Now;

            lblTime.Text = Renderer.GetSafeString(dtn.ToLongTimeString(), lblTime.FontIndex);
            string dateText = Renderer.GetSafeString(dtn.ToShortDateString(), lblDate.FontIndex);
            if (lblDate.Text != dateText)
                lblDate.Text = dateText;

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
}
