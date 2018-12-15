using ClientCore;
using DTAClient.Domain;
using DTAClient.DXGUI.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
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
            string windowTitle = ClientConfiguration.Instance.WindowTitle;
            Window.Title = string.IsNullOrEmpty(windowTitle) ?
                string.Format("{0} Client", MainClientConstants.GAME_NAME_SHORT) : windowTitle;
        }

        GraphicsDeviceManager graphics;
        ContentManager content;

        protected override void Initialize()
        {
            Logger.Log("Initializing GameClass.");

            base.Initialize();

            string primaryNativeCursorPath = ProgramConstants.GetResourcePath() + "cursor.cur";
            string alternativeNativeCursorPath = ProgramConstants.GetBaseResourcePath() + "cursor.cur";

            AssetLoader.Initialize(GraphicsDevice, content);
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetBaseResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GamePath);

            InitializeUISettings();

            WindowManager wm = new WindowManager(this, graphics);
            wm.Initialize(content, ProgramConstants.GetBaseResourcePath());

            int windowWidth = UserINISettings.Instance.ClientResolutionX;
            int windowHeight = UserINISettings.Instance.ClientResolutionY;

            bool borderlessWindowedClient = UserINISettings.Instance.BorderlessWindowedClient;

            if (Screen.PrimaryScreen.Bounds.Width >= windowWidth && Screen.PrimaryScreen.Bounds.Height >= windowHeight)
            {
                if (!wm.InitGraphicsMode(windowWidth, windowHeight, false))
                    throw new Exception("Setting graphics mode failed! " + windowWidth + "x" + windowHeight);
            }
            else
            {
                if (!wm.InitGraphicsMode(1024, 600, false))
                    throw new Exception("Setting default graphics mode failed!");
            }

            int renderResolutionX = Math.Max(windowWidth, ClientConfiguration.Instance.MinimumRenderWidth);
            int renderResolutionY = Math.Max(windowHeight, ClientConfiguration.Instance.MinimumRenderHeight);

            renderResolutionX = Math.Min(renderResolutionX, ClientConfiguration.Instance.MaximumRenderWidth);
            renderResolutionY = Math.Min(renderResolutionY, ClientConfiguration.Instance.MaximumRenderHeight);

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

            playerName = playerName.Replace(",", string.Empty);
            playerName = Renderer.GetSafeString(playerName, 0);
            playerName.Trim();
            int maxNameLength = ClientConfiguration.Instance.MaxNameLength;
            if (playerName.Length > maxNameLength)
                playerName = playerName.Substring(0, maxNameLength);

            ProgramConstants.PLAYERNAME = playerName;
            UserINISettings.Instance.PlayerName.Value = playerName;

            LoadingScreen ls = new LoadingScreen(wm);
            wm.AddAndInitializeControl(ls);
            ls.ClientRectangle = new Rectangle((renderResolutionX - ls.Width) / 2,
                (renderResolutionY - ls.Height) / 2, ls.Width, ls.Height);
        }

        private void InitializeUISettings()
        {
            UISettings.AltColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIColor);
            UISettings.SubtleTextColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.UIHintTextColor);
            UISettings.ButtonColor = UISettings.AltColor;
            UISettings.ButtonHoverColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ButtonHoverColor);
            UISettings.TextColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.UILabelColor);
            UISettings.WindowBorderColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.WindowBorderColor);
            UISettings.PanelBorderColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.PanelBorderColor);
            UISettings.BackgroundColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor);
            UISettings.FocusColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ListBoxFocusColor);
            UISettings.DisabledButtonColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.DisabledButtonColor);

            UISettings.DefaultAlphaRate = ClientConfiguration.Instance.DefaultAlphaRate;
            UISettings.CheckBoxAlphaRate = ClientConfiguration.Instance.CheckBoxAlphaRate;

            UISettings.CheckBoxClearTexture = AssetLoader.LoadTexture("checkBoxClear.png");
            UISettings.CheckBoxCheckedTexture = AssetLoader.LoadTexture("checkBoxChecked.png");
            UISettings.CheckBoxDisabledClearTexture = AssetLoader.LoadTexture("checkBoxClearD.png");
            UISettings.CheckBoxDisabledCheckedTexture = AssetLoader.LoadTexture("checkBoxCheckedD.png");
        }
    }
}
