using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using dtasetup.domain;
using ClientCore;
using ClientGUI;

namespace dtasetup.gui
{
    public partial class SplashScreen : Form
    {
        Image image;

        public SplashScreen()
        {
            InitializeComponent();
            this.Text = MCDomainController.Instance().GetLongGameName();
            image = SharedUILogic.LoadImage("splashScreen.png");
            this.Size = new Size(image.Width, image.Height);
            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainclienticon.ico");
        }

        private void SplashScreen_Paint(object sender, PaintEventArgs e)
        {

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Do nothing
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.DrawImage(image, new Rectangle(0, 0, this.Width, this.Height));
        }
    }
}
