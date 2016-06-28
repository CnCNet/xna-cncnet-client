using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;

namespace ClientGUI
{
    /// <summary>
    /// A panel that darkens the whole screen.
    /// </summary>
    public class DarkeningPanel : XNAPanel
    {
        public DarkeningPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public event EventHandler Hidden;

        public override void Initialize()
        {
            Name = "DarkeningPanel";

            if (Parent != null)
            {
                ClientRectangle = new Rectangle(-Parent.ClientRectangle.X, -Parent.ClientRectangle.Y,
                    Parent.ClientRectangle.Width + Parent.ClientRectangle.X,
                    Parent.ClientRectangle.Height + Parent.ClientRectangle.Y);
            }
            else
            {
                ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);
            }

            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            DrawBorders = false;

            base.Initialize();
        }

        public void Show()
        {
            Enabled = true;
            Visible = true;
            AlphaRate = 0.04f;
            Alpha = 0.01f;

            foreach (XNAControl child in Children)
            {
                child.Enabled = true;
                child.Visible = true;
            }
        }

        public void Hide()
        {
            AlphaRate = -0.04f;

            foreach (XNAControl child in Children)
            {
                child.Enabled = false;
                child.Visible = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Alpha <= 0.0f)
            {
                Enabled = false;
                Visible = false;
                Hidden?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
