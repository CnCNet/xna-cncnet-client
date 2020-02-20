using ClientCore;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI
{
    /// <summary>
    /// A tool tip.
    /// </summary>
    public class ToolTip : XNAControl
    {
        /// <summary>
        /// Creates a new tool tip and attaches it to the given control.
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        /// <param name="masterControl">The control to attach the tool tip to.</param>
        public ToolTip(WindowManager windowManager, XNAControl masterControl) : base(windowManager)
        {
            this.masterControl = masterControl ?? throw new ArgumentNullException("masterControl");
            masterControl.MouseEnter += MasterControl_MouseEnter;
            masterControl.MouseLeave += MasterControl_MouseLeave;
            masterControl.MouseMove += MasterControl_MouseMove;
            masterControl.EnabledChanged += MasterControl_EnabledChanged;
            InputEnabled = false;
            DrawOrder = int.MinValue;
            // TODO: adding tool tips as root-level controls might be CPU-intensive.
            // instead we could find out the root-level parent and only have the tooltip
            // in the window manager's list when the root-level parent is visible.
            WindowManager.AddControl(this);
            Visible = false;
        }

        private void MasterControl_EnabledChanged(object sender, EventArgs e)
            => Enabled = masterControl.Enabled;

        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                Vector2 textSize = Renderer.GetTextDimensions(base.Text, ClientConfiguration.Instance.ToolTipFontIndex);
                Width = (int)textSize.X + ClientConfiguration.Instance.ToolTipMargin * 2;
                Height = (int)textSize.Y + ClientConfiguration.Instance.ToolTipMargin * 2;
            }
        }

        public override float Alpha { get; set; }
        public bool IsMasterControlOnCursor { get; set; }

        private XNAControl masterControl;

        private TimeSpan cursorTime = TimeSpan.Zero;
        

        private void MasterControl_MouseEnter(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            DisplayAtLocation(SumPoints(WindowManager.Cursor.Location,
                new Point(ClientConfiguration.Instance.ToolTipOffsetX, ClientConfiguration.Instance.ToolTipOffsetY)));
            IsMasterControlOnCursor = true;
        }

        private void MasterControl_MouseLeave(object sender, EventArgs e)
        {
            IsMasterControlOnCursor = false;
            cursorTime = TimeSpan.Zero;
        }

        private void MasterControl_MouseMove(object sender, EventArgs e)
        {
            if (!Visible && !string.IsNullOrEmpty(Text))
            {
                // Move the tooltip if the cursor has moved while staying 
                // on the control area and we're invisible
                DisplayAtLocation(SumPoints(WindowManager.Cursor.Location,
                    new Point(ClientConfiguration.Instance.ToolTipOffsetX, ClientConfiguration.Instance.ToolTipOffsetY)));
            }
        }

        /// <summary>
        /// Sets the tool tip's location, checking that it doesn't exceed the window's bounds.
        /// </summary>
        /// <param name="location">The point at location coordinates.</param>
        public void DisplayAtLocation(Point location)
        {
            X = location.X + Width > WindowManager.RenderResolutionX ?
                WindowManager.RenderResolutionX - Width : location.X;
            Y = location.Y - Height < 0 ? 0 : location.Y - Height;
        }

        public override void Update(GameTime gameTime)
        {
            if (IsMasterControlOnCursor)
            {
                cursorTime += gameTime.ElapsedGameTime;

                if (cursorTime > TimeSpan.FromSeconds(ClientConfiguration.Instance.ToolTipDelay))
                {
                    Alpha += ClientConfiguration.Instance.ToolTipAlphaRatePerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    Visible = true;
                    if (Alpha > 1.0f)
                        Alpha = 1.0f;
                    return;
                }
            }

            Alpha -= ClientConfiguration.Instance.ToolTipAlphaRatePerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (Alpha < 0f)
            {
                Alpha = 0f;
                Visible = false;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Renderer.FillRectangle(ClientRectangle,
                ColorFromAlpha(UISettings.ActiveSettings.BackgroundColor));
            Renderer.DrawRectangle(ClientRectangle,
                ColorFromAlpha(UISettings.ActiveSettings.AltColor));
            Renderer.DrawString(Text, ClientConfiguration.Instance.ToolTipFontIndex,
                new Vector2(X + ClientConfiguration.Instance.ToolTipMargin, Y + ClientConfiguration.Instance.ToolTipMargin),
                ColorFromAlpha(UISettings.ActiveSettings.AltColor), 1.0f);
        }

        private Color ColorFromAlpha(Color color)
            // This is necessary because XNA lacks the color constructor that
            // takes a color and a float value for alpha.
#if XNA
            => new Color(color.R, color.G, color.B, (int)(Alpha * 255.0f));
#else
            => new Color(color, Alpha);
#endif

        private Point SumPoints(Point p1, Point p2)
            // This is also needed for XNA compatibility
#if XNA
            => new Point(p1.X + p2.X, p1.Y + p2.Y);
#else
            => p1 + p2;
#endif
    }
}
