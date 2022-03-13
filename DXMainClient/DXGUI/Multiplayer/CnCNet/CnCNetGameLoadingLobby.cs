using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A game lobby for loading saved CnCNet games.
    /// </summary>
    public class CnCNetGameLoadingLobby : GameLoadingLobbyBase
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
        private const string PLAYER_READY_CTCP_COMMAND = "READY";
        private const string CHANGE_TUNNEL_SERVER_MESSAGE = "CHTNL";

        public CnCNetGameLoadingLobby(WindowManager windowManager, TopBar topBar,
            CnCNetManager connectionManager, TunnelHandler tunnelHandler,
            List<GameMode> gameModes, GameCollection gameCollection, DiscordHandler discordHandler) : base(windowManager, discordHandler)
        {
            this.connectionManager = connectionManager;
            this.tunnelHandler = tunnelHandler;
            this.gameModes = gameModes;
            this.topBar = topBar;
            this.gameCollection = gameCollection;

            ctcpCommandHandlers = new CommandHandlerBase[]
            {
                new NoParamCommandHandler(NOT_ALL_PLAYERS_PRESENT_CTCP_COMMAND, HandleNotAllPresentNotification),
                new NoParamCommandHandler(GET_READY_CTCP_COMMAND, HandleGetReadyNotification),
                new StringCommandHandler(FILE_HASH_CTCP_COMMAND, HandleFileHashCommand),
                new StringCommandHandler(INVALID_FILE_HASH_CTCP_COMMAND, HandleCheaterNotification),
                new IntCommandHandler(TUNNEL_PING_CTCP_COMMAND, HandleTunnelPing),
                new StringCommandHandler(OPTIONS_CTCP_COMMAND, HandleOptionsMessage),
                new NoParamCommandHandler(INVALID_SAVED_GAME_INDEX_CTCP_COMMAND, HandleInvalidSaveIndexCommand),
                new StringCommandHandler(START_GAME_CTCP_COMMAND, HandleStartGameCommand),
                new IntCommandHandler(PLAYER_READY_CTCP_COMMAND, HandlePlayerReadyRequest),
                new StringCommandHandler(CHANGE_TUNNEL_SERVER_MESSAGE, HandleTunnelServerChangeMessage)
            };
        }

        private CommandHandlerBase[] ctcpCommandHandlers;

        private CnCNetManager connectionManager;

        private List<GameMode> gameModes;

        private TunnelHandler tunnelHandler;
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

        public override void Initialize()
        {
            dp = new DarkeningPanel(WindowManager);
            //WindowManager.AddAndInitializeControl(dp);

            //dp.AddChildWithoutInitialize(this);

            //dp.Alpha = 0.0f;
            //dp.Hide();
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
            btnChangeTunnel.Text = "Change Tunnel".L10N("UI:Main:ChangeTunnel");
            btnChangeTunnel.LeftClick += BtnChangeTunnel_LeftClick;
            AddChild(btnChangeTunnel);

            gameBroadcastTimer = new XNATimerControl(WindowManager);
            gameBroadcastTimer.AutoReset = true;
            gameBroadcastTimer.Interval = TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL);
            gameBroadcastTimer.Enabled = true;
            gameBroadcastTimer.TimeElapsed += GameBroadcastTimer_TimeElapsed;

            WindowManager.AddAndInitializeControl(gameBroadcastTimer);
        }

        private void BtnChangeTunnel_LeftClick(object sender, EventArgs e) => ShowTunnelSelectionWindow("Select tunnel server:");

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

            tunnelHandler.CurrentTunnel = tunnel;
            tunnelHandler.CurrentTunnelPinged += TunnelHandler_CurrentTunnelPinged;

            started = false;

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
            fhc.CalculateHashes(gameModes);

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

                channel.SendCTCPMessage(TUNNEL_PING_CTCP_COMMAND + " " + tunnelHandler.CurrentTunnel.PingInMs, QueuedMessageType.SYSTEM_MESSAGE, 10);

                if (tunnelHandler.CurrentTunnel.PingInMs < 0)
                    AddNotice(string.Format("{0} - unknown ping to tunnel server.".L10N("UI:Main:PlayerUnknownPing"), ProgramConstants.PLAYERNAME));
                else
                    AddNotice(string.Format("{0} - ping to tunnel server: {1} ms".L10N("UI:Main:PlayerPing"), ProgramConstants.PLAYERNAME, tunnelHandler.CurrentTunnel.PingInMs));
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

            BroadcastOptions();
            CopyPlayerDataToUI();
            UpdateDiscordPresence();
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

            Players.RemoveAt(index);

            CopyPlayerDataToUI();

            if (!IsHost && playerName == hostName && !ProgramConstants.IsInGame)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    Color.Yellow, "The game host left the game!".L10N("UI:Main:HostLeft")));

                Clear();
            }
        }

        private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            lbChatMessages.AddMessage(e.Message);

            if (e.Message.SenderName != null)
                sndMessageSound.Play();
        }

        protected override void AddNotice(string message, Color color) => channel.AddMessage(new ChatMessage(color, message));

        protected override void BroadcastOptions()
        {
            if (!IsHost)
                return;

            //if (Players.Count > 0)
            Players[0].Ready = true;

            StringBuilder message = new StringBuilder(OPTIONS_CTCP_COMMAND + " ");
            message.Append(ddSavedGame.SelectedIndex);
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
                tunnelHandler.CurrentTunnel?.Address);
        }

        private void TunnelSelectionWindow_TunnelSelected(object sender, TunnelEventArgs e)
        {
            channel.SendCTCPMessage($"{CHANGE_TUNNEL_SERVER_MESSAGE} {e.Tunnel.Address}:{e.Tunnel.Port}",
                QueuedMessageType.SYSTEM_MESSAGE, 10);
            HandleTunnelServerChange(e.Tunnel);
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

            if (fileHash != gameFilesHash)
            {
                PlayerInfo pInfo = Players.Find(p => p.Name == sender);

                if (pInfo == null)
                    return;

                pInfo.Verified = true;

                HandleCheaterNotification(hostName, sender); // This is kinda hacky
            }
        }

        private void HandleCheaterNotification(string sender, string cheaterName)
        {
            if (sender != hostName)
                return;

            AddNotice(string.Format("{0} - modified files detected! They could be cheating!".L10N("UI:Main:PlayerCheating"), cheaterName), Color.Red);

            if (IsHost)
                channel.SendCTCPMessage(INVALID_FILE_HASH_CTCP_COMMAND + " " + cheaterName, QueuedMessageType.SYSTEM_MESSAGE, 0);
        }

        private void HandleTunnelPing(string sender, int pingInMs)
        {
            if (pingInMs < 0)
                AddNotice(string.Format("{0} - unknown ping to tunnel server.".L10N("UI:Main:PlayerUnknownPing"), sender));
            else
                AddNotice(string.Format("{0} - ping to tunnel server: {1} ms".L10N("UI:Main:PlayerPing"), sender, pingInMs));
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
                AddNotice("The game host has selected an invalid saved game index!".L10N("UI:Main:HostInvalidIndex") + " " + sgIndex);
                channel.SendCTCPMessage(INVALID_SAVED_GAME_INDEX_CTCP_COMMAND, QueuedMessageType.SYSTEM_MESSAGE, 10);
                return;
            }

            ddSavedGame.SelectedIndex = sgIndex;

            Players.Clear();

            for (int i = 1; i < parts.Length; i++)
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
        }

        private void HandleInvalidSaveIndexCommand(string sender)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            pInfo.Ready = false;

            AddNotice(string.Format("{0} does not have the selected saved game on their system! Try selecting an earlier saved game.".L10N("UI:Main:PlayerDontHaveSavedGame"), pInfo.Name));

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
            string tunnelAddress = split[0];
            int tunnelPort = int.Parse(split[1]);

            CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort);
            if (tunnel == null)
            {
                AddNotice(("The game host has selected an invalid tunnel server! " +
                    "The game host needs to change the server or you will be unable " +
                    "to participate in the match.").L10N("UI:Main:HostInvalidTunnel"),
                    Color.Yellow);
                btnLoadGame.AllowClick = false;
                return;
            }

            HandleTunnelServerChange(tunnel);
            btnLoadGame.AllowClick = true;
        }

        /// <summary>
        /// Changes the tunnel server used for the game.
        /// </summary>
        /// <param name="tunnel">The new tunnel server to use.</param>
        private void HandleTunnelServerChange(CnCNetTunnel tunnel)
        {
            tunnelHandler.CurrentTunnel = tunnel;
            AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("UI:Main:HostChangeTunnel"), tunnel.Name));
            //UpdatePing();
        }

        #endregion

        protected override void HostStartGame()
        {
            AddNotice("Contacting tunnel server...".L10N("UI:Main:ConnectingTunnel"));
            List<int> playerPorts = tunnelHandler.CurrentTunnel.GetPlayerPortInfo(SGPlayers.Count);

            if (playerPorts.Count < Players.Count)
            {
                ShowTunnelSelectionWindow(("An error occured while contacting " +
                        "the CnCNet tunnel server." + Environment.NewLine +
                        "Try picking a different tunnel server:").L10N("UI:Main:ConnectTunnelError1"));
                AddNotice(("An error occured while contacting the specified CnCNet " +
                    "tunnel server. Please try using a different tunnel server ").L10N("UI:Main:ConnectTunnelError2"), Color.Yellow);
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

            AddNotice("Starting game...".L10N("UI:Main:StartingGame"));

            started = true;

            LoadGame();
        }

        protected override void WriteSpawnIniAdditions(IniFile spawnIni)
        {
            spawnIni.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
            spawnIni.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);

            base.WriteSpawnIniAdditions(spawnIni);
        }

        protected override void HandleGameProcessExited()
        {
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
            sb.Append(lblMapNameValue.Text);
            sb.Append(";");
            sb.Append(lblGameModeValue.Text);
            sb.Append(";");
            sb.Append(tunnelHandler.CurrentTunnel.Address + ":" + tunnelHandler.CurrentTunnel.Port);
            sb.Append(";");
            sb.Append(0); // LoadedGameId

            broadcastChannel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 20);
        }

        public override string GetSwitchName() => "Load Game".L10N("UI:Main:LoadGame");

        protected override void UpdateDiscordPresence(bool resetTimer = false)
        {
            if (discordHandler == null)
                return;

            PlayerInfo player = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
            if (player == null)
                return;
            string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby"; // not UI strings

            discordHandler.UpdatePresence(
                lblMapNameValue.Text, lblGameModeValue.Text, "Multiplayer",
                currentState, Players.Count, SGPlayers.Count,
                channel.UIName, IsHost, resetTimer);
        }
    }
}
