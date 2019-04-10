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

        private bool isVisible = true;
        private float alpha = 0f;

        public override void Initialize()
        {
            Name = "CoopBriefingBox";
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            ClientRectangle = new Rectangle(0, 0, 400, 300);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 2, 2);

            InputEnabled = false;

            base.Initialize();

            CenterOnParent();
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
            this.text = Renderer.FixText(text, fontIndex, Width - (MARGIN * 2)).Text;
            int textHeight = (int)Renderer.GetTextDimensions(this.text, fontIndex).Y;
            ClientRectangle = new Rectangle(X, 0,
                Width, textHeight + MARGIN * 2);
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

            FillControlArea(new Color(0, 0, 0, 224));
            DrawRectangle(new Rectangle(0, 0, Width, Height), BorderColor);

            Renderer.DrawStringWithShadow(text, fontIndex,
                new Vector2(X + MARGIN, Y + MARGIN),
                UISettings.ActiveSettings.AltColor);
        }
    }
}
