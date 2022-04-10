using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Generic
{
    public static class AssetTextures
    {
        // Game list box
        public static readonly Texture2D LockedGame = AssetLoader.LoadTexture("lockedgame.png");
        public static readonly Texture2D IncompatibleGame = AssetLoader.LoadTexture("incompatible.png");
        public static readonly Texture2D PasswordedGame = AssetLoader.LoadTexture("passwordedgame.png");
    }
}
