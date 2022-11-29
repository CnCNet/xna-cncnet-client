using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Extensions;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

internal sealed class CnCNetGameLobby : MultiplayerGameLobby
{
    private const int HUMAN_PLAYER_OPTIONS_LENGTH = 3;
    private const int AI_PLAYER_OPTIONS_LENGTH = 2;
    private const double GAME_BROADCAST_INTERVAL = 30.0;
    private const double GAME_BROADCAST_ACCELERATION = 10.0;
    private const double INITIAL_GAME_BROADCAST_DELAY = 10.0;
    private const double MAX_TIME_FOR_GAME_LAUNCH = 20.0;
    private const int PRIORITY_START_GAME = 10;
    private const int PINNED_DYNAMIC_TUNNELS = 10;

    private static readonly Color ERROR_MESSAGE_COLOR = Color.Yellow;

    private readonly TunnelHandler tunnelHandler;
    private readonly CnCNetManager connectionManager;
    private readonly string localGame;
    private readonly List<CommandHandlerBase> ctcpCommandHandlers;
    private readonly GameCollection gameCollection;
    private readonly CnCNetUserData cncnetUserData;
    private readonly PrivateMessagingWindow pmWindow;
    private readonly List<uint> tunnelPlayerIds = new();
    private readonly List<string> hostUploadedMaps = new();
    private readonly List<string> chatCommandDownloadedMaps = new();
    private readonly List<(string Name, CnCNetTunnel Tunnel)> playerTunnels = new();
    private readonly List<(string Sender, string TunnelPingsMessage)> tunnelPingsMessages = new();
    private readonly List<(List<string> Names, V3GameTunnelHandler Tunnel)> dynamicV3GameTunnelHandlers = new();

    private TunnelSelectionWindow tunnelSelectionWindow;
    private XNAClientButton btnChangeTunnel;
    private Channel channel;
    private GlobalContextMenu globalContextMenu;
    private string hostName;
    private IRCColor chatColor;
    private XNATimerControl gameBroadcastTimer;
    private XNATimerControl gameStartTimer;
    private int playerLimit;
    private bool closed;
    private bool isCustomPassword;
    private bool[] isPlayerConnectedToTunnel;
    private bool isStartingGame;
    private string gameFilesHash;
    private MapSharingConfirmationPanel mapSharingConfirmationPanel;
    private CnCNetTunnel initialTunnel;

    /// <summary>
    /// The SHA1 of the latest selected map.
    /// Used for map sharing.
    /// </summary>
    private string lastMapHash;

    /// <summary>
    /// The map name of the latest selected map.
    /// Used for map sharing.
    /// </summary>
    private string lastMapName;

    /// <summary>
    /// Set to true if host has selected invalid tunnel server.
    /// </summary>
    private bool tunnelErrorMode;

    private EventHandler<ChannelUserEventArgs> channel_UserAddedFunc;
    private EventHandler<UserNameEventArgs> channel_UserQuitIRCFunc;
    private EventHandler<UserNameEventArgs> channel_UserLeftFunc;
    private EventHandler<UserNameEventArgs> channel_UserKickedFunc;
    private EventHandler channel_UserListReceivedFunc;
    private EventHandler<ConnectionLostEventArgs> connectionManager_ConnectionLostFunc;
    private EventHandler connectionManager_DisconnectedFunc;
    private EventHandler tunnelHandler_CurrentTunnelFunc;
    private List<(int Ping, string Hash)> pinnedTunnels;
    private string pinnedTunnelPingsMessage;
    private bool dynamicTunnelsEnabled;

    public CnCNetGameLobby(
        WindowManager windowManager,
        TopBar topBar,
        CnCNetManager connectionManager,
        TunnelHandler tunnelHandler,
        GameCollection gameCollection,
        CnCNetUserData cncnetUserData,
        MapLoader mapLoader,
        DiscordHandler discordHandler,
        PrivateMessagingWindow pmWindow)
        : base(windowManager, "MultiplayerGameLobby", topBar, mapLoader, discordHandler)
    {
        this.connectionManager = connectionManager;
        localGame = ClientConfiguration.Instance.LocalGame;
        this.tunnelHandler = tunnelHandler;
        this.gameCollection = gameCollection;
        this.cncnetUserData = cncnetUserData;
        this.pmWindow = pmWindow;

        ctcpCommandHandlers = new()
        {
            new IntCommandHandler(CnCNetCommands.OPTIONS_REQUEST, (playerName, options) => HandleOptionsRequestAsync(playerName, options).HandleTask()),
            new IntCommandHandler(CnCNetCommands.READY_REQUEST, (playerName, options) => HandleReadyRequestAsync(playerName, options).HandleTask()),
            new StringCommandHandler(CnCNetCommands.PLAYER_OPTIONS, ApplyPlayerOptions),
            new StringCommandHandler(CnCNetCommands.PLAYER_EXTRA_OPTIONS, ApplyPlayerExtraOptions),
            new StringCommandHandler(CnCNetCommands.GAME_OPTIONS, (sender, message) => ApplyGameOptionsAsync(sender, message).HandleTask()),
            new StringCommandHandler(CnCNetCommands.GAME_START_V2, (sender, message) => NonHostLaunchGameAsync(sender, message).HandleTask()),
            new StringCommandHandler(CnCNetCommands.GAME_START_V3, HandleGameStartV3TunnelMessage),
            new NoParamCommandHandler(CnCNetCommands.TUNNEL_CONNECTION_OK, playerName => HandlePlayerConnectedToTunnelAsync(playerName).HandleTask()),
            new NoParamCommandHandler(CnCNetCommands.TUNNEL_CONNECTION_FAIL, HandleTunnelFail),
            new NotificationHandler(CnCNetCommands.AI_SPECTATORS, HandleNotification, () => AISpectatorsNotificationAsync().HandleTask()),
            new NotificationHandler(CnCNetCommands.GET_READY_LOBBY, HandleNotification, () => GetReadyNotificationAsync().HandleTask()),
            new NotificationHandler(CnCNetCommands.INSUFFICIENT_PLAYERS, HandleNotification, () => InsufficientPlayersNotificationAsync().HandleTask()),
            new NotificationHandler(CnCNetCommands.TOO_MANY_PLAYERS, HandleNotification, () => TooManyPlayersNotificationAsync().HandleTask()),
            new NotificationHandler(CnCNetCommands.SHARED_COLORS, HandleNotification, () => SharedColorsNotificationAsync().HandleTask()),
            new NotificationHandler(CnCNetCommands.SHARED_STARTING_LOCATIONS, HandleNotification, () => SharedStartingLocationNotificationAsync().HandleTask()),
            new NotificationHandler(CnCNetCommands.LOCK_GAME, HandleNotification, () => LockGameNotificationAsync().HandleTask()),
            new IntNotificationHandler(CnCNetCommands.NOT_VERIFIED, HandleIntNotification, playerIndex => NotVerifiedNotificationAsync(playerIndex).HandleTask()),
            new IntNotificationHandler(CnCNetCommands.STILL_IN_GAME, HandleIntNotification, playerIndex => StillInGameNotificationAsync(playerIndex).HandleTask()),
            new StringCommandHandler(CnCNetCommands.MAP_SHARING_UPLOAD, HandleMapUploadRequest),
            new StringCommandHandler(CnCNetCommands.MAP_SHARING_FAIL, HandleMapTransferFailMessage),
            new StringCommandHandler(CnCNetCommands.MAP_SHARING_DOWNLOAD, HandleMapDownloadRequest),
            new NoParamCommandHandler(CnCNetCommands.MAP_SHARING_DISABLED, HandleMapSharingBlockedMessage),
            new NoParamCommandHandler(CnCNetCommands.RETURN, ReturnNotification),
            new StringCommandHandler(CnCNetCommands.FILE_HASH, (sender, filesHash) => FileHashNotificationAsync(sender, filesHash).HandleTask()),
            new StringCommandHandler(CnCNetCommands.CHEATER, CheaterNotification),
            new StringCommandHandler(CnCNetCommands.DICE_ROLL, HandleDiceRollResult),
            new NoParamCommandHandler(CnCNetCommands.CHEAT_DETECTED, HandleCheatDetectedMessage),
            new IntCommandHandler(CnCNetCommands.TUNNEL_PING, HandleTunnelPing),
            new StringCommandHandler(CnCNetCommands.CHANGE_TUNNEL_SERVER, (sender, hash) => HandleTunnelServerChangeMessageAsync(sender, hash).HandleTask()),
            new StringCommandHandler(CnCNetCommands.PLAYER_TUNNEL_PINGS, HandleTunnelPingsMessage)
        };

        MapSharer.MapDownloadFailed += (_, e) => WindowManager.AddCallback(() => MapSharer_HandleMapDownloadFailedAsync(e).HandleTask());
        MapSharer.MapDownloadComplete += (_, e) => WindowManager.AddCallback(() => MapSharer_HandleMapDownloadCompleteAsync(e).HandleTask());
        MapSharer.MapUploadFailed += (_, e) => WindowManager.AddCallback(() => MapSharer_HandleMapUploadFailedAsync(e).HandleTask());
        MapSharer.MapUploadComplete += (_, e) => WindowManager.AddCallback(() => MapSharer_HandleMapUploadCompleteAsync(e).HandleTask());

        AddChatBoxCommand(new(
            CnCNetLobbyCommands.TUNNELINFO,
            "View tunnel server information".L10N("Client:Main:TunnelInfo"),
            false,
            PrintTunnelServerInformation));
        AddChatBoxCommand(new(
            CnCNetLobbyCommands.CHANGETUNNEL,
            "Change the used CnCNet tunnel server (game host only)".L10N("Client:Main:ChangeTunnel"),
            true,
            _ => ShowTunnelSelectionWindow("Select tunnel server:".L10N("Client:Main:SelectTunnelServer"))));
        AddChatBoxCommand(new(
            CnCNetLobbyCommands.DOWNLOADMAP,
            "Download a map from CNCNet's map server using a map ID and an optional filename.\nExample: \"/downloadmap MAPID [2] My Battle Map\"".L10N("Client:Main:DownloadMapCommandDescription"),
            false,
            DownloadMapByIdCommand));
        AddChatBoxCommand(new(
            CnCNetLobbyCommands.DYNAMICTUNNELS,
            "Toggle dynamic CnCNet tunnel servers on/off (game host only)".L10N("UI:Main:ChangeDynamicTunnels"),
            true,
            _ => ToggleDynamicTunnelsAsync().HandleTask()));
    }

    public event EventHandler GameLeft;

    public override void Initialize()
    {
        IniNameOverride = nameof(CnCNetGameLobby);

        base.Initialize();

        btnChangeTunnel = FindChild<XNAClientButton>(nameof(btnChangeTunnel));

        btnChangeTunnel.LeftClick += BtnChangeTunnel_LeftClick;

        gameBroadcastTimer = new(WindowManager)
        {
            AutoReset = true,
            Interval = TimeSpan.FromSeconds(GAME_BROADCAST_INTERVAL),
            Enabled = false
        };
        gameBroadcastTimer.TimeElapsed += (_, _) => BroadcastGameAsync().HandleTask();

        gameStartTimer = new(WindowManager)
        {
            AutoReset = false,
            Interval = TimeSpan.FromSeconds(MAX_TIME_FOR_GAME_LAUNCH)
        };
        gameStartTimer.TimeElapsed += GameStartTimer_TimeElapsed;

        tunnelSelectionWindow = new(WindowManager, tunnelHandler);

        tunnelSelectionWindow.Initialize();

        tunnelSelectionWindow.DrawOrder = 1;
        tunnelSelectionWindow.UpdateOrder = 1;

        DarkeningPanel.AddAndInitializeWithControl(WindowManager, tunnelSelectionWindow);
        tunnelSelectionWindow.CenterOnParent();
        tunnelSelectionWindow.Disable();

        tunnelSelectionWindow.TunnelSelected += (_, e) => TunnelSelectionWindow_TunnelSelectedAsync(e).HandleTask();

        mapSharingConfirmationPanel = new(WindowManager);

        MapPreviewBox.AddChild(mapSharingConfirmationPanel);

        mapSharingConfirmationPanel.MapDownloadConfirmed += MapSharingConfirmationPanel_MapDownloadConfirmed;

        WindowManager.AddAndInitializeControl(gameBroadcastTimer);

        globalContextMenu = new(WindowManager, connectionManager, cncnetUserData, pmWindow);

        AddChild(globalContextMenu);
        AddChild(gameStartTimer);

        MultiplayerNameRightClicked += MultiplayerName_RightClick;

        channel_UserAddedFunc = (_, e) => Channel_UserAddedAsync(e).HandleTask();
        channel_UserQuitIRCFunc = (_, e) => ChannelUserLeftAsync(e).HandleTask();
        channel_UserLeftFunc = (_, e) => ChannelUserLeftAsync(e).HandleTask();
        channel_UserKickedFunc = (_, e) => Channel_UserKickedAsync(e).HandleTask();
        channel_UserListReceivedFunc = (_, _) => Channel_UserListReceivedAsync().HandleTask();
        connectionManager_ConnectionLostFunc = (_, _) => HandleConnectionLossAsync().HandleTask();
        connectionManager_DisconnectedFunc = (_, _) => HandleConnectionLossAsync().HandleTask();
        tunnelHandler_CurrentTunnelFunc = (_, _) => UpdatePingAsync().HandleTask();

        PostInitialize();
    }

    private void GameStartTimer_TimeElapsed(object sender, EventArgs e)
    {
        string playerString = string.Empty;

        for (int i = 0; i < Players.Count; i++)
        {
            if (!isPlayerConnectedToTunnel[i])
            {
                if (playerString == string.Empty)
                    playerString = Players[i].Name;
                else
                    playerString += ", " + Players[i].Name;
            }
        }

        AddNotice($"Some players ({playerString}) failed to connect within the time limit. Aborting game launch.");
        AbortGameStart();
    }

    private void MultiplayerName_RightClick(object sender, MultiplayerNameRightClickedEventArgs args)
    {
        globalContextMenu.Show(
            new GlobalContextMenuData
            {
                PlayerName = args.PlayerName,
                PreventJoinGame = true
            },
            GetCursorPoint());
    }

    private void BtnChangeTunnel_LeftClick(object sender, EventArgs e) => ShowTunnelSelectionWindow("Select tunnel server:".L10N("Client:Main:SelectTunnelServer"));

    public async Task SetUpAsync(
        Channel channel,
        bool isHost,
        int playerLimit,
        CnCNetTunnel tunnel,
        string hostName,
        bool isCustomPassword)
    {
        this.channel = channel;
        this.hostName = hostName;
        this.playerLimit = playerLimit;
        this.isCustomPassword = isCustomPassword;
        dynamicTunnelsEnabled = UserINISettings.Instance.UseDynamicTunnels;
        channel.MessageAdded += Channel_MessageAdded;
        channel.CTCPReceived += Channel_CTCPReceived;
        channel.UserKicked += channel_UserKickedFunc;
        channel.UserQuitIRC += channel_UserQuitIRCFunc;
        channel.UserLeft += channel_UserLeftFunc;
        channel.UserAdded += channel_UserAddedFunc;
        channel.UserNameChanged += Channel_UserNameChanged;
        channel.UserListReceived += channel_UserListReceivedFunc;

        if (isHost)
        {
            RandomSeed = new Random().Next();

            await RefreshMapSelectionUIAsync();
            btnChangeTunnel.Enable();
        }
        else
        {
            channel.ChannelModesChanged += Channel_ChannelModesChanged;

            AIPlayers.Clear();
        }

        initialTunnel = tunnel;

        if (!dynamicTunnelsEnabled)
        {
            tunnelHandler.CurrentTunnel = initialTunnel;
        }
        else
        {
            tunnelHandler.CurrentTunnel = tunnelHandler.Tunnels
                .Where(q => q.PingInMs > -1 && !q.RequiresPassword && q.Clients < q.MaxClients - 8 && q.Version == Constants.TUNNEL_VERSION_3)
                .MinBy(q => q.PingInMs);
        }

        tunnelHandler.CurrentTunnelPinged += tunnelHandler_CurrentTunnelFunc;
        connectionManager.ConnectionLost += connectionManager_ConnectionLostFunc;
        connectionManager.Disconnected += connectionManager_DisconnectedFunc;

        Refresh(isHost);
    }

    public async Task OnJoinedAsync()
    {
        var fhc = new FileHashCalculator();

        fhc.CalculateHashes(GameModeMaps.GameModes);

        gameFilesHash = fhc.GetCompleteHash();
        pinnedTunnels = tunnelHandler.Tunnels
            .Where(q => !q.RequiresPassword && q.PingInMs > -1 && q.Clients < q.MaxClients - 8 && q.Version == Constants.TUNNEL_VERSION_3)
            .OrderBy(q => q.PingInMs)
            .ThenBy(q => q.Hash, StringComparer.OrdinalIgnoreCase)
            .Take(PINNED_DYNAMIC_TUNNELS)
            .Select(q => (q.PingInMs, q.Hash))
            .ToList();

        IEnumerable<string> tunnelPings = pinnedTunnels
            .Select(q => FormattableString.Invariant($"{q.Ping};{q.Hash}\t"));

        pinnedTunnelPingsMessage = string.Concat(tunnelPings);

        if (IsHost)
        {
            await connectionManager.SendCustomMessageAsync(new(
                FormattableString.Invariant($"{IRCCommands.MODE} {channel.ChannelName} +klnNs {channel.Password} {playerLimit}"),
                QueuedMessageType.SYSTEM_MESSAGE,
                50));

            await connectionManager.SendCustomMessageAsync(new(
                FormattableString.Invariant($"{IRCCommands.TOPIC} {channel.ChannelName} :{ProgramConstants.CNCNET_PROTOCOL_REVISION}:{localGame.ToLower()}"),
                QueuedMessageType.SYSTEM_MESSAGE,
                50));

            gameBroadcastTimer.Enabled = true;

            gameBroadcastTimer.Start();
            gameBroadcastTimer.SetTime(TimeSpan.FromSeconds(INITIAL_GAME_BROADCAST_DELAY));
        }
        else
        {
            await channel.SendCTCPMessageAsync(CnCNetCommands.FILE_HASH + " " + gameFilesHash, QueuedMessageType.SYSTEM_MESSAGE, 10);

            if (dynamicTunnelsEnabled)
                await BroadcastPlayerTunnelPingsAsync();

            if (UserINISettings.Instance.UseP2P)
            {
                // todo broadcast IPs so others can ping
                // await channel.SendCTCPMessageAsync(CnCNetCommands.PLAYER_P2P_REQUEST + " " + x, QueuedMessageType.SYSTEM_MESSAGE, 10);

                // todo ping other players, if both sides can ping each other, add p2p ping as extra result to tunnel ping list
                // await channel.SendCTCPMessageAsync(CnCNetCommands.PLAYER_P2P_PINGS + " " + x, QueuedMessageType.SYSTEM_MESSAGE, 10);
            }
        }

        TopBar.AddPrimarySwitchable(this);
        TopBar.SwitchToPrimary();
        WindowManager.SelectedControl = tbChatInput;
        ResetAutoReadyCheckbox();
        await UpdatePingAsync();
        UpdateDiscordPresence(true);
    }

    private async Task UpdatePingAsync()
    {
        int ping;

        if (dynamicTunnelsEnabled)
            ping = pinnedTunnels.Min(q => q.Ping);
        else if (tunnelHandler.CurrentTunnel == null)
            return;
        else
            ping = tunnelHandler.CurrentTunnel.PingInMs;

        await channel.SendCTCPMessageAsync(CnCNetCommands.TUNNEL_PING + " " + ping, QueuedMessageType.SYSTEM_MESSAGE, 10);

        PlayerInfo pInfo = FindLocalPlayer();

        if (pInfo != null)
        {
            pInfo.Ping = ping;

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
        if (dynamicTunnelsEnabled)
        {
            AddNotice("Dynamic tunnels enabled".L10N("Client:Main:DynamicTunnelsEnabled"));
        }
        else if (tunnelHandler.CurrentTunnel is null)
        {
            AddNotice("Tunnel server unavailable!".L10N("Client:Main:TunnelUnavailable"));
        }
        else
        {
            AddNotice(string.Format("Current tunnel server: {0} {1} (Players: {2}/{3}) (Official: {4})".L10N("Client:Main:TunnelInfo"),
                    tunnelHandler.CurrentTunnel.Name, tunnelHandler.CurrentTunnel.Country, tunnelHandler.CurrentTunnel.Clients, tunnelHandler.CurrentTunnel.MaxClients, tunnelHandler.CurrentTunnel.Official));
        }
    }

    private void ShowTunnelSelectionWindow(string description)
        => tunnelSelectionWindow.Open(description, tunnelHandler.CurrentTunnel);

    private async Task TunnelSelectionWindow_TunnelSelectedAsync(TunnelEventArgs e)
    {
        await channel.SendCTCPMessageAsync(
            $"{CnCNetCommands.CHANGE_TUNNEL_SERVER} {e.Tunnel.Hash}",
            QueuedMessageType.SYSTEM_MESSAGE,
            10);
        await HandleTunnelServerChangeAsync(e.Tunnel);
    }

    public void ChangeChatColor(IRCColor chatColor)
    {
        this.chatColor = chatColor;
        tbChatInput.TextColor = chatColor.XnaColor;
    }

    public override async Task ClearAsync()
    {
        await base.ClearAsync();

        if (channel != null)
        {
            channel.MessageAdded -= Channel_MessageAdded;
            channel.CTCPReceived -= Channel_CTCPReceived;
            channel.UserKicked -= channel_UserKickedFunc;
            channel.UserQuitIRC -= channel_UserQuitIRCFunc;
            channel.UserLeft -= channel_UserLeftFunc;
            channel.UserAdded -= channel_UserAddedFunc;
            channel.UserNameChanged -= Channel_UserNameChanged;
            channel.UserListReceived -= channel_UserListReceivedFunc;

            if (!IsHost)
                channel.ChannelModesChanged -= Channel_ChannelModesChanged;

            connectionManager.RemoveChannel(channel);
        }

        Disable();
        connectionManager.ConnectionLost -= connectionManager_ConnectionLostFunc;
        connectionManager.Disconnected -= connectionManager_DisconnectedFunc;

        gameBroadcastTimer.Enabled = false;
        closed = false;

        tbChatInput.Text = string.Empty;

        tunnelHandler.CurrentTunnel = null;
        tunnelHandler.CurrentTunnelPinged -= tunnelHandler_CurrentTunnelFunc;

        playerTunnels.Clear();
        tunnelPlayerIds.Clear();
        dynamicV3GameTunnelHandlers.Clear();
        pinnedTunnels.Clear();
        tunnelPingsMessages.Clear();
        pinnedTunnelPingsMessage = null;

        GameLeft?.Invoke(this, EventArgs.Empty);

        TopBar.RemovePrimarySwitchable(this);
        ResetDiscordPresence();
    }

    public async Task LeaveGameLobbyAsync()
    {
        if (IsHost)
        {
            closed = true;
            await BroadcastGameAsync();
        }

        await ClearAsync();
        await channel.LeaveAsync();
    }

    private async Task HandleConnectionLossAsync()
    {
        await ClearAsync();
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

    protected override Task BtnLeaveGame_LeftClickAsync()
        => LeaveGameLobbyAsync();

    protected override void UpdateDiscordPresence(bool resetTimer = false)
    {
        if (discordHandler == null)
            return;

        PlayerInfo player = FindLocalPlayer();

        if (player == null || Map == null || GameMode == null)
            return;
        string side = string.Empty;

        if (ddPlayerSides.Length > Players.IndexOf(player))
            side = (string)ddPlayerSides[Players.IndexOf(player)].SelectedItem.Tag;

        string currentState = ProgramConstants.IsInGame ? "In Game" : "In Lobby"; // not UI strings

        discordHandler.UpdatePresence(
            Map.UntranslatedName,
            GameMode.UntranslatedUIName,
            "Multiplayer",
            currentState,
            Players.Count,
            playerLimit,
            side,
            channel.UIName,
            IsHost,
            isCustomPassword,
            Locked,
            resetTimer);
    }

    private async Task ChannelUserLeftAsync(UserNameEventArgs e)
    {
        await RemovePlayerAsync(e.UserName);

        if (e.UserName == hostName)
        {
            connectionManager.MainChannel.AddMessage(
                new(ERROR_MESSAGE_COLOR, "The game host abandoned the game.".L10N("Client:Main:HostAbandoned")));
            await BtnLeaveGame_LeftClickAsync();
        }
        else
        {
            UpdateDiscordPresence();
        }
    }

    private async Task Channel_UserKickedAsync(UserNameEventArgs e)
    {
        if (e.UserName == ProgramConstants.PLAYERNAME)
        {
            connectionManager.MainChannel.AddMessage(
                new(ERROR_MESSAGE_COLOR, "You were kicked from the game!".L10N("Client:Main:YouWereKicked")));
            await ClearAsync();

            Visible = false;
            Enabled = false;
            return;
        }

        int index = Players.FindIndex(p => p.Name == e.UserName);

        if (index > -1)
        {
            Players.RemoveAt(index);
            CopyPlayerDataToUI();
            UpdateDiscordPresence();
            ClearReadyStatuses();

            (string Name, CnCNetTunnel Tunnel) playerTunnel = playerTunnels.SingleOrDefault(q => q.Name.Equals(e.UserName, StringComparison.OrdinalIgnoreCase));

            if (playerTunnel.Name is not null)
                playerTunnels.Remove(playerTunnel);

            tunnelPlayerIds.Clear();
            dynamicV3GameTunnelHandlers.Clear();
        }
    }

    private async Task Channel_UserListReceivedAsync()
    {
        if (!IsHost)
        {
            if (channel.Users.Find(hostName) == null)
            {
                connectionManager.MainChannel.AddMessage(
                    new(ERROR_MESSAGE_COLOR, "The game host has abandoned the game.".L10N("Client:Main:HostHasAbandoned")));
                await BtnLeaveGame_LeftClickAsync();
            }
        }

        UpdateDiscordPresence();
    }

    private async Task Channel_UserAddedAsync(ChannelUserEventArgs e)
    {
        var pInfo = new PlayerInfo(e.User.IRCUser.Name);

        Players.Add(pInfo);

        if (Players.Count + AIPlayers.Count > MAX_PLAYER_COUNT && AIPlayers.Count > 0)
            AIPlayers.RemoveAt(AIPlayers.Count - 1);

        if (dynamicTunnelsEnabled && pInfo != FindLocalPlayer())
            await BroadcastPlayerTunnelPingsAsync();

        sndJoinSound.Play();
#if WINFORMS
        WindowManager.FlashWindow();
#endif

        if (!IsHost)
        {
            CopyPlayerDataToUI();
            return;
        }

        if (e.User.IRCUser.Name != ProgramConstants.PLAYERNAME)
        {
            // Changing the map applies forced settings (co-op sides etc.) to the
            // new player, and it also sends an options broadcast message
            await ChangeMapAsync(GameModeMap);
            await BroadcastPlayerOptionsAsync();
            await BroadcastPlayerExtraOptionsAsync();
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
            await LockGameAsync();
        }
    }

    private async Task RemovePlayerAsync(string playerName)
    {
        AbortGameStart();

        PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

        if (pInfo != null)
        {
            Players.Remove(pInfo);
            CopyPlayerDataToUI();

            (string Name, CnCNetTunnel Tunnel) playerTunnel = playerTunnels.SingleOrDefault(q => q.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));

            if (playerTunnel.Name is not null)
                playerTunnels.Remove(playerTunnel);

            tunnelPlayerIds.Clear();
            dynamicV3GameTunnelHandlers.Clear();

            // This might not be necessary
            if (IsHost)
                await BroadcastPlayerOptionsAsync();
        }

        sndLeaveSound.Play();

        if (IsHost && Locked && !ProgramConstants.IsInGame)
            await UnlockGameAsync(true);
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
            lbChatMessages.AddMessage(new ChatMessage(
                Color.Silver,
                string.Format("Message blocked from {0}".L10N("Client:Main:MessageBlockedFromPlayer"),
                e.Message.SenderName)));
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
    protected override async Task HostLaunchGameAsync()
    {
        if (Players.Count > 1)
        {
            AddNotice("Contacting tunnel server...".L10N("Client:Main:ConnectingTunnel"));

            if (tunnelHandler.CurrentTunnel?.Version == Constants.TUNNEL_VERSION_2)
                await HostLaunchGameV2TunnelAsync();
            else if (tunnelHandler.CurrentTunnel?.Version == Constants.TUNNEL_VERSION_3)
                await HostLaunchGameV3TunnelAsync();
            else if (dynamicTunnelsEnabled)
                await HostLaunchGameV3TunnelAsync();
            else
                throw new InvalidOperationException("Unknown tunnel server version!");

            return;
        }

        Logger.Log("One player MP -- starting!");
        Players.ForEach(pInfo => pInfo.IsInGame = true);
        CopyPlayerDataToUI();
        cncnetUserData.AddRecentPlayers(Players.Select(p => p.Name), channel.UIName);

        await StartGameAsync();
    }

    private async Task HostLaunchGameV2TunnelAsync()
    {
        List<int> playerPorts = await tunnelHandler.CurrentTunnel.GetPlayerPortInfoAsync(Players.Count);

        if (playerPorts.Count < Players.Count)
        {
            ShowTunnelSelectionWindow(("An error occured while contacting " +
                "the CnCNet tunnel server.\nTry picking a different tunnel server:").L10N("Client:Main:ConnectTunnelError1"));
            AddNotice(("An error occured while contacting the specified CnCNet " +
                "tunnel server. Please try using a different tunnel server " +
                "(accessible by typing /CHANGETUNNEL in the chat box).").L10N("Client:Main:ConnectTunnelError2"),
                ERROR_MESSAGE_COLOR);
            return;
        }

        StringBuilder sb = new StringBuilder(CnCNetCommands.GAME_START_V2).Append(' ').Append(UniqueGameID);

        for (int pId = 0; pId < Players.Count; pId++)
        {
            Players[pId].Port = playerPorts[pId];

            sb.Append(';')
                .Append(Players[pId].Name)
                .Append(';')
                .Append("0.0.0.0:")
                .Append(playerPorts[pId]);
        }

        await channel.SendCTCPMessageAsync(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, PRIORITY_START_GAME);
        Players.ForEach(pInfo => pInfo.IsInGame = true);
        await StartGameAsync();
    }

    private async Task HostLaunchGameV3TunnelAsync()
    {
        btnLaunchGame.InputEnabled = false;

        var random = new Random();
        uint randomNumber = (uint)random.Next(0, int.MaxValue - (MAX_PLAYER_COUNT / 2)) * (uint)random.Next(1, 3);
        StringBuilder sb = new StringBuilder(CnCNetCommands.GAME_START_V3).Append(' ').Append(UniqueGameID);

        tunnelPlayerIds.Clear();

        for (int i = 0; i < Players.Count; i++)
        {
            uint id = randomNumber + (uint)i;

            sb.Append(';')
                .Append(id);
            tunnelPlayerIds.Add(id);
        }

        await channel.SendCTCPMessageAsync(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, PRIORITY_START_GAME);

        isStartingGame = true;

        ContactTunnel();
    }

    private void HandleGameStartV3TunnelMessage(string sender, string message)
    {
        if (sender != hostName)
            return;

        string[] parts = message.Split(';');

        if (parts.Length != Players.Count + 1)
            return;

        UniqueGameID = Conversions.IntFromString(parts[0], -1);

        if (UniqueGameID < 0)
            return;

        tunnelPlayerIds.Clear();

        for (int i = 1; i < parts.Length; i++)
        {
            if (!uint.TryParse(parts[i], out uint id))
                return;

            tunnelPlayerIds.Add(id);
        }

        isStartingGame = true;

        ContactTunnel();
    }

    private void ContactTunnel()
    {
        isPlayerConnectedToTunnel = new bool[Players.Count];

        uint localId = tunnelPlayerIds[Players.FindIndex(p => p == FindLocalPlayer())];

        dynamicV3GameTunnelHandlers.Clear();

        if (!dynamicTunnelsEnabled)
        {
            var gameTunnelHandler = new V3GameTunnelHandler();

            gameTunnelHandler.Connected += (_, _) => AddCallback(() => GameTunnelHandler_Connected_CallbackAsync().HandleTask());
            gameTunnelHandler.ConnectionFailed += (_, _) => AddCallback(() => GameTunnelHandler_ConnectionFailed_CallbackAsync().HandleTask());

            gameTunnelHandler.SetUp(tunnelHandler.CurrentTunnel, localId);
            gameTunnelHandler.ConnectToTunnel();
            dynamicV3GameTunnelHandlers.Add(new(Players.Where(q => q != FindLocalPlayer()).Select(q => q.Name).ToList(), gameTunnelHandler));
        }
        else
        {
            foreach (IGrouping<CnCNetTunnel, (string Name, CnCNetTunnel Tunnel)> tunnelGrouping in playerTunnels.GroupBy(q => q.Tunnel))
            {
                var gameTunnelHandler = new V3GameTunnelHandler();

                gameTunnelHandler.Connected += (_, _) => AddCallback(() => GameTunnelHandler_Connected_CallbackAsync().HandleTask());
                gameTunnelHandler.ConnectionFailed += (_, _) => AddCallback(() => GameTunnelHandler_ConnectionFailed_CallbackAsync().HandleTask());

                gameTunnelHandler.SetUp(tunnelGrouping.Key, localId);
                gameTunnelHandler.ConnectToTunnel();
                dynamicV3GameTunnelHandlers.Add(new(tunnelGrouping.Select(q => q.Name).ToList(), gameTunnelHandler));
            }
        }

        // Abort starting the game if not everyone
        // replies within the timer's limit
        gameStartTimer.Start();
    }

    private async Task GameTunnelHandler_Connected_CallbackAsync()
    {
        if (dynamicTunnelsEnabled)
        {
            if (dynamicV3GameTunnelHandlers.Any() && dynamicV3GameTunnelHandlers.All(q => q.Tunnel.IsConnected))
                isPlayerConnectedToTunnel[Players.FindIndex(p => p == FindLocalPlayer())] = true;
        }
        else
        {
            isPlayerConnectedToTunnel[Players.FindIndex(p => p == FindLocalPlayer())] = true;
        }

        await channel.SendCTCPMessageAsync(CnCNetCommands.TUNNEL_CONNECTION_OK, QueuedMessageType.SYSTEM_MESSAGE, PRIORITY_START_GAME);
    }

    private async Task GameTunnelHandler_ConnectionFailed_CallbackAsync()
    {
        await channel.SendCTCPMessageAsync(CnCNetCommands.TUNNEL_CONNECTION_FAIL, QueuedMessageType.INSTANT_MESSAGE, 0);
        HandleTunnelFail(ProgramConstants.PLAYERNAME);
    }

    private void HandleTunnelFail(string playerName)
    {
        Logger.Log(playerName + " failed to connect to tunnel - aborting game launch.");
        AddNotice(playerName + " failed to connect to the tunnel server. Please " +
            "retry or pick another tunnel server by type /CHANGETUNNEL to the chat input box.");
        AbortGameStart();
    }

    private async Task HandlePlayerConnectedToTunnelAsync(string playerName)
    {
        if (!isStartingGame)
            return;

        int index = Players.FindIndex(p => p.Name == playerName);

        if (index == -1)
        {
            Logger.Log("HandleTunnelConnected: Couldn't find player " + playerName + "!");
            AbortGameStart();
            return;
        }

        isPlayerConnectedToTunnel[index] = true;

        if (isPlayerConnectedToTunnel.All(b => b))
            await HandleAllPlayersConnectedToTunnelAsync();
    }

    private async Task HandleAllPlayersConnectedToTunnelAsync()
    {
        Logger.Log("All players are connected to the tunnel, starting game!");
        AddNotice("All players have connected to the tunnel...");

        List<int> playerPorts = new();

        foreach (V3GameTunnelHandler dynamicV3GameTunnelHandler in dynamicV3GameTunnelHandlers.Select(q => q.Tunnel))
        {
            var currentTunnelPlayers = Players.Where(q => dynamicV3GameTunnelHandlers.Single(r => r.Tunnel == dynamicV3GameTunnelHandler).Names.Contains(q.Name)).ToList();
            IEnumerable<int> indexes = currentTunnelPlayers.Select(q => q.Index);
            var playerIds = indexes.Select(q => tunnelPlayerIds[q]).ToList();
            List<int> createdPlayerPorts = dynamicV3GameTunnelHandler.CreatePlayerConnections(playerIds);
            int i = 0;

            foreach (PlayerInfo currentTunnelPlayer in currentTunnelPlayers)
            {
                currentTunnelPlayer.Port = createdPlayerPorts.Skip(i++).Take(1).Single();
            }

            playerPorts.AddRange(createdPlayerPorts);
        }

        int gamePort = V3GameTunnelHandler.GetFreePort(playerPorts);

        foreach (V3GameTunnelHandler dynamicV3GameTunnelHandler in dynamicV3GameTunnelHandlers.Select(q => q.Tunnel))
        {
            dynamicV3GameTunnelHandler.StartPlayerConnections(gamePort);
        }

        FindLocalPlayer().Port = gamePort;

        gameStartTimer.Pause();

        btnLaunchGame.InputEnabled = true;

        await StartGameAsync();
    }

    private void AbortGameStart()
    {
        btnLaunchGame.InputEnabled = true;

        foreach (V3GameTunnelHandler dynamicV3GameTunnelHandler in dynamicV3GameTunnelHandlers.Select(q => q.Tunnel))
        {
            dynamicV3GameTunnelHandler.Clear();
        }

        gameStartTimer.Pause();

        isStartingGame = false;
    }

    protected override string GetIPAddressForPlayer(PlayerInfo player)
    {
        if (UserINISettings.Instance.UseP2P)
            return IPAddress.Parse(player.IPAddress).MapToIPv4().ToString();

        if (dynamicTunnelsEnabled || tunnelHandler.CurrentTunnel.Version == Constants.TUNNEL_VERSION_3)
            return IPAddress.Loopback.MapToIPv4().ToString();

        return base.GetIPAddressForPlayer(player);
    }

    protected override Task RequestPlayerOptionsAsync(int side, int color, int start, int team)
    {
        byte[] value =
        {
            (byte)side,
            (byte)color,
            (byte)start,
            (byte)team
        };
        int intValue = BitConverter.ToInt32(value, 0);

        return channel.SendCTCPMessageAsync(
            FormattableString.Invariant($"{CnCNetCommands.OPTIONS_REQUEST} {intValue}"),
            QueuedMessageType.GAME_SETTINGS_MESSAGE,
            6);
    }

    protected override async Task RequestReadyStatusAsync()
    {
        if (Map == null || GameMode == null)
        {
            AddNotice(("The game host needs to select a different map or " +
                "you will be unable to participate in the match.").L10N("Client:Main:HostMustReplaceMap"));

            if (chkAutoReady.Checked)
                await channel.SendCTCPMessageAsync(CnCNetCommands.READY_REQUEST + " 0", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);

            return;
        }

        PlayerInfo pInfo = FindLocalPlayer();
        int readyState = 0;

        if (chkAutoReady.Checked)
            readyState = 2;
        else if (!pInfo.Ready)
            readyState = 1;

        await channel.SendCTCPMessageAsync($"{CnCNetCommands.READY_REQUEST} {readyState}", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);
    }

    protected override void AddNotice(string message, Color color) => channel.AddMessage(new(color, message));

    /// <summary>
    /// Handles player option requests received from non-host players.
    /// </summary>
    private async Task HandleOptionsRequestAsync(string playerName, int options)
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

        if (side > SideCount + RandomSelectorCount)
            return;

        if (color > MPColors.Count)
            return;

        bool[] disallowedSides = GetDisallowedSides();

        if (side > 0 && side <= SideCount && disallowedSides[side - 1])
            return;

        if (Map.CoopInfo != null)
        {
            if (Map.CoopInfo.DisallowedPlayerSides.Contains(side - 1) || side == SideCount + RandomSelectorCount)
                return;

            if (Map.CoopInfo.DisallowedPlayerColors.Contains(color - 1))
                return;
        }

        if (start > Map.MaxPlayers)
            return;

        if (team > 4)
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
        await BroadcastPlayerOptionsAsync();
    }

    /// <summary>
    /// Handles "I'm ready" messages received from non-host players.
    /// </summary>
    private async Task HandleReadyRequestAsync(string playerName, int readyStatus)
    {
        if (!IsHost)
            return;

        PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

        if (pInfo == null)
            return;

        pInfo.Ready = readyStatus > 0;
        pInfo.AutoReady = readyStatus > 1;

        CopyPlayerDataToUI();
        await BroadcastPlayerOptionsAsync();
    }

    /// <summary>
    /// Broadcasts player options to non-host players.
    /// </summary>
    protected override Task BroadcastPlayerOptionsAsync()
    {
        // Broadcast player options
        var sb = new StringBuilder(CnCNetCommands.PLAYER_OPTIONS + " ");

        foreach (PlayerInfo pInfo in Players.Concat(AIPlayers))
        {
            if (pInfo.IsAI)
                sb.Append(pInfo.AILevel);
            else
                sb.Append(pInfo.Name);

            sb.Append(';');

            // Combine the options into one integer to save bandwidth in
            // cases where the player uses default options (this is common for AI players)
            // Will hopefully make GameSurge kicking people a bit less common
            byte[] byteArray = new[]
            {
                (byte)pInfo.TeamId,
                (byte)pInfo.StartingLocation,
                (byte)pInfo.ColorId,
                (byte)pInfo.SideId,
            };
            int value = BitConverter.ToInt32(byteArray, 0);

            sb.Append(value);
            sb.Append(';');

            if (!pInfo.IsAI)
            {
                if (pInfo.AutoReady && !pInfo.IsInGame)
                    sb.Append(2);
                else
                    sb.Append(Convert.ToInt32(pInfo.Ready));

                sb.Append(';');
            }
        }

        return channel.SendCTCPMessageAsync(sb.ToString(), QueuedMessageType.GAME_PLAYERS_MESSAGE, 11);
    }

    protected override async Task PlayerExtraOptions_OptionsChangedAsync()
    {
        await base.PlayerExtraOptions_OptionsChangedAsync();
        await BroadcastPlayerExtraOptionsAsync();
    }

    protected override async Task BroadcastPlayerExtraOptionsAsync()
    {
        if (!IsHost)
            return;

        PlayerExtraOptions playerExtraOptions = GetPlayerExtraOptions();

        await channel.SendCTCPMessageAsync(playerExtraOptions.ToCncnetMessage(), QueuedMessageType.GAME_PLAYERS_EXTRA_MESSAGE, 11, true);
    }

    private Task BroadcastPlayerTunnelPingsAsync()
        => channel.SendCTCPMessageAsync(CnCNetCommands.PLAYER_TUNNEL_PINGS + " " + pinnedTunnelPingsMessage, QueuedMessageType.SYSTEM_MESSAGE, 10);

    /// <summary>
    /// Handles player option messages received from the game host.
    /// </summary>
    private void ApplyPlayerOptions(string sender, string message)
    {
        if (sender != hostName)
            return;

        Players.Clear();
        AIPlayers.Clear();

        string[] parts = message.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length;)
        {
            var pInfo = new PlayerInfo();
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

            if (side > SideCount + RandomSelectorCount)
                return;

            if (color > MPColors.Count)
                return;

            if (start > MAX_PLAYER_COUNT)
                return;

            if (team > 4)
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

                if (pInfo == FindLocalPlayer())
                    btnLaunchGame.Text = pInfo.Ready ? BTN_LAUNCH_NOT_READY : BTN_LAUNCH_READY;

                Players.Add(pInfo);

                i += HUMAN_PLAYER_OPTIONS_LENGTH;
            }
        }

        CopyPlayerDataToUI();
    }

    /// <summary>
    /// Broadcasts game options to non-host players
    /// when the host has changed an option.
    /// </summary>
    protected override async Task OnGameOptionChangedAsync()
    {
        await base.OnGameOptionChangedAsync();

        if (!IsHost)
            return;

        bool[] optionValues = new bool[CheckBoxes.Count];

        for (int i = 0; i < CheckBoxes.Count; i++)
            optionValues[i] = CheckBoxes[i].Checked;

        // Let's pack the booleans into bytes
        var byteList = Conversions.BoolArrayIntoBytes(optionValues).ToList();

        while (byteList.Count % 4 != 0)
            byteList.Add(0);

        int integerCount = byteList.Count / 4;
        byte[] byteArray = byteList.ToArray();
        var sb = new ExtendedStringBuilder(CnCNetCommands.GAME_OPTIONS + " ", true, ';');

        for (int i = 0; i < integerCount; i++)
            sb.Append(BitConverter.ToInt32(byteArray, i * 4));

        // We don't gain much in most cases by packing the drop-down values
        // (because they're bytes to begin with, and usually non-zero),
        // so let's just transfer them as usual
        foreach (GameLobbyDropDown dd in DropDowns)
            sb.Append(dd.SelectedIndex);

        sb.Append(Convert.ToInt32(Map.Official));
        sb.Append(Map.SHA1);
        sb.Append(GameMode.Name);
        sb.Append(FrameSendRate);
        sb.Append(MaxAhead);
        sb.Append(ProtocolVersion);
        sb.Append(RandomSeed);
        sb.Append(Convert.ToInt32(RemoveStartingLocations));
        sb.Append(Map.UntranslatedName);
        sb.Append(Convert.ToInt32(dynamicTunnelsEnabled)); // todo get from UI

        await channel.SendCTCPMessageAsync(sb.ToString(), QueuedMessageType.GAME_SETTINGS_MESSAGE, 11);
    }

    private async Task ToggleDynamicTunnelsAsync()
    {
        await ChangeDynamicTunnelsSettingAsync(!dynamicTunnelsEnabled);
        await OnGameOptionChangedAsync();

        if (!dynamicTunnelsEnabled)
            await TunnelSelectionWindow_TunnelSelectedAsync(new TunnelEventArgs(initialTunnel));
    }

    /// <summary>
    /// Handles game option messages received from the game host.
    /// </summary>
    private async Task ApplyGameOptionsAsync(string sender, string message)
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
        string mapHash = parts[partIndex + 1];
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

        lastMapHash = mapHash;
        lastMapName = mapName;

        GameModeMap = GameModeMaps.Find(gmm => gmm.GameMode.Name == gameMode && gmm.Map.SHA1 == mapHash);

        if (GameModeMap == null)
        {
            await ChangeMapAsync(null);

            if (!isMapOfficial)
                await RequestMapAsync();
            else
                await ShowOfficialMapMissingMessageAsync(mapHash);
        }
        else if (GameModeMap != currentGameModeMap)
        {
            await ChangeMapAsync(GameModeMap);
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

            bool success = int.TryParse(parts[i], out int checkBoxStatusInt);

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
                int gameOptionIndex = (i * 32) + optionIndex;

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

            bool success = int.TryParse(parts[i], out int ddSelectedIndex);

            if (!success)
            {
                AddNotice(("Failed to parse drop down options sent by game host (2)! " +
                    "The game host's game version might be different from yours.").L10N("Client:Main:HostGameOptionInvalidTheSecondTime"), Color.Red);
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

        bool parseSuccess = int.TryParse(parts[partIndex + 6], out int randomSeed);

        if (!parseSuccess)
        {
            AddNotice(("Failed to parse random seed from game options message! " +
                "The game host's game version might be different from yours.").L10N("Client:Main:HostRandomSeedError"), Color.Red);
        }

        bool removeStartingLocations = Convert.ToBoolean(Conversions.IntFromString(parts[partIndex + 7],
            Convert.ToInt32(RemoveStartingLocations)));

        SetRandomStartingLocations(removeStartingLocations);

        RandomSeed = randomSeed;

        bool newDynamicTunnelsSetting = Conversions.BooleanFromString(parts[partIndex + 9], true);

        if (newDynamicTunnelsSetting != dynamicTunnelsEnabled)
            await ChangeDynamicTunnelsSettingAsync(newDynamicTunnelsSetting);
    }

    private async Task ChangeDynamicTunnelsSettingAsync(bool newDynamicTunnelsEnabledValue)
    {
        dynamicTunnelsEnabled = newDynamicTunnelsEnabledValue;

        if (newDynamicTunnelsEnabledValue)
            AddNotice(string.Format("The game host has enabled Dynamic Tunnels".L10N("UI:Main:HostEnableDynamicTunnels")));
        else
            AddNotice(string.Format("The game host has disabled Dynamic Tunnels".L10N("UI:Main:HostDisableDynamicTunnels")));

        if (newDynamicTunnelsEnabledValue)
        {
            tunnelHandler.CurrentTunnel = tunnelHandler.Tunnels
                .Where(q => q.PingInMs > -1 && !q.RequiresPassword && q.Clients < q.MaxClients - 8 && q.Version == Constants.TUNNEL_VERSION_3)
                .MinBy(q => q.PingInMs);

            await BroadcastPlayerTunnelPingsAsync();
        }
    }

    private async Task RequestMapAsync()
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
            await channel.SendCTCPMessageAsync(CnCNetCommands.MAP_SHARING_DISABLED, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }
    }

    private Task ShowOfficialMapMissingMessageAsync(string sha1)
    {
        AddNotice(("The game host has selected an official map that doesn't exist on your installation. " +
            "This could mean that the game host has modified game files, or is running a different game version. " +
            "They need to change the map or you will be unable to participate in the match.").L10N("Client:Main:OfficialMapNotExist"));
        return channel.SendCTCPMessageAsync(CnCNetCommands.MAP_SHARING_FAIL + " " + sha1, QueuedMessageType.SYSTEM_MESSAGE, 9);
    }

    private void MapSharingConfirmationPanel_MapDownloadConfirmed(object sender, EventArgs e)
    {
        Logger.Log("Map sharing confirmed.");
        AddNotice("Attempting to download map.".L10N("Client:Main:DownloadingMap"));
        mapSharingConfirmationPanel.SetDownloadingStatus();
        MapSharer.DownloadMap(lastMapHash, localGame, lastMapName);
    }

    protected override Task ChangeMapAsync(GameModeMap gameModeMap)
    {
        mapSharingConfirmationPanel.Disable();
        return base.ChangeMapAsync(gameModeMap);
    }

    /// <summary>
    /// Signals other players that the local player has returned from the game,
    /// and unlocks the game as well as generates a new random seed as the game host.
    /// </summary>
    protected override async Task GameProcessExitedAsync()
    {
        await base.GameProcessExitedAsync();
        await channel.SendCTCPMessageAsync(CnCNetCommands.RETURN, QueuedMessageType.SYSTEM_MESSAGE, 20);
        ReturnNotification(ProgramConstants.PLAYERNAME);

        if (IsHost)
        {
            RandomSeed = new Random().Next();
            await OnGameOptionChangedAsync();
            ClearReadyStatuses();
            CopyPlayerDataToUI();
            await BroadcastPlayerOptionsAsync();
            await BroadcastPlayerExtraOptionsAsync();

            if (Players.Count < playerLimit)
                await UnlockGameAsync(true);
        }
    }

    /// <summary>
    /// Handles the "START" (game start) command sent by the game host.
    /// </summary>
    private async Task NonHostLaunchGameAsync(string sender, string message)
    {
        if (tunnelHandler.CurrentTunnel.Version != Constants.TUNNEL_VERSION_2)
            return;

        if (sender != hostName)
            return;

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

            bool success = int.TryParse(ipAndPort[1], out int port);

            if (!success)
                return;

            PlayerInfo pInfo = Players.Find(p => p.Name == pName);

            if (pInfo == null)
                return;

            pInfo.Port = port;
            recentPlayers.Add(pName);
        }

        cncnetUserData.AddRecentPlayers(recentPlayers, channel.UIName);
        await StartGameAsync();
    }

    protected override async Task StartGameAsync()
    {
        AddNotice("Starting game...".L10N("Client:Main:StartingGame"));

        isStartingGame = false;

        var fhc = new FileHashCalculator();

        fhc.CalculateHashes(GameModeMaps.GameModes);

        if (gameFilesHash != fhc.GetCompleteHash())
        {
            Logger.Log("Game files modified during client session!");
            await channel.SendCTCPMessageAsync(CnCNetCommands.CHEAT_DETECTED, QueuedMessageType.INSTANT_MESSAGE, 0);
            HandleCheatDetectedMessage(ProgramConstants.PLAYERNAME);
        }

        await base.StartGameAsync();
    }

    protected override void WriteSpawnIniAdditions(IniFile iniFile)
    {
        base.WriteSpawnIniAdditions(iniFile);

        if (!UserINISettings.Instance.UseP2P && !UserINISettings.Instance.UseDynamicTunnels && tunnelHandler.CurrentTunnel.Version == Constants.TUNNEL_VERSION_2)
        {
            iniFile.SetStringValue("Tunnel", "Ip", tunnelHandler.CurrentTunnel.Address);
            iniFile.SetIntValue("Tunnel", "Port", tunnelHandler.CurrentTunnel.Port);
        }

        iniFile.SetIntValue("Settings", "GameID", UniqueGameID);
        iniFile.SetBooleanValue("Settings", "Host", IsHost);

        PlayerInfo localPlayer = FindLocalPlayer();

        if (localPlayer == null)
            return;

        iniFile.SetIntValue("Settings", "Port", localPlayer.Port);
    }

    protected override Task SendChatMessageAsync(string message) => channel.SendChatMessageAsync(message, chatColor);

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

    protected override async Task GetReadyNotificationAsync()
    {
        await base.GetReadyNotificationAsync();
#if WINFORMS
        WindowManager.FlashWindow();
#endif
        TopBar.SwitchToPrimary();

        if (IsHost)
            await channel.SendCTCPMessageAsync(CnCNetCommands.GET_READY_LOBBY, QueuedMessageType.GAME_GET_READY_MESSAGE, 0);
    }

    protected override async Task AISpectatorsNotificationAsync()
    {
        await base.AISpectatorsNotificationAsync();

        if (IsHost)
            await channel.SendCTCPMessageAsync(CnCNetCommands.AI_SPECTATORS, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override async Task InsufficientPlayersNotificationAsync()
    {
        await base.InsufficientPlayersNotificationAsync();

        if (IsHost)
            await channel.SendCTCPMessageAsync(CnCNetCommands.INSUFFICIENT_PLAYERS, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override async Task TooManyPlayersNotificationAsync()
    {
        await base.TooManyPlayersNotificationAsync();

        if (IsHost)
            await channel.SendCTCPMessageAsync(CnCNetCommands.TOO_MANY_PLAYERS, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override async Task SharedColorsNotificationAsync()
    {
        await base.SharedColorsNotificationAsync();

        if (IsHost)
            await channel.SendCTCPMessageAsync(CnCNetCommands.SHARED_COLORS, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override async Task SharedStartingLocationNotificationAsync()
    {
        await base.SharedStartingLocationNotificationAsync();

        if (IsHost)
            await channel.SendCTCPMessageAsync(CnCNetCommands.SHARED_STARTING_LOCATIONS, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override async Task LockGameNotificationAsync()
    {
        await base.LockGameNotificationAsync();

        if (IsHost)
            await channel.SendCTCPMessageAsync(CnCNetCommands.LOCK_GAME, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override async Task NotVerifiedNotificationAsync(int playerIndex)
    {
        await base.NotVerifiedNotificationAsync(playerIndex);

        if (IsHost)
            await channel.SendCTCPMessageAsync(CnCNetCommands.NOT_VERIFIED + " " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
    }

    protected override async Task StillInGameNotificationAsync(int playerIndex)
    {
        await base.StillInGameNotificationAsync(playerIndex);

        if (IsHost)
            await channel.SendCTCPMessageAsync(CnCNetCommands.STILL_IN_GAME + " " + playerIndex, QueuedMessageType.GAME_NOTIFICATION_MESSAGE, 0);
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

    private async Task FileHashNotificationAsync(string sender, string filesHash)
    {
        if (!IsHost)
            return;

        PlayerInfo pInfo = Players.Find(p => p.Name == sender);

        if (pInfo != null)
            pInfo.Verified = true;

        CopyPlayerDataToUI();

        if (filesHash != gameFilesHash)
        {
            await channel.SendCTCPMessageAsync(CnCNetCommands.CHEATER + " " + sender, QueuedMessageType.GAME_CHEATER_MESSAGE, 10);
            CheaterNotification(ProgramConstants.PLAYERNAME, sender);
        }
    }

    private void CheaterNotification(string sender, string cheaterName)
    {
        if (sender != hostName)
            return;

        AddNotice(string.Format("Player {0} has different files compared to the game host. Either {0} or the game host could be cheating.".L10N("Client:Main:DifferentFileCheating"), cheaterName), Color.Red);
    }

    protected override async Task BroadcastDiceRollAsync(int dieSides, int[] results)
    {
        string resultString = string.Join(",", results);

        await channel.SendCTCPMessageAsync($"{CnCNetCommands.DICE_ROLL} {dieSides},{resultString}", QueuedMessageType.CHAT_MESSAGE, 0);
        PrintDiceRollResult(ProgramConstants.PLAYERNAME, dieSides, results);
    }

    protected override async Task HandleLockGameButtonClickAsync()
    {
        if (!Locked)
        {
            AddNotice("You've locked the game room.".L10N("Client:Main:RoomLockedByYou"));
            await LockGameAsync();
        }
        else
        {
            if (Players.Count < playerLimit)
            {
                AddNotice("You've unlocked the game room.".L10N("Client:Main:RoomUnockedByYou"));
                await UnlockGameAsync(false);
            }
            else
            {
                AddNotice(string.Format("Cannot unlock game; the player limit ({0}) has been reached.".L10N("Client:Main:RoomCantUnlockAsLimit"), playerLimit));
            }
        }
    }

    protected override async Task LockGameAsync()
    {
        await connectionManager.SendCustomMessageAsync(
            new(string.Format(IRCCommands.MODE + " {0} +i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));

        Locked = true;
        btnLockGame.Text = "Unlock Game".L10N("Client:Main:UnlockGame");
        AccelerateGameBroadcasting();
    }

    protected override async Task UnlockGameAsync(bool announce)
    {
        await connectionManager.SendCustomMessageAsync(
            new(string.Format(IRCCommands.MODE + " {0} -i", channel.ChannelName), QueuedMessageType.INSTANT_MESSAGE, -1));

        Locked = false;

        if (announce)
            AddNotice("The game room has been unlocked.".L10N("Client:Main:GameRoomUnlocked"));

        btnLockGame.Text = "Lock Game".L10N("Client:Main:LockGame");
        AccelerateGameBroadcasting();
    }

    protected override async Task KickPlayerAsync(int playerIndex)
    {
        if (playerIndex >= Players.Count)
            return;

        PlayerInfo pInfo = Players[playerIndex];

        AddNotice(string.Format("Kicking {0} from the game...".L10N("Client:Main:KickPlayer"), pInfo.Name));
        await channel.SendKickMessageAsync(pInfo.Name, 8);
    }

    protected override async Task BanPlayerAsync(int playerIndex)
    {
        if (playerIndex >= Players.Count)
            return;

        PlayerInfo pInfo = Players[playerIndex];
        IRCUser user = connectionManager.UserList.Find(u => u.Name == pInfo.Name);

        if (user != null)
        {
            AddNotice(string.Format("Banning and kicking {0} from the game...".L10N("Client:Main:BanAndKickPlayer"), pInfo.Name));
            await channel.SendBanMessageAsync(user.Hostname, 8);
            await channel.SendKickMessageAsync(user.Name, 8);
        }
    }

    private void HandleCheatDetectedMessage(string sender) =>
        AddNotice(string.Format("{0} has modified game files during the client session. They are likely attempting to cheat!".L10N("Client:Main:PlayerModifyFileCheat"), sender), Color.Red);

    private async Task HandleTunnelServerChangeMessageAsync(string sender, string hash)
    {
        if (sender != hostName)
            return;

        CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));

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
        await HandleTunnelServerChangeAsync(tunnel);
        UpdateLaunchGameButtonStatus();
    }

    private void HandleTunnelPingsMessage(string sender, string tunnelPingsMessage)
    {
        (string Name, CnCNetTunnel Tunnel) playerTunnelInfo = playerTunnels.SingleOrDefault(p => p.Name.Equals(sender, StringComparison.OrdinalIgnoreCase));

        if (playerTunnelInfo.Tunnel is not null)
            return;

        tunnelPingsMessages.Add((sender, tunnelPingsMessage));

        if (!pinnedTunnels.Any())
            return;

        string[] tunnelPingsLines = tunnelPingsMessage.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<(int Ping, string Hash)> tunnelPings = tunnelPingsLines.Select(q =>
        {
            string[] split = q.Split(';');

            return (int.Parse(split[0], CultureInfo.InvariantCulture), split[1]);
        });
        IEnumerable<(int CombinedPing, string Hash)> combinedTunnelResults = tunnelPings
            .Where(q => pinnedTunnels.Select(r => r.Hash).Contains(q.Hash))
            .Select(q => (CombinedPing: q.Ping + pinnedTunnels.SingleOrDefault(r => q.Hash.Equals(r.Hash, StringComparison.OrdinalIgnoreCase)).Ping, q.Hash));
        (int _, string hash) = combinedTunnelResults
            .OrderBy(q => q.CombinedPing)
            .ThenBy(q => q.Hash, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (hash is null)
        {
            AddNotice(string.Format("No common tunnel server found for: {0}".L10N("Client:Main:NoCommonTunnel"), sender));
        }
        else
        {
            CnCNetTunnel tunnel = tunnelHandler.Tunnels.Single(q => q.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));

            playerTunnels.Add(new(sender, tunnel));
            AddNotice(string.Format("Dynamic tunnel server negotiated with {0}: {1} ({2}ms)".L10N("Client:Main:TunnelNegotiated"), sender, tunnel.Name, tunnel.PingInMs));
        }
    }

    /// <summary>
    /// Changes the tunnel server used for the game.
    /// </summary>
    /// <param name="tunnel">The new tunnel server to use.</param>
    private Task HandleTunnelServerChangeAsync(CnCNetTunnel tunnel)
    {
        tunnelHandler.CurrentTunnel = tunnel;

        AddNotice(string.Format("The game host has changed the tunnel server to: {0}".L10N("Client:Main:HostChangeTunnel"), tunnel.Name));
        return UpdatePingAsync();
    }

    protected override bool UpdateLaunchGameButtonStatus()
    {
        btnLaunchGame.Enabled = base.UpdateLaunchGameButtonStatus() && !tunnelErrorMode;
        return btnLaunchGame.Enabled;
    }

    private async Task MapSharer_HandleMapDownloadFailedAsync(SHA1EventArgs e)
    {
        // If the host has already uploaded the map, we shouldn't request them to re-upload it
        if (hostUploadedMaps.Contains(e.SHA1))
        {
            AddNotice("Download of the custom map failed. The host needs to change the map or you will be unable to participate in this match.".L10N("Client:Main:DownloadCustomMapFailed"));
            mapSharingConfirmationPanel.SetFailedStatus();
            await channel.SendCTCPMessageAsync(CnCNetCommands.MAP_SHARING_FAIL + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
            return;
        }

        if (chatCommandDownloadedMaps.Contains(e.SHA1))
        {
            // Notify the user that their chat command map download failed.
            // Do not notify other users with a CTCP message as this is irrelevant to them.
            AddNotice("Downloading map via chat command has failed. Check the map ID and try again.".L10N("Client:Main:DownloadMapCommandFailedGeneric"));
            mapSharingConfirmationPanel.SetFailedStatus();
            return;
        }

        AddNotice("Requesting the game host to upload the map to the CnCNet map database.".L10N("Client:Main:RequestHostUploadMapToDB"));
        await channel.SendCTCPMessageAsync(CnCNetCommands.MAP_SHARING_UPLOAD + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
    }

    private async Task MapSharer_HandleMapDownloadCompleteAsync(SHA1EventArgs e)
    {
        string mapFileName = MapSharer.GetMapFileName(e.SHA1, e.MapName);
        Logger.Log("Map " + mapFileName + " downloaded, parsing.");
        string mapPath = "Maps/Custom/" + mapFileName;
        Map map = MapLoader.LoadCustomMap(mapPath, out string returnMessage);

        if (map != null)
        {
            AddNotice(returnMessage);

            if (lastMapHash == e.SHA1)
            {
                GameModeMap = GameModeMaps.Find(gmm => gmm.Map.SHA1 == lastMapHash);

                await ChangeMapAsync(GameModeMap);
            }
        }
        else if (chatCommandDownloadedMaps.Contains(e.SHA1))
        {
            // Somehow the user has managed to download an already existing sha1 hash.
            // This special case prevents user confusion from the file successfully downloading but showing an error anyway.
            AddNotice(returnMessage, Color.Yellow);
            AddNotice(
                "Map was downloaded, but a duplicate is already loaded from a different filename. This may cause strange behavior.".L10N("Client:Main:DownloadMapCommandDuplicateMapFileLoaded"),
                Color.Yellow);
        }
        else
        {
            AddNotice(returnMessage, Color.Red);
            AddNotice("Transfer of the custom map failed. The host needs to change the map or you will be unable to participate in this match.".L10N("Client:Main:MapTransferFailed"));
            mapSharingConfirmationPanel.SetFailedStatus();
            await channel.SendCTCPMessageAsync(CnCNetCommands.MAP_SHARING_FAIL + " " + e.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }
    }

    private async Task MapSharer_HandleMapUploadFailedAsync(MapEventArgs e)
    {
        Map map = e.Map;

        hostUploadedMaps.Add(map.SHA1);
        AddNotice(string.Format("Uploading map {0} to the CnCNet map database failed.".L10N("Client:Main:UpdateMapToDBFailed"), map.Name));

        if (map == Map)
        {
            AddNotice("You need to change the map or some players won't be able to participate in this match.".L10N("Client:Main:YouMustReplaceMap"));
            await channel.SendCTCPMessageAsync(CnCNetCommands.MAP_SHARING_FAIL + " " + map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }
    }

    private async Task MapSharer_HandleMapUploadCompleteAsync(MapEventArgs e)
    {
        hostUploadedMaps.Add(e.Map.SHA1);
        AddNotice(string.Format("Uploading map {0} to the CnCNet map database complete.".L10N("Client:Main:UpdateMapToDBSuccess"), e.Map.Name));

        if (e.Map == Map)
        {
            await channel.SendCTCPMessageAsync(CnCNetCommands.MAP_SHARING_DOWNLOAD + " " + Map.SHA1, QueuedMessageType.SYSTEM_MESSAGE, 9);
        }
    }

    /// <summary>
    /// Handles a map upload request sent by a player.
    /// </summary>
    /// <param name="sender">The sender of the request.</param>
    /// <param name="mapHash">The SHA1 of the requested map.</param>
    private void HandleMapUploadRequest(string sender, string mapHash)
    {
        if (hostUploadedMaps.Contains(mapHash))
        {
            Logger.Log("HandleMapUploadRequest: Map " + mapHash + " is already uploaded!");
            return;
        }

        Map map = null;

        foreach (GameMode gm in GameModeMaps.GameModes)
        {
            map = gm.Maps.Find(m => m.SHA1 == mapHash);

            if (map != null)
                break;
        }

        if (map == null)
        {
            Logger.Log("Unknown map upload request from " + sender + ": " + mapHash);
            return;
        }

        if (map.Official)
        {
            Logger.Log("HandleMapUploadRequest: Map is official, so skip request");
            AddNotice(
                string.Format(("{0} doesn't have the map '{1}' on their local installation. " +
                "The map needs to be changed or {0} is unable to participate in the match.").L10N("Client:Main:PlayerMissingMap"),
                sender,
                map.Name));

            return;
        }

        if (!IsHost)
            return;

        AddNotice(
            string.Format(("{0} doesn't have the map '{1}' on their local installation. " +
            "Attempting to upload the map to the CnCNet map database.").L10N("Client:Main:UpdateMapToDBPrompt"),
            sender,
            map.Name));
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

            if (lastMapHash == sha1 && Map == null)
                AddNotice("The game host needs to change the map or you won't be able to participate in this match.".L10N("Client:Main:HostMustChangeMap"));

            return;
        }

        if (lastMapHash == sha1)
        {
            if (!IsHost)
            {
                AddNotice(
                    string.Format("{0} has failed to download the map from the CnCNet map database.".L10N("Client:Main:PlayerDownloadMapFailed") + " " +
                    "The host needs to change the map or {0} won't be able to participate in this match.".L10N("Client:Main:HostNeedChangeMapForPlayer"),
                    sender));
            }
            else
            {
                AddNotice(
                    string.Format("{0} has failed to download the map from the CnCNet map database.".L10N("Client:Main:PlayerDownloadMapFailed") + " " +
                    "You need to change the map or {0} won't be able to participate in this match.".L10N("Client:Main:YouNeedChangeMapForPlayer"),
                    sender));
            }
        }
    }

    private void HandleMapDownloadRequest(string sender, string sha1)
    {
        if (sender != hostName)
            return;

        hostUploadedMaps.Add(sha1);

        if (lastMapHash == sha1 && Map == null)
        {
            Logger.Log("The game host has uploaded the map into the database. Re-attempting download...");
            MapSharer.DownloadMap(sha1, localGame, lastMapName);
        }
    }

    private void HandleMapSharingBlockedMessage(string sender)
    {
        AddNotice(
            string.Format("The selected map doesn't exist on {0}'s installation, and they " +
            "have map sharing disabled in settings. The game host needs to change to a non-custom map or " +
            "they will be unable to participate in this match.".L10N("Client:Main:PlayerMissingMapDisabledSharing"),
            sender));
    }

    /// <summary>
    /// Download a map from CNCNet using a map hash ID.
    ///
    /// Users and testers can get map hash IDs from this URL template:
    ///
    /// - https://mapdb.cncnet.org/search.php?game=GAME_ID&search=MAP_NAME_SEARCH_STRING.
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
            sha1 = parameters[..firstSpaceIndex];
            mapName = parameters[(firstSpaceIndex + 1)..];
            mapName = mapName.Trim();
        }

        // Remove erroneous "?". These sneak in when someone double-clicks a map ID and copies it from the cncnet search endpoint.
        // There is some weird whitespace that gets copied to chat as a "?" at the end of the hash. It's hard to spot, so just hold the user's hand.
        sha1 = sha1.Replace("?", string.Empty);

        // See if the user already has this map, with any filename, before attempting to download it.
        GameModeMap loadedMap = GameModeMaps.Find(gmm => gmm.Map.SHA1 == sha1);

        if (loadedMap != null)
        {
            message = string.Format(
                "The map for ID \"{0}\" is already loaded from \"{1}.map\", delete the existing file before trying again.".L10N("Client:Main:DownloadMapCommandSha1AlreadyExists"),
                sha1,
                loadedMap.Map.BaseFilePath);
            AddNotice(message, Color.Yellow);
            Logger.Log(message);
            return;
        }

        // Replace any characters that are not safe for filenames.
        char replaceUnsafeCharactersWith = '-';

        // Use a hashset instead of an array for quick lookups in `invalidChars.Contains()`.
        var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
        string safeMapName = new(mapName.Select(c => invalidChars.Contains(c) ? replaceUnsafeCharactersWith : c).ToArray());

        chatCommandDownloadedMaps.Add(sha1);

        message = string.Format("Attempting to download map via chat command: sha1={0}, mapName={1}".L10N("Client:Main:DownloadMapCommandStartingDownload"), sha1, mapName);

        Logger.Log(message);
        AddNotice(message);

        MapSharer.DownloadMap(sha1, localGame, safeMapName);
    }

    /// <summary>
    /// Lowers the time until the next game broadcasting message.
    /// </summary>
    private void AccelerateGameBroadcasting() =>
        gameBroadcastTimer.Accelerate(TimeSpan.FromSeconds(GAME_BROADCAST_ACCELERATION));

    private async Task BroadcastGameAsync()
    {
        Channel broadcastChannel = connectionManager.FindChannel(gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame));

        if (broadcastChannel == null)
            return;

        if (ProgramConstants.IsInGame && broadcastChannel.Users.Count > 500)
            return;

        if (GameMode == null || Map == null)
            return;

        StringBuilder sb = new StringBuilder(CnCNetCommands.GAME + " ")
            .Append(ProgramConstants.CNCNET_PROTOCOL_REVISION)
            .Append(';')
            .Append(ProgramConstants.GAME_VERSION)
            .Append(';')
            .Append(playerLimit)
            .Append(';')
            .Append(channel.ChannelName)
            .Append(';')
            .Append(channel.UIName)
            .Append(';')
            .Append(Locked ? '1' : '0')
            .Append(Convert.ToInt32(isCustomPassword))
            .Append(Convert.ToInt32(closed))
            .Append('0') // IsLoadedGame
            .Append('0') // IsLadder
            .Append(';');

        foreach (PlayerInfo pInfo in Players)
        {
            sb.Append(pInfo.Name);
            sb.Append(',');
        }

        sb.Remove(sb.Length - 1, 1)
            .Append(';')
            .Append(Map.UntranslatedName)
            .Append(';')
            .Append(GameMode.UntranslatedUIName)
            .Append(';')
            .Append(tunnelHandler.CurrentTunnel?.Hash ?? ProgramConstants.CNCNET_DYNAMIC_TUNNELS)
            .Append(';')
            .Append(0); // LoadedGameId
        await broadcastChannel.SendCTCPMessageAsync(sb.ToString(), QueuedMessageType.SYSTEM_MESSAGE, 20);
    }

    public override string GetSwitchName() => "Game Lobby".L10N("Client:Main:GameLobby");
}