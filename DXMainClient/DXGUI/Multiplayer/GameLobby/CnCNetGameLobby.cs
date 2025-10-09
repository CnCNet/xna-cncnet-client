using ClientCore;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore.Extensions;
using System.Net;
using System.Diagnostics;
using System.Security.Cryptography;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public enum NegotiationStatus
    {
        NotStarted,
        InProgress,
        Succeeded,
        Failed
    }

    public class CnCNetGameLobby : MultiplayerGameLobby
    {
        private const int HUMAN_PLAYER_OPTIONS_LENGTH = 3;
        private const int AI_PLAYER_OPTIONS_LENGTH = 2;

        private const double GAME_BROADCAST_INTERVAL = 30.0;
        private const double GAME_BROADCAST_ACCELERATION = 10.0;
        private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;

        private static readonly Color ERROR_MESSAGE_COLOR = Color.Yellow;

        private const string MAP_SHARING_FAIL_MESSAGE = "MAPFAIL";
        private const string MAP_SHARING_DOWNLOAD_REQUEST = "MAPOK";
        private const string MAP_SHARING_UPLOAD_REQUEST = "MAPREQ";
        private const string MAP_SHARING_DISABLED_MESSAGE = "MAPSDISABLED";
        private const string CHEAT_DETECTED_MESSAGE = "CD";
        private const string DICE_ROLL_MESSAGE = "DR";
        private const string CHANGE_TUNNEL_SERVER_MESSAGE = "CHTNL";
        private const string PLAYER_TUNNEL_MESSAGE = "PLYTNL";
        private const string NEGOTIATION_INFO_MESSAGE = "NEGINFO";
        private const string TUNNEL_RENEGOTIATE_MESSAGE = "TNLRENEG";
        private const string TUNNEL_FAILED_MESSAGE = "TNLFAIL";

        public CnCNetGameLobby(
            WindowManager windowManager,
            TopBar topBar,
            CnCNetManager connectionManager,
            TunnelHandler tunnelHandler,
            GameCollection gameCollection,
            CnCNetUserData cncnetUserData,
            MapLoader mapLoader,
            DiscordHandler discordHandler,
            PrivateMessagingWindow pmWindow,
            Random random
        ) : base(windowManager, "MultiplayerGameLobby", topBar, mapLoader, discordHandler, pmWindow, random)
        {
            this.connectionManager = connectionManager;
            localGame = ClientConfiguration.Instance.LocalGame;
            this.tunnelHandler = tunnelHandler;
            this.gameCollection = gameCollection;
            this.cncnetUserData = cncnetUserData;
            this.pmWindow = pmWindow;
            this.random = random;
            this._useDynamicTunnels = UserINISettings.Instance.UseDynamicTunnels;

            gameHostInactiveChecker = ClientConfiguration.Instance.InactiveHostKickEnabled? new GameHostInactiveChecker(WindowManager) : null;

            ctcpCommandHandlers = new CommandHandlerBase[]
            {
                new IntCommandHandler("OR", HandleOptionsRequest),
                new IntCommandHandler("R", HandleReadyRequest),
                new StringCommandHandler("PO", ApplyPlayerOptions),
                new StringCommandHandler(PlayerExtraOptions.CNCNET_MESSAGE_KEY, ApplyPlayerExtraOptions),
                new StringCommandHandler("GO", ApplyGameOptions),
                new StringCommandHandler("STARTV2", NonHostLaunchGameV2),
                new StringCommandHandler("STARTV3", NonHostLaunchGameV3),
                new NotificationHandler("AISPECS", HandleNotification, AISpectatorsNotification),
                new NotificationHandler("GETREADY", HandleNotification, GetReadyNotification),
                new NotificationHandler("INSFSPLRS", HandleNotification, InsufficientPlayersNotification),
                new NotificationHandler("TMPLRS", HandleNotification, TooManyPlayersNotification),
                new NotificationHandler("CLRS", HandleNotification, SharedColorsNotification),
                new NotificationHandler("SLOC", HandleNotification, SharedStartingLocationNotification),
                new NotificationHandler("LCKGME", HandleNotification, LockGameNotification),
                new IntNotificationHandler("NVRFY", HandleIntNotification, NotVerifiedNotification),
                new IntNotificationHandler("INGM", HandleIntNotification, StillInGameNotification),
                new StringCommandHandler(MAP_SHARING_UPLOAD_REQUEST, HandleMapUploadRequest),
                new StringCommandHandler(MAP_SHARING_FAIL_MESSAGE, HandleMapTransferFailMessage),
                new StringCommandHandler(MAP_SHARING_DOWNLOAD_REQUEST, HandleMapDownloadRequest),
                new NoParamCommandHandler(MAP_SHARING_DISABLED_MESSAGE, HandleMapSharingBlockedMessage),
                new NoParamCommandHandler("STRTD", GameStartedNotification),
                new NoParamCommandHandler("RETURN", ReturnNotification),
                new IntCommandHandler("TNLPNG", HandleTunnelPing),
                new StringCommandHandler("FHSH", FileHashNotification),
                new StringCommandHandler("MM", CheaterNotification),
                new StringCommandHandler(DICE_ROLL_MESSAGE, HandleDiceRollResult),
                new NoParamCommandHandler(CHEAT_DETECTED_MESSAGE, HandleCheatDetectedMessage),
                new StringCommandHandler(CHANGE_TUNNEL_SERVER_MESSAGE, HandleTunnelServerChangeMessage),
                new StringCommandHandler(PLAYER_TUNNEL_MESSAGE, HandlePlayerTunnelMessage),
                new StringCommandHandler(NEGOTIATION_INFO_MESSAGE, HandleNegotiationInfoMessage),
                new StringCommandHandler(TUNNEL_RENEGOTIATE_MESSAGE, HandleTunnelRenegotiateMessage),
                new StringCommandHandler(TUNNEL_FAILED_MESSAGE, HandleTunnelFailedMessage)
            };

            MapSharer.MapDownloadFailed += MapSharer_MapDownloadFailed;
            MapSharer.MapDownloadComplete += MapSharer_MapDownloadComplete;
            MapSharer.MapUploadFailed += MapSharer_MapUploadFailed;
            MapSharer.MapUploadComplete += MapSharer_MapUploadComplete;

            AddChatBoxCommand(new ChatBoxCommand("TUNNELINFO",
                "View tunnel server information".L10N("Client:Main:TunnelInfoCommand"), false, PrintTunnelServerInformation));
            AddChatBoxCommand(new ChatBoxCommand("CHANGETUNNEL",
                "Change the used CnCNet tunnel server (game host only)".L10N("Client:Main:ChangeTunnelCommand"),
                true, (s) => ShowTunnelSelectionWindow("Select tunnel server:".L10N("Client:Main:SelectTunnelServerCommand"))));
            AddChatBoxCommand(new ChatBoxCommand("DOWNLOADMAP",
                "Download a map from CNCNet's map server using a map ID and an optional filename.\nExample: \"/downloadmap MAPID [2] My Battle Map\"".L10N("Client:Main:DownloadMapCommandDescription"),
                false, DownloadMapByIdCommand));
            AddChatBoxCommand(new ChatBoxCommand("NEGSTATUS",
                "Toggle the tunnel negotiation status display",
                false, ToggleNegotiationStatus));
            AddChatBoxCommand(new ChatBoxCommand("TUNNELMODE",
                "Change tunnel mode (0=V3 static, 1=V3 dynamic, 2=V2 legacy) (game host only)".L10N("Client:Main:TunnelModeCommand"),
                true, HandleTunnelModeCommand));
        }

        public event EventHandler GameLeft;

        private TunnelHandler tunnelHandler;
        private TunnelSelectionWindow tunnelSelectionWindow;
        private XNAClientButton btnChangeTunnel;

        private Channel channel;
        private CnCNetManager connectionManager;
        private string localGame;

        private readonly GameHostInactiveChecker gameHostInactiveChecker;

        private GameCollection gameCollection;
        private CnCNetUserData cncnetUserData;
        private readonly PrivateMessagingWindow pmWindow;
        private GlobalContextMenu globalContextMenu;

        private string hostName;

        private CommandHandlerBase[] ctcpCommandHandlers;

        private IRCColor chatColor;

        private XNATimerControl gameBroadcastTimer;

        private int playerLimit;

        private bool closed = false;

        private int skillLevel = ClientConfiguration.Instance.DefaultSkillLevelIndex;

        private bool isCustomPassword = false;

        private string gameFilesHash;

        private List<string> hostUploadedMaps = new List<string>();
        private List<string> chatCommandDownloadedMaps = new List<string>();

        private MapSharingConfirmationPanel mapSharingConfirmationPanel;

        private Random random;

        private readonly List<V3PlayerInfo> _v3PlayerInfos = new();
        private bool _useLegacyTunnels;
        private bool _useDynamicTunnels;
        private readonly NegotiationDataManager _negotiationData = new();
        private TunnelNegotiationStatusPanel _negotiationStatusPanel;
        private const int TUNNEL_MODE_V3_STATIC = 0;  // V3 tunnels, host-selected
        private const int TUNNEL_MODE_V3_DYNAMIC = 1; // V3 tunnels, dynamic negotiation
        private const int TUNNEL_MODE_V2_LEGACY = 2;  // V2 tunnels, host-selected

        /// <summary>
        /// The SHA1 of the latest selected map.
        /// Used for map sharing.
        /// </summary>
        private string lastMapSHA1;

        /// <summary>
        /// The map name of the latest selected map.
        /// Used for map sharing.
        /// </summary>
        private string lastMapName;

        /// <summary>
        /// The game mode of the latest selected map.
        /// Used for map sharing.
        /// </summary>
        private string lastGameMode;

        /// <summary>
        /// Set to true if host has selected invalid tunnel server.
        /// </summary>
        private bool tunnelErrorMode;

        public override void Initialize()
        {
            IniNameOverride = nameof(CnCNetGameLobby);
            base.Initialize();

            if (gameHostInactiveChecker != null)
            {
                MouseMove += (sender, args) => gameHostInactiveChecker.Reset();
                gameHostInactiveChecker.CloseEvent += GameHostInactiveChecker_CloseEvent;
            }

            btnChangeTunnel = FindChild<XNAClientButton>(nameof(btnChangeTunnel));
            btnChangeTunnel.LeftClick += BtnChangeTunnel_LeftClick;

            gameBroadcastTimer = new XNATimerControl(WindowManager);
            gameBroadcastTimer.AutoReset = true;
            gameBroadcastTimer.Interval = TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL);
            gameBroadcastTimer.Enabled = false;
            gameBroadcastTimer.TimeElapsed += GameBroadcastTimer_TimeElapsed;

            tunnelSelectionWindow = new TunnelSelectionWindow(WindowManager, tunnelHandler);
            tunnelSelectionWindow.Initialize();
            tunnelSelectionWindow.DrawOrder = 1;
            tunnelSelectionWindow.UpdateOrder = 1;
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, tunnelSelectionWindow);
            tunnelSelectionWindow.CenterOnParent();
            tunnelSelectionWindow.Disable();
            tunnelSelectionWindow.TunnelSelected += TunnelSelectionWindow_TunnelSelected;

            MapLoader.MapChanged += MapLoader_MapChanged;
            mapSharingConfirmationPanel = new MapSharingConfirmationPanel(WindowManager);
            MapPreviewBox.AddChild(mapSharingConfirmationPanel);
            mapSharingConfirmationPanel.MapDownloadConfirmed += MapSharingConfirmationPanel_MapDownloadConfirmed;

            WindowManager.AddAndInitializeControl(gameBroadcastTimer);

            globalContextMenu = new GlobalContextMenu(WindowManager, connectionManager, cncnetUserData, pmWindow);
            AddChild(globalContextMenu);

            MultiplayerNameRightClicked += MultiplayerName_RightClick;

            _negotiationStatusPanel = new TunnelNegotiationStatusPanel(WindowManager);
            _negotiationStatusPanel.Name = nameof(_negotiationStatusPanel);
            _negotiationStatusPanel.X = Width - _negotiationStatusPanel.Width - 10;
            _negotiationStatusPanel.Y = MapPreviewBox.Y;
            AddChild(_negotiationStatusPanel);

            PostInitialize();
        }

        private void UpdateNegotiationUI()
        {
            if (!_useDynamicTunnels || !_negotiationStatusPanel.Enabled)
            {
                _negotiationStatusPanel.Disable();
                return;
            }

            var playerNames = Players.Select(p => p.Name).ToList();
            _negotiationStatusPanel.UpdateNegotiationStatus(playerNames, _negotiationData);

            if (IsHost)
            {
                var summary = _negotiationData.GetStatusSummary(playerNames);
                Logger.Log($"Negotiation Status: {summary}");
            }
        }

        private void ToggleNegotiationStatus(string args)
        {
            if (!_useDynamicTunnels)
            {
                AddNotice("Negotiation status is only available when using dynamic tunnels.");
                return;
            }

            if (_negotiationStatusPanel.Enabled)
            {
                _negotiationStatusPanel.Disable();
            }
            else
            {
                _negotiationStatusPanel.Enable();
                UpdateNegotiationUI();
                AddNotice("Negotiation status panel shown.");
            }
        }

        private void MultiplayerName_RightClick(object sender, MultiplayerNameRightClickedEventArgs args)
        {
            globalContextMenu.Show(new GlobalContextMenuData()
            {
                PlayerName = args.PlayerName,
                PreventJoinGame = true
            }, GetCursorPoint());
        }

        private void BtnChangeTunnel_LeftClick(object sender, EventArgs e) => ShowTunnelSelectionWindow("Select tunnel server:".L10N("Client:Main:SelectTunnelServer"));

        private void GameBroadcastTimer_TimeElapsed(object sender, EventArgs e) => BroadcastGame();

        public void SetUp(Channel channel, bool isHost, int playerLimit,
            CnCNetTunnel tunnel, string hostName, bool isCustomPassword,
            int skillLevel)
        {
            this.channel = channel;
            channel.MessageAdded += Channel_MessageAdded;
            channel.CTCPReceived += Channel_CTCPReceived;
            channel.UserKicked += Channel_UserKicked;
            channel.UserQuitIRC += Channel_UserQuitIRC;
            channel.UserLeft += Channel_UserLeft;
            channel.UserAdded += Channel_UserAdded;
            channel.UserNameChanged += Channel_UserNameChanged;
            channel.UserListReceived += Channel_UserListReceived;

            this.hostName = hostName;
            this.playerLimit = playerLimit;
            this.isCustomPassword = isCustomPassword;
            this.skillLevel = skillLevel;

            RegenerateV3PlayerInfos();

            this._useDynamicTunnels = tunnel == null;
            this._useLegacyTunnels = tunnel?.Version == 2;

            if (isHost)
            {
                RandomSeed = random.Next();
                RefreshMapSelectionUI();
                StartInactiveCheck();
            }
            else
            {

                channel.ChannelModesChanged += Channel_ChannelModesChanged;
                AIPlayers.Clear();
            }

            if (!_useDynamicTunnels)
                tunnelHandler.CurrentTunnel = tunnel;

            tunnelHandler.TunnelFailed += TunnelHandler_TunnelFailed;
            tunnelHandler.CurrentTunnelPinged += TunnelHandler_CurrentTunnelPinged;
            connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
            connectionManager.Disconnected += ConnectionManager_Disconnected;

            Refresh(isHost);

            if (IsHost && !_useDynamicTunnels)
                btnChangeTunnel.Enable();
            else
                btnChangeTunnel.Disable();
        }

        private List<CnCNetTunnel> GetAvailableTunnelsForNegotiation()
        {
            return tunnelHandler.Tunnels
                .Where(t => t.Version == 3 &&
                    (UserINISettings.Instance.PingUnofficialCnCNetTunnels || t.Official || t.Recommended))
                .ToList();
        }

        private void OnPlayerNegotiationResult(object sender, TunnelChosenEventArgs e)
        {
            var v3PlayerInfo = _v3PlayerInfos.FirstOrDefault(p => p.Id == e.PlayerId);
            if (v3PlayerInfo == null) return;

            v3PlayerInfo.HasNegotiated = true;
            v3PlayerInfo.IsNegotiating = false;

            var playerInfo = Players.FirstOrDefault(p => p.Name == e.PlayerName);
            if (playerInfo != null)
            {
                if (e.ChosenTunnel != null)
                {
                    // Success
                    v3PlayerInfo.Tunnel = e.ChosenTunnel;


                    if (e.IsLocalDecision)
                    {
                        AddNotice($"Selected tunnel for {e.PlayerName}: {e.ChosenTunnel.Name} (Ping: {e.NegotiationPing}ms)");

                        _negotiationData.UpdatePing(ProgramConstants.PLAYERNAME, e.PlayerName, e.NegotiationPing);

                        playerInfo.Ping = e.NegotiationPing;
                        UpdatePlayerPingIndicator(playerInfo);
                    }
                    else
                    {
                        AddNotice($"Assigned to tunnel: {e.ChosenTunnel.Name} (Ping: {e.NegotiationPing}ms) from {e.PlayerName}");

                        _negotiationData.UpdatePing(e.PlayerName, ProgramConstants.PLAYERNAME, e.NegotiationPing);

                        playerInfo.Ping = e.NegotiationPing;
                        UpdatePlayerPingIndicator(playerInfo);
                    }

                    _negotiationData.UpdateStatus(ProgramConstants.PLAYERNAME, e.PlayerName, NegotiationStatus.Succeeded);
                    UpdateNegotiationUI();
                    BroadcastNegotiationInfo(e.PlayerName, NegotiationStatus.Succeeded, e.NegotiationPing);
                }
                else
                {
                    // Failure
                    string failureMessage = $"Failed to negotiate tunnel with {e.PlayerName}";
                    if (!string.IsNullOrEmpty(e.FailureReason))
                        failureMessage += $": {e.FailureReason}";
                    AddNotice(failureMessage, Color.Yellow);

                    _negotiationData.UpdateStatus(ProgramConstants.PLAYERNAME, e.PlayerName, NegotiationStatus.Failed);
                    UpdateNegotiationUI();

                    BroadcastNegotiationInfo(e.PlayerName, NegotiationStatus.Failed);
                }
            }

            if (v3PlayerInfo.Negotiator != null)
            {
                v3PlayerInfo.Negotiator.NegotiationResult -= OnPlayerNegotiationResult;
                v3PlayerInfo.Negotiator.NegotiationComplete -= OnPlayerNegotiationComplete;
                v3PlayerInfo.StopNegotiation();
            }
        }

        private void OnPlayerNegotiationComplete(object sender, EventArgs e)
        {
            var negotiator = (V3PlayerNegotiator)sender;
            var player = negotiator.RemotePlayer;
            if (player == null) return;

            if (!player.HasNegotiated)
            {
                player.HasNegotiated = true;
                player.IsNegotiating = false;
                BroadcastNegotiationInfo(player.Name, NegotiationStatus.Failed);
            }

            if (player.Negotiator != null)
            {
                player.Negotiator.NegotiationResult -= OnPlayerNegotiationResult;
                player.Negotiator.NegotiationComplete -= OnPlayerNegotiationComplete;
            }
        }

        private void TunnelHandler_CurrentTunnelPinged(object sender, EventArgs e) => UpdatePing();

        private void TunnelHandler_TunnelFailed(object sender, CnCNetTunnel failedTunnel)
        {
            if (tunnelHandler.GameTunnelBridge.IsRunning)
                return;

            if (_useDynamicTunnels)
            {
                // Dynamic tunnels: find which remote players are using the failed tunnel with us
                var affectedPlayers = _v3PlayerInfos
                    .Where(p => p.Name != ProgramConstants.PLAYERNAME &&
                               p.Tunnel?.Address == failedTunnel.Address &&
                               p.Tunnel?.Port == failedTunnel.Port)
                    .ToList();

                if (affectedPlayers.Count > 0)
                {
                    AddNotice($"Tunnel {failedTunnel.Name} failed. Starting renegotiation with affected players...", Color.Orange);

                    // Broadcast to other players that we need to renegotiate, include tunnel info
                    channel.SendCTCPMessage($"{TUNNEL_RENEGOTIATE_MESSAGE} {failedTunnel.Address}:{failedTunnel.Port}",
                        QueuedMessageType.SYSTEM_MESSAGE, 10);

                    // Restart negotiation only with the affected player pairs
                    RestartNegotiations(affectedPlayers);
                }
            }
            else
            {
                // Non-dynamic tunnels: host picks a single tunnel for everyone
                if (IsHost)
                {
                    AddNotice($"Tunnel {failedTunnel.Name} failed. Selecting a new tunnel...", Color.Orange);
                    AutoSelectBestTunnel();
                }
                else
                {
                    // Non-host: broadcast tunnel failure so everyone (including host) is aware
                    AddNotice($"Tunnel {failedTunnel.Name} failed. Waiting for host to select a new tunnel...", Color.Orange);
                    channel.SendCTCPMessage($"{TUNNEL_FAILED_MESSAGE} {failedTunnel.Name}",
                        QueuedMessageType.SYSTEM_MESSAGE, 10);
                }
            }
        }

        private void RestartNegotiations(List<V3PlayerInfo> affectedPlayers)
        {
            foreach (var v3Player in affectedPlayers)
            {
                v3Player.Tunnel = null;
                v3Player.HasNegotiated = false;
                v3Player.IsNegotiating = false;

                _negotiationData.ClearPlayer(v3Player.Name);

                if (v3Player.Name != ProgramConstants.PLAYERNAME)
                    StartTunnelNegotiationForPlayer(v3Player);
            }

            UpdateNegotiationUI();
        }


        private void GameHostInactiveChecker_CloseEvent(object sender, EventArgs e) => LeaveGameLobby();

        public void StartInactiveCheck()
        {
            if (isCustomPassword)
                return;

            gameHostInactiveChecker?.Start();
        }

        public void StopInactiveCheck() => gameHostInactiveChecker?.Stop();

        public void OnJoined()
        {
            FileHashCalculator fhc = new FileHashCalculator();
            fhc.CalculateHashes();

            gameFilesHash = fhc.GetCompleteHash();

            if (IsHost)
            {
                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("MODE {0} +klnNs {1} {2}", channel.ChannelName,
                    channel.Password, playerLimit),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));

                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("TOPIC {0} :{1}", channel.ChannelName,
                    ProgramConstants.CNCNET_PROTOCOL_REVISION + ";" + localGame.ToLower()),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));

                gameBroadcastTimer.Enabled = true;
                gameBroadcastTimer.Start();
                gameBroadcastTimer.SetTime(TimeSpan.FromSeconds(INITIAL_GAME_BROADCAST_DELAY));
            }
            else
            {
                channel.SendCTCPMessage("FHSH " + gameFilesHash, QueuedMessageType.SYSTEM_MESSAGE, 10);
            }

            TopBar.AddPrimarySwitchable(this);
            TopBar.SwitchToPrimary();
            WindowManager.SelectedControl = tbChatInput;
            ResetAutoReadyCheckbox();
            UpdatePing();
            UpdateDiscordPresence(true);
        }

        private void UpdatePing()
        {
            if (tunnelHandler.CurrentTunnel == null)
                return;

            channel.SendCTCPMessage("TNLPNG " + tunnelHandler.CurrentTunnel.PingInMs, QueuedMessageType.SYSTEM_MESSAGE, 10);

            PlayerInfo pInfo = Players.Find(p => p.Name.Equals(ProgramConstants.PLAYERNAME));
            if (pInfo != null)
            {
                pInfo.Ping = tunnelHandler.CurrentTunnel.PingInMs;
                UpdatePlayerPingIndicator(pInfo);
            }
        }

        protected override void CopyPlayerDataToUI()
        {
            base.CopyPlayerDataToUI();

            for (int i = AIPlayers.Count + Players.Count; i < MAX_PLAYER_COUNT; i++)
            {
                StatusIndicators[i].SwitchTexture(
                    i < playerLimit ? PlayerSlotState.Empty : PlayerSlotState.Unavailable);
            }
        }

        private void PrintTunnelServerInformation(string s)
        {
            if (tunnelHandler.CurrentTunnel == null)
            {
                AddNotice("Tunnel server unavailable!".L10N("Client:Main:TunnelUnavailable"));
            }
            else
            {
                AddNotice(string.Format("Current tunnel server: {0} {1} (Players: {2}/{3}) (Official: {4}) Version: {5}".L10N("Client:Main:TunnelInfo"),
                        tunnelHandler.CurrentTunnel.Name, tunnelHandler.CurrentTunnel.Country, tunnelHandler.CurrentTunnel.Clients, tunnelHandler.CurrentTunnel.MaxClients, tunnelHandler.CurrentTunnel.Official, tunnelHandler.CurrentTunnel.Version.ToString()
                    ));
            }
        }

        private void ShowTunnelSelectionWindow(string description)
        {
            if (_useDynamicTunnels)
            {
                AddNotice("Cannot manually select tunnel when using dynamic tunnels.", Color.Yellow);
                return;
            }

            tunnelSelectionWindow.Open(description,
                tunnelHandler.CurrentTunnel?.Address,
                _useLegacyTunnels ? 2 : 3);
        }

        private void TunnelSelectionWindow_TunnelSelected(object sender, TunnelEventArgs e)
        {
            channel.SendCTCPMessage($"{CHANGE_TUNNEL_SERVER_MESSAGE} {e.Tunnel.Address}:{e.Tunnel.Port}",
                QueuedMessageType.SYSTEM_MESSAGE, 10);
            HandleTunnelServerChange(e.Tunnel);
        }

        public void ChangeChatColor(IRCColor chatColor)
        {
            this.chatColor = chatColor;
            tbChatInput.TextColor = chatColor.XnaColor;
        }

        public override void Clear()
        {
            base.Clear();

            foreach (var v3Player in _v3PlayerInfos)
            {
                if (v3Player.Negotiator != null)
                {
                    v3Player.Negotiator.NegotiationResult -= OnPlayerNegotiationResult;
                    v3Player.Negotiator.NegotiationComplete -= OnPlayerNegotiationComplete;
                }
                v3Player.StopNegotiation();
            }

            _negotiationData.ClearAll();

            _negotiationStatusPanel?.Disable();

            _v3PlayerInfos.Clear();

            if (channel != null)
            {
                channel.MessageAdded -= Channel_MessageAdded;
                channel.CTCPReceived -= Channel_CTCPReceived;
                channel.UserKicked -= Channel_UserKicked;
                channel.UserQuitIRC -= Channel_UserQuitIRC;
                channel.UserLeft -= Channel_UserLeft;
                channel.UserAdded -= Channel_UserAdded;
                channel.UserNameChanged -= Channel_UserNameChanged;
                channel.UserListReceived -= Channel_UserListReceived;

                if (!IsHost)
                {
                    channel.ChannelModesChanged -= Channel_ChannelModesChanged;
                }

                connectionManager.RemoveChannel(channel);
            }

            Disable();
            PlayerExtraOptionsPanel?.Disable();

            connectionManager.ConnectionLost -= ConnectionManager_ConnectionLost;
            connectionManager.Disconnected -= ConnectionManager_Disconnected;

            gameBroadcastTimer.Enabled = false;
            closed = false;

            tbChatInput.Text = string.Empty;

            tunnelHandler.CurrentTunnel = null;
            tunnelHandler.TunnelFailed -= TunnelHandler_TunnelFailed;
            tunnelHandler.CurrentTunnelPinged -= TunnelHandler_CurrentTunnelPinged;

            if (MapLoader != null)
                MapLoader.MapChanged -= MapLoader_MapChanged;

            GameLeft?.Invoke(this, EventArgs.Empty);

            TopBar.RemovePrimarySwitchable(this);
            ResetDiscordPresence();
        }

        public void LeaveGameLobby()
        {
            if (IsHost)
            {
                StopInactiveCheck();
                closed = true;
                BroadcastGame();
            }

            Clear();
            channel?.Leave();
        }

        private void ConnectionManager_Disconnected(object sender, EventArgs e) => HandleConnectionLoss();

        private void ConnectionManager_ConnectionLost(object sender, ConnectionLostEventArgs e) => HandleConnectionLoss();

        private void HandleConnectionLoss()
        {
            Clear();
            Disable();
        }

        private void Channel_UserNameChanged(object sender, UserNameChangedEventArgs e)
        {
            Logger.Log("CnCNetGameLobby: Nickname change: " + e.OldUserName + " to " + e.User.Name);
            int index = Players.FindIndex(p => p.Name == e.OldUserName);
            if (index > -1)
            {
                PlayerInfo player = Players[index];
                player.Name = e.User.Name;
                ddPlayerNames[index].Items[0].Text = player.Name;
                AddNotice(string.Format("Player {0} changed their name to {1}".L10N("Client:Main:PlayerRename"), e.OldUserName, e.User.Name));
            }
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e) => LeaveGameLobby();

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
                Map.UntranslatedName, GameMode.UntranslatedUIName, "Multiplayer",
                currentState, Players.Count, playerLimit, side,
                channel.UIName, IsHost, isCustomPassword, Locked, resetTimer);
        }

        private void Channel_UserQuitIRC(object sender, UserNameEventArgs e)
        {
            RemovePlayer(e.UserName);

            if (e.UserName == hostName)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "The game host abandoned the game.".L10N("Client:Main:HostAbandoned")));
                BtnLeaveGame_LeftClick(this, EventArgs.Empty);
            }
            else
                UpdateDiscordPresence();
        }

        private void Channel_UserLeft(object sender, UserNameEventArgs e)
        {
            RemovePlayer(e.UserName);

            if (e.UserName == hostName)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "The game host abandoned the game.".L10N("Client:Main:HostAbandoned")));
                BtnLeaveGame_LeftClick(this, EventArgs.Empty);
            }
            else
                UpdateDiscordPresence();
        }

        private void Channel_UserKicked(object sender, UserNameEventArgs e)
        {
            if (e.UserName == ProgramConstants.PLAYERNAME)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    ERROR_MESSAGE_COLOR, "You were kicked from the game!".L10N("Client:Main:YouWereKicked")));
                Clear();
                this.Visible = false;
                this.Enabled = false;
                return;
            }

            int index = Players.FindIndex(p => p.Name == e.UserName);

            if (index > -1)
            {
                Players.RemoveAt(index);
                CopyPlayerDataToUI();
                UpdateDiscordPresence();
                ClearReadyStatuses();
            }
        }

        private void Channel_UserListReceived(object sender, EventArgs e)
        {
            if (!IsHost)
            {
                if (channel.Users.Find(hostName) == null)
                {
                    connectionManager.MainChannel.AddMessage(new ChatMessage(
                        ERROR_MESSAGE_COLOR, "The game host has abandoned the game.".L10N("Client:Main:HostHasAbandoned")));
                    BtnLeaveGame_LeftClick(this, EventArgs.Empty);
                }
            }

            RegenerateV3PlayerInfos();

            UpdateDiscordPresence();
        }

        private void Channel_UserAdded(object sender, ChannelUserEventArgs e)
        {
            PlayerInfo pInfo = new PlayerInfo(e.User.IRCUser.Name);
            Players.Add(pInfo);

            if (Players.Count + AIPlayers.Count > MAX_PLAYER_COUNT && AIPlayers.Count > 0)
                AIPlayers.RemoveAt(AIPlayers.Count - 1);

            sndJoinSound.Play();
#if WINFORMS
            WindowManager.FlashWindow();
#endif

            RegenerateV3PlayerInfos();
            CopyPlayerDataToUI();

            if (IsHost)
            {
                if (e.User.IRCUser.Name != ProgramConstants.PLAYERNAME)
                {
                    // Changing the map applies forced settings (co-op sides etc.) to the
                    // new player, and it also sends an options broadcast message
                    ChangeMap(GameModeMap);
                    BroadcastPlayerOptions();
                    BroadcastPlayerExtraOptions();
                    UpdateDiscordPresence();
                }
                else
                {
                    Players[0].Ready = true;
                    CopyPlayerDataToUI();
                }

                if (Players.Count >= playerLimit)
                {
                    AddNotice("Player limit reached. The game room has been locked.".L10N("Client:Main:GameRoomNumberLimitReached"));
                    LockGame();
                }
            }

            if (pInfo.Name != ProgramConstants.PLAYERNAME && _useDynamicTunnels)
            {
                var newV3Player = _v3PlayerInfos.FirstOrDefault(p => p.Name == pInfo.Name);
                if (newV3Player != null)
                    StartTunnelNegotiationForPlayer(newV3Player);
            }
        }

        private void StartTunnelNegotiationForPlayer(V3PlayerInfo player)
        {
            if (!_useDynamicTunnels)
                return;

            if (player.Name == ProgramConstants.PLAYERNAME)
                return;

            var localV3Player = _v3PlayerInfos.FirstOrDefault(p => p.Name == ProgramConstants.PLAYERNAME);
            if (localV3Player == null)
                return;

            var availableTunnels = GetAvailableTunnelsForNegotiation();

            BroadcastNegotiationInfo(player.Name, NegotiationStatus.InProgress);

            try
            {
                AddNotice($"Negotiating tunnel with {player.Name}...");

                bool success = player.StartNegotiation(localV3Player, tunnelHandler, availableTunnels);

                if (success && player.Negotiator != null)
                {
                    player.Negotiator.NegotiationResult += OnPlayerNegotiationResult;
                    player.Negotiator.NegotiationComplete += OnPlayerNegotiationComplete;
                }

                if (!success)
                {
                    AddNotice($"Failed to negotiate tunnel with {player.Name}", Color.Yellow);
                    BroadcastNegotiationInfo(player.Name, NegotiationStatus.Failed);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error negotiating with player {player.Name}: {ex.Message}");
                AddNotice($"Error negotiating tunnel with {player.Name}", Color.Red);
                BroadcastNegotiationInfo(player.Name, NegotiationStatus.Failed);
            }
        }

        private void RemovePlayer(string playerName)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo != null)
            {
                var v3Player = _v3PlayerInfos.FirstOrDefault(p => p.Name == playerName);
                if (v3Player != null)
                {
                    _v3PlayerInfos.Remove(v3Player);

                    if (_useDynamicTunnels)
                    {
                        if (v3Player.Negotiator != null)
                        {
                            v3Player.Negotiator.NegotiationResult -= OnPlayerNegotiationResult;
                            v3Player.Negotiator.NegotiationComplete -= OnPlayerNegotiationComplete;
                        }
                        v3Player.StopNegotiation();
                    }
                }

                Players.Remove(pInfo);
                CopyPlayerDataToUI();

                if (IsHost)
                    BroadcastPlayerOptions();
            }

            sndLeaveSound.Play();

            if (IsHost && Locked && !ProgramConstants.IsInGame)
            {
                UnlockGame(true);
            }

            _negotiationData.ClearPlayer(playerName);

            UpdateNegotiationUI();

            if (Players.Count > 1 && _useDynamicTunnels)
                CheckAllNegotiationsComplete();
        }

        private void Channel_ChannelModesChanged(object sender, ChannelModeEventArgs e)
        {
            if (e.ModeString == "+i")
            {
                if (Players.Count >= playerLimit)
                    AddNotice("Player limit reached. The game room has been locked.".L10N("Client:Main:GameRoomNumberLimitReached"));
                else
                    AddNotice("The game host has locked the game room.".L10N("Client:Main:RoomLockedByHost"));
                Locked = true;
            }
            else if (e.ModeString == "-i")
            {
                AddNotice("The game room has been unlocked.".L10N("Client:Main:GameRoomUnlocked"));
                Locked = false;
            }
        }

        private void Channel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
        {
            Logger.Log("CnCNetGameLobby_CTCPReceived");

            foreach (CommandHandlerBase cmdHandler in ctcpCommandHandlers)
            {
                if (cmdHandler.Handle(e.UserName, e.Message))
                {
                    UpdateDiscordPresence();
                    return;
                }
            }

            Logger.Log("Unhandled CTCP command: " + e.Message + " from " + e.UserName);
        }

        private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            if (cncnetUserData.IsIgnored(e.Message.SenderIdent))
            {
                lbChatMessages.AddMessage(new ChatMessage(Color.Silver,
                    string.Format("Message blocked from {0}".L10N("Client:Main:MessageBlockedFromPlayer"), e.Message.SenderName)));
            }
            else
            {
                lbChatMessages.AddMessage(e.Message);

                if (e.Message.SenderName != null)
                    sndMessageSound.Play();
            }
        }

        /// <summary>
        /// Starts the game for the game host.
        /// </summary>
        protected override void HostLaunchGame()
        {
            if (_useDynamicTunnels && !AreAllNegotiationsSuccessful())
            {
                var (incomplete, failed) = _negotiationData.GetNegotiationStatusCounts(Players.Select(p => p.Name).ToList());

                if (failed > 0)
                {
                    AddNotice("Cannot start game: Some tunnel negotiations have failed.", Color.Red);
                    ShowFailedNegotiations();
                    return;
                }

                if (incomplete > 0)
                {
                    var incompleteNegotiations = _negotiationData.GetIncompleteNegotiations(Players.Select(p => p.Name).ToList());
                    AddNotice("Waiting for negotiations between:", Color.Yellow);
                    foreach (var (p1, p2, status) in incompleteNegotiations)
                        AddNotice($"  {p1} <-> {p2} ({status})", Color.Yellow);
                    return;
                }
            }

            if (Players.Count > 1)
            {
                // with V2 tunnels we get our ids from the tunnel server
                // V3 tunnels register on the fly
                if (tunnelHandler.CurrentTunnel?.Version == 2)
                {
                    AddNotice("Contacting V2 tunnel server...".L10N("Client:Main:ConnectingTunnelV2"));

                    List<int> playerPorts = tunnelHandler.CurrentTunnel.GetPlayerPortInfo(Players.Count);

                    if (playerPorts.Count < Players.Count)
                    {
                        ShowTunnelSelectionWindow(("An error occured while contacting " +
                            "the CnCNet tunnel server.\nTry picking a different tunnel server:").L10N("Client:Main:ConnectTunnelError1"));
                        AddNotice(("An error occured while contacting the specified CnCNet " +
                            "tunnel server. Please try using a different tunnel server").L10N("Client:Main:ConnectTunnelError2") + " ", ERROR_MESSAGE_COLOR);
                        return;
                    }

                    SendStartV2ToPlayers(playerPorts);
                }
                else if (_useDynamicTunnels || tunnelHandler.CurrentTunnel.Version == 3)
                {
                    SendStartV3ToPlayers();
                }
            }

            cncnetUserData.AddRecentPlayers(Players.Select(p => p.Name), channel.UIName);

            StartGame();
        }

        private void SendStartV2ToPlayers(List<int> playerPorts)
        {
            StringBuilder sb = new("STARTV2 ");
            sb.Append(UniqueGameID);
            for (int pId = 0; pId < Players.Count; pId++)
            {
                Players[pId].Port = playerPorts[pId];
                sb.Append(";");
                sb.Append(Players[pId].Name);
                sb.Append(";");
                sb.Append("0.0.0.0:");
                sb.Append(playerPorts[pId]);
            }
            channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 10);
        }

        private void SendStartV3ToPlayers()
        {
            string playerStartString = GenerateV3PlayerStartString();

            //STARTV3 345353;1234567891;Player1;[tunnelIP]:[tunnelPort];9876543210;Player2;[tunnelIP]:[tunnelPort]
            channel.SendCTCPMessage($"STARTV3 {UniqueGameID};{playerStartString}", QueuedMessageType.SYSTEM_MESSAGE, 10);
        }

        private string GenerateV3PlayerStartString()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < Players.Count; i++)
            {
                var playerData = PreparePlayerGameData(i);
                sb.Append(playerData.Id)
                  .Append(';')
                  .Append(playerData.Name)
                  .Append(';')
                  .Append(playerData.TunnelAddress)
                  .Append(';');
            }

            return sb.ToString().TrimEnd(';');
        }

        private (uint Id, string Name, string TunnelAddress) PreparePlayerGameData(int playerIndex)
        {
            var player = Players[playerIndex];
            uint id = GeneratePlayerID(player.Name);
            int port = 48000 - playerIndex;  //with v3, this is more like an ID for the game. Saved in spawn.ini and is the first 4 bytes of the game's packet data (sender+receiver)
            player.Port = port;

            var v3PlayerInfo = _v3PlayerInfos.FirstOrDefault(v3p => v3p.Name == player.Name);
            if (v3PlayerInfo != null)
            {
                v3PlayerInfo.Id = id;
                v3PlayerInfo.PlayerIndex = playerIndex;

                if (!_useLegacyTunnels && !_useDynamicTunnels)
                    v3PlayerInfo.Tunnel = tunnelHandler.CurrentTunnel;
                v3PlayerInfo.PlayerGameId = (ushort)port;
            }

            string address = v3PlayerInfo.Tunnel == null ? IPAddress.Any + ":0" : v3PlayerInfo.Tunnel.Address + ":" + v3PlayerInfo.Tunnel.Port;
            return (id, player.Name, address);
        }

        private uint GeneratePlayerID(string playerName)
        {
            //we need an ID for a player that all players can generate without communicating
            //(same id generated by all players).

            using var sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes($"{playerName}:{channel.ChannelName}"));
            return BitConverter.ToUInt32(hash, 0);
        }

        private void RegenerateV3PlayerInfos()
        {
            // Remove players who are no longer in the game
            _v3PlayerInfos.RemoveAll(v3p => !Players.Any(p => p.Name == v3p.Name));

            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                var v3Player = _v3PlayerInfos.FirstOrDefault(v3p => v3p.Name == player.Name);
                if (v3Player == null)
                {
                    // Add new players
                    _v3PlayerInfos.Add(new V3PlayerInfo(
                        GeneratePlayerID(player.Name),
                        player.Name,
                        i,
                        0 // PlayerGameId will be set at game start
                    ));
                }
                else
                {
                    // Update existing players
                    v3Player.PlayerIndex = i;
                }
            }
        }


        private bool AreAllNegotiationsSuccessful()
        {
            if (!_useDynamicTunnels || Players.Count <= 1)
                return true;

            return _negotiationData.AreAllNegotiationsSuccessful(Players.Select(p => p.Name).ToList());
        }


        private void ShowFailedNegotiations()
        {
            var failedPairs = _negotiationData.GetFailedPairs(Players.Select(p => p.Name).ToList());

            if (failedPairs.Count > 0)
            {
                AddNotice("Failed negotiations between:", Color.Red);
                foreach (var (p1, p2) in failedPairs)
                    AddNotice($" {p1} <-> {p2}", Color.Red);
                AddNotice("Consider changing tunnel mode or having affected players rejoin.", Color.Yellow);
            }
        }

        protected override void RequestPlayerOptions(int side, int color, int start, int team)
        {
            byte[] value = new byte[]
            {
                (byte)side,
                (byte)color,
                (byte)start,
                (byte)team
            };

            int intValue = BitConverter.ToInt32(value, 0);

            channel.SendCTCPMessage(
                string.Format("OR {0}", intValue),
                QueuedMessageType.GAME_SETTINGS_MESSAGE, 6);
        }

        protected override void RequestReadyStatus()
        {
            if (Map == null || GameMode == null)
            {
                AddNotice(("The game host needs to select a different map or " +
                    "you will be unable to participate in the match.").L10N("Client:Main:HostMustReplaceMap"));

                if (chkAutoReady.Checked)
                    channel.SendCTCPMessage("R 0", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);

                return;
            }

            PlayerInfo pInfo = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);
            if (pInfo == null)
                return;

            int readyState = 0;

            if (chkAutoReady.Checked)
                readyState = 2;
            else if (!pInfo.Ready)
                readyState = 1;

            channel.SendCTCPMessage($"R {readyState}", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);
        }

        protected override void AddNotice(string message, Color color) => channel.AddMessage(new ChatMessage(color, message));

        /// <summary>
        /// Handles player option requests received from non-host players.
        /// </summary>
        private void HandleOptionsRequest(string playerName, int options)
        {
            if (!IsHost)
                return;

            if (ProgramConstants.IsInGame)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo == null)
                return;

            byte[] bytes = BitConverter.GetBytes(options);

            int side = bytes[0];
            int color = bytes[1];
            int start = bytes[2];
            int team = bytes[3];

            if (side < 0 || side > SideCount + RandomSelectorCount)
                return;

            if (color < 0 || color > MPColors.Count)
                return;

            // Disallowed sides from client, maps, or game modes do not take random selectors into account
            // So, we need to insert "false" for each random at the beginning of this list AFTER getting them
            // from client, maps, or game modes.
            var randomDisallowedSides = new List<bool>(RandomSelectorCount);
            for (int i = 0; i < RandomSelectorCount; i++)
                randomDisallowedSides.Add(false);

            var disallowedSides = randomDisallowedSides.Concat(GetDisallowedSides()).ToArray();

            if (0 < side && side < SideCount && disallowedSides[side])
                return;

            if (Map?.CoopInfo != null)
            {
                if (Map.CoopInfo.DisallowedPlayerSides.Contains(side - 1) || side == SideCount + RandomSelectorCount)
                    return;

                if (Map.CoopInfo.DisallowedPlayerColors.Contains(color - 1))
                    return;
            }

            if (start < 0 || start > Map?.MaxPlayers)
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

        /// <summary>
        /// Handles "I'm ready" messages received from non-host players.
        /// </summary>
        private void HandleReadyRequest(string playerName, int readyStatus)
        {
            if (!IsHost)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo == null)
                return;

            pInfo.Ready = readyStatus > 0;
            pInfo.AutoReady = readyStatus > 1;

            CopyPlayerDataToUI();
            BroadcastPlayerOptions();
        }

        /// <summary>
        /// Broadcasts player options to non-host players.
        /// </summary>
        protected override void BroadcastPlayerOptions()
        {
            // Broadcast player options
            StringBuilder sb = new StringBuilder("PO ");
            foreach (PlayerInfo pInfo in Players.Concat(AIPlayers))
            {
                if (pInfo.IsAI)
                    sb.Append(pInfo.AILevel);
                else
                    sb.Append(pInfo.Name);
                sb.Append(";");

                // Combine the options into one integer to save bandwidth in
                // cases where the player uses default options (this is common for AI players)
                // Will hopefully make GameSurge kicking people a bit less common
                byte[] byteArray = new byte[]
                {
                    (byte)pInfo.TeamId,
                    (byte)pInfo.StartingLocation,
                    (byte)pInfo.ColorId,
                    (byte)pInfo.SideId,
                };

                int value = BitConverter.ToInt32(byteArray, 0);
                sb.Append(value);
                sb.Append(";");
                if (!pInfo.IsAI)
                {
                    if (pInfo.AutoReady && !pInfo.IsInGame && !LastMapChangeWasInvalid)
                        sb.Append(2);
                    else
                        sb.Append(Convert.ToInt32(pInfo.Ready));
                    sb.Append(';');
                }
            }

            channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAME_PLAYERS_MESSAGE, 11);
        }

        protected override void PlayerExtraOptions_OptionsChanged(object sender, EventArgs e)
        {
            base.PlayerExtraOptions_OptionsChanged(sender, e);
            BroadcastPlayerExtraOptions();
        }

        protected override void BroadcastPlayerExtraOptions()
        {
            if (!IsHost)
                return;

            var playerExtraOptions = GetPlayerExtraOptions();

            channel.SendCTCPMessage(playerExtraOptions.ToCncnetMessage(), QueuedMessageType.GAME_PLAYERS_EXTRA_MESSAGE, 11, true);
        }

        /// <summary>
        /// Handles player option messages received from the game host.
        /// </summary>
        private void ApplyPlayerOptions(string sender, string message)
        {
            if (sender != hostName)
                return;

            Players.Clear();
            AIPlayers.Clear();

            string[] parts = message.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length;)
            {
                PlayerInfo pInfo = new PlayerInfo();

                string pName = parts[i];
                int converted = Conversions.IntFromString(pName, -1);

                if (converted > -1)
                {
                    pInfo.IsAI = true;
                    pInfo.AILevel = converted;
                    pInfo.Name = AILevelToName(converted);
                }
                else
                {
                    pInfo.Name = pName;

                    // If we can't find the player from the channel user list,
                    // ignore the player
                    // They've either left the channel or got kicked before the
                    // player options message reached us
                    if (channel.Users.Find(pName) == null)
                    {
                        i += HUMAN_PLAYER_OPTIONS_LENGTH;
                        continue;
                    }
                }

                if (parts.Length <= i + 1)
                    return;

                int playerOptions = Conversions.IntFromString(parts[i + 1], -1);
                if (playerOptions == -1)
                    return;

                byte[] byteArray = BitConverter.GetBytes(playerOptions);

                int team = byteArray[0];
                int start = byteArray[1];
                int color = byteArray[2];
                int side = byteArray[3];

                if (side < 0 || side > SideCount + RandomSelectorCount)
                    return;

                if (color < 0 || color > MPColors.Count)
                    return;

                if (start < 0 || start > MAX_PLAYER_COUNT)
                    return;

                if (team < 0 || team > 4)
                    return;

                pInfo.TeamId = byteArray[0];
                pInfo.StartingLocation = byteArray[1];
                pInfo.ColorId = byteArray[2];
                pInfo.SideId = byteArray[3];

                if (pInfo.IsAI)
                {
                    pInfo.Ready = true;
                    AIPlayers.Add(pInfo);
                    i += AI_PLAYER_OPTIONS_LENGTH;
                }
                else
                {
                    if (parts.Length <= i + 2)
                        return;

                    int readyStatus = Conversions.IntFromString(parts[i + 2], -1);

                    if (readyStatus == -1)
                        return;

                    pInfo.Ready = readyStatus > 0;
                    pInfo.AutoReady = readyStatus > 1;

                    if (pInfo.Name == ProgramConstants.PLAYERNAME)
                        btnLaunchGame.Text = pInfo.Ready ? BTN_LAUNCH_NOT_READY : BTN_LAUNCH_READY;

                    Players.Add(pInfo);
                    i += HUMAN_PLAYER_OPTIONS_LENGTH;
                }
            }

            RegenerateV3PlayerInfos();

            CopyPlayerDataToUI();

            // When you join a lobby, you get existing player information here.
            // Start negotiating with players that we haven't already negotiated with or in the middle of negotiating
            if (_useDynamicTunnels && Players.Count > 1)
                foreach (var v3Player in _v3PlayerInfos.Where(p => p.Name != ProgramConstants.PLAYERNAME && p.HasNegotiated == false && p.IsNegotiating == false))
                    StartTunnelNegotiationForPlayer(v3Player);
        }

        private void HandleTunnelModeCommand(string args)
        {
            if (!IsHost)
            {
                AddNotice("Only the game host can change tunnel settings.", Color.Yellow);
                return;
            }

            int currentMode = _useDynamicTunnels ? TUNNEL_MODE_V3_DYNAMIC :
                             (_useLegacyTunnels ? TUNNEL_MODE_V2_LEGACY : TUNNEL_MODE_V3_STATIC);

            int newMode;
            if (string.IsNullOrWhiteSpace(args))
            {
                // Cycle through modes
                newMode = (currentMode + 1) % 3;
            }
            else
            {
                newMode = Conversions.IntFromString(args, currentMode);
                if (newMode < 0 || newMode > 2)
                {
                    AddNotice("Invalid tunnel mode. Use 0 (V3 static), 1 (V3 dynamic), or 2 (V2 legacy).", Color.Yellow);
                    return;
                }
            }

            HandleTunnelModeChange(newMode, true);
            OnGameOptionChanged();
            ClearReadyStatuses();
        }

        private void HandleNegotiationInfoMessage(string sender, string message)
        {
            string[] parts = message.Split(';');
            if (parts.Length < 2)
                return;

            string targetPlayer = parts[0];
            if (!Enum.TryParse<NegotiationStatus>(parts[1], out var status))
                return;

            _negotiationData.UpdateStatus(sender, targetPlayer, status);

            // Handle ping if present
            if (parts.Length >= 3 && int.TryParse(parts[2], out int ping) && ping >= 0)
            {
                _negotiationData.UpdatePing(sender, targetPlayer, ping);

                if (sender == ProgramConstants.PLAYERNAME)
                {
                    PlayerInfo pInfo = Players.Find(p => p.Name == targetPlayer);
                    if (pInfo != null)
                    {
                        pInfo.Ping = ping;
                        UpdatePlayerPingIndicator(pInfo);
                    }
                }
                else if (targetPlayer == ProgramConstants.PLAYERNAME)
                {
                    PlayerInfo pInfo = Players.Find(p => p.Name == sender);
                    if (pInfo != null)
                    {
                        pInfo.Ping = ping;
                        UpdatePlayerPingIndicator(pInfo);
                    }
                }
            }

            UpdateNegotiationUI();
            CheckAllNegotiationsComplete();
        }

        private void BroadcastNegotiationInfo(string targetPlayer, NegotiationStatus status, int ping = -1)
        {
            string message = ping >= 0
                ? $"{NEGOTIATION_INFO_MESSAGE} {targetPlayer};{status};{ping}"
                : $"{NEGOTIATION_INFO_MESSAGE} {targetPlayer};{status}";

            channel.SendCTCPMessage(message, QueuedMessageType.SYSTEM_MESSAGE, 10);
        }

        private void CheckAllNegotiationsComplete()
        {
            if (!IsHost || !_useDynamicTunnels)
                return;

            if (Players.Count <= 1)
                return;

            bool anyNegotiationStarted = false;
            bool allComplete = true;
            int totalNegotiations = 0;
            int completedNegotiations = 0;
            int failedNegotiations = 0;

            for (int i = 0; i < Players.Count; i++)
            {
                for (int j = i + 1; j < Players.Count; j++)
                {
                    var player1 = Players[i].Name;
                    var player2 = Players[j].Name;

                    totalNegotiations++;

                    var status = GetNegotiationStatus(player1, player2);

                    if (status != NegotiationStatus.NotStarted)
                        anyNegotiationStarted = true;

                    if (status == NegotiationStatus.Succeeded)
                        completedNegotiations++;
                    else if (status == NegotiationStatus.Failed)
                        failedNegotiations++;
                    else if (status == NegotiationStatus.InProgress || status == NegotiationStatus.NotStarted)
                        allComplete = false;
                }
            }

            if (allComplete && anyNegotiationStarted && totalNegotiations > 0)
            {
                if (failedNegotiations > 0)
                    AddNotice($"All tunnel negotiations complete. {completedNegotiations} succeeded, {failedNegotiations} failed.",
                        failedNegotiations > 0 ? Color.Yellow : Color.LightGreen);
                else
                    AddNotice($"All tunnel negotiations successfully completed!", Color.LightGreen);

                CheckHighPingPairs();
            }

            UpdateLaunchGameButtonStatus();
        }

        private NegotiationStatus GetNegotiationStatus(string player1, string player2)
        {
            return _negotiationData.GetNegotiationStatus(player1, player2);
        }

        private void CheckHighPingPairs()
        {
            const int HIGH_PING_THRESHOLD = 350; //ms
            var highPingPairs = new List<(string, string, int)>();

            var playerNames = Players.Select(p => p.Name).ToList();
            foreach (var (player1, player2) in _negotiationData.GetPlayerPairs(playerNames))
            {
                var ping = _negotiationData.GetPing(player1, player2);
                if (ping.HasValue && ping.Value > HIGH_PING_THRESHOLD)
                    highPingPairs.Add((player1, player2, ping.Value));
            }

            if (highPingPairs.Count > 0)
            {
                AddNotice("Warning: The following player pairs have high ping:", Color.Yellow);
                foreach (var (p1, p2, ping) in highPingPairs)
                    AddNotice($"  {p1} <-> {p2}: {ping}ms", Color.Yellow);
            }
        }

        /// <summary>
        /// Broadcasts game options to non-host players
        /// when the host has changed an option.
        /// </summary>
        protected override void OnGameOptionChanged()
        {
            base.OnGameOptionChanged();

            if (!IsHost)
                return;

            bool[] optionValues = new bool[CheckBoxes.Count];
            for (int i = 0; i < CheckBoxes.Count; i++)
                optionValues[i] = CheckBoxes[i].Checked;

            // Let's pack the booleans into bytes
            List<byte> byteList = Conversions.BoolArrayIntoBytes(optionValues).ToList();

            while (byteList.Count % 4 != 0)
                byteList.Add(0);

            int integerCount = byteList.Count / 4;
            byte[] byteArray = byteList.ToArray();

            ExtendedStringBuilder sb = new ExtendedStringBuilder("GO ", true, ';');

            for (int i = 0; i < integerCount; i++)
                sb.Append(BitConverter.ToInt32(byteArray, i * 4));

            // We don't gain much in most cases by packing the drop-down values
            // (because they're bytes to begin with, and usually non-zero),
            // so let's just transfer them as usual

            foreach (GameLobbyDropDown dd in DropDowns)
                sb.Append(dd.SelectedIndex);

            sb.Append(Convert.ToInt32(Map?.Official ?? false));
            sb.Append(Map?.SHA1 ?? string.Empty);
            sb.Append(GameMode?.Name ?? string.Empty);
            sb.Append(FrameSendRate);
            sb.Append(MaxAhead);
            sb.Append(ProtocolVersion);
            sb.Append(RandomSeed);
            sb.Append(Convert.ToInt32(RemoveStartingLocations));
            sb.Append(Map?.UntranslatedName ?? string.Empty);
            int tunnelMode = _useDynamicTunnels ? TUNNEL_MODE_V3_DYNAMIC :
                             (_useLegacyTunnels ? TUNNEL_MODE_V2_LEGACY : TUNNEL_MODE_V3_STATIC);
            sb.Append(tunnelMode);

            channel.SendCTCPMessage(sb.ToString(), QueuedMessageType.GAME_SETTINGS_MESSAGE, 11);
        }

        /// <summary>
        /// Handles game option messages received from the game host.
        /// </summary>
        private void ApplyGameOptions(string sender, string message)
        {
            if (sender != hostName)
                return;

            string[] parts = message.Split(';');

            int checkBoxIntegerCount = (CheckBoxes.Count / 32) + 1;

            int partIndex = checkBoxIntegerCount + DropDowns.Count;

            if (parts.Length < partIndex + 10)
            {
                AddNotice(("The game host has sent an invalid game options message! " +
                    "The game host's game version might be different from yours.").L10N("Client:Main:HostGameOptionInvalid"), Color.Red);
                return;
            }

            string mapOfficial = parts[partIndex];
            bool isMapOfficial = Conversions.BooleanFromString(mapOfficial, true);

            string mapSHA1 = parts[partIndex + 1];

            string gameMode = parts[partIndex + 2];

            int frameSendRate = Conversions.IntFromString(parts[partIndex + 3], FrameSendRate);
            if (frameSendRate != FrameSendRate)
            {
                FrameSendRate = frameSendRate;
                AddNotice(string.Format("The game host has changed FrameSendRate (order lag) to {0}".L10N("Client:Main:HostChangeFrameSendRate"), frameSendRate));
            }

            int maxAhead = Conversions.IntFromString(parts[partIndex + 4], MaxAhead);
            if (maxAhead != MaxAhead)
            {
                MaxAhead = maxAhead;
                AddNotice(string.Format("The game host has changed MaxAhead to {0}".L10N("Client:Main:HostChangeMaxAhead"), maxAhead));
            }

            int protocolVersion = Conversions.IntFromString(parts[partIndex + 5], ProtocolVersion);
            if (protocolVersion != ProtocolVersion)
            {
                ProtocolVersion = protocolVersion;
                AddNotice(string.Format("The game host has changed ProtocolVersion to {0}".L10N("Client:Main:HostChangeProtocolVersion"), protocolVersion));
            }

            string mapName = parts[partIndex + 8];
            GameModeMap currentGameModeMap = GameModeMap;

            lastGameMode = gameMode;
            lastMapSHA1 = mapSHA1;
            lastMapName = mapName;

            GameModeMap = GameModeMaps.Find(gmm => gmm.GameMode.Name == gameMode && gmm.Map.SHA1 == mapSHA1);
            if (GameModeMap == null)
            {
                ChangeMap(null);

                if (!string.IsNullOrEmpty(mapSHA1))
                {
                    if (!isMapOfficial)
                        RequestMap(mapSHA1);
                    else
                        ShowOfficialMapMissingMessage(mapSHA1);
                }
            }
            else if (GameModeMap != currentGameModeMap)
            {
                ChangeMap(GameModeMap);
            }

            // By changing the game options after changing the map, we know which
            // game options were changed by the map and which were changed by the game host

            // If the map doesn't exist on the local installation, it's impossible
            // to know which options were set by the host and which were set by the
            // map, so we'll just assume that the host has set all the options.
            // Very few (if any) custom maps force options, so it'll be correct nearly always

            for (int i = 0; i < checkBoxIntegerCount; i++)
            {
                if (parts.Length <= i)
                    return;

                int checkBoxStatusInt;
                bool success = int.TryParse(parts[i], out checkBoxStatusInt);

                if (!success)
                {
                    AddNotice(("Failed to parse check box options sent by game host!" +
                        "The game host's game version might be different from yours.").L10N("Client:Main:HostCheckBoxParseError"), Color.Red);
                    return;
                }

                byte[] byteArray = BitConverter.GetBytes(checkBoxStatusInt);
                bool[] boolArray = Conversions.BytesIntoBoolArray(byteArray);

                for (int optionIndex = 0; optionIndex < boolArray.Length; optionIndex++)
                {
                    int gameOptionIndex = i * 32 + optionIndex;

                    if (gameOptionIndex >= CheckBoxes.Count)
                        break;

                    GameLobbyCheckBox checkBox = CheckBoxes[gameOptionIndex];

                    if (checkBox.Checked != boolArray[optionIndex])
                    {
                        if (boolArray[optionIndex])
                            AddNotice(string.Format("The game host has enabled {0}".L10N("Client:Main:HostEnableOption"), checkBox.Text));
                        else
                            AddNotice(string.Format("The game host has disabled {0}".L10N("Client:Main:HostDisableOption"), checkBox.Text));
                    }

                    CheckBoxes[gameOptionIndex].Checked = boolArray[optionIndex];
                }
            }

            for (int i = checkBoxIntegerCount; i < DropDowns.Count + checkBoxIntegerCount; i++)
            {
                if (parts.Length <= i)
                {
                    AddNotice(("The game host has sent an invalid game options message! " +
                    "The game host's game version might be different from yours.").L10N("Client:Main:HostGameOptionInvalid"), Color.Red);
                    return;
                }

                int ddSelectedIndex;
                bool success = int.TryParse(parts[i], out ddSelectedIndex);

                if (!success)
                {
                    AddNotice(("Failed to parse drop down options sent by game host (2)! " +
                        "The game host's game version might be different from yours.").L10N("Client:Main:HostDropDownParseError"), Color.Red);
                    return;
                }

                GameLobbyDropDown dd = DropDowns[i - checkBoxIntegerCount];

                if (ddSelectedIndex < -1 || ddSelectedIndex >= dd.Items.Count)
                    continue;

                if (dd.SelectedIndex != ddSelectedIndex)
                {
                    string ddName = dd.OptionName;
                    if (dd.OptionName == null)
                        ddName = dd.Name;

                    AddNotice(string.Format("The game host has set {0} to {1}".L10N("Client:Main:HostSetOption"), ddName, dd.Items[ddSelectedIndex].Text));
                }

                DropDowns[i - checkBoxIntegerCount].SelectedIndex = ddSelectedIndex;
            }

            int randomSeed;
            bool parseSuccess = int.TryParse(parts[partIndex + 6], out randomSeed);

            if (!parseSuccess)
            {
                AddNotice(("Failed to parse random seed from game options message! " +
                    "The game host's game version might be different from yours.").L10N("Client:Main:HostRandomSeedError"), Color.Red);
            }

            bool removeStartingLocations = Convert.ToBoolean(Conversions.IntFromString(parts[partIndex + 7],
                Convert.ToInt32(RemoveStartingLocations)));
            SetRandomStartingLocations(removeStartingLocations);

            RandomSeed = randomSeed;

            if (parts.Length > partIndex + 9)
            {
                int tunnelMode = Conversions.IntFromString(parts[partIndex + 9], TUNNEL_MODE_V3_STATIC);
                HandleTunnelModeChange(tunnelMode, false);
            }
        }

        private void HandleTunnelModeChange(int tunnelMode, bool isHostInitiated)
        {
            bool newUseDynamic = tunnelMode == TUNNEL_MODE_V3_DYNAMIC;
            bool newUseLegacy = tunnelMode == TUNNEL_MODE_V2_LEGACY;

            if (newUseDynamic == _useDynamicTunnels && newUseLegacy == _useLegacyTunnels)
                return;

            if (!newUseDynamic && _useDynamicTunnels)
            {
                foreach (var v3Player in _v3PlayerInfos)
                {
                    if (v3Player.Negotiator != null)
                    {
                        v3Player.Negotiator.NegotiationResult -= OnPlayerNegotiationResult;
                        v3Player.Negotiator.NegotiationComplete -= OnPlayerNegotiationComplete;
                    }
                    v3Player.StopNegotiation();
                }
            }

            _useDynamicTunnels = newUseDynamic;
            _useLegacyTunnels = newUseLegacy;

            string modeDescription = tunnelMode switch
            {
                TUNNEL_MODE_V3_DYNAMIC => "dynamic tunnels (V3)",
                TUNNEL_MODE_V2_LEGACY => "legacy tunnels (V2)",
                _ => "static tunnels (V3)"
            };

            AddNotice(isHostInitiated
                ? $"Tunnel mode changed to {modeDescription}."
                : $"The game host has changed tunnel mode to {modeDescription}.");

            if (IsHost)
            {
                if (newUseDynamic)
                {
                    btnChangeTunnel.Disable();
                    tunnelHandler.CurrentTunnel = null;
                }
                else
                {
                    btnChangeTunnel.Enable();
                    AutoSelectBestTunnel();
                }
            }
            else
            {
                btnChangeTunnel.Disable();
            }

            if (newUseDynamic)
            {
                foreach (PlayerInfo pInfo in Players)
                {
                    pInfo.Ping = -1;
                    UpdatePlayerPingIndicator(pInfo);
                }
                CopyPlayerDataToUI();

                foreach (var v3Player in _v3PlayerInfos)
                {
                    v3Player.Tunnel = null;
                    v3Player.HasNegotiated = false;
                    v3Player.IsNegotiating = false;
                }

                if (Players.Count > 1)
                {
                    foreach (var v3Player in _v3PlayerInfos.Where(p => p.Name != ProgramConstants.PLAYERNAME))
                        StartTunnelNegotiationForPlayer(v3Player);
                }
            }
            else
            {
                _negotiationData.ClearAll();
                _negotiationStatusPanel.Disable();
            }
        }

        private void RequestMap(string mapSHA1)
        {
            if (UserINISettings.Instance.EnableMapSharing)
            {
                AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("Client:Main:MapNotExist"));
                mapSharingConfirmationPanel.ShowForMapDownload();
            }
            else
            {
                AddNotice("The game host has selected a map that doesn't exist on your installation.".L10N("Client:Main:MapNotExist") + " " +
                    ("Because you've disabled map sharing, it cannot be transferred. The game host needs " +
                    "to change the map or you will be unable to participate in the match.").L10N("Client:Main:MapSharingDisabledNotice"));
                channel.SendCTCPMessage(MAP_SHARING_DISABLED_MESSAGE, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        private void ShowOfficialMapMissingMessage(string sha1)
        {
            AddNotice(("The game host has selected an official map that doesn't exist on your installation. " +
                "This could mean that the game host has modified game files, or is running a different game version. " +
                "They need to change the map or you will be unable to participate in the match.").L10N("Client:Main:OfficialMapNotExist"));
            channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + sha1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }

        private void MapSharingConfirmationPanel_MapDownloadConfirmed(object sender, EventArgs e)
        {
            Logger.Log("Map sharing confirmed.");
            AddNotice("Attempting to download map.".L10N("Client:Main:DownloadingMap"));
            mapSharingConfirmationPanel.SetDownloadingStatus();
            MapSharer.DownloadMap(lastMapSHA1, localGame, lastMapName);
        }

        protected override void ChangeMap(GameModeMap gameModeMap)
        {
            mapSharingConfirmationPanel.Disable();
            base.ChangeMap(gameModeMap);
        }

        protected override void HandleMapUpdated(Map updatedMap, string previousSHA1)
        {
            base.HandleMapUpdated(updatedMap, previousSHA1);

            // If the host's currently selected map was updated, broadcast the new map to other players
            if (IsHost && Map != null && Map.SHA1 == updatedMap.SHA1)
                OnGameOptionChanged();
        }

        /// <summary>
        /// Signals other players that the local player has returned from the game,
        /// and unlocks the game as well as generates a new random seed as the game host.
        /// </summary>
        protected override void GameProcessExited()
        {
            ResetGameState();
        }

        protected void GameStartAborted()
        {
            ResetGameState();
        }

        protected void ResetGameState()
        {
            base.GameProcessExited();

            tunnelHandler.StopGameBridge();

            channel.SendCTCPMessage("RETURN", QueuedMessageType.SYSTEM_MESSAGE, 20);
            ReturnNotification(ProgramConstants.PLAYERNAME);

            if (IsHost)
            {
                RandomSeed = random.Next();
                OnGameOptionChanged();
                ClearReadyStatuses();
                CopyPlayerDataToUI();
                BroadcastPlayerOptions();
                BroadcastPlayerExtraOptions();
                StartInactiveCheck();

                if (Players.Count < playerLimit)
                    UnlockGame(true);
            }
        }

        /// <summary>
        /// Handles the "STARTV2" (game start) command sent by the game host.
        /// </summary>
        private void NonHostLaunchGameV2(string sender, string message)
        {
            if (sender != hostName)
                return;

            if (Map == null)
            {
                GameStartAborted();
                return;
            }

            string[] parts = message.Split(';');

            if (parts.Length < 1)
                return;

            UniqueGameID = Conversions.IntFromString(parts[0], -1);
            if (UniqueGameID < 0)
                return;

            var recentPlayers = new List<string>();

            for (int i = 1; i < parts.Length; i += 2)
            {
                if (parts.Length <= i + 1)
                    return;

                string pName = parts[i];
                string[] ipAndPort = parts[i + 1].Split(':');

                if (ipAndPort.Length < 2)
                    return;

                int port;
                bool success = int.TryParse(ipAndPort[1], out port);

                if (!success)
                    return;

                PlayerInfo pInfo = Players.Find(p => p.Name == pName);

                if (pInfo == null)
                    return;

                pInfo.Port = port;
                recentPlayers.Add(pName);
            }
            cncnetUserData.AddRecentPlayers(recentPlayers, channel.UIName);

            StartGame();
        }

        /// <summary>
        /// Handles the "STARTV3" (game start) command sent by the game host.
        /// </summary>
        private void NonHostLaunchGameV3(string sender, string message)
        {
            if (sender != hostName)
                return;

            if (Map == null)
            {
                GameStartAborted();
                return;
            }

            string[] parts = message.Split(';');

            if (parts.Length != (Players.Count * 3) + 1)
                return;

            UniqueGameID = Conversions.IntFromString(parts[0], -1);
            if (UniqueGameID < 0)
                return;

            var recentPlayers = new List<string>();

            for (int i = 1; i < parts.Length; i += 3)
            {
                if (!uint.TryParse(parts[i], out uint id))
                    return;

                string pName = parts[i + 1];
                string[] ipAndPort = parts[i + 2].Split(':');

                if (ipAndPort.Length != 2 || !int.TryParse(ipAndPort[1], out int port))
                    return;

                PlayerInfo pInfo = Players.Find(p => p.Name == pName);
                if (pInfo == null)
                    return;

                pInfo.Port = 48000 - pInfo.Index;
                recentPlayers.Add(pName);

                V3PlayerInfo v3PlayerInfo = _v3PlayerInfos.Find(p => p.Name == pName);
                if (v3PlayerInfo != null)
                {
                    if (!_useDynamicTunnels) // host set tunnel
                    {
                        CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == ipAndPort[0] && t.Port == port);
                        v3PlayerInfo.Tunnel = tunnel;
                    }
                    v3PlayerInfo.PlayerIndex = pInfo.Index;
                    v3PlayerInfo.PlayerGameId = (ushort)pInfo.Port;
                    v3PlayerInfo.Id = id;
                }
            }

            cncnetUserData.AddRecentPlayers(recentPlayers, channel.UIName);
            StartGame();
        }

        protected override void StartGame()
        {
            AddNotice("Starting game...".L10N("Client:Main:StartingGame"));

            FileHashCalculator fhc = new FileHashCalculator();
            fhc.CalculateHashes();

            if (gameFilesHash != fhc.GetCompleteHash())
            {
                Logger.Log("Game files modified during client session!");
                channel.SendCTCPMessage(CHEAT_DETECTED_MESSAGE, QueuedMessageType.INSTANT_MESSAGE, 0);
                HandleCheatDetectedMessage(ProgramConstants.PLAYERNAME);
            }

            StopInactiveCheck();

            channel.SendCTCPMessage("STRTD", QueuedMessageType.SYSTEM_MESSAGE, 20);

            if (_useDynamicTunnels || tunnelHandler.CurrentTunnel.Version == 3)
            {
                PlayerInfo localPlayer = FindLocalPlayer();
                if (localPlayer == null)
                {
                    Logger.Log("Could not find local player.");
                    return;
                }

                V3PlayerInfo localV3Player = _v3PlayerInfos.FirstOrDefault(p => p.Name == ProgramConstants.PLAYERNAME);
                if (localV3Player == null)
                {
                    Logger.Log("Could not find local V3 player info.");
                    return;
                }

                tunnelHandler.StartGameBridge(
                    localV3Player.Id,
                    localV3Player.PlayerGameId,
                    _v3PlayerInfos);
            }

            base.StartGame();
        }

        protected override void WriteSpawnIniAdditions(IniFile iniFile)
        {
            base.WriteSpawnIniAdditions(iniFile);

            PlayerInfo localPlayer = FindLocalPlayer();
            if (localPlayer == null)
                return;

            if (_useLegacyTunnels == false)
            {
                // tell the game to connect to our bridge
                iniFile.SetStringValue("Tunnel", "Ip", IPAddress.Loopback.ToString());
                iniFile.SetIntValue("Tunnel", "Port", localPlayer.Port);
            }
            else if (tunnelHandler.CurrentTunnel.Version == 2)
            {
                iniFile.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
                iniFile.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);
            }

            iniFile.SetIntValue("Settings", "GameID", UniqueGameID);
            iniFile.SetBooleanValue("Settings", "Host", IsHost);
            iniFile.SetIntValue("Settings", "Port", localPlayer.Port);
        }
        protected override void SendChatMessage(string message) => channel.SendChatMessage(message, chatColor);

        #region Notifications

        private void HandleNotification(string sender, Action handler)
        {
            if (sender != hostName)
                return;

            handler();
        }

        private void HandleIntNotification(string sender, int parameter, Action<int> handler)
        {
            if (sender != hostName)
                return;

            handler(parameter);
        }

        protected override void GetReadyNotification()
        {
            base.GetReadyNotification();
#if WINFORMS
            WindowManager.FlashWindow();
#endif
            TopBar.SwitchToPrimary();

            if (IsHost)
                channel.SendCTCPMessage("GETREADY", QueuedMessageType.GAME_GET_READY_MESSAGE, 0);
        }

        protected override void AISpectatorsNotification()
        {
            base.AISpectatorsNotification();

            if (IsHost)
                channel.SendCTCPMessage("AISPECS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void InsufficientPlayersNotification()
        {
            base.InsufficientPlayersNotification();

            if (IsHost)
                channel.SendCTCPMessage("INSFSPLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void TooManyPlayersNotification()
        {
            base.TooManyPlayersNotification();

            if (IsHost)
                channel.SendCTCPMessage("TMPLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void SharedColorsNotification()
        {
            base.SharedColorsNotification();

            if (IsHost)
                channel.SendCTCPMessage("CLRS", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void SharedStartingLocationNotification()
        {
            base.SharedStartingLocationNotification();

            if (IsHost)
                channel.SendCTCPMessage("SLOC", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void LockGameNotification()
        {
            base.LockGameNotification();

            if (IsHost)
                channel.SendCTCPMessage("LCKGME", QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void NotVerifiedNotification(int playerIndex)
        {
            base.NotVerifiedNotification(playerIndex);

            if (IsHost)
                channel.SendCTCPMessage("NVRFY " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        protected override void StillInGameNotification(int playerIndex)
        {
            base.StillInGameNotification(playerIndex);

            if (IsHost)
                channel.SendCTCPMessage("INGM " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
        }

        private void GameStartedNotification(string sender)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo != null)
                pInfo.IsInGame = true;

            CopyPlayerDataToUI();
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

        private void HandleTunnelPing(string sender, int ping)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name.Equals(sender));
            if (pInfo != null)
            {
                pInfo.Ping = ping;
                UpdatePlayerPingIndicator(pInfo);
            }
        }

        private void FileHashNotification(string sender, string filesHash)
        {
            if (!IsHost)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == sender);

            if (pInfo != null)
                pInfo.HashReceived = true;
            CopyPlayerDataToUI();

            if (filesHash != gameFilesHash)
            {
                channel.SendCTCPMessage("MM " + sender, QueuedMessageType.GAME_CHEATER_MESSAGE, 10);
                CheaterNotification(ProgramConstants.PLAYERNAME, sender);
            }
        }

        private void CheaterNotification(string sender, string cheaterName)
        {
            if (sender != hostName)
                return;

            AddNotice(string.Format("Player {0} has different files compared to the game host. Either {0} or the game host could be cheating.".L10N("Client:Main:DifferentFileCheating"), cheaterName), Color.Red);
        }

        protected override void BroadcastDiceRoll(int dieSides, int[] results)
        {
            string resultString = string.Join(",", results);
            channel.SendCTCPMessage($"{DICE_ROLL_MESSAGE} {dieSides},{resultString}", QueuedMessageType.CHAT_MESSAGE, 0);
            PrintDiceRollResult(ProgramConstants.PLAYERNAME, dieSides, results);
        }

        #endregion

        protected override void HandleLockGameButtonClick()
        {
            if (!Locked)
            {
                AddNotice("You've locked the game room.".L10N("Client:Main:RoomLockedByYou"));
                LockGame();
            }
            else
            {
                if (Players.Count < playerLimit)
                {
                    AddNotice("You've unlocked the game room.".L10N("Client:Main:RoomUnockedByYou"));
                    UnlockGame(false);
                }
                else
                    AddNotice(string.Format(
                        "Cannot unlock game; the player limit ({0}) has been reached.".L10N("Client:Main:RoomCantUnlockAsLimit"), playerLimit));
            }
        }

        protected override void LockGame()
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} +i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));

            Locked = true;
            btnLockGame.Text = "Unlock Game".L10N("Client:Main:UnlockGame");
            AccelerateGameBroadcasting();
        }

        protected override void UnlockGame(bool announce)
        {
            connectionManager.SendCustomMessage(new QueuedMessage(
                string.Format("MODE {0} -i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));

            Locked = false;
            if (announce)
                AddNotice("The game room has been unlocked.".L10N("Client:Main:GameRoomUnlocked"));
            btnLockGame.Text = "Lock Game".L10N("Client:Main:LockGame");
            AccelerateGameBroadcasting();
        }

        protected override void KickPlayer(int playerIndex)
        {
            if (playerIndex >= Players.Count)
                return;

            var pInfo = Players[playerIndex];

            AddNotice(string.Format("Kicking {0} from the game...".L10N("Client:Main:KickPlayer"), pInfo.Name));
            channel.SendKickMessage(pInfo.Name, 8);
        }

        protected override void BanPlayer(int playerIndex)
        {
            if (playerIndex >= Players.Count)
                return;

            var pInfo = Players[playerIndex];

            var user = connectionManager.UserList.Find(u => u.Name == pInfo.Name);

            if (user != null)
            {
                AddNotice(string.Format("Banning and kicking {0} from the game...".L10N("Client:Main:BanAndKickPlayer"), pInfo.Name));
                channel.SendBanMessage(user.Hostname, 8);
                channel.SendKickMessage(user.Name, 8);
            }
        }

        private void HandleCheatDetectedMessage(string sender) =>
            AddNotice(string.Format("{0} has modified game files during the client session. They are likely attempting to cheat!".L10N("Client:Main:PlayerModifyFileCheat"), sender), Color.Red);

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
                tunnelErrorMode = true;
                AddNotice(("The game host has selected an invalid tunnel server! " +
                    "The game host needs to change the server or you will be unable " +
                    "to participate in the match.").L10N("Client:Main:HostInvalidTunnel"),
                    Color.Yellow);
                UpdateLaunchGameButtonStatus();
                return;
            }

            tunnelErrorMode = false;
            HandleTunnelServerChange(tunnel);
            UpdateLaunchGameButtonStatus();
        }

        private void HandlePlayerTunnelMessage(string sender, string tunnelAddressAndPort)
        {
            if (!_useDynamicTunnels)
                return;

            string[] split = tunnelAddressAndPort.Split(':');
            if (split.Length != 2)
                return;

            string tunnelAddress = split[0];
            if (!int.TryParse(split[1], out int tunnelPort))
                return;

            CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort);
            AddNotice($"{sender} is using tunnel: {tunnel.Name}");

            if (!IsHost)
                return;

            var v3PlayerInfo = _v3PlayerInfos.FirstOrDefault(p => p.Name == sender);
            if (v3PlayerInfo != null)
                v3PlayerInfo.Tunnel = tunnel;
        }

        private void HandleTunnelRenegotiateMessage(string sender, string tunnelAddressAndPort)
        {
            if (!_useDynamicTunnels)
                return;

            string[] split = tunnelAddressAndPort.Split(':');
            if (split.Length != 2)
                return;

            string tunnelAddress = split[0];
            if (!int.TryParse(split[1], out int tunnelPort))
                return;

            // Find the V3PlayerInfo for the sender
            var remoteV3Player = _v3PlayerInfos.FirstOrDefault(p => p.Name == sender);
            if (remoteV3Player == null)
                return;

            // Check if we're using the same tunnel with this player
            if (remoteV3Player.Tunnel?.Address == tunnelAddress &&
                remoteV3Player.Tunnel?.Port == tunnelPort)
            {
                AddNotice($"{sender} needs to renegotiate tunnel. Starting renegotiation...", Color.Orange);

                // Restart negotiation with this specific player
                RestartNegotiations(new List<V3PlayerInfo> { remoteV3Player });
            }
        }

        private void HandleTunnelFailedMessage(string sender, string tunnelName)
        {
            AddNotice($"{sender} can no longer connect to tunnel: {tunnelName}. The host needs to change the tunnel or the game won't start.", Color.Orange);
        }

        private void AutoSelectBestTunnel()
        {
            int targetVersion = _useLegacyTunnels ? 2 : 3;

            var bestTunnel = tunnelHandler.Tunnels
                .Where(t => t.PingInMs > 0
                    && (UserINISettings.Instance.PingUnofficialCnCNetTunnels || t.Official || t.Recommended)
                    && t.Version == targetVersion)
                .OrderBy(t => t.PingInMs)
                .FirstOrDefault();

            if (bestTunnel != null)
            {
                AddNotice($"Auto-selected tunnel: {bestTunnel.Name} (Ping: {bestTunnel.PingInMs}ms)");
                channel.SendCTCPMessage($"{CHANGE_TUNNEL_SERVER_MESSAGE} {bestTunnel.Address}:{bestTunnel.Port}",
                    QueuedMessageType.SYSTEM_MESSAGE, 10);
                HandleTunnelServerChange(bestTunnel);
            }
        }

        /// <summary>
        /// Changes the tunnel server used for the game.
        /// </summary>
        /// <param name="tunnel">The new tunnel server to use.</param>
        private void HandleTunnelServerChange(CnCNetTunnel tunnel)
        {
            tunnelHandler.CurrentTunnel = tunnel;
            AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("Client:Main:HostChangeTunnel"), tunnel.Name));

            foreach (PlayerInfo pInfo in Players)
            {
                pInfo.Ping = -1;
                UpdatePlayerPingIndicator(pInfo);
            }

            CopyPlayerDataToUI();
            UpdatePing();

            if (_useLegacyTunnels == false && _useDynamicTunnels == false)
            {
                foreach (var v3Player in _v3PlayerInfos)
                    v3Player.Tunnel = tunnel;
            }
        }

        protected override bool UpdateLaunchGameButtonStatus()
        {
            return base.UpdateLaunchGameButtonStatus();
        }

        #region CnCNet map sharing

        private void MapSharer_MapDownloadFailed(object sender, SHA1EventArgs e)
            => WindowManager.AddCallback(new Action<SHA1EventArgs>(MapSharer_HandleMapDownloadFailed), e);

        private void MapSharer_HandleMapDownloadFailed(SHA1EventArgs e)
        {
            // If the host has already uploaded the map, we shouldn't request them to re-upload it
            if (hostUploadedMaps.Contains(e.SHA1))
            {
                AddNotice("Download of the custom map failed. The host needs to change the map or you will be unable to participate in this match.".L10N("Client:Main:DownloadCustomMapFailed"));
                mapSharingConfirmationPanel.SetFailedStatus();

                channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
                return;
            }
            else if (chatCommandDownloadedMaps.Contains(e.SHA1))
            {
                // Notify the user that their chat command map download failed.
                // Do not notify other users with a CTCP message as this is irrelevant to them.
                AddNotice("Downloading map via chat command has failed. Check the map ID and try again.".L10N("Client:Main:DownloadMapCommandFailedGeneric"));
                mapSharingConfirmationPanel.SetFailedStatus();
                return;
            }

            AddNotice("Requesting the game host to upload the map to the CnCNet map database.".L10N("Client:Main:RequestHostUploadMapToDB"));

            channel.SendCTCPMessage(MAP_SHARING_UPLOAD_REQUEST + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }

        private void MapSharer_MapDownloadComplete(object sender, SHA1EventArgs e) =>
            WindowManager.AddCallback(new Action<SHA1EventArgs>(MapSharer_HandleMapDownloadComplete), e);

        private void MapSharer_HandleMapDownloadComplete(SHA1EventArgs e)
        {
            string mapFileName = MapSharer.GetMapFileName(e.SHA1, e.MapName);
            Logger.Log("Map " + mapFileName + " downloaded successfully.");

            // MapLoader_MapChanged will fire when it's processed.
        }

        private void MapLoader_MapChanged(object sender, MapChangedEventArgs e)
        {
            if (e.ChangeType != MapChangeType.Added)
                return;

            bool isFromChatCommand = chatCommandDownloadedMaps.Contains(e.Map.SHA1);
            bool isFromHostSharing = lastMapSHA1 == e.Map.SHA1 && !isFromChatCommand;

            if (!isFromChatCommand && !isFromHostSharing)
                return;

            AddNotice($"Map {e.Map.Name} loaded successfully.");

            GameModeMap = GameModeMaps.Find(gmm => gmm.Map.SHA1 == e.Map.SHA1);
            ChangeMap(GameModeMap);

            if (isFromChatCommand)
                chatCommandDownloadedMaps.Remove(e.Map.SHA1);
        }

        protected override void HandleMapAdded(Map addedMap)
        {
            bool isFromChatCommand = chatCommandDownloadedMaps.Contains(addedMap.SHA1);
            bool isFromHostSharing = lastMapSHA1 == addedMap.SHA1 && !isFromChatCommand;

            // If this is a map we downloaded, select it
            if (isFromChatCommand || isFromHostSharing)
            {
                AddNotice($"Map {addedMap.Name} loaded successfully.");

                RefreshGameModeFilter();

                GameModeMap gameModeMap = GameModeMaps.FirstOrDefault(gmm => gmm.Map.SHA1 == addedMap.SHA1);

                if (gameModeMap != null)
                {
                    // select game mode
                    int gameModeIndex = ddGameModeMapFilter.Items.FindIndex(item =>
                        (item.Tag as GameModeMapFilter)?.GetGameModeMaps().Any(gmm => gmm.GameMode.Name == gameModeMap.GameMode.Name) ?? false);

                    if (gameModeIndex >= 0)
                        ddGameModeMapFilter.SelectedIndex = gameModeIndex;

                    ListMaps();

                    // select map
                    for (int i = 0; i < lbGameModeMapList.ItemCount; i++)
                    {
                        var item = lbGameModeMapList.GetItem(1, i);
                        if ((item.Tag as GameModeMap)?.Map.SHA1 == addedMap.SHA1)
                        {
                            lbGameModeMapList.SelectedIndex = i;
                            break;
                        }
                    }

                    ChangeMap(gameModeMap);
                }

                if (isFromChatCommand)
                    chatCommandDownloadedMaps.Remove(addedMap.SHA1);
            }
            else
            {
                base.HandleMapAdded(addedMap);
            }
        }

        private void MapSharer_MapUploadFailed(object sender, MapEventArgs e) =>
            WindowManager.AddCallback(new Action<MapEventArgs>(MapSharer_HandleMapUploadFailed), e);

        private void MapSharer_HandleMapUploadFailed(MapEventArgs e)
        {
            Map map = e.Map;

            hostUploadedMaps.Add(map.SHA1);

            AddNotice(string.Format("Uploading map {0} to the CnCNet map database failed.".L10N("Client:Main:UpdateMapToDBFailed"), map.Name));
            if (map == Map)
            {
                AddNotice("You need to change the map or some players won't be able to participate in this match.".L10N("Client:Main:YouMustReplaceMap"));
                channel.SendCTCPMessage(MAP_SHARING_FAIL_MESSAGE + " " + map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        private void MapSharer_MapUploadComplete(object sender, MapEventArgs e) =>
            WindowManager.AddCallback(new Action<MapEventArgs>(MapSharer_HandleMapUploadComplete), e);

        private void MapSharer_HandleMapUploadComplete(MapEventArgs e)
        {
            hostUploadedMaps.Add(e.Map.SHA1);

            AddNotice(string.Format("Uploading map {0} to the CnCNet map database complete.".L10N("Client:Main:UpdateMapToDBSuccess"), e.Map.Name));
            if (e.Map == Map)
            {
                channel.SendCTCPMessage(MAP_SHARING_DOWNLOAD_REQUEST + " " + Map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            }
        }

        /// <summary>
        /// Handles a map upload request sent by a player.
        /// </summary>
        /// <param name="sender">The sender of the request.</param>
        /// <param name="mapSHA1">The SHA1 of the requested map.</param>
        private void HandleMapUploadRequest(string sender, string mapSHA1)
        {
            if (hostUploadedMaps.Contains(mapSHA1))
            {
                Logger.Log("HandleMapUploadRequest: Map " + mapSHA1 + " is already uploaded!");
                return;
            }

            Map map = null;

            foreach (GameMode gm in GameModeMaps.GameModes)
            {
                map = gm.Maps.Find(m => m.SHA1 == mapSHA1);

                if (map != null)
                    break;
            }

            if (map == null)
            {
                Logger.Log("Unknown map upload request from " + sender + ": " + mapSHA1);
                return;
            }

            if (map.Official)
            {
                Logger.Log("HandleMapUploadRequest: Map is official, so skip request");

                AddNotice(string.Format(("{0} doesn't have the map '{1}' on their local installation. " +
                    "The map needs to be changed or {0} is unable to participate in the match.").L10N("Client:Main:PlayerMissingMap"),
                    sender, map.Name));

                return;
            }

            if (!IsHost)
                return;

            AddNotice(string.Format(("{0} doesn't have the map '{1}' on their local installation. " +
                "Attempting to upload the map to the CnCNet map database.").L10N("Client:Main:UpdateMapToDBPrompt"),
                sender, map.Name));

            MapSharer.UploadMap(map, localGame);
        }

        /// <summary>
        /// Handles a map transfer failure message sent by either the player or the game host.
        /// </summary>
        private void HandleMapTransferFailMessage(string sender, string sha1)
        {
            if (sender == hostName)
            {
                AddNotice("The game host failed to upload the map to the CnCNet map database.".L10N("Client:Main:HostUpdateMapToDBFailed"));

                hostUploadedMaps.Add(sha1);

                if (lastMapSHA1 == sha1 && Map == null)
                {
                    AddNotice("The game host needs to change the map or you won't be able to participate in this match.".L10N("Client:Main:HostMustChangeMap"));
                }

                return;
            }

            if (lastMapSHA1 == sha1)
            {
                if (!IsHost)
                {
                    AddNotice(string.Format("{0} has failed to download the map from the CnCNet map database.".L10N("Client:Main:PlayerDownloadMapFailed") + " " +
                        "The host needs to change the map or {0} won't be able to participate in this match.".L10N("Client:Main:HostNeedChangeMapForPlayer"), sender));
                }
                else
                {
                    AddNotice(string.Format("{0} has failed to download the map from the CnCNet map database.".L10N("Client:Main:PlayerDownloadMapFailed") + " " +
                        "You need to change the map or {0} won't be able to participate in this match.".L10N("Client:Main:YouNeedChangeMapForPlayer"), sender));
                }
            }
        }

        private void HandleMapDownloadRequest(string sender, string sha1)
        {
            if (sender != hostName)
                return;

            hostUploadedMaps.Add(sha1);

            if (lastMapSHA1 == sha1 && Map == null)
            {
                Logger.Log("The game host has uploaded the map into the database. Re-attempting download...");
                MapSharer.DownloadMap(sha1, localGame, lastMapName);
            }
        }

        private void HandleMapSharingBlockedMessage(string sender)
        {
            AddNotice(string.Format(("The selected map doesn't exist on {0}'s installation, and they " +
                "have map sharing disabled in settings. The game host needs to change to a non-custom map or " +
                "they will be unable to participate in this match.").L10N("Client:Main:PlayerMissingMapDisabledSharing"), sender));
        }

        /// <summary>
        /// Download a map from CNCNet using a map hash ID.
        ///
        /// Users and testers can get map hash IDs from this URL template:
        ///
        /// - http://mapdb.cncnet.org/search.php?game=GAME_ID&search=MAP_NAME_SEARCH_STRING
        ///
        /// </summary>
        /// <param name="parameters">
        /// This is a string beginning with the sha1 hash map ID, and (optionally) the name to use as a local filename for the map file.
        /// Every character after the first space will be treated as part of the map name.
        ///
        /// "?" characters are removed from the sha1 due to weird copy and paste behavior from the map search endpoint.
        /// </param>
        private void DownloadMapByIdCommand(string parameters)
        {
            string sha1;
            string mapName;
            string message;

            // Make sure no spaces at the beginning or end of the string will mess up arg parsing.
            parameters = parameters.Trim();
            // Check if the parameter's contain spaces.
            // The presence of spaces indicates a user-specified map name.
            int firstSpaceIndex = parameters.IndexOf(' ');

            if (firstSpaceIndex == -1)
            {
                // The user did not supply a map name.
                sha1 = parameters;
                mapName = "user_chat_command_download";
            }
            else
            {
                // User supplied a map name.
                sha1 = parameters.Substring(0, firstSpaceIndex);
                mapName = parameters.Substring(firstSpaceIndex + 1);
                mapName = mapName.Trim();
            }

            // Remove erroneous "?". These sneak in when someone double-clicks a map ID and copies it from the cncnet search endpoint.
            // There is some weird whitespace that gets copied to chat as a "?" at the end of the hash. It's hard to spot, so just hold the user's hand.
            sha1 = sha1.Replace("?", "");

            // See if the user already has this map, with any filename, before attempting to download it.
            GameModeMap loadedMap = GameModeMaps.Find(gmm => gmm.Map.SHA1 == sha1);

            if (loadedMap != null)
            {
                message = String.Format(
                    "The map for ID \"{0}\" is already loaded from \"{1}.{2}\", delete the existing file before trying again.".L10N("Client:Main:DownloadMapCommandSha1AlreadyExists"),
                    sha1,
                    loadedMap.Map.BaseFilePath,
                    ClientConfiguration.Instance.MapFileExtension);
                AddNotice(message, Color.Yellow);
                Logger.Log(message);
                return;
            }

            // Replace any characters that are not safe for filenames.
            char replaceUnsafeCharactersWith = '-';
            // Use a hashset instead of an array for quick lookups in `invalidChars.Contains()`.
            HashSet<char> invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            string safeMapName = new String(mapName.Select(c => invalidChars.Contains(c) ? replaceUnsafeCharactersWith : c).ToArray());

            chatCommandDownloadedMaps.Add(sha1);

            message = String.Format("Attempting to download map via chat command: sha1={0}, mapName={1}".L10N("Client:Main:DownloadMapCommandStartingDownload"), sha1, mapName);
            Logger.Log(message);
            AddNotice(message);

            MapSharer.DownloadMap(sha1, localGame, safeMapName);
        }

        #endregion

        #region Game broadcasting logic

        /// <summary>
        /// Lowers the time until the next game broadcasting message.
        /// </summary>
        private void AccelerateGameBroadcasting() =>
            gameBroadcastTimer.Accelerate(TimeSpan.FromSeconds(GAME_BROADCAST_ACCELERATION));

        private void BroadcastGame()
        {
            Channel broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

            if (broadcastChannel == null)
                return;

            if (ProgramConstants.IsInGame && broadcastChannel.Users.Count > 500)
                return;

            StringBuilder sb = new StringBuilder("GAME ");
            sb.Append(ProgramConstants.CNCNET_PROTOCOL_REVISION);
            sb.Append(";");
            sb.Append(ProgramConstants.GAME_VERSION);
            sb.Append(";");
            sb.Append(playerLimit);
            sb.Append(";");
            sb.Append(channel.ChannelName);
            sb.Append(";");
            sb.Append(channel.UIName);
            sb.Append(";");
            if (Locked)
                sb.Append("1");
            else
                sb.Append("0");
            sb.Append(Convert.ToInt32(isCustomPassword));
            sb.Append(Convert.ToInt32(closed));
            sb.Append("0"); // IsLoadedGame
            sb.Append("0"); // IsLadder
            sb.Append(";");
            foreach (PlayerInfo pInfo in Players)
            {
                sb.Append(pInfo.Name);
                sb.Append(",");
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append(";");
            sb.Append(Map?.UntranslatedName ?? string.Empty);
            sb.Append(";");
            sb.Append(GameMode?.UntranslatedUIName ?? string.Empty);
            sb.Append(";");
            if (_useDynamicTunnels)
                sb.Append("[DYN]");
            else
                sb.Append(tunnelHandler.CurrentTunnel.Address + ":" + tunnelHandler.CurrentTunnel.Port);
            sb.Append(";");
            sb.Append(0); // LoadedGameId
            sb.Append(";");
            sb.Append(skillLevel); // SkillLevel
            sb.Append(";");
            sb.Append(Map?.SHA1);

            broadcastChannel.SendCTCPMessage(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 20);
        }

        #endregion

        public override string GetSwitchName() => "Game Lobby".L10N("Client:Main:GameLobby");
    }
}
