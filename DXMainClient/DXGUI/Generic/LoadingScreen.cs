using System.Threading.Tasks;
using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Updater;

namespace DTAClient.DXGUI.Generic
{
    public class LoadingScreen : XNAWindow
    {

        private static readonly object locker = new object();
        private bool visibleSpriteCursor = false;
        private Task updaterInitTask = null;
        private Task mapLoadTask = null;
        private readonly CnCNetManager cncnetManager;
        private readonly ServiceProvider serviceProvider;
        private readonly MapLoader mapLoader;

        public LoadingScreen(
            CnCNetManager cncnetManager,
            WindowManager windowManager,
            ServiceProvider serviceProvider,
            MapLoader mapLoader
        ) : base(windowManager)
        {
            this.cncnetManager = cncnetManager;
            this.serviceProvider = serviceProvider;
            this.mapLoader = mapLoader;
        }

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
            mapLoader.LoadMaps();
        }

        private void Finish()
        {
            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ? 
                "N/A" : CUpdater.GameVersion;

            var mainMenu = serviceProvider.GetRequiredService<MainMenu>();
            WindowManager.AddAndInitializeControl(mainMenu);

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
