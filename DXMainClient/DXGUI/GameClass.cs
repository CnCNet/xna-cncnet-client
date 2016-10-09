using ClientCore;
using DTAClient.Domain;
using DTAClient.DXGUI.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System;
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
            content = new ContentManager(Services);
        }

        GraphicsDeviceManager graphics;
        ContentManager content;

        protected override void Initialize()
        {
            base.Initialize();

            AssetLoader.Initialize(GraphicsDevice, content);
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetBaseResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GamePath);

            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / 120.0);

            InitializeUISettings();

            WindowManager wm = new WindowManager(this, graphics);
            wm.Initialize(content, ProgramConstants.GetBaseResourcePath());

            int windowWidth = UserINISettings.Instance.ClientResolutionX;
            int windowHeight = UserINISettings.Instance.ClientResolutionY;

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

            int renderResolutionX = Math.Max(windowWidth, MCDomainController.Instance.MinimumRenderWidth);
            int renderResolutionY = Math.Max(windowHeight, MCDomainController.Instance.MinimumRenderHeight);

            renderResolutionX = Math.Min(renderResolutionX, MCDomainController.Instance.MaximumRenderWidth);
            renderResolutionY = Math.Min(renderResolutionY, MCDomainController.Instance.MaximumRenderHeight);

            wm.SetBorderlessMode(UserINISettings.Instance.BorderlessWindowedClient);
            wm.CenterOnScreen();
            wm.SetRenderResolution(renderResolutionX, renderResolutionY);
            wm.SetIcon(ProgramConstants.GetBaseResourcePath() + "clienticon.ico");
            wm.SetWindowTitle(MainClientConstants.GAME_NAME_SHORT + " Client");
            wm.SetControlBox(true);

            wm.Cursor.Textures = new Texture2D[]
            {
                AssetLoader.LoadTexture("cursor.png"),
                AssetLoader.LoadTexture("waitCursor.png")
            };

            Components.Add(wm);

            string playerName = UserINISettings.Instance.PlayerName.Value.Trim();

            if (string.IsNullOrEmpty(playerName))
            {
                playerName = WindowsIdentity.GetCurrent().Name;

                playerName = playerName.Substring(playerName.IndexOf("\\") + 1);
            }

            playerName = playerName.Replace(",", string.Empty);
            playerName = Renderer.GetSafeString(playerName, 0);
            playerName.Trim();
            int maxNameLength = DomainController.Instance().MaxNameLength;
            if (playerName.Length > maxNameLength)
                playerName = playerName.Substring(0, maxNameLength);

            ProgramConstants.PLAYERNAME = playerName;

            LoadingScreen ls = new LoadingScreen(wm);
            wm.AddAndInitializeControl(ls);
            ls.ClientRectangle = new Rectangle((renderResolutionX - ls.ClientRectangle.Width) / 2,
                (renderResolutionY - ls.ClientRectangle.Height) / 2, ls.ClientRectangle.Width, ls.ClientRectangle.Height);
        }

        private void InitializeUISettings()
        {
            UISettings.AltColor = AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltColor());
            UISettings.SubtleTextColor = AssetLoader.GetColorFromString(DomainController.Instance().GetUIHintTextColor());
            UISettings.ButtonColor = UISettings.AltColor;
            UISettings.ButtonHoverColor = AssetLoader.GetColorFromString(DomainController.Instance().GetButtonHoverColor());
            UISettings.TextColor = AssetLoader.GetColorFromString(DomainController.Instance().GetUILabelColor());
            UISettings.WindowBorderColor = AssetLoader.GetColorFromString(DomainController.Instance().GetWindowBorderColor());
            UISettings.PanelBorderColor = AssetLoader.GetColorFromString(DomainController.Instance().GetPanelBorderColor());
            UISettings.BackgroundColor = AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor());
            UISettings.FocusColor = AssetLoader.GetColorFromString(DomainController.Instance().GetListBoxFocusColor());
            UISettings.DisabledButtonColor = AssetLoader.GetColorFromString(DomainController.Instance().GetButtonDisabledColor());

            UISettings.DefaultAlphaRate = DomainController.Instance().GetDefaultAlphaRate();
            UISettings.CheckBoxAlphaRate = DomainController.Instance().GetCheckBoxAlphaRate();

            UISettings.CheckBoxClearTexture = AssetLoader.LoadTexture("checkBoxClear.png");
            UISettings.CheckBoxCheckedTexture = AssetLoader.LoadTexture("checkBoxChecked.png");
            UISettings.CheckBoxDisabledClearTexture = AssetLoader.LoadTexture("checkBoxClearD.png");
            UISettings.CheckBoxDisabledCheckedTexture = AssetLoader.LoadTexture("checkBoxCheckedD.png");
        }
    }
}
