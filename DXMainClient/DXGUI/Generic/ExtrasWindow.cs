using System;
using System.Diagnostics;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using DTAClient.Domain;

using Microsoft.Xna.Framework;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Generic;

public class ExtrasWindow : XNAWindow
{
    public ExtrasWindow(WindowManager windowManager) : base(windowManager)
    {

    }

    public override void Initialize()
    {
        Name = "ExtrasWindow";
        ClientRectangle = new Rectangle(0, 0, 284, 190);
        BackgroundTexture = AssetLoader.LoadTexture("extrasMenu.png");

        XNAClientButton btnExStatistics = new(WindowManager)
        {
            Name = "btnExStatistics",
            ClientRectangle = new Rectangle(76, 17, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Statistics".L10N("Client:Main:Statistics")
        };
        btnExStatistics.LeftClick += BtnExStatistics_LeftClick;

        XNAClientButton btnExMapEditor = new(WindowManager)
        {
            Name = "btnExMapEditor",
            ClientRectangle = new Rectangle(76, 59, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Map Editor".L10N("Client:Main:MapEditor")
        };
        btnExMapEditor.LeftClick += BtnExMapEditor_LeftClick;

        XNAClientButton btnExCredits = new(WindowManager)
        {
            Name = "btnExCredits",
            ClientRectangle = new Rectangle(76, 101, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Credits".L10N("Client:Main:Credits")
        };
        btnExCredits.LeftClick += BtnExCredits_LeftClick;

        XNAClientButton btnExCancel = new(WindowManager)
        {
            Name = "btnExCancel",
            ClientRectangle = new Rectangle(76, 160, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Cancel".L10N("Client:Main:ButtonCancel")
        };
        btnExCancel.LeftClick += BtnExCancel_LeftClick;

        AddChild(btnExStatistics);
        AddChild(btnExMapEditor);
        AddChild(btnExCredits);
        AddChild(btnExCancel);

        base.Initialize();

        CenterOnParent();
    }

    private void BtnExStatistics_LeftClick(object sender, EventArgs e)
    {
        MainMenuDarkeningPanel parent = (MainMenuDarkeningPanel)Parent;
        parent.Show(parent.StatisticsWindow);
    }

    private void BtnExMapEditor_LeftClick(object sender, EventArgs e)
    {
        OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();
        using Process mapEditorProcess = new();

        mapEditorProcess.StartInfo.FileName = osVersion != OSVersion.UNIX
            ? SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MapEditorExePath)
            : SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.UnixMapEditorExePath);

        mapEditorProcess.StartInfo.UseShellExecute = false;

        _ = mapEditorProcess.Start();

        Enabled = false;
    }

    private void BtnExCredits_LeftClick(object sender, EventArgs e)
    {
        ProcessLauncher.StartShellProcess(MainClientConstants.CREDITS_URL);
    }

    private void BtnExCancel_LeftClick(object sender, EventArgs e)
    {
        Enabled = false;
    }
}