using ClientGUI;
using DTAClient.Domain;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Runtime.InteropServices;
using Updater;

namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// The update window, displaying the update progress to the user.
    /// </summary>
    public class UpdateWindow : XNAWindow
    {
        public delegate void UpdateCancelEventHandler(object sender, EventArgs e);
        public event UpdateCancelEventHandler UpdateCancelled;

        public delegate void UpdateCompletedEventHandler(object sender, EventArgs e);
        public event UpdateCompletedEventHandler UpdateCompleted;

        public delegate void UpdateFailureEventHandler(object sender, UpdateFailureEventArgs e);
        public event UpdateFailureEventHandler UpdateFailed;

        delegate void UpdateProgressChangedDelegate(string fileName, int filePercentage, int totalPercentage);

        private const double DOT_TIME = 0.66;
        private const int MAX_DOTS = 5;

        public UpdateWindow(WindowManager windowManager) : base(windowManager)
        {

        }

        private XNALabel lblDescription;
        private XNALabel lblCurrentFileProgressPercentageValue;
        private XNALabel lblTotalProgressPercentageValue;
        private XNALabel lblCurrentFile;
        private XNALabel lblUpdaterStatus;

        private XNAProgressBar prgCurrentFile;
        private XNAProgressBar prgTotal;

        private TaskbarProgress tbp;

        private bool isStartingForceUpdate;

        bool infoUpdated = false;

        string currFileName = string.Empty;
        int currFilePercentage = 0;
        int totalPercentage = 0;
        int dotCount = 0;
        double currentDotTime = 0.0;

        private static readonly object locker = new object();

        public override void Initialize()
        {
            Name = "UpdateWindow";
            ClientRectangle = new Rectangle(0, 0, 446, 270);
            BackgroundTexture = AssetLoader.LoadTexture("updaterbg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Text = string.Empty;
            lblDescription.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblDescription.Name = "lblDescription";

            var lblCurrentFileProgressPercentage = new XNALabel(WindowManager);
            lblCurrentFileProgressPercentage.Text = "Progress percentage of current file:";
            lblCurrentFileProgressPercentage.ClientRectangle = new Rectangle(12, 90, 0, 0);
            lblCurrentFileProgressPercentage.Name = "lblCurrentFileProgressPercentage";

            lblCurrentFileProgressPercentageValue = new XNALabel(WindowManager);
            lblCurrentFileProgressPercentageValue.Text = "0%";
            lblCurrentFileProgressPercentageValue.ClientRectangle = new Rectangle(409, lblCurrentFileProgressPercentage.Y, 0, 0);
            lblCurrentFileProgressPercentageValue.Name = "lblCurrentFileProgressPercentageValue";

            prgCurrentFile = new XNAProgressBar(WindowManager);
            prgCurrentFile.Name = "prgCurrentFile";
            prgCurrentFile.Maximum = 100;
            prgCurrentFile.ClientRectangle = new Rectangle(12, 110, 422, 30);
            //prgCurrentFile.BorderColor = UISettings.WindowBorderColor;
            prgCurrentFile.SmoothForwardTransition = true;
            prgCurrentFile.SmoothTransitionRate = 10;

            lblCurrentFile = new XNALabel(WindowManager);
            lblCurrentFile.Name = "lblCurrentFile";
            lblCurrentFile.ClientRectangle = new Rectangle(12, 142, 0, 0);

            var lblTotalProgressPercentage = new XNALabel(WindowManager);
            lblTotalProgressPercentage.Text = "Total progress percentage:";
            lblTotalProgressPercentage.ClientRectangle = new Rectangle(12, 170, 0, 0);
            lblTotalProgressPercentage.Name = "lblTotalProgressPercentage";

            lblTotalProgressPercentageValue = new XNALabel(WindowManager);
            lblTotalProgressPercentageValue.Text = "0%";
            lblTotalProgressPercentageValue.ClientRectangle = new Rectangle(409, lblTotalProgressPercentage.Y, 0, 0);
            lblTotalProgressPercentageValue.Name = "lblTotalProgressPercentageValue";

            prgTotal = new XNAProgressBar(WindowManager);
            prgTotal.Name = "prgTotal";
            prgTotal.Maximum = 100;
            prgTotal.ClientRectangle = new Rectangle(12, 190, prgCurrentFile.Width, prgCurrentFile.Height);
            //prgTotal.BorderColor = UISettings.WindowBorderColor;

            lblUpdaterStatus = new XNALabel(WindowManager);
            lblUpdaterStatus.Name = "lblUpdaterStatus";
            lblUpdaterStatus.Text = "Preparing";
            lblUpdaterStatus.ClientRectangle = new Rectangle(12, 240, 0, 0);

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.ClientRectangle = new Rectangle(301, 240, 133, 23);
            btnCancel.Text = "Cancel";
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

            CUpdater.FileIdentifiersUpdated += CUpdater_FileIdentifiersUpdated;
            CUpdater.OnUpdateCompleted += Updater_OnUpdateCompleted;
            CUpdater.OnUpdateFailed += Updater_OnUpdateFailed;
            CUpdater.UpdateProgressChanged += Updater_UpdateProgressChanged;
            CUpdater.LocalFileCheckProgressChanged += CUpdater_LocalFileCheckProgressChanged;

            if (IsTaskbarSupported())
                tbp = new TaskbarProgress();
        }

        private void CUpdater_FileIdentifiersUpdated()
        {
            if (!isStartingForceUpdate)
                return;

            if (CUpdater.DTAVersionState == VersionState.UNKNOWN)
            {
                XNAMessageBox.Show(WindowManager, "Force Update Failure", "Checking for updates failed.");
                CloseWindow();
                return;
            }

            SetData(CUpdater.ServerGameVersion);
            CUpdater.StartAsyncUpdate();
            isStartingForceUpdate = false;
        }

        private void CUpdater_LocalFileCheckProgressChanged(int checkedFileCount, int totalFileCount)
        {
            AddCallback(new Action<int>(UpdateFileProgress),
                (checkedFileCount * 100 / totalFileCount));
        }

        private void UpdateFileProgress(int value)
        {
            prgCurrentFile.Value = value;
            lblCurrentFileProgressPercentageValue.Text = value + "%";
        }

        private void Updater_UpdateProgressChanged(string currFileName, int currFilePercentage, int totalPercentage)
        {
            lock (locker)
            {
                infoUpdated = true;
                this.currFileName = currFileName;
                this.currFilePercentage = currFilePercentage;
                this.totalPercentage = totalPercentage;
            }
        }

        private void HandleUpdateProgressChange()
        {
            if (!infoUpdated)
                return;

            infoUpdated = false;

            if (currFilePercentage < 0 || currFilePercentage > prgCurrentFile.Maximum)
                prgCurrentFile.Value = 0;
            else
                prgCurrentFile.Value = currFilePercentage;

            if (totalPercentage < 0 || totalPercentage > prgTotal.Maximum)
                prgTotal.Value = 0;
            else
                prgTotal.Value = totalPercentage;

            lblCurrentFileProgressPercentageValue.Text = prgCurrentFile.Value.ToString() + "%";
            lblTotalProgressPercentageValue.Text = prgTotal.Value.ToString() + "%";
            lblCurrentFile.Text = "Current file: " + currFileName;
            lblUpdaterStatus.Text = "Downloading files";

            /*/ TODO Improve the updater
             * When the updater thread in DTAUpdater.dll has completed the update, it will
             * restart the client right away without giving the UI thread a chance to
             * finish its tasks and free resources in a proper way.
             * Because of that, this function is sometimes executed when
             * the game window has already been hidden / removed, and the code below
             * will then crash the client, causing the user to see a KABOOM message
             * along with the succesful update, which is likely quite confusing for the user.
             * The try-catch is a dirty temporary workaround.
             * /*/
            try
            {
                if (IsTaskbarSupported())
                {
                    tbp.SetState(WindowManager.GetWindowHandle(), TaskbarProgress.TaskbarStates.Normal);
                    tbp.SetValue(WindowManager.GetWindowHandle(), prgTotal.Value, prgTotal.Maximum);
                }
            }
            catch
            {

            }
        }

        private void Updater_OnUpdateCompleted()
        {
            AddCallback(new Action(HandleUpdateCompleted), null);
        }

        private void HandleUpdateCompleted()
        {
            if (IsTaskbarSupported())
                tbp.SetState(WindowManager.GetWindowHandle(), TaskbarProgress.TaskbarStates.NoProgress);

            UpdateCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void Updater_OnUpdateFailed(Exception ex)
        {
            AddCallback(new Action<string>(HandleUpdateFailed), ex.Message);
        }

        private void HandleUpdateFailed(string updateFailureErrorMessage)
        {
            if (IsTaskbarSupported())
                tbp.SetState(WindowManager.GetWindowHandle(), TaskbarProgress.TaskbarStates.NoProgress);

            UpdateFailed?.Invoke(this, new UpdateFailureEventArgs(updateFailureErrorMessage));
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            if (!isStartingForceUpdate)
                CUpdater.TerminateUpdate = true;

            CloseWindow();
        }

        private void CloseWindow()
        {
            isStartingForceUpdate = false;

            if (IsTaskbarSupported())
                tbp.SetState(WindowManager.GetWindowHandle(), TaskbarProgress.TaskbarStates.NoProgress);

            UpdateCancelled?.Invoke(this, EventArgs.Empty);
        }

        public void SetData(string newGameVersion)
        {
            lblDescription.Text = String.Format("Please wait while {0} is updated to version {1}." + Environment.NewLine +
                "This window will automatically close once the update is complete." + Environment.NewLine + Environment.NewLine +
                "The client may also restart after the update has been downloaded.", MainClientConstants.GAME_NAME_SHORT, newGameVersion);
            lblUpdaterStatus.Text = "Preparing";
        }

        public void ForceUpdate()
        {
            isStartingForceUpdate = true;
            lblDescription.Text = $"Force updating {MainClientConstants.GAME_NAME_SHORT} to latest version...";
            lblUpdaterStatus.Text = "Connecting";
            CUpdater.CheckForUpdates();
        }

        private bool IsTaskbarSupported()
        {
            return MainClientConstants.OSId == OSVersion.WIN7 || MainClientConstants.OSId == OSVersion.WIN810;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            lock (locker)
            {
                HandleUpdateProgressChange();
            }

            currentDotTime += gameTime.ElapsedGameTime.TotalSeconds;
            if (currentDotTime > DOT_TIME)
            {
                currentDotTime = 0.0;
                dotCount++;
                if (dotCount > MAX_DOTS)
                    dotCount = 0;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            float xOffset = 3.0f;

            for (int i = 0; i < dotCount; i++)
            {
                var wrect = lblUpdaterStatus.RenderRectangle();
                Renderer.DrawStringWithShadow(".", lblUpdaterStatus.FontIndex,
                    new Vector2(wrect.Right + xOffset, wrect.Bottom - 15.0f), lblUpdaterStatus.TextColor);
                xOffset += 3.0f;
            }
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
