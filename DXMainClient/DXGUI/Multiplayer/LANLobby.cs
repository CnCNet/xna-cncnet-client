using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.LAN;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.LAN;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DTAClient.DXGUI.Multiplayer
{
    class LANLobby : XNAWindow
    {
        private const double ALIVE_MESSAGE_INTERVAL = 5.0;
        private const double INACTIVITY_REMOVE_TIME = 10.0;
        private const double GAME_INACTIVITY_REMOVE_TIME = 20.0;

        public LANLobby(WindowManager windowManager, GameCollection gameCollection,
            List<GameMode> gameModes, MapLoader mapLoader, DiscordHandler discordHandler)
            : base(windowManager)
        {
            this.gameCollection = gameCollection;
            this.gameModes = gameModes;
            this.mapLoader = mapLoader;
            this.discordHandler = discordHandler;
        }

        public event EventHandler Exited;

        XNAListBox lbPlayerList;
        ChatListBox lbChatMessages;
        GameListBox lbGameList;

        XNAClientButton btnMainMenu;
        XNAClientButton btnNewGame;
        XNAClientButton btnJoinGame;

        XNAChatTextBox tbChatInput;

        XNALabel lblColor;

        XNAClientDropDown ddColor;

        LANGameCreationWindow gameCreationWindow;

        LANGameLobby lanGameLobby;

        LANGameLoadingLobby lanGameLoadingLobby;

        Texture2D unknownGameIcon;

        LANColor[] chatColors;

        string localGame;
        int localGameIndex;

        GameCollection gameCollection;

        List<GameMode> gameModes;

        TimeSpan timeSinceGameRefresh = TimeSpan.Zero;

        EnhancedSoundEffect sndGameCreated;

        Socket socket;
        IPEndPoint endPoint;
        Encoding encoding;

        List<LANLobbyUser> players = new List<LANLobbyUser>();

        TimeSpan timeSinceAliveMessage = TimeSpan.Zero;

        MapLoader mapLoader;

        DiscordHandler discordHandler;

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
            btnNewGame.Text = "Create Game".L10N("UI:Main:CreateGame");
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnJoinGame = new XNAClientButton(WindowManager);
            btnJoinGame.Name = "btnJoinGame";
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.Right + 12,
                btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnJoinGame.Text = "Join Game".L10N("UI:Main:JoinGame");
            btnJoinGame.LeftClick += BtnJoinGame_LeftClick;

            btnMainMenu = new XNAClientButton(WindowManager);
            btnMainMenu.Name = "btnMainMenu";
            btnMainMenu.ClientRectangle = new Rectangle(Width - 145,
                btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnMainMenu.Text = "Main Menu".L10N("UI:Main:MainMenu");
            btnMainMenu.LeftClick += BtnMainMenu_LeftClick;

            lbGameList = new GameListBox(WindowManager, localGame, null);
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
            tbChatInput.Suggestion = "Type here to chat...".L10N("UI:Main:ChatHere");
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            lblColor = new XNALabel(WindowManager);
            lblColor.Name = "lblColor";
            lblColor.ClientRectangle = new Rectangle(lbChatMessages.X, 14, 0, 0);
            lblColor.FontIndex = 1;
            lblColor.Text = "YOUR COLOR:".L10N("UI:Main:YourColor");

            ddColor = new XNAClientDropDown(WindowManager);
            ddColor.Name = "ddColor";
            ddColor.ClientRectangle = new Rectangle(lblColor.X + 95, 12,
                150, 21);

            chatColors = new LANColor[]
            {
                new LANColor("Gray".L10N("UI:Main:ColorGray"), Color.Gray),
                new LANColor("Metalic".L10N("UI:Main:ColorLightGrayMetalic"), Color.LightGray),
                new LANColor("Green".L10N("UI:Main:ColorGreen"), Color.Green),
                new LANColor("Lime Green".L10N("UI:Main:ColorLimeGreen"), Color.LimeGreen),
                new LANColor("Green Yellow".L10N("UI:Main:ColorGreenYellow"), Color.GreenYellow),
                new LANColor("Goldenrod".L10N("UI:Main:ColorGoldenrod"), Color.Goldenrod),
                new LANColor("Yellow".L10N("UI:Main:ColorYellow"), Color.Yellow),
                new LANColor("Orange".L10N("UI:Main:ColorOrange"), Color.Orange),
                new LANColor("Red".L10N("UI:Main:ColorRed"), Color.Red),
                new LANColor("Pink".L10N("UI:Main:ColorDeepPink"), Color.DeepPink),
                new LANColor("Purple".L10N("UI:Main:ColorMediumPurple"), Color.MediumPurple),
                new LANColor("Sky Blue".L10N("UI:Main:ColorSkyBlue"), Color.SkyBlue),
                new LANColor("Blue".L10N("UI:Main:ColorBlue"), Color.Blue),
                new LANColor("Brown".L10N("UI:Main:ColorSaddleBrown"), Color.SaddleBrown),
                new LANColor("Teal".L10N("UI:Main:ColorTeal"), Color.Teal)
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

            gameCreationWindow = new LANGameCreationWindow(WindowManager);
            var gameCreationPanel = new DarkeningPanel(WindowManager);
            AddChild(gameCreationPanel);
            gameCreationPanel.AddChild(gameCreationWindow);
            gameCreationWindow.Disable();

            gameCreationWindow.NewGame += GameCreationWindow_NewGame;
            gameCreationWindow.LoadGame += GameCreationWindow_LoadGame;

            unknownGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.unknownicon);

            sndGameCreated = new EnhancedSoundEffect("gamecreated.wav");

            encoding = Encoding.UTF8;

            base.Initialize();

            CenterOnParent();
            gameCreationPanel.SetPositionAndSize();

            lanGameLobby = new LANGameLobby(WindowManager, "MultiplayerGameLobby",
                null, chatColors, mapLoader, discordHandler);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, lanGameLobby);
            lanGameLobby.Disable();

            lanGameLoadingLobby = new LANGameLoadingLobby(WindowManager,
                gameModes, chatColors, discordHandler);
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

        private void LanGameLoadingLobby_GameLeft(object sender, EventArgs e)
        {
            Enable();
        }

        private void WindowManager_GameClosing(object sender, EventArgs e)
        {
            if (socket == null)
                return;

            if (socket.IsBound)
            {
                try
                {
                    SendMessage("QUIT");
                    socket.Close();
                }
                catch (ObjectDisposedException)
                {

                }
            }
        }

        private void LanGameLobby_GameBroadcast(object sender, GameBroadcastEventArgs e)
        {
            SendMessage(e.Message);
        }

        private void LanGameLobby_GameLeft(object sender, EventArgs e)
        {
            Enable();
        }

        private void LanGameLoadingLobby_GameBroadcast(object sender, GameBroadcastEventArgs e)
        {
            SendMessage(e.Message);
        }

        private void GameCreationWindow_LoadGame(object sender, GameLoadEventArgs e)
        {
            lanGameLoadingLobby.SetUp(true,
                new IPEndPoint(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT),
                null, e.LoadedGameID);

            lanGameLoadingLobby.Enable();
        }

        private void GameCreationWindow_NewGame(object sender, EventArgs e)
        {
            lanGameLobby.SetUp(true,
                new IPEndPoint(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT), null);

            lanGameLobby.Enable();
        }

        private void SetChatColor()
        {
            tbChatInput.TextColor = chatColors[ddColor.SelectedIndex].XNAColor;
            lanGameLobby.SetChatColorIndex(ddColor.SelectedIndex);
            UserINISettings.Instance.LANChatColor.Value = ddColor.SelectedIndex;
        }

        private void DdColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetChatColor();
            UserINISettings.Instance.SaveSettings();
        }

        public void Open()
        {
            players.Clear();
            lbPlayerList.Clear();
            lbGameList.ClearGames();

            Visible = true;
            Enabled = true;

            Logger.Log("Creating LAN socket.");

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.EnableBroadcast = true;
                socket.Bind(new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_LOBBY_PORT));
                endPoint = new IPEndPoint(IPAddress.Broadcast, ProgramConstants.LAN_LOBBY_PORT);
                initSuccess = true;
            }
            catch (SocketException ex)
            {
                Logger.Log("Creating LAN socket failed! Message: " + ex.Message);
                lbChatMessages.AddMessage(new ChatMessage(Color.Red,
                    "Creating LAN socket failed! Message:".L10N("UI:Main:SocketFailure1")+ " " + ex.Message));
                lbChatMessages.AddMessage(new ChatMessage(Color.Red,
                    "Please check your firewall settings.".L10N("UI:Main:SocketFailure2")));
                lbChatMessages.AddMessage(new ChatMessage(Color.Red,
                    "Also make sure that no other application is listening to traffic on UDP ports 1232 - 1234.".L10N("UI:Main:SocketFailure3")));
                initSuccess = false;
                return;
            }

            Logger.Log("Starting listener.");
            new Thread(new ThreadStart(Listen)).Start();

            SendAlive();
        }

        private void SendMessage(string message)
        {
            if (!initSuccess)
                return;

            byte[] buffer;

            buffer = encoding.GetBytes(message);

            socket.SendTo(buffer, endPoint);
        }

        private void Listen()
        {
            try
            {
                while (true)
                {
                    EndPoint ep = new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_LOBBY_PORT);
                    byte[] buffer = new byte[4096];
                    int receivedBytes = 0;
                    receivedBytes = socket.ReceiveFrom(buffer, ref ep);

                    IPEndPoint iep = (IPEndPoint)ep;

                    string data = encoding.GetString(buffer, 0, receivedBytes);

                    if (data == string.Empty)
                        continue;

                    AddCallback(new Action<string, IPEndPoint>(HandleNetworkMessage), data, iep);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("LAN socket listener: exception: " + ex.Message);
            }
        }

        private void HandleNetworkMessage(string data, IPEndPoint endPoint)
        {
            string[] commandAndParams = data.Split(' ');

            if (commandAndParams.Length < 2)
                return;

            string command = commandAndParams[0];

            string[] parameters = data.Substring(command.Length + 1).Split(
                new char[] { ProgramConstants.LAN_DATA_SEPARATOR },
                StringSplitOptions.RemoveEmptyEntries);

            LANLobbyUser user = players.Find(p => p.EndPoint.Equals(endPoint));

            switch (command)
            {
                case "ALIVE":
                    if (parameters.Length < 2)
                        return;

                    int gameIndex = Conversions.IntFromString(parameters[0], -1);
                    string name = parameters[1];

                    if (user == null)
                    {
                        Texture2D gameTexture = unknownGameIcon;

                        if (gameIndex > -1 && gameIndex < gameCollection.GameList.Count)
                            gameTexture = gameCollection.GameList[gameIndex].Texture;

                        user = new LANLobbyUser(name, gameTexture, endPoint);
                        players.Add(user);
                        lbPlayerList.AddItem(user.Name, gameTexture);
                    }

                    user.TimeWithoutRefresh = TimeSpan.Zero;

                    break;
                case "CHAT":
                    if (user == null)
                        return;

                    if (parameters.Length < 2)
                        return;

                    int colorIndex = Conversions.IntFromString(parameters[0], -1);

                    if (colorIndex < 0 || colorIndex >= chatColors.Length)
                        return;

                    lbChatMessages.AddMessage(new ChatMessage(user.Name,
                        chatColors[colorIndex].XNAColor, DateTime.Now, parameters[1]));

                    break;
                case "QUIT":
                    if (user == null)
                        return;

                    int index = players.FindIndex(p => p == user);

                    players.RemoveAt(index);
                    lbPlayerList.Items.RemoveAt(index);
                    break;
                case "GAME":
                    if (user == null)
                        return;

                    HostedLANGame game = new HostedLANGame();
                    if (!game.SetDataFromStringArray(gameCollection, parameters))
                        return;
                    game.EndPoint = endPoint;

                    int existingGameIndex = lbGameList.HostedGames.FindIndex(g => ((HostedLANGame)g).EndPoint.Equals(endPoint));

                    if (existingGameIndex > -1)
                        lbGameList.HostedGames[existingGameIndex] = game;
                    else
                    {
                        lbGameList.HostedGames.Add(game);
                    }

                    lbGameList.Refresh();

                    break;
            }
        }

        private void SendAlive()
        {
            StringBuilder sb = new StringBuilder("ALIVE ");
            sb.Append(localGameIndex);
            sb.Append(ProgramConstants.LAN_DATA_SEPARATOR);
            sb.Append(ProgramConstants.PLAYERNAME);
            SendMessage(sb.ToString());
            timeSinceAliveMessage = TimeSpan.Zero;
        }

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            string chatMessage = tbChatInput.Text.Replace((char)01, '?');

            StringBuilder sb = new StringBuilder("CHAT ");
            sb.Append(ddColor.SelectedIndex);
            sb.Append(ProgramConstants.LAN_DATA_SEPARATOR);
            sb.Append(chatMessage);

            SendMessage(sb.ToString());

            tbChatInput.Text = string.Empty;
        }

        private void LbGameList_DoubleLeftClick(object sender, EventArgs e)
        {
            if (lbGameList.SelectedIndex < 0 || lbGameList.SelectedIndex >= lbGameList.Items.Count)
                return;

            HostedLANGame hg = (HostedLANGame)lbGameList.Items[lbGameList.SelectedIndex].Tag;

            if (hg.Game.InternalName.ToUpper() != localGame.ToUpper())
            {
                lbChatMessages.AddMessage(
                    string.Format("The selected game is for {0}!".L10N("UI:Main:GameIsOfPurpose"), gameCollection.GetGameNameFromInternalName(hg.Game.InternalName)));
                return;
            }

            if (hg.Locked)
            {
                lbChatMessages.AddMessage("The selected game is locked!".L10N("UI:Main:GameLocked"));
                return;
            }

            if (hg.IsLoadedGame)
            {
                if (!hg.Players.Contains(ProgramConstants.PLAYERNAME))
                {
                    lbChatMessages.AddMessage("You do not exist in the saved game!".L10N("UI:Main:NotInSavedGame"));
                    return;
                }
            }
            else
            {
                if (hg.Players.Contains(ProgramConstants.PLAYERNAME))
                {
                    lbChatMessages.AddMessage("Your name is already taken in the game.".L10N("UI:Main:NameOccupied"));
                    return;
                }
            }

            if (hg.GameVersion != ProgramConstants.GAME_VERSION)
            {
                // TODO Show warning
            }

            lbChatMessages.AddMessage(string.Format("Attempting to join game {0} ...".L10N("UI:Main:AttemptJoin"), hg.RoomName));

            try
            {
                var client = new TcpClient(hg.EndPoint.Address.ToString(), ProgramConstants.LAN_GAME_LOBBY_PORT);

                byte[] buffer;

                if (hg.IsLoadedGame)
                {
                    var spawnSGIni = new IniFile(ProgramConstants.GamePath +
                        ProgramConstants.SAVED_GAME_SPAWN_INI);

                    int loadedGameId = spawnSGIni.GetIntValue("Settings", "GameID", -1);

                    lanGameLoadingLobby.SetUp(false, hg.EndPoint, client, loadedGameId);
                    lanGameLoadingLobby.Enable();

                    buffer = encoding.GetBytes("JOIN" + ProgramConstants.LAN_DATA_SEPARATOR +
                        ProgramConstants.PLAYERNAME + ProgramConstants.LAN_DATA_SEPARATOR +
                        loadedGameId + ProgramConstants.LAN_MESSAGE_SEPARATOR);

                    client.GetStream().Write(buffer, 0, buffer.Length);
                    client.GetStream().Flush();

                    lanGameLoadingLobby.PostJoin();
                }
                else
                {
                    lanGameLobby.SetUp(false, hg.EndPoint, client);
                    lanGameLobby.Enable();

                    buffer = encoding.GetBytes("JOIN" + ProgramConstants.LAN_DATA_SEPARATOR +
                        ProgramConstants.PLAYERNAME + ProgramConstants.LAN_MESSAGE_SEPARATOR);

                    client.GetStream().Write(buffer, 0, buffer.Length);
                    client.GetStream().Flush();

                    lanGameLobby.PostJoin();
                }
            }
            catch (Exception ex)
            {
                lbChatMessages.AddMessage(null,
                    "Connecting to the game failed! Message:".L10N("UI:Main:ConnectGameFailed") + " " + ex.Message, Color.White);
            }
        }

        private void BtnMainMenu_LeftClick(object sender, EventArgs e)
        {
            Visible = false;
            Enabled = false;
            SendMessage("QUIT");
            socket.Close();
            Exited?.Invoke(this, EventArgs.Empty);
        }

        private void BtnJoinGame_LeftClick(object sender, EventArgs e)
        {
            LbGameList_DoubleLeftClick(this, EventArgs.Empty);
        }

        private void BtnNewGame_LeftClick(object sender, EventArgs e)
        {
            if (!ClientConfiguration.Instance.DisableMultiplayerGameLoading)
                gameCreationWindow.Open();
            else
                GameCreationWindow_NewGame(sender, e);
        }

        public override void Update(GameTime gameTime)
        {
            for (int i = 0; i < players.Count; i++)
            {
                players[i].TimeWithoutRefresh += gameTime.ElapsedGameTime;

                if (players[i].TimeWithoutRefresh > TimeSpan.FromSeconds(INACTIVITY_REMOVE_TIME))
                {
                    lbPlayerList.Items.RemoveAt(i);
                    players.RemoveAt(i);
                    i--;
                }
            }

            timeSinceAliveMessage += gameTime.ElapsedGameTime;
            if (timeSinceAliveMessage > TimeSpan.FromSeconds(ALIVE_MESSAGE_INTERVAL))
                SendAlive();

            base.Update(gameTime);
        }
    }
}
