using ClientCore.Extensions;
using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using ClientUpdater;

namespace DTAClient.DXGUI.Generic.OptionPanels
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

            if (Updater.CustomComponents == null)
                return;

            foreach (CustomComponent c in Updater.CustomComponents)
            {
                string buttonText = "Not Available".L10N("Client:DTAConfig:NotAvailable");

                if (SafePath.GetFile(ProgramConstants.GamePath, c.LocalPath).Exists)
                {
                    buttonText = "Uninstall".L10N("Client:DTAConfig:Uninstall");

                    if (c.LocalIdentifier != c.RemoteIdentifier)
                        buttonText = "Update".L10N("Client:DTAConfig:Update");
                }
                else
                {
                    if (!string.IsNullOrEmpty(c.RemoteIdentifier))
                        buttonText = "Install".L10N("Client:DTAConfig:Install");
                }

                XNAClientButton btn = new(WindowManager)
                {
                    Name = "btn" + c.ININame,
                    ClientRectangle = new Rectangle(Width - 145,
                        12 + componentIndex * 35, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
                    Text = buttonText,
                    Tag = c
                };
                btn.LeftClick += Btn_LeftClick;

                XNALabel lbl = new(WindowManager)
                {
                    Name = "lbl" + c.ININame,
                    ClientRectangle = new Rectangle(12, btn.Y + 2, 0, 0),
                    Text = c.GUIName
                };

                AddChild(btn);
                AddChild(lbl);

                installationButtons.Add(btn);

                componentIndex++;
            }

            Updater.FileIdentifiersUpdated += Updater_FileIdentifiersUpdated;
        }

        private void Updater_FileIdentifiersUpdated()
            => UpdateInstallationButtons();

        public override void Load()
        {
            base.Load();

            UpdateInstallationButtons();
        }

        private void UpdateInstallationButtons()
        {
            if (Updater.CustomComponents == null)
                return;

            int componentIndex = 0;

            foreach (CustomComponent c in Updater.CustomComponents)
            {
                if (!c.Initialized || c.IsBeingDownloaded)
                {
                    installationButtons[componentIndex].AllowClick = false;
                    componentIndex++;
                    continue;
                }

                string buttonText = "Not Available".L10N("Client:DTAConfig:NotAvailable");
                bool buttonEnabled = false;

                if (SafePath.GetFile(ProgramConstants.GamePath, c.LocalPath).Exists)
                {
                    buttonText = "Uninstall".L10N("Client:DTAConfig:Uninstall");
                    buttonEnabled = true;

                    if (c.LocalIdentifier != c.RemoteIdentifier)
                        buttonText = "Update".L10N("Client:DTAConfig:Update") + $" ({GetSizeString(c.RemoteSize)})";
                }
                else
                {
                    if (!string.IsNullOrEmpty(c.RemoteIdentifier))
                    {
                        buttonText = "Install".L10N("Client:DTAConfig:Install") + $" ({GetSizeString(c.RemoteSize)})";
                        buttonEnabled = true;
                    }
                }

                installationButtons[componentIndex].Text = buttonText;
                installationButtons[componentIndex].AllowClick = buttonEnabled;

                componentIndex++;
            }
        }

        private void Btn_LeftClick(object sender, EventArgs e)
        {
            var btn = (XNAClientButton)sender;

            var cc = (CustomComponent)btn.Tag;

            if (cc.IsBeingDownloaded)
                return;

            FileInfo localFileInfo = SafePath.GetFile(ProgramConstants.GamePath, cc.LocalPath);

            if (localFileInfo.Exists)
            {
                if (cc.LocalIdentifier == cc.RemoteIdentifier)
                {
                    localFileInfo.IsReadOnly = false;
                    localFileInfo.Delete();
                    btn.Text = "Install".L10N("Client:DTAConfig:Install") + $" ({GetSizeString(cc.RemoteSize)})";
                    return;
                }

                btn.AllowClick = false;

                cc.DownloadFinished += cc_DownloadFinished;
                cc.DownloadProgressChanged += cc_DownloadProgressChanged;
                cc.DownloadComponent();
            }
            else
            {
                var msgBox = new XNAMessageBox(WindowManager, "Confirmation Required".L10N("Client:DTAConfig:UpdateConfirmRequiredTitle"),
                    string.Format(("To enable {0} the Client will need to download the necessary files to your game directory.\n\n" +
                        "This will take an additional {1} of disk space, and the download may take some time\n" +
                        "depending on your Internet connection speed. The size of the download is {2}.\n\n" +
                        "You will not be able to play during the download. Do you wish to continue?").L10N("Client:DTAConfig:UpdateConfirmRequiredText"),
                        cc.GUIName, GetSizeString(cc.RemoteSize), GetSizeString(cc.Archived ? cc.RemoteArchiveSize : cc.RemoteSize)),
                    XNAMessageBoxButtons.YesNo);

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
            cc.DownloadComponent();
        }

        public void InstallComponent(int id)
        {
            var btn = installationButtons[id];
            btn.AllowClick = false;

            var cc = (CustomComponent)btn.Tag;

            cc.DownloadFinished += cc_DownloadFinished;
            cc.DownloadProgressChanged += cc_DownloadProgressChanged;
            cc.DownloadComponent();
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

            if (cc.Archived && percentage == 100)
                btn.Text = "Unpacking...".L10N("Client:DTAConfig:Unpacking");
            else
                btn.Text = "Downloading...".L10N("Client:DTAConfig:Downloading") + " " + percentage + "%";
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
                    XNAMessageBox.Show(WindowManager, "Optional Component Download Failed".L10N("Client:DTAConfig:OptionalComponentDownloadFailedTitle"),
                        string.Format(("Download of optional component {0} failed.\n" +
                        "See client.log for details.\n\n" +
                        "If this problem continues, please contact your mod's authors for support.").L10N("Client:DTAConfig:OptionalComponentDownloadFailedText"),
                        cc.GUIName));
                }

                btn.Text = "Install".L10N("Client:DTAConfig:Install") + $" ({GetSizeString(cc.RemoteSize)})";

                if (SafePath.GetFile(ProgramConstants.GamePath, cc.LocalPath).Exists)
                    btn.Text = "Update".L10N("Client:DTAConfig:Update") + $" ({GetSizeString(cc.RemoteSize)})";
            }
            else
            {
                XNAMessageBox.Show(WindowManager, "Download Completed".L10N("Client:DTAConfig:DownloadCompleteTitle"),
                    string.Format("Download of optional component {0} completed succesfully.".L10N("Client:DTAConfig:DownloadCompleteText"), cc.GUIName));
                btn.Text = "Uninstall".L10N("Client:DTAConfig:Uninstall");
            }
        }

        public void CancelAllDownloads()
        {
            Logger.Log("Cancelling all custom component downloads.");

            downloadCancelled = true;

            if (Updater.CustomComponents == null)
                return;

            foreach (CustomComponent cc in Updater.CustomComponents)
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
