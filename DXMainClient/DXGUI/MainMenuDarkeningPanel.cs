using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dtasetup.DXGUI
{
    public class MainMenuDarkeningPanel : DXPanel
    {
        public MainMenuDarkeningPanel(Game game) : base(game)
        {

        }

        public DXCampaignSelector CampaignSelector;
        public DXGameInProgressWindow GameInProgressWindow;
        public DXGameLoadingWindow GameLoadingWindow;
        public StatisticsWindow StatisticsWindow;
        public UpdateQueryWindow UpdateQueryWindow;
        public UpdateWindow UpdateWindow;

        const float BG_ALPHA_APPEAR_RATE = 0.1f;
        const float BG_ALPHA_DISAPPEAR_RATE = -0.1f;

        float bgAlpha = 1.0f;
        float bgAlphaRate = -0.05f;

        public override void Initialize()
        {
            base.Initialize();

            Name = "DarkeningPanel";
            BorderColor = UISettings.WindowBorderColor;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            //ClientRectangle = new Rectangle(0, 0, 1, 1);
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            Alpha = 1.0f;

            //ClientRectangle = new Rectangle(0, 0, WindowManager.Instance.RenderResolutionX, WindowManager.Instance.RenderResolutionY);

            CampaignSelector = new DXCampaignSelector(Game);
            AddChild(CampaignSelector);

            GameInProgressWindow = new DXGameInProgressWindow(Game);
            AddChild(GameInProgressWindow);

            GameLoadingWindow = new DXGameLoadingWindow(Game);
            AddChild(GameLoadingWindow);

            StatisticsWindow = new StatisticsWindow(Game);
            AddChild(StatisticsWindow);

            UpdateQueryWindow = new UpdateQueryWindow(Game);
            AddChild(UpdateQueryWindow);

            UpdateWindow = new UpdateWindow(Game);
            AddChild(UpdateWindow);

            foreach (DXControl child in Children)
                child.Visible = false;
        }

        public void Show(DXControl dxControl)
        {
            Enabled = true;
            Visible = true;

            bgAlphaRate = BG_ALPHA_APPEAR_RATE;
            Alpha = 1.0f;

            foreach (DXControl child in Children)
            {
                child.Enabled = false;
                child.Visible = false;
            }

            if (dxControl != null)
            {
                dxControl.Enabled = true;
                dxControl.Visible = true;
                dxControl.IgnoreInputOnFrame = true;
            }
        }

        public void InstantShow()
        {
            bgAlpha = 1.0f;
            Enabled = true;
            Visible = true;
        }

        public void Hide()
        {
            bgAlphaRate = BG_ALPHA_DISAPPEAR_RATE;

            foreach (DXControl child in Children)
            {
                child.Enabled = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            bgAlpha += bgAlphaRate;
            if (bgAlphaRate < 0.0f)
                Alpha += bgAlphaRate;

            if (bgAlpha > 1f)
                bgAlpha = 1f;

            if (bgAlpha < 0f)
            {
                Enabled = false;
                Visible = false;
                bgAlpha = 0f;

                foreach (DXControl child in Children)
                {
                    child.Visible = false;
                }

                ((MainMenu)Parent).Enable();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Renderer.DrawTexture(BackgroundTexture, ClientRectangle, new Color(255, 255, 255, (int)(bgAlpha * 255)));

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Visible)
                {
                    Children[i].Draw(gameTime);
                }
            }
        }
    }
}
