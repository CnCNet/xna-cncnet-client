using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Media;
using System.Diagnostics;
using System.Threading;
using dtasetup.gui;
using dtasetup.domain;
using ClientCore;
using ClientGUI;

namespace dtasetup.gui
{
    /// <summary>
    /// A form that informs the user about an update and queries if they 
    /// want the client to perform the update.
    /// </summary>
    public partial class UpdateQueryForm : MovableForm
    {
        string NewGameVersion = "";

        public UpdateQueryForm()
        {
            InitializeComponent();
        }

        public UpdateQueryForm(string newGameVersion, int updateSizeInKb)
        {
            InitializeComponent();
            this.BackgroundImage = SharedUILogic.LoadImage("updatequerybg.png");
            this.Font = Utilities.GetFont(DomainController.Instance().GetCommonFont());
            this.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUILabelColor());
            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainclienticon.ico");
            lblUpdateInfo.Text = String.Format(lblUpdateInfo.Text, newGameVersion);
            lblUpdateSize.Text = String.Format(lblUpdateSize.Text, Math.Round(updateSizeInKb / 1024.0));
            SoundPlayer sPlayer = new SoundPlayer(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "button.wav");

            btnAccept.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltColor());
            btnDeny.ForeColor = btnAccept.ForeColor;
            btnAccept.DefaultImage = SharedUILogic.LoadImage("75pxbtn.png");
            btnAccept.HoveredImage = SharedUILogic.LoadImage("75pxbtn_c.png");
            btnAccept.HoverSound = sPlayer;
            btnDeny.DefaultImage = btnAccept.DefaultImage;
            btnDeny.HoveredImage = btnAccept.HoveredImage;
            btnDeny.HoverSound = btnAccept.HoverSound;

            NewGameVersion = newGameVersion;

            SharedUILogic.ParseClientThemeIni(this);
        }

        private void changelogLL_MouseClick(object sender, MouseEventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(ShowChangelog));
            thread.Start();
        }

        private void changelogLL_MouseEnter(object sender, EventArgs e)
        {
            changelogLL.LinkColor = Color.Purple;
            changelogLL.VisitedLinkColor = Color.Red;
        }

        private void changelogLL_MouseLeave(object sender, EventArgs e)
        {
            changelogLL.LinkColor = Color.Goldenrod;
        }

        private void ShowChangelog()
        {
            Process.Start(MainClientConstants.CHANGELOG_URL + '#' + NewGameVersion);
        }
    }
}
