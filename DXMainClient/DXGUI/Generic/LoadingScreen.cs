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
        private ClanManagerPanel clanManagerPanel;

        private bool load = false;

        private bool visibleSpriteCursor = false;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";

            BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

            base.Initialize();

            CenterOnParent();
        }

        public void Start()
        {
            bool initUpdater = !ClientConfiguration.Instance.ModMode;
            Task t = null;

            if (initUpdater)
            {
                t = new Task(InitUpdater);
                t.Start();
            }

            mapLoader = new MapLoader();
            mapLoader.LoadMaps();

            if (initUpdater)
            {
                t.Wait();
                ProgramConstants.GAME_VERSION = CUpdater.GameVersion;
            }

            Finish();
        }

        private void InitUpdater()
        {
            CUpdater.CheckLocalFileVersions();
        }

        private void Finish()
        {
            var gameCollection = new GameCollection();
            gameCollection.Initialize(GraphicsDevice);

            var optionsWindow = new OptionsWindow(WindowManager, gameCollection);

            var lanLobby = new LANLobby(WindowManager, gameCollection, mapLoader.GameModes);

            var cncnetManager = new CnCNetManager(WindowManager, gameCollection);
            var tunnelHandler = new TunnelHandler(WindowManager, cncnetManager);

            var topBar = new TopBar(WindowManager, cncnetManager);

            var pmWindow = new PrivateMessagingWindow(WindowManager,
                cncnetManager, gameCollection);
            privateMessagingPanel = new PrivateMessagingPanel(WindowManager);

            var cmWindow = new ClanManagerWindow(WindowManager,
                cncnetManager, gameCollection);
            clanManagerPanel = new ClanManagerPanel(WindowManager);

            var cncnetGameLobby = new CnCNetGameLobby(WindowManager,
                "MultiplayerGameLobby", topBar, mapLoader.GameModes, cncnetManager, tunnelHandler);
            var cncnetGameLoadingLobby = new CnCNetGameLoadingLobby(WindowManager,
                topBar, cncnetManager, tunnelHandler, mapLoader.GameModes);
            var cncnetLobby = new CnCNetLobby(WindowManager, cncnetManager,
                cncnetGameLobby, cncnetGameLoadingLobby, topBar, pmWindow, cmWindow,
                tunnelHandler, gameCollection);

            var gipw = new GameInProgressWindow(WindowManager);

            var skirmishLobby = new SkirmishLobby(WindowManager, topBar, mapLoader.GameModes);

            topBar.SetSecondarySwitch(cncnetLobby);

            var mainMenu = new MainMenu(WindowManager, skirmishLobby, lanLobby,
                topBar, optionsWindow, cncnetLobby, cncnetManager);
            WindowManager.AddAndInitializeControl(mainMenu);
            WindowManager.AddAndInitializeControl(skirmishLobby);
            WindowManager.AddAndInitializeControl(cncnetGameLoadingLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetGameLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, lanLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, optionsWindow);

            WindowManager.AddAndInitializeControl(privateMessagingPanel);
            WindowManager.AddAndInitializeControl(clanManagerPanel);
            privateMessagingPanel.AddChild(pmWindow);
            clanManagerPanel.AddChild(cmWindow);

            topBar.SetTertiarySwitch(pmWindow);
            topBar.SetQuaternarySwitch(cmWindow);

            WindowManager.AddAndInitializeControl(gipw);
            skirmishLobby.Disable();
            cncnetLobby.Disable();
            cncnetGameLobby.Disable();
            cncnetGameLoadingLobby.Disable();
            lanLobby.Disable();
            pmWindow.Disable();
            cmWindow.Disable();
            optionsWindow.Disable();

            WindowManager.AddAndInitializeControl(topBar);
            topBar.AddPrimarySwitchable(mainMenu);

            WindowManager.AddAndInitializeControl(new PrivateMessageNotificationBox(WindowManager));

            mainMenu.PostInit();

            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
            {
                cncnetManager.Connect();
            }

            WindowManager.RemoveControl(this);

            Cursor.Visible = visibleSpriteCursor;
        }

        public override void Update(GameTime gameTime)
        {
            // We don't start loading immediately, but let the client draw one frame
            // first so the user can see the loading screen

            if (load)
            {
                Start();
                return;
            }

            load = true;

            if (Cursor.Visible)
            {
                Cursor.Visible = false;
                visibleSpriteCursor = true;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
