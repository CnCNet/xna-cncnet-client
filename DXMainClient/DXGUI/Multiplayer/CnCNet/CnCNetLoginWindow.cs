using System;

using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Extensions;
using ClientCore.Settings;

using ClientGUI;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

internal class CnCNetLoginWindow : XNAWindow
{
    public CnCNetLoginWindow(WindowManager windowManager) : base(windowManager)
    {
    }

    private XNALabel lblConnectToCnCNet;
    private XNATextBox tbPlayerName;
    private XNALabel lblPlayerName;
    private XNAClientCheckBox chkRememberMe;
    private XNAClientCheckBox chkPersistentMode;
    private XNAClientCheckBox chkAutoConnect;
    private XNAClientButton btnConnect;
    private XNAClientButton btnCancel;

    public event EventHandler Cancelled;
    public event EventHandler Connect;

    public override void Initialize()
    {
        Name = "CnCNetLoginWindow";
        ClientRectangle = new Rectangle(0, 0, 300, 220);
        BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");

        lblConnectToCnCNet = new XNALabel(WindowManager)
        {
            Name = "lblConnectToCnCNet",
            FontIndex = 1,
            Text = "CONNECT TO CNCNET".L10N("Client:Main:ConnectToCncNet")
        };

        AddChild(lblConnectToCnCNet);
        lblConnectToCnCNet.CenterOnParent();
        lblConnectToCnCNet.ClientRectangle = new Rectangle(
            lblConnectToCnCNet.X, 12,
            lblConnectToCnCNet.Width,
            lblConnectToCnCNet.Height);

        tbPlayerName = new XNATextBox(WindowManager)
        {
            Name = "tbPlayerName",
            ClientRectangle = new Rectangle(Width - 132, 50, 120, 19),
            MaximumTextLength = ClientConfiguration.Instance.MaxNameLength
        };
        string defgame = ClientConfiguration.Instance.LocalGame;

        lblPlayerName = new XNALabel(WindowManager)
        {
            Name = "lblPlayerName",
            FontIndex = 1,
            Text = "PLAYER NAME:".L10N("Client:Main:PlayerName")
        };
        lblPlayerName.ClientRectangle = new Rectangle(12, tbPlayerName.Y + 1,
            lblPlayerName.Width, lblPlayerName.Height);

        chkRememberMe = new XNAClientCheckBox(WindowManager)
        {
            Name = "chkRememberMe",
            ClientRectangle = new Rectangle(12, tbPlayerName.Bottom + 12, 0, 0),
            Text = "Remember me".L10N("Client:Main:RememberMe"),
            TextPadding = 7
        };
        chkRememberMe.CheckedChanged += ChkRememberMe_CheckedChanged;

        chkPersistentMode = new XNAClientCheckBox(WindowManager)
        {
            Name = "chkPersistentMode",
            ClientRectangle = new Rectangle(12, chkRememberMe.Bottom + 30, 0, 0),
            Text = "Stay connected outside of the CnCNet lobby".L10N("Client:Main:StayConnect"),
            TextPadding = chkRememberMe.TextPadding
        };
        chkPersistentMode.CheckedChanged += ChkPersistentMode_CheckedChanged;

        chkAutoConnect = new XNAClientCheckBox(WindowManager)
        {
            Name = "chkAutoConnect",
            ClientRectangle = new Rectangle(12, chkPersistentMode.Bottom + 30, 0, 0),
            Text = "Connect automatically on client startup".L10N("Client:Main:AutoConnect"),
            TextPadding = chkRememberMe.TextPadding,
            AllowChecking = false
        };

        btnConnect = new XNAClientButton(WindowManager)
        {
            Name = "btnConnect",
            ClientRectangle = new Rectangle(12, Height - 35, 110, 23),
            Text = "Connect".L10N("Client:Main:ButtonConnect")
        };
        btnConnect.LeftClick += BtnConnect_LeftClick;

        btnCancel = new XNAClientButton(WindowManager)
        {
            Name = "btnCancel",
            ClientRectangle = new Rectangle(Width - 122, btnConnect.Y, 110, 23),
            Text = "Cancel".L10N("Client:Main:ButtonCancel")
        };
        btnCancel.LeftClick += BtnCancel_LeftClick;

        AddChild(tbPlayerName);
        AddChild(lblPlayerName);
        AddChild(chkRememberMe);
        AddChild(chkPersistentMode);
        AddChild(chkAutoConnect);
        AddChild(btnConnect);
        AddChild(btnCancel);

        base.Initialize();

        CenterOnParent();

        UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;
    }

    private void Instance_SettingsSaved(object sender, EventArgs e)
    {
        tbPlayerName.Text = UserINISettings.Instance.PlayerName;
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void ChkRememberMe_CheckedChanged(object sender, EventArgs e)
    {
        CheckAutoConnectAllowance();
    }

    private void ChkPersistentMode_CheckedChanged(object sender, EventArgs e)
    {
        CheckAutoConnectAllowance();
    }

    private void CheckAutoConnectAllowance()
    {
        chkAutoConnect.AllowChecking = chkPersistentMode.Checked && chkRememberMe.Checked;
        if (!chkAutoConnect.AllowChecking)
        {
            chkAutoConnect.Checked = false;
        }
    }

    private void BtnConnect_LeftClick(object sender, EventArgs e)
    {
        string errorMessage = NameValidator.IsNameValid(tbPlayerName.Text);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            XNAMessageBox.Show(WindowManager, "Invalid Player Name".L10N("Client:Main:InvalidPlayerName"), errorMessage);
            return;
        }

        ProgramConstants.PLAYERNAME = tbPlayerName.Text;

        UserINISettings.Instance.SkipConnectDialog.Value = chkRememberMe.Checked;
        UserINISettings.Instance.PersistentMode.Value = chkPersistentMode.Checked;
        UserINISettings.Instance.AutomaticCnCNetLogin.Value = chkAutoConnect.Checked;
        UserINISettings.Instance.PlayerName.Value = ProgramConstants.PLAYERNAME;

        UserINISettings.Instance.SaveSettings();

        Connect?.Invoke(this, EventArgs.Empty);
    }

    public void LoadSettings()
    {
        chkAutoConnect.Checked = UserINISettings.Instance.AutomaticCnCNetLogin;
        chkPersistentMode.Checked = UserINISettings.Instance.PersistentMode;
        chkRememberMe.Checked = UserINISettings.Instance.SkipConnectDialog;

        tbPlayerName.Text = UserINISettings.Instance.PlayerName;

        if (chkRememberMe.Checked)
        {
            BtnConnect_LeftClick(this, EventArgs.Empty);
        }
    }
}