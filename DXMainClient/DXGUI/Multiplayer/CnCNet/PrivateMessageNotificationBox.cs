using Rampastring.XNAUI.XNAControls;
using System;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.Properties;
using ClientCore;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A box that notifies users of new private messages,
    /// top-right of the game window.
    /// </summary>
    public class PrivateMessageNotificationBox : XNAPanel
    {
        const double DOWN_TIME_WAIT_SECONDS = 4.0;
        const double DOWN_MOVEMENT_RATE = 2.0;
        const double UP_MOVEMENT_RATE = 2.0;

        public PrivateMessageNotificationBox(WindowManager windowManager) : base(windowManager)
        {
            downTimeWaitTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS);
        }

        XNALabel lblSender;
        XNAPanel gameIconPanel;
        XNALabel lblMessage;

        TimeSpan downTime = TimeSpan.Zero;

        TimeSpan downTimeWaitTime;

        bool isDown = false;

        double locationY = -100.0;

        public override void Initialize()
        {
            Name = "PrivateMessageNotificationBox";
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 1, 1);
            ClientRectangle = new Rectangle(WindowManager.RenderResolutionX - 300, -100, 300, 100);
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            XNALabel lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = "lblHeader";
            lblHeader.FontIndex = 1;
            lblHeader.Text = "PRIVATE MESSAGE";
            AddChild(lblHeader);
            lblHeader.CenterOnParent();
            lblHeader.ClientRectangle = new Rectangle(lblHeader.ClientRectangle.X,
                6, lblHeader.ClientRectangle.Width, lblHeader.ClientRectangle.Height);

            XNAPanel linePanel = new XNAPanel(WindowManager);
            linePanel.Name = "linePanel";
            linePanel.ClientRectangle = new Rectangle(0, ClientRectangle.Height - 20, ClientRectangle.Width, 1);

            XNALabel lblHint = new XNALabel(WindowManager);
            lblHint.Name = "lblHint";
            lblHint.RemapColor = UISettings.SubtleTextColor;
            lblHint.Text = "Press F4 to respond";

            AddChild(lblHint);
            lblHint.CenterOnParent();
            lblHint.ClientRectangle = new Rectangle(lblHint.ClientRectangle.X,
                linePanel.ClientRectangle.Y + 3,
                lblHint.ClientRectangle.Width, lblHint.ClientRectangle.Height);

            gameIconPanel = new XNAPanel(WindowManager);
            gameIconPanel.Name = "gameIconPanel";
            gameIconPanel.ClientRectangle = new Rectangle(12, 30, 16, 16);
            gameIconPanel.DrawBorders = false;
            gameIconPanel.BackgroundTexture = AssetLoader.TextureFromImage(Resources.dtaicon);

            lblSender = new XNALabel(WindowManager);
            lblSender.Name = "lblSender";
            lblSender.FontIndex = 1;
            lblSender.ClientRectangle = new Rectangle(gameIconPanel.ClientRectangle.Right + 3,
                gameIconPanel.ClientRectangle.Y, 0, 0);
            lblSender.Text = "Rampastring:";

            lblMessage = new XNALabel(WindowManager);
            lblMessage.Name = "lblMessage";
            lblMessage.ClientRectangle = new Rectangle(12, lblSender.ClientRectangle.Bottom + 6, 0, 0);
            lblMessage.RemapColor = AssetLoader.GetColorFromString(DomainController.Instance().GetReceivedPMColor());
            lblMessage.Text = "This is a test message.";

            AddChild(lblHeader);
            AddChild(gameIconPanel);
            AddChild(linePanel);
            AddChild(lblSender);
            AddChild(lblMessage);

            base.Initialize();
        }

        public void Show(Texture2D gameIcon, string sender, string message)
        {
            Visible = true;
            Enabled = true;
            gameIconPanel.BackgroundTexture = gameIcon;
            lblSender.Text = sender + ":";
            lblMessage.Text = message;

            if (lblMessage.ClientRectangle.Right > ClientRectangle.Width)
            {
                while (lblMessage.ClientRectangle.Right > ClientRectangle.Width)
                {
                    lblMessage.Text = lblMessage.Text.Remove(lblMessage.Text.Length - 1);
                }

                if (lblMessage.Text.Length > 3)
                {
                    lblMessage.Text = lblMessage.Text.Remove(lblMessage.Text.Length - 3) + "...";
                }
            }

            downTime = TimeSpan.Zero;
            isDown = true;
        }

        public void Hide()
        {
            isDown = false;
            locationY = -ClientRectangle.Height;
            ClientRectangle = new Rectangle(ClientRectangle.X, (int)locationY,
                ClientRectangle.Width, ClientRectangle.Height);
            Visible = false;
            Enabled = false;
        }

        public override void Update(GameTime gameTime)
        {
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
                else
                {
                    Visible = false;
                    Enabled = false;
                }
            }

            base.Update(gameTime);
        }
    }
}
