using ClientCore;
using ClientCore.CnCNet5;
using DTAClient.Domain;
using DTAClient.DXGUI.Generic;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace DTAClient.DXGUI
{
    /// <summary>
    /// The main class for the game. Sets up asset search paths
    /// and initializes components.
    /// </summary>
    public class GameClass : Game
    {
        public GameClass()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.SynchronizeWithVerticalRetrace = false;
#if !XNA
            graphics.HardwareModeSwitch = false;
#endif
            content = new ContentManager(Services);
        }

        private static GraphicsDeviceManager graphics;
        ContentManager content;

        protected override void Initialize()
        {
            Logger.Log("Initializing GameClass.");

            string windowTitle = ClientConfiguration.Instance.WindowTitle;
            Window.Title = string.IsNullOrEmpty(windowTitle) ?
                string.Format("{0} Client", MainClientConstants.GAME_NAME_SHORT) : windowTitle;

            base.Initialize();

            string primaryNativeCursorPath = ProgramConstants.GetResourcePath() + "cursor.cur";
            string alternativeNativeCursorPath = ProgramConstants.GetBaseResourcePath() + "cursor.cur";

            AssetLoader.Initialize(GraphicsDevice, content);
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetBaseResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GamePath);

#if !XNA && !WINDOWSGL
            // Try to create and load a texture to check for MonoGame 3.7.1 compatibility
            try
            {
                Texture2D texture = new Texture2D(GraphicsDevice, 100, 100, false, SurfaceFormat.Color);
                Color[] colorArray = new Color[100 * 100];
                texture.SetData(colorArray);

                UISettings.ActiveSettings.CheckBoxClearTexture = AssetLoader.LoadTextureUncached("checkBoxClear.png");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("DeviceRemoved"))
                {
                    Logger.Log("Creating texture on startup failed! Creating .dxfail file and re-launching client launcher.");

                    if (!Directory.Exists(ProgramConstants.GamePath + "Client"))
                        Directory.CreateDirectory(ProgramConstants.GamePath + "Client");

                    // Create .dxfail file that the launcher can check for this error
                    // and handle it by redirecting the user to the XNA version instead

                    File.WriteAllBytes(ProgramConstants.GamePath + "Client" + Path.DirectorySeparatorChar + ".dxfail",
                        new byte[] { 1 });

                    string launcherExe = ClientConfiguration.Instance.LauncherExe;
                    if (string.IsNullOrEmpty(launcherExe))
                    {
                        // LauncherExe is unspecified, just throw the exception forward
                        // because we can't handle it

                        Logger.Log("No LauncherExe= specified in ClientDefinitions.ini! " +
                            "Forwarding exception to regular exception handler.");

                        throw ex;
                    }
                    else
                    {
                        Logger.Log("Starting " + launcherExe + " and exiting.");

                        Process.Start(ProgramConstants.GamePath + launcherExe);
                        Environment.Exit(0);
                    }
                }
            }
#endif

            InitializeUISettings();

            WindowManager wm = new WindowManager(this, graphics);
            wm.Initialize(content, ProgramConstants.GetBaseResourcePath());

            SetGraphicsMode(wm);

            wm.SetIcon(ProgramConstants.GetBaseResourcePath() + "clienticon.ico");

            wm.SetControlBox(true);

            wm.Cursor.Textures = new Texture2D[]
            {
                AssetLoader.LoadTexture("cursor.png"),
                AssetLoader.LoadTexture("waitCursor.png")
            };

            if (File.Exists(primaryNativeCursorPath))
                wm.Cursor.LoadNativeCursor(primaryNativeCursorPath);
            else if (File.Exists(alternativeNativeCursorPath))
                wm.Cursor.LoadNativeCursor(alternativeNativeCursorPath);

            Components.Add(wm);

            string playerName = UserINISettings.Instance.PlayerName.Value.Trim();
            
            if (UserINISettings.Instance.AutoRemoveUnderscoresFromName)
            {
                while (playerName.EndsWith("_"))
                    playerName = playerName.Substring(0, playerName.Length - 1);
            }

            if (string.IsNullOrEmpty(playerName))
            {
                playerName = WindowsIdentity.GetCurrent().Name;

                playerName = playerName.Substring(playerName.IndexOf("\\") + 1);
            }

            playerName = Renderer.GetSafeString(NameValidator.GetValidOfflineName(playerName), 0);

            ProgramConstants.PLAYERNAME = playerName;
            UserINISettings.Instance.PlayerName.Value = playerName;

            LoadingScreen ls = new LoadingScreen(wm);
            wm.AddAndInitializeControl(ls);
            ls.ClientRectangle = new Rectangle((wm.RenderResolutionX - ls.Width) / 2,
                (wm.RenderResolutionY - ls.Height) / 2, ls.Width, ls.Height);
        }

        private void InitializeUISettings()
        {
            UISettings settings = new UISettings();

            settings.AltColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIColor);
            settings.SubtleTextColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.UIHintTextColor);
            settings.ButtonTextColor = settings.AltColor;
            settings.ButtonHoverColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ButtonHoverColor);
            settings.TextColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.UILabelColor);
            //settings.WindowBorderColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.WindowBorderColor);
            settings.PanelBorderColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.PanelBorderColor);
            settings.BackgroundColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor);
            settings.FocusColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ListBoxFocusColor);
            settings.DisabledItemColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.DisabledButtonColor);
            
            settings.DefaultAlphaRate = ClientConfiguration.Instance.DefaultAlphaRate;
            settings.CheckBoxAlphaRate = ClientConfiguration.Instance.CheckBoxAlphaRate;
            
            settings.CheckBoxClearTexture = AssetLoader.LoadTexture("checkBoxClear.png");
            settings.CheckBoxCheckedTexture = AssetLoader.LoadTexture("checkBoxChecked.png");
            settings.CheckBoxDisabledClearTexture = AssetLoader.LoadTexture("checkBoxClearD.png");
            settings.CheckBoxDisabledCheckedTexture = AssetLoader.LoadTexture("checkBoxCheckedD.png");

            UISettings.ActiveSettings = settings;
        }

        /// <summary>
        /// Sets the client's graphics mode.
        /// TODO move to some helper class?
        /// </summary>
        /// <param name="wm">The window manager</param>
        public static void SetGraphicsMode(WindowManager wm)
        {
            var clientConfiguration = ClientConfiguration.Instance;

            int windowWidth = UserINISettings.Instance.ClientResolutionX;
            int windowHeight = UserINISettings.Instance.ClientResolutionY;

            bool borderlessWindowedClient = UserINISettings.Instance.BorderlessWindowedClient;

            if (Screen.PrimaryScreen.Bounds.Width >= windowWidth && Screen.PrimaryScreen.Bounds.Height >= windowHeight)
            {
                if (!wm.InitGraphicsMode(windowWidth, windowHeight, false))
                    throw new GraphicsModeInitializationException("Setting graphics mode failed!".L10N("UI:Main:SettingGraphicModeFailed") + " " + windowWidth + "x" + windowHeight);
            }
            else
            {
                if (!wm.InitGraphicsMode(1024, 600, false))
                    throw new GraphicsModeInitializationException("Setting default graphics mode failed!".L10N("UI:Main:SettingDefaultGraphicModeFailed"));
            }

            int renderResolutionX = 0;
            int renderResolutionY = 0;

            int initialXRes = Math.Max(windowWidth, clientConfiguration.MinimumRenderWidth);
            initialXRes = Math.Min(initialXRes, clientConfiguration.MaximumRenderWidth);

            int initialYRes = Math.Max(windowHeight, clientConfiguration.MinimumRenderHeight);
            initialYRes = Math.Min(initialYRes, clientConfiguration.MaximumRenderHeight);

            double xRatio = (windowWidth) / (double)initialXRes;
            double yRatio = (windowHeight) / (double)initialYRes;

            double ratio = xRatio > yRatio ? yRatio : xRatio;

            if ((windowWidth == 1366 || windowWidth == 1360) && windowHeight == 768)
            {
                renderResolutionX = windowWidth;
                renderResolutionY = windowHeight;
            }

            if (ratio > 1.0)
            {
                // Check whether we could sharp-scale our client window
                for (int i = 2; i < 10; i++)
                {
                    int sharpScaleRenderResX = windowWidth / i;
                    int sharpScaleRenderResY = windowHeight / i;

                    if (sharpScaleRenderResX >= clientConfiguration.MinimumRenderWidth &&
                        sharpScaleRenderResX <= clientConfiguration.MaximumRenderWidth &&
                        sharpScaleRenderResY >= clientConfiguration.MinimumRenderHeight &&
                        sharpScaleRenderResY <= clientConfiguration.MaximumRenderHeight)
                    {
                        renderResolutionX = sharpScaleRenderResX;
                        renderResolutionY = sharpScaleRenderResY;
                        break;
                    }
                }
            }

            if (renderResolutionX == 0 || renderResolutionY == 0)
            {
                renderResolutionX = initialXRes;
                renderResolutionY = initialYRes;

                if (ratio == xRatio)
                    renderResolutionY = (int)(windowHeight / ratio);
            }

            wm.SetBorderlessMode(borderlessWindowedClient);
#if !XNA
            if (borderlessWindowedClient)
            {
                graphics.IsFullScreen = true;
                graphics.ApplyChanges();
            }
#endif
            wm.CenterOnScreen();
            wm.SetRenderResolution(renderResolutionX, renderResolutionY);
        }
    }

    /// <summary>
    /// An exception that is thrown when initializing display / graphics mode fails.
    /// </summary>
    class GraphicsModeInitializationException : Exception
    {
        public GraphicsModeInitializationException(string message) : base(message)
        {
        }
    }
}
