using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientCore;
using Rampastring.XNAUI;
using Updater;
using ClientGUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using System.IO;
using System.Threading;
using Rampastring.Tools;

namespace DTAConfig
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
                btn.ClientRectangle = new Rectangle(ClientRectangle.Width - 145,
                    12 + componentIndex * 35, 133, 23);
                btn.Text = buttonText;
                btn.Tag = c;
                btn.LeftClick += Btn_LeftClick;

                var lbl = new XNALabel(WindowManager);
                lbl.Name = "lbl" + c.ININame;
                lbl.ClientRectangle = new Rectangle(12, btn.ClientRectangle.Y + 2, 0, 0);
                lbl.Text = c.GUIName;

                AddChild(btn);
                AddChild(lbl);

                installationButtons.Add(btn);

                componentIndex++;
            }
        }

        public override void Load()
        {
            int componentIndex = 0;
            bool buttonEnabled = false;

            foreach (CustomComponent c in CUpdater.CustomComponents)
            {
                string buttonText = "Not Available";

                if (File.Exists(ProgramConstants.GamePath + c.LocalPath))
                {
                    buttonText = "Uninstall";
                    buttonEnabled = true;

                    if (c.LocalIdentifier != c.RemoteIdentifier)
                        buttonText = "Update";
                }
                else
                {
                    if (!string.IsNullOrEmpty(c.RemoteIdentifier))
                    {
                        buttonText = "Install";
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
            return false;
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
            }
            else
            {
                var msgBox = new XNAMessageBox(WindowManager, "Confirmation Required",
                    "To enable " + cc.GUIName + " the Client will download the necessary files to your game directory." +
                    Environment.NewLine + Environment.NewLine +
                    "This will take an additional " + cc.RemoteSize / 1048576 + " MB of disk space, and the download may last" +
                    Environment.NewLine +
                    "from a few minutes to multiple hours depending on your Internet connection speed." +
                    Environment.NewLine + Environment.NewLine +
                    "You will not be able to play during the download. Do you want to continue?", DXMessageBoxButtons.YesNo);
                msgBox.Tag = btn;

                msgBox.Show();
                msgBox.YesClicked += MsgBox_YesClicked;
                msgBox.NoClicked += MsgBox_NoClicked;
            }
        }

        private void MsgBox_NoClicked(object sender, EventArgs e)
        {
            var msgBox = (XNAMessageBox)sender;
            msgBox.YesClicked -= MsgBox_YesClicked;
            msgBox.NoClicked -= MsgBox_NoClicked;
        }

        private void MsgBox_YesClicked(object sender, EventArgs e)
        {
            var msgBox = (XNAMessageBox)sender;
            msgBox.YesClicked -= MsgBox_YesClicked;
            msgBox.NoClicked -= MsgBox_NoClicked;

            var btn = (XNAClientButton)msgBox.Tag;
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

                btn.Text = "Install";

                if (File.Exists(ProgramConstants.GamePath + cc.LocalPath))
                    btn.Text = "Update";
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
    }
}
