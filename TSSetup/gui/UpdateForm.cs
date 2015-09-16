using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Media;
using dtasetup.domain;
using Updater;
using ClientCore;
using ClientGUI;

namespace dtasetup.gui
{
    public partial class UpdateForm : Form
    {
        public UpdateForm()
        {
            InitializeComponent();
            lblUpdateDescription.Text = string.Format(lblUpdateDescription.Text, MainClientConstants.GAME_NAME_LONG, CUpdater.ServerGameVersion);
        }

        private delegate void UpdateProgressChangedCallback(string currFileName, int currFilePercentage, int totalPercentage);
        private delegate void NoParamCallback();

        private void UpdateForm_Load(object sender, EventArgs e)
        {
            Logger.Log("Opening update display.");

            this.Text = MainClientConstants.GAME_NAME_SHORT + " Updater";
            this.Icon = dtasetup.Properties.Resources.dtasetup_icon;
            this.BackgroundImage = SharedUILogic.LoadImage("updaterbg.png");
            this.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUILabelColor());
            this.Font = Utilities.GetFont(DomainController.Instance().GetCommonFont());

            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainclienticon.ico");

            btnCancel.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltColor());
            btnCancel.DefaultImage = SharedUILogic.LoadImage("133pxbtn.png");
            btnCancel.HoveredImage = SharedUILogic.LoadImage("133pxbtn_c.png");
            btnCancel.HoverSound = new SoundPlayer(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "button.wav");

            CUpdater.OnUpdateCompleted += Updater_OnUpdateCompleted;
            CUpdater.OnUpdateFailed += Updater_OnUpdateFailed;
            CUpdater.BeforeSelfUpdate += Updater_BeforeSelfUpdate;
            CUpdater.UpdateProgressChanged += Updater_UpdateProgressChanged;

            SharedUILogic.ParseClientThemeIni(this);
        }

        void Updater_UpdateProgressChanged(string currFileName, int currFilePercentage, int totalPercentage)
        {
            if (this.InvokeRequired)
            {
                UpdateProgressChangedCallback d = new UpdateProgressChangedCallback(Updater_UpdateProgressChanged);
                this.Invoke(d, new object[] { currFileName, currFilePercentage, totalPercentage });
            }
            else
            {
                if (currFilePercentage < 0 || currFilePercentage > 100)
                    progressBar1.Value = 0;
                else
                    progressBar1.Value = currFilePercentage;

                if (totalPercentage < 0 || totalPercentage > 100)
                    progressBar2.Value = 0;
                else
                    progressBar2.Value = totalPercentage;

                lblFileProgressValue.Text = Convert.ToString(currFilePercentage) + "%";
                lblTotalProgressValue.Text = Convert.ToString(totalPercentage) + "%";
                lblCurrFileName.Text = "Current file: " + currFileName;
            }
        }

        void Updater_BeforeSelfUpdate()
        {
            if (this.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(Updater_BeforeSelfUpdate);
                this.Invoke(d, null);
            }
            else
            {
                lblStatusText.Text = "Waiting for custom component downloads to finish...";
            }
        }

        void Updater_OnUpdateFailed(Exception ex)
        {
            if (this.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(Updater_OnUpdateCompleted);
                this.Invoke(d, null);
            }
            else
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
        }

        void Updater_OnUpdateCompleted()
        {
            if (this.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(Updater_OnUpdateCompleted);
                this.Invoke(d, null);
            }
            else
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult dr = new MsgBoxForm("Cancelling the update while files are being downloaded might corrupt the installation."
                + Environment.NewLine + "To fix your " + MainClientConstants.GAME_NAME_SHORT + " installation, you might be forced to re-update later."
                + Environment.NewLine + Environment.NewLine + "Are you sure you want to cancel the update?", "Update in progress", MessageBoxButtons.YesNo).ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                CUpdater.TerminateUpdate = true;

                CUpdater.OnUpdateCompleted -= Updater_OnUpdateCompleted;
                CUpdater.OnUpdateFailed -= Updater_OnUpdateFailed;
                CUpdater.BeforeSelfUpdate -= Updater_BeforeSelfUpdate;
                CUpdater.UpdateProgressChanged -= Updater_UpdateProgressChanged;

                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            }
        }

        private bool _Moving = false;
        private Point _Offset;

        private void UpdateForm_MouseDown(object sender, MouseEventArgs e)
        {
            _Moving = true;
            _Offset = new Point(e.X, e.Y);
        }

        private void UpdateForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (_Moving)
            {
                Point newlocation = this.Location;
                newlocation.X += e.X - _Offset.X;
                newlocation.Y += e.Y - _Offset.Y;
                this.Location = newlocation;
            }
        }

        private void UpdateForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (_Moving)
            {
                _Moving = false;
            }
        }
    }
}
