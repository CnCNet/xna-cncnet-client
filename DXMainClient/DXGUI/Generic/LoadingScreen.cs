using ClientCore;
using ClientGUI;
using DTAClient.domain;
using DTAClient.domain.CnCNet;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using System.Threading;
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
                t.Wait();

            Finish();
        }

        private void InitUpdater()
        {
            CUpdater.CheckLocalFileVersions();
        }

        private void Finish()
        {
            CnCNetManager cncnetManager = new CnCNetManager(WindowManager);
            TunnelHandler tunnelHandler = new TunnelHandler(WindowManager, cncnetManager);

            TopBar topBar = new TopBar(WindowManager, cncnetManager);

            PrivateMessagingWindow pmWindow = new PrivateMessagingWindow(WindowManager,
                cncnetManager);
            privateMessagingPanel = new PrivateMessagingPanel(WindowManager);

            CnCNetGameLobby cncnetGameLobby = new CnCNetGameLobby(WindowManager,
                "MultiplayerGameLobby", topBar, mapLoader.GameModes, cncnetManager, tunnelHandler);
            CnCNetGameLoadingLobby cncnetGameLoadingLobby = new CnCNetGameLoadingLobby(WindowManager, 
                topBar, cncnetManager, tunnelHandler, mapLoader.GameModes);
            CnCNetLobby cncnetLobby = new CnCNetLobby(WindowManager, cncnetManager, 
                cncnetGameLobby, cncnetGameLoadingLobby, topBar, pmWindow, tunnelHandler);
            GameInProgressWindow gipw = new GameInProgressWindow(WindowManager);

            SkirmishLobby sl = new SkirmishLobby(WindowManager, topBar, mapLoader.GameModes);

            topBar.SetSecondarySwitch(cncnetLobby);

            MainMenu mm = new MainMenu(WindowManager, sl, topBar, cncnetManager);
            WindowManager.AddAndInitializeControl(mm);
            WindowManager.AddAndInitializeControl(sl);
            WindowManager.AddAndInitializeControl(cncnetGameLoadingLobby);

            cncnetGameLobbyPanel = new DarkeningPanel(WindowManager);
            WindowManager.AddAndInitializeControl(cncnetGameLobbyPanel);
            cncnetGameLobbyPanel.AddChild(cncnetGameLobby);
            cncnetGameLobby.VisibleChanged += Darkened_VisibleChanged;

            cncnetLobbyPanel = new DarkeningPanel(WindowManager);
            WindowManager.AddAndInitializeControl(cncnetLobbyPanel);
            cncnetLobbyPanel.AddChild(cncnetLobby);
            cncnetLobby.VisibleChanged += Darkened_VisibleChanged;

            WindowManager.AddAndInitializeControl(privateMessagingPanel);
            privateMessagingPanel.AddChild(pmWindow);
            pmWindow.VisibleChanged += Darkened_VisibleChanged;

            topBar.SetTertiarySwitch(pmWindow);

            WindowManager.AddAndInitializeControl(gipw);
            sl.Visible = false;
            sl.Enabled = false;
            cncnetLobby.Visible = false;
            cncnetLobby.Enabled = false;
            cncnetGameLobby.Visible = false;
            cncnetGameLobby.Enabled = false;
            cncnetGameLoadingLobby.Visible = false;
            cncnetGameLoadingLobby.Enabled = false;
            pmWindow.Visible = false;
            pmWindow.Enabled = false;
            WindowManager.RemoveControl(this);
            mm.PostInit();

            WindowManager.AddAndInitializeControl(topBar);
            topBar.AddPrimarySwitchable(mm);

            Cursor.Visible = true;

            WindowManager.AddAndInitializeControl(new PrivateMessageNotificationBox(WindowManager));

            if (DomainController.Instance().GetCnCNetAutologinStatus())
                cncnetManager.Connect();
        }

        /// <summary>
        /// Hides the darkening panel of a window that has a darkening panel as its parent
        /// when the window is hidden.
        /// </summary>
        private void Darkened_VisibleChanged(object sender, EventArgs e)
        {
            var senderWindow = (XNAWindow)sender;
            var dp = (DarkeningPanel)senderWindow.Parent;

            if (senderWindow.Visible)
                dp.Show();
            else
                dp.Hide();
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
