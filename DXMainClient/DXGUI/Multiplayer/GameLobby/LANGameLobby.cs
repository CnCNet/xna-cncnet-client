using ClientCore;
using DTAClient.Domain;
using DTAClient.Domain.LAN;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.LAN;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using DTAClient.Online;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClientCore.Extensions;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    internal sealed class LANGameLobby : MultiplayerGameLobby
    {
        private const int GAME_OPTION_SPECIAL_FLAG_COUNT = 5;

        private const double DROPOUT_TIMEOUT = 20.0;
        private const double GAME_BROADCAST_INTERVAL = 10.0;

        public LANGameLobby(
            WindowManager windowManager,
            string iniName,
            TopBar topBar,
            LANColor[] chatColors,
            MapLoader mapLoader,
            DiscordHandler discordHandler)
            : base(windowManager, iniName, topBar, mapLoader, discordHandler)
        {
            this.chatColors = chatColors;
            encoding = Encoding.UTF8;
            hostCommandHandlers = new CommandHandlerBase[]
            {
                new StringCommandHandler(LANCommands.CHAT_LOBBY_COMMAND, (sender, data) => GameHost_HandleChatCommandAsync(sender, data).HandleTask()),
                new NoParamCommandHandler(LANCommands.RETURN, sender => GameHost_HandleReturnCommandAsync(sender).HandleTask()),
                new StringCommandHandler(LANCommands.PLAYER_OPTIONS_REQUEST, (sender, data) => HandlePlayerOptionsRequestAsync(sender, data).HandleTask()),
                new NoParamCommandHandler(LANCommands.PLAYER_QUIT_COMMAND, sender => HandlePlayerQuitAsync(sender).HandleTask()),
                new StringCommandHandler(LANCommands.PLAYER_READY_REQUEST, (sender, autoReady) => GameHost_HandleReadyRequestAsync(sender, autoReady).HandleTask()),
                new StringCommandHandler(LANCommands.FILE_HASH, HandleFileHashCommand),
                new StringCommandHandler(LANCommands.DICE_ROLL, (sender, result) => Host_HandleDiceRollAsync(sender, result).HandleTask()),
                new NoParamCommandHandler(LANCommands.PING, _ => { })
            };

            playerCommandHandlers = new LANClientCommandHandler[]
            {
                new ClientStringCommandHandler(LANCommands.CHAT_LOBBY_COMMAND, Player_HandleChatCommand),
                new ClientNoParamCommandHandler(LANCommands.GET_READY, () => HandleGetReadyCommandAsync().HandleTask()),
                new ClientStringCommandHandler(LANCommands.RETURN, Player_HandleReturnCommand),
                new ClientStringCommandHandler(LANCommands.PLAYER_OPTIONS_BROADCAST, HandlePlayerOptionsBroadcast),
                new ClientStringCommandHandler(LANCommands.PLAYER_EXTRA_OPTIONS, HandlePlayerExtraOptionsBroadcast),
                new ClientStringCommandHandler(LANCommands.LAUNCH_GAME, gameId => HandleGameLaunchCommandAsync(gameId).HandleTask()),
                new ClientStringCommandHandler(LANCommands.GAME_OPTIONS, data => HandleGameOptionsMessageAsync(data).HandleTask()),
                new ClientStringCommandHandler(LANCommands.DICE_ROLL, Client_HandleDiceRoll),
                new ClientNoParamCommandHandler(LANCommands.PING, () => HandlePingAsync().HandleTask())
            };

            localGame = ClientConfiguration.Instance.LocalGame;

            WindowManager.GameClosing += (_, _) => WindowManager_GameClosingAsync().HandleTask();
        }

        private async ValueTask WindowManager_GameClosingAsync()
        {
            if (client is { Connected: true })
                await ClearAsync(true).ConfigureAwait(false);

            cancellationTokenSource?.Cancel();
        }

        private void HandleFileHashCommand(string sender, string fileHash)
        {
            if (fileHash != localFileHash)
                AddNotice(string.Format("{0} has modified game files! They could be cheating!".L10N("Client:Main:PlayerModifiedFiles"), sender));

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            pInfo.Verified = true;
            CopyPlayerDataToUI();
        }

        public event EventHandler GameLeft;
        public event EventHandler<GameBroadcastEventArgs> GameBroadcast;

        private Socket listener;
        private Socket client;

        private IPEndPoint hostEndPoint;
        private LANColor[] chatColors;
        private int chatColorIndex;
        private Encoding encoding;

        private CommandHandlerBase[] hostCommandHandlers;
        private LANClientCommandHandler[] playerCommandHandlers;

        private TimeSpan timeSinceGameBroadcast = TimeSpan.Zero;

        private TimeSpan timeSinceLastReceivedCommand = TimeSpan.Zero;

        private string overMessage = string.Empty;

        private readonly string localGame;

        private string localFileHash;

        private EventHandler lpInfo_ConnectionLostFunc;

        private CancellationTokenSource cancellationTokenSource;

        public override void Initialize()
        {
            IniNameOverride = nameof(LANGameLobby);
            lpInfo_ConnectionLostFunc = (sender, _) => LpInfo_ConnectionLostAsync(sender).HandleTask();
            base.Initialize();
            PostInitialize();
        }

        public async ValueTask SetUpAsync(bool isHost, IPEndPoint hostEndPoint, Socket client)
        {
            Refresh(isHost);

            this.hostEndPoint = hostEndPoint;

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            if (isHost)
            {
                RandomSeed = new Random().Next();
                ListenForClientsAsync(cancellationTokenSource.Token).HandleTask();
                SendHostPlayerJoinedMessageAsync(cancellationTokenSource.Token).HandleTask();

                var fhc = new FileHashCalculator();
                fhc.CalculateHashes(GameModeMaps.GameModes);
                localFileHash = fhc.GetCompleteHash();

                await RefreshMapSelectionUIAsync().ConfigureAwait(false);
            }
            else
            {
                this.client = client;
            }

            HandleServerCommunicationAsync(cancellationTokenSource.Token).HandleTask();

            if (IsHost)
                CopyPlayerDataToUI();

            WindowManager.SelectedControl = tbChatInput;
            btnLaunchGame.Enabled = true;
        }

        private async ValueTask SendHostPlayerJoinedMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                client = new Socket(SocketType.Stream, ProtocolType.Tcp);

                await client.ConnectAsync(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT, cancellationToken).ConfigureAwait(false);

                string message = LANCommands.PLAYER_JOIN + ProgramConstants.LAN_DATA_SEPARATOR + ProgramConstants.PLAYERNAME;
                const int charSize = sizeof(char);
                int bufferSize = message.Length * charSize;
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
                Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
                int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);

                buffer = buffer[..bytes];

                await client.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async ValueTask PostJoinAsync()
        {
            var fhc = new FileHashCalculator();
            fhc.CalculateHashes(GameModeMaps.GameModes);
            await SendMessageToHostAsync(LANCommands.FILE_HASH + " " + fhc.GetCompleteHash(), cancellationTokenSource?.Token ?? default).ConfigureAwait(false);
            ResetAutoReadyCheckbox();
        }

        #region Server code

        private async ValueTask ListenForClientsAsync(CancellationToken cancellationToken)
        {
            listener = new Socket(SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(new IPEndPoint(IPAddress.IPv6Any, ProgramConstants.LAN_GAME_LOBBY_PORT));
            listener.Listen();

            while (!cancellationToken.IsCancellationRequested)
            {
                Socket newClient;

                try
                {
                    newClient = await listener.AcceptAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex, "Listener error.");
                    break;
                }

                Logger.Log("New client connected from " + ((IPEndPoint)newClient.RemoteEndPoint).Address);

                if (Players.Count >= MAX_PLAYER_COUNT)
                {
                    Logger.Log("Dropping client because of player limit.");
                    newClient.Shutdown(SocketShutdown.Both);
                    newClient.Close();
                    continue;
                }

                if (Locked)
                {
                    Logger.Log("Dropping client because the game room is locked.");
                    newClient.Shutdown(SocketShutdown.Both);
                    newClient.Close();
                    continue;
                }

                LANPlayerInfo lpInfo = new LANPlayerInfo(encoding);
                lpInfo.SetClient(newClient);

                HandleClientConnectionAsync(lpInfo, cancellationToken).HandleTask();
            }
        }

        private async ValueTask HandleClientConnectionAsync(LANPlayerInfo lpInfo, CancellationToken cancellationToken)
        {
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;
                Memory<byte> message;

                try
                {
                    message = memoryOwner.Memory[..1024];
                    bytesRead = await lpInfo.TcpClient.ReceiveAsync(message, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex, "Socket error with client " + lpInfo.IPAddress + "; removing.");
                    break;
                }

                if (bytesRead == 0)
                {
                    Logger.Log("Connect attempt from " + lpInfo.IPAddress + " failed! (0 bytes read)");

                    break;
                }

                string msg = encoding.GetString(message.Span[..bytesRead]);
                string[] command = msg.Split(ProgramConstants.LAN_MESSAGE_SEPARATOR);
                string[] parts = command[0].Split(ProgramConstants.LAN_DATA_SEPARATOR);

                if (parts.Length != 2)
                    break;

                string name = parts[1].Trim();

                if (parts[0] == LANCommands.PLAYER_JOIN && !string.IsNullOrEmpty(name))
                {
                    lpInfo.Name = name;

                    AddCallback(() => AddPlayerAsync(lpInfo, cancellationToken).HandleTask());
                    return;
                }

                break;
            }

            lpInfo.TcpClient.Shutdown(SocketShutdown.Both);
            lpInfo.TcpClient.Close();
        }

        private async ValueTask AddPlayerAsync(LANPlayerInfo lpInfo, CancellationToken cancellationToken)
        {
            if (Players.Find(p => p.Name == lpInfo.Name) != null ||
                Players.Count >= MAX_PLAYER_COUNT || Locked)
                return;

            Players.Add(lpInfo);

            if (IsHost && Players.Count == 1)
                Players[0].Ready = true;

            lpInfo.MessageReceived += LpInfo_MessageReceived;
            lpInfo.ConnectionLost += lpInfo_ConnectionLostFunc;

            AddNotice(string.Format("{0} connected from {1}".L10N("Client:Main:PlayerFromIP"), lpInfo.Name, lpInfo.IPAddress));
            lpInfo.StartReceiveLoopAsync(cancellationToken).HandleTask();

            CopyPlayerDataToUI();
            await BroadcastPlayerOptionsAsync().ConfigureAwait(false);
            await BroadcastPlayerExtraOptionsAsync().ConfigureAwait(false);
            await OnGameOptionChangedAsync().ConfigureAwait(false);
            UpdateDiscordPresence();
        }

        private async ValueTask LpInfo_ConnectionLostAsync(object sender)
        {
            var lpInfo = (LANPlayerInfo)sender;
            CleanUpPlayer(lpInfo);
            Players.Remove(lpInfo);

            AddNotice(string.Format("{0} has left the game.".L10N("Client:Main:PlayerLeftGame"), lpInfo.Name));

            CopyPlayerDataToUI();
            await BroadcastPlayerOptionsAsync().ConfigureAwait(false);

            if (lpInfo.Name == ProgramConstants.PLAYERNAME)
                ResetDiscordPresence();
            else
                UpdateDiscordPresence();
        }

        private void LpInfo_MessageReceived(object sender, NetworkMessageEventArgs e)
        {
            AddCallback(() => HandleClientMessage(e.Message, (LANPlayerInfo)sender));
        }

        private void HandleClientMessage(string data, LANPlayerInfo lpInfo)
        {
            lpInfo.TimeSinceLastReceivedMessage = TimeSpan.Zero;

            foreach (CommandHandlerBase cmdHandler in hostCommandHandlers)
            {
                if (cmdHandler.Handle(lpInfo.Name, data))
                    return;
            }

            Logger.Log("Unknown LAN command from " + lpInfo + " : " + data);
        }

        private void CleanUpPlayer(LANPlayerInfo lpInfo)
        {
            lpInfo.MessageReceived -= LpInfo_MessageReceived;
            lpInfo.ConnectionLost -= lpInfo_ConnectionLostFunc;
            lpInfo.TcpClient.Shutdown(SocketShutdown.Both);
            lpInfo.TcpClient.Close();
        }

        #endregion

        private async ValueTask HandleServerCommunicationAsync(CancellationToken cancellationToken)
        {
            if (!client.Connected)
                return;

            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;
                Memory<byte> message;

                try
                {
                    message = memoryOwner.Memory[..1024];
                    bytesRead = await client.ReceiveAsync(message, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex, "Reading data from the server failed!");
                    await BtnLeaveGame_LeftClickAsync().ConfigureAwait(false);
                    break;
                }

                if (bytesRead > 0)
                {
                    string msg = encoding.GetString(message.Span[..bytesRead]);

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

                        commands.Add(msg[..index]);
                        msg = msg[(index + 1)..];
                    }

                    foreach (string cmd in commands)
                    {
                        AddCallback(() => HandleMessageFromServer(cmd));
                    }

                    continue;
                }

                Logger.Log("Reading data from the server failed (0 bytes received)!");
                await BtnLeaveGame_LeftClickAsync().ConfigureAwait(false);
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

        protected override async ValueTask BtnLeaveGame_LeftClickAsync()
        {
            await ClearAsync(false).ConfigureAwait(false);
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
                side = (string)ddPlayerSides[Players.IndexOf(player)].SelectedItem.Tag;
            string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby"; // not UI strings

            discordHandler.UpdatePresence(
                Map.UntranslatedName, GameMode.UntranslatedUIName, "LAN",
                currentState, Players.Count, 8, side,
                "LAN Game", IsHost, false, Locked, resetTimer);
        }

        public override async ValueTask ClearAsync(bool exiting)
        {
            await base.ClearAsync(exiting).ConfigureAwait(false);

            if (IsHost)
            {
                await BroadcastMessageAsync(LANCommands.PLAYER_QUIT_COMMAND).ConfigureAwait(false);
                Players.ForEach(p => CleanUpPlayer((LANPlayerInfo)p));
                Players.Clear();

                if (listener.Connected)
                    listener.Shutdown(SocketShutdown.Both);

                listener.Close();
            }
            else
            {
                await SendMessageToHostAsync(LANCommands.PLAYER_QUIT_COMMAND, cancellationTokenSource?.Token ?? default).ConfigureAwait(false);
            }

            cancellationTokenSource.Cancel();

            if (client.Connected)
                client.Shutdown(SocketShutdown.Both);

            client.Close();
            ResetDiscordPresence();
        }

        public void SetChatColorIndex(int colorIndex)
        {
            chatColorIndex = colorIndex;
            tbChatInput.TextColor = chatColors[colorIndex].XNAColor;
        }

        public override string GetSwitchName() => "LAN Game Lobby".L10N("Client:Main:LANGameLobby");

        protected override void AddNotice(string message, Color color) =>
            lbChatMessages.AddMessage(null, message, color);

        protected override async ValueTask BroadcastPlayerOptionsAsync()
        {
            if (!IsHost)
                return;

            var sb = new ExtendedStringBuilder(LANCommands.PLAYER_OPTIONS_BROADCAST + " ", true);
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

            await BroadcastMessageAsync(sb.ToString()).ConfigureAwait(false);
        }

        protected override async ValueTask BroadcastPlayerExtraOptionsAsync()
        {
            var playerExtraOptions = GetPlayerExtraOptions();

            await BroadcastMessageAsync(playerExtraOptions.ToLanMessage(), true).ConfigureAwait(false);
        }

        protected override ValueTask HostLaunchGameAsync() => BroadcastMessageAsync(LANCommands.LAUNCH_GAME + " " + UniqueGameID);

        protected override IPAddress GetIPAddressForPlayer(PlayerInfo player)
        {
            var lpInfo = (LANPlayerInfo)player;
            return lpInfo.IPAddress.MapToIPv4();
        }

        protected override ValueTask RequestPlayerOptionsAsync(int side, int color, int start, int team)
        {
            var sb = new ExtendedStringBuilder(LANCommands.PLAYER_OPTIONS_REQUEST + " ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
            sb.Append(side);
            sb.Append(color);
            sb.Append(start);
            sb.Append(team);
            return SendMessageToHostAsync(sb.ToString(), cancellationTokenSource?.Token ?? default);
        }

        protected override ValueTask RequestReadyStatusAsync()
        {
            return SendMessageToHostAsync(LANCommands.PLAYER_READY_REQUEST + " " + Convert.ToInt32(chkAutoReady.Checked), cancellationTokenSource?.Token ?? default);
        }

        protected override ValueTask SendChatMessageAsync(string message)
        {
            var sb = new ExtendedStringBuilder(LANCommands.CHAT_LOBBY_COMMAND + " ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
            sb.Append(chatColorIndex);
            sb.Append(message);
            return SendMessageToHostAsync(sb.ToString(), cancellationTokenSource?.Token ?? default);
        }

        protected override async ValueTask OnGameOptionChangedAsync()
        {
            await base.OnGameOptionChangedAsync().ConfigureAwait(false);

            if (!IsHost)
                return;

            var sb = new ExtendedStringBuilder(LANCommands.GAME_OPTIONS + " ", true);
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

            await BroadcastMessageAsync(sb.ToString()).ConfigureAwait(false);
        }

        protected override async ValueTask GetReadyNotificationAsync()
        {
            await base.GetReadyNotificationAsync().ConfigureAwait(false);
#if WINFORMS
            WindowManager.FlashWindow();
#endif

            if (IsHost)
                await BroadcastMessageAsync(LANCommands.GET_READY).ConfigureAwait(false);
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
        private async ValueTask BroadcastMessageAsync(string message, bool otherPlayersOnly = false)
        {
            if (!IsHost)
                return;

            foreach (PlayerInfo pInfo in Players.Where(p => !otherPlayersOnly || p.Name != ProgramConstants.PLAYERNAME))
            {
                var lpInfo = (LANPlayerInfo)pInfo;
                await lpInfo.SendMessageAsync(message, cancellationTokenSource?.Token ?? default).ConfigureAwait(false);
            }
        }

        protected override async ValueTask PlayerExtraOptions_OptionsChangedAsync()
        {
            await base.PlayerExtraOptions_OptionsChangedAsync().ConfigureAwait(false);
            await BroadcastPlayerExtraOptionsAsync().ConfigureAwait(false);
        }

        private async ValueTask SendMessageToHostAsync(string message, CancellationToken cancellationToken)
        {
            if (!client.Connected)
                return;

            message += ProgramConstants.LAN_MESSAGE_SEPARATOR;

            try
            {
                const int charSize = sizeof(char);
                int bufferSize = message.Length * charSize;
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
                Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
                int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);

                buffer = buffer[..bytes];

                await client.SendAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Sending message to game host failed!");
            }
        }

        protected override ValueTask UnlockGameAsync(bool manual)
        {
            Locked = false;

            btnLockGame.Text = "Lock Game".L10N("Client:Main:LockGame");

            if (manual)
                AddNotice("You've unlocked the game room.".L10N("Client:Main:RoomUnockedByYou"));

            return ValueTask.CompletedTask;
        }

        protected override ValueTask LockGameAsync()
        {
            Locked = true;

            btnLockGame.Text = "Unlock Game".L10N("Client:Main:UnlockGame");

            if (Locked)
                AddNotice("You've locked the game room.".L10N("Client:Main:RoomLockedByYou"));

            return ValueTask.CompletedTask;
        }

        protected override async ValueTask GameProcessExitedAsync()
        {
            await base.GameProcessExitedAsync().ConfigureAwait(false);
            await SendMessageToHostAsync(LANCommands.RETURN, cancellationTokenSource?.Token ?? default).ConfigureAwait(false);

            if (IsHost)
            {
                RandomSeed = new Random().Next();
                await OnGameOptionChangedAsync().ConfigureAwait(false);
                ClearReadyStatuses();
                CopyPlayerDataToUI();
                await BroadcastPlayerOptionsAsync().ConfigureAwait(false);
                await BroadcastPlayerExtraOptionsAsync().ConfigureAwait(false);

                if (Players.Count < MAX_PLAYER_COUNT)
                    await UnlockGameAsync(true).ConfigureAwait(false);
            }
        }

        private void ReturnNotification(string sender)
        {
            AddNotice(string.Format("{0} has returned from the game.".L10N("Client:Main:PlayerReturned"), sender));

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo != null)
                pInfo.IsInGame = false;

            sndReturnSound.Play();
            CopyPlayerDataToUI();
        }

        public override void Update(GameTime gameTime)
        {
            if (IsHost)
            {
                for (int i = 1; i < Players.Count; i++)
                {
                    LANPlayerInfo lpInfo = (LANPlayerInfo)Players[i];
                    if (!Task.Run(() => lpInfo.UpdateAsync(gameTime).HandleTask(true)).Result)
                    {
                        CleanUpPlayer(lpInfo);
                        Players.RemoveAt(i);
                        AddNotice(string.Format("{0} - connection timed out".L10N("Client:Main:PlayerTimeout"), lpInfo.Name));
                        CopyPlayerDataToUI();
                        Task.Run(() => BroadcastPlayerOptionsAsync().HandleTask()).Wait();
                        Task.Run(() => BroadcastPlayerExtraOptionsAsync().HandleTask()).Wait();
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
                    Task.Run(() => BtnLeaveGame_LeftClickAsync().HandleTask()).Wait();
            }

            base.Update(gameTime);
        }

        private void BroadcastGame()
        {
            if (GameMode == null || Map == null)
                return;

            var sb = new ExtendedStringBuilder(LANCommands.GAME + " ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;
            sb.Append(ProgramConstants.LAN_PROTOCOL_REVISION);
            sb.Append(ProgramConstants.GAME_VERSION);
            sb.Append(localGame);
            sb.Append(Map.UntranslatedName);
            sb.Append(GameMode.UntranslatedUIName);
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

        private async ValueTask GameHost_HandleChatCommandAsync(string sender, string data)
        {
            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

            if (parts.Length < 2)
                return;

            int colorIndex = Conversions.IntFromString(parts[0], -1);

            if (colorIndex < 0 || colorIndex >= chatColors.Length)
                return;

            await BroadcastMessageAsync(LANCommands.CHAT_LOBBY_COMMAND + " " + sender + ProgramConstants.LAN_DATA_SEPARATOR + data).ConfigureAwait(false);
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

        private ValueTask GameHost_HandleReturnCommandAsync(string sender)
            => BroadcastMessageAsync(LANCommands.RETURN + ProgramConstants.LAN_DATA_SEPARATOR + sender);

        private void Player_HandleReturnCommand(string sender)
        {
            ReturnNotification(sender);
        }

        private async ValueTask HandleGetReadyCommandAsync()
        {
            if (!IsHost)
                await GetReadyNotificationAsync().ConfigureAwait(false);
        }

        private async ValueTask HandlePlayerOptionsRequestAsync(string sender, string data)
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
            await BroadcastPlayerOptionsAsync().ConfigureAwait(false);
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
                var ipAddress = IPAddress.Parse(parts[baseIndex + 6]);
                int aiLevel = Conversions.IntFromString(parts[baseIndex + 7], -1);

                if (side < 0 || side > SideCount + RandomSelectorCount)
                    return;

                if (color < 0 || color > MPColors.Count)
                    return;

                if (start < 0 || start > MAX_PLAYER_COUNT)
                    return;

                if (team < 0 || team > 4)
                    return;

                if (IPAddress.IsLoopback(ipAddress))
                    ipAddress = hostEndPoint.Address.MapToIPv4();

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

        private async ValueTask HandlePlayerQuitAsync(string sender)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            AddNotice(string.Format("{0} has left the game.".L10N("Client:Main:PlayerLeftGame"), pInfo.Name));
            Players.Remove(pInfo);
            ClearReadyStatuses();
            CopyPlayerDataToUI();
            await BroadcastPlayerOptionsAsync().ConfigureAwait(false);
            UpdateDiscordPresence();
        }

        private async ValueTask HandleGameOptionsMessageAsync(string data)
        {
            if (IsHost)
                return;

            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

            if (parts.Length != CheckBoxes.Count + DropDowns.Count + GAME_OPTION_SPECIAL_FLAG_COUNT)
            {
                AddNotice(("The game host has sent an invalid game options message! " +
                    "The game host's game version might be different from yours.").L10N("Client:Main:HostGameOptionInvalid"));
                Logger.Log("Invalid game options message from host: " + data);
                return;
            }

            int randomSeed = Conversions.IntFromString(parts[^GAME_OPTION_SPECIAL_FLAG_COUNT], -1);
            if (randomSeed == -1)
                return;

            RandomSeed = randomSeed;

            string mapSHA1 = parts[^(GAME_OPTION_SPECIAL_FLAG_COUNT - 1)];
            string gameMode = parts[^(GAME_OPTION_SPECIAL_FLAG_COUNT - 2)];

            GameModeMap gameModeMap = GameModeMaps.Find(gmm => gmm.GameMode.Name == gameMode && gmm.Map.SHA1 == mapSHA1);

            if (gameModeMap == null)
            {
                AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("Client:Main:MapNotExist") +
                    "The host needs to change the map or you won't be able to play.".L10N("Client:Main:HostNeedChangeMapForYou"));
                await ChangeMapAsync(null).ConfigureAwait(false);
                return;
            }

            if (GameModeMap != gameModeMap)
                await ChangeMapAsync(gameModeMap).ConfigureAwait(false);

            int frameSendRate = Conversions.IntFromString(parts[^(GAME_OPTION_SPECIAL_FLAG_COUNT - 3)], FrameSendRate);
            if (frameSendRate != FrameSendRate)
            {
                FrameSendRate = frameSendRate;
                AddNotice(string.Format("The game host has changed FrameSendRate (order lag) to {0}".L10N("Client:Main:HostChangeFrameSendRate"), frameSendRate));
            }

            bool removeStartingLocations = Convert.ToBoolean(Conversions.IntFromString(
                parts[^(GAME_OPTION_SPECIAL_FLAG_COUNT - 4)], Convert.ToInt32(RemoveStartingLocations)));
            SetRandomStartingLocations(removeStartingLocations);

            for (int i = 0; i < CheckBoxes.Count; i++)
            {
                GameLobbyCheckBox chkBox = CheckBoxes[i];

                bool oldValue = chkBox.Checked;
                chkBox.Checked = Conversions.IntFromString(parts[i], -1) > 0;

                if (chkBox.Checked != oldValue)
                {
                    if (chkBox.Checked)
                        AddNotice(string.Format("The game host has enabled {0}".L10N("Client:Main:HostEnableOption"), chkBox.Text));
                    else
                        AddNotice(string.Format("The game host has disabled {0}".L10N("Client:Main:HostDisableOption"), chkBox.Text));
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

                    AddNotice(string.Format("The game host has set {0} to {1}".L10N("Client:Main:HostSetOption"), ddName, dd.SelectedItem.Text));
                }
            }
        }

        private async ValueTask GameHost_HandleReadyRequestAsync(string sender, string autoReady)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            pInfo.Ready = true;
            pInfo.AutoReady = Convert.ToBoolean(Conversions.IntFromString(autoReady, 0));
            CopyPlayerDataToUI();
            await BroadcastPlayerOptionsAsync().ConfigureAwait(false);
        }

        private async ValueTask HandleGameLaunchCommandAsync(string gameId)
        {
            Players.ForEach(pInfo => pInfo.IsInGame = true);
            UniqueGameID = Conversions.IntFromString(gameId, -1);

            if (UniqueGameID < 0)
                return;

            CopyPlayerDataToUI();
            await StartGameAsync().ConfigureAwait(false);
        }

        private ValueTask HandlePingAsync()
            => SendMessageToHostAsync(LANCommands.PING, cancellationTokenSource?.Token ?? default);

        protected override async ValueTask BroadcastDiceRollAsync(int dieSides, int[] results)
        {
            string resultString = string.Join(",", results);
            await SendMessageToHostAsync($"{LANCommands.DICE_ROLL} {dieSides},{resultString}", cancellationTokenSource?.Token ?? default).ConfigureAwait(false);
        }

        private ValueTask Host_HandleDiceRollAsync(string sender, string result)
            => BroadcastMessageAsync($"{LANCommands.DICE_ROLL} {sender}{ProgramConstants.LAN_DATA_SEPARATOR}{result}");

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

    public class GameBroadcastEventArgs : EventArgs
    {
        public GameBroadcastEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}