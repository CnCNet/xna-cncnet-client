using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
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
            btnExStatistics.Text = "Statistics".L10N("UI:Main:Statistics");
            btnExStatistics.LeftClick += BtnExStatistics_LeftClick;

            var btnExMapEditor = new XNAClientButton(WindowManager);
            btnExMapEditor.Name = "btnExMapEditor";
            btnExMapEditor.ClientRectangle = new Rectangle(76, 59, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnExMapEditor.Text = "Map Editor".L10N("UI:Main:MapEditor");
            btnExMapEditor.LeftClick += BtnExMapEditor_LeftClick;

            var btnExCredits = new XNAClientButton(WindowManager);
            btnExCredits.Name = "btnExCredits";
            btnExCredits.ClientRectangle = new Rectangle(76, 101, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnExCredits.Text = "Credits".L10N("UI:Main:Credits");
            btnExCredits.LeftClick += BtnExCredits_LeftClick;

            var btnExCancel = new XNAClientButton(WindowManager);
            btnExCancel.Name = "btnExCancel";
            btnExCancel.ClientRectangle = new Rectangle(76, 160, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnExCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
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
            Process.Start(ProgramConstants.GamePath + ClientConfiguration.Instance.MapEditorExePath);
            Enabled = false;
        }

        private void BtnExCredits_LeftClick(object sender, EventArgs e)
        {
            Process.Start(MainClientConstants.CREDITS_URL);
        }

        private void BtnExCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }
    }
}
