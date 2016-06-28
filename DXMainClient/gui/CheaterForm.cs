using System;
using System.Media;
using ClientCore;
using ClientGUI;

namespace DTAClient.gui
{
    public partial class CheaterForm : MovableForm
    {
        public CheaterForm()
        {
            InitializeComponent();
        }

        private void CheaterForm_Load(object sender, EventArgs e)
        {
            SoundPlayer sPlayer = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "button.wav");

            this.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUILabelColor());
            btnCancel.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltColor());
            btnCancel.DefaultImage = SharedUILogic.LoadImage("133pxbtn.png");
            btnCancel.HoveredImage = SharedUILogic.LoadImage("133pxbtn_c.png");
            btnCancel.HoverSound = sPlayer;
            btnYes.DefaultImage = btnCancel.DefaultImage;
            btnYes.HoveredImage = btnCancel.HoveredImage;
            btnYes.HoverSound = sPlayer;
            btnYes.ForeColor = btnCancel.ForeColor;

            this.BackgroundImage = SharedUILogic.LoadImage("missionselectorbg.png");

            pbFacepalm.Image = SharedUILogic.LoadImage("cheater.png");
        }
    }
}
