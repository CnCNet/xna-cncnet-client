using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Updater;

namespace DTAConfig.OptionPanels
{
    class ComponentsPanel : XNAOptionsPanel
    {
        public ComponentsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        List<XNAClientButton> installationButtons = new List<XNAClientButton>();

        bool downloadCancelled = false;

        public override void Initialize()
        {
            base.Initialize();

            Name = "ComponentsPanel";

            int componentIndex = 0;

            if (CUpdater.CustomComponents == null)
                return;

            foreach (CustomComponent c in CUpdater.CustomComponents)
            {
                string buttonText = "Not Available";

                if (File.Exists(ProgramConstants.GamePath + c.LocalPath))
                {
                    buttonText = "Uninstall";

                    if (c.LocalIdentifier != c.RemoteIdentifier)
                        buttonText = "Update";
                }
                else
                {
                    if (!string.IsNullOrEmpty(c.RemoteIdentifier))
                    {
                        buttonText = "Install";
                    }
                }

                var btn = new XNAClientButton(WindowManager);
                btn.Name = "btn" + c.ININame;
                btn.ClientRectangle = new Rectangle(Width - 145,
                    12 + componentIndex * 35, 133, 23);
                btn.Text = buttonText;
                btn.Tag = c;
                btn.LeftClick += Btn_LeftClick;

                var lbl = new XNALabel(WindowManager);
                lbl.Name = "lbl" + c.ININame;
                lbl.ClientRectangle = new Rectangle(12, btn.Y + 2, 0, 0);
                lbl.Text = c.GUIName;

                AddChild(btn);
                AddChild(lbl);

                installationButtons.Add(btn);

                componentIndex++;
            }
        }

        public override void Load()
        {
            base.Load();

            int componentIndex = 0;
            bool buttonEnabled = false;

            if (CUpdater.CustomComponents == null)
                return;

            foreach (CustomComponent c in CUpdater.CustomComponents)
            {
                string buttonText = "Not Available";

                if (File.Exists(ProgramConstants.GamePath + c.LocalPath))
                {
                    buttonText = "Uninstall";
                    buttonEnabled = true;

                    if (c.LocalIdentifier != c.RemoteIdentifier)
                        buttonText = "Update (" + GetSizeString(c.RemoteSize) + ")";
                }
                else
                {
                    if (!string.IsNullOrEmpty(c.RemoteIdentifier))
                    {
                        buttonText = "Install (" + GetSizeString(c.RemoteSize) + ")";
                        buttonEnabled = true;
                    }
                }

                installationButtons[componentIndex].Text = buttonText;
                installationButtons[componentIndex].AllowClick = buttonEnabled;

                componentIndex++;
            }
        }

        public override bool Save()
        {
            return base.Save();
        }

        private void Btn_LeftClick(object sender, EventArgs e)
        {
            var btn = (XNAClientButton)sender;

            var cc = (CustomComponent)btn.Tag;

            if (cc.IsBeingDownloaded)
                return;

            if (File.Exists(ProgramConstants.GamePath + cc.LocalPath))
            {
                if (cc.LocalIdentifier == cc.RemoteIdentifier)
                {
                    File.Delete(ProgramConstants.GamePath + cc.LocalPath);
                    btn.Text = "Install";
                    return;
                }

                btn.AllowClick = false;

                cc.DownloadFinished += cc_DownloadFinished;
                cc.DownloadProgressChanged += cc_DownloadProgressChanged;
                Thread thread = new Thread(cc.DownloadComponent);
                thread.Start();
            }
            else
            {
                var msgBox = new XNAMessageBox(WindowManager, "Confirmation Required",
                    "To enable " + cc.GUIName + " the Client will download the necessary files to your game directory." +
                    Environment.NewLine + Environment.NewLine +
                    "This will take an additional " + GetSizeString(cc.RemoteSize) + " of disk space, and the download may last" +
                    Environment.NewLine +
                    "from a few minutes to multiple hours depending on your Internet connection speed." +
                    Environment.NewLine + Environment.NewLine +
                    "You will not be able to play during the download. Do you want to continue?", XNAMessageBoxButtons.YesNo);
                msgBox.Tag = btn;

                msgBox.Show();
                msgBox.YesClickedAction = MsgBox_YesClicked;
            }
        }

        private void MsgBox_YesClicked(XNAMessageBox messageBox)
        {
            var btn = (XNAClientButton)messageBox.Tag;
            btn.AllowClick = false;

            var cc = (CustomComponent)btn.Tag;

            cc.DownloadFinished += cc_DownloadFinished;
            cc.DownloadProgressChanged += cc_DownloadProgressChanged;
            Thread thread = new Thread(cc.DownloadComponent);
            thread.Start();
        }

        public void InstallComponent(int id)
        {
            var btn = installationButtons[id];
            btn.AllowClick = false;

            var cc = (CustomComponent)btn.Tag;

            cc.DownloadFinished += cc_DownloadFinished;
            cc.DownloadProgressChanged += cc_DownloadProgressChanged;
            Thread thread = new Thread(cc.DownloadComponent);
            thread.Start();
        }

        /// <summary>
        /// Called whenever a custom component download's progress is changed.
        /// </summary>
        /// <param name="c">The CustomComponent object.</param>
        /// <param name="percentage">The current download progress percentage.</param>
        private void cc_DownloadProgressChanged(CustomComponent c, int percentage)
        {
            WindowManager.AddCallback(new Action<CustomComponent, int>(HandleDownloadProgressChanged), c, percentage);
        }

        private void HandleDownloadProgressChanged(CustomComponent cc, int percentage)
        {
            percentage = Math.Min(percentage, 100);

            var btn = installationButtons.Find(b => object.ReferenceEquals(b.Tag, cc));
            btn.Text = "Downloading.. " + percentage + "%";
        }

        /// <summary>
        /// Called whenever a custom component download is finished.
        /// </summary>
        /// <param name="c">The CustomComponent object.</param>
        /// <param name="success">True if the download succeeded, otherwise false.</param>
        private void cc_DownloadFinished(CustomComponent c, bool success)
        {
            WindowManager.AddCallback(new Action<CustomComponent, bool>(HandleDownloadFinished), c, success);
        }

        private void HandleDownloadFinished(CustomComponent cc, bool success)
        {
            cc.DownloadFinished -= cc_DownloadFinished;
            cc.DownloadProgressChanged -= cc_DownloadProgressChanged;

            var btn = installationButtons.Find(b => object.ReferenceEquals(b.Tag, cc));
            btn.AllowClick = true;

            if (!success)
            {
                if (!downloadCancelled)
                {
                    XNAMessageBox.Show(WindowManager, "Optional Component Download Failed",
                        string.Format("Download of optional component {0} failed." + Environment.NewLine +
                        "See client.log for details." + Environment.NewLine + Environment.NewLine +
                        "If this problem continues, please contact your mod's authors for support.",
                        cc.GUIName));
                }

                btn.Text = "Install (" + GetSizeString(cc.RemoteSize) + ")";

                if (File.Exists(ProgramConstants.GamePath + cc.LocalPath))
                    btn.Text = "Update (" + GetSizeString(cc.RemoteSize) + ")";
            }
            else
            {
                XNAMessageBox.Show(WindowManager, "Download Completed",
                    string.Format("Download of optional component {0} completed succesfully.", cc.GUIName));
                btn.Text = "Uninstall";
            }
        }

        public void CancelAllDownloads()
        {
            Logger.Log("Cancelling all downloads.");

            downloadCancelled = true;

            if (CUpdater.CustomComponents == null)
                return;

            foreach (CustomComponent cc in CUpdater.CustomComponents)
            {
                if (cc.IsBeingDownloaded)
                    cc.StopDownload();
            }
        }

        public void Open()
        {
            downloadCancelled = false;
        }

        private string GetSizeString(long size)
        {
            if (size < 1048576)
            {
                return (size / 1024) + " KB";
            }
            else
            {
                return (size / 1048576) + " MB";
            }
        }
    }
}
