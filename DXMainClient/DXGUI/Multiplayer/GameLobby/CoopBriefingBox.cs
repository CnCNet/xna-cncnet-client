using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    class CoopBriefingBox : XNAPanel
    {
        const int MARGIN = 12;

        public CoopBriefingBox(WindowManager windowManager) : base(windowManager)
        {
        }

        string text = string.Empty;
        int fontIndex = 3;

        public override void Initialize()
        {
            Name = "CoopBriefingBox";
            ClientRectangle = new Rectangle(0, 0, 400, 300);
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 2, 2);

            base.Initialize();

            CenterOnParent();
        }

        public void SetText(string text)
        {
            this.text = Renderer.FixText(text, fontIndex, ClientRectangle.Width - (MARGIN * 2)).Text;
            int textHeight = (int)Renderer.GetTextDimensions(this.text, fontIndex).Y;
            ClientRectangle = new Rectangle(ClientRectangle.X, 0,
                ClientRectangle.Width, textHeight + MARGIN * 2);
            CenterOnParent();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            var drawRectangle = WindowRectangle();

            Renderer.DrawStringWithShadow(text, fontIndex,
                new Vector2(drawRectangle.X + MARGIN, drawRectangle.Y + MARGIN),
                UISettings.AltColor);
        }
    }
}
