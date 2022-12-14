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
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ClientCore.Extensions;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A game lobby for loading saved CnCNet games.
    /// </summary>
    internal sealed class CnCNetGameLoadingLobby : GameLoadingLobbyBase
    {
        private const double GAME_BROADCAST_INTERVAL = 20.0;
        private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;

        public CnCNetGameLoadingLobby(
            WindowManager windowManager,
            TopBar topBar,
            CnCNetManager connectionManager,
            TunnelHandler tunnelHandler,
            GameCollection gameCollection,
            DiscordHandler discordHandler)
            : base(windowManager, discordHandler)
        {
            this.connectionManager = connectionManager;
            this.tunnelHandler = tunnelHandler;
            this.topBar = topBar;
            this.gameCollection = gameCollection;

            ctcpCommandHandlers = new CommandHandlerBase[]
            {
                new NoParamCommandHandler(CnCNetCommands.NOT_ALL_PLAYERS_PRESENT, sender => HandleNotAllPresentNotificationAsync(sender).HandleTask()),
                new NoParamCommandHandler(CnCNetCommands.GET_READY, sender => HandleGetReadyNotificationAsync(sender).HandleTask()),
                new StringCommandHandler(CnCNetCommands.FILE_HASH, (sender, fileHash) => HandleFileHashCommandAsync(sender, fileHash).HandleTask()),
                new StringCommandHandler(CnCNetCommands.INVALID_FILE_HASH, (sender, cheaterName) => HandleCheaterNotificationAsync(sender, cheaterName).HandleTask()),
                new IntCommandHandler(CnCNetCommands.TUNNEL_PING, HandleTunnelPing),
                new StringCommandHandler(CnCNetCommands.OPTIONS, (sender, data) => HandleOptionsMessageAsync(sender, data).HandleTask()),
                new NoParamCommandHandler(CnCNetCommands.INVALID_SAVED_GAME_INDEX, HandleInvalidSaveIndexCommand),
                new StringCommandHandler(CnCNetCommands.START_GAME, (sender, data) => HandleStartGameCommandAsync(sender, data).HandleTask()),
                new IntCommandHandler(CnCNetCommands.PLAYER_READY, (sender, readyStatus) => HandlePlayerReadyRequestAsync(sender, readyStatus).HandleTask()),
                new StringCommandHandler(CnCNetCommands.CHANGE_TUNNEL_SERVER, HandleTunnelServerChangeMessage)
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

        private EventHandler<UserNameEventArgs> channel_UserLeftFunc;
        private EventHandler<UserNameEventArgs> channel_UserQuitIRCFunc;
        private EventHandler<ChannelUserEventArgs> channel_UserAddedFunc;

        public override void Initialize()
        {
            dp = new DarkeningPanel(WindowManager);
            localGame = ClientConfiguration.Instance.LocalGame;

            base.Initialize();

            connectionManager.ConnectionLost += (_, _) => ClearAsync().HandleTask();
            connectionManager.Disconnected += (_, _) => ClearAsync().HandleTask();

            tunnelSelectionWindow = new TunnelSelectionWindow(WindowManager, tunnelHandler);
            tunnelSelectionWindow.Initialize();
            tunnelSelectionWindow.DrawOrder = 1;
            tunnelSelectionWindow.UpdateOrder = 1;
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, tunnelSelectionWindow);
            tunnelSelectionWindow.CenterOnParent();
            tunnelSelectionWindow.Disable();
            tunnelSelectionWindow.TunnelSelected += (_, e) => TunnelSelectionWindow_TunnelSelectedAsync(e).HandleTask();

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
            gameBroadcastTimer.Enabled = true;
            gameBroadcastTimer.TimeElapsed += (_, _) => BroadcastGameAsync().HandleTask();

            WindowManager.AddAndInitializeControl(gameBroadcastTimer);
        }

        private void BtnChangeTunnel_LeftClick(object sender, EventArgs e) => ShowTunnelSelectionWindow("Select tunnel server:");

        /// <summary>
        /// Sets up events and information before joining the channel.
        /// </summary>
        public void SetUp(bool isHost, CnCNetTunnel tunnel, Channel channel, string hostName)
        {
            this.channel = channel;
            this.hostName = hostName;

            channel_UserLeftFunc = (_, args) => OnPlayerLeftAsync(args).HandleTask();
            channel_UserQuitIRCFunc = (_, args) => OnPlayerLeftAsync(args).HandleTask();
            channel_UserAddedFunc = (_, args) => Channel_UserAddedAsync(args).HandleTask();

            channel.MessageAdded += Channel_MessageAdded;
            channel.UserAdded += channel_UserAddedFunc;
            channel.UserLeft += channel_UserLeftFunc;
            channel.UserQuitIRC += channel_UserQuitIRCFunc;
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
        public async ValueTask ClearAsync()
        {
            gameBroadcastTimer.Enabled = false;

            if (channel != null)
            {
                // TODO leave channel only if we've joined the channel
                await channel.LeaveAsync().ConfigureAwait(false);

                channel.MessageAdded -= Channel_MessageAdded;
                channel.UserAdded -= channel_UserAddedFunc;
                channel.UserLeft -= channel_UserLeftFunc;
                channel.UserQuitIRC -= channel_UserQuitIRCFunc;
                channel.CTCPReceived -= Channel_CTCPReceived;

                connectionManager.RemoveChannel(channel);
            }

            if (Enabled)
            {
                Enabled = false;
                Visible = false;

                await base.LeaveGameAsync().ConfigureAwait(false);
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
        public async ValueTask OnJoinedAsync()
        {
            FileHashCalculator fhc = new FileHashCalculator();
            fhc.CalculateHashes(gameModes);

            if (IsHost)
            {
                await connectionManager.SendCustomMessageAsync(new QueuedMessage(
                    FormattableString.Invariant($"{IRCCommands.MODE} {channel.ChannelName} +{IRCChannelModes.DEFAULT} {channel.Password} {SGPlayers.Count}"),
                    QueuedMessageType.SYSTEM_MESSAGE, 50)).ConfigureAwait(false);

                await connectionManager.SendCustomMessageAsync(new QueuedMessage(
                    string.Format(IRCCommands.TOPIC + " {0} :{1}", channel.ChannelName,
                    ProgramConstants.CNCNET_PROTOCOL_REVISION + ";" + localGame.ToLower()),
                    QueuedMessageType.SYSTEM_MESSAGE, 50)).ConfigureAwait(false);

                gameFilesHash = fhc.GetCompleteHash();

                gameBroadcastTimer.Enabled = true;
                gameBroadcastTimer.Start();
                gameBroadcastTimer.SetTime(TimeSpan.FromSeconds(INITIAL_GAME_BROADCAST_DELAY));
            }
            else
            {
                await channel.SendCTCPMessageAsync(
                    CnCNetCommands.FILE_HASH + " " + fhc.GetCompleteHash(), QueuedMessageType.SYSTEM_MESSAGE, 10).ConfigureAwait(false);
                await channel.SendCTCPMessageAsync(
                    CnCNetCommands.TUNNEL_PING + " " + tunnelHandler.CurrentTunnel.PingInMs, QueuedMessageType.SYSTEM_MESSAGE, 10).ConfigureAwait(false);

                if (tunnelHandler.CurrentTunnel.PingInMs < 0)
                    AddNotice(string.Format("{0} - unknown ping to tunnel server.".L10N("Client:Main:PlayerUnknownPing"), ProgramConstants.PLAYERNAME));
                else
                    AddNotice(string.Format("{0} - ping to tunnel server: {1} ms".L10N("Client:Main:PlayerPing"), ProgramConstants.PLAYERNAME, tunnelHandler.CurrentTunnel.PingInMs));
            }

            topBar.AddPrimarySwitchable(this);
            topBar.SwitchToPrimary();
            WindowManager.SelectedControl = tbChatInput;
            UpdateDiscordPresence(true);
        }

        private async ValueTask Channel_UserAddedAsync(ChannelUserEventArgs e)
        {
            PlayerInfo pInfo = new PlayerInfo();
            pInfo.Name = e.User.IRCUser.Name;

            Players.Add(pInfo);

            sndJoinSound.Play();

            await BroadcastOptionsAsync().ConfigureAwait(false);
            CopyPlayerDataToUI();
            UpdateDiscordPresence();
        }

        private async ValueTask OnPlayerLeftAsync(UserNameEventArgs e)
        {
            await RemovePlayerAsync(e.UserName).ConfigureAwait(false);
            UpdateDiscordPresence();
        }

        private async ValueTask RemovePlayerAsync(string playerName)
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
                    Color.Yellow, "The game host left the game!".L10N("Client:Main:HostLeft")));

                await ClearAsync().ConfigureAwait(false);
            }
        }

        private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            lbChatMessages.AddMessage(e.Message);

            if (e.Message.SenderName != null)
                sndMessageSound.Play();
        }

        protected override void AddNotice(string message, Color color) => channel.AddMessage(new ChatMessage(color, message));

        protected override async ValueTask BroadcastOptionsAsync()
        {
            if (!IsHost)
                return;

            //if (Players.Count > 0)
            Players[0].Ready = true;

            StringBuilder message = new StringBuilder(CnCNetCommands.OPTIONS + " ");
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

            await channel.SendCTCPMessageAsync(message.ToString(), QueuedMessageType.GAME_SETTINGS_MESSAGE, 10).ConfigureAwait(false);
        }

        protected override ValueTask SendChatMessageAsync(string message)
        {
            sndMessageSound.Play();

            return channel.SendChatMessageAsync(message, chatColor);
        }

        protected override ValueTask RequestReadyStatusAsync() =>
            channel.SendCTCPMessageAsync(CnCNetCommands.PLAYER_READY + " 1", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 10);

        protected override async ValueTask GetReadyNotificationAsync()
        {
            await base.GetReadyNotificationAsync().ConfigureAwait(false);

            topBar.SwitchToPrimary();

            if (IsHost)
                await channel.SendCTCPMessageAsync(CnCNetCommands.GET_READY, QueuedMessageType.GAME_GET_READY_MESSAGE, 0).ConfigureAwait(false);
        }

        protected override async ValueTask NotAllPresentNotificationAsync()
        {
            await base.NotAllPresentNotificationAsync().ConfigureAwait(false);

            if (IsHost)
            {
                await channel.SendCTCPMessageAsync(CnCNetCommands.NOT_ALL_PLAYERS_PRESENT,
                    QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0).ConfigureAwait(false);
            }
        }

        private void ShowTunnelSelectionWindow(string description)
            => tunnelSelectionWindow.Open(description, tunnelHandler.CurrentTunnel);

        private async ValueTask TunnelSelectionWindow_TunnelSelectedAsync(TunnelEventArgs e)
        {
            await channel.SendCTCPMessageAsync(
                $"{CnCNetCommands.CHANGE_TUNNEL_SERVER} {e.Tunnel.Hash}",
                QueuedMessageType.SYSTEM_MESSAGE,
                10).ConfigureAwait(false);
            HandleTunnelServerChange(e.Tunnel);
        }

        #region CTCP Handlers

        private async ValueTask HandleGetReadyNotificationAsync(string sender)
        {
            if (sender != hostName)
                return;

            await GetReadyNotificationAsync().ConfigureAwait(false);
        }

        private async ValueTask HandleNotAllPresentNotificationAsync(string sender)
        {
            if (sender != hostName)
                return;

            await NotAllPresentNotificationAsync().ConfigureAwait(false);
        }

        private async ValueTask HandleFileHashCommandAsync(string sender, string fileHash)
        {
            if (!IsHost)
                return;

            if (fileHash != gameFilesHash)
            {
                PlayerInfo pInfo = Players.Find(p => p.Name == sender);

                if (pInfo == null)
                    return;

                pInfo.Verified = true;

                await HandleCheaterNotificationAsync(hostName, sender).ConfigureAwait(false); // This is kinda hacky
            }
        }

        private async ValueTask HandleCheaterNotificationAsync(string sender, string cheaterName)
        {
            if (sender != hostName)
                return;

            AddNotice(string.Format("{0} - modified files detected! They could be cheating!".L10N("Client:Main:PlayerCheating"), cheaterName), Color.Red);

            if (IsHost)
                await channel.SendCTCPMessageAsync(CnCNetCommands.INVALID_FILE_HASH + " " + cheaterName, QueuedMessageType.SYSTEM_MESSAGE, 0).ConfigureAwait(false);
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
        private async ValueTask HandleOptionsMessageAsync(string sender, string data)
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
                await channel.SendCTCPMessageAsync(CnCNetCommands.INVALID_SAVED_GAME_INDEX, QueuedMessageType.SYSTEM_MESSAGE, 10).ConfigureAwait(false);
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

            AddNotice(string.Format("{0} does not have the selected saved game on their system! Try selecting an earlier saved game.".L10N("Client:Main:PlayerDontHaveSavedGame"), pInfo.Name));

            CopyPlayerDataToUI();
        }

        private async ValueTask HandleStartGameCommandAsync(string sender, string data)
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

            await LoadGameAsync().ConfigureAwait(false);
        }

        private async ValueTask HandlePlayerReadyRequestAsync(string sender, int readyStatus)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo == null)
                return;

            pInfo.Ready = Convert.ToBoolean(readyStatus);

            CopyPlayerDataToUI();

            if (IsHost)
                await BroadcastOptionsAsync().ConfigureAwait(false);
        }

        private void HandleTunnelServerChangeMessage(string sender, string hash)
        {
            if (sender != hostName)
                return;

            CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));

            if (tunnel == null)
            {
                AddNotice(("The game host has selected an invalid tunnel server! " +
                    "The game host needs to change the server or you will be unable " +
                    "to participate in the match.").L10N("Client:Main:HostInvalidTunnel"),
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
            AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("Client:Main:HostChangeTunnel"), tunnel.Name));
        }

        #endregion

        protected override async ValueTask HostStartGameAsync()
        {
            AddNotice("Contacting tunnel server...".L10N("Client:Main:ConnectingTunnel"));
            List<int> playerPorts = await tunnelHandler.CurrentTunnel.GetPlayerPortInfoAsync(SGPlayers.Count).ConfigureAwait(false);

            if (playerPorts.Count < Players.Count)
            {
                ShowTunnelSelectionWindow(("An error occured while contacting " +
                        "the CnCNet tunnel server." + Environment.NewLine +
                        "Try picking a different tunnel server:").L10N("Client:Main:ConnectTunnelError1"));
                AddNotice(("An error occured while contacting the specified CnCNet " +
                    "tunnel server. Please try using a different tunnel server ").L10N("Client:Main:ConnectTunnelError2"), Color.Yellow);
                return;
            }

            StringBuilder sb = new StringBuilder(CnCNetCommands.START_GAME + " ");
            for (int pId = 0; pId < Players.Count; pId++)
            {
                Players[pId].Port = playerPorts[pId];
                sb.Append(Players[pId].Name);
                sb.Append(";");
                sb.Append($"{IPAddress.Any}:");
                sb.Append(playerPorts[pId]);
                sb.Append(";");
            }
            sb.Remove(sb.Length - 1, 1);
            await channel.SendCTCPMessageAsync(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 9).ConfigureAwait(false);

            AddNotice("Starting game...".L10N("Client:Main:StartingGame"));

            started = true;

            await LoadGameAsync().ConfigureAwait(false);
        }

        protected override void WriteSpawnIniAdditions(IniFile spawnIni)
        {
            spawnIni.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
            spawnIni.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);

            base.WriteSpawnIniAdditions(spawnIni);
        }

        protected override async ValueTask HandleGameProcessExitedAsync()
        {
            await base.HandleGameProcessExitedAsync().ConfigureAwait(false);
            await ClearAsync().ConfigureAwait(false);
        }

        protected override ValueTask LeaveGameAsync() => ClearAsync();

        public void ChangeChatColor(IRCColor chatColor)
        {
            this.chatColor = chatColor;
            tbChatInput.TextColor = chatColor.XnaColor;
        }

        private async ValueTask BroadcastGameAsync()
        {
            Channel broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

            if (broadcastChannel == null)
                return;

            StringBuilder sb = new StringBuilder(CnCNetCommands.GAME + " ");
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
            sb.Append(tunnelHandler.CurrentTunnel?.Hash ?? ProgramConstants.CNCNET_DYNAMIC_TUNNELS);
            sb.Append(";");
            sb.Append(0); // LoadedGameId

            await broadcastChannel.SendCTCPMessageAsync(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 20).ConfigureAwait(false);
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
    }
}