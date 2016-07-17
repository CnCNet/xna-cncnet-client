using ClientCore;
using ClientGUI;
using DTAClient.domain;
using DTAClient.domain.Multiplayer;
using DTAClient.domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
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

        DarkeningPanel cncnetLobbyPanel;
        DarkeningPanel cncnetGameLobbyPanel;
        PrivateMessagingPanel privateMessagingPanel;

        bool load = false;

        //DXProgressBar progressBar;

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
            GameCollection gameCollection = new GameCollection();
            gameCollection.Initialize(GraphicsDevice);

            LANLobby lanLobby = new LANLobby(WindowManager, gameCollection, mapLoader.GameModes);

            CnCNetManager cncnetManager = new CnCNetManager(WindowManager, gameCollection);
            TunnelHandler tunnelHandler = new TunnelHandler(WindowManager, cncnetManager);

            TopBar topBar = new TopBar(WindowManager, cncnetManager);

            PrivateMessagingWindow pmWindow = new PrivateMessagingWindow(WindowManager,
                cncnetManager, gameCollection);
            privateMessagingPanel = new PrivateMessagingPanel(WindowManager);

            CnCNetGameLobby cncnetGameLobby = new CnCNetGameLobby(WindowManager,
                "MultiplayerGameLobby", topBar, mapLoader.GameModes, cncnetManager, tunnelHandler);
            CnCNetGameLoadingLobby cncnetGameLoadingLobby = new CnCNetGameLoadingLobby(WindowManager, 
                topBar, cncnetManager, tunnelHandler, mapLoader.GameModes);
            CnCNetLobby cncnetLobby = new CnCNetLobby(WindowManager, cncnetManager, 
                cncnetGameLobby, cncnetGameLoadingLobby, topBar, pmWindow, tunnelHandler,
                gameCollection);
            GameInProgressWindow gipw = new GameInProgressWindow(WindowManager);

            SkirmishLobby sl = new SkirmishLobby(WindowManager, topBar, mapLoader.GameModes);

            topBar.SetSecondarySwitch(cncnetLobby);

            MainMenu mm = new MainMenu(WindowManager, sl, lanLobby, topBar, cncnetManager);
            WindowManager.AddAndInitializeControl(mm);
            WindowManager.AddAndInitializeControl(sl);
            WindowManager.AddAndInitializeControl(cncnetGameLoadingLobby);

            cncnetGameLobbyPanel = new DarkeningPanel(WindowManager);
            WindowManager.AddAndInitializeControl(cncnetGameLobbyPanel);
            cncnetGameLobbyPanel.AddChild(cncnetGameLobby);

            cncnetLobbyPanel = new DarkeningPanel(WindowManager);
            WindowManager.AddAndInitializeControl(cncnetLobbyPanel);
            cncnetLobbyPanel.AddChild(cncnetLobby);

            DarkeningPanel lanPanel = new DarkeningPanel(WindowManager);
            WindowManager.AddAndInitializeControl(lanPanel);
            lanPanel.AddChild(lanLobby);

            WindowManager.AddAndInitializeControl(privateMessagingPanel);
            privateMessagingPanel.AddChild(pmWindow);

            topBar.SetTertiarySwitch(pmWindow);

            WindowManager.AddAndInitializeControl(gipw);
            sl.Disable();
            cncnetLobby.Disable();
            cncnetGameLobby.Disable();
            cncnetGameLoadingLobby.Disable();
            lanLobby.Disable();
            pmWindow.Disable();

            WindowManager.AddAndInitializeControl(topBar);
            topBar.AddPrimarySwitchable(mm);

            WindowManager.AddAndInitializeControl(new PrivateMessageNotificationBox(WindowManager));

            mm.PostInit();

            if (DomainController.Instance().GetCnCNetAutologinStatus())
                cncnetManager.Connect();

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
