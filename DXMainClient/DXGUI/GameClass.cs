using ClientCore;
using DTAClient.domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Cursor = Rampastring.XNAUI.Cursor;

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
            content = new ContentManager(Services);
        }

        GraphicsDeviceManager graphics;
        ContentManager content;

        protected override void Initialize()
        {
            base.Initialize();

            AssetLoader.Initialize(GraphicsDevice);
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetBaseResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.gamepath);

            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / 120.0);

            InitializeUISettings();

            WindowManager wm = new WindowManager(this, graphics);
            wm.Initialize(content, ProgramConstants.GetBaseResourcePath());

            int windowWidth = MCDomainController.Instance.GetClientResolutionX();
            int windowHeight = MCDomainController.Instance.GetClientResolutionY();

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

            int renderResolutionX = Math.Max(windowWidth, MCDomainController.Instance.GetMinimumRenderWidth());
            int renderResolutionY = Math.Max(windowHeight, MCDomainController.Instance.GetMinimumRenderHeight());

            wm.SetBorderlessMode(MCDomainController.Instance.GetBorderlessWindowedStatus());
            wm.CenterOnScreen();
            wm.SetRenderResolution(renderResolutionX, renderResolutionY);
            wm.SetIcon(ProgramConstants.GetBaseResourcePath() + "mainclienticon.ico");
            wm.SetWindowTitle(MainClientConstants.GAME_NAME_LONG);
            wm.SetControlBox(false);

            wm.Cursor.Textures = new Texture2D[]
            {
                AssetLoader.LoadTexture("cursor.png"),
                AssetLoader.LoadTexture("waitCursor.png")
            };

            Components.Add(wm);

            LoadingScreen ls = new LoadingScreen(this, wm);
            wm.AddAndInitializeControl(ls);
            ls.ClientRectangle = new Rectangle((renderResolutionX - ls.ClientRectangle.Width) / 2,
                (renderResolutionY - ls.ClientRectangle.Height) / 2, ls.ClientRectangle.Width, ls.ClientRectangle.Height);
            ls.Start();
        }

        public void InitializeUISettings()
        {
            UISettings.AltColor = AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltColor());
            UISettings.ButtonColor = UISettings.AltColor;
            UISettings.TextColor = AssetLoader.GetColorFromString(DomainController.Instance().GetUILabelColor());
            UISettings.WindowBorderColor = AssetLoader.GetColorFromString(DomainController.Instance().GetWindowBorderColor());
            UISettings.PanelBorderColor = AssetLoader.GetColorFromString(DomainController.Instance().GetPanelBorderColor());
            UISettings.BackgroundColor = AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor());
            UISettings.FocusColor = AssetLoader.GetColorFromString(DomainController.Instance().GetListBoxFocusColor());

            UISettings.DefaultAlphaRate = DomainController.Instance().GetDefaultAlphaRate();

            UISettings.CheckBoxClearTexture = AssetLoader.LoadTexture("checkBoxClear.png");
            UISettings.CheckBoxCheckedTexture = AssetLoader.LoadTexture("checkBoxChecked.png");
            UISettings.CheckBoxDisabledClearTexture = AssetLoader.LoadTexture("checkBoxClearD.png");
            UISettings.CheckBoxDisabledCheckedTexture = AssetLoader.LoadTexture("checkBoxCheckedD.png");
        }
    }
}
