using System;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework.Input;

namespace ClientGUI
{
    public class XNAMessageBox : XNAWindow
    {
        public delegate void OKClickedEventHandler(object sender, EventArgs e);
        public event OKClickedEventHandler OKClicked;

        public delegate void YesClickedEventHandler(object sender, EventArgs e);
        public event YesClickedEventHandler YesClicked;

        public delegate void NoClickedEventHandler(object sender, EventArgs e);
        public event NoClickedEventHandler NoClicked;

        public delegate void CancelClickedEventHandler(object sender, EventArgs e);
        public event CancelClickedEventHandler CancelClicked;

        public XNAMessageBox(WindowManager windowManager,
            string caption, string description, DXMessageBoxButtons messageBoxButtons)
            : base(windowManager)
        {
            this.caption = caption;
            this.description = description;
            this.messageBoxButtons = messageBoxButtons;
        }

        string caption;
        string description;
        DXMessageBoxButtons messageBoxButtons;

        public override void Initialize()
        {
            Name = "MessageBox";
            BackgroundTexture = AssetLoader.LoadTexture("msgboxform.png");

            XNALabel lblCaption = new XNALabel(WindowManager);
            lblCaption.Text = caption;
            lblCaption.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblCaption.FontIndex = 1;

            XNAPanel line = new XNAPanel(WindowManager);
            line.ClientRectangle = new Rectangle(6, 29, 0, 1);

            XNALabel lblDescription = new XNALabel(WindowManager);
            lblDescription.Text = description;
            lblDescription.ClientRectangle = new Rectangle(12, 39, 0, 0);

            AddChild(lblCaption);
            AddChild(line);
            AddChild(lblDescription);

            Vector2 textDimensions = Renderer.GetTextDimensions(lblDescription.Text, lblDescription.FontIndex);
            ClientRectangle = new Rectangle(0, 0, (int)textDimensions.X + 24, (int)textDimensions.Y + 81);
            line.ClientRectangle = new Rectangle(6, 29, ClientRectangle.Width - 12, 1);

            if (messageBoxButtons == DXMessageBoxButtons.OK)
            {
                AddOKButton();
            }
            else if (messageBoxButtons == DXMessageBoxButtons.YesNo)
            {
                AddYesNoButtons();
            }
            else // messageBoxButtons == DXMessageBoxButtons.OKCancel
            {
                AddOKCancelButtons();
            }

            base.Initialize();

            WindowManager.CenterControlOnScreen(this);
        }

        private void AddOKButton()
        {
            XNAButton btnOK = new XNAButton(WindowManager);
            btnOK.FontIndex = 1;
            btnOK.ClientRectangle = new Rectangle(0, 0, 75, 23);
            btnOK.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnOK.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnOK.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnOK.Name = "btnOK";
            btnOK.Text = "OK";
            btnOK.LeftClick += BtnOK_LeftClick;
            btnOK.HotKey = Keys.Enter;

            AddChild(btnOK);

            btnOK.CenterOnParent();
            btnOK.ClientRectangle = new Rectangle(btnOK.ClientRectangle.X,
                ClientRectangle.Height - 28, btnOK.ClientRectangle.Width, btnOK.ClientRectangle.Height);
        }

        private void AddYesNoButtons()
        {
            XNAButton btnYes = new XNAButton(WindowManager);
            btnYes.FontIndex = 1;
            btnYes.ClientRectangle = new Rectangle(0, 0, 75, 23);
            btnYes.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnYes.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnYes.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnYes.Name = "btnYes";
            btnYes.Text = "Yes";
            btnYes.LeftClick += BtnYes_LeftClick;
            btnYes.HotKey = Keys.Y;

            AddChild(btnYes);

            btnYes.ClientRectangle = new Rectangle((ClientRectangle.Width - ((btnYes.ClientRectangle.Width + 5) * 2)) / 2,
                ClientRectangle.Height - 28, btnYes.ClientRectangle.Width, btnYes.ClientRectangle.Height);

            XNAButton btnNo = new XNAButton(WindowManager);
            btnNo.FontIndex = 1;
            btnNo.ClientRectangle = new Rectangle(0, 0, 75, 23);
            btnNo.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnNo.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnNo.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnNo.Name = "btnNo";
            btnNo.Text = "No";
            btnNo.LeftClick += BtnNo_LeftClick;
            btnNo.HotKey = Keys.N;

            AddChild(btnNo);

            btnNo.ClientRectangle = new Rectangle(btnYes.ClientRectangle.X + btnYes.ClientRectangle.Width + 10,
                ClientRectangle.Height - 28, btnNo.ClientRectangle.Width, btnNo.ClientRectangle.Height);
        }

        private void AddOKCancelButtons()
        {
            XNAButton btnOK = new XNAButton(WindowManager);
            btnOK.FontIndex = 1;
            btnOK.ClientRectangle = new Rectangle(0, 0, 75, 23);
            btnOK.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnOK.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnOK.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnOK.Name = "btnOK";
            btnOK.Text = "OK";
            btnOK.LeftClick += BtnYes_LeftClick;
            btnOK.HotKey = Keys.Enter;

            AddChild(btnOK);

            btnOK.ClientRectangle = new Rectangle((ClientRectangle.Width - ((btnOK.ClientRectangle.Width + 5) * 2)) / 2,
                ClientRectangle.Height - 28, btnOK.ClientRectangle.Width, btnOK.ClientRectangle.Height);

            XNAButton btnCancel = new XNAButton(WindowManager);
            btnCancel.FontIndex = 1;
            btnCancel.ClientRectangle = new Rectangle(0, 0, 75, 23);
            btnCancel.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnCancel.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnCancel.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnCancel.Name = "btnCancel";
            btnCancel.Text = "Cancel";
            btnCancel.LeftClick += BtnCancel_LeftClick;
            btnCancel.HotKey = Keys.C;

            AddChild(btnCancel);

            btnCancel.ClientRectangle = new Rectangle(btnOK.ClientRectangle.X + btnOK.ClientRectangle.Width + 10,
                ClientRectangle.Height - 28, btnCancel.ClientRectangle.Width, btnCancel.ClientRectangle.Height);
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            Hide();
            OKClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnYes_LeftClick(object sender, EventArgs e)
        {
            Hide();
            YesClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnNo_LeftClick(object sender, EventArgs e)
        {
            Hide();
            NoClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Hide();
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }

        private void Hide()
        {
            WindowManager.RemoveControl(this);
        }

        public void Show()
        {
            WindowManager.AddAndInitializeControl(this);
            Focused = true;
        }

        /// <summary>
        /// Creates and displays a new message box with the specified caption and description.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="caption">The caption/header of the message box.</param>
        /// <param name="description">The description of the message box.</param>
        public static void Show(WindowManager windowManager, string caption, string description)
        {
            DarkeningPanel panel = new DarkeningPanel(windowManager);
            panel.Focused = true;
            windowManager.AddAndInitializeControl(panel);

            XNAMessageBox msgBox = new XNAMessageBox(windowManager, caption, description, DXMessageBoxButtons.OK);

            panel.AddChild(msgBox);
            msgBox.OKClicked += MsgBox_OKClicked;
            windowManager.AddAndInitializeControl(msgBox);
        }

        private static void MsgBox_OKClicked(object sender, EventArgs e)
        {
            var messagebox = (XNAMessageBox)sender;

            messagebox.OKClicked -= MsgBox_OKClicked;

            var parent = (DarkeningPanel)messagebox.Parent;
            parent.Hide();
            parent.Hidden += Parent_Hidden;
        }

        private static void Parent_Hidden(object sender, EventArgs e)
        {
            var darkeningPanel = (DarkeningPanel)sender;

            darkeningPanel.WindowManager.RemoveControl(darkeningPanel);
            darkeningPanel.Hidden -= Parent_Hidden;
        }
    }

    public enum DXMessageBoxButtons
    {
        OK,
        YesNo,
        OKCancel
    }
}
