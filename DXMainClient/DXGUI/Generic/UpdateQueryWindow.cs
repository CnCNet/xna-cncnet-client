using System;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic;

/// <summary>
/// A window that asks the user whether they want to update their game.
/// </summary>
public class UpdateQueryWindow : XNAWindow
{
    public delegate void UpdateAcceptedEventHandler(object sender, EventArgs e);
    public event UpdateAcceptedEventHandler UpdateAccepted;

    public delegate void UpdateDeclinedEventHandler(object sender, EventArgs e);
    public event UpdateDeclinedEventHandler UpdateDeclined;

    public UpdateQueryWindow(WindowManager windowManager) : base(windowManager) { }

    private XNALabel lblDescription;
    private XNALabel lblUpdateSize;

    private string changelogUrl;

    public override void Initialize()
    {
        changelogUrl = ClientConfiguration.Instance.ChangelogURL;

        Name = "UpdateQueryWindow";
        ClientRectangle = new Rectangle(0, 0, 251, 140);
        BackgroundTexture = AssetLoader.LoadTexture("updatequerybg.png");

        lblDescription = new XNALabel(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 9, 0, 0),
            Text = string.Empty
        };
        lblDescription.Name = nameof(lblDescription);

        XNALinkLabel lblChangelogLink = new(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 50, 0, 0),
            Text = "View Changelog".L10N("Client:Main:ViewChangeLog"),
            IdleColor = Color.Goldenrod
        };
        lblChangelogLink.Name = nameof(lblChangelogLink);
        lblChangelogLink.LeftClick += LblChangelogLink_LeftClick;

        lblUpdateSize = new XNALabel(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 80, 0, 0),
            Text = string.Empty
        };
        lblUpdateSize.Name = nameof(lblUpdateSize);

        XNAClientButton btnYes = new(WindowManager)
        {
            ClientRectangle = new Rectangle(12, 110, 75, 23),
            Text = "Yes".L10N("Client:Main:ButtonYes")
        };
        btnYes.LeftClick += BtnYes_LeftClick;
        btnYes.Name = nameof(btnYes);

        XNAClientButton btnNo = new(WindowManager)
        {
            ClientRectangle = new Rectangle(164, 110, 75, 23),
            Text = "No".L10N("Client:Main:ButtonNo")
        };
        btnNo.LeftClick += BtnNo_LeftClick;
        btnNo.Name = nameof(btnNo);

        AddChild(lblDescription);
        AddChild(lblChangelogLink);
        AddChild(lblUpdateSize);
        AddChild(btnYes);
        AddChild(btnNo);

        base.Initialize();

        CenterOnParent();
    }

    private void LblChangelogLink_LeftClick(object sender, EventArgs e)
    {
        ProcessLauncher.StartShellProcess(changelogUrl);
    }

    private void BtnYes_LeftClick(object sender, EventArgs e)
    {
        UpdateAccepted?.Invoke(this, e);
    }

    private void BtnNo_LeftClick(object sender, EventArgs e)
    {
        UpdateDeclined?.Invoke(this, e);
    }

    public void SetInfo(string version, int updateSize)
    {
        lblDescription.Text = string.Format("Version {0} is available for download.\nDo you wish to install it?".L10N("Client:Main:VersionAvailable"), version);
        lblUpdateSize.Text = updateSize >= 1000
            ? string.Format("The size of the update is {0} MB.".L10N("Client:Main:UpdateSizeMB"), updateSize / 1000)
            : string.Format("The size of the update is {0} KB.".L10N("Client:Main:UpdateSizeKB"), updateSize);
    }
}