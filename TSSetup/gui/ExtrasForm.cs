using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Media;
using System.Diagnostics;
using ClientCore;
using ClientGUI;
using dtasetup.domain;

namespace dtasetup.gui
{
    public partial class ExtrasForm : MovableForm
    {
        public ExtrasForm()
        {
            InitializeComponent();

            IniFile clientThemeIni = new IniFile(MainClientConstants.gamepath + ProgramConstants.RESOURCES_DIR + "ExtrasMenu.ini");

            this.BackgroundImage = SharedUILogic.LoadImage("extrasMenu.png");
            this.Size = this.BackgroundImage.Size;

            SoundPlayer sPlayer = new SoundPlayer(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "MainMenu\\button.wav");
            btnExtraMapEditor.DefaultImage = SharedUILogic.LoadImage("133pxbtn.png");
            btnExtraMapEditor.HoveredImage = SharedUILogic.LoadImage("133pxbtn_c.png");
            btnExtraMapEditor.RefreshSize();
            btnExtraMapEditor.HoverSound = sPlayer;

            btnExtraStatistics.DefaultImage = btnExtraMapEditor.DefaultImage;
            btnExtraStatistics.HoveredImage = btnExtraMapEditor.HoveredImage;
            btnExtraStatistics.RefreshSize();
            btnExtraStatistics.HoverSound = sPlayer;

            btnExtraCancel.DefaultImage = btnExtraMapEditor.DefaultImage;
            btnExtraCancel.HoveredImage = btnExtraMapEditor.HoveredImage;
            btnExtraCancel.RefreshSize();
            btnExtraCancel.HoverSound = sPlayer;

            btnExtraCredits.DefaultImage = btnExtraMapEditor.DefaultImage;
            btnExtraCredits.HoveredImage = btnExtraMapEditor.HoveredImage;
            btnExtraCredits.RefreshSize();
            btnExtraCredits.HoverSound = sPlayer;

            this.ForeColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUIAltColor());

            InitializeMovableForm();

            if (this.ParentForm == null)
                return;

            this.Location = new Point(ParentForm.Location.X + (ParentForm.Size.Width - this.Size.Width) / 2,
                ParentForm.Location.Y + (ParentForm.Size.Height - this.Size.Height / 2));
        }

        private void ExtrasForm_Load(object sender, EventArgs e)
        {
        }

        private void btnExtraCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnExtraMapEditor_Click(object sender, EventArgs e)
        {
            Process.Start(ProgramConstants.gamepath + MCDomainController.Instance().GetMapEditorExePath());
        }

        private void btnExtraStatistics_Click(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(ProgramConstants.gamepath + "statistics.dat");
            startInfo.Arguments = "\"-RESDIR=" + ProgramConstants.RESOURCES_DIR.Remove(ProgramConstants.RESOURCES_DIR.Length - 1) + "\"";
            startInfo.UseShellExecute = false;

            Logger.Log("Starting DTAScore viewer.");

            Process process = new Process();
            process.StartInfo = startInfo;
            this.Hide();

            process.Start();
        }

        private void btnExtraCredits_Click(object sender, EventArgs e)
        {
            Process.Start(MainClientConstants.CREDITS_URL);
        }
    }
}
