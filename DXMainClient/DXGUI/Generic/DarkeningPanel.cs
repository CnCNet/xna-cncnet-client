using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;

namespace DTAClient.DXGUI.Generic
{
    public class DarkeningPanel : DXPanel
    {
        public DarkeningPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            Name = "DarkeningPanel";
            ClientRectangle = new Rectangle(-Parent.ClientRectangle.X, -Parent.ClientRectangle.Y,
                Parent.ClientRectangle.Width + Parent.ClientRectangle.X, 
                Parent.ClientRectangle.Height + Parent.ClientRectangle.Y);
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

            foreach (DXControl child in Children)
            {
                child.Enabled = true;
                child.Visible = true;
            }
        }

        public void Hide()
        {
            AlphaRate = -0.04f;

            foreach (DXControl child in Children)
            {
                child.Enabled = false;
                child.Visible = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Alpha == 0.0f)
            {
                Enabled = false;
                Visible = false;
            }
        }
    }
}
