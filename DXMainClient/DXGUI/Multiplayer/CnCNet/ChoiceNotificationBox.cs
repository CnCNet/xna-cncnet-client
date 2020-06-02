using ClientCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A box that allows users to make a choice,
    /// top-left of the game window.
    /// </summary>
    public class ChoiceNotificationBox : XNAPanel
    {
        const double DOWN_TIME_WAIT_SECONDS = 4.0;
        const double DOWN_MOVEMENT_RATE = 2.0;
        const double UP_MOVEMENT_RATE = 2.0;

        public ChoiceNotificationBox(WindowManager windowManager) : base(windowManager)
        {
            downTimeWaitTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS);
        }

        public Action<ChoiceNotificationBox> AffirmativeClickedAction { get; set; }
        public Action<ChoiceNotificationBox> NegativeClickedAction { get; set; }

        XNALabel lblHeader;
        XNALabel lblChoiceText;
        XNAButton affirmativeButton;
        XNAButton negativeButton;

        TimeSpan downTime = TimeSpan.Zero;

        TimeSpan downTimeWaitTime;

        bool isDown = false;

        const int boxHeight = 79;

        double locationY = -boxHeight;

        public override void Initialize()
        {
            Name = "ChoiceNotificationBox";
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 1, 1);
            ClientRectangle = new Rectangle(0, -boxHeight, 300, boxHeight);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = "lblHeader";
            lblHeader.FontIndex = 1;
            lblHeader.AnchorPoint = new Vector2(ClientRectangle.Width / 2, 12);
            lblHeader.TextAnchor = LabelTextAnchorInfo.CENTER;
            lblHeader.Text = "MAKE A CHOICE";
            AddChild(lblHeader);

            lblChoiceText = new XNALabel(WindowManager);
            lblChoiceText.Name = "lblChoiceText";
            lblChoiceText.FontIndex = 1;
            lblChoiceText.ClientRectangle = new Rectangle(12, lblHeader.Bottom + 6, 0, 0);
            lblChoiceText.Text = "What do you want to do?";
            AddChild(lblChoiceText);

            affirmativeButton = new XNAButton(WindowManager);
            affirmativeButton.FontIndex = 1;
            affirmativeButton.ClientRectangle = new Rectangle(ClientRectangle.Left + 8, lblChoiceText.Bottom + 6, 75, 23);
            affirmativeButton.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            affirmativeButton.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            affirmativeButton.HoverSoundEffect = new EnhancedSoundEffect("button.wav");
            affirmativeButton.Name = "affirmativeButton";
            affirmativeButton.Text = "Yes";
            affirmativeButton.LeftClick += AffirmativeButton_LeftClick;
            AddChild(affirmativeButton);

            negativeButton = new XNAButton(WindowManager);
            negativeButton.FontIndex = 1;
            negativeButton.ClientRectangle = new Rectangle(ClientRectangle.Width - (75 + 8), lblChoiceText.Bottom + 6, 75, 23);
            negativeButton.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            negativeButton.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            negativeButton.HoverSoundEffect = new EnhancedSoundEffect("button.wav");
            negativeButton.Name = "negativeButton";
            negativeButton.Text = "No";
            negativeButton.LeftClick += NegativeButton_LeftClick;
            AddChild(negativeButton);

            base.Initialize();
        }

        public void Show(string headerText, string choiceText, string affirmativeText, string negativeText, int timeout)
        {
            Visible = true;
            Enabled = true;

            lblHeader.Text = headerText;
            lblChoiceText.Text = choiceText;
            affirmativeButton.Text = affirmativeText;
            negativeButton.Text = negativeText;

            // use the same clipping logic as the PM notification
            if (lblChoiceText.Width > Width)
            {
                while (lblChoiceText.Width > Width)
                {
                    lblChoiceText.Text = lblChoiceText.Text.Remove(lblChoiceText.Text.Length - 1);
                }
            }

            downTime = TimeSpan.Zero;
            isDown = true;

            downTimeWaitTime = TimeSpan.FromSeconds(timeout);
        }

        public void Hide()
        {
            isDown = false;
            locationY = -Height;
            ClientRectangle = new Rectangle(X, (int)locationY,
                Width, Height);
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
                    ClientRectangle = new Rectangle(X, (int)locationY,
                        Width, Height);
                }

                if (WindowManager.HasFocus)
                {
                    downTime += gameTime.ElapsedGameTime;
                    isDown = downTime < downTimeWaitTime;
                }
            }
            else
            {
                if (locationY > -Height)
                {
                    locationY -= UP_MOVEMENT_RATE;
                    ClientRectangle = new Rectangle(X, (int)locationY, Width, Height);
                }
                else
                {
                    // effectively delete ourselves when we've timed out
                    WindowManager.RemoveControl(this);
                }
            }

            base.Update(gameTime);
        }

        private void AffirmativeButton_LeftClick(object sender, EventArgs e)
        {
            AffirmativeClickedAction?.Invoke(this);
            WindowManager.RemoveControl(this);
        }

        private void NegativeButton_LeftClick(object sender, EventArgs e)
        {
            NegativeClickedAction?.Invoke(this);
            WindowManager.RemoveControl(this);
        }
    }
}
