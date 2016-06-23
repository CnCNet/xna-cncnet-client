using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.Input;
using Microsoft.Xna.Framework.Input;
using ClientCore;

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
        const double DOWN_TIME_WAIT_SECONDS = 1.5;

        const double DOWN_MOVEMENT_RATE = 2.0;
        const double UP_MOVEMENT_RATE = 2.0;
        const int APPEAR_CURSOR_THRESHOLD_Y = 15;

        public TopBar(WindowManager windowManager) : base(windowManager)
        {
            downTimeWaitTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS);
        }

        List<ISwitchable> primarySwitches = new List<ISwitchable>();
        ISwitchable cncnetLobbySwitch;
        ISwitchable privateMessageSwitch;

        XNAButton btnMainButton;
        XNAButton btnCnCNetLobby;
        XNAButton btnPrivateMessages;
        XNALabel lblTime;
        XNALabel lblDate;

        TimeSpan downTime = TimeSpan.Zero;

        TimeSpan downTimeWaitTime;

        bool isDown = false;

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

        public override void Initialize()
        {
            Name = "TopBar";
            ClientRectangle = new Rectangle(0, -40, WindowManager.RenderResolutionX, 40);
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(Color.Black, 1, 1);
            DrawBorders = false;

            btnMainButton = new XNAButton(WindowManager);
            btnMainButton.Name = "btnMainButton";
            btnMainButton.ClientRectangle = new Rectangle(12, 12, 160, 23);
            btnMainButton.FontIndex = 1;
            btnMainButton.Text = "Main Menu (F2)";
            btnMainButton.IdleTexture = AssetLoader.LoadTexture("160pxbtn.png");
            btnMainButton.HoverTexture = AssetLoader.LoadTexture("160pxbtn_c.png");
            btnMainButton.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnMainButton.LeftClick += BtnMainButton_LeftClick;

            btnCnCNetLobby = new XNAButton(WindowManager);
            btnCnCNetLobby.Name = "btnCnCNetLobby";
            btnCnCNetLobby.ClientRectangle = new Rectangle(184, 12, 160, 23);
            btnCnCNetLobby.FontIndex = 1;
            btnCnCNetLobby.Text = "CnCNet Lobby (F3)";
            btnCnCNetLobby.IdleTexture = AssetLoader.LoadTexture("160pxbtn.png");
            btnCnCNetLobby.HoverTexture = AssetLoader.LoadTexture("160pxbtn_c.png");
            btnCnCNetLobby.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnCnCNetLobby.LeftClick += BtnCnCNetLobby_LeftClick;

            btnPrivateMessages = new XNAButton(WindowManager);
            btnPrivateMessages.Name = "btnPrivateMessages";
            btnPrivateMessages.ClientRectangle = new Rectangle(356, 12, 160, 23);
            btnPrivateMessages.FontIndex = 1;
            btnPrivateMessages.Text = "Private Messages (F4)";
            btnPrivateMessages.IdleTexture = AssetLoader.LoadTexture("160pxbtn.png");
            btnPrivateMessages.HoverTexture = AssetLoader.LoadTexture("160pxbtn_c.png");
            btnPrivateMessages.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnPrivateMessages.AllowClick = false;

            lblTime = new XNALabel(WindowManager);
            lblTime.FontIndex = 1;
            lblTime.Name = "lblTime";
            lblTime.Text = "99:99:99";
            lblTime.ClientRectangle = new Rectangle(ClientRectangle.Width - 
                (int)Renderer.GetTextDimensions(lblTime.Text, lblTime.FontIndex).X - 12, 6, 
                lblTime.ClientRectangle.Width, lblTime.ClientRectangle.Height);

            lblDate = new XNALabel(WindowManager);
            lblDate.FontIndex = 1;
            lblDate.Name = "lblDate";
            lblDate.Text = DateTime.Now.ToShortDateString();
            lblDate.ClientRectangle = new Rectangle(ClientRectangle.Width -
                (int)Renderer.GetTextDimensions(lblDate.Text, lblDate.FontIndex).X - 12, 20, 
                lblDate.ClientRectangle.Width, lblDate.ClientRectangle.Height);

            AddChild(btnMainButton);
            AddChild(btnCnCNetLobby);
            AddChild(btnPrivateMessages);
            AddChild(lblTime);
            AddChild(lblDate);

            base.Initialize();

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
        }

        private void BtnCnCNetLobby_LeftClick(object sender, EventArgs e)
        {
            primarySwitches[primarySwitches.Count - 1].SwitchOff();
            cncnetLobbySwitch.SwitchOn();
        }

        private void BtnMainButton_LeftClick(object sender, EventArgs e)
        {
            cncnetLobbySwitch.SwitchOff();
            primarySwitches[primarySwitches.Count - 1].SwitchOn();
        }

        private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (!Enabled || ProgramConstants.IsInGame)
                return;

            if (e.PressedKey == Keys.F1)
            {
                BringDown();
            }
            else if (e.PressedKey == Keys.F2)
            {
                BtnMainButton_LeftClick(this, EventArgs.Empty);
            }
            else if (e.PressedKey == Keys.F3)
            {
                BtnCnCNetLobby_LeftClick(this, EventArgs.Empty);
            }
            else if (e.PressedKey == Keys.F4)
            {
                // Do nothing for now
                // TODO show private messages
            }
        }

        public override void OnMouseOnControl(MouseEventArgs eventArgs)
        {
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

        public override void Update(GameTime gameTime)
        {
            if (Cursor.Location.Y < APPEAR_CURSOR_THRESHOLD_Y && Cursor.Location.Y > -1)
            {
                BringDown();
            }

            if (isDown)
            {
                if (locationY < 0)
                {
                    locationY += DOWN_MOVEMENT_RATE;
                    ClientRectangle = new Rectangle(ClientRectangle.X, (int)locationY, 
                        ClientRectangle.Width, ClientRectangle.Height);
                }

                downTime += gameTime.ElapsedGameTime;

                isDown = downTime < downTimeWaitTime;
            }
            else
            {
                if (locationY > -ClientRectangle.Height)
                {
                    locationY -= UP_MOVEMENT_RATE;
                    ClientRectangle = new Rectangle(ClientRectangle.X, (int)locationY, ClientRectangle.Width, ClientRectangle.Height);
                }
            }

            DateTime dtn = DateTime.Now;

            lblTime.Text = dtn.ToLongTimeString();
            if (lblDate.Text != dtn.ToShortDateString())
                lblDate.Text = dtn.ToShortDateString();

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            Renderer.DrawRectangle(new Rectangle(ClientRectangle.X, ClientRectangle.Bottom, ClientRectangle.Width, 1), Color.Gray);
        }
    }
}
