using ClientCore;
using ClientGUI.DirectX;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClientGUI.DXControls
{
    public class DXPanel : DXControl
    {
        public DXPanel(Game game) : base(game)
        {
            BorderColor = UISettingsLoader.GetPanelBorderColor();
        }

        public PanelBackgroundImageDrawMode DrawMode = PanelBackgroundImageDrawMode.TILED;

        public Texture2D BackgroundTexture { get; set; }

        public Color BorderColor { get; set; }

        bool _drawBorders = true;
        public bool DrawBorders
        {
            get { return _drawBorders; }
            set { _drawBorders = value; }
        }

        /// <summary>
        /// If this is set, the DXPanel will render itself on a separate render target.
        /// After the rendering is complete, it'll set this render target to be the
        /// primary render target.
        /// </summary>
        public RenderTarget2D OriginalRenderTarget { get; set; }

        RenderTarget2D renderTarget;

        Texture2D BorderTexture { get; set; }

        public float AlphaRate = 0.01f;

        public override void Initialize()
        {
            base.Initialize();

            BorderTexture = AssetLoader.CreateTexture(Color.White, 1, 1);

            renderTarget = new RenderTarget2D(GraphicsDevice, WindowManager.Instance.RenderResolutionX, WindowManager.Instance.RenderResolutionY);
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key)
        {
            switch (key)
            {
                case "BorderColor":
                    BorderColor = AssetLoader.GetColorFromString(iniFile.GetStringValue(Name, "BorderColor", "255,255,255"));
                    return;
                case "DrawMode":
                    string value = iniFile.GetStringValue(Name, "DrawMode", "Tiled");
                    if (value == "Tiled")
                        DrawMode = PanelBackgroundImageDrawMode.TILED;
                    else
                        DrawMode = PanelBackgroundImageDrawMode.STRECHED;
                    return;
                case "AlphaRate":
                    AlphaRate = iniFile.GetSingleValue(Name, "AlphaRate", 0.01f);
                    return;
                case "BackgroundTexture":
                    BackgroundTexture = AssetLoader.LoadTexture(iniFile.GetStringValue(Name, "BackgroundTexture", String.Empty));
                    return;
                case "DrawBorders":
                    DrawBorders = iniFile.GetBooleanValue(Name, "DrawBorders", true);
                    return;
                case "Padding":
                    string[] parts = iniFile.GetStringValue(Name, "Padding", "0,0,0,0").Split(',');
                    int left = Int32.Parse(parts[0]);
                    int top = Int32.Parse(parts[1]);
                    int right = Int32.Parse(parts[2]);
                    int bottom = Int32.Parse(parts[3]);
                    ClientRectangle = new Rectangle(ClientRectangle.X - left, ClientRectangle.Y - top,
                        ClientRectangle.Width + left + right, ClientRectangle.Height + top + bottom);
                    foreach (DXControl child in Children)
                    {
                        child.ClientRectangle = new Rectangle(child.ClientRectangle.X + left,
                            child.ClientRectangle.Y + top, child.ClientRectangle.Width, child.ClientRectangle.Height);
                    }
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key);
        }

        public override void Update(GameTime gameTime)
        {
            Alpha += AlphaRate;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Color color = GetRemapColor();

            if (OriginalRenderTarget != null)
            {
                GraphicsDevice.SetRenderTarget(renderTarget);
                GraphicsDevice.Clear(Color.MonoGameOrange);
            }

            Rectangle windowRectangle = WindowRectangle();

            if (BackgroundTexture != null)
            {
                if (DrawMode == PanelBackgroundImageDrawMode.TILED)
                {
                    for (int x = 0; x < windowRectangle.Width; x += BackgroundTexture.Width)
                    {
                        for (int y = 0; y < windowRectangle.Height; y += BackgroundTexture.Height)
                        {
                            if (x + BackgroundTexture.Width < windowRectangle.Width)
                            {
                                if (y + BackgroundTexture.Height < windowRectangle.Height)
                                {
                                    Renderer.DrawTexture(BackgroundTexture, new Rectangle(windowRectangle.X + x, windowRectangle.Y + y,
                                        BackgroundTexture.Width, BackgroundTexture.Height), color);
                                }
                                else
                                {
                                    Renderer.DrawTexture(BackgroundTexture,
                                        new Rectangle(0, 0, BackgroundTexture.Width, windowRectangle.Height - y),
                                        new Rectangle(windowRectangle.X + x, windowRectangle.Y + y,
                                        BackgroundTexture.Width, windowRectangle.Height - y), color);
                                }
                            }
                            else if (y + BackgroundTexture.Height < windowRectangle.Height)
                            {
                                Renderer.DrawTexture(BackgroundTexture,
                                    new Rectangle(0, 0, windowRectangle.Width - x, BackgroundTexture.Height),
                                    new Rectangle(windowRectangle.X + x, windowRectangle.Y + y,
                                    windowRectangle.Width - x, BackgroundTexture.Height), color);
                            }
                            else
                            {
                                Renderer.DrawTexture(BackgroundTexture,
                                    new Rectangle(0, 0, windowRectangle.Width - x, windowRectangle.Height - y),
                                    new Rectangle(windowRectangle.X + x, windowRectangle.Y + y,
                                    windowRectangle.Width - x, windowRectangle.Height - y), color);
                            }
                        }
                    }
                }
                else
                {
                    Renderer.DrawTexture(BackgroundTexture, windowRectangle, color);
                }
            }

            if (DrawBorders)
            {
                Renderer.DrawRectangle(windowRectangle, GetColorWithAlpha(BorderColor));
            }

            base.Draw(gameTime);

            if (OriginalRenderTarget != null)
            {
                Renderer.EndDraw();

                GraphicsDevice.SetRenderTarget(OriginalRenderTarget);

                Renderer.BeginDraw2();

                Renderer.DrawTexture(renderTarget, windowRectangle, GetColorWithAlpha(Color.White));

                Renderer.EndDraw();

                Renderer.BeginDraw();
            }
        }
    }

    public enum PanelBackgroundImageDrawMode
    {
        TILED,
        STRECHED
    }
}
