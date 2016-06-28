using ClientCore;
using ClientGUI.DXControls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClientGUI.DirectX
{
    /// <summary>
    /// The class responsible for handling the game window and the game loop.
    /// </summary>
    public class WindowManager : Game
    {
        const double FRAME_RATE = 120.0;

        public WindowManager()
        {
            graphics = new GraphicsDeviceManager(this);
        }

        public static WindowManager Instance;

        ContentManager content;

        public List<DXControl> Controls = new List<DXControl>();

        public DXControl FirstControl { get; set; }

        public bool IsBorderlessWindowed { get; set; }

        GraphicsDeviceManager graphics;

        RenderTarget2D renderTarget;

        public int SceneXPosition = 0;

        int renderResX = 800;
        int renderResY = 600;

        int resolutionWidth = 800;
        int resolutionHeight = 600;
        public int ResolutionWidth
        {
            get { return resolutionWidth; }
        }

        public int ResolutionHeight
        {
            get { return resolutionHeight; }
        }

        double scaleRatio = 1.0;
        public double ScaleRatio
        {
            get { return scaleRatio; }
            set { scaleRatio = value; }
        }

        public void SetRenderResolution(int x, int y)
        {
            renderResX = x;
            renderResY = y;

            double intendedRatio = renderResX / (double)renderResY;
            double xyRatio = resolutionWidth / (double)resolutionHeight;

            double ratioDifference = xyRatio - intendedRatio;

            SceneXPosition = (int)(ratioDifference * resolutionHeight) / 2;
            scaleRatio = resolutionHeight / (double)renderResY;

            renderTarget = new RenderTarget2D(GraphicsDevice, renderResX, renderResY, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        public int RenderResolutionX
        {
            get { return renderResX; }
        }

        public int RenderResolutionY
        {
            get { return renderResY; }
        }

        public void AddControl(DXControl control)
        {
            control.Initialize();
        }

        protected override void Initialize()
        {
            graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / FRAME_RATE);

            base.Initialize();

            content = new ContentManager(Services);

            AssetLoader.Initialize(GraphicsDevice);
            Renderer.Initialize(GraphicsDevice, content);

            Cursor cursor = new Cursor(this);
            cursor.Initialize();
            cursor.DrawOrder = 20000;

            RKeyboard keyboard = new RKeyboard(this);
            keyboard.Initialize();
            Components.Add(keyboard);

            Instance = this;

            FirstControl.Initialize();
            Controls.Add(FirstControl);
        }

        /// <summary>
        /// Attempt to set the display mode to the desired resolution.  Itterates through the display
        /// capabilities of the default graphics adapter to determine if the graphics adapter supports the
        /// requested resolution.  If so, the resolution is set and the function returns true.  If not,
        /// no change is made and the function returns false.
        /// </summary>
        /// <param name="iWidth">Desired screen width.</param>
        /// <param name="iHeight">Desired screen height.</param>
        /// <param name="bFullScreen">True if you wish to go to Full Screen, false for Windowed Mode.</param>
        public bool InitGraphicsMode(int iWidth, int iHeight, bool bFullScreen)
        {
            Logger.Log("InitGraphicsMode: " + iWidth + "x" + iHeight);
            resolutionWidth = iWidth;
            resolutionHeight = iHeight;
            // If we aren't using a full screen mode, the height and width of the window can
            // be set to anything equal to or smaller than the actual screen size.
            if (bFullScreen == false)
            {
                if ((iWidth <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width)
                    && (iHeight <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height))
                {
                    graphics.PreferredBackBufferWidth = iWidth;
                    graphics.PreferredBackBufferHeight = iHeight;
                    graphics.IsFullScreen = bFullScreen;
                    graphics.ApplyChanges();
                    return true;
                }
            }
            else
            {
                // If we are using full screen mode, we should check to make sure that the display
                // adapter can handle the video mode we are trying to set.  To do this, we will
                // iterate thorugh the display modes supported by the adapter and check them against
                // the mode we want to set.
                foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                {
                    // Check the width and height of each mode against the passed values
                    if ((dm.Width == iWidth) && (dm.Height == iHeight))
                    {
                        // The mode is supported, so set the buffer formats, apply changes and return
                        graphics.PreferredBackBufferWidth = iWidth;
                        graphics.PreferredBackBufferHeight = iHeight;
                        graphics.IsFullScreen = bFullScreen;
                        graphics.ApplyChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        public void CenterOnScreen()
        {
            Form gameForm = (Form)Form.FromHandle(this.Window.Handle);
            gameForm.FormBorderStyle = FormBorderStyle.None;
            gameForm.DesktopLocation = new System.Drawing.Point(
                (Screen.PrimaryScreen.Bounds.Width - gameForm.Width) / 2,
                (Screen.PrimaryScreen.Bounds.Height - gameForm.Height) / 2); 
        }

        public void HideWindow()
        {
            Form gameForm = (Form)Form.FromHandle(Window.Handle);
            gameForm.Hide();
        }

        public void ShowWindow()
        {
            Form gameForm = (Form)Form.FromHandle(Window.Handle);
            gameForm.Show();
        }

        public void MinimizeWindow()
        {
            Form gameForm = (Form)Form.FromHandle(Window.Handle);
            gameForm.WindowState = FormWindowState.Minimized;
        }

        public void MaximizeWindow()
        {
            Form gameForm = (Form)Form.FromHandle(Window.Handle);
            gameForm.WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// Lowers the tick rate of the game loop.
        /// Use to avoid wasting processing power for the client while the main executable is running.
        /// </summary>
        public void EnableEnergySavingMode()
        {
            TargetElapsedTime = TimeSpan.FromMilliseconds(100.0);
        }

        /// <summary>
        /// Sets the tick rate of the game loop to its normal rate.
        /// </summary>
        public void DisableEnergySavingMode()
        {
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / FRAME_RATE);
        }

        public RenderTarget2D GetFinalRenderTarget()
        {
            return renderTarget;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            bool hasFocus = (System.Windows.Forms.Form.ActiveForm != null);
            Cursor.Instance().HasFocus = hasFocus;
            RKeyboard.Instance().HasFocus = hasFocus;

            if (IsBorderlessWindowed)
            {
                Form gameForm = (Form)Form.FromHandle(Window.Handle);
                gameForm.TopMost = hasFocus;
            }

            Cursor.Instance().Update(gameTime);

            for (int i = 0; i < Controls.Count; i++)
            {
                if (Controls[i].Enabled)
                    Controls[i].Update(gameTime);

                if (Controls[i].Killed)
                {
                    Controls.RemoveAt(i);
                    i--;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            GraphicsDevice.SetRenderTarget(renderTarget);

            GraphicsDevice.Clear(Color.Black);

            Renderer.BeginDraw();

            for (int i = 0; i < Controls.Count; i++)
            {
                if (Controls[i].Visible)
                    Controls[i].Draw(gameTime);
            }

            Cursor.Instance().Draw(gameTime);

            Renderer.EndDraw();

            GraphicsDevice.SetRenderTarget(null);

            GraphicsDevice.Clear(Color.Black);

            Renderer.BeginDraw();

            Renderer.DrawTexture(renderTarget, new Rectangle(SceneXPosition, 0, resolutionWidth - (SceneXPosition * 2), resolutionHeight), Color.White);

            Renderer.EndDraw();
        }
    }
}
