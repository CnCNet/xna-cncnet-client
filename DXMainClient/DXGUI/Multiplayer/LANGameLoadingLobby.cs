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
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClientCore.Extensions;

namespace DTAClient.DXGUI.Multiplayer
{
    internal sealed class LANGameLoadingLobby : GameLoadingLobbyBase
    {
        private const double DROPOUT_TIMEOUT = 20.0;
        private const double GAME_BROADCAST_INTERVAL = 10.0;

        public LANGameLoadingLobby(
            WindowManager windowManager,
            LANColor[] chatColors,
            MapLoader mapLoader,
            DiscordHandler discordHandler)
            : base(windowManager, discordHandler)
        {
            encoding = ProgramConstants.LAN_ENCODING;
            this.chatColors = chatColors;
            this.mapLoader = mapLoader;

            localGame = ClientConfiguration.Instance.LocalGame;

            hostCommandHandlers = new LANServerCommandHandler[]
            {
                new ServerStringCommandHandler(LANCommands.CHAT_GAME_LOADING_COMMAND, (sender, data) => Server_HandleChatMessageAsync(sender, data).HandleTask()),
                new ServerStringCommandHandler(LANCommands.FILE_HASH, Server_HandleFileHashMessage),
                new ServerNoParamCommandHandler(LANCommands.READY_STATUS, sender => Server_HandleReadyRequestAsync(sender).HandleTask())
            };

            playerCommandHandlers = new LANClientCommandHandler[]
            {
                new ClientStringCommandHandler(LANCommands.CHAT_GAME_LOADING_COMMAND, Client_HandleChatMessage),
                new ClientStringCommandHandler(LANCommands.OPTIONS, Client_HandleOptionsMessage),
                new ClientNoParamCommandHandler(LANCommands.GAME_START, () => Client_HandleStartCommandAsync().HandleTask())
            };

            WindowManager.GameClosing += (_, _) => WindowManager_GameClosingAsync().HandleTask();
        }

        private async ValueTask WindowManager_GameClosingAsync()
        {
            if (client is { Connected: true })
                await ClearAsync().ConfigureAwait(false);
        }

        public event EventHandler<GameBroadcastEventArgs> GameBroadcast;

        private Socket listener;
        private Socket client;

        private readonly LANColor[] chatColors;
        private readonly MapLoader mapLoader;
        private const int chatColorIndex = 0;
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

        public async ValueTask SetUpAsync(bool isHost, Socket client, int loadedGameId)
        {
            Refresh(isHost);

            this.loadedGameId = loadedGameId;

            started = false;

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            if (isHost)
            {
                ListenForClientsAsync(cancellationTokenSource.Token).HandleTask();

                this.client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await this.client.ConnectAsync(IPAddress.Loopback, ProgramConstants.LAN_GAME_LOBBY_PORT).ConfigureAwait(false);

                string message = LANCommands.PLAYER_JOIN +
                     ProgramConstants.LAN_DATA_SEPARATOR + ProgramConstants.PLAYERNAME +
                     ProgramConstants.LAN_DATA_SEPARATOR + loadedGameId;

                const int charSize = sizeof(char);
                int bufferSize = message.Length * charSize;
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
                Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
                int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);

                buffer = buffer[..bytes];

                await this.client.SendAsync(buffer, SocketFlags.None, CancellationToken.None).ConfigureAwait(false);

                var fhc = new FileHashCalculator();
                fhc.CalculateHashes(gameModes);
                localFileHash = fhc.GetCompleteHash();
            }
            else
            {
                this.client?.Dispose();
                this.client = client;
            }

            HandleServerCommunicationAsync(cancellationTokenSource.Token).HandleTask();

            if (IsHost)
                CopyPlayerDataToUI();

            WindowManager.SelectedControl = tbChatInput;
        }

        public async ValueTask PostJoinAsync()
        {
            var fhc = new FileHashCalculator();
            fhc.CalculateHashes(gameModes);
            await SendMessageToHostAsync(LANCommands.FILE_HASH + " " + fhc.GetCompleteHash(), cancellationTokenSource?.Token ?? default).ConfigureAwait(false);
            UpdateDiscordPresence(true);
        }

        #region Server code

        private async ValueTask ListenForClientsAsync(CancellationToken cancellationToken)
        {
            listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.IPv6Any, ProgramConstants.LAN_GAME_LOBBY_PORT));
            listener.Listen();

            while (!cancellationToken.IsCancellationRequested)
            {
                Socket newPlayerSocket;

                try
                {
                    newPlayerSocket = await listener.AcceptAsync(cancellationToken).ConfigureAwait(false);
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

                Logger.Log("New client connected from " + ((IPEndPoint)newPlayerSocket.RemoteEndPoint).Address);

                LANPlayerInfo lpInfo = new LANPlayerInfo(encoding);
                lpInfo.SetClient(newPlayerSocket);

                HandleClientConnectionAsync(lpInfo, cancellationToken).HandleTask();
            }
        }

        private async ValueTask HandleClientConnectionAsync(LANPlayerInfo lpInfo, CancellationToken cancellationToken)
        {
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;
                Memory<byte> message;

                try
                {
                    message = memoryOwner.Memory[..4096];
                    bytesRead = await client.ReceiveAsync(message, SocketFlags.None, cancellationToken).ConfigureAwait(false);
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

                if (parts.Length != 3)
                    break;

                string name = parts[1].Trim();
                int loadedGameId = Conversions.IntFromString(parts[2], -1);

                if (parts[0] == LANCommands.PLAYER_JOIN && !string.IsNullOrEmpty(name)
                    && loadedGameId == this.loadedGameId)
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
                Players.Count >= SGPlayers.Count ||
                SGPlayers.Find(p => p.Name == lpInfo.Name) == null)
            {
                lpInfo.TcpClient.Shutdown(SocketShutdown.Both);
                lpInfo.TcpClient.Close();
                return;
            }

            if (Players.Count == 0)
                lpInfo.Ready = true;

            Players.Add(lpInfo);

            lpInfo.MessageReceived += LpInfo_MessageReceived;
            lpInfo.ConnectionLost += (sender, _) => LpInfo_ConnectionLostAsync(sender).HandleTask();

            sndJoinSound.Play();

            AddNotice(string.Format("{0} connected from {1}".L10N("Client:Main:PlayerFromIP"), lpInfo.Name, lpInfo.IPAddress));
            lpInfo.StartReceiveLoopAsync(cancellationToken).HandleTask();

            CopyPlayerDataToUI();
            await BroadcastOptionsAsync().ConfigureAwait(false);
            UpdateDiscordPresence();
        }

        private async ValueTask LpInfo_ConnectionLostAsync(object sender)
        {
            var lpInfo = (LANPlayerInfo)sender;
            CleanUpPlayer(lpInfo);
            Players.Remove(lpInfo);

            AddNotice(string.Format("{0} has left the game.".L10N("Client:Main:PlayerLeftGame"), lpInfo.Name));

            sndLeaveSound.Play();

            CopyPlayerDataToUI();
            await BroadcastOptionsAsync().ConfigureAwait(false);
            UpdateDiscordPresence();
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

            if (lpInfo.TcpClient.Connected)
                lpInfo.TcpClient.Shutdown(SocketShutdown.Both);

            lpInfo.TcpClient.Close();
        }

        #endregion

        private async ValueTask HandleServerCommunicationAsync(CancellationToken cancellationToken)
        {
            if (!client.Connected)
                return;

            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;
                Memory<byte> message;

                try
                {
                    message = memoryOwner.Memory[..4096];
                    bytesRead = await client.ReceiveAsync(message, SocketFlags.None, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex, "Reading data from the server failed!");
                    await LeaveGameAsync().ConfigureAwait(false);
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
                await LeaveGameAsync().ConfigureAwait(false);
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

        protected override async ValueTask LeaveGameAsync()
        {
            await ClearAsync().ConfigureAwait(false);
            Disable();
            await base.LeaveGameAsync().ConfigureAwait(false);
        }

        private async ValueTask ClearAsync()
        {
            if (IsHost)
            {
                await BroadcastMessageAsync(LANCommands.PLAYER_QUIT_COMMAND, CancellationToken.None).ConfigureAwait(false);
                Players.ForEach(p => CleanUpPlayer((LANPlayerInfo)p));
                Players.Clear();
                listener.Close();
            }
            else
            {
                await SendMessageToHostAsync(LANCommands.PLAYER_QUIT_COMMAND, CancellationToken.None).ConfigureAwait(false);
            }

            cancellationTokenSource.Cancel();
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        protected override void AddNotice(string message, Color color)
        {
            lbChatMessages.AddMessage(null, message, color);
        }

        protected override async ValueTask BroadcastOptionsAsync()
        {
            if (Players.Count > 0)
                Players[0].Ready = true;

            var sb = new ExtendedStringBuilder(LANCommands.OPTIONS + " ", true);
            sb.Separator = ProgramConstants.LAN_DATA_SEPARATOR;

            sb.Append(ddSavedGame.SelectedIndex);

            foreach (PlayerInfo pInfo in Players)
            {
                sb.Append(pInfo.Name);
                sb.Append(Convert.ToInt32(pInfo.Ready));
                sb.Append(pInfo.IPAddress);
            }

            await BroadcastMessageAsync(sb.ToString(), cancellationTokenSource?.Token ?? default).ConfigureAwait(false);
        }

        protected override ValueTask HostStartGameAsync()
            => BroadcastMessageAsync(LANCommands.GAME_START, cancellationTokenSource?.Token ?? default);

        protected override ValueTask RequestReadyStatusAsync()
            => SendMessageToHostAsync(LANCommands.READY_STATUS, cancellationTokenSource?.Token ?? default);

        protected override async ValueTask SendChatMessageAsync(string message)
        {
            await SendMessageToHostAsync(LANCommands.CHAT_GAME_LOADING_COMMAND + " " + chatColorIndex +
                ProgramConstants.LAN_DATA_SEPARATOR + message, cancellationTokenSource?.Token ?? default).ConfigureAwait(false);

            sndMessageSound.Play();
        }

        #region Server's command handlers

        private async ValueTask Server_HandleChatMessageAsync(LANPlayerInfo sender, string data)
        {
            string[] parts = data.Split(ProgramConstants.LAN_DATA_SEPARATOR);

            if (parts.Length < 2)
                return;

            int colorIndex = Conversions.IntFromString(parts[0], -1);

            if (colorIndex < 0 || colorIndex >= chatColors.Length)
                return;

            await BroadcastMessageAsync(LANCommands.CHAT_GAME_LOADING_COMMAND + " " + sender +
                ProgramConstants.LAN_DATA_SEPARATOR + colorIndex +
                ProgramConstants.LAN_DATA_SEPARATOR + data, cancellationTokenSource?.Token ?? default).ConfigureAwait(false);
        }

        private void Server_HandleFileHashMessage(LANPlayerInfo sender, string hash)
        {
            if (hash != localFileHash)
                AddNotice(string.Format("{0} - modified files detected! They could be cheating!".L10N("Client:Main:PlayerCheating"), sender.Name), Color.Red);
            sender.Verified = true;
        }

        private async ValueTask Server_HandleReadyRequestAsync(LANPlayerInfo sender)
        {
            if (sender.Ready)
                return;

            sender.Ready = true;
            CopyPlayerDataToUI();
            await BroadcastOptionsAsync().ConfigureAwait(false);
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

            if ((pCount * PLAYER_INFO_PARTS) + 1 != parts.Length)
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
                int baseIndex = 1 + (i * PLAYER_INFO_PARTS);

                Players.Add(new LANPlayerInfo(encoding)
                {
                    Name = parts[baseIndex],
                    Ready = Conversions.IntFromString(parts[baseIndex + 1], -1) > 0,
                    IPAddress = IPAddress.Parse(parts[baseIndex + 2])
                });
            }

            if (Players.Count > 0) // Set IP of host
                Players[0].IPAddress = ((IPEndPoint)client.RemoteEndPoint).Address;

            CopyPlayerDataToUI();
        }

        private ValueTask Client_HandleStartCommandAsync()
        {
            started = true;

            return LoadGameAsync();
        }

        #endregion

        /// <summary>
        /// Broadcasts a command to all players in the game as the game host.
        /// </summary>
        /// <param name="message">The command to send.</param>
        private async ValueTask BroadcastMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (!IsHost)
                return;

            foreach (PlayerInfo pInfo in Players)
            {
                var lpInfo = (LANPlayerInfo)pInfo;
                await lpInfo.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask SendMessageToHostAsync(string message, CancellationToken cancellationToken)
        {
            if (!client.Connected)
                return;

            message += ProgramConstants.LAN_MESSAGE_SEPARATOR;

            const int charSize = sizeof(char);
            int bufferSize = message.Length * charSize;
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
            Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
            int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);

            buffer = buffer[..bytes];

            try
            {
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

        public override string GetSwitchName()
            => "Load Game".L10N("Client:Main:LoadGameSwitchName");

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
                        Task.Run(() => BroadcastOptionsAsync().HandleTask()).Wait();
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
                    Task.Run(() => LeaveGameAsync().HandleTask()).Wait();
            }

            base.Update(gameTime);
        }

        private void BroadcastGame()
        {
            var sb = new ExtendedStringBuilder(LANCommands.GAME + " ", true);
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

        protected override async ValueTask HandleGameProcessExitedAsync()
        {
            await base.HandleGameProcessExitedAsync().ConfigureAwait(false);
            await LeaveGameAsync().ConfigureAwait(false);
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