using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A game lobby for loading saved CnCNet games.
    /// </summary>
    public class CnCNetGameLoadingLobby : GameLoadingLobbyBase, IV3NegotiationHost
    {
        private const double GAME_BROADCAST_INTERVAL = 20.0;
        private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;

        private const string NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND = "NPRSNT";
        private const string GET_READY_CTCP_COMMAND = "GTRDY";
        private const string FILE_HASH_CTCP_COMMAND = "FHSH";
        private const string INVALID_FILE_HASH_CTCP_COMMAND = "IHSH";
        private const string TUNNEL_PING_CTCP_COMMAND = "TNLPNG";
        private const string OPTIONS_CTCP_COMMAND = "OP";
        private const string INVALID_SAVED_GAME_INDEX_CTCP_COMMAND = "ISGI";
        private const string START_GAME_CTCP_COMMAND = "START";
        private const string START_GAME_V3_CTCP_COMMAND = "STARTV3";
        private const string PLAYER_READY_CTCP_COMMAND = "READY";
        public CnCNetGameLoadingLobby(
            WindowManager windowManager,
            TopBar topBar,
            CnCNetManager connectionManager,
            TunnelHandler tunnelHandler,
            MapLoader mapLoader,
            GameCollection gameCollection,
            DiscordHandler discordHandler,
            CnCNetUserData cncnetUserData
        ) : base(windowManager, discordHandler)
        {
            this.connectionManager = connectionManager;
            this.tunnelHandler = tunnelHandler;
            this.topBar = topBar;
            this.gameCollection = gameCollection;
            this.mapLoader = mapLoader;
            this.cncnetUserData = cncnetUserData;

            _negotiator = new V3TunnelNegotiationManager(this, tunnelHandler, windowManager);

            ctcpCommandHandlers = new CommandHandlerBase[]
            {
                new NoParamCommandHandler(NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND, HandleNotAllPresentNotification),
                new NoParamCommandHandler(GET_READY_CTCP_COMMAND, HandleGetReadyNotification),
                new StringCommandHandler(FILE_HASH_CTCP_COMMAND, HandleFileHashCommand),
                new StringCommandHandler(INVALID_FILE_HASH_CTCP_COMMAND, HandleCheaterNotification),
                new IntCommandHandler(TUNNEL_PING_CTCP_COMMAND, HandleTunnelPing),
                new StringCommandHandler(OPTIONS_CTCP_COMMAND, HandleOptionsMessage),
                new NoParamCommandHandler(INVALID_SAVED_GAME_INDEX_CTCP_COMMAND, HandleInvalidSaveIndexCommand),
                new StringCommandHandler(START_GAME_V3_CTCP_COMMAND, HandleStartGameV3Command),
                new StringCommandHandler(START_GAME_CTCP_COMMAND, HandleStartGameCommand),
                new IntCommandHandler(PLAYER_READY_CTCP_COMMAND, HandlePlayerReadyRequest),
                new StringCommandHandler(TunnelNegotiationCommands.ChangeTunnelServer, HandleTunnelServerChangeMessage),
                new StringCommandHandler(TunnelNegotiationCommands.NegotiationReport, HandleNegotiationReportMessage),
                new StringCommandHandler(TunnelNegotiationCommands.TunnelRenegotiate, HandleTunnelRenegotiateMessage),
                new StringCommandHandler(TunnelNegotiationCommands.TunnelFailed, HandleTunnelFailedMessage),
            };
        }

        private CommandHandlerBase[] ctcpCommandHandlers;

        private CnCNetManager connectionManager;

        private CnCNetUserData cncnetUserData;

        private List<GameMode> gameModes;

        private TunnelHandler tunnelHandler;
        private readonly MapLoader mapLoader;
        private TunnelSelectionWindow tunnelSelectionWindow;
        private XNAClientButton btnChangeTunnel;

        private Channel channel;

        private GameCollection gameCollection;

        private IRCColor chatColor;

        private string hostName;

        private string localGame;

        private string gameFilesHash;

        private XNATimerControl gameBroadcastTimer;

        private bool started;

        private DarkeningPanel dp;

        private TopBar topBar;

        private readonly V3TunnelNegotiationManager _negotiator;
        private TunnelMode _tunnelMode;

        public override void Initialize()
        {
            dp = new DarkeningPanel(WindowManager);

            localGame = ClientConfiguration.Instance.LocalGame;

            base.Initialize();

            connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
            connectionManager.Disconnected += ConnectionManager_Disconnected;

            tunnelSelectionWindow = new TunnelSelectionWindow(WindowManager, tunnelHandler);
            tunnelSelectionWindow.Initialize();
            tunnelSelectionWindow.DrawOrder = 1;
            tunnelSelectionWindow.UpdateOrder = 1;
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, tunnelSelectionWindow);
            tunnelSelectionWindow.CenterOnParent();
            tunnelSelectionWindow.Disable();
            tunnelSelectionWindow.TunnelSelected += TunnelSelectionWindow_TunnelSelected;

            btnChangeTunnel = new XNAClientButton(WindowManager);
            btnChangeTunnel.Name = nameof(btnChangeTunnel);
            btnChangeTunnel.ClientRectangle = new Rectangle(btnLeaveGame.Right - btnLeaveGame.Width - 145,
                btnLeaveGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnChangeTunnel.Text = "Change Tunnel".L10N("Client:Main:ChangeTunnel");
            btnChangeTunnel.LeftClick += BtnChangeTunnel_LeftClick;
            AddChild(btnChangeTunnel);

            gameBroadcastTimer = new XNATimerControl(WindowManager);
            gameBroadcastTimer.AutoReset = true;
            gameBroadcastTimer.Interval = TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL);
            gameBroadcastTimer.Enabled = false;
            gameBroadcastTimer.TimeElapsed += GameBroadcastTimer_TimeElapsed;

            WindowManager.AddAndInitializeControl(gameBroadcastTimer);
        }

        public override void Refresh(bool isHost)
        {
            base.Refresh(isHost);

            btnChangeTunnel.Visible = isHost;
            gameBroadcastTimer.Enabled = isHost;
        }

        private void BtnChangeTunnel_LeftClick(object sender, EventArgs e) => ShowTunnelSelectionWindow("Select tunnel server:".L10N("Client:Main:SelectTunnelServer"));

        private void GameBroadcastTimer_TimeElapsed(object sender, EventArgs e) => BroadcastGame();

        private void ConnectionManager_Disconnected(object sender, EventArgs e) => Clear();

        private void ConnectionManager_ConnectionLost(object sender, ConnectionLostEventArgs e) => Clear();

        /// <summary>
        /// Sets up events and information before joining the channel.
        /// </summary>
        public void SetUp(bool isHost, CnCNetTunnel tunnel, Channel channel,
            string hostName)
        {
            this.channel = channel;
            this.hostName = hostName;

            channel.MessageAdded += Channel_MessageAdded;
            channel.UserAdded += Channel_UserAdded;
            channel.UserLeft += Channel_UserLeft;
            channel.UserQuitIRC += Channel_UserQuitIRC;
            channel.CTCPReceived += Channel_CTCPReceived;

            _tunnelMode = TunnelModeExtensions.FromTunnel(tunnel);

            tunnelHandler.CurrentTunnel = _tunnelMode == TunnelMode.V3Dynamic ? null : tunnel;
            tunnelHandler.CurrentTunnelPinged += TunnelHandler_CurrentTunnelPinged;
            tunnelHandler.TunnelFailed += TunnelHandler_TunnelFailed;

            started = false;

            _negotiator.RegenerateV3PlayerInfos();
            Refresh(isHost);
        }

        private void TunnelHandler_CurrentTunnelPinged(object sender, EventArgs e)
        {
            // TODO Rampastring pls, review and merge that XNAIndicator PR already
        }

        /// <summary>
        /// Clears event subscriptions and leaves the channel.
        /// </summary>
        public void Clear()
        {
            gameBroadcastTimer.Enabled = false;

            _negotiator.ClearAll();

            if (channel != null)
            {
                // TODO leave channel only if we've joined the channel
                channel.Leave();

                channel.MessageAdded -= Channel_MessageAdded;
                channel.UserAdded -= Channel_UserAdded;
                channel.UserLeft -= Channel_UserLeft;
                channel.UserQuitIRC -= Channel_UserQuitIRC;
                channel.CTCPReceived -= Channel_CTCPReceived;

                connectionManager.RemoveChannel(channel);
            }

            if (Enabled)
            {
                Enabled = false;
                Visible = false;

                base.LeaveGame();
            }

            tunnelHandler.CurrentTunnel = null;
            tunnelHandler.CurrentTunnelPinged -= TunnelHandler_CurrentTunnelPinged;
            tunnelHandler.TunnelFailed -= TunnelHandler_TunnelFailed;

            topBar.RemovePrimarySwitchable(this);
        }

        private void Channel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
        {
            foreach (CommandHandlerBase cmdHandler in ctcpCommandHandlers)
            {
                if (cmdHandler.Handle(e.UserName, e.Message))
                    return;
            }

            Logger.Log("Unhandled CTCP command: " + e.Message + " from " + e.UserName);
        }

        /// <summary>
        /// Called when the local user has joined the game channel.
        /// </summary>
        public void OnJoined()
        {
            FileHashCalculator fhc = new FileHashCalculator();
            fhc.CalculateHashes();

            if (IsHost)
            {
                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("MODE {0} +klnNs {1} {2}", channel.ChannelName,
                    channel.Password, SGPlayers.Count),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));

                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("TOPIC {0} :{1}", channel.ChannelName,
                    ProgramConstants.CNCNET_PROTOCOL_REVISION + ";" + localGame.ToLower()),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));

                gameFilesHash = fhc.GetCompleteHash();

                gameBroadcastTimer.Enabled = true;
                gameBroadcastTimer.Start();
                gameBroadcastTimer.SetTime(TimeSpan.FromSeconds(INITIAL_GAME_BROADCAST_DELAY));
            }
            else
            {
                channel.SendCTCPMessage(FILE_HASH_CTCP_COMMAND + " " + fhc.GetCompleteHash(), QueuedMessageType.SYSTEM_MESSAGE, 10);

                if (tunnelHandler.CurrentTunnel != null)
                {
                    channel.SendCTCPMessage(TUNNEL_PING_CTCP_COMMAND + " " + tunnelHandler.CurrentTunnel.Ping.Milliseconds, QueuedMessageType.SYSTEM_MESSAGE, 10);

                    if (tunnelHandler.CurrentTunnel.Ping.IsUnknown())
                        AddNotice(string.Format("{0} - unknown ping to tunnel server.".L10N("Client:Main:PlayerUnknownPing"), ProgramConstants.PLAYERNAME));
                    else
                        AddNotice(string.Format("{0} - ping to tunnel server: {1} ms".L10N("Client:Main:PlayerPing"), ProgramConstants.PLAYERNAME, tunnelHandler.CurrentTunnel.Ping.Milliseconds));
                }
            }

            topBar.AddPrimarySwitchable(this);
            topBar.SwitchToPrimary();
            WindowManager.SelectedControl = tbChatInput;
            UpdateDiscordPresence(true);
        }

        private void Channel_UserAdded(object sender, ChannelUserEventArgs e)
        {
            PlayerInfo pInfo = new PlayerInfo();
            pInfo.Name = e.User.IRCUser.Name;

            Players.Add(pInfo);

            sndJoinSound.Play();

            _negotiator.RegenerateV3PlayerInfos();

            BroadcastOptions();
            CopyPlayerDataToUI();
            UpdateDiscordPresence();

            _negotiator.StartNegotiationForPlayerName(pInfo.Name);
        }

        private void Channel_UserLeft(object sender, UserNameEventArgs e)
        {
            RemovePlayer(e.UserName);
            UpdateDiscordPresence();
        }

        private void Channel_UserQuitIRC(object sender, UserNameEventArgs e)
        {
            RemovePlayer(e.UserName);
            UpdateDiscordPresence();
        }

        private void RemovePlayer(string playerName)
        {
            int index = Players.FindIndex(p => p.Name == playerName);

            if (index == -1)
                return;

            sndLeaveSound.Play();

            _negotiator.RemovePlayer(playerName);

            Players.RemoveAt(index);

            CopyPlayerDataToUI();
            UpdateLoadGameButtonStatus();

            if (!IsHost && playerName == hostName && !ProgramConstants.IsInGame)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    Color.Yellow, "The game host left the game!".L10N("Client:Main:HostLeft")));

                Clear();
            }
        }

        private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message.SenderIdent) &&
                cncnetUserData.IsIgnored(e.Message.SenderIdent) &&
                !e.Message.SenderIsAdmin)
            {
                lbChatMessages.AddMessage(new ChatMessage(Color.Silver, string.Format("Message blocked from - {0}".L10N("Client:Main:PMBlockedFrom"), e.Message.SenderName)));
            }
            else
            {
                lbChatMessages.AddMessage(e.Message);
                sndMessageSound.Play();
            }
        }

        protected override void AddNotice(string message, Color color) => channel.AddMessage(new ChatMessage(color, message));

        protected override void BroadcastOptions()
        {
            if (!IsHost)
                return;

            Players[0].Ready = true;

            StringBuilder message = new StringBuilder(OPTIONS_CTCP_COMMAND + " ");
            message.Append(ddSavedGame.SelectedIndex);
            message.Append(";");
            message.Append((int)_tunnelMode);
            message.Append(";");
            foreach (PlayerInfo pInfo in Players)
            {
                message.Append(pInfo.Name);
                message.Append(":");
                message.Append(Convert.ToInt32(pInfo.Ready));
                message.Append(";");
            }
            message.Remove(message.Length - 1, 1);

            channel.SendCTCPMessage(message.ToString(), QueuedMessageType.GAME_SETTINGS_MESSAGE, 10);
        }

        protected override void SendChatMessage(string message)
        {
            sndMessageSound.Play();

            channel.SendChatMessage(message, chatColor);
        }

        protected override void RequestReadyStatus() =>
            channel.SendCTCPMessage(PLAYER_READY_CTCP_COMMAND + " 1", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 10);

        protected override void GetReadyNotification()
        {
            base.GetReadyNotification();

            topBar.SwitchToPrimary();

            if (IsHost)
                channel.SendCTCPMessage(GET_READY_CTCP_COMMAND, QueuedMessageType.GAME_GET_READY_MESSAGE, 0);
        }

        protected override void NotAllPresentNotification()
        {
            base.NotAllPresentNotification();

            if (IsHost)
            {
                channel.SendCTCPMessage(NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND,
                    QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
            }
        }

        private void ShowTunnelSelectionWindow(string description)
        {
            tunnelSelectionWindow.Open(description,
                tunnelHandler.CurrentTunnel,
                _tunnelMode);
        }

        private void TunnelSelectionWindow_TunnelSelected(object sender, TunnelSelectedEventArgs e)
        {
            HandleTunnelModeChange(e.Mode, true);

            if (e.Mode != TunnelMode.V3Dynamic && e.Tunnel != null)
            {
                channel.SendCTCPMessage($"{TunnelNegotiationCommands.ChangeTunnelServer} {e.Tunnel.Address}:{e.Tunnel.Port}",
                    QueuedMessageType.SYSTEM_MESSAGE, 10);
                AddNotice(string.Format("Changed the tunnel server to: {0}".L10N("Client:Main:YouChangedTunnel"), e.Tunnel.Name));
                HandleTunnelServerChange(e.Tunnel);
            }

            BroadcastOptions();
        }

        #region CTCP Handlers

        private void HandleGetReadyNotification(string sender)
        {
            if (sender != hostName)
                return;

            GetReadyNotification();
        }

        private void HandleNotAllPresentNotification(string sender)
        {
            if (sender != hostName)
                return;

            NotAllPresentNotification();
        }

        private void HandleFileHashCommand(string sender, string fileHash)
        {
            if (!IsHost)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);
            if (pInfo == null)
                return;

            pInfo.HashReceived = true;

            if (fileHash != gameFilesHash)
                HandleCheaterNotification(hostName, sender); // This is kinda hacky
        }

        private void HandleCheaterNotification(string sender, string cheaterName)
        {
            if (sender != hostName)
                return;

            AddNotice(string.Format("{0} - modified files detected! They could be cheating!".L10N("Client:Main:PlayerCheating"), cheaterName), Color.Red);

            if (IsHost)
                channel.SendCTCPMessage(INVALID_FILE_HASH_CTCP_COMMAND + " " + cheaterName, QueuedMessageType.SYSTEM_MESSAGE, 0);
        }

        private void HandleTunnelPing(string sender, int pingInMs)
        {
            if (pingInMs < 0)
                AddNotice(string.Format("{0} - unknown ping to tunnel server.".L10N("Client:Main:PlayerUnknownPing"), sender));
            else
                AddNotice(string.Format("{0} - ping to tunnel server: {1} ms".L10N("Client:Main:PlayerPing"), sender, pingInMs));
        }

        /// <summary>
        /// Handles an options broadcast sent by the game host.
        /// </summary>
        private void HandleOptionsMessage(string sender, string data)
        {
            if (sender != hostName)
                return;

            string[] parts = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 1)
                return;

            int sgIndex = Conversions.IntFromString(parts[0], -1);

            if (sgIndex < 0)
                return;

            if (sgIndex >= ddSavedGame.Items.Count)
            {
                AddNotice("The game host has selected an invalid saved game index!".L10N("Client:Main:HostInvalidIndex") + " " + sgIndex);
                channel.SendCTCPMessage(INVALID_SAVED_GAME_INDEX_CTCP_COMMAND, QueuedMessageType.SYSTEM_MESSAGE, 10);
                return;
            }

            ddSavedGame.SelectedIndex = sgIndex;

            int tunnelModeFromOp = -1;
            int playerStartIndex = 1;
            if (parts.Length >= 2 && !parts[1].Contains(':') && int.TryParse(parts[1], out int parsedMode))
            {
                tunnelModeFromOp = parsedMode;
                playerStartIndex = 2;
            }

            Players.Clear();

            for (int i = playerStartIndex; i < parts.Length; i++)
            {
                string[] playerAndReadyStatus = parts[i].Split(':');
                if (playerAndReadyStatus.Length < 2)
                    return;

                string playerName = playerAndReadyStatus[0];
                int readyStatus = Conversions.IntFromString(playerAndReadyStatus[1], -1);

                if (string.IsNullOrEmpty(playerName) || readyStatus == -1)
                    return;

                PlayerInfo pInfo = new PlayerInfo();
                pInfo.Name = playerName;
                pInfo.Ready = Convert.ToBoolean(readyStatus);

                Players.Add(pInfo);
            }

            CopyPlayerDataToUI();

            _negotiator.RegenerateV3PlayerInfos();

            if (tunnelModeFromOp >= 0 && !IsHost)
                HandleTunnelModeChange((TunnelMode)tunnelModeFromOp, false);

            _negotiator.StartPendingNegotiations();
        }

        private void HandleInvalidSaveIndexCommand(string sender)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            pInfo.Ready = false;

            AddNotice(string.Format("{0} does not have the selected saved game on their system! Try selecting an earlier saved game.".L10N("Client:Main:PlayerDontHaveSavedGame"), pInfo.Name));

            CopyPlayerDataToUI();
        }

        private void HandleStartGameCommand(string sender, string data)
        {
            if (sender != hostName)
                return;

            string[] parts = data.Split(';');

            int playerCount = parts.Length / 2;

            for (int i = 0; i < playerCount; i++)
            {
                if (parts.Length < i * 2 + 1)
                    return;

                string pName = parts[i * 2];
                string ipAndPort = parts[i * 2 + 1];
                string[] ipAndPortSplit = ipAndPort.Split(':');

                if (ipAndPortSplit.Length < 2)
                    return;

                int port = 0;
                bool success = int.TryParse(ipAndPortSplit[1], out port);
                if (!success)
                    return;

                PlayerInfo pInfo = Players.Find(p => p.Name == pName);

                if (pInfo == null)
                    continue;

                pInfo.Port = port;
            }

            LoadGame();
        }

        private void HandleStartGameV3Command(string sender, string data)
        {
            if (sender != hostName)
                return;

            string[] parts = data.Split(';');

            if (parts.Length != Players.Count * 3)
            {
                Logger.Log($"HandleStartGameV3Command: Invalid start message: expected {Players.Count * 3} parts for {Players.Count} players, got {parts.Length}.");
                NotifyStartFailed();
                return;
            }

            for (int i = 0; i < Players.Count; i++)
            {
                if (!_negotiator.ApplyV3StartEntry(parts, i * 3, i))
                {
                    Logger.Log($"HandleStartGameV3Command: Could not apply start entry for player at position {i}.");
                    NotifyStartFailed();
                    return;
                }
            }

            StartV3Game();
        }

        /// <summary>
        /// Tells the player their client could not act on the host's game start message —
        /// everyone else launches, so silence here would leave them stranded in the lobby
        /// with no explanation.
        /// </summary>
        private void NotifyStartFailed()
        {
            AddNotice(("Failed to process the game start message from the host. The game was started " +
                "without you; the host's player list may be out of sync with yours. Try rejoining the game.").L10N("Client:Main:StartMessageInvalid"),
                Color.Yellow);
        }

        private void HandlePlayerReadyRequest(string sender, int readyStatus)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            pInfo.Ready = Convert.ToBoolean(readyStatus);

            CopyPlayerDataToUI();

            if (IsHost)
                BroadcastOptions();
        }

        private void HandleTunnelServerChangeMessage(string sender, string tunnelAddressAndPort)
        {
            if (sender != hostName)
                return;

            string[] split = tunnelAddressAndPort.Split(':');
            if (split.Length < 2 || !int.TryParse(split[1], out int tunnelPort))
                return;

            string tunnelAddress = split[0];

            CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort);
            if (tunnel == null)
            {
                AddNotice(("The game host has selected an invalid tunnel server! " +
                    "The game host needs to change the server or you will be unable " +
                    "to participate in the match.").L10N("Client:Main:HostInvalidTunnel"),
                    Color.Yellow);
                btnLoadGame.AllowClick = false;
                return;
            }

            AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("Client:Main:HostChangeTunnel"), tunnel.Name));
            HandleTunnelServerChange(tunnel);
            btnLoadGame.AllowClick = true;
        }

        /// <summary>
        /// Changes the tunnel server used for the game.
        /// </summary>
        private void HandleTunnelServerChange(CnCNetTunnel tunnel)
        {
            tunnelHandler.CurrentTunnel = tunnel;

            _negotiator.ApplyStaticTunnel(tunnel);
        }

        private void HandleNegotiationReportMessage(string sender, string data)
            => _negotiator.HandleNegotiationReportMessage(sender, data);

        private void HandleTunnelRenegotiateMessage(string sender, string tunnelAddressAndPort)
            => _negotiator.HandleRemoteTunnelRenegotiate(sender, tunnelAddressAndPort);

        private void HandleTunnelFailedMessage(string sender, string tunnelName)
            => _negotiator.HandleRemoteTunnelFailed(sender, tunnelName);

        #endregion

        protected override void HostStartGame()
        {
            if (_tunnelMode == TunnelMode.V3Dynamic && !_negotiator.AreAllNegotiationsSuccessful())
            {
                AddNotice("Cannot start game: tunnel negotiations have not completed.".L10N("Client:Main:CannotStartNegotiationsIncomplete"), Color.Yellow);
                return;
            }

            if (_tunnelMode == TunnelMode.V2Legacy || tunnelHandler.CurrentTunnel?.Version == 2)
            {
                if (tunnelHandler.CurrentTunnel == null)
                {
                    ShowTunnelSelectionWindow(("No tunnel server is selected. Please pick one:").L10N("Client:Main:NoTunnelSelected"));
                    return;
                }

                AddNotice("Contacting tunnel server...".L10N("Client:Main:ConnectingTunnel"));
                List<int> playerPorts = tunnelHandler.CurrentTunnel.GetPlayerPortInfo(SGPlayers.Count);

                if (playerPorts.Count < Players.Count)
                {
                    ShowTunnelSelectionWindow(("An error occured while contacting the CnCNet tunnel server.\nTry picking a different tunnel server:").L10N("Client:Main:ConnectTunnelError1"));
                    AddNotice(("An error occured while contacting the specified CnCNet " +
                        "tunnel server. Please try using a different tunnel server").L10N("Client:Main:ConnectTunnelError2") + " ", Color.Yellow);
                    return;
                }

                StringBuilder sb = new StringBuilder(START_GAME_CTCP_COMMAND + " ");
                for (int pId = 0; pId < Players.Count; pId++)
                {
                    Players[pId].Port = playerPorts[pId];
                    sb.Append(Players[pId].Name);
                    sb.Append(";");
                    sb.Append("0.0.0.0:");
                    sb.Append(playerPorts[pId]);
                    sb.Append(";");
                }
                sb.Remove(sb.Length - 1, 1);
                channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 9);

                AddNotice("Starting game...".L10N("Client:Main:StartingGame"));
                started = true;
                LoadGame();
            }
            else if (_tunnelMode == TunnelMode.V3Dynamic && Players.Count > 1)
            {
                // Double-check everyone is still reachable before STARTV3 goes out over IRC —
                // IRC can take minutes to notice a dead connection, and a start command sent
                // to a player who never receives it strands the rest at the loading screen.
                if (_negotiator.LaunchConnectivityCheckInProgress)
                {
                    AddNotice("Still verifying player connections...".L10N("Client:Main:VerifyingConnectionsWait"), Color.Yellow);
                    return;
                }

                _negotiator.BeginLaunchConnectivityCheck(FinishV3DynamicLaunch);
            }
            else
            {
                // V3 static (or dynamic with no other players)
                SendStartV3ToPlayers();
                AddNotice("Starting game...".L10N("Client:Main:StartingGame"));
                started = true;
                StartV3Game();
            }
        }

        /// <summary>
        /// Launch tail for V3 dynamic mode, run once the pre-launch connectivity check verifies
        /// that every player is still reachable.
        /// </summary>
        private void FinishV3DynamicLaunch()
        {
            SendStartV3ToPlayers();
            AddNotice("Starting game...".L10N("Client:Main:StartingGame"));
            started = true;
            StartV3Game();
        }

        protected override void WriteSpawnIniAdditions(IniFile spawnIni)
        {
            if (_tunnelMode == TunnelMode.V2Legacy && tunnelHandler.CurrentTunnel != null)
            {
                spawnIni.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
                spawnIni.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);
            }
            else
            {
                PlayerInfo localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
                if (localPlayer != null)
                {
                    spawnIni.SetStringValue("Tunnel", "Ip", IPAddress.Loopback.ToString());
                    spawnIni.SetIntValue("Tunnel", "Port", localPlayer.Port);
                }
            }

            base.WriteSpawnIniAdditions(spawnIni);
        }

        protected override void HandleGameProcessExited()
        {
            tunnelHandler.StopGameBridge();
            base.HandleGameProcessExited();
            Clear();
        }

        protected override void LeaveGame() => Clear();

        public void ChangeChatColor(IRCColor chatColor)
        {
            this.chatColor = chatColor;
            tbChatInput.TextColor = chatColor.XnaColor;
        }

        private void BroadcastGame()
        {
            Channel broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

            if (broadcastChannel == null)
                return;

            StringBuilder sb = new StringBuilder("GAME ");
            sb.Append(ProgramConstants.CNCNET_PROTOCOL_REVISION);
            sb.Append(";");
            sb.Append(ProgramConstants.GAME_VERSION);
            sb.Append(";");
            sb.Append(SGPlayers.Count);
            sb.Append(";");
            sb.Append(channel.ChannelName);
            sb.Append(";");
            sb.Append(channel.UIName);
            sb.Append(";");
            if (started || Players.Count == SGPlayers.Count)
                sb.Append("1");
            else
                sb.Append("0");
            sb.Append("0"); // IsCustomPassword
            sb.Append("0"); // Closed
            sb.Append("1"); // IsLoadedGame
            sb.Append("0"); // IsLadder
            sb.Append(";");
            foreach (SavedGamePlayer sgPlayer in SGPlayers)
            {
                sb.Append(sgPlayer.Name);
                sb.Append(",");
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append(";");
            sb.Append((string)lblMapNameValue.Tag);
            sb.Append(";");
            sb.Append((string)lblGameModeValue.Tag);
            sb.Append(";");
            sb.Append(_tunnelMode == TunnelMode.V3Dynamic
                ? "[DYN]"
                : tunnelHandler.CurrentTunnel != null
                    ? tunnelHandler.CurrentTunnel.Address + ":" + tunnelHandler.CurrentTunnel.Port
                    : "0.0.0.0:0");
            sb.Append(";");
            sb.Append(0); // LoadedGameId
            sb.Append(";");
            sb.Append(ClientConfiguration.Instance.DefaultSkillLevelIndex); // we don't know the original skill level
            sb.Append(";");
            sb.Append(savedMapSHA1);
            sb.Append(";");
            sb.Append(savedBroadcastOptionValues);

            broadcastChannel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 20);
        }

        public override string GetSwitchName() => "Load Game".L10N("Client:Main:LoadGame");

        protected override void UpdateDiscordPresence(bool resetTimer = false)
        {
            if (discordHandler == null)
                return;

            PlayerInfo player = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
            if (player == null)
                return;
            string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby"; // not UI strings

            discordHandler.UpdatePresence(
                (string)lblMapNameValue.Tag, (string)lblGameModeValue.Tag, "Multiplayer",
                currentState, Players.Count, SGPlayers.Count,
                channel.UIName, IsHost, resetTimer);
        }

        #region V3 Tunnel Support

        private void UpdateLoadGameButtonStatus()
        {
            if (IsHost)
                btnLoadGame.AllowClick = true;
        }

        // IV3NegotiationHost implementation — the shared negotiation orchestration lives in
        // V3TunnelNegotiationManager; these members supply lobby-specific transport and UI.
        List<PlayerInfo> IV3NegotiationHost.Players => Players;

        string IV3NegotiationHost.ChannelName => channel.ChannelName;

        TunnelMode IV3NegotiationHost.TunnelMode => _tunnelMode;

        bool IV3NegotiationHost.IsHost => IsHost;

        void IV3NegotiationHost.SendNegotiationReport(string message)
            => channel.SendCTCPMessage(message, QueuedMessageType.GAME_NEGOTIATION_MESSAGE, 10);

        void IV3NegotiationHost.SendChannelCTCP(string message, int priority)
            => channel.SendCTCPMessage(message, QueuedMessageType.SYSTEM_MESSAGE, priority);

        void IV3NegotiationHost.AddNotice(string message, Color color) => AddNotice(message, color);

        void IV3NegotiationHost.OnNegotiationStateChanged() => UpdateLoadGameButtonStatus();

        void IV3NegotiationHost.OnLocalNegotiationStatus(PlayerInfo player, NegotiationStatus status, int ping)
        {
            if (status == NegotiationStatus.Succeeded)
            {
                var tunnel = _negotiator.FindPlayer(player.Name)?.Tunnel;
                if (tunnel != null)
                    AddNotice(string.Format("Tunnel negotiated with {0}: {1}".L10N("Client:Main:TunnelNegotiatedWith"), player.Name, tunnel.Name));
            }
        }

        void IV3NegotiationHost.OnRemoteNegotiationStatus(PlayerInfo player, NegotiationStatus status, int ping)
        {
        }

        void IV3NegotiationHost.OnNegotiationsRestarted()
        {
        }

        void IV3NegotiationHost.OnPairPingUpdated(PlayerInfo player, int ping)
        {
            // The loading lobby has no per-player ping display to refresh.
        }

        private void SendStartV3ToPlayers()
            => channel.SendCTCPMessage($"{START_GAME_V3_CTCP_COMMAND} {_negotiator.GenerateV3StartPayload()}",
                QueuedMessageType.SYSTEM_MESSAGE, 9);

        private void StartV3Game()
        {
            if (!_negotiator.StartGameBridge())
                return;

            LoadGame();
        }

        private void HandleTunnelModeChange(TunnelMode mode, bool isHostInitiated)
        {
            if (mode == _tunnelMode)
                return;

            var oldMode = _tunnelMode;
            _tunnelMode = mode;

            _negotiator.ApplyModeTransition(oldMode, mode);

            string modeDescription = mode.GetDescription();
            AddNotice(isHostInitiated
                ? string.Format("Tunnel mode changed to {0}.".L10N("Client:Main:TunnelModeChanged"), modeDescription)
                : string.Format("The game host has changed tunnel mode to {0}.".L10N("Client:Main:TunnelModeChangedByHost"), modeDescription));

            if (mode == TunnelMode.V3Dynamic)
                tunnelHandler.CurrentTunnel = null;

            UpdateLoadGameButtonStatus();
        }

        private void TunnelHandler_TunnelFailed(object sender, TunnelFailedEventArgs e)
        {
            CnCNetTunnel failedTunnel = e.Tunnel;
            if (tunnelHandler.GameTunnelBridge != null && tunnelHandler.GameTunnelBridge.IsRunning)
                return;

            if (_negotiator.TryHandleTunnelFailure(failedTunnel))
                return;

            if (IsHost)
                AddNotice(string.Format("Tunnel {0} failed. Please select a different tunnel.".L10N("Client:Main:TunnelFailedSelectDifferent"), failedTunnel.Name), Color.Orange);
            else
            {
                AddNotice(string.Format("Tunnel {0} failed. Waiting for host to select a new tunnel...".L10N("Client:Main:TunnelFailedWaitingForHost"), failedTunnel.Name), Color.Orange);
                channel.SendCTCPMessage($"{TunnelNegotiationCommands.TunnelFailed} {failedTunnel.Name}",
                    QueuedMessageType.SYSTEM_MESSAGE, 10);
            }
        }

        #endregion
    }
}
