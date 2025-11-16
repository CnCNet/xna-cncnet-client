using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    ///<summary>
    /// A panel for showing a game information icon next to its associated label.
    ///</summary>
    public class GameInformationIconPanel : XNAPanel
    {
        private readonly Texture2D icon;
        private readonly string label;
        private const int iconLabelSpacing = 6;
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

            var textSize = Renderer.GetTextDimensions(label, FontIndex);
            int textHeight = (int)textSize.Y;

            int iconY = (textHeight - icon.Height) / 2;
            if (iconY < 0) iconY = 0;

            DrawTexture(icon, new Rectangle(0, iconY, icon.Width, icon.Height), Color.White);

            int panelHeight = Math.Max(icon.Height, textHeight);
            float textY = (panelHeight - textHeight) / 2f;

            DrawString(label, FontIndex, new Vector2(icon.Width + iconLabelSpacing, textY), UISettings.ActiveSettings.TextColor);
        }
    }
}
