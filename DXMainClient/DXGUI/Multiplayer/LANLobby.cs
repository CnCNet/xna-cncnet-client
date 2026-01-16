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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using NetworkInterface = System.Net.NetworkInformation.NetworkInterface;
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

        // When a client broadcasts to multiple local interfaces, we may receive the same
        // logical message multiple times from different local source addresses. To avoid
        // showing duplicate messages we remember the last source IP we received a
        // message from for a given username and ignore messages from other IPs for a
        // short grace period. This is a compatibility-friendly approach because it
        // doesn't change the on-wire protocol (no message ids) and keeps behavior
        // reasonable for the common case where duplicated deliveries arrive within a
        // couple of seconds. See also <see cref="IsNotDuplicateMessage(string, IPAddress)"/>.
        private const double DUPLICATE_MESSAGE_IGNORE_SECONDS = 3.0;

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
        Encoding encoding;

        // ====== Player list ======
        readonly object lbPlayerListLock = new object();
        readonly ConcurrentDictionary<string, LANLobbyUser> players = [];
        readonly ConcurrentDictionary<string, PlayerUsernameInfo> playerUsernameInfos = [];
        record PlayerUsernameInfo(int ListIndex, int Count);
        // ========================

        // ====== Player's IP address ======
        readonly object playerIPInfosLock = new object();
        readonly ConcurrentDictionary<string, PlayerIPInfo> playerIPInfos = [];
        record PlayerIPInfo(IPAddress IP, DateTime LastMessageTime);
        // ================================

        // ====== Which network interface should be used to access a player ======
        // Use a concurrent dictionary keyed by local IP string to represent broadcast interfaces. No additional locking is needed.
        // Note: local IP is different from player IP -- local IP binds to the local network interface's IP address.
        readonly ConcurrentDictionary<string, PlayerNetworkInterface> broadcastInterfaces = [];
        record PlayerNetworkInterface(IPAddress LocalIP, IPEndPoint Broadcast);
        // ===================================================================

        // Additional locks for UI controls to ensure thread-safe access
        readonly object lbChatMessagesLock = new object();
        readonly object lbGameListLock = new object();

        Thread listener;

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

        private void LanGameLoadingLobby_GameLeft(object sender, EventArgs e)
        {
            Enable();
        }

        private void WindowManager_GameClosing(object sender, EventArgs e)
        {
            if (socket != null && socket.IsBound)
            {
                try
                {
                    // Must include a trailing space; otherwise HandleNetworkMessage will not process it
                    SendMessage("QUIT ");
                }
                catch (ObjectDisposedException)
                {

                }

                try
                {
                    socket.Close();
                }
                catch (ObjectDisposedException)
                {

                }
            }

            if (listener != null)
            {
                bool success = listener.Join(millisecondsTimeout: 1000);
                if (!success)
                    Logger.Log("Failed to shut down listener after timeout!");
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

        private void AddBroadcastInterfaces()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface iface in interfaces)
            {
                IPInterfaceProperties prop = iface.GetIPProperties();
                UnicastIPAddressInformation info = prop.UnicastAddresses.FirstOrDefault(info =>
                    info.Address.AddressFamily == AddressFamily.InterNetwork);

                if (info == null || info.IPv4Mask == null)
                    continue;

                IPAddress localIPAddress = info.Address;
                byte[] ipBytes = localIPAddress.GetAddressBytes();
                byte[] maskBytes = info.IPv4Mask.GetAddressBytes();
                byte[] broadcastBytes = new byte[ipBytes.Length];
                for (int i = 0; i < ipBytes.Length; i++)
                {
                    broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                }
                IPAddress broadcastIP = new IPAddress(broadcastBytes);

                string key = localIPAddress.ToString();
                var netIf = new PlayerNetworkInterface(localIPAddress, new IPEndPoint(broadcastIP, ProgramConstants.LAN_LOBBY_PORT));
                broadcastInterfaces[key] = netIf;
            }
        }

        public void Open()
        {
            players.Clear();

            lock (lbPlayerListLock)
            {
                lbPlayerList.Clear();
            }

            playerIPInfos.Clear();
            playerUsernameInfos.Clear();

            // This should be synchronized because XNA game lists and other UI objects are not thread-safe.
            // They are accessed both here in Open and from HandleNetworkMessage callbacks.
            // For full thread safety, access must be synchronized wherever games are added, removed, or read.
            // The lock below provides synchronization against concurrent HandleNetworkMessage callbacks,
            // which improves safety even if it does not cover all possible access paths.
            lock (lbGameListLock)
            {
                lbGameList.ClearGames();
            }

            broadcastInterfaces.Clear();

            Visible = true;
            Enabled = true;

            Logger.Log("Creating LAN socket.");

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.EnableBroadcast = true;
                socket.Bind(new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_LOBBY_PORT));
                AddBroadcastInterfaces();
                initSuccess = true;
            }
            catch (SocketException ex)
            {
                Logger.Log("Creating LAN socket failed! Message: " + ex.ToString());
                lbChatMessages.AddMessage(new ChatMessage(Color.Red,
                    "Creating LAN socket failed! Message:".L10N("Client:Main:SocketFailure1") + " " + ex.Message));
                lbChatMessages.AddMessage(new ChatMessage(Color.Red,
                    "Please check your firewall settings.".L10N("Client:Main:SocketFailure2")));
                lbChatMessages.AddMessage(new ChatMessage(Color.Red,
                    "Also make sure that no other application is listening to traffic on UDP ports 1232 - 1234.".L10N("Client:Main:SocketFailure3")));
                initSuccess = false;

                return;
            }

            Logger.Log("Starting listener.");
            listener = new Thread(new ThreadStart(Listen));
            listener.Start();

            SendAlive();
        }

        private void SendMessage(string message)
        {
            if (!initSuccess)
                return;

            byte[] buffer;

            buffer = encoding.GetBytes(message);

            // If there is a socket error when sending to an interface, remove that interface.
            // This is rare, so keep `forDeletion` null by default to avoid allocating a list on every SendMessage.
            List<PlayerNetworkInterface> forDeletion = null;

            foreach (KeyValuePair<string, PlayerNetworkInterface> kvp in broadcastInterfaces)
            {
                try
                {
                    socket.SendTo(buffer, kvp.Value.Broadcast);
                }
                catch (SocketException)
                {
                    forDeletion ??= new List<PlayerNetworkInterface>();
                    forDeletion.Add(kvp.Value);
                }
            }

            if (forDeletion != null)
            {
                foreach (var iface in forDeletion)
                {
                    string key = iface.LocalIP.ToString();
                    broadcastInterfaces.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// Decide whether to accept a message from the given username that arrived
        /// from the specified IP address. This implements a local duplicate-suppression
        /// heuristic: remember the last source IP for a username and ignore messages
        /// from different IPs for a short period defined by
        /// <see cref="DUPLICATE_MESSAGE_IGNORE_SECONDS"/>.
        ///
        /// Rationale:
        /// - Clients broadcast on all local interfaces which can cause some receivers
        ///   to get the same logical packet multiple times (once per interface).
        /// - Introducing message IDs would require a protocol change and would break
        ///   compatibility with older clients, so we avoid it here.
        /// - This heuristic may drop messages briefly if a client's primary interface
        ///   fails and they switch to another interface within the grace period, and
        ///   it may result in a late duplicate being delivered after the grace period.
        ///   Both cases are considered acceptably rare on LANs.
        /// </summary>
        private bool IsNotDuplicateMessage(string username, IPAddress ip)
        {
            lock (playerIPInfosLock)
            {
                DateTime now = DateTime.Now;

                if (!playerIPInfos.TryGetValue(username, out PlayerIPInfo existing))
                {
                    // New username - accept and add
                    playerIPInfos[username] = new PlayerIPInfo(ip, now);
                    return true;
                }

                if (existing.IP.Equals(ip))
                {
                    // Same IP: accept and update timestamp
                    playerIPInfos[username] = new PlayerIPInfo(ip, now);
                    return true;
                }

                if ((now - existing.LastMessageTime).TotalSeconds >= DUPLICATE_MESSAGE_IGNORE_SECONDS)
                {
                    // Different IP but grace period expired: accept and update to new IP
                    playerIPInfos[username] = new PlayerIPInfo(ip, now);
                    return true;
                }

                // Different IP within grace period: reject and keep existing entry
                return false;
            }
        }

        private void Listen()
        {
            try
            {
                while (true)
                {
                    EndPoint endPoint = new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_LOBBY_PORT);
                    byte[] buffer = new byte[4096];
                    int receivedBytes = 0;
                    receivedBytes = socket.ReceiveFrom(buffer, ref endPoint);

                    IPEndPoint ipEndPoint = (IPEndPoint)endPoint;
                    string data = encoding.GetString(buffer, 0, receivedBytes);

                    if (data == string.Empty)
                        continue;

                    AddCallback(new Action<string, IPEndPoint>(HandleNetworkMessage), data, ipEndPoint);
                }
            }
            catch (Exception ex)
            {
                if (ex is SocketException socketEx && socketEx.SocketErrorCode == SocketError.Interrupted)
                {
                    // Do nothing; this is the expected way for the listener thread to end.
                }
                else
                {
                    Logger.Log("LAN socket listener: exception: " + ex.ToString());
                }
            }
        }

        private void PlayerListAdd(string username, Texture2D texture)
        {
            lock (lbPlayerListLock)
            {
                // Check if username already exists
                if (playerUsernameInfos.TryGetValue(username, out PlayerUsernameInfo existingInfo))
                {
                    // Increment count atomically
                    var updated = new PlayerUsernameInfo(existingInfo.ListIndex, existingInfo.Count + 1);
                    playerUsernameInfos[username] = updated;
                    return;
                }

                // Add new entry
                int index = lbPlayerList.Items.Count;
                var newInfo = new PlayerUsernameInfo(index, 1);
                playerUsernameInfos[username] = newInfo;
                lbPlayerList.AddItem(username, texture);
            }
        }

        private void PlayerListRemove(string username)
        {
            lock (lbPlayerListLock)
            {
                if (!playerUsernameInfos.TryGetValue(username, out PlayerUsernameInfo info))
                    return;

                if (info.Count == 1)
                {
                    int idx = info.ListIndex;

                    // Decrement ListIndex for entries after the removed index
                    // Create a snapshot and collect entries to update in one pass
                    var entriesToUpdate = new List<KeyValuePair<string, PlayerUsernameInfo>>();
                    foreach (var kvp in playerUsernameInfos.ToArray())
                    {
                        if (kvp.Value.ListIndex > idx)
                        {
                            entriesToUpdate.Add(kvp);
                        }
                    }

                    // Update the entries
                    foreach (var entry in entriesToUpdate)
                    {
                        var updated = new PlayerUsernameInfo(entry.Value.ListIndex - 1, entry.Value.Count);
                        playerUsernameInfos[entry.Key] = updated;
                    }

                    // Remove the username and remove UI item
                    playerUsernameInfos.TryRemove(username, out _);
                    lbPlayerList.RemoveItem(idx);
                }
                else
                {
                    // Decrement count
                    var updated = new PlayerUsernameInfo(info.ListIndex, info.Count - 1);
                    playerUsernameInfos[username] = updated;
                }
            }
        }

        // NOTE: LAN protocol messages are expected to contain a command and a parameter
        // section separated by a space. Due to the parsing logic below (data.Split(' ')
        // and the commandAndParams.Length < 2 check), messages that do not contain at
        // least one space are treated as invalid and are ignored.
        private void HandleNetworkMessage(string data, IPEndPoint endPoint)
        {
            string[] commandAndParams = data.Split(' ');

            if (commandAndParams.Length < 2)
                return;

            string command = commandAndParams[0];

            string[] parameters = data.Substring(command.Length + 1).Split(
                new char[] { ProgramConstants.LAN_DATA_SEPARATOR });

            string key = endPoint.ToString();
            players.TryGetValue(key, out LANLobbyUser user);

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

                        var newUser = new LANLobbyUser(name, gameTexture, endPoint);

                        // Use GetOrAdd to ensure atomicity: only add if not present
                        // If the returned value is our new instance, we added it; otherwise another thread did
                        user = players.GetOrAdd(key, newUser);

                        // Only add to player list if we successfully added a new user
                        if (ReferenceEquals(user, newUser))
                        {
                            PlayerListAdd(user.Name, gameTexture);
                        }
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

                    if (!IsNotDuplicateMessage(user.Name, endPoint.Address))
                        break;

                    lock (lbChatMessagesLock)
                    {
                        lbChatMessages.AddMessage(new ChatMessage(user.Name,
                            chatColors[colorIndex].XNAColor, DateTime.Now, parameters[1]));
                    }

                    break;

                case "QUIT":
                    if (user == null)
                        return;

                    if (players.TryRemove(key, out LANLobbyUser removed))
                    {
                        PlayerListRemove(removed.Name);
                    }

                    break;

                case "GAME":
                    if (user == null)
                        return;

                    HostedLANGame game = new HostedLANGame();
                    if (!game.SetDataFromStringArray(gameCollection, parameters))
                        return;

                    game.EndPoint = endPoint;

                    lock (lbGameListLock)
                    {
                        int existingGameIndex =
                            lbGameList.HostedGames.FindIndex(g => ((HostedLANGame)g).EndPoint.Equals(endPoint));

                        if (existingGameIndex > -1)
                            lbGameList.HostedGames[existingGameIndex] = game;
                        else
                        {
                            lbGameList.HostedGames.Add(game);
                        }

                        lbGameList.Refresh();
                    }

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
                    string.Format("The selected game is for {0}!".L10N("Client:Main:GameIsOfPurpose"), gameCollection.GetGameNameFromInternalName(hg.Game.InternalName)));
                return;
            }

            if (hg.Locked)
            {
                lbChatMessages.AddMessage("The selected game is locked!".L10N("Client:Main:GameLocked"));
                return;
            }

            if (hg.IsLoadedGame)
            {
                if (!hg.Players.Contains(ProgramConstants.PLAYERNAME))
                {
                    lbChatMessages.AddMessage("You do not exist in the saved game!".L10N("Client:Main:NotInSavedGame"));
                    return;
                }
            }
            else
            {
                if (hg.Players.Contains(ProgramConstants.PLAYERNAME))
                {
                    lbChatMessages.AddMessage("Your name is already taken in the game.".L10N("Client:Main:NameOccupied"));
                    return;
                }
            }

            if (hg.GameVersion != ProgramConstants.GAME_VERSION)
            {
                // TODO Show warning
            }

            lbChatMessages.AddMessage(string.Format("Attempting to join game {0} ...".L10N("Client:Main:AttemptJoin"), hg.RoomName));

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
                lbChatMessages.AddMessage(null,
                    "Connecting to the game failed! Message:".L10N("Client:Main:ConnectGameFailed") + " " + ex.Message, Color.White);
            }
        }

        private void BtnMainMenu_LeftClick(object sender, EventArgs e)
        {
            Visible = false;
            Enabled = false;
            // The trailing space is required because HandleNetworkMessage expects this exact format.
            // Do not remove it unless the message parsing in HandleNetworkMessage is updated accordingly.
            SendMessage("QUIT ");
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
            // Copy values to a list to avoid enumerating the concurrent dictionary while modifying it.
            var playersCopy = players.Values.ToList();
            foreach (var player in playersCopy)
            {
                player.TimeWithoutRefresh += gameTime.ElapsedGameTime;

                if (player.TimeWithoutRefresh > TimeSpan.FromSeconds(INACTIVITY_REMOVE_TIME))
                {
                    string key = player.EndPoint.ToString();
                    if (players.TryRemove(key, out LANLobbyUser removed))
                    {
                        PlayerListRemove(removed.Name);
                    }
                }
            }

            timeSinceAliveMessage += gameTime.ElapsedGameTime;
            if (timeSinceAliveMessage > TimeSpan.FromSeconds(ALIVE_MESSAGE_INTERVAL))
                SendAlive();

            base.Update(gameTime);
        }
    }
}
