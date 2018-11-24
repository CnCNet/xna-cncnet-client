using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A tool tip for a game option.
    /// </summary>
    class ToolTip
    {
        private const int FONT_INDEX = 0;
        private const int MARGIN = 6;
        private const float ALPHA_RATE_PER_SECOND = 2.0f;

        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                Vector2 textSize = Renderer.GetTextDimensions(text, FONT_INDEX);
                Size = new Point((int)textSize.X + MARGIN * 2, (int)textSize.Y + MARGIN * 2);
            }
        }

        public Point Location { get; set; }
        public Point Size { get; private set; }
        public float Alpha { get; private set; }
        public bool IsMasterControlOnCursor { get; set; }
        public bool IsVisible { get; private set; }

        public void Update(GameTime gameTime)
        {
            if (IsMasterControlOnCursor)
            {
                Alpha += ALPHA_RATE_PER_SECOND * (float)gameTime.ElapsedGameTime.TotalSeconds;
                IsVisible = true;
                if (Alpha > 1.0f)
                    Alpha = 1.0f;
            }
            else
            {
                Alpha -= ALPHA_RATE_PER_SECOND * (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (Alpha < 0f)
                {
                    Alpha = 0f;
                    IsVisible = false;
                }
            }
        }

        public void Draw()
        {
            Renderer.FillRectangle(new Rectangle(Location.X, Location.Y, Size.X, Size.Y),
                ColorFromAlpha(UISettings.BackgroundColor));
            Renderer.DrawRectangle(new Rectangle(Location.X, Location.Y, Size.X, Size.Y),
                ColorFromAlpha(UISettings.AltColor));
            Renderer.DrawString(Text, FONT_INDEX, 1.0f,
                new Vector2(Location.X + MARGIN, Location.Y + MARGIN),
                ColorFromAlpha(UISettings.AltColor));
        }

        private Color ColorFromAlpha(Color color)
        {
#if XNA
            return new Color(color.R, color.G, color.B, (int)(Alpha * 255.0f));
#else
            return new Color(color, Alpha);
#endif

        }
    }
}
