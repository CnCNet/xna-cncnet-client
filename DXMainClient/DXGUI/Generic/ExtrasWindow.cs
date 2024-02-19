using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System;
using System.Diagnostics;

namespace DTAClient.DXGUI.Generic
{
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

            var btnExStatistics = new XNAClientButton(WindowManager);
            btnExStatistics.Name = "btnExStatistics";
            btnExStatistics.ClientRectangle = new Rectangle(76, 17, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnExStatistics.Text = "Statistics".L10N("Client:Main:Statistics");
            btnExStatistics.LeftClick += BtnExStatistics_LeftClick;

            var btnExMapEditor = new XNAClientButton(WindowManager);
            btnExMapEditor.Name = "btnExMapEditor";
            btnExMapEditor.ClientRectangle = new Rectangle(76, 59, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnExMapEditor.Text = "Map Editor".L10N("Client:Main:MapEditor");
            btnExMapEditor.LeftClick += BtnExMapEditor_LeftClick;

            var btnExCredits = new XNAClientButton(WindowManager);
            btnExCredits.Name = "btnExCredits";
            btnExCredits.ClientRectangle = new Rectangle(76, 101, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnExCredits.Text = "Credits".L10N("Client:Main:Credits");
            btnExCredits.LeftClick += BtnExCredits_LeftClick;

            var btnExCancel = new XNAClientButton(WindowManager);
            btnExCancel.Name = "btnExCancel";
            btnExCancel.ClientRectangle = new Rectangle(76, 160, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnExCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
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
            using var mapEditorProcess = new Process();

            if (osVersion != OSVersion.UNIX)
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MapEditorExePath);
            else
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.UnixMapEditorExePath);

            mapEditorProcess.StartInfo.UseShellExecute = false;

            mapEditorProcess.Start();

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
}
