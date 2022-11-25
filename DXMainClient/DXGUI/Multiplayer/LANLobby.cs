using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.LAN;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.LAN;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DTAClient.DXGUI.Multiplayer
{
    class LANLobby : XNAWindow
    {
        private const double ALIVE_MESSAGE_INTERVAL = 5.0;
        private const double INACTIVITY_REMOVE_TIME = 10.0;

        public LANLobby(
            WindowManager windowManager,
            GameCollection gameCollection,
            MapLoader mapLoader,
            DiscordHandler discordHandler
        ) : base(windowManager)
        {
            this.gameCollection = gameCollection;
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

        private List<GameMode> gameModes => mapLoader.GameModes;

        Socket socket;
        IPEndPoint endPoint;
        Encoding encoding;

        List<LANLobbyUser> players = new List<LANLobbyUser>();

        TimeSpan timeSinceAliveMessage = TimeSpan.Zero;

        MapLoader mapLoader;

        DiscordHandler discordHandler;

        bool initSuccess;

        private CancellationTokenSource cancellationTokenSource;

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
            btnNewGame.LeftClick += (_, _) => BtnNewGame_LeftClickAsync();

            btnJoinGame = new XNAClientButton(WindowManager);
            btnJoinGame.Name = "btnJoinGame";
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.Right + 12,
                btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnJoinGame.Text = "Join Game".L10N("UI:Main:JoinGame");
            btnJoinGame.LeftClick += (_, _) => BtnJoinGame_LeftClickAsync();

            btnMainMenu = new XNAClientButton(WindowManager);
            btnMainMenu.Name = "btnMainMenu";
            btnMainMenu.ClientRectangle = new Rectangle(Width - 145,
                btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnMainMenu.Text = "Main Menu".L10N("UI:Main:MainMenu");
            btnMainMenu.LeftClick += (_, _) => BtnMainMenu_LeftClickAsync();

            lbGameList = new GameListBox(WindowManager, localGame, null);
            lbGameList.Name = "lbGameList";
            lbGameList.ClientRectangle = new Rectangle(btnNewGame.X,
                41, btnJoinGame.Right - btnNewGame.X,
                btnNewGame.Y - 53);
            lbGameList.GameLifetime = 15.0; // Smaller lifetime in LAN
            lbGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameList.DoubleLeftClick += (_, _) => LbGameList_DoubleLeftClickAsync();
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
            tbChatInput.EnterPressed += (_, _) => TbChatInput_EnterPressedAsync(cancellationTokenSource?.Token ?? default);

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

            gameCreationWindow.NewGame += (_, _) => GameCreationWindow_NewGameAsync();
            gameCreationWindow.LoadGame += (_, e) => GameCreationWindow_LoadGameAsync(e);

            var assembly = Assembly.GetAssembly(typeof(GameCollection));
            using Stream unknownIconStream = assembly.GetManifestResourceStream("ClientCore.Resources.unknownicon.png");

            unknownGameIcon = AssetLoader.TextureFromImage(Image.Load(unknownIconStream));

            encoding = Encoding.UTF8;

            base.Initialize();

            CenterOnParent();
            gameCreationPanel.SetPositionAndSize();

            lanGameLobby = new LANGameLobby(WindowManager, "MultiplayerGameLobby",
                null, chatColors, mapLoader, discordHandler);
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
            lanGameLobby.GameBroadcast += (_, e) => LanGameLobby_GameBroadcastAsync(e, cancellationTokenSource?.Token ?? default);

            lanGameLoadingLobby.GameBroadcast += (_, e) => LanGameLoadingLobby_GameBroadcastAsync(e, cancellationTokenSource?.Token ?? default);
            lanGameLoadingLobby.GameLeft += LanGameLoadingLobby_GameLeft;

            WindowManager.GameClosing += (_, _) => WindowManager_GameClosingAsync(cancellationTokenSource?.Token ?? default);
        }

        private void LanGameLoadingLobby_GameLeft(object sender, EventArgs e)
            => Enable();

        private async Task WindowManager_GameClosingAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (socket == null)
                    return;

                if (socket.IsBound)
                {
                    await SendMessageAsync("QUIT", cancellationToken);
                    cancellationTokenSource.Cancel();
                    socket.Close();
                }
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private Task LanGameLobby_GameBroadcastAsync(GameBroadcastEventArgs e, CancellationToken cancellationToken)
            => SendMessageAsync(e.Message, cancellationToken);

        private void LanGameLobby_GameLeft(object sender, EventArgs e)
            => Enable();

        private Task LanGameLoadingLobby_GameBroadcastAsync(GameBroadcastEventArgs e, CancellationToken cancellationToken)
            => SendMessageAsync(e.Message, cancellationToken);

        private async Task GameCreationWindow_LoadGameAsync(GameLoadEventArgs e)
        {
            try
            {
                await lanGameLoadingLobby.SetUpAsync(true, null, e.LoadedGameID);

                lanGameLoadingLobby.Enable();
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task GameCreationWindow_NewGameAsync()
        {
            try
            {
                await lanGameLobby.SetUpAsync(true,
                    new IPEndPoint(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT), null);

                lanGameLobby.Enable();
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
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

        public async Task OpenAsync()
        {
            players.Clear();
            lbPlayerList.Clear();
            lbGameList.ClearGames();

            Visible = true;
            Enabled = true;

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            Logger.Log("Creating LAN socket.");

            try
            {
                socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                socket.EnableBroadcast = true;
                socket.Bind(new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_LOBBY_PORT));
                endPoint = new IPEndPoint(IPAddress.Broadcast, ProgramConstants.LAN_LOBBY_PORT);
                initSuccess = true;
            }
            catch (SocketException ex)
            {
                PreStartup.LogException(ex, "Creating LAN socket failed!");
                lbChatMessages.AddMessage(new ChatMessage(Color.Red,
                    "Creating LAN socket failed! Message:".L10N("UI:Main:SocketFailure1") + " " + ex.Message));
                lbChatMessages.AddMessage(new ChatMessage(Color.Red,
                    "Please check your firewall settings.".L10N("UI:Main:SocketFailure2")));
                lbChatMessages.AddMessage(new ChatMessage(Color.Red,
                    $"Also make sure that no other application is listening to traffic on UDP ports" +
                    $" {ProgramConstants.LAN_LOBBY_PORT} - {ProgramConstants.LAN_INGAME_PORT}.".L10N("UI:Main:SocketFailure3")));
                initSuccess = false;
                return;
            }

            Logger.Log("Starting listener.");
            ListenAsync(cancellationTokenSource.Token);

            await SendAliveAsync(cancellationTokenSource.Token);
        }

        private async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            try
            {
                if (!initSuccess)
                    return;

                const int charSize = sizeof(char);
                int bufferSize = message.Length * charSize;
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
                Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
                int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);

                buffer = buffer[..bytes];

                await socket.SendToAsync(buffer, SocketFlags.None, endPoint, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            try
            {
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);

                while (!cancellationToken.IsCancellationRequested)
                {
                    EndPoint ep = new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_GAME_LOBBY_PORT);
                    Memory<byte> buffer = memoryOwner.Memory[..4096];
                    SocketReceiveFromResult socketReceiveFromResult = await socket.ReceiveFromAsync(buffer, SocketFlags.None, ep, cancellationToken);
                    var iep = (IPEndPoint)socketReceiveFromResult.RemoteEndPoint;
                    string data = encoding.GetString(buffer.Span[..socketReceiveFromResult.ReceivedBytes]);

                    if (data == string.Empty)
                        continue;

                    AddCallback(() => HandleNetworkMessage(data, iep));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                PreStartup.LogException(ex, "LAN socket listener exception.");
            }
        }

        private void HandleNetworkMessage(string data, IPEndPoint endPoint)
        {
            string[] commandAndParams = data.Split(' ');

            if (commandAndParams.Length < 2)
                return;

            string command = commandAndParams[0];

            string[] parameters = data.Substring(command.Length + 1).Split(
                new[] { ProgramConstants.LAN_DATA_SEPARATOR },
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

        private async Task SendAliveAsync(CancellationToken cancellationToken)
        {
            try
            {
                StringBuilder sb = new StringBuilder("ALIVE ");
                sb.Append(localGameIndex);
                sb.Append(ProgramConstants.LAN_DATA_SEPARATOR);
                sb.Append(ProgramConstants.PLAYERNAME);
                await SendMessageAsync(sb.ToString(), cancellationToken);
                timeSinceAliveMessage = TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task TbChatInput_EnterPressedAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(tbChatInput.Text))
                    return;

                string chatMessage = tbChatInput.Text.Replace((char)01, '?');

                StringBuilder sb = new StringBuilder("CHAT ");
                sb.Append(ddColor.SelectedIndex);
                sb.Append(ProgramConstants.LAN_DATA_SEPARATOR);
                sb.Append(chatMessage);

                await SendMessageAsync(sb.ToString(), cancellationToken);

                tbChatInput.Text = string.Empty;
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task LbGameList_DoubleLeftClickAsync()
        {
            try
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
                    var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await client.ConnectAsync(new IPEndPoint(hg.EndPoint.Address, ProgramConstants.LAN_GAME_LOBBY_PORT), CancellationToken.None);

                    const int charSize = sizeof(char);

                    if (hg.IsLoadedGame)
                    {
                        var spawnSGIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));
                        int loadedGameId = spawnSGIni.GetIntValue("Settings", "GameID", -1);

                        await lanGameLoadingLobby.SetUpAsync(false, client, loadedGameId);
                        lanGameLoadingLobby.Enable();

                        string message = "JOIN" + ProgramConstants.LAN_DATA_SEPARATOR +
                            ProgramConstants.PLAYERNAME + ProgramConstants.LAN_DATA_SEPARATOR +
                            loadedGameId + ProgramConstants.LAN_MESSAGE_SEPARATOR;
                        int bufferSize = message.Length * charSize;
                        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
                        Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
                        int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);
                        buffer = buffer[..bytes];

                        await client.SendAsync(buffer, SocketFlags.None, CancellationToken.None);
                        await lanGameLoadingLobby.PostJoinAsync();
                    }
                    else
                    {
                        await lanGameLobby.SetUpAsync(false, hg.EndPoint, client);
                        lanGameLobby.Enable();

                        string message = "JOIN" + ProgramConstants.LAN_DATA_SEPARATOR +
                            ProgramConstants.PLAYERNAME + ProgramConstants.LAN_MESSAGE_SEPARATOR;
                        int bufferSize = message.Length * charSize;
                        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
                        Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
                        int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);
                        buffer = buffer[..bytes];

                        await client.SendAsync(buffer, SocketFlags.None, CancellationToken.None);
                        await lanGameLobby.PostJoinAsync();
                    }
                }
                catch (Exception ex)
                {
                    PreStartup.LogException(ex, "Connecting to the game failed!");
                    lbChatMessages.AddMessage(null,
                        "Connecting to the game failed! Message:".L10N("UI:Main:ConnectGameFailed") + " " + ex.Message, Color.White);
                }
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task BtnMainMenu_LeftClickAsync()
        {
            try
            {
                Visible = false;
                Enabled = false;
                await SendMessageAsync("QUIT", CancellationToken.None);
                cancellationTokenSource.Cancel();
                socket.Close();
                Exited?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private Task BtnJoinGame_LeftClickAsync()
            => LbGameList_DoubleLeftClickAsync();

        private async Task BtnNewGame_LeftClickAsync()
        {
            try
            {
                if (!ClientConfiguration.Instance.DisableMultiplayerGameLoading)
                    gameCreationWindow.Open();
                else
                    await GameCreationWindow_NewGameAsync();
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
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
                Task.Run(() => SendAliveAsync(cancellationTokenSource?.Token ?? default)).Wait();

            base.Update(gameTime);
        }
    }
}