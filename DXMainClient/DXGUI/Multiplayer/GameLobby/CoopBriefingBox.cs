using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A box for drawing scenario briefings.
    /// </summary>
    class CoopBriefingBox : XNAPanel
    {
        private const int MARGIN = 12;
        private const float ALPHA_RATE = 0.4f;

        public CoopBriefingBox(WindowManager windowManager) : base(windowManager)
        {
        }

        /// <summary>
        /// The index of the text font.
        /// </summary>
        public int FontIndex { get; set; } = 0;

        string text = string.Empty;

        private bool isVisible = true;

        public override void Initialize()
        {
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            ClientRectangle = new Rectangle(0, 0, 400, 300);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 224), 2, 2);

            InputEnabled = false;

            AlphaRate = ALPHA_RATE;

            base.Initialize();

            CenterOnParent();
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "FontIndex":
                    FontIndex = Conversions.IntFromString(value, 0);
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public void SetFadeVisibility(bool visible)
        {
            isVisible = visible;
        }

        public void SetAlpha(float alpha)
        {
            Alpha = alpha;
        }

        public void SetText(string text)
        {
            this.text = Renderer.FixText(text, FontIndex, Width - (MARGIN * 2)).Text;
            int textHeight = (int)Renderer.GetTextDimensions(this.text, FontIndex).Y;
            ClientRectangle = new Rectangle(X, 0,
                Width, textHeight + MARGIN * 2);
            CenterOnParent();
        }

        public override void Update(GameTime gameTime)
        {
            if (isVisible)
            {
                AlphaRate = ALPHA_RATE;
            }
            else
            {
                AlphaRate = -ALPHA_RATE;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            //base.Draw(gameTime);

            FillControlArea(new Color(0, 0, 0, 224));
            DrawRectangle(new Rectangle(0, 0, Width, Height), BorderColor);
            DrawStringWithShadow(text, FontIndex,
                new Vector2(MARGIN, MARGIN),
                UISettings.ActiveSettings.AltColor);
        }
    }
}
