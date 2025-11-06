using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.LAN;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.LAN;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using SixLabors.ImageSharp;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DTAClient.DXGUI.Multiplayer.CnCNet;

namespace DTAClient.DXGUI.Multiplayer
{
    class LANLobby : XNAWindow
    {
        private const double ALIVE_MESSAGE_INTERVAL = 5.0;
        private const double INACTIVITY_REMOVE_TIME = 10.0;
        private const double GAME_INACTIVITY_REMOVE_TIME = 20.0;

        public LANLobby(
            WindowManager windowManager,
            GameCollection gameCollection,
            MapLoader mapLoader,
            DiscordHandler discordHandler,
            Random random
        ) : base(windowManager)
        {
            this.gameCollection = gameCollection;
            this.mapLoader = mapLoader;
            this.discordHandler = discordHandler;
            this.random = random;
        }

        public event EventHandler Exited;

        private Random random;

        XNAListBox lbPlayerList;
        ChatListBox lbChatMessages;
        GameListBox lbGameList;

        XNAClientButton btnMainMenu;
        XNAClientButton btnNewGame;
        XNAClientButton btnJoinGame;

        XNAChatTextBox tbChatInput;

        XNALabel lblColor;
        XNALabel lblRA1_1v1;
        XNALabel lblRA1_2v2;

        XNAClientDropDown ddColor;

        LANGameCreationWindow gameCreationWindow;

        LANGameLobby lanGameLobby;

        LANGameLoadingLobby lanGameLoadingLobby;

        Texture2D unknownGameIcon;

        LANColor[] chatColors;

        string localGame;
        int localGameIndex;

        GameCollection gameCollection;

        private List<GameMode> gameModes => mapLoader.GameModes;

        TimeSpan timeSinceGameRefresh = TimeSpan.Zero;

        EnhancedSoundEffect sndGameCreated;

        Socket socket;
        IPEndPoint endPoint;
        Encoding encoding;

        List<LANLobbyUser> players = new List<LANLobbyUser>();

        TimeSpan timeSinceAliveMessage = TimeSpan.Zero;

        MapLoader mapLoader;

        DiscordHandler discordHandler;
        PrivateMessagingWindow pmWindow;

        bool initSuccess = false;

        public override void Initialize()
        {
            Name = "LANLobby";
            BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbybg.png");
            ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX - 64,
                WindowManager.RenderResolutionY - 64);

            localGame = ClientConfiguration.Instance.LocalGame;
            localGameIndex = gameCollection.GameList.FindIndex(
                g => g.InternalName.ToUpper() == localGame.ToUpper());

            btnNewGame = new XNAClientButton(WindowManager);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.ClientRectangle = new Rectangle(12, Height - 35, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnNewGame.Text = "Create Game".L10N("Client:Main:CreateGame");
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnJoinGame = new XNAClientButton(WindowManager);
            btnJoinGame.Name = "btnJoinGame";
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.Right + 12,
                btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnJoinGame.Text = "Join Game".L10N("Client:Main:JoinGame");
            btnJoinGame.LeftClick += BtnJoinGame_LeftClick;

            btnMainMenu = new XNAClientButton(WindowManager);
            btnMainMenu.Name = "btnMainMenu";
            btnMainMenu.ClientRectangle = new Rectangle(Width - 145,
                btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnMainMenu.Text = "Main Menu".L10N("Client:Main:MainMenu");
            btnMainMenu.LeftClick += BtnMainMenu_LeftClick;

            lbGameList = new GameListBox(WindowManager, mapLoader, localGame);
            lbGameList.Name = "lbGameList";
            lbGameList.ClientRectangle = new Rectangle(btnNewGame.X,
                41, btnJoinGame.Right - btnNewGame.X,
                btnNewGame.Y - 53);
            lbGameList.GameLifetime = 15.0; // Smaller lifetime in LAN
            lbGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameList.DoubleLeftClick += LbGameList_DoubleLeftClick;
            lbGameList.AllowMultiLineItems = false;

            lbPlayerList = new XNAListBox(WindowManager);
            lbPlayerList.Name = "lbPlayerList";
            lbPlayerList.ClientRectangle = new Rectangle(Width - 202,
                lbGameList.Y, 190,
                lbGameList.Height);
            lbPlayerList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbPlayerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbPlayerList.LineHeight = 16;

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.ClientRectangle = new Rectangle(lbGameList.Right + 12,
                lbGameList.Y,
                lbPlayerList.X - lbGameList.Right - 24,
                lbGameList.Height);
            lbChatMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;

            tbChatInput = new XNAChatTextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                btnNewGame.Y, lbChatMessages.Width,
                btnNewGame.Height);
            tbChatInput.Suggestion = "Type here to chat...".L10N("Client:Main:ChatHere");
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            lblColor = new XNALabel(WindowManager);
            lblColor.Name = "lblColor";
            lblColor.ClientRectangle = new Rectangle(lbChatMessages.X, 14, 0, 0);
            lblColor.FontIndex = 1;
            lblColor.Text = "YOUR COLOR:".L10N("Client:Main:YourColor");

            ddColor = new XNAClientDropDown(WindowManager);
            ddColor.Name = "ddColor";
            ddColor.ClientRectangle = new Rectangle(lblColor.X + 95, 12,
                150, 21);

            chatColors = new LANColor[]
            {
                new LANColor("Gray".L10N("Client:Main:ColorGray"), Color.Gray),
                new LANColor("Metalic".L10N("Client:Main:ColorLightGrayMetalic"), Color.LightGray),
                new LANColor("Green".L10N("Client:Main:ColorGreen"), Color.ForestGreen),
                new LANColor("Lime Green".L10N("Client:Main:ColorLimeGreen"), Color.LimeGreen),
                new LANColor("Green Yellow".L10N("Client:Main:ColorGreenYellow"), Color.GreenYellow),
                new LANColor("Goldenrod".L10N("Client:Main:ColorGoldenrod"), Color.Goldenrod),
                new LANColor("Yellow".L10N("Client:Main:ColorYellow"), Color.Yellow),
                new LANColor("Orange".L10N("Client:Main:ColorOrange"), Color.Orange),
                new LANColor("Red".L10N("Client:Main:ColorRed"), Color.Red),
                new LANColor("Pink".L10N("Client:Main:ColorPink"), Color.DeepPink),
                new LANColor("Purple".L10N("Client:Main:ColorPurple"), Color.MediumPurple),
                new LANColor("Sky Blue".L10N("Client:Main:ColorSkyBlue"), Color.LightSkyBlue),
                new LANColor("Blue".L10N("Client:Main:ColorBlue"), Color.RoyalBlue),
                new LANColor("Brown".L10N("Client:Main:ColorBrown"), Color.SaddleBrown),
                new LANColor("Teal".L10N("Client:Main:ColorTeal"), Color.Teal)
            };

            foreach (LANColor color in chatColors)
            {
                ddColor.AddItem(color.Name, color.XNAColor);
            }

            AddChild(btnNewGame);
            AddChild(btnJoinGame);
            AddChild(btnMainMenu);

            AddChild(lbPlayerList);
            AddChild(lbChatMessages);
            AddChild(lbGameList);
            AddChild(tbChatInput);
            AddChild(lblColor);
            AddChild(ddColor);

            // ---- Ladder Labels ----
            lblRA1_1v1 = new XNALabel(WindowManager);
            lblRA1_1v1.Name = "lblRA1_1v1";
            lblRA1_1v1.ClientRectangle = new Rectangle(12, lbGameList.Bottom + 10, 300, 20);
            lblRA1_1v1.FontIndex = 1;
            lblRA1_1v1.Text = "RA1 1v1: ???"; // will update dynamically later
            AddChild(lblRA1_1v1);

            lblRA1_2v2 = new XNALabel(WindowManager);
            lblRA1_2v2.Name = "lblRA1_2v2";
            lblRA1_2v2.ClientRectangle = new Rectangle(12, lblRA1_1v1.Bottom + 5, 300, 20);
            lblRA1_2v2.FontIndex = 1;
            lblRA1_2v2.Text = "RA1 2v2: ???"; // will update dynamically later
            AddChild(lblRA1_2v2);

            // ------------------------

            gameCreationWindow = new LANGameCreationWindow(WindowManager);
            var gameCreationPanel = new DarkeningPanel(WindowManager);
            AddChild(gameCreationPanel);
            gameCreationPanel.AddChild(gameCreationWindow);
            gameCreationWindow.Disable();

            gameCreationWindow.NewGame += GameCreationWindow_NewGame;
            gameCreationWindow.LoadGame += GameCreationWindow_LoadGame;

            var assembly = Assembly.GetAssembly(typeof(GameCollection));
            using Stream unknownIconStream = assembly.GetManifestResourceStream("DTAClient.Icons.unknownicon.png");

            unknownGameIcon = AssetLoader.TextureFromImage(Image.Load(unknownIconStream));

            sndGameCreated = new EnhancedSoundEffect("gamecreated.wav");

            encoding = Encoding.UTF8;

            base.Initialize();

            CenterOnParent();
            gameCreationPanel.SetPositionAndSize();

            lanGameLobby = new LANGameLobby(WindowManager, "MultiplayerGameLobby",
                null, chatColors, mapLoader, discordHandler, pmWindow, random);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, lanGameLobby);
            lanGameLobby.Disable();

            lanGameLoadingLobby = new LANGameLoadingLobby(WindowManager,
                chatColors, mapLoader, discordHandler);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, lanGameLoadingLobby);
            lanGameLoadingLobby.Disable();

            int selectedColor = UserINISettings.Instance.LANChatColor;

            ddColor.SelectedIndex = selectedColor >= ddColor.Items.Count || selectedColor < 0
                ? 0 : selectedColor;

            SetChatColor();
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;

            lanGameLobby.GameLeft += LanGameLobby_GameLeft;
            lanGameLobby.GameBroadcast += LanGameLobby_GameBroadcast;

            lanGameLoadingLobby.GameBroadcast += LanGameLoadingLobby_GameBroadcast;
            lanGameLoadingLobby.GameLeft += LanGameLoadingLobby_GameLeft;

            WindowManager.GameClosing += WindowManager_GameClosing;
        }

        // ---------------------- All existing methods remain unchanged ----------------------
        // BtnNewGame_LeftClick, BtnJoinGame_LeftClick, BtnMainMenu_LeftClick, 
        // LbGameList_DoubleLeftClick, TbChatInput_EnterPressed, SetChatColor, 
        // DdColor_SelectedIndexChanged, GameCreationWindow_NewGame, GameCreationWindow_LoadGame
        // Listen, HandleNetworkMessage, SendMessage, SendAlive, Update, etc.
        // ---------------------------------------------------------------------------------
    }
}