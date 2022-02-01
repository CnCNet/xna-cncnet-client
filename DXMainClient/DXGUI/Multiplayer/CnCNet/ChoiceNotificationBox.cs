using ClientCore;
using ClientGUI;
using Localization;
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
        private const double DOWN_TIME_WAIT_SECONDS = 4.0;
        private const double DOWN_MOVEMENT_RATE = 2.0;
        private const double UP_MOVEMENT_RATE = 2.0;

        public ChoiceNotificationBox(WindowManager windowManager) : base(windowManager)
        {
            downTimeWaitTime = TimeSpan.FromSeconds(DOWN_TIME_WAIT_SECONDS);
        }

        public Action<ChoiceNotificationBox> AffirmativeClickedAction { get; set; }
        public Action<ChoiceNotificationBox> NegativeClickedAction { get; set; }

        private XNALabel lblHeader;
        private XNAPanel gameIconPanel;
        private XNALabel lblSender;
        private XNALabel lblChoiceText;
        private XNAClientButton affirmativeButton;
        private XNAClientButton negativeButton;

        private TimeSpan downTime = TimeSpan.Zero;

        private TimeSpan downTimeWaitTime;

        private bool isDown = false;

        private const int boxHeight = 101;

        private double locationY = -boxHeight;

        public override void Initialize()
        {
            Name = nameof(ChoiceNotificationBox);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 1, 1);
            ClientRectangle = new Rectangle(0, -boxHeight, 300, boxHeight);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.FontIndex = 1;
            lblHeader.AnchorPoint = new Vector2(ClientRectangle.Width / 2, 12);
            lblHeader.TextAnchor = LabelTextAnchorInfo.CENTER;
            lblHeader.Text = "MAKE A CHOICE".L10N("UI:Main:MakeAChoice");
            AddChild(lblHeader);

            gameIconPanel = new XNAPanel(WindowManager);
            gameIconPanel.Name = nameof(gameIconPanel);
            gameIconPanel.ClientRectangle = new Rectangle(12, lblHeader.Bottom + 6, 16, 16);
            gameIconPanel.DrawBorders = false;
            gameIconPanel.BackgroundTexture = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.dtaicon);
            AddChild(gameIconPanel);

            lblSender = new XNALabel(WindowManager);
            lblSender.Name = nameof(lblSender);
            lblSender.FontIndex = 1;
            lblSender.ClientRectangle = new Rectangle(gameIconPanel.Right + 3, lblHeader.Bottom + 6, 0, 0);
            lblSender.Text = "fonger";
            AddChild(lblSender);

            lblChoiceText = new XNALabel(WindowManager);
            lblChoiceText.Name = nameof(lblChoiceText);
            lblChoiceText.FontIndex = 1;
            lblChoiceText.ClientRectangle = new Rectangle(12, lblSender.Bottom + 6, 0, 0);
            lblChoiceText.Text = "What do you want to do?".L10N("UI:Main:ChoiceWhatDoYouWant");
            AddChild(lblChoiceText);

            affirmativeButton = new XNAClientButton(WindowManager);
            affirmativeButton.ClientRectangle = new Rectangle(ClientRectangle.Left + 8, lblChoiceText.Bottom + 6, 75, 23);
            affirmativeButton.Name = nameof(affirmativeButton);
            affirmativeButton.Text = "Yes".L10N("UI:Main:ButtonYes");
            affirmativeButton.LeftClick += AffirmativeButton_LeftClick;
            AddChild(affirmativeButton);

            negativeButton = new XNAClientButton(WindowManager);
            negativeButton.ClientRectangle = new Rectangle(ClientRectangle.Width - (75 + 8), lblChoiceText.Bottom + 6, 75, 23);
            negativeButton.Name = nameof(negativeButton);
            negativeButton.Text = "No".L10N("UI:Main:ButtonNo");
            negativeButton.LeftClick += NegativeButton_LeftClick;
            AddChild(negativeButton);

            base.Initialize();
        }

        // a timeout of zero means the notification will never be automatically dismissed
        public void Show(
            string headerText, 
            Texture2D gameIcon,
            string sender,
            string choiceText,
            string affirmativeText,
            string negativeText,
            int timeout = 0)
        {
            Enable();

            lblHeader.Text = headerText;
            gameIconPanel.BackgroundTexture = gameIcon;
            lblSender.Text = sender;
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
            Disable();
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

                    // only change our "down" state if we have a valid timeout
                    if (downTimeWaitTime != TimeSpan.Zero)
                    {
                        isDown = downTime < downTimeWaitTime;
                    }
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
