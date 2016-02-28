using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Media;
using System.Runtime.InteropServices;
using dtasetup.domain;
using Updater;
using ClientCore;
using ClientGUI;

namespace dtasetup.gui
{
    public partial class UpdateForm : MovableForm
    {
        public UpdateForm()
        {
            InitializeComponent();
            lblUpdateDescription.Text = string.Format(lblUpdateDescription.Text, MainClientConstants.GAME_NAME_LONG, CUpdater.ServerGameVersion);
        }

        private delegate void UpdateProgressChangedCallback(string currFileName, int currFilePercentage, int totalPercentage);
        private delegate void NoParamCallback();

        TaskbarProgress tbp;

        private void UpdateForm_Load(object sender, EventArgs e)
        {
            Logger.Log("Opening update display.");

            this.Text = MainClientConstants.GAME_NAME_SHORT + " Updater";
            this.BackgroundImage = SharedUILogic.LoadImage("updaterbg.png");
            this.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUILabelColor());
            this.Font = Utilities.GetFont(DomainController.Instance().GetCommonFont());

            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainclienticon.ico");

            btnCancel.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltColor());
            btnCancel.DefaultImage = SharedUILogic.LoadImage("133pxbtn.png");
            btnCancel.HoveredImage = SharedUILogic.LoadImage("133pxbtn_c.png");
            btnCancel.HoverSound = new SoundPlayer(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "button.wav");

            SharedUILogic.ParseClientThemeIni(this);

            if (MainClientConstants.OSId == OSVersion.WIN7 || MainClientConstants.OSId == OSVersion.WIN8)
            {
                tbp = new TaskbarProgress();
            }

            CUpdater.OnUpdateCompleted += Updater_OnUpdateCompleted;
            CUpdater.OnUpdateFailed += Updater_OnUpdateFailed;
            CUpdater.BeforeSelfUpdate += Updater_BeforeSelfUpdate;
            CUpdater.UpdateProgressChanged += Updater_UpdateProgressChanged;
        }

        void Updater_UpdateProgressChanged(string currFileName, int currFilePercentage, int totalPercentage)
        {
            if (this.InvokeRequired)
            {
                UpdateProgressChangedCallback d = new UpdateProgressChangedCallback(Updater_UpdateProgressChanged);
                this.BeginInvoke(d, new object[] { currFileName, currFilePercentage, totalPercentage });
                return;
            }

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

            if (MainClientConstants.OSId == OSVersion.WIN7 || MainClientConstants.OSId == OSVersion.WIN8)
            {
                if (tbp != null)
                {
                    tbp.SetState(this.Handle, TaskbarProgress.TaskbarStates.Normal);
                    tbp.SetValue(this.Handle, progressBar2.Value, progressBar2.Maximum);
                }
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
                tbp.SetState(this.Handle, TaskbarProgress.TaskbarStates.Paused);
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
                Unsubscribe();
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
        }

        void Updater_OnUpdateCompleted()
        {
            if (this.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(Updater_OnUpdateCompleted);
                this.BeginInvoke(d, null);
                return;
            }

            Unsubscribe();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult dr = new MsgBoxForm("Cancelling the update while files are being downloaded might corrupt the installation."
                + Environment.NewLine + "To fix your " + MainClientConstants.GAME_NAME_SHORT + " installation, you might be forced to re-update later."
                + Environment.NewLine + Environment.NewLine + "Are you sure you want to cancel the update?", "Update in progress", MessageBoxButtons.YesNo).ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                CUpdater.TerminateUpdate = true;

                Unsubscribe();

                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            }
        }

        private void Unsubscribe()
        {
            CUpdater.OnUpdateCompleted -= Updater_OnUpdateCompleted;
            CUpdater.OnUpdateFailed -= Updater_OnUpdateFailed;
            CUpdater.BeforeSelfUpdate -= Updater_BeforeSelfUpdate;
            CUpdater.UpdateProgressChanged -= Updater_UpdateProgressChanged;
        }
    }


    /// <summary>
    /// For utilizing the taskbar progress bar introduced in Windows 7:
    /// http://stackoverflow.com/questions/1295890/windows-7-progress-bar-in-taskbar-in-c
    /// </summary>
    public class TaskbarProgress
    {
        public enum TaskbarStates
        {
            NoProgress = 0,
            Indeterminate = 0x1,
            Normal = 0x2,
            Error = 0x4,
            Paused = 0x8
        }

        [ComImportAttribute()]
        [GuidAttribute("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            // ITaskbarList
            [PreserveSig]
            void HrInit();
            [PreserveSig]
            void AddTab(IntPtr hwnd);
            [PreserveSig]
            void DeleteTab(IntPtr hwnd);
            [PreserveSig]
            void ActivateTab(IntPtr hwnd);
            [PreserveSig]
            void SetActiveAlt(IntPtr hwnd);

            // ITaskbarList2
            [PreserveSig]
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

            // ITaskbarList3
            [PreserveSig]
            void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
            [PreserveSig]
            void SetProgressState(IntPtr hwnd, TaskbarStates state);
        }

        [GuidAttribute("56FDF344-FD6D-11d0-958A-006097C9A090")]
        [ClassInterfaceAttribute(ClassInterfaceType.None)]
        [ComImportAttribute()]
        private class TaskbarInstance
        {
        }

        private ITaskbarList3 taskbarInstance = (ITaskbarList3)new TaskbarInstance();
        private bool taskbarSupported = Environment.OSVersion.Version >= new Version(6, 1);

        public void SetState(IntPtr windowHandle, TaskbarStates taskbarState)
        {
            if (taskbarSupported) taskbarInstance.SetProgressState(windowHandle, taskbarState);
        }

        public void SetValue(IntPtr windowHandle, double progressValue, double progressMax)
        {
            if (taskbarSupported) taskbarInstance.SetProgressValue(windowHandle, (ulong)progressValue, (ulong)progressMax);
        }
    }
}
