using System;
using System.Drawing;
using System.Windows.Forms;
using System.Media;
using ClientCore;

namespace ClientGUI
{
    public partial class PasswordQueryForm : MovableForm
    {
        public PasswordQueryForm()
        {
            InitializeComponent();
        }

        Image btn133px;
        Image btn133px_c;
        SoundPlayer sp;

        public string rtnPassword = String.Empty;

        private void PasswordQueryForm_Load(object sender, EventArgs e)
        {
            this.Font = SharedLogic.GetCommonFont();

            this.BackgroundImage = SharedUILogic.LoadImage("passwordquerybg.png");

            btn133px = SharedUILogic.LoadImage("133pxbtn.png");
            btn133px_c = SharedUILogic.LoadImage("133pxbtn_c.png");

            sp = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "button.wav");

            btnCancel.BackgroundImage = btn133px;
            btnOK.BackgroundImage = btn133px;

            string[] labelColor = DomainController.Instance().GetUILabelColor().Split(',');
            Color cLabelColor = Color.FromArgb(Convert.ToByte(labelColor[0]), Convert.ToByte(labelColor[1]), Convert.ToByte(labelColor[2]));
            lblEnterPassword.ForeColor = cLabelColor;

            string[] altUiColor = DomainController.Instance().GetUIAltColor().Split(',');
            Color cAltUiColor = Color.FromArgb(Convert.ToByte(altUiColor[0]), Convert.ToByte(altUiColor[1]), Convert.ToByte(altUiColor[2]));
            tbPassword.ForeColor = cAltUiColor;
            btnCancel.ForeColor = cAltUiColor;
            btnOK.ForeColor = cAltUiColor;

            string[] backgroundColor = DomainController.Instance().GetUIAltBackgroundColor().Split(',');
            Color cBackColor = Color.FromArgb(Convert.ToByte(backgroundColor[0]), Convert.ToByte(backgroundColor[1]), Convert.ToByte(backgroundColor[2]));
            tbPassword.BackColor = cBackColor;
            btnCancel.BackColor = cBackColor;
            btnOK.BackColor = cBackColor;
        }

        private void btnOK_MouseEnter(object sender, EventArgs e)
        {
            btnOK.BackgroundImage = btn133px_c;
            sp.Play();
        }

        private void btnOK_MouseLeave(object sender, EventArgs e)
        {
            btnOK.BackgroundImage = btn133px;
        }

        private void btnCancel_MouseEnter(object sender, EventArgs e)
        {
            btnCancel.BackgroundImage = btn133px_c;
            sp.Play();
        }

        private void btnCancel_MouseLeave(object sender, EventArgs e)
        {
            btnCancel.BackgroundImage = btn133px;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            btn133px.Dispose();
            btn133px_c.Dispose();
            sp.Dispose();
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(tbPassword.Text))
            {
                MessageBox.Show("Please enter the password for the game you wish to join and click OK.");
                return;
            }

            rtnPassword = tbPassword.Text;

            btn133px.Dispose();
            btn133px_c.Dispose();
            sp.Dispose();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
