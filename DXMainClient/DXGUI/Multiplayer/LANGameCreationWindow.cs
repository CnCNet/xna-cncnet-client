using ClientGUI;
using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using ClientCore;
using System.IO;
using Rampastring.Tools;
using Localization;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A window that makes it possible for a LAN player who's hosting a game
    /// to pick between hosting a new game and hosting a loaded game.
    /// </summary>
    class LANGameCreationWindow : XNAWindow
    {
        public LANGameCreationWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        public event EventHandler NewGame;
        public event EventHandler<GameLoadEventArgs> LoadGame;

        private XNALabel lblDescription;

        private XNAButton btnNewGame;
        private XNAButton btnLoadGame;
        private XNAButton btnCancel;

        public override void Initialize()
        {
            Name = "LANGameCreationWindow";
            BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            ClientRectangle = new Rectangle(0, 0, 447, 77);

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.FontIndex = 1;
            lblDescription.Text = "SELECT SESSION TYPE".L10N("UI:Main:SelectMissionType");

            AddChild(lblDescription);

            lblDescription.CenterOnParent();
            lblDescription.ClientRectangle = new Rectangle(
                lblDescription.X,
                12,
                lblDescription.Width,
                lblDescription.Height);

            btnNewGame = new XNAButton(WindowManager);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.ClientRectangle = new Rectangle(12, 42, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnNewGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnNewGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnNewGame.FontIndex = 1;
            btnNewGame.Text = "New Game".L10N("UI:Main:NewGame");
            btnNewGame.HoverSoundEffect = new EnhancedSoundEffect("button.wav");
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnLoadGame = new XNAButton(WindowManager);
            btnLoadGame.Name = "btnLoadGame";
            btnLoadGame.ClientRectangle = new Rectangle(btnNewGame.Right + 12,
                btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLoadGame.IdleTexture = btnNewGame.IdleTexture;
            btnLoadGame.HoverTexture = btnNewGame.HoverTexture;
            btnLoadGame.FontIndex = 1;
            btnLoadGame.Text = "Load Game".L10N("UI:Main:LoadGame");
            btnLoadGame.HoverSoundEffect = btnNewGame.HoverSoundEffect;
            btnLoadGame.LeftClick += BtnLoadGame_LeftClick;

            btnCancel = new XNAButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(btnLoadGame.Right + 12,
                btnNewGame.Y, 133, 23);
            btnCancel.IdleTexture = btnNewGame.IdleTexture;
            btnCancel.HoverTexture = btnNewGame.HoverTexture;
            btnCancel.FontIndex = 1;
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.HoverSoundEffect = btnNewGame.HoverSoundEffect;
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(btnNewGame);
            AddChild(btnLoadGame);
            AddChild(btnCancel);

            base.Initialize();

            CenterOnParent();
        }

        private void BtnNewGame_LeftClick(object sender, EventArgs e)
        {
            Disable();
            NewGame?.Invoke(this, EventArgs.Empty);
        }

        private void BtnLoadGame_LeftClick(object sender, EventArgs e)
        {
            Disable();

            IniFile iniFile = new IniFile(ProgramConstants.GamePath +
                ProgramConstants.SAVED_GAME_SPAWN_INI);

            LoadGame?.Invoke(this, new GameLoadEventArgs(iniFile.GetIntValue("Settings", "GameID", -1)));
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }

        public void Open()
        {
            btnLoadGame.AllowClick = AllowLoadingGame();
            Enable();
        }

        private bool AllowLoadingGame()
        {
            if (!File.Exists(ProgramConstants.GamePath +
                ProgramConstants.SAVED_GAME_SPAWN_INI))
                return false;

            IniFile iniFile = new IniFile(ProgramConstants.GamePath + 
                ProgramConstants.SAVED_GAME_SPAWN_INI);
            if (iniFile.GetStringValue("Settings", "Name", string.Empty) != ProgramConstants.PLAYERNAME)
                return false;

            if (!iniFile.GetBooleanValue("Settings", "Host", false))
                return false;

            // Don't allow loading CnCNet games in LAN mode
            if (iniFile.SectionExists("Tunnel"))
                return false;

            return true;
        }
    }

    public class GameLoadEventArgs : EventArgs
    {
        public GameLoadEventArgs(int loadedGameId)
        {
            LoadedGameID = loadedGameId;
        }

        public int LoadedGameID { get; private set; }
    }
}
