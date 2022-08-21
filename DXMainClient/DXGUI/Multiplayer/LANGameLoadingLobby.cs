using ClientCore;
using DTAClient.Domain;
using DTAClient.Domain.LAN;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.LAN;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
#if !NETFRAMEWORK
using System.Buffers;
#endif
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.DXGUI.Multiplayer
{
    class LANGameLoadingLobby : GameLoadingLobbyBase
    {
        private const double DROPOUT_TIMEOUT = 20.0;
        private const double GAME_BROADCAST_INTERVAL = 10.0;

        private const string OPTIONS_COMMAND = "OPTS";
        private const string GAME_LAUNCH_COMMAND = "START";
        private const string READY_STATUS_COMMAND = "READY";
        private const string CHAT_COMMAND = "CHAT";
        private const string PLAYER_QUIT_COMMAND = "QUIT";
        private const string PLAYER_JOIN_COMMAND = "JOIN";
        private const string FILE_HASH_COMMAND = "FHASH";

        public LANGameLoadingLobby(
            WindowManager windowManager,
            LANColor[] chatColors,
            MapLoader mapLoader,
            DiscordHandler discordHandler
            ) : base(windowManager, discordHandler)
        {
            encoding = ProgramConstants.LAN_ENCODING;
            this.chatColors = chatColors;
            this.mapLoader = mapLoader;

            localGame = ClientConfiguration.Instance.LocalGame;

            hostCommandHandlers = new LANServerCommandHandler[]
            {
                new ServerStringCommandHandler(CHAT_COMMAND, (sender, data) => Server_HandleChatMessageAsync(sender, data)),
                new ServerStringCommandHandler(FILE_HASH_COMMAND, Server_HandleFileHashMessage),
                new ServerNoParamCommandHandler(READY_STATUS_COMMAND, sender => Server_HandleReadyRequestAsync(sender))
            };

            playerCommandHandlers = new LANClientCommandHandler[]
            {
                new ClientStringCommandHandler(CHAT_COMMAND, Client_HandleChatMessage),
                new ClientStringCommandHandler(OPTIONS_COMMAND, Client_HandleOptionsMessage),
                new ClientNoParamCommandHandler(GAME_LAUNCH_COMMAND, Client_HandleStartCommand)
            };

            WindowManager.GameClosing += (_, _) => WindowManager_GameClosingAsync();
        }

        private async Task WindowManager_GameClosingAsync()
        {
            if (client is { Connected: true })
                await ClearAsync();
        }

        public event EventHandler<GameBroadcastEventArgs> GameBroadcast;

        private Socket listener;
        private Socket client;

        private readonly LANColor[] chatColors;
        private readonly MapLoader mapLoader;
        private int chatColorIndex;
        private readonly Encoding encoding;

        private readonly LANServerCommandHandler[] hostCommandHandlers;
        private readonly LANClientCommandHandler[] playerCommandHandlers;

        private TimeSpan timeSinceGameBroadcast = TimeSpan.Zero;

        private TimeSpan timeSinceLastReceivedCommand = TimeSpan.Zero;

        private string overMessage = string.Empty;

        private readonly string localGame;

        private string localFileHash;

        private List<GameMode> gameModes => mapLoader.GameModes;

        private int loadedGameId;

        private bool started;

        private CancellationTokenSource cancellationTokenSource;

        public async Task SetUpAsync(bool isHost, Socket client, int loadedGameId)
        {
            Refresh(isHost);

            this.loadedGameId = loadedGameId;

            started = false;

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            if (isHost)
            {
                ListenForClientsAsync(cancellationTokenSource.Token);

                this.client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await this.client.ConnectAsync(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT);

                string message = PLAYER_JOIN_COMMAND +
                     ProgramConstants.LAN_DATA_SEPARATOR + ProgramConstants.PLAYERNAME +
                     ProgramConstants.LAN_DATA_SEPARATOR + loadedGameId;

#if NETFRAMEWORK
                byte[] buffer1 = encoding.GetBytes(message);
                var buffer = new ArraySegment<byte>(buffer1);

                await this.client.SendAsync(buffer, SocketFlags.None);
#else
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(message.Length * 2);
                Memory<byte> buffer = memoryOwner.Memory[..(message.Length * 2)];
                int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);
                buffer = buffer[..bytes];

                await this.client.SendAsync(buffer, SocketFlags.None, CancellationToken.None);
#endif

                var fhc = new FileHashCalculator();
                fhc.CalculateHashes(gameModes);
                localFileHash = fhc.GetCompleteHash();
            }
            else
            {
                this.client?.Dispose();
                this.client = client;
            }

            HandleServerCommunicationAsync(cancellationTokenSource.Token);

            if (IsHost)
                CopyPlayerDataToUI();

            WindowManager.SelectedControl = tbChatInput;
        }

        public async Task PostJoinAsync()
        {
            var fhc = new FileHashCalculator();
            fhc.CalculateHashes(gameModes);
            await SendMessageToHostAsync(FILE_HASH_COMMAND + " " + fhc.GetCompleteHash(), cancellationTokenSource?.Token ?? default);
            UpdateDiscordPresence(true);
        }

        #region Server code

        private async Task ListenForClientsAsync(CancellationToken cancellationToken)
        {
            try
            {
                listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, ProgramConstants.LAN_GAME_LOBBY_PORT));
#if NETFRAMEWORK
                listener.Listen(int.MaxValue);
#else
                listener.Listen();
#endif

                while (!cancellationToken.IsCancellationRequested)
                {
                    Socket newPlayerSocket;

#if NETFRAMEWORK
                    try
                    {
                        newPlayerSocket = await listener.AcceptAsync();
                    }
#else
                    try
                    {
                        newPlayerSocket = await listener.AcceptAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
#endif
                    catch (Exception ex)
                    {
                        PreStartup.LogException(ex, "Listener error.");
                        break;
                    }

                    Logger.Log("New client connected from " + ((IPEndPoint)newPlayerSocket.RemoteEndPoint).Address);

                    LANPlayerInfo lpInfo = new LANPlayerInfo(encoding);
                    lpInfo.SetClient(newPlayerSocket);

                    HandleClientConnectionAsync(lpInfo, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task HandleClientConnectionAsync(LANPlayerInfo lpInfo, CancellationToken cancellationToken)
        {
            try
            {
#if !NETFRAMEWORK
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

#endif
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead;
#if NETFRAMEWORK
                    byte[] buffer1;
#else
                    Memory<byte> message;
#endif

                    try
                    {
#if NETFRAMEWORK
                        buffer1 = new byte[1024];
                        var message = new ArraySegment<byte>(buffer1);
                        bytesRead = await client.ReceiveAsync(message, SocketFlags.None);
                    }
#else
                        message = memoryOwner.Memory[..1024];
                        bytesRead = await client.ReceiveAsync(message, SocketFlags.None, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
#endif
                    catch (Exception ex)
                    {
                        PreStartup.LogException(ex, "Socket error with client " + lpInfo.IPAddress + "; removing.");
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        Logger.Log("Connect attempt from " + lpInfo.IPAddress + " failed! (0 bytes read)");

                        break;
                    }

#if NETFRAMEWORK
                    string msg = encoding.GetString(buffer1, 0, bytesRead);
#else
                    string msg = encoding.GetString(message.Span[..bytesRead]);
#endif
                    string[] command = msg.Split(ProgramConstants.LAN_MESSAGE_SEPARATOR);
                    string[] parts = command[0].Split(ProgramConstants.LAN_DATA_SEPARATOR);

                    if (parts.Length != 3)
                        break;

                    string name = parts[1].Trim();
                    int loadedGameId = Conversions.IntFromString(parts[2], -1);

                    if (parts[0] == "JOIN" && !string.IsNullOrEmpty(name)
                        && loadedGameId == this.loadedGameId)
                    {
                        lpInfo.Name = name;

                        AddCallback(() => AddPlayerAsync(lpInfo, cancellationToken));
                        return;
                    }

                    break;
                }

                if (lpInfo.TcpClient.Connected)
                    lpInfo.TcpClient.Close();
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task AddPlayerAsync(LANPlayerInfo lpInfo, CancellationToken cancellationToken)
        {
            try
            {
                if (Players.Find(p => p.Name == lpInfo.Name) != null ||
                    Players.Count >= SGPlayers.Count ||
                    SGPlayers.Find(p => p.Name == lpInfo.Name) == null)
                {
                    lpInfo.TcpClient.Close();
                    return;
                }

                if (Players.Count == 0)
                    lpInfo.Ready = true;

                Players.Add(lpInfo);

                lpInfo.MessageReceived += LpInfo_MessageReceived;
                lpInfo.ConnectionLost += (sender, _) => LpInfo_ConnectionLostAsync(sender);

                sndJoinSound.Play();

                AddNotice(string.Format("{0} connected from {1}".L10N("Client:Main:PlayerFromIP"), lpInfo.Name, lpInfo.IPAddress));
                lpInfo.StartReceiveLoop(cancellationToken);

                CopyPlayerDataToUI();
                await BroadcastOptionsAsync();
                UpdateDiscordPresence();
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task LpInfo_ConnectionLostAsync(object sender)
        {
            try
            {
                var lpInfo = (LANPlayerInfo)sender;
                CleanUpPlayer(lpInfo);
                Players.Remove(lpInfo);

                AddNotice(string.Format("{0} has left the game.".L10N("Client:Main:PlayerLeftGame"), lpInfo.Name));

                sndLeaveSound.Play();

                CopyPlayerDataToUI();
                await BroadcastOptionsAsync();
                UpdateDiscordPresence();
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private void LpInfo_MessageReceived(object sender, NetworkMessageEventArgs e)
        {
            AddCallback(() => HandleClientMessage(e.Message, (LANPlayerInfo)sender));
        }

        private void HandleClientMessage(string data, LANPlayerInfo lpInfo)
        {
            lpInfo.TimeSinceLastReceivedMessage = TimeSpan.Zero;

            foreach (var cmdHandler in hostCommandHandlers)
            {
                if (cmdHandler.Handle(lpInfo, data))
                    return;
            }

            Logger.Log("Unknown LAN command from " + lpInfo + " : " + data);
        }

        private void CleanUpPlayer(LANPlayerInfo lpInfo)
        {
            lpInfo.MessageReceived -= LpInfo_MessageReceived;
            lpInfo.TcpClient.Close();
        }

        #endregion

        private async Task HandleServerCommunicationAsync(CancellationToken cancellationToken)
        {
            if (!client.Connected)
                return;

#if !NETFRAMEWORK
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

#endif
            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;
#if NETFRAMEWORK
                byte[] buffer1;
#else
                Memory<byte> message;
#endif

                try
                {
#if NETFRAMEWORK
                    buffer1 = new byte[1024];
                    var message = new ArraySegment<byte>(buffer1);
                    bytesRead = await client.ReceiveAsync(message, SocketFlags.None);
                }
#else
                    message = memoryOwner.Memory[..1024];
                    bytesRead = await client.ReceiveAsync(message, SocketFlags.None, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
#endif
                catch (Exception ex)
                {
                    Logger.Log("Reading data from the server failed! Message: " + ex.Message);
                    await LeaveGameAsync();
                    break;
                }

                if (bytesRead > 0)
                {
#if NETFRAMEWORK
                    string msg = encoding.GetString(buffer1, 0, bytesRead);
#else
                    string msg = encoding.GetString(message.Span[..bytesRead]);
#endif

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

                        commands.Add(msg.Substring(0, index));
                        msg = msg.Substring(index + 1);
                    }

                    foreach (string cmd in commands)
                    {
                        AddCallback(() => HandleMessageFromServer(cmd));
                    }

                    continue;
                }

                Logger.Log("Reading data from the server failed (0 bytes received)!");
                await LeaveGameAsync();
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

        protected override async Task LeaveGameAsync()
        {
            try
            {
                await ClearAsync();
                Disable();

                await base.LeaveGameAsync();
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task ClearAsync()
        {
            if (IsHost)
            {
                await BroadcastMessageAsync(PLAYER_QUIT_COMMAND, CancellationToken.None);
                Players.ForEach(p => CleanUpPlayer((LANPlayerInfo)p));
                Players.Clear();
                listener.Close();
            }
            else
            {
                await SendMessageToHostAsync(PLAYER_QUIT_COMMAND, CancellationToken.None);
            }

            cancellationTokenSource.Cancel();

            if (client.Connected)
                client.Close();
        }

        protected override void AddNotice(string message, Color color)
        {
            lbChatMessages.AddMessage(null, message, color);
        }

        protected override async Task BroadcastOptionsAsync()
        {
            if (Players.Count > 0)
                Players[0].Ready = true;

            var sb = new ExtendedStringBuilder(OPTIONS_COMMAND + " ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;

            sb.Append(ddSavedGame.SelectedIndex);

            foreach (PlayerInfo pInfo in Players)
            {
                sb.Append(pInfo.Name);
                sb.Append(Convert.ToInt32(pInfo.Ready));
                sb.Append(pInfo.IPAddress);
            }

            await BroadcastMessageAsync(sb.ToString(), cancellationTokenSource?.Token ?? default);
        }

        protected override Task HostStartGameAsync()
            => BroadcastMessageAsync(GAME_LAUNCH_COMMAND, cancellationTokenSource?.Token ?? default);

        protected override Task RequestReadyStatusAsync()
            => SendMessageToHostAsync(READY_STATUS_COMMAND, cancellationTokenSource?.Token ?? default);

        protected override async Task SendChatMessageAsync(string message)
        {
            await SendMessageToHostAsync(CHAT_COMMAND + " " + chatColorIndex +
                ProgramConstants.LAN_DATA_SEPARATOR + message, cancellationTokenSource?.Token ?? default);

            sndMessageSound.Play();
        }

        #region Server's command handlers

        private async Task Server_HandleChatMessageAsync(LANPlayerInfo sender, string data)
        {
            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

            if (parts.Length < 2)
                return;

            int colorIndex = Conversions.IntFromString(parts[0], -1);

            if (colorIndex < 0 || colorIndex >= chatColors.Length)
                return;

            await BroadcastMessageAsync(CHAT_COMMAND + " " + sender +
                ProgramConstants.LAN_DATA_SEPARATOR + colorIndex +
                ProgramConstants.LAN_DATA_SEPARATOR + data, cancellationTokenSource?.Token ?? default);
        }

        private void Server_HandleFileHashMessage(LANPlayerInfo sender, string hash)
        {
            if (hash != localFileHash)
                AddNotice(string.Format("{0} - modified files detected! They could be cheating!".L10N("Client:Main:PlayerCheating"), sender.Name), Color.Red);
            sender.Verified = true;
        }

        private async Task Server_HandleReadyRequestAsync(LANPlayerInfo sender)
        {
            try
            {
                if (!sender.Ready)
                {
                    sender.Ready = true;
                    CopyPlayerDataToUI();
                    await BroadcastOptionsAsync();
                }
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        #endregion

        #region Client's command handlers

        private void Client_HandleChatMessage(string data)
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

            sndMessageSound.Play();
        }

        private void Client_HandleOptionsMessage(string data)
        {
            if (IsHost)
                return;

            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);
            const int PLAYER_INFO_PARTS = 3;
            int pCount = (parts.Length - 1) / PLAYER_INFO_PARTS;

            if (pCount * PLAYER_INFO_PARTS + 1 != parts.Length)
                return;

            int savedGameIndex = Conversions.IntFromString(parts[0], -1);
            if (savedGameIndex < 0 || savedGameIndex >= ddSavedGame.Items.Count)
            {
                return;
            }

            ddSavedGame.SelectedIndex = savedGameIndex;

            Players.Clear();

            for (int i = 0; i < pCount; i++)
            {
                int baseIndex = 1 + i * PLAYER_INFO_PARTS;
                string pName = parts[baseIndex];
                bool ready = Conversions.IntFromString(parts[baseIndex + 1], -1) > 0;
                string ipAddress = parts[baseIndex + 2];

                LANPlayerInfo pInfo = new LANPlayerInfo(encoding);
                pInfo.Name = pName;
                pInfo.Ready = ready;
                pInfo.IPAddress = ipAddress;
                Players.Add(pInfo);
            }

            if (Players.Count > 0) // Set IP of host
                Players[0].IPAddress = ((IPEndPoint)client.RemoteEndPoint).Address.ToString();

            CopyPlayerDataToUI();
        }

        private void Client_HandleStartCommand()
        {
            started = true;

            LoadGame();
        }

        #endregion

        /// <summary>
        /// Broadcasts a command to all players in the game as the game host.
        /// </summary>
        /// <param name="message">The command to send.</param>
        private async Task BroadcastMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (!IsHost)
                return;

            foreach (PlayerInfo pInfo in Players)
            {
                var lpInfo = (LANPlayerInfo)pInfo;
                await lpInfo.SendMessageAsync(message, cancellationToken);
            }
        }

        private async Task SendMessageToHostAsync(string message, CancellationToken cancellationToken)
        {
            if (!client.Connected)
                return;

            message += ProgramConstants.LAN_MESSAGE_SEPARATOR;

#if NETFRAMEWORK
            byte[] buffer1 = encoding.GetBytes(message);
            var buffer = new ArraySegment<byte>(buffer1);

            try
            {
                await client.SendAsync(buffer, SocketFlags.None);
            }
#else
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(message.Length * 2);
            Memory<byte> buffer = memoryOwner.Memory[..(message.Length * 2)];
            int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);
            buffer = buffer[..bytes];

            try
            {
                await client.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
#endif
            catch (Exception ex)
            {
                PreStartup.LogException(ex, "Sending message to game host failed!");
            }
        }

        public override string GetSwitchName()
            => "Load Game".L10N("Client:Main:LoadGameSwitchName");

        public override void Update(GameTime gameTime)
        {
            if (IsHost)
            {
                for (int i = 1; i < Players.Count; i++)
                {
                    LANPlayerInfo lpInfo = (LANPlayerInfo)Players[i];
                    if (!Task.Run(() => lpInfo.UpdateAsync(gameTime)).Result)
                    {
                        CleanUpPlayer(lpInfo);
                        Players.RemoveAt(i);
                        AddNotice(string.Format("{0} - connection timed out".L10N("Client:Main:PlayerTimeout"), lpInfo.Name));
                        CopyPlayerDataToUI();
                        Task.Run(BroadcastOptionsAsync).Wait();
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
                    Task.Run(LeaveGameAsync).Wait();
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
            sb.Append((string)lblMapNameValue.Tag);
            sb.Append((string)lblGameModeValue.Tag);
            sb.Append(0); // LoadedGameID
            var sbPlayers = new StringBuilder();
            SGPlayers.ForEach(p => sbPlayers.Append(p.Name + ","));
            sbPlayers.Remove(sbPlayers.Length - 1, 1);
            sb.Append(sbPlayers.ToString());
            sb.Append(Convert.ToInt32(started || Players.Count == SGPlayers.Count));
            sb.Append(1); // IsLoadedGame

            GameBroadcast?.Invoke(this, new GameBroadcastEventArgs(sb.ToString()));
        }

        protected override async Task HandleGameProcessExitedAsync()
        {
            try
            {
                await base.HandleGameProcessExitedAsync();

                await LeaveGameAsync();
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        protected override void UpdateDiscordPresence(bool resetTimer = false)
        {
            if (discordHandler == null)
                return;

            PlayerInfo player = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
            if (player == null)
                return;
            string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby"; // not UI strings

            discordHandler.UpdatePresence(
                (string)lblMapNameValue.Tag, (string)lblGameModeValue.Tag, currentState, "LAN",
                Players.Count, SGPlayers.Count,
                "LAN Game", IsHost, resetTimer);
        }
    }
}