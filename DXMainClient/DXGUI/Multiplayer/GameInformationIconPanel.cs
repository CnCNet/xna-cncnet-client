using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    public class GameInformationIconPanel : XNAPanel
    {
        private readonly Texture2D icon;
        private readonly string label;
        public int FontIndex = 1;

        public GameInformationIconPanel(WindowManager windowManager, Texture2D icon, string label) : base(windowManager)
        {
            this.icon = icon;
            this.label = label;

            DrawBorders = false;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            DrawTexture(icon, new Rectangle(0, 0, icon.Width, icon.Height), Color.White);

            DrawString(label, FontIndex, new Vector2(icon.Width + 4, 0), UISettings.ActiveSettings.TextColor);
        }
    }
}
