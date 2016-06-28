using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Media;
using ClientCore;

namespace ClientGUI
{
    public partial class MsgBoxForm : MovableForm
    {
        public MsgBoxForm()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "clienticon.ico");
        }

        public MsgBoxForm(string message, string title, MessageBoxButtons msgButtons)
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "clienticon.ico");

            SoundPlayer sPlayer = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "button.wav");

            lblMsgText.Text = message;
            lblTitle.Text = title;
            this.Text = title;

            lblMsgText.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUILabelColor());
            lblMsgText.Font = Utilities.GetFont(DomainController.Instance().GetCommonFont());
            lblTitle.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltColor());

            this.BackgroundImage = SharedUILogic.LoadImage("msgboxform.png");
            btnOK.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltColor());
            btnCancel.ForeColor = btnOK.ForeColor;
            btnOK.DefaultImage = SharedUILogic.LoadImage("97pxbtn.png");
            btnOK.HoveredImage = SharedUILogic.LoadImage("97pxbtn_c.png");
            btnOK.HoverSound = sPlayer;
            btnOK.Font = lblMsgText.Font;
            btnCancel.DefaultImage = btnOK.DefaultImage;
            btnCancel.HoveredImage = btnOK.HoveredImage;
            btnCancel.HoverSound = btnOK.HoverSound;
            btnCancel.Font = lblMsgText.Font;

            System.Drawing.Graphics graphics = CreateGraphics();
            Font font = lblMsgText.Font;
            SizeF stringSize = graphics.MeasureString(message, font);

            this.Size = new Size(Convert.ToInt32(stringSize.Width), Convert.ToInt32((stringSize.Height * 1.10) + 82));
            line1.Size = new Size(new Point(this.Size.Width - 12, 1));

            if (msgButtons == MessageBoxButtons.OK)
            {
                btnCancel.Visible = false;
                btnOK.Location = new Point((this.Size.Width / 2) - 48, this.Size.Height - 28);
            }
            else if (msgButtons == MessageBoxButtons.OKCancel)
            {
                btnOK.Location = new Point((this.Size.Width / 2) - 100, this.Size.Height - 28);
                btnCancel.Location = new Point((this.Size.Width / 2) +3, this.Size.Height - 28);
            }
            else if (msgButtons == MessageBoxButtons.YesNo)
            {
                btnOK.Location = new Point((this.Size.Width / 2) - 100, this.Size.Height - 28);
                btnCancel.Location = new Point((this.Size.Width / 2) + 3, this.Size.Height - 28);

                btnOK.Text = "Yes";
                btnCancel.Text = "No";
            }

            if (this.ParentForm == null || this.Parent == null || this.Location.Y == 0 || this.Location.X == 0)
            {
                this.Location = new Point((Screen.PrimaryScreen.Bounds.Width - this.Width) / 2,
                    (Screen.PrimaryScreen.Bounds.Height - this.Height) / 2);
            }

            SharedUILogic.ParseClientThemeIni(this);
        }

        private void MsgBoxForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.ToLower(e.KeyChar) == 'y')
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
            else if (Char.ToLower(e.KeyChar) == 'n')
            {
                if (btnCancel.Visible)
                {
                    this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                    this.Close();
                }
            }
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            MsgBoxForm form = new MsgBoxForm(text, caption, buttons);
            form.Location = new Point((Screen.PrimaryScreen.Bounds.Width - form.Width) / 2,
                    (Screen.PrimaryScreen.Bounds.Height - form.Height) / 2);
            DialogResult dr = form.ShowDialog();
            form.Dispose();
            return dr;
        }

        private void MsgBoxForm_Load(object sender, EventArgs e)
        {
            this.TopMost = true;
            this.BringToFront();
            this.TopMost = false;
        }
    }
}
