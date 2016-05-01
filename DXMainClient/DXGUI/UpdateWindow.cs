using ClientGUI;
using DTAClient.domain;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Updater;

namespace DTAClient.DXGUI
{
    /// <summary>
    /// The update window, displaying the update progress to the user.
    /// </summary>
    public class UpdateWindow : DXWindow
    {
        public delegate void UpdateCancelEventHandler(object sender, EventArgs e);
        public event UpdateCancelEventHandler UpdateCancelled;

        public delegate void UpdateCompletedEventHandler(object sender, EventArgs e);
        public event UpdateCompletedEventHandler UpdateCompleted;

        public delegate void UpdateFailureEventHandler(object sender, UpdateFailureEventArgs e);
        public event UpdateFailureEventHandler UpdateFailed;

        delegate void UpdateProgressChangedDelegate(string fileName, int filePercentage, int totalPercentage);

        public UpdateWindow(Game game, WindowManager windowManager) : base(game, windowManager)
        {

        }

        DXLabel lblDescription;
        DXLabel lblCurrentFileProgressPercentageValue;
        DXLabel lblTotalProgressPercentageValue;
        DXLabel lblCurrentFile;
        DXLabel lblUpdaterStatus;

        DXProgressBar prgCurrentFile;
        DXProgressBar prgTotal;

        TaskbarProgress tbp;

        private static readonly object locker = new object();

        public override void Initialize()
        {
            Name = "UpdateWindow";
            ClientRectangle = new Rectangle(0, 0, 446, 270);
            BackgroundTexture = AssetLoader.LoadTexture("updaterbg.png");

            lblDescription = new DXLabel(Game, WindowManager);
            lblDescription.Text = String.Empty;
            lblDescription.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblDescription.Name = "lblDescription";

            DXLabel lblCurrentFileProgressPercentage = new DXLabel(Game, WindowManager);
            lblCurrentFileProgressPercentage.Text = "Progress percentage of current file:";
            lblCurrentFileProgressPercentage.ClientRectangle = new Rectangle(12, 90, 0, 0);
            lblCurrentFileProgressPercentage.Name = "lblCurrentFileProgressPercentage";

            lblCurrentFileProgressPercentageValue = new DXLabel(Game, WindowManager);
            lblCurrentFileProgressPercentageValue.Text = "0%";
            lblCurrentFileProgressPercentageValue.ClientRectangle = new Rectangle(409, lblCurrentFileProgressPercentage.ClientRectangle.Y, 0, 0);
            lblCurrentFileProgressPercentageValue.Name = "lblCurrentFileProgressPercentageValue";

            prgCurrentFile = new DXProgressBar(Game, WindowManager);
            prgCurrentFile.Name = "prgCurrentFile";
            prgCurrentFile.Maximum = 100;
            prgCurrentFile.ClientRectangle = new Rectangle(12, 110, 422, 30);
            prgCurrentFile.BorderColor = UISettings.WindowBorderColor;
            prgCurrentFile.SmoothForwardTransition = true;
            prgCurrentFile.SmoothTransitionRate = 10;

            lblCurrentFile = new DXLabel(Game, WindowManager);
            lblCurrentFile.Name = "lblCurrentFile";
            lblCurrentFile.ClientRectangle = new Rectangle(12, 142, 0, 0);

            DXLabel lblTotalProgressPercentage = new DXLabel(Game, WindowManager);
            lblTotalProgressPercentage.Text = "Total progress percentage:";
            lblTotalProgressPercentage.ClientRectangle = new Rectangle(12, 170, 0, 0);
            lblTotalProgressPercentage.Name = "lblTotalProgressPercentage";

            lblTotalProgressPercentageValue = new DXLabel(Game, WindowManager);
            lblTotalProgressPercentageValue.Text = "0%";
            lblTotalProgressPercentageValue.ClientRectangle = new Rectangle(409, lblTotalProgressPercentage.ClientRectangle.Y, 0, 0);
            lblTotalProgressPercentageValue.Name = "lblTotalProgressPercentageValue";

            prgTotal = new DXProgressBar(Game, WindowManager);
            prgTotal.Name = "prgTotal";
            prgTotal.Maximum = 100;
            prgTotal.ClientRectangle = new Rectangle(12, 190, prgCurrentFile.ClientRectangle.Width, prgCurrentFile.ClientRectangle.Height);
            prgTotal.BorderColor = UISettings.WindowBorderColor;

            lblUpdaterStatus = new DXLabel(Game, WindowManager);
            lblUpdaterStatus.Name = "lblUpdaterStatus";
            lblUpdaterStatus.Text = "Preparing...";
            lblUpdaterStatus.ClientRectangle = new Rectangle(12, 240, 0, 0);

            DXButton btnCancel = new DXButton(Game, WindowManager);
            btnCancel.ClientRectangle = new Rectangle(301, 240, 133, 23);
            btnCancel.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnCancel.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnCancel.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnCancel.Text = "Cancel";
            btnCancel.FontIndex = 1;
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lblDescription);
            AddChild(lblCurrentFileProgressPercentage);
            AddChild(lblCurrentFileProgressPercentageValue);
            AddChild(prgCurrentFile);
            AddChild(lblCurrentFile);
            AddChild(lblTotalProgressPercentage);
            AddChild(lblTotalProgressPercentageValue);
            AddChild(prgTotal);
            AddChild(lblUpdaterStatus);
            AddChild(btnCancel);

            base.Initialize(); // Read theme settings from INI

            CenterOnParent();

            CUpdater.OnUpdateCompleted += Updater_OnUpdateCompleted;
            CUpdater.OnUpdateFailed += Updater_OnUpdateFailed;
            CUpdater.UpdateProgressChanged += Updater_UpdateProgressChanged;
            CUpdater.LocalFileCheckProgressChanged += CUpdater_LocalFileCheckProgressChanged;

            if (IsTaskbarSupported())
                tbp = new TaskbarProgress();
        }

        private void CUpdater_LocalFileCheckProgressChanged(int checkedFileCount, int totalFileCount)
        {
            AddCallback(new Delegates.IntDelegate(UpdateFileProgress),
                (checkedFileCount * 100 / totalFileCount));
        }

        private void UpdateFileProgress(int value)
        {
            prgCurrentFile.Value = value;
        }

        private void Updater_UpdateProgressChanged(string currFileName, int currFilePercentage, int totalPercentage)
        {
            AddCallback(new UpdateProgressChangedDelegate(HandleUpdateProgressChange),
                currFileName, currFilePercentage, totalPercentage);
        }

        private void HandleUpdateProgressChange(string fileName, int filePercentage, int totalPercentage)
        {
            if (filePercentage < 0 || filePercentage > prgCurrentFile.Maximum)
                prgCurrentFile.Value = 0;
            else
                prgCurrentFile.Value = filePercentage;

            if (totalPercentage < 0 || totalPercentage > prgTotal.Maximum)
                prgTotal.Value = 0;
            else
                prgTotal.Value = totalPercentage;

            lblCurrentFileProgressPercentageValue.Text = prgCurrentFile.Value.ToString() + "%";
            lblTotalProgressPercentageValue.Text = prgTotal.Value.ToString() + "%";
            lblCurrentFile.Text = "Current file: " + fileName;
            lblUpdaterStatus.Text = "Downloading files...";

            if (IsTaskbarSupported())
            {
                tbp.SetState(WindowManager.Instance.GetWindowHandle(), TaskbarProgress.TaskbarStates.Normal);
                tbp.SetValue(WindowManager.Instance.GetWindowHandle(), prgTotal.Value, prgTotal.Maximum);
            }
        }

        private void Updater_OnUpdateCompleted()
        {
            AddCallback(new Action(HandleUpdateCompleted), null);
        }

        private void HandleUpdateCompleted()
        {
            if (IsTaskbarSupported())
                tbp.SetState(WindowManager.Instance.GetWindowHandle(), TaskbarProgress.TaskbarStates.NoProgress);

            UpdateCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void Updater_OnUpdateFailed(Exception ex)
        {
            AddCallback(new Delegates.StringDelegate(HandleUpdateFailed), ex.Message);
        }

        private void HandleUpdateFailed(string updateFailureErrorMessage)
        {
            if (IsTaskbarSupported())
                tbp.SetState(WindowManager.Instance.GetWindowHandle(), TaskbarProgress.TaskbarStates.NoProgress);

            UpdateFailed?.Invoke(this, new UpdateFailureEventArgs(updateFailureErrorMessage));
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            CUpdater.TerminateUpdate = true;

            if (IsTaskbarSupported())
                tbp.SetState(WindowManager.Instance.GetWindowHandle(), TaskbarProgress.TaskbarStates.NoProgress);

            UpdateCancelled?.Invoke(this, EventArgs.Empty);
        }

        public void SetData(string newGameVersion)
        {
            lblDescription.Text = String.Format("Please wait while {0} is updated to version {1}." + Environment.NewLine +
                "This window will automatically close once the update is complete." + Environment.NewLine + Environment.NewLine +
                "The client may also restart after the update has been downloaded.", MainClientConstants.GAME_NAME_SHORT, newGameVersion);
            lblUpdaterStatus.Text = "Preparing...";
        }

        private bool IsTaskbarSupported()
        {
            return MainClientConstants.OSId == OSVersion.WIN7 || MainClientConstants.OSId == OSVersion.WIN810;
        }
    }

    public class UpdateFailureEventArgs : EventArgs
    {
        public UpdateFailureEventArgs(string reason)
        {
            this.reason = reason;
        }

        string reason = String.Empty;
        
        /// <summary>
        /// The returned error message from the update failure.
        /// </summary>
        public string Reason
        {
            get { return reason; }
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

        public void SetState(IntPtr windowHandle, TaskbarStates taskbarState)
        {
            taskbarInstance.SetProgressState(windowHandle, taskbarState);
        }

        public void SetValue(IntPtr windowHandle, double progressValue, double progressMax)
        {
            taskbarInstance.SetProgressValue(windowHandle, (ulong)progressValue, (ulong)progressMax);
        }
    }
}
