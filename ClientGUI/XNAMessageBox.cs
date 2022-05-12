using Localization;
using System;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework.Input;

namespace ClientGUI
{
    /// <summary>
    /// A generic message box with OK or Yes/No or OK/Cancel buttons.
    /// </summary>
    public class XNAMessageBox : XNAWindow
    {
        /// <summary>
        /// Creates a new message box.
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        /// <param name="caption">The caption of the message box.</param>
        /// <param name="description">The actual message of the message box.</param>
        /// <param name="messageBoxButtons">Defines which buttons are available in the dialog.</param>
        public XNAMessageBox(WindowManager windowManager,
            string caption, string description, XNAMessageBoxButtons messageBoxButtons)
            : base(windowManager)
        {
            this.caption = caption;
            this.description = description;
            this.messageBoxButtons = messageBoxButtons;
        }

        /// <summary>
        /// The method that is called when the user clicks OK on the message box.
        /// </summary>
        public Action<XNAMessageBox> OKClickedAction { get; set; }

        /// <summary>
        /// The method that is called when the user clicks Yes on the message box.
        /// </summary>
        public Action<XNAMessageBox> YesClickedAction { get; set; }

        /// <summary>
        /// The method that is called when the user clicks No on the message box.
        /// </summary>
        public Action<XNAMessageBox> NoClickedAction { get; set; }

        /// <summary>
        /// The method that is called when the user clicks Cancel on the message box.
        /// </summary>
        public Action<XNAMessageBox> CancelClickedAction { get; set; }


        private string caption;
        private string description;
        private XNAMessageBoxButtons messageBoxButtons;

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
            line.ClientRectangle = new Rectangle(6, 29, Width - 12, 1);

            if (messageBoxButtons == XNAMessageBoxButtons.OK)
            {
                AddOKButton();
            }
            else if (messageBoxButtons == XNAMessageBoxButtons.YesNo)
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
            btnOK.HoverSoundEffect = new EnhancedSoundEffect("button.wav");
            btnOK.Name = "btnOK";
            btnOK.Text = "OK".L10N("UI:ClientGUI:ButtonOK");
            btnOK.LeftClick += BtnOK_LeftClick;
            btnOK.HotKey = Keys.Enter;

            AddChild(btnOK);

            btnOK.CenterOnParent();
            btnOK.ClientRectangle = new Rectangle(btnOK.X,
                Height - 28, btnOK.Width, btnOK.Height);
        }

        private void AddYesNoButtons()
        {
            XNAButton btnYes = new XNAButton(WindowManager);
            btnYes.FontIndex = 1;
            btnYes.ClientRectangle = new Rectangle(0, 0, 75, 23);
            btnYes.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnYes.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnYes.HoverSoundEffect = new EnhancedSoundEffect("button.wav");
            btnYes.Name = "btnYes";
            btnYes.Text = "Yes".L10N("UI:ClientGUI:ButtonYes");
            btnYes.LeftClick += BtnYes_LeftClick;
            btnYes.HotKey = Keys.Y;

            AddChild(btnYes);

            btnYes.ClientRectangle = new Rectangle((Width - ((btnYes.Width + 5) * 2)) / 2,
                Height - 28, btnYes.Width, btnYes.Height);

            XNAButton btnNo = new XNAButton(WindowManager);
            btnNo.FontIndex = 1;
            btnNo.ClientRectangle = new Rectangle(0, 0, 75, 23);
            btnNo.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnNo.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnNo.HoverSoundEffect = new EnhancedSoundEffect("button.wav");
            btnNo.Name = "btnNo";
            btnNo.Text = "No".L10N("UI:ClientGUI:ButtonNo");
            btnNo.LeftClick += BtnNo_LeftClick;
            btnNo.HotKey = Keys.N;

            AddChild(btnNo);

            btnNo.ClientRectangle = new Rectangle(btnYes.X + btnYes.Width + 10,
                Height - 28, btnNo.Width, btnNo.Height);
        }

        private void AddOKCancelButtons()
        {
            XNAButton btnOK = new XNAButton(WindowManager);
            btnOK.FontIndex = 1;
            btnOK.ClientRectangle = new Rectangle(0, 0, 75, 23);
            btnOK.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnOK.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnOK.HoverSoundEffect = new EnhancedSoundEffect("button.wav");
            btnOK.Name = "btnOK";
            btnOK.Text = "OK".L10N("UI:ClientGUI:ButtonOK");
            btnOK.LeftClick += BtnYes_LeftClick;
            btnOK.HotKey = Keys.Enter;

            AddChild(btnOK);

            btnOK.ClientRectangle = new Rectangle((Width - ((btnOK.Width + 5) * 2)) / 2,
                Height - 28, btnOK.Width, btnOK.Height);

            XNAButton btnCancel = new XNAButton(WindowManager);
            btnCancel.FontIndex = 1;
            btnCancel.ClientRectangle = new Rectangle(0, 0, 75, 23);
            btnCancel.IdleTexture = AssetLoader.LoadTexture("75pxbtn.png");
            btnCancel.HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png");
            btnCancel.HoverSoundEffect = new EnhancedSoundEffect("button.wav");
            btnCancel.Name = "btnCancel";
            btnCancel.Text = "Cancel".L10N("UI:ClientGUI:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;
            btnCancel.HotKey = Keys.C;

            AddChild(btnCancel);

            btnCancel.ClientRectangle = new Rectangle(btnOK.X + btnOK.Width + 10,
                Height - 28, btnCancel.Width, btnCancel.Height);
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            Hide();
            OKClickedAction?.Invoke(this);
        }

        private void BtnYes_LeftClick(object sender, EventArgs e)
        {
            Hide();
            YesClickedAction?.Invoke(this);
        }

        private void BtnNo_LeftClick(object sender, EventArgs e)
        {
            Hide();
            NoClickedAction?.Invoke(this);
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Hide();
            CancelClickedAction?.Invoke(this);
        }

        private void Hide()
        {
            if (this.Parent != null)
                WindowManager.RemoveControl(this.Parent);
            else
                WindowManager.RemoveControl(this);
        }

        public void Show()
        {
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
        }

        #region Static Show methods

        /// <summary>
        /// Creates and displays a new message box with the specified caption and description.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="caption">The caption/header of the message box.</param>
        /// <param name="description">The description of the message box.</param>
        public static void Show(WindowManager windowManager, string caption, string description)
        {
            var panel = new DarkeningPanel(windowManager);
            panel.Focused = true;
            windowManager.AddAndInitializeControl(panel);

            var msgBox = new XNAMessageBox(windowManager,
                Renderer.GetSafeString(caption, 1), 
                Renderer.GetSafeString(description, 0), 
                XNAMessageBoxButtons.OK);

            panel.AddChild(msgBox);
            msgBox.OKClickedAction = MsgBox_OKClicked;
            windowManager.AddAndInitializeControl(msgBox);
            windowManager.SelectedControl = null;
        }

        private static void MsgBox_OKClicked(XNAMessageBox messageBox)
        {
            var parent = (DarkeningPanel)messageBox.Parent;
            parent.Hide();
            parent.Hidden += Parent_Hidden;
        }

        /// <summary>
        /// Shows a message box with "Yes" and "No" being the user input options.
        /// </summary>
        /// <param name="windowManager">The WindowManager.</param>
        /// <param name="caption">The caption of the message box.</param>
        /// <param name="description">The description in the message box.</param>
        /// <returns>The XNAMessageBox instance that is created.</returns>
        public static XNAMessageBox ShowYesNoDialog(WindowManager windowManager, string caption, string description) 
            => ShowYesNoDialog(windowManager, caption, description, null);

        public static XNAMessageBox ShowYesNoDialog(WindowManager windowManager, string caption, string description, Action<XNAMessageBox> yesAction)
        {
            var panel = new DarkeningPanel(windowManager);
            windowManager.AddAndInitializeControl(panel);

            var msgBox = new XNAMessageBox(windowManager,
                Renderer.GetSafeString(caption, 1),
                Renderer.GetSafeString(description, 0),
                XNAMessageBoxButtons.YesNo);

            panel.AddChild(msgBox);
            msgBox.YesClickedAction = MsgBox_YesClicked;
            if (yesAction != null)
                msgBox.YesClickedAction += yesAction;
            msgBox.NoClickedAction = MsgBox_NoClicked;

            return msgBox;
        }

        private static void MsgBox_NoClicked(XNAMessageBox messageBox)
        {
            var parent = (DarkeningPanel)messageBox.Parent;
            parent.Hide();
            parent.Hidden += Parent_Hidden;
        }

        private static void MsgBox_YesClicked(XNAMessageBox messageBox)
        {
            var parent = (DarkeningPanel)messageBox.Parent;
            parent.Hide();
            parent.Hidden += Parent_Hidden;
        }

        private static void Parent_Hidden(object sender, EventArgs e)
        {
            var darkeningPanel = (DarkeningPanel)sender;

            darkeningPanel.WindowManager.RemoveControl(darkeningPanel);
            darkeningPanel.Hidden -= Parent_Hidden;
        }

        #endregion
    }

    public enum XNAMessageBoxButtons
    {
        OK,
        YesNo,
        OKCancel
    }
}
