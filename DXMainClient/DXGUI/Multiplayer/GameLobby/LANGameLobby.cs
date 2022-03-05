using ClientCore;
using DTAClient.Domain;
using DTAClient.Domain.LAN;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.LAN;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using DTAClient.Online;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class LANGameLobby : MultiplayerGameLobby
    {
        private const int GAME_OPTION_SPECIAL_FLAG_COUNT = 5;

        private const double DROPOUT_TIMEOUT = 20.0;
        private const double GAME_BROADCAST_INTERVAL = 10.0;

        private const string CHAT_COMMAND = "GLCHAT";
        private const string RETURN_COMMAND = "RETURN";
        private const string GET_READY_COMMAND = "GETREADY";
        private const string PLAYER_OPTIONS_REQUEST_COMMAND = "POREQ";
        private const string PLAYER_OPTIONS_BROADCAST_COMMAND = "POPTS";
        private const string PLAYER_JOIN_COMMAND = "JOIN";
        private const string PLAYER_QUIT_COMMAND = "QUIT";
        private const string GAME_OPTIONS_COMMAND = "OPTS";
        private const string PLAYER_READY_REQUEST = "READY";
        private const string LAUNCH_GAME_COMMAND = "LAUNCH";
        private const string FILE_HASH_COMMAND = "FHASH";
        private const string DICE_ROLL_COMMAND = "DR";
        public const string PING = "PING";

        public LANGameLobby(WindowManager windowManager, string iniName,
            TopBar topBar, LANColor[] chatColors, MapLoader mapLoader, DiscordHandler discordHandler) :
            base(windowManager, iniName, topBar, mapLoader, discordHandler)
        {
            this.chatColors = chatColors;
            encoding = Encoding.UTF8;
            hostCommandHandlers = new CommandHandlerBase[]
            {
                new StringCommandHandler(CHAT_COMMAND, GameHost_HandleChatCommand),
                new NoParamCommandHandler(RETURN_COMMAND, GameHost_HandleReturnCommand),
                new StringCommandHandler(PLAYER_OPTIONS_REQUEST_COMMAND, HandlePlayerOptionsRequest),
                new NoParamCommandHandler(PLAYER_QUIT_COMMAND, HandlePlayerQuit),
                new StringCommandHandler(PLAYER_READY_REQUEST, GameHost_HandleReadyRequest),
                new StringCommandHandler(FILE_HASH_COMMAND, HandleFileHashCommand),
                new StringCommandHandler(DICE_ROLL_COMMAND, Host_HandleDiceRoll),
                new NoParamCommandHandler(PING, s => { }),
            };

            playerCommandHandlers = new LANClientCommandHandler[]
            {
                new ClientStringCommandHandler(CHAT_COMMAND, Player_HandleChatCommand),
                new ClientNoParamCommandHandler(GET_READY_COMMAND, HandleGetReadyCommand),
                new ClientStringCommandHandler(RETURN_COMMAND, Player_HandleReturnCommand),
                new ClientStringCommandHandler(PLAYER_OPTIONS_BROADCAST_COMMAND, HandlePlayerOptionsBroadcast),
                new ClientStringCommandHandler(PlayerExtraOptions.LAN_MESSAGE_KEY, HandlePlayerExtraOptionsBroadcast),
                new ClientStringCommandHandler(LAUNCH_GAME_COMMAND, HandleGameLaunchCommand),
                new ClientStringCommandHandler(GAME_OPTIONS_COMMAND, HandleGameOptionsMessage),
                new ClientStringCommandHandler(DICE_ROLL_COMMAND, Client_HandleDiceRoll),
                new ClientNoParamCommandHandler(PING, HandlePing),
            };

            localGame = ClientConfiguration.Instance.LocalGame;

            WindowManager.GameClosing += WindowManager_GameClosing;
        }

        private void WindowManager_GameClosing(object sender, EventArgs e)
        {
            if (client != null && client.Connected)
                Clear();
        }

        private void HandleFileHashCommand(string sender, string fileHash)
        {
            if (fileHash != localFileHash)
                AddNotice(string.Format("{0} has modified game files! They could be cheating!".L10N("UI:Main:PlayerModifiedFiles"), sender));

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            pInfo.Verified = true;
        }

        public event EventHandler<LobbyNotificationEventArgs> LobbyNotification;
        public event EventHandler GameLeft;
        public event EventHandler<GameBroadcastEventArgs> GameBroadcast;

        private TcpListener listener;
        private TcpClient client;

        private IPEndPoint hostEndPoint;
        private LANColor[] chatColors;
        private int chatColorIndex;
        private Encoding encoding;

        private CommandHandlerBase[] hostCommandHandlers;
        private LANClientCommandHandler[] playerCommandHandlers;

        private TimeSpan timeSinceGameBroadcast = TimeSpan.Zero;

        private TimeSpan timeSinceLastReceivedCommand = TimeSpan.Zero;

        private string overMessage = string.Empty;

        private string localGame;

        private string localFileHash;

        public override void Initialize()
        {
            IniNameOverride = nameof(LANGameLobby);
            base.Initialize();
            PostInitialize();
        }

        public void SetUp(bool isHost,
            IPEndPoint hostEndPoint, TcpClient client)
        {
            Refresh(isHost);

            this.hostEndPoint = hostEndPoint;

            if (isHost)
            {
                RandomSeed = new Random().Next();
                Thread thread = new Thread(ListenForClients);
                thread.Start();

                this.client = new TcpClient();
                this.client.Connect("127.0.0.1", ProgramConstants.LAN_GAME_LOBBY_PORT);

                byte[] buffer = encoding.GetBytes(PLAYER_JOIN_COMMAND +
                    ProgramConstants.LAN_DATA_SEPARATOR + ProgramConstants.PLAYERNAME);

                this.client.GetStream().Write(buffer, 0, buffer.Length);
                this.client.GetStream().Flush();

                var fhc = new FileHashCalculator();
                fhc.CalculateHashes(GameModeMaps.GameModes);
                localFileHash = fhc.GetCompleteHash();

                RefreshMapSelectionUI();
            }
            else
            {
                this.client = client;
            }

            new Thread(HandleServerCommunication).Start();

            if (IsHost)
                CopyPlayerDataToUI();

            WindowManager.SelectedControl = tbChatInput;
        }

        public void PostJoin()
        {
            var fhc = new FileHashCalculator();
            fhc.CalculateHashes(GameModeMaps.GameModes);
            SendMessageToHost(FILE_HASH_COMMAND + " " + fhc.GetCompleteHash());
            ResetAutoReadyCheckbox();
        }

        #region Server code

        private void ListenForClients()
        {
            listener = new TcpListener(IPAddress.Any, ProgramConstants.LAN_GAME_LOBBY_PORT);
            listener.Start();

            while (true)
            {
                TcpClient client;

                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception ex)
                {
                    Logger.Log("Listener error: " + ex.Message);
                    break;
                }

                Logger.Log("New client connected from " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());

                if (Players.Count >= MAX_PLAYER_COUNT)
                {
                    Logger.Log("Dropping client because of player limit.");
                    client.Close();
                    continue;
                }

                if (Locked)
                {
                    Logger.Log("Dropping client because the game room is locked.");
                    client.Close();
                    continue;
                }

                LANPlayerInfo lpInfo = new LANPlayerInfo(encoding);
                lpInfo.SetClient(client);

                Thread thread = new Thread(new ParameterizedThreadStart(HandleClientConnection));
                thread.Start(lpInfo);
            }
        }

        private void HandleClientConnection(object clientInfo)
        {
            var lpInfo = (LANPlayerInfo)clientInfo;

            byte[] message = new byte[1024];

            while (true)
            {
                int bytesRead = 0;

                try
                {
                    bytesRead = lpInfo.TcpClient.GetStream().Read(message, 0, message.Length);
                }
                catch (Exception ex)
                {
                    Logger.Log("Socket error with client " + lpInfo.IPAddress + "; removing. Message: " + ex.Message);
                    break;
                }

                if (bytesRead == 0)
                {
                    Logger.Log("Connect attempt from " + lpInfo.IPAddress + " failed! (0 bytes read)");

                    break;
                }

                string msg = encoding.GetString(message, 0, bytesRead);

                string[] command = msg.Split(ProgramConstants.LAN_MESSAGE_SEPARATOR);
                string[] parts = command[0].Split(ProgramConstants.LAN_DATA_SEPARATOR);

                if (parts.Length != 2)
                    break;

                string name = parts[1].Trim();

                if (parts[0] == "JOIN" && !string.IsNullOrEmpty(name))
                {
                    lpInfo.Name = name;

                    AddCallback(new Action<LANPlayerInfo>(AddPlayer), lpInfo);
                    return;
                }

                break;
            }

            if (lpInfo.TcpClient.Connected)
                lpInfo.TcpClient.Close();
        }

        private void AddPlayer(LANPlayerInfo lpInfo)
        {
            if (Players.Find(p => p.Name == lpInfo.Name) != null ||
                Players.Count >= MAX_PLAYER_COUNT || Locked)
                return;

            Players.Add(lpInfo);

            if (IsHost && Players.Count == 1)
                Players[0].Ready = true;

            lpInfo.MessageReceived += LpInfo_MessageReceived;
            lpInfo.ConnectionLost += LpInfo_ConnectionLost;

            AddNotice(string.Format("{0} connected from {1}".L10N("UI:Main:PlayerFromIP"), lpInfo.Name, lpInfo.IPAddress));
            lpInfo.StartReceiveLoop();

            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
            BroadcastPlayerExtraOptions();
            OnGameOptionChanged();
            UpdateDiscordPresence();
        }

        private void LpInfo_ConnectionLost(object sender, EventArgs e)
        {
            var lpInfo = (LANPlayerInfo)sender;
            CleanUpPlayer(lpInfo);
            Players.Remove(lpInfo);

            AddNotice(string.Format("{0} has left the game.".L10N("UI:Main:PlayerLeftGame"), lpInfo.Name));

            CopyPlayerDataToUI();
            BroadcastPlayerOptions();

            if (lpInfo.Name == ProgramConstants.PLAYERNAME)
                ResetDiscordPresence();
            else
                UpdateDiscordPresence();
        }

        private void LpInfo_MessageReceived(object sender, NetworkMessageEventArgs e)
        {
            AddCallback(new Action<string, LANPlayerInfo>(HandleClientMessage),
                e.Message, (LANPlayerInfo)sender);
        }

        private void HandleClientMessage(string data, LANPlayerInfo lpInfo)
        {
            lpInfo.TimeSinceLastReceivedMessage = TimeSpan.Zero;

            foreach (CommandHandlerBase cmdHandler in hostCommandHandlers)
            {
                if (cmdHandler.Handle(lpInfo.Name, data))
                    return;
            }

            Logger.Log("Unknown LAN command from " + lpInfo.ToString() + " : " + data);
        }

        private void CleanUpPlayer(LANPlayerInfo lpInfo)
        {
            lpInfo.MessageReceived -= LpInfo_MessageReceived;
            lpInfo.ConnectionLost -= LpInfo_ConnectionLost;
            lpInfo.TcpClient.Close();
        }

        #endregion

        private void HandleServerCommunication()
        {
            byte[] message = new byte[1024];

            var msg = string.Empty;

            int bytesRead = 0;

            if (!client.Connected)
                return;

            var stream = client.GetStream();

            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = stream.Read(message, 0, message.Length);
                }
                catch (Exception ex)
                {
                    Logger.Log("Reading data from the server failed! Message: " + ex.Message);
                    BtnLeaveGame_LeftClick(this, EventArgs.Empty);
                    break;
                }

                if (bytesRead > 0)
                {
                    msg = encoding.GetString(message, 0, bytesRead);

                    msg = overMessage + msg;
                    List<string> commands = new List<string>();

                    while (true)
                    {
                        int index = msg.IndexOf(ProgramConstants.LAN_MESSAGE_SEPARATOR);

                        if (index == -1)
                        {
                            overMessage = msg;
                            break;
                        }
                        else
                        {
                            commands.Add(msg.Substring(0, index));
                            msg = msg.Substring(index + 1);
                        }
                    }

                    foreach (string cmd in commands)
                    {
                        AddCallback(new Action<string>(HandleMessageFromServer), cmd);
                    }

                    continue;
                }

                Logger.Log("Reading data from the server failed (0 bytes received)!");
                BtnLeaveGame_LeftClick(this, EventArgs.Empty);
                break;
            }
        }

        private void HandleMessageFromServer(string message)
        {
            timeSinceLastReceivedCommand = TimeSpan.Zero;

            foreach (var cmdHandler in playerCommandHandlers)
            {
                if (cmdHandler.Handle(message))
                    return;
            }

            Logger.Log("Unknown LAN command from the server: " + message);
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            Clear();
            GameLeft?.Invoke(this, EventArgs.Empty);
            Disable();
        }

        protected override void UpdateDiscordPresence(bool resetTimer = false)
        {
            if (discordHandler == null)
                return;

            PlayerInfo player = FindLocalPlayer();
            if (player == null || Map == null || GameMode == null)
                return;
            string side = "";
            if (ddPlayerSides.Length > Players.IndexOf(player))
                side = ddPlayerSides[Players.IndexOf(player)].SelectedItem.Text;
            string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby"; // not UI strings

            discordHandler.UpdatePresence(
                Map.Name, GameMode.Name, "LAN",
                currentState, Players.Count, 8, side,
                "LAN Game", IsHost, false, Locked, resetTimer);
        }

        public override void Clear()
        {
            base.Clear();

            if (IsHost)
            {
                BroadcastMessage(PLAYER_QUIT_COMMAND);
                Players.ForEach(p => CleanUpPlayer((LANPlayerInfo)p));
                Players.Clear();
                listener.Stop();
            }
            else
            {
                SendMessageToHost(PLAYER_QUIT_COMMAND);
            }

            if (this.client.Connected)
                this.client.Close();

            ResetDiscordPresence();
        }

        public void SetChatColorIndex(int colorIndex)
        {
            chatColorIndex = colorIndex;
            tbChatInput.TextColor = chatColors[colorIndex].XNAColor;
        }

        public override string GetSwitchName() => "LAN Game Lobby".L10N("UI:Main:LANGameLobby");

        protected override void AddNotice(string message, Color color) =>
            lbChatMessages.AddMessage(null, message, color);

        protected override void BroadcastPlayerOptions()
        {
            if (!IsHost)
                return;

            var sb = new ExtendedStringBuilder(PLAYER_OPTIONS_BROADCAST_COMMAND + " ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
            foreach (PlayerInfo pInfo in Players.Concat(AIPlayers))
            {
                sb.Append(pInfo.Name);
                sb.Append(pInfo.SideId);
                sb.Append(pInfo.ColorId);
                sb.Append(pInfo.StartingLocation);
                sb.Append(pInfo.TeamId);
                if (pInfo.AutoReady && !pInfo.IsInGame)
                    sb.Append(2);
                else
                    sb.Append(Convert.ToInt32(pInfo.IsAI || pInfo.Ready));
                sb.Append(pInfo.IPAddress);
                if (pInfo.IsAI)
                    sb.Append(pInfo.AILevel);
                else
                    sb.Append("-1");
            }

            BroadcastMessage(sb.ToString());
        }

        protected override void BroadcastPlayerExtraOptions()
        {
            var playerExtraOptions = GetPlayerExtraOptions();

            BroadcastMessage(playerExtraOptions.ToLanMessage(), true);
        }

        protected override void HostLaunchGame() => BroadcastMessage(LAUNCH_GAME_COMMAND + " " + UniqueGameID);

        protected override string GetIPAddressForPlayer(PlayerInfo player)
        {
            var lpInfo = (LANPlayerInfo)player;
            return lpInfo.IPAddress;
        }

        protected override void RequestPlayerOptions(int side, int color, int start, int team)
        {
            var sb = new ExtendedStringBuilder(PLAYER_OPTIONS_REQUEST_COMMAND + " ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
            sb.Append(side);
            sb.Append(color);
            sb.Append(start);
            sb.Append(team);
            SendMessageToHost(sb.ToString());
        }

        protected override void RequestReadyStatus() =>
            SendMessageToHost(PLAYER_READY_REQUEST + " " + Convert.ToInt32(chkAutoReady.Checked));

        protected override void SendChatMessage(string message)
        {
            var sb = new ExtendedStringBuilder(CHAT_COMMAND + " ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
            sb.Append(chatColorIndex);
            sb.Append(message);
            SendMessageToHost(sb.ToString());
        }

        protected override void OnGameOptionChanged()
        {
            base.OnGameOptionChanged();

            if (!IsHost)
                return;

            var sb = new ExtendedStringBuilder(GAME_OPTIONS_COMMAND + " ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
            foreach (GameLobbyCheckBox chkBox in CheckBoxes)
            {
                sb.Append(Convert.ToInt32(chkBox.Checked));
            }

            foreach (GameLobbyDropDown dd in DropDowns)
            {
                sb.Append(dd.SelectedIndex);
            }

            sb.Append(RandomSeed);
            sb.Append(Map.SHA1);
            sb.Append(GameMode.Name);
            sb.Append(FrameSendRate);
            sb.Append(Convert.ToInt32(RemoveStartingLocations));

            BroadcastMessage(sb.ToString());
        }

        protected override void GetReadyNotification()
        {
            base.GetReadyNotification();

            WindowManager.FlashWindow();

            if (IsHost)
                BroadcastMessage(GET_READY_COMMAND);
        }

        protected override void ClearPingIndicators()
        {
            // TODO Implement pings for LAN lobbies
        }

        protected override void UpdatePlayerPingIndicator(PlayerInfo pInfo)
        {
            // TODO Implement pings for LAN lobbies
        }

        /// <summary>
        /// Broadcasts a command to all players in the game as the game host.
        /// </summary>
        /// <param name="message">The command to send.</param>
        /// <param name="otherPlayersOnly">If true, only send this to other players. Otherwise, even the sender will receive their message.</param>
        private void BroadcastMessage(string message, bool otherPlayersOnly = false)
        {
            if (!IsHost)
                return;

            foreach (PlayerInfo pInfo in Players.Where(p => !otherPlayersOnly || p.Name != ProgramConstants.PLAYERNAME))
            {
                var lpInfo = (LANPlayerInfo)pInfo;
                lpInfo.SendMessage(message);
            }
        }

        protected override void PlayerExtraOptions_OptionsChanged(object sender, EventArgs e)
        {
            base.PlayerExtraOptions_OptionsChanged(sender, e);
            BroadcastPlayerExtraOptions();
        }

        private void SendMessageToHost(string message)
        {
            if (!client.Connected)
                return;

            byte[] buffer = encoding.GetBytes(message + ProgramConstants.LAN_MESSAGE_SEPARATOR);

            NetworkStream ns = client.GetStream();

            try
            {
                ns.Write(buffer, 0, buffer.Length);
                ns.Flush();
            }
            catch
            {
                Logger.Log("Sending message to game host failed!");
            }
        }

        protected override void UnlockGame(bool manual)
        {
            Locked = false;

            btnLockGame.Text = "Lock Game".L10N("UI:Main:LockGame");

            if (manual)
                AddNotice("You've unlocked the game room.".L10N("UI:Main:RoomUnockedByYou"));
        }

        protected override void LockGame()
        {
            Locked = true;

            btnLockGame.Text = "Unlock Game".L10N("UI:Main:UnlockGame");

            if (Locked)
                AddNotice("You've locked the game room.".L10N("UI:Main:RoomLockedByYou"));
        }

        protected override void GameProcessExited()
        {
            base.GameProcessExited();

            SendMessageToHost(RETURN_COMMAND);

            if (IsHost)
            {
                RandomSeed = new Random().Next();
                OnGameOptionChanged();
                ClearReadyStatuses();
                CopyPlayerDataToUI();
                BroadcastPlayerOptions();
                BroadcastPlayerExtraOptions();

                if (Players.Count < MAX_PLAYER_COUNT)
                {
                    UnlockGame(true);
                }
            }
        }

        private void ReturnNotification(string sender)
        {
            AddNotice(string.Format("{0} has returned from the game.".L10N("UI:Main:PlayerReturned"), sender));

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo != null)
                pInfo.IsInGame = false;

            sndReturnSound.Play();
        }

        public override void Update(GameTime gameTime)
        {
            if (IsHost)
            {
                for (int i = 1; i < Players.Count; i++)
                {
                    LANPlayerInfo lpInfo = (LANPlayerInfo)Players[i];
                    if (!lpInfo.Update(gameTime))
                    {
                        CleanUpPlayer(lpInfo);
                        Players.RemoveAt(i);
                        AddNotice(string.Format("{0} - connection timed out".L10N("UI:Main:PlayerTimeout"), lpInfo.Name));
                        CopyPlayerDataToUI();
                        BroadcastPlayerOptions();
                        BroadcastPlayerExtraOptions();
                        UpdateDiscordPresence();
                        i--;
                    }
                }

                timeSinceGameBroadcast += gameTime.ElapsedGameTime;

                if (timeSinceGameBroadcast > TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL))
                {
                    BroadcastGame();
                    timeSinceGameBroadcast = TimeSpan.Zero;
                }
            }
            else
            {
                timeSinceLastReceivedCommand += gameTime.ElapsedGameTime;

                if (timeSinceLastReceivedCommand > TimeSpan.FromSeconds(DROPOUT_TIMEOUT))
                {
                    LobbyNotification?.Invoke(this,
                        new LobbyNotificationEventArgs("Connection to the game host timed out.".L10N("UI:Main:HostConnectTimeOut")));
                    BtnLeaveGame_LeftClick(this, EventArgs.Empty);
                }
            }

            base.Update(gameTime);
        }

        private void BroadcastGame()
        {
            var sb = new ExtendedStringBuilder("GAME ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
            sb.Append(ProgramConstants.LAN_PROTOCOL_REVISION);
            sb.Append(ProgramConstants.GAME_VERSION);
            sb.Append(localGame);
            sb.Append(Map.Name);
            sb.Append(GameMode.UIName);
            sb.Append(0); // LoadedGameID
            var sbPlayers = new StringBuilder();
            Players.ForEach(p => sbPlayers.Append(p.Name + ","));
            sbPlayers.Remove(sbPlayers.Length - 1, 1);
            sb.Append(sbPlayers.ToString());
            sb.Append(Convert.ToInt32(Locked));
            sb.Append(0); // IsLoadedGame

            GameBroadcast?.Invoke(this, new GameBroadcastEventArgs(sb.ToString()));
        }

        #region Command Handlers

        private void GameHost_HandleChatCommand(string sender, string data)
        {
            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

            if (parts.Length < 2)
                return;

            int colorIndex = Conversions.IntFromString(parts[0], -1);

            if (colorIndex < 0 || colorIndex >= chatColors.Length)
                return;

            BroadcastMessage(CHAT_COMMAND + " " + sender + ProgramConstants.LAN_DATA_SEPARATOR + data);
        }

        private void Player_HandleChatCommand(string data)
        {
            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

            if (parts.Length < 3)
                return;

            string playerName = parts[0];

            int colorIndex = Conversions.IntFromString(parts[1], -1);

            if (colorIndex < 0 || colorIndex >= chatColors.Length)
                return;

            lbChatMessages.AddMessage(new ChatMessage(playerName,
                chatColors[colorIndex].XNAColor, DateTime.Now, parts[2]));
        }

        private void GameHost_HandleReturnCommand(string sender)
        {
            BroadcastMessage(RETURN_COMMAND + ProgramConstants.LAN_DATA_SEPARATOR + sender);
        }

        private void Player_HandleReturnCommand(string sender)
        {
            ReturnNotification(sender);
        }

        private void HandleGetReadyCommand()
        {
            if (!IsHost)
                GetReadyNotification();
        }

        private void HandlePlayerOptionsRequest(string sender, string data)
        {
            if (!IsHost)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

            if (parts.Length != 4)
                return;

            int side = Conversions.IntFromString(parts[0], -1);
            int color = Conversions.IntFromString(parts[1], -1);
            int start = Conversions.IntFromString(parts[2], -1);
            int team = Conversions.IntFromString(parts[3], -1);

            if (side < 0 || side > SideCount + RandomSelectorCount)
                return;

            if (color < 0 || color > MPColors.Count)
                return;

            if (Map.CoopInfo != null)
            {
                if (Map.CoopInfo.DisallowedPlayerSides.Contains(side - 1) || side == SideCount + RandomSelectorCount)
                    return;

                if (Map.CoopInfo.DisallowedPlayerColors.Contains(color - 1))
                    return;
            }

            if (start < 0 || start > Map.MaxPlayers)
                return;

            if (team < 0 || team > 4)
                return;

            if (side != pInfo.SideId
                || start != pInfo.StartingLocation
                || team != pInfo.TeamId)
            {
                ClearReadyStatuses();
            }

            pInfo.SideId = side;
            pInfo.ColorId = color;
            pInfo.StartingLocation = start;
            pInfo.TeamId = team;

            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
        }

        private void HandlePlayerExtraOptionsBroadcast(string data) => ApplyPlayerExtraOptions(null, data);

        private void HandlePlayerOptionsBroadcast(string data)
        {
            if (IsHost)
                return;

            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

            int playerCount = parts.Length / 8;

            if (parts.Length != playerCount * 8)
                return;

            PlayerInfo localPlayer = FindLocalPlayer();
            int oldSideId = localPlayer == null ? -1 : localPlayer.SideId;

            Players.Clear();
            AIPlayers.Clear();

            for (int i = 0; i < playerCount; i++)
            {
                int baseIndex = i * 8;

                string name = parts[baseIndex];
                int side = Conversions.IntFromString(parts[baseIndex + 1], -1);
                int color = Conversions.IntFromString(parts[baseIndex + 2], -1);
                int start = Conversions.IntFromString(parts[baseIndex + 3], -1);
                int team = Conversions.IntFromString(parts[baseIndex + 4], -1);
                int readyStatus = Conversions.IntFromString(parts[baseIndex + 5], -1);
                string ipAddress = parts[baseIndex + 6];
                int aiLevel = Conversions.IntFromString(parts[baseIndex + 7], -1);

                if (side < 0 || side > SideCount + RandomSelectorCount)
                    return;

                if (color < 0 || color > MPColors.Count)
                    return;

                if (start < 0 || start > MAX_PLAYER_COUNT)
                    return;

                if (team < 0 || team > 4)
                    return;

                if (ipAddress == "127.0.0.1")
                    ipAddress = hostEndPoint.Address.ToString();

                bool isAi = aiLevel > -1;
                if (aiLevel > 2)
                    return;

                PlayerInfo pInfo;

                if (!isAi)
                {
                    pInfo = new LANPlayerInfo(encoding);
                    pInfo.Name = name;
                    Players.Add(pInfo);
                }
                else
                {
                    pInfo = new PlayerInfo();
                    pInfo.Name = AILevelToName(aiLevel);
                    pInfo.IsAI = true;
                    pInfo.AILevel = aiLevel;
                    AIPlayers.Add(pInfo);
                }

                pInfo.SideId = side;
                pInfo.ColorId = color;
                pInfo.StartingLocation = start;
                pInfo.TeamId = team;
                pInfo.Ready = readyStatus > 0;
                pInfo.AutoReady = readyStatus > 1;
                pInfo.IPAddress = ipAddress;
            }

            CopyPlayerDataToUI();
            localPlayer = FindLocalPlayer();
            if (localPlayer != null && oldSideId != localPlayer.SideId)
                UpdateDiscordPresence();
        }

        private void HandlePlayerQuit(string sender)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            AddNotice(string.Format("{0} has left the game.".L10N("UI:Main:PlayerLeftGame"), pInfo.Name));
            Players.Remove(pInfo);
            ClearReadyStatuses();
            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
            UpdateDiscordPresence();
        }

        private void HandleGameOptionsMessage(string data)
        {
            if (IsHost)
                return;

            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

            if (parts.Length != CheckBoxes.Count + DropDowns.Count + GAME_OPTION_SPECIAL_FLAG_COUNT)
            {
                AddNotice(("The game host has sent an invalid game options message! " +
                    "The game host's game version might be different from yours.").L10N("UI:Main:HostGameOptionInvalid"));
                Logger.Log("Invalid game options message from host: " + data);
                return;
            }

            int randomSeed = Conversions.IntFromString(parts[parts.Length - GAME_OPTION_SPECIAL_FLAG_COUNT], -1);
            if (randomSeed == -1)
                return;

            RandomSeed = randomSeed;

            string mapSHA1 = parts[parts.Length - (GAME_OPTION_SPECIAL_FLAG_COUNT - 1)];
            string gameMode = parts[parts.Length - (GAME_OPTION_SPECIAL_FLAG_COUNT - 2)];

            GameModeMap gameModeMap = GameModeMaps.Find(gmm => gmm.GameMode.Name == gameMode && gmm.Map.SHA1 == mapSHA1);

            if (gameModeMap == null)
            {
                AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("UI:Main:MapNotExist")+
                    "The host needs to change the map or you won't be able to play.".L10N("UI:Main:HostNeedChangeMapForYou"));
                ChangeMap(null);
                return;
            }

            if (GameModeMap != gameModeMap)
                ChangeMap(gameModeMap);

            int frameSendRate = Conversions.IntFromString(parts[parts.Length - (GAME_OPTION_SPECIAL_FLAG_COUNT - 3)], FrameSendRate);
            if (frameSendRate != FrameSendRate)
            {
                FrameSendRate = frameSendRate;
                AddNotice(string.Format("The game host has changed FrameSendRate (order lag) to {0}".L10N("UI:Main:HostChangeFrameSendRate"), frameSendRate));
            }

            bool removeStartingLocations = Convert.ToBoolean(Conversions.IntFromString(
                parts[parts.Length - (GAME_OPTION_SPECIAL_FLAG_COUNT - 4)], Convert.ToInt32(RemoveStartingLocations)));
            SetRandomStartingLocations(removeStartingLocations);

            for (int i = 0; i < CheckBoxes.Count; i++)
            {
                GameLobbyCheckBox chkBox = CheckBoxes[i];

                bool oldValue = chkBox.Checked;
                chkBox.Checked = Conversions.IntFromString(parts[i], -1) > 0;

                if (chkBox.Checked != oldValue)
                {
                    if (chkBox.Checked)
                        AddNotice(string.Format("The game host has enabled {0}".L10N("UI:Main:HostEnableOption"), chkBox.Text));
                    else
                        AddNotice(string.Format("The game host has disabled {0}".L10N("UI:Main:HostDisableOption"), chkBox.Text));
                }
            }

            for (int i = 0; i < DropDowns.Count; i++)
            {
                int index = Conversions.IntFromString(parts[CheckBoxes.Count + i], -1);

                GameLobbyDropDown dd = DropDowns[i];

                if (index < 0 || index >= dd.Items.Count)
                    return;

                int oldValue = dd.SelectedIndex;
                dd.SelectedIndex = index;

                if (index != oldValue)
                {
                    string ddName = dd.OptionName;
                    if (dd.OptionName == null)
                        ddName = dd.Name;

                    AddNotice(string.Format("The game host has set {0} to {1}".L10N("UI:Main:HostSetOption"), ddName, dd.SelectedItem.Text));
                }
            }
        }

        private void GameHost_HandleReadyRequest(string sender, string autoReady)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            pInfo.Ready = true;
            pInfo.AutoReady = Convert.ToBoolean(Conversions.IntFromString(autoReady, 0));
            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
        }

        private void HandleGameLaunchCommand(string gameId)
        {
            Players.ForEach(pInfo => pInfo.IsInGame = true);
            UniqueGameID = Conversions.IntFromString(gameId, -1);
            if (UniqueGameID < 0)
                return;

            StartGame();
        }

        private void HandlePing()
        {
            SendMessageToHost(PING);
        }

        protected override void BroadcastDiceRoll(int dieSides, int[] results)
        {
            string resultString = string.Join(",", results);
            SendMessageToHost($"DR {dieSides},{resultString}");
        }

        private void Host_HandleDiceRoll(string sender, string result)
        {
            BroadcastMessage($"{DICE_ROLL_COMMAND} {sender}{ProgramConstants.LAN_DATA_SEPARATOR}{result}");
        }

        private void Client_HandleDiceRoll(string data)
        {
            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);
            if (parts.Length != 2)
                return;

            HandleDiceRollResult(parts[0], parts[1]);
        }

        #endregion

        protected override void WriteSpawnIniAdditions(IniFile iniFile)
        {
            base.WriteSpawnIniAdditions(iniFile);

            iniFile.SetIntValue("Settings", "Port", ProgramConstants.LAN_INGAME_PORT);
            iniFile.SetIntValue("Settings", "GameID", UniqueGameID);
            iniFile.SetBooleanValue("Settings", "Host", IsHost);
        }
    }

    public class LobbyNotificationEventArgs : EventArgs
    {
        public LobbyNotificationEventArgs(string notification)
        {
            Notification = notification;
        }

        public string Notification { get; private set; }
    }

    public class GameBroadcastEventArgs : EventArgs
    {
        public GameBroadcastEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }

}
