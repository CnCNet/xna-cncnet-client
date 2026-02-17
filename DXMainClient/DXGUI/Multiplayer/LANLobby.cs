using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using DTAClient.Domain;
using DTAClient.Domain.LAN;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.Domain.Multiplayer.LAN;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

using SixLabors.ImageSharp;

using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DTAClient.DXGUI.Multiplayer
{
    class LANLobby : XNAWindow
    {
        private const double ALIVE_MESSAGE_INTERVAL = 5.0;
        private const double INACTIVITY_REMOVE_TIME = 10.0;
        private const double GAME_INACTIVITY_REMOVE_TIME = 20.0;
        private const double MESSAGE_ID_EXPIRATION_SECONDS = 60.0;

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

        private List<GameMode> gameModes => mapLoader.GameModes;

        TimeSpan timeSinceGameRefresh = TimeSpan.Zero;

        EnhancedSoundEffect sndGameCreated;

        Encoding encoding;

        ChatListBox lbChatMessages;

        GameListBox lbGameList;

        // lbPlayerList is now managed by LANPlayerManager `playerManager`
        // XNAListBox lbPlayerList;
        LANPlayerManager playerManager;

        LANMessageDeduplicator messageDeduplicator;

        LANLobbyBroadcastManager broadcastManager;

        TimeSpan timeSinceAliveMessage = TimeSpan.Zero;

        MapLoader mapLoader;

        DiscordHandler discordHandler;
        PrivateMessagingWindow pmWindow;

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

            var lbPlayerList = new XNAListBox(WindowManager);
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

            gameCreationWindow = new LANGameCreationWindow(WindowManager);
            var gameCreationPanel = new DarkeningPanel(WindowManager);
            AddChild(gameCreationPanel);
            gameCreationPanel.AddChild(gameCreationWindow);
            gameCreationWindow.Disable();

            gameCreationWindow.NewGame += GameCreationWindow_NewGame;
            gameCreationWindow.LoadGame += GameCreationWindow_LoadGame;

            // Initialize player manager after lbPlayerList is created
            playerManager = new LANPlayerManager(lbPlayerList);

            // Initialize message deduplicator with a random seed
            messageDeduplicator = new LANMessageDeduplicator(random.Next(), MESSAGE_ID_EXPIRATION_SECONDS);

            // Initialize broadcast manager
            encoding = Encoding.UTF8;
            broadcastManager = new LANLobbyBroadcastManager(ProgramConstants.LAN_LOBBY_PORT, encoding);
            // Dispatch to UI thread
            broadcastManager.MessageReceived += (sender, e) =>
                AddCallback(() => HandleNetworkMessage(e.Data, e.EndPoint));

            var assembly = Assembly.GetAssembly(typeof(GameCollection));
            using Stream unknownIconStream = assembly.GetManifestResourceStream("DTAClient.Icons.unknownicon.png");

            unknownGameIcon = AssetLoader.TextureFromImage(Image.Load(unknownIconStream));

            sndGameCreated = new EnhancedSoundEffect("gamecreated.wav");

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

        private void LanGameLoadingLobby_GameLeft(object sender, EventArgs e)
        {
            Enable();
        }

        private void WindowManager_GameClosing(object sender, EventArgs e)
        {
            SendMessage("QUIT");

            // Dispose the broadcast manager (which closes the socket and stops the listener)
            broadcastManager?.Dispose();

            // Dispose the message deduplicator to stop the cleanup timer
            messageDeduplicator?.Dispose();
        }

        private void LanGameLobby_GameBroadcast(object sender, GameBroadcastEventArgs e)
        {
            SendMessage(e.Message);
        }

        private void LanGameLobby_GameLeft(object sender, GameLeftEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Message))
                AddChatMessage(new ChatMessage(Color.Red, e.Message));

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

        void AddChatMessage(ChatMessage message)
            => lbChatMessages.AddMessage(message);

        void AddChatMessage(string message)
            => lbChatMessages.AddMessage(message);

        void AddChatMessage(string sender, string message, Color color)
            => lbChatMessages.AddMessage(sender, message, color);

        public void Open()
        {
            playerManager.Clear();
            messageDeduplicator.Clear();
            lbGameList.ClearGames();

            Visible = true;
            Enabled = true;

            try
            {
                broadcastManager.Initialize();
            }
            catch (Exception ex)
            {
                AddChatMessage(new ChatMessage(Color.Red,
                    "Creating LAN socket failed! Message:".L10N("Client:Main:SocketFailure1") + " " + ex.Message + "\n" +
                    "Please check your firewall settings.".L10N("Client:Main:SocketFailure2") + " " +
                    "Also make sure that no other application is listening to traffic on UDP ports 1232 - 1234.".L10N("Client:Main:SocketFailure3")));

                return;
            }

            SendAlive();
        }

        private void SendMessage(string message)
        {
            // Wrap message with message ID at the beginning
            string wrappedMessage = messageDeduplicator.WrapMessage(message);

            bool sendSucceeded = broadcastManager.SendMessage(wrappedMessage);

            if (!sendSucceeded)
            {
                // Socket is not initialized or sending failed; report this so failures are not silent.
                AddChatMessage(new ChatMessage(Color.Red,
                        "Failed to send LAN broadcast message. The network socket may not be initialized."));
            }
        }

        private void HandleNetworkMessage(string data, IPEndPoint endPoint)
        {
            // Unwrap message to extract message ID and check for duplicates
            messageDeduplicator.UnwrapMessage(data, out string payload, out bool isDuplicate);

            if (isDuplicate || string.IsNullOrWhiteSpace(payload))
                return;

            string[] commandAndParams = payload.Split(' ');

            string command = commandAndParams[0];

            string[] parameters;
            {
                // For parameterless commands like "QUIT", avoid the potential out-of-bounds issue by locating the first space first.
                int firstSpace = payload.IndexOf(' ');
                parameters = firstSpace >= 0
                    ? payload.Substring(firstSpace + 1).Split([ProgramConstants.LAN_DATA_SEPARATOR])
                    : [];
            }

            LANLobbyUser user = playerManager.GetPlayerIfExist(endPoint);

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

                        user = playerManager.GetOrCreatePlayer(endPoint, name, gameTexture);
                    }

                    user.ClearTimeWithoutRefresh();

                    break;

                case "CHAT":
                    if (user == null)
                        return;

                    if (parameters.Length < 2)
                        return;

                    int colorIndex = Conversions.IntFromString(parameters[0], -1);

                    if (colorIndex < 0 || colorIndex >= chatColors.Length)
                        return;

                    AddChatMessage(new ChatMessage(user.Name,
                        chatColors[colorIndex].XNAColor, DateTime.Now, parameters[1]));

                    break;

                case "QUIT":
                    if (user == null)
                        return;

                    playerManager.RemovePlayer(endPoint);

                    break;

                case "GAME":
                    if (user == null)
                        return;

                    HostedLANGame game = new HostedLANGame();
                    if (!game.SetDataFromStringArray(gameCollection, parameters))
                        return;

                    game.EndPoint = endPoint;

                    int existingGameIndex =
                        lbGameList.HostedGames.FindIndex(g => ((HostedLANGame)g).EndPoint.Equals(endPoint));

                    if (existingGameIndex > -1)
                        lbGameList.HostedGames[existingGameIndex] = game;
                    else
                        lbGameList.HostedGames.Add(game);

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
                AddChatMessage(
                    string.Format("The selected game is for {0}!".L10N("Client:Main:GameIsOfPurpose"),
                    gameCollection.GetGameNameFromInternalName(hg.Game.InternalName)));
                return;
            }

            if (hg.Locked)
            {
                AddChatMessage("The selected game is locked!".L10N("Client:Main:GameLocked"));
                return;
            }

            if (hg.IsLoadedGame)
            {
                if (!hg.Players.Contains(ProgramConstants.PLAYERNAME))
                {
                    AddChatMessage("You do not exist in the saved game!".L10N("Client:Main:NotInSavedGame"));
                    return;
                }
            }
            else
            {
                if (hg.Players.Contains(ProgramConstants.PLAYERNAME))
                {
                    AddChatMessage("Your name is already taken in the game.".L10N("Client:Main:NameOccupied"));
                    return;
                }
            }

            if (hg.GameVersion != ProgramConstants.GAME_VERSION)
            {
                // TODO Show warning
            }

            AddChatMessage(string.Format("Attempting to join game {0} ...".L10N("Client:Main:AttemptJoin"), hg.RoomName));

            try
            {
                var client = new TcpClient(hg.EndPoint.Address.ToString(), ProgramConstants.LAN_GAME_LOBBY_PORT);

                byte[] buffer;

                if (hg.IsLoadedGame)
                {
                    var spawnSGIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));

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
                AddChatMessage(null,
                    "Connecting to the game failed! Message:".L10N("Client:Main:ConnectGameFailed") + " " + ex.Message, Color.White);
            }
        }

        private void BtnMainMenu_LeftClick(object sender, EventArgs e)
        {
            Visible = false;
            Enabled = false;
            SendMessage("QUIT");
            broadcastManager.Shutdown();
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
            // Remove inactive players periodically
            {
                // Get a thread-safe snapshot of all players
                var playersCopy = playerManager.GetAllPlayers();
                foreach (var player in playersCopy)
                {
                    player.AddToTimeWithoutRefresh(gameTime.ElapsedGameTime);

                    if (player.TimeWithoutRefresh > TimeSpan.FromSeconds(INACTIVITY_REMOVE_TIME))
                        playerManager.RemovePlayer(player.EndPoint);
                }
            }

            // Send ALIVE message periodically
            timeSinceAliveMessage += gameTime.ElapsedGameTime;
            if (timeSinceAliveMessage > TimeSpan.FromSeconds(ALIVE_MESSAGE_INTERVAL))
                SendAlive();

            base.Update(gameTime);
        }
    }
}
