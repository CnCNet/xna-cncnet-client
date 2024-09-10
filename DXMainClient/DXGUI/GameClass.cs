using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.DXGUI.Generic;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using DTAConfig;
using DTAConfig.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rampastring.XNAUI.XNAControls;
using MainMenu = DTAClient.DXGUI.Generic.MainMenu;
#if DX || (GL && WINFORMS)
using System.Diagnostics;
using System.IO;
#endif
#if WINFORMS
using System.Windows.Forms;
using System.IO;
#endif

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

            // Enable HiDef on a large monitor.
            if (!ScreenResolution.HiDefLimitResolution.Fit(ScreenResolution.DesktopResolution))
            {
                // Enabling HiDef profile drops legacy GPUs not supporting DirectX 10.
                // In practice, it's recommended to have a DirectX 11 capable GPU.
                graphics.GraphicsProfile = GraphicsProfile.HiDef;
            }
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

            AssetLoader.Initialize(GraphicsDevice, content);
            AssetLoader.AssetSearchPaths.Add(UserINISettings.Instance.TranslationThemeFolderPath);
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetResourcePath());
            AssetLoader.AssetSearchPaths.Add(UserINISettings.Instance.TranslationFolderPath);
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GetBaseResourcePath());
            AssetLoader.AssetSearchPaths.Add(ProgramConstants.GamePath);

#if DX || (GL && WINFORMS)
            // Try to create and load a texture to check for MonoGame compatibility
#if DX
            const string startupFailureFile = ".dxfail";
#elif GL && WINFORMS
            const string startupFailureFile = ".oglfail";
#endif

            try
            {
                Texture2D texture = new Texture2D(GraphicsDevice, 100, 100, false, SurfaceFormat.Color);
                Color[] colorArray = new Color[100 * 100];
                texture.SetData(colorArray);

                _ = AssetLoader.LoadTextureUncached("checkBoxClear.png");
            }
            catch (Exception ex)
            {
                // TODO Get English exception message
                if (ex.Message.Contains("DeviceRemoved"))
                {
                    Logger.Log($"Creating texture on startup failed! Creating {startupFailureFile} file and re-launching client launcher.");

                    DirectoryInfo clientDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);

                    if (!clientDirectory.Exists)
                        clientDirectory.Create();

                    // Create startup failure file that the launcher can check for this error
                    // and handle it by redirecting the user to another version instead

                    File.WriteAllBytes(SafePath.CombineFilePath(clientDirectory.FullName, startupFailureFile), new byte[] { 1 });

                    string launcherExe = ClientConfiguration.Instance.LauncherExe;
                    if (string.IsNullOrEmpty(launcherExe))
                    {
                        // LauncherExe is unspecified, just throw the exception forward
                        // because we can't handle it

                        Logger.Log("No LauncherExe= specified in ClientDefinitions.ini! " +
                            "Forwarding exception to regular exception handler.");

                        throw;
                    }
                    else
                    {
                        Logger.Log("Starting " + launcherExe + " and exiting.");

                        Process.Start(SafePath.CombineFilePath(ProgramConstants.GamePath, launcherExe));
                        Environment.Exit(1);
                    }
                }
            }

#endif
            InitializeUISettings();

            WindowManager wm = new WindowManager(this, graphics);
            wm.Initialize(content, ProgramConstants.GetBaseResourcePath());

            wm.ControlINIAttributeParsers.Add(new TranslationINIParser());

            MainClientConstants.DisplayErrorAction = (title, error, exit) =>
            {
                new XNAMessageBox(wm, title, error, XNAMessageBoxButtons.OK)
                {
                    OKClickedAction = _ =>
                    {
                        if (exit)
                            Environment.Exit(1);
                    }
                }.Show();
            };

            SetGraphicsMode(wm);
#if WINFORMS

            wm.SetIcon(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clienticon.ico"));
            wm.SetControlBox(true);
#endif

            wm.Cursor.Textures = new Texture2D[]
            {
                AssetLoader.LoadTexture("cursor.png"),
                AssetLoader.LoadTexture("waitCursor.png")
            };

#if WINFORMS
            FileInfo primaryNativeCursorPath = SafePath.GetFile(ProgramConstants.GetResourcePath(), "cursor.cur");
            FileInfo alternativeNativeCursorPath = SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), "cursor.cur");

            if (primaryNativeCursorPath.Exists)
                wm.Cursor.LoadNativeCursor(primaryNativeCursorPath.FullName);
            else if (alternativeNativeCursorPath.Exists)
                wm.Cursor.LoadNativeCursor(alternativeNativeCursorPath.FullName);

#endif
            Components.Add(wm);

            string playerName = UserINISettings.Instance.PlayerName.Value.Trim();

            if (UserINISettings.Instance.AutoRemoveUnderscoresFromName)
            {
                while (playerName.EndsWith("_"))
                    playerName = playerName.Substring(0, playerName.Length - 1);
            }

            if (string.IsNullOrEmpty(playerName))
            {
                playerName = Environment.UserName;

                playerName = playerName.Substring(playerName.IndexOf("\\") + 1);
            }

            playerName = Renderer.GetSafeString(NameValidator.GetValidOfflineName(playerName), 0);

            ProgramConstants.PLAYERNAME = playerName;
            UserINISettings.Instance.PlayerName.Value = playerName;

            IServiceProvider serviceProvider = BuildServiceProvider(wm);
            LoadingScreen ls = serviceProvider.GetService<LoadingScreen>();
            wm.AddAndInitializeControl(ls);
            ls.ClientRectangle = new Rectangle((wm.RenderResolutionX - ls.Width) / 2,
                (wm.RenderResolutionY - ls.Height) / 2, ls.Width, ls.Height);
        }

        private IServiceProvider BuildServiceProvider(WindowManager windowManager)
        {
            // Create host - this allows for things like DependencyInjection
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                    {
                        // services (or service-like)
                        services
                            .AddSingleton<ServiceProvider>()
                            .AddSingleton(windowManager)
                            .AddSingleton(GraphicsDevice)
                            .AddSingleton<GameCollection>()
                            .AddSingleton<CnCNetUserData>()
                            .AddSingleton<CnCNetManager>()
                            .AddSingleton<TunnelHandler>()
                            .AddSingleton<DiscordHandler>()
                            .AddSingleton<PrivateMessageHandler>()
                            .AddSingleton<MapLoader>();

                        // singleton xna controls - same instance on each request
                        services
                            .AddSingletonXnaControl<LoadingScreen>()
                            .AddSingletonXnaControl<TopBar>()
                            .AddSingletonXnaControl<OptionsWindow>()
                            .AddSingletonXnaControl<PrivateMessagingWindow>()
                            .AddSingletonXnaControl<PrivateMessagingPanel>()
                            .AddSingletonXnaControl<LANLobby>()
                            .AddSingletonXnaControl<CnCNetGameLobby>()
                            .AddSingletonXnaControl<CnCNetGameLoadingLobby>()
                            .AddSingletonXnaControl<CnCNetLobby>()
                            .AddSingletonXnaControl<GameInProgressWindow>()
                            .AddSingletonXnaControl<SkirmishLobby>()
                            .AddSingletonXnaControl<MainMenu>()
                            .AddSingletonXnaControl<MapPreviewBox>()
                            .AddSingletonXnaControl<GameLaunchButton>()
                            .AddSingletonXnaControl<PlayerExtraOptionsPanel>();

                        // transient xna controls - new instance on each request
                        services
                            .AddTransientXnaControl<XNAControl>()
                            .AddTransientXnaControl<XNAButton>()
                            .AddTransientXnaControl<XNAClientButton>()
                            .AddTransientXnaControl<XNAClientCheckBox>()
                            .AddTransientXnaControl<XNAClientDropDown>()
                            .AddTransientXnaControl<XNALinkButton>()
                            .AddTransientXnaControl<XNAExtraPanel>()
                            .AddTransientXnaControl<XNACheckBox>()
                            .AddTransientXnaControl<XNADropDown>()
                            .AddTransientXnaControl<XNALabel>()
                            .AddTransientXnaControl<XNALinkLabel>()
                            .AddTransientXnaControl<XNAClientLinkLabel>()
                            .AddTransientXnaControl<XNAListBox>()
                            .AddTransientXnaControl<XNAMultiColumnListBox>()
                            .AddTransientXnaControl<XNAPanel>()
                            .AddTransientXnaControl<XNAProgressBar>()
                            .AddTransientXnaControl<XNASuggestionTextBox>()
                            .AddTransientXnaControl<XNATextBox>()
                            .AddTransientXnaControl<XNATrackbar>()
                            .AddTransientXnaControl<XNAChatTextBox>()
                            .AddTransientXnaControl<ChatListBox>()
                            .AddTransientXnaControl<GameLobbyCheckBox>()
                            .AddTransientXnaControl<GameLobbyDropDown>()
                            .AddTransientXnaControl<SettingCheckBox>()
                            .AddTransientXnaControl<SettingDropDown>()
                            .AddTransientXnaControl<FileSettingCheckBox>()
                            .AddTransientXnaControl<FileSettingDropDown>();
                    }
                )
                .Build();

            return host.Services.GetService<IServiceProvider>();
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
            settings.IndicatorAlphaRate = ClientConfiguration.Instance.IndicatorAlphaRate;

            settings.CheckBoxClearTexture = AssetLoader.LoadTexture("checkBoxClear.png");
            settings.CheckBoxCheckedTexture = AssetLoader.LoadTexture("checkBoxChecked.png");
            settings.CheckBoxDisabledClearTexture = AssetLoader.LoadTexture("checkBoxClearD.png");
            settings.CheckBoxDisabledCheckedTexture = AssetLoader.LoadTexture("checkBoxCheckedD.png");

            XNAPlayerSlotIndicator.LoadTextures();

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

            (int desktopWidth, int desktopHeight) = ScreenResolution.SafeMaximumResolution;

            if (desktopWidth >= windowWidth && desktopHeight >= windowHeight)
            {
                if (!wm.InitGraphicsMode(windowWidth, windowHeight, false))
                    throw new GraphicsModeInitializationException("Setting graphics mode failed!".L10N("Client:Main:SettingGraphicModeFailed") + " " + windowWidth + "x" + windowHeight);
            }
            else
            {
                // fallback to the minimum supported resolution when the desktop is not sufficient to contain the client
                // e.g., when users set a lower desktop resolution but the client resolution in the settings file remains high
                if (!wm.InitGraphicsMode(1024, 600, false))
                    throw new GraphicsModeInitializationException("Setting default graphics mode failed!".L10N("Client:Main:SettingDefaultGraphicModeFailed"));
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
                for (int i = 2; i <= ScreenResolution.MAX_INT_SCALE; i++)
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
                // Note: on fullscreen mode, the client resolution must exactly match the desktop resolution. Otherwise buttons outside of client resolution are unclickable.
                ScreenResolution clientResolution = (windowWidth, windowHeight);
                if (ScreenResolution.DesktopResolution == clientResolution)
                {
                    Logger.Log($"Entering fullscreen mode with resolution {ScreenResolution.DesktopResolution}.");
                    graphics.IsFullScreen = true;
                    graphics.ApplyChanges();
                }
                else
                {
                    Logger.Log($"Not entering fullscreen mode due to resolution mismatch. Desktop: {ScreenResolution.DesktopResolution}, Client: {clientResolution}.");
                }
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