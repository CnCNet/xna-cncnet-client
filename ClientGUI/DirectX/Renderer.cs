using ClientCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DirectX
{
    public static class Renderer
    {
        static SpriteBatch SpriteBatch;

        static List<SpriteFont> Fonts;

        static Texture2D whitePixelTexture;

        public static void Initialize(GraphicsDevice gd, ContentManager content)
        {
            SpriteBatch = new SpriteBatch(gd);
            Fonts = new List<SpriteFont>();

            content.RootDirectory = ProgramConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH;
            Fonts.Add(content.Load<SpriteFont>("SpriteFont1"));
            Fonts.Add(content.Load<SpriteFont>("SpriteFont2"));

            whitePixelTexture = AssetLoader.CreateTexture(Color.White, 1, 1);
        }

        public static TextParseReturnValue FixText(string text, int fontIndex, int width)
        {
            return TextParseReturnValue.FixText(Fonts[fontIndex], width, text);
        }

        public static List<string> GetFixedTextLines(string text, int fontIndex, int width)
        {
            return TextParseReturnValue.GetFixedTextLines(Fonts[fontIndex], width, text);
        }

        public static void BeginDraw()
        {
            BlendState bs = new BlendState();
            //bs.SeparateAlphaBlendEnabled = true;
            bs.AlphaDestinationBlend = Blend.One;
            bs.AlphaSourceBlend = Blend.SourceAlpha;
            bs.ColorSourceBlend = Blend.SourceAlpha;
            bs.ColorDestinationBlend = Blend.InverseSourceAlpha;

            SpriteBatch.Begin(SpriteSortMode.Deferred, bs);
        }

        public static void BeginDraw2()
        {
            BlendState bs = new BlendState();
            bs.AlphaDestinationBlend = Blend.One;
            bs.AlphaSourceBlend = Blend.SourceAlpha;
            bs.ColorSourceBlend = Blend.SourceAlpha;
            bs.ColorDestinationBlend = Blend.InverseSourceAlpha;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        }

        public static void DrawTexture(Texture2D texture, Rectangle rectangle, Color color)
        {
            SpriteBatch.Draw(texture, rectangle, color);
        }

        public static void DrawTexture(Texture2D texture, Rectangle sourceRectangle, Rectangle destinationRectangle, Color color)
        {
            SpriteBatch.Draw(texture, destinationRectangle, sourceRectangle, color);
        }

        public static void DrawTexture(Texture2D texture, Vector2 location, float rotation, Vector2 origin, Vector2 scale, Color color)
        {
            SpriteBatch.Draw(texture, location, null, null, origin, rotation, scale, color, SpriteEffects.None, 0f);
        }

        public static void DrawStringWithShadow(string text, int fontIndex, Vector2 location, Color color)
        {
            if (fontIndex >= Fonts.Count)
                return;

            SpriteBatch.DrawString(Fonts[fontIndex], text, new Vector2(location.X + 1f, location.Y + 1f), new Color(0, 0, 0, color.A));
            SpriteBatch.DrawString(Fonts[fontIndex], text, location, color);
        }

        public static void DrawRectangle(Rectangle rect, Color color)
        {
            SpriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            SpriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + 1, 1, rect.Height - 1), color);
            SpriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X + rect.Width - 1, rect.Y, 1, rect.Height), color);
            SpriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - 1, rect.Width, 1), color);
        }

        public static void FillRectangle(Rectangle rect, Color color)
        {
            SpriteBatch.Draw(whitePixelTexture, rect, color);
        }

        public static Vector2 GetTextDimensions(string text, int fontIndex)
        {
            return Fonts[fontIndex].MeasureString(text);
        }

        public static void EndDraw()
        {
            SpriteBatch.End();
        }
    }
}
