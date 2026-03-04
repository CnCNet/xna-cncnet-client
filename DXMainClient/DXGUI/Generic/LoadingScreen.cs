using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore.Extensions;

using ClientGUI;
using ClientUpdater;
using DTAClient.Domain.Multiplayer;
using DTAClient.Online;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System.Diagnostics;

namespace DTAClient.DXGUI.Generic
{
    public class LoadingScreen : XNAWindow
    {
        public LoadingScreen(
            CnCNetManager cncnetManager,
            WindowManager windowManager,
            IServiceProvider serviceProvider,
            MapLoader mapLoader,
            Random random
        ) : base(windowManager)
        {
            this.cncnetManager = cncnetManager;
            this.serviceProvider = serviceProvider;
            this.mapLoader = mapLoader;
            this.random = random;
        }

        private MapLoader mapLoader;

        private Random random;

        private bool visibleSpriteCursor;

        private Task updaterInitTask;
        private Task mapLoadTask;

        private readonly CnCNetManager cncnetManager;
        private readonly IServiceProvider serviceProvider;

        private List<string> randomTextures;

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

            mapLoader.Initialize();
            mapLoadTask = mapLoader.LoadMapsAsync();

            if (Cursor.Visible)
            {
                Cursor.Visible = false;
                visibleSpriteCursor = true;
            }
        }

        protected override void GetINIAttributes(IniFile iniFile)
        {
            base.GetINIAttributes(iniFile);

            randomTextures = iniFile.GetStringListValue(Name, "RandomBackgroundTextures", string.Empty).ToList();

            if (randomTextures.Count == 0)
                return;

            BackgroundTexture = AssetLoader.LoadTexture(randomTextures[random.Next(randomTextures.Count)]);
        }

        private void InitUpdater()
        {
            Logger.Log("Updater: Updater initialization task started.");

            Updater.OnLocalFileVersionsChecked += LogGameClientVersion;
            Updater.CheckLocalFileVersions();

            Logger.Log("Updater: Updater initialization task completed.");
        }

        private void LogGameClientVersion()
        {
            Logger.Log($"Game Client Version: {ClientConfiguration.Instance.LocalGame} {Updater.GameVersion}");
            Updater.OnLocalFileVersionsChecked -= LogGameClientVersion;
        }

        private void Finish()
        {
            Logger.Log("LoadingScreen: Finish waiting for updater and map loading tasks. Proceeding to main menu.");

            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ?
                "N/A" : Updater.GameVersion;

            MainMenu mainMenu = serviceProvider.GetService<MainMenu>();

            WindowManager.AddAndInitializeControl(mainMenu);
            mainMenu.PostInit();

            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME, out _) == NameValidationError.None)
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


        private TimeSpan Update_LastLogTime = TimeSpan.Zero;
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            bool updaterDone = updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion;
            bool mapLoadDone = mapLoadTask.Status == TaskStatus.RanToCompletion;

            if (updaterDone && mapLoadDone)
            {
                Finish();
                return;
            }

            var timeSinceLastLog = gameTime.TotalGameTime.Subtract(Update_LastLogTime);
            if (timeSinceLastLog > TimeSpan.FromSeconds(5))
            {
                Update_LastLogTime = gameTime.TotalGameTime;

                string logMessage;
                if (!updaterDone && !mapLoadDone)
                    logMessage = "LoadingScreen: Waiting for updater initialization and loading maps...";
                else if (!updaterDone)
                    logMessage = "LoadingScreen: Waiting for updater initialization...";
                else if (!mapLoadDone)
                    logMessage = "LoadingScreen: Waiting for loading maps...";
                else
                    throw new Exception("Assert failed. No pending tasks. This should not happen.");

                Debug.WriteLine(logMessage);
                Logger.Log(logMessage);
            }
        }
    }
}
