using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using ClientGUI.IME;
using DTAClient.Domain;
using DTAClient.DXGUI.Generic;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Diagnostics;
using System.IO;
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
#if WINFORMS
using System.Windows.Forms;
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
            if (!ScreenResolution.HiDefLimitResolution.Fits(ScreenResolution.DesktopResolution))
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

            {
                string developBuildTitle = "Development Build".L10N("Client:Main:DevelopmentBuildTitle");

#if DEVELOPMENT_BUILD
                if (ClientConfiguration.Instance.ShowDevelopmentBuildWarnings)
                    Window.Title += $" ({developBuildTitle})";
#endif
            }

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

            WindowManager wm = new(this, graphics);
            wm.Initialize(content, ProgramConstants.GetBaseResourcePath());
            IMEHandler imeHandler = IMEHandler.Create(this);
            wm.IMEHandler = imeHandler;

            wm.ControlINIAttributeParsers.Add(new TranslationINIParser());

            SetGraphicsMode(wm);

#if WINFORMS
            wm.SetIcon(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "clienticon.ico"));
            wm.SetControlBox(true);

            // Enable resizable window for non-borderless windowed client, if integer scaling is enabled
            if (!UserINISettings.Instance.BorderlessWindowedClient && UserINISettings.Instance.IntegerScaledClient)
            {
                wm.SetFormBorderStyle(FormBorderStyle.Sizable);
                wm.SetMaximizeBox(true);

                //// Automatically update render resolution when the window size changes
                //// Disabled for now. It does not work as expected.
                //// To fix this, we need to make every window and control to be able to handle window size changes.
                //// This is not a trivial work and does not gain much benefit since the minimum render resolution and the maximum one are close.
                //// Example: https://github.com/Rampastring/WorldAlteringEditor/blob/71d9bd0ed9b9843d5dc15de14005f86b18e5465c/src/TSMapEditor/UI/Controls/INItializableWindow.cs#L98

                //ScreenResolution lastWindowSizeCaptured = new(wm.Game.Window.ClientBounds);

                //wm.WindowSizeChangedByUser += (sender, e) =>
                //{
                //    ScreenResolution currentWindowSize = new(wm.Game.Window.ClientBounds);

                //    if (currentWindowSize != lastWindowSizeCaptured)
                //    {
                //        Logger.Log($"Window size changed from {lastWindowSizeCaptured} to {currentWindowSize}.");
                //        lastWindowSizeCaptured = currentWindowSize;
                //        SetGraphicsMode(wm, currentWindowSize.Width, currentWindowSize.Height, centerOnScreen: false);
                //    }
                //};

                wm.WindowSizeChangedByUser += (sender, e) =>
                {
                    imeHandler.SetIMETextInputRectangle(wm);
                };
            }
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

        private static Random GetRandom()
        {
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] intBytes = new byte[sizeof(int)];
            rng.GetBytes(intBytes);
            int seed = BitConverter.ToInt32(intBytes, 0);
            return new Random(seed);
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
                            .AddSingleton<MapLoader>()
                            .AddSingleton<Random>(GetRandom());

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
                            .AddSingletonXnaControl<PlayerExtraOptionsPanel>()
                            .AddSingletonXnaControl<CampaignSelector>()
                            .AddSingletonXnaControl<GameLoadingWindow>()
                            .AddSingletonXnaControl<StatisticsWindow>()
                            .AddSingletonXnaControl<UpdateQueryWindow>()
                            .AddSingletonXnaControl<ManualUpdateQueryWindow>()
                            .AddSingletonXnaControl<UpdateWindow>()
                            .AddSingletonXnaControl<ExtrasWindow>();

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
        /// <param name="centerOnScreen">Whether to center the client window on the screen</param>
        public static void SetGraphicsMode(WindowManager wm, bool centerOnScreen = true)
        {
            int windowWidth = UserINISettings.Instance.ClientResolutionX;
            int windowHeight = UserINISettings.Instance.ClientResolutionY;

            SetGraphicsMode(wm, windowWidth, windowHeight, centerOnScreen);
        }

        /// <inheritdoc cref="SetGraphicsMode(WindowManager, bool)"/>
        /// <param name="windowWidth">The viewport width</param>
        /// <param name="windowHeight">The viewport height</param>
        public static void SetGraphicsMode(WindowManager wm, int windowWidth, int windowHeight, bool centerOnScreen = true)
        {
            bool borderlessWindowedClient = UserINISettings.Instance.BorderlessWindowedClient;
            bool integerScale = UserINISettings.Instance.IntegerScaledClient;

            SetGraphicsMode(wm, windowWidth, windowHeight, borderlessWindowedClient, integerScale, centerOnScreen);
        }

        /// <inheritdoc cref="SetGraphicsMode(WindowManager, int, int, bool)"/>
        /// <param name="borderlessWindowedClient">Whether to use borderless windowed mode</param>
        /// <param name="integerScale">Whether to use integer scaling</param>
        public static void SetGraphicsMode(WindowManager wm, int windowWidth, int windowHeight, bool borderlessWindowedClient, bool integerScale, bool centerOnScreen = true)
        {
            var clientConfiguration = ClientConfiguration.Instance;

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

            if (!integerScale || windowWidth < clientConfiguration.MinimumRenderWidth || windowHeight < clientConfiguration.MinimumRenderHeight)
            {
                int initialXRes = Math.Max(windowWidth, clientConfiguration.MinimumRenderWidth);
                initialXRes = Math.Min(initialXRes, clientConfiguration.MaximumRenderWidth);

                int initialYRes = Math.Max(windowHeight, clientConfiguration.MinimumRenderHeight);
                initialYRes = Math.Min(initialYRes, clientConfiguration.MaximumRenderHeight);

                double xRatio = (windowWidth) / (double)initialXRes;
                double yRatio = (windowHeight) / (double)initialYRes;

                double ratio = xRatio > yRatio ? yRatio : xRatio;

                // Special rule for 1360x768 and 1366x768                
                if ((windowWidth == 1366 || windowWidth == 1360) && windowHeight == 768)
                {
                    // Most client interface has been designed for 1280x720 or 1280x800.
                    // 1280x720 upscaled to 1366x768 doesn't look great, so we allow players with 1366x768 to use their native resolution with small black bars on the sides
                    // This behavior is enforced even if IntegerScaledClient is turned off.
                    renderResolutionX = windowWidth;
                    renderResolutionY = windowHeight;
                }

                // Special rule: if 1280x720 is a valid render resolution, we allow 1.5x scaling for 1920x1080.
                if (windowWidth == 1920 && windowHeight == 1080
                    && 1280 >= clientConfiguration.MinimumRenderWidth && 1280 <= clientConfiguration.MaximumRenderWidth
                    && 720 >= clientConfiguration.MinimumRenderHeight && 720 <= clientConfiguration.MaximumRenderHeight)
                {
                    renderResolutionX = 1280;
                    renderResolutionY = 720;
                }

                // Special rule: if 1280x800 is a valid render resolution, we allow 1.5x scaling for 1920x1200.
                if (windowWidth == 1920 && windowHeight == 1200
                    && 1280 >= clientConfiguration.MinimumRenderWidth && 1280 <= clientConfiguration.MaximumRenderWidth
                    && 800 >= clientConfiguration.MinimumRenderHeight && 800 <= clientConfiguration.MaximumRenderHeight)
                {
                    renderResolutionX = 1280;
                    renderResolutionY = 800;
                }

                // Check whether we could integer-scale our client window
                if (ratio > 1.0)
                {
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

                // No special rules are triggered. Just zoom the client to the window size with minimal black bars.
                if (renderResolutionX == 0 || renderResolutionY == 0)
                {
                    renderResolutionX = initialXRes;
                    renderResolutionY = initialYRes;

                    if (ratio == xRatio)
                        renderResolutionY = (int)(windowHeight / ratio);
                }
            }
            else
            {
                // Compute integer scale ratio using minimum render resolution
                // Note: this means we prefer larger scale ratio than render resolution.
                // This policy works best when maximum and minimum render resolution are close.
                int xScale = windowWidth / clientConfiguration.MinimumRenderWidth;
                int yScale = windowHeight / clientConfiguration.MinimumRenderHeight;
                int scale = Math.Min(xScale, yScale);

                // Compute render resolution
                renderResolutionX = Math.Min(clientConfiguration.MaximumRenderWidth,
                    clientConfiguration.MinimumRenderWidth + (windowWidth - clientConfiguration.MinimumRenderWidth * scale) / scale);
                renderResolutionY = Math.Min(clientConfiguration.MaximumRenderHeight,
                    clientConfiguration.MinimumRenderHeight + (windowHeight - clientConfiguration.MinimumRenderHeight * scale) / scale);
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
            if (centerOnScreen)
                wm.CenterOnScreen();

            Logger.Log("Setting render resolution to " + renderResolutionX + "x" + renderResolutionY + ". Integer scaling: " + integerScale);
            wm.IntegerScalingOnly = integerScale;
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