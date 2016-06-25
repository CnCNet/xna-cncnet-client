using ClientCore;
using ClientGUI;
using DTAClient.domain;
using DTAClient.domain.CnCNet;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        bool updaterReady = false;
        bool mapsReady = false;

        MapLoader mapLoader;

        DarkeningPanel cncnetLobbyPanel;
        DarkeningPanel cncnetGameLobbyPanel;

        //DXProgressBar progressBar;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";

            BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

            CenterOnParent();

            base.Initialize();
        }

        private void CUpdater_OnLocalFileVersionsChecked()
        {
            lock (locker)
            {
                updaterReady = true;

                if (mapsReady)
                    AddCallback(new Action(Finish), null);
            }
        }

        public void Start()
        {
            Cursor.Visible = false;

            mapLoader = new MapLoader();
            mapLoader.MapLoadingComplete += MapLoader_MapLoadingComplete;

            if (!MCDomainController.Instance.GetModModeStatus())
            {
                CUpdater.OnLocalFileVersionsChecked += CUpdater_OnLocalFileVersionsChecked;

                Thread thread = new Thread(CUpdater.CheckLocalFileVersions);
                thread.Start();
            }
            else
            {
                updaterReady = true;
            }

            mapLoader.LoadMaps();

            Finish();
        }

        private void MapLoader_MapLoadingComplete(object sender, EventArgs e)
        {
            lock (locker)
            {
                mapsReady = true;

                if (updaterReady)
                    AddCallback(new Action(Finish), null);
            }
        }

        private void Finish()
        {
            CnCNetManager cncnetManager = new CnCNetManager(WindowManager);
            TunnelHandler tunnelHandler = new TunnelHandler(WindowManager, cncnetManager);

            TopBar topBar = new TopBar(WindowManager, cncnetManager);

            CnCNetGameLobby cncnetGameLobby = new CnCNetGameLobby(WindowManager,
                "MultiplayerGameLobby", topBar, mapLoader.GameModes, cncnetManager, tunnelHandler);
            CnCNetGameLoadingLobby cncnetGameLoadingLobby = new CnCNetGameLoadingLobby(WindowManager, 
                topBar, cncnetManager, tunnelHandler, mapLoader.GameModes);
            CnCNetLobby cncnetLobby = new CnCNetLobby(WindowManager, cncnetManager, 
                cncnetGameLobby, cncnetGameLoadingLobby, topBar, tunnelHandler);
            GameInProgressWindow gipw = new GameInProgressWindow(WindowManager);

            SkirmishLobby sl = new SkirmishLobby(WindowManager, topBar, mapLoader.GameModes);

            topBar.SetSecondarySwitch(cncnetLobby);

            MainMenu mm = new MainMenu(WindowManager, sl, topBar, cncnetManager);
            CUpdater.OnLocalFileVersionsChecked -= CUpdater_OnLocalFileVersionsChecked;
            WindowManager.AddAndInitializeControl(mm);
            WindowManager.AddAndInitializeControl(sl);
            WindowManager.AddAndInitializeControl(cncnetGameLoadingLobby);

            cncnetGameLobbyPanel = new DarkeningPanel(WindowManager);
            WindowManager.AddAndInitializeControl(cncnetGameLobbyPanel);
            cncnetGameLobbyPanel.AddChild(cncnetGameLobby);
            cncnetGameLobby.VisibleChanged += Lobby_VisibleChanged;

            cncnetLobbyPanel = new DarkeningPanel(WindowManager);
            WindowManager.AddAndInitializeControl(cncnetLobbyPanel);
            cncnetLobbyPanel.AddChild(cncnetLobby);
            cncnetLobby.VisibleChanged += Lobby_VisibleChanged;

            WindowManager.AddAndInitializeControl(gipw);
            sl.Visible = false;
            sl.Enabled = false;
            cncnetLobby.Visible = false;
            cncnetLobby.Enabled = false;
            cncnetGameLobby.Visible = false;
            cncnetGameLobby.Enabled = false;
            cncnetGameLoadingLobby.Visible = false;
            cncnetGameLoadingLobby.Enabled = false;
            WindowManager.RemoveControl(this);
            mm.PostInit();

            WindowManager.AddAndInitializeControl(topBar);
            topBar.AddPrimarySwitchable(mm);

            Cursor.Visible = true;

            if (DomainController.Instance().GetCnCNetAutologinStatus())
                cncnetManager.Connect();
        }

        private void Lobby_VisibleChanged(object sender, EventArgs e)
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
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
