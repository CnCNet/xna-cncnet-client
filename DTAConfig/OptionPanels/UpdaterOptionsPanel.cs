using System;

using ClientCore.Extensions;
using ClientCore.Settings;

using ClientGUI;

using ClientUpdater;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels;

internal class UpdaterOptionsPanel : XNAOptionsPanel
{
    public UpdaterOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
        : base(windowManager, iniSettings)
    {
    }

    public event EventHandler OnForceUpdate;

    private XNAListBox lbUpdateServerList;
    private XNAClientCheckBox chkAutoCheck;
    private XNAClientButton btnForceUpdate;

    public override void Initialize()
    {
        base.Initialize();

        Name = "UpdaterOptionsPanel";

        XNALabel lblDescription = new(WindowManager)
        {
            Name = "lblDescription",
            ClientRectangle = new Rectangle(12, 12, 0, 0),
            Text = "To change download server priority, select a server from the list and\nuse the Move Up / Down buttons to change its priority.".L10N("Client:DTAConfig:ServerPriorityTip")
        };

        lbUpdateServerList = new XNAListBox(WindowManager)
        {
            Name = "lblUpdateServerList",
            ClientRectangle = new Rectangle(lblDescription.X,
            lblDescription.Bottom + 12, Width - 24, 100),
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2),
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED
        };

        XNAClientButton btnMoveUp = new(WindowManager)
        {
            Name = "btnMoveUp",
            ClientRectangle = new Rectangle(lbUpdateServerList.X,
            lbUpdateServerList.Bottom + 12, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Move Up".L10N("Client:DTAConfig:MoveUp")
        };
        btnMoveUp.LeftClick += btnMoveUp_LeftClick;

        XNAClientButton btnMoveDown = new(WindowManager)
        {
            Name = "btnMoveDown",
            ClientRectangle = new Rectangle(
            lbUpdateServerList.Right - UIDesignConstants.BUTTON_WIDTH_133,
            btnMoveUp.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Move Down".L10N("Client:DTAConfig:MoveDown")
        };
        btnMoveDown.LeftClick += btnMoveDown_LeftClick;

        chkAutoCheck = new XNAClientCheckBox(WindowManager)
        {
            Name = "chkAutoCheck",
            ClientRectangle = new Rectangle(lblDescription.X,
            btnMoveUp.Bottom + 24, 0, 0),
            Text = "Check for updates automatically".L10N("Client:DTAConfig:AutoCheckUpdate")
        };

        btnForceUpdate = new XNAClientButton(WindowManager)
        {
            Name = "btnForceUpdate",
            ClientRectangle = new Rectangle(btnMoveDown.X, btnMoveDown.Bottom + 24, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Force Update".L10N("Client:DTAConfig:ForceUpdate")
        };
        btnForceUpdate.LeftClick += BtnForceUpdate_LeftClick;

        AddChild(lblDescription);
        AddChild(lbUpdateServerList);
        AddChild(btnMoveUp);
        AddChild(btnMoveDown);
        AddChild(chkAutoCheck);
        AddChild(btnForceUpdate);
    }

    private void BtnForceUpdate_LeftClick(object sender, EventArgs e)
    {
        XNAMessageBox msgBox = new(WindowManager, "Force Update Confirmation".L10N("Client:DTAConfig:ForceUpdateConfirmTitle"),
                ("WARNING: Force update will result in files being re-verified\n" +
                "and re-downloaded. While this may fix problems with game\n" +
                "files, this also may delete some custom modifications\n" +
                "made to this installation. Use at your own risk!\n\n" +
                "If you proceed, the options window will close and the\n" +
                "client will proceed to checking for updates.\n\n" +
                "Do you really want to force update?").L10N("Client:DTAConfig:ForceUpdateConfirmText") + "\n", XNAMessageBoxButtons.YesNo);
        msgBox.Show();
        msgBox.YesClickedAction = ForceUpdateMsgBox_YesClicked;
    }

    private void ForceUpdateMsgBox_YesClicked(XNAMessageBox obj)
    {
        Updater.ClearVersionInfo();
        OnForceUpdate?.Invoke(this, EventArgs.Empty);
    }

    private void btnMoveUp_LeftClick(object sender, EventArgs e)
    {
        int selectedIndex = lbUpdateServerList.SelectedIndex;

        if (selectedIndex < 1)
        {
            return;
        }

        (lbUpdateServerList.Items[selectedIndex], lbUpdateServerList.Items[selectedIndex - 1]) = (lbUpdateServerList.Items[selectedIndex - 1], lbUpdateServerList.Items[selectedIndex]);
        lbUpdateServerList.SelectedIndex--;

        Updater.MoveMirrorUp(selectedIndex);
    }

    private void btnMoveDown_LeftClick(object sender, EventArgs e)
    {
        int selectedIndex = lbUpdateServerList.SelectedIndex;

        if (selectedIndex > lbUpdateServerList.Items.Count - 2 || selectedIndex < 0)
        {
            return;
        }

        (lbUpdateServerList.Items[selectedIndex], lbUpdateServerList.Items[selectedIndex + 1]) = (lbUpdateServerList.Items[selectedIndex + 1], lbUpdateServerList.Items[selectedIndex]);
        lbUpdateServerList.SelectedIndex++;

        Updater.MoveMirrorDown(selectedIndex);
    }

    public override void Load()
    {
        base.Load();

        lbUpdateServerList.Clear();

        foreach (UpdateMirror updaterMirror in Updater.UpdateMirrors)
        {

            string name = updaterMirror.Name.L10N($"INI:UpdateMirrors:{updaterMirror.Name}:Name");
            string location = updaterMirror.Location.L10N($"INI:UpdateMirrors:{updaterMirror.Name}:Location");

            lbUpdateServerList.AddItem(name +
                (!string.IsNullOrEmpty(location)
                    ? $" ({location})"
                    : string.Empty));
        }

        chkAutoCheck.Checked = IniSettings.CheckForUpdates;
    }

    public override bool Save()
    {
        bool restartRequired = base.Save();

        IniSettings.CheckForUpdates.Value = chkAutoCheck.Checked;

        IniSettings.SettingsIni.EraseSectionKeys("DownloadMirrors");

        int id = 0;

        foreach (UpdateMirror um in Updater.UpdateMirrors)
        {
            IniSettings.SettingsIni.SetStringValue("DownloadMirrors", id.ToString(), um.Name);
            id++;
        }

        return restartRequired;
    }

    public override void ToggleMainMenuOnlyOptions(bool enable)
    {
        btnForceUpdate.AllowClick = enable;
    }
}