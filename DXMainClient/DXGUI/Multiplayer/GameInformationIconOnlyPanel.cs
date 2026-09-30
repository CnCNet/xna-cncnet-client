using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    ///<summary>
    /// A panel for showing a game information icon without text.
    ///</summary>
    public class GameInformationIconOnlyPanel : XNAPanel
    {
        private readonly Texture2D icon;

        public GameInformationIconOnlyPanel(WindowManager windowManager, Texture2D icon) : base(windowManager)
        {
            this.icon = icon;
            DrawBorders = false;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (icon == null)
                return;

            DrawTexture(icon, new Rectangle(0, 0, icon.Width, icon.Height), Color.White);
        }
    }
}
