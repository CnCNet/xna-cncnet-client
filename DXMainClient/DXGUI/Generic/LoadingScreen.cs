using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using DTAConfig;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System.Threading.Tasks;
using Updater;
using SkirmishLobby = DTAClient.DXGUI.Multiplayer.GameLobby.SkirmishLobby;

namespace DTAClient.DXGUI.Generic
{
    public class LoadingScreen : XNAWindow
    {
        public LoadingScreen(WindowManager windowManager) : base(windowManager)
        {

        }

        private static readonly object locker = new object();

        private MapLoader mapLoader;

        private PrivateMessagingPanel privateMessagingPanel;

        private bool visibleSpriteCursor = false;

        private Task updaterInitTask = null;
        private Task mapLoadTask = null;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";

            BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

            base.Initialize();

            CenterOnParent();

            bool initUpdater = !ClientConfiguration.Instance.ModMode;

            if (initUpdater)
            {
                updaterInitTask = new Task(InitUpdater);
                updaterInitTask.Start();
            }

            mapLoadTask = new Task(LoadMaps);
            mapLoadTask.Start();

            if (Cursor.Visible)
            {
                Cursor.Visible = false;
                visibleSpriteCursor = true;
            }
        }

        private void InitUpdater()
        {
            CUpdater.CheckLocalFileVersions();
        }

        private void LoadMaps()
        {
            mapLoader = new MapLoader();
            mapLoader.LoadMaps();
        }

        private void Finish()
        {
            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ? 
                "N/A" : CUpdater.GameVersion;

            DiscordHandler discordHandler = null;
            if (!string.IsNullOrEmpty(ClientConfiguration.Instance.DiscordAppId) && UserINISettings.Instance.DiscordIntegration)
                discordHandler = new DiscordHandler(WindowManager);

            var gameCollection = new GameCollection();
            gameCollection.Initialize(GraphicsDevice);

            var lanLobby = new LANLobby(WindowManager, gameCollection, mapLoader.GameModes, mapLoader, discordHandler);

            var cncnetUserData = new CnCNetUserData(WindowManager);
            var cncnetManager = new CnCNetManager(WindowManager, gameCollection);
            var tunnelHandler = new TunnelHandler(WindowManager, cncnetManager);
            

            var topBar = new TopBar(WindowManager, cncnetManager);

            var optionsWindow = new OptionsWindow(WindowManager, gameCollection, topBar);

            var pmWindow = new PrivateMessagingWindow(WindowManager,
                cncnetManager, gameCollection, cncnetUserData);
            privateMessagingPanel = new PrivateMessagingPanel(WindowManager);

            var cncnetGameLobby = new CnCNetGameLobby(WindowManager,
                "MultiplayerGameLobby", topBar, mapLoader.GameModes, cncnetManager, tunnelHandler, gameCollection, cncnetUserData, mapLoader, discordHandler);
            var cncnetGameLoadingLobby = new CnCNetGameLoadingLobby(WindowManager, 
                topBar, cncnetManager, tunnelHandler, mapLoader.GameModes, gameCollection, discordHandler);
            var cncnetLobby = new CnCNetLobby(WindowManager, cncnetManager, 
                cncnetGameLobby, cncnetGameLoadingLobby, topBar, pmWindow, tunnelHandler,
                gameCollection, cncnetUserData);
            var gipw = new GameInProgressWindow(WindowManager);

            var skirmishLobby = new SkirmishLobby(WindowManager, topBar, mapLoader.GameModes, discordHandler);

            topBar.SetSecondarySwitch(cncnetLobby);

            var mainMenu = new MainMenu(WindowManager, skirmishLobby, lanLobby,
                topBar, optionsWindow, cncnetLobby, cncnetManager, discordHandler);
            WindowManager.AddAndInitializeControl(mainMenu);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, skirmishLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetGameLoadingLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetGameLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, lanLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, optionsWindow);

            WindowManager.AddAndInitializeControl(privateMessagingPanel);
            privateMessagingPanel.AddChild(pmWindow);

            topBar.SetTertiarySwitch(pmWindow);
            topBar.SetOptionsWindow(optionsWindow);

            WindowManager.AddAndInitializeControl(gipw);
            skirmishLobby.Disable();
            cncnetLobby.Disable();
            cncnetGameLobby.Disable();
            cncnetGameLoadingLobby.Disable();
            lanLobby.Disable();
            pmWindow.Disable();
            optionsWindow.Disable();

            WindowManager.AddAndInitializeControl(topBar);
            topBar.AddPrimarySwitchable(mainMenu);

            mainMenu.PostInit();

            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
            {
                cncnetManager.Connect();
            }

            if (!UserINISettings.Instance.PrivacyPolicyAccepted)
            {
                WindowManager.AddAndInitializeControl(new PrivacyNotification(WindowManager));
            }

            WindowManager.RemoveControl(this);

            Cursor.Visible = visibleSpriteCursor;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion)
            {
                if (mapLoadTask.Status == TaskStatus.RanToCompletion)
                    Finish();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
