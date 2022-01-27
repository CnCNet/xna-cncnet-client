using Rampastring.XNAUI.XNAControls;
using System;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;

namespace ClientGUI
{
    /// <summary>
    /// A panel that darkens the whole screen.
    /// </summary>
    public class DarkeningPanel : XNAPanel
    {
        public const float ALPHA_RATE = 0.6f;

        public DarkeningPanel(WindowManager windowManager) : base(windowManager)
        {
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        }

        public event EventHandler Hidden;

        public override void Initialize()
        {
            Name = "DarkeningPanel";

            SetPositionAndSize();
            
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            DrawBorders = false;

            base.Initialize();
        }

        public void SetPositionAndSize()
        {
            if (Parent != null)
            {
                ClientRectangle = new Rectangle(-Parent.X, -Parent.Y,
                    WindowManager.RenderResolutionX,
                    WindowManager.RenderResolutionY);
            }
            else
            {
                ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);
            }
        }

        public override void AddChild(XNAControl child)
        {
            base.AddChild(child);

            child.VisibleChanged += Child_VisibleChanged;
        }

        private void Child_VisibleChanged(object sender, EventArgs e)
        {
            var xnaControl = (XNAControl)sender;

            if (xnaControl.Visible)
                Show();
            else
                Hide();
        }

        public void Show()
        {
            Enabled = true;
            Visible = true;
            AlphaRate = ALPHA_RATE;
            Alpha = 0.01f;

            foreach (XNAControl child in Children)
            {
                child.Enabled = true;
                child.Visible = true;
            }
        }

        public void Hide()
        {
            AlphaRate = -ALPHA_RATE;

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

        public static void AddAndInitializeWithControl(WindowManager wm, XNAControl control)
        {
            var dp = new DarkeningPanel(wm);
            wm.AddAndInitializeControl(dp);
            dp.AddChild(control);
        }
    }
}
