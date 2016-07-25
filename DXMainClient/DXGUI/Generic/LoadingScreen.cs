using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.domain;
using DTAClient.domain.Multiplayer;
using DTAClient.domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using DTAConfig;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
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

        MapLoader mapLoader;

        PrivateMessagingPanel privateMessagingPanel;

        bool load = false;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";

            BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

            CenterOnParent();

            base.Initialize();
        }

        public void Start()
        {
            bool initUpdater = !MCDomainController.Instance.GetModModeStatus();
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

            var cncnetGameLobby = new CnCNetGameLobby(WindowManager,
                "MultiplayerGameLobby", topBar, mapLoader.GameModes, cncnetManager, tunnelHandler);
            var cncnetGameLoadingLobby = new CnCNetGameLoadingLobby(WindowManager, 
                topBar, cncnetManager, tunnelHandler, mapLoader.GameModes);
            var cncnetLobby = new CnCNetLobby(WindowManager, cncnetManager, 
                cncnetGameLobby, cncnetGameLoadingLobby, topBar, pmWindow, tunnelHandler,
                gameCollection);
            var gipw = new GameInProgressWindow(WindowManager);

            var skirmishLobby = new SkirmishLobby(WindowManager, topBar, mapLoader.GameModes);

            topBar.SetSecondarySwitch(cncnetLobby);

            var mainMenu = new MainMenu(WindowManager, skirmishLobby, lanLobby,
                topBar, optionsWindow, cncnetManager);
            WindowManager.AddAndInitializeControl(mainMenu);
            WindowManager.AddAndInitializeControl(skirmishLobby);
            WindowManager.AddAndInitializeControl(cncnetGameLoadingLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetGameLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, cncnetLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, lanLobby);

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, optionsWindow);

            WindowManager.AddAndInitializeControl(privateMessagingPanel);
            privateMessagingPanel.AddChild(pmWindow);

            topBar.SetTertiarySwitch(pmWindow);

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

            WindowManager.AddAndInitializeControl(new PrivateMessageNotificationBox(WindowManager));

            mainMenu.PostInit();

            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
            {
                cncnetManager.Connect();
            }

            WindowManager.RemoveControl(this);

            Cursor.Visible = true;
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
            Cursor.Visible = false;

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
