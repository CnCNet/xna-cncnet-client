using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using Updater;

namespace DTAConfig.OptionPanels
{
    class UpdaterOptionsPanel : XNAOptionsPanel
    {
        public UpdaterOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        private XNAListBox lbUpdateServerList;
        private XNAClientCheckBox chkAutoCheck;

        public override void Initialize()
        {
            base.Initialize();

            Name = "UpdaterOptionsPanel";

            var lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblDescription.Text = "To change download server priority, select a server from the list and" +
                Environment.NewLine + "use the Move Up / Down buttons to change its priority.";

            lbUpdateServerList = new XNAListBox(WindowManager);
            lbUpdateServerList.Name = "lblUpdateServerList";
            lbUpdateServerList.ClientRectangle = new Rectangle(lblDescription.X,
                lblDescription.Bottom + 12, Width - 24, 100);
            lbUpdateServerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            lbUpdateServerList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            var btnMoveUp = new XNAClientButton(WindowManager);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.ClientRectangle = new Rectangle(lbUpdateServerList.X,
                lbUpdateServerList.Bottom + 12, 133, 23);
            btnMoveUp.Text = "Move Up";
            btnMoveUp.LeftClick += btnMoveUp_LeftClick;

            var btnMoveDown = new XNAClientButton(WindowManager);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.ClientRectangle = new Rectangle(
                lbUpdateServerList.Right - 133,
                btnMoveUp.Y, 133, 23);
            btnMoveDown.Text = "Move Down";
            btnMoveDown.LeftClick += btnMoveDown_LeftClick;

            chkAutoCheck = new XNAClientCheckBox(WindowManager);
            chkAutoCheck.Name = "chkAutoCheck";
            chkAutoCheck.ClientRectangle = new Rectangle(lblDescription.X,
                btnMoveUp.Bottom + 24, 0, 0);
            chkAutoCheck.Text = "Check for updates automatically";

            AddChild(lblDescription);
            AddChild(lbUpdateServerList);
            AddChild(btnMoveUp);
            AddChild(btnMoveDown);
            AddChild(chkAutoCheck);
        }

        private void btnMoveUp_LeftClick(object sender, EventArgs e)
        {
            int selectedIndex = lbUpdateServerList.SelectedIndex;

            if (selectedIndex < 1)
                return;

            var tmp = lbUpdateServerList.Items[selectedIndex - 1];
            lbUpdateServerList.Items[selectedIndex - 1] = lbUpdateServerList.Items[selectedIndex];
            lbUpdateServerList.Items[selectedIndex] = tmp;

            lbUpdateServerList.SelectedIndex--;

            UpdateMirror umtmp = CUpdater.UPDATEMIRRORS[selectedIndex - 1];
            CUpdater.UPDATEMIRRORS[selectedIndex - 1] = CUpdater.UPDATEMIRRORS[selectedIndex];
            CUpdater.UPDATEMIRRORS[selectedIndex] = umtmp;
        }

        private void btnMoveDown_LeftClick(object sender, EventArgs e)
        {
            int selectedIndex = lbUpdateServerList.SelectedIndex;

            if (selectedIndex > lbUpdateServerList.Items.Count - 2 || selectedIndex < 0)
                return;

            var tmp = lbUpdateServerList.Items[selectedIndex + 1];
            lbUpdateServerList.Items[selectedIndex + 1] = lbUpdateServerList.Items[selectedIndex];
            lbUpdateServerList.Items[selectedIndex] = tmp;

            lbUpdateServerList.SelectedIndex++;

            UpdateMirror umtmp = CUpdater.UPDATEMIRRORS[selectedIndex + 1];
            CUpdater.UPDATEMIRRORS[selectedIndex + 1] = CUpdater.UPDATEMIRRORS[selectedIndex];
            CUpdater.UPDATEMIRRORS[selectedIndex] = umtmp;
        }

        public override void Load()
        {
            base.Load();

            lbUpdateServerList.Clear();

            foreach (var updaterMirror in CUpdater.UPDATEMIRRORS)
                lbUpdateServerList.AddItem(updaterMirror.Name + " (" + updaterMirror.Location + ")");

            chkAutoCheck.Checked = IniSettings.CheckForUpdates;
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.CheckForUpdates.Value = chkAutoCheck.Checked;

            IniSettings.SettingsIni.EraseSectionKeys("DownloadMirrors");

            int id = 0;

            foreach (UpdateMirror um in CUpdater.UPDATEMIRRORS)
            {
                IniSettings.SettingsIni.SetStringValue("DownloadMirrors", id.ToString(), um.Name);
                id++;
            }

            return restartRequired;
        }
    }
}
