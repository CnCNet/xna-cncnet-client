using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A box for drawing scenario briefings.
    /// </summary>
    class CoopBriefingBox : XNAPanel
    {
        private const int MARGIN = 12;
        private const float ALPHA_RATE = 0.2f;

        public CoopBriefingBox(WindowManager windowManager) : base(windowManager)
        {
        }

        string text = string.Empty;
        int fontIndex = 3;

        private RenderTarget2D renderTarget;

        private bool isVisible = true;
        private float alpha = 0f;

        public override void Initialize()
        {
            Name = "CoopBriefingBox";
            ClientRectangle = new Rectangle(0, 0, 400, 300);
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 2, 2);

            InputEnabled = false;

            base.Initialize();

            CreateRenderTarget();

            CenterOnParent();
        }

        public void CreateRenderTarget()
        {
            if (renderTarget != null)
                renderTarget.Dispose();
            renderTarget = new RenderTarget2D(GraphicsDevice, Parent.ClientRectangle.Width, Parent.ClientRectangle.Height,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PlatformContents);
        }

        public void SetFadeVisibility(bool visible)
        {
            isVisible = visible;
        }

        public void SetAlpha(float alpha)
        {
            this.alpha = alpha;
        }

        public void SetText(string text)
        {
            this.text = Renderer.FixText(text, fontIndex, ClientRectangle.Width - (MARGIN * 2)).Text;
            int textHeight = (int)Renderer.GetTextDimensions(this.text, fontIndex).Y;
            ClientRectangle = new Rectangle(ClientRectangle.X, 0,
                ClientRectangle.Width, textHeight + MARGIN * 2);
            CenterOnParent();
        }

        public override void Update(GameTime gameTime)
        {
            if (isVisible)
            {
                alpha = Math.Min(alpha + AlphaRate, 1.0f);
            }
            else
            {
                alpha = Math.Max(alpha - AlphaRate, 0.0f);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            //base.Draw(gameTime);

            Renderer.EndDraw();

            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(Color.Transparent);

            Renderer.BeginDraw();

            Renderer.FillRectangle(ClientRectangle, new Color(0, 0, 0, 224));
            Renderer.DrawRectangle(ClientRectangle, BorderColor);

            Renderer.DrawStringWithShadow(text, fontIndex,
                new Vector2(ClientRectangle.X + MARGIN, ClientRectangle.Y + MARGIN),
                UISettings.AltColor);

            Renderer.EndDraw();

            WindowManager.SetFinalRenderTarget();

            Renderer.BeginDraw();

            Renderer.DrawTexture(renderTarget, Parent.WindowRectangle(), new Color(1.0f, 1.0f, 1.0f, alpha));
        }
    }
}
