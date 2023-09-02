﻿using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ClientCore.Enums;
using DTAConfig;
using ClientCore.Extensions;
using SixLabors.ImageSharp;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    using UserChannelPair = Tuple<string, string>;
    using InvitationIndex = Dictionary<Tuple<string, string>, WeakReference>;

    internal sealed class CnCNetLobby : XNAWindow, ISwitchable
    {
        public event EventHandler UpdateCheck;

        public CnCNetLobby(WindowManager windowManager, CnCNetManager connectionManager,
            CnCNetGameLobby gameLobby, CnCNetGameLoadingLobby gameLoadingLobby,
            TopBar topBar, PrivateMessagingWindow pmWindow, TunnelHandler tunnelHandler,
            GameCollection gameCollection, CnCNetUserData cncnetUserData, MapLoader mapLoader)
            : base(windowManager)
        {
            this.connectionManager = connectionManager;
            this.gameLobby = gameLobby;
            this.gameLoadingLobby = gameLoadingLobby;
            this.tunnelHandler = tunnelHandler;
            this.topBar = topBar;
            this.pmWindow = pmWindow;
            this.gameCollection = gameCollection;
            this.cncnetUserData = cncnetUserData;
            this.mapLoader = mapLoader;

            ctcpCommandHandlers = new CommandHandlerBase[]
            {
                new StringCommandHandler(CnCNetCommands.GAME_INVITE, (sender, argumentsString) => HandleGameInviteCommandAsync(sender, argumentsString).HandleTask()),
                new NoParamCommandHandler(CnCNetCommands.GAME_INVITATION_FAILED, HandleGameInvitationFailedNotification)
            };

            topBar.LogoutEvent += LogoutEvent;
        }

        private readonly MapLoader mapLoader;
        private readonly CnCNetManager connectionManager;
        private readonly CnCNetUserData cncnetUserData;

        private PlayerListBox lbPlayerList;
        private ChatListBox lbChatMessages;
        private GameListBox lbGameList;
        private GlobalContextMenu globalContextMenu;

        private XNAClientButton btnLogout;
        private XNAClientButton btnNewGame;
        private XNAClientButton btnJoinGame;

        private XNAChatTextBox tbChatInput;

        private XNALabel lblColor;
        private XNALabel lblCurrentChannel;
        private XNALabel lblOnline;
        private XNALabel lblOnlineCount;

        private XNAClientDropDown ddColor;
        private XNAClientDropDown ddCurrentChannel;

        private XNASuggestionTextBox tbGameSearch;

        private XNAClientStateButton<SortDirection> btnGameSortAlpha;

        private XNAClientToggleButton btnGameFilterOptions;

        private DarkeningPanel gameCreationPanel;

        private Channel currentChatChannel;

        private readonly GameCollection gameCollection;

        private Texture2D unknownGameIcon;

        private EnhancedSoundEffect sndGameCreated;
        private EnhancedSoundEffect sndGameInviteReceived;

        private readonly CnCNetGameLobby gameLobby;
        private readonly CnCNetGameLoadingLobby gameLoadingLobby;

        private readonly TunnelHandler tunnelHandler;

        private CnCNetLoginWindow loginWindow;

        private readonly TopBar topBar;

        private readonly PrivateMessagingWindow pmWindow;

        private PasswordRequestWindow passwordRequestWindow;

        private bool isInGameRoom;
        private bool updateDenied;

        private string localGameID;
        private CnCNetGame localGame;

        private readonly List<string> followedGames = new List<string>();

        private bool isJoiningGame;
        private HostedCnCNetGame gameOfLastJoinAttempt;

        private CancellationTokenSource gameCheckCancellation;

        private readonly CommandHandlerBase[] ctcpCommandHandlers;

        private InvitationIndex invitationIndex;

        private GameFiltersPanel panelGameFilters;

        private EventHandler<ChannelUserEventArgs> gameChannel_UserAddedFunc;
        private EventHandler gameChannel_InvalidPasswordEntered_LoadedGameFunc;
        private EventHandler<ChannelUserEventArgs> gameLoadingChannel_UserAddedFunc;
        private EventHandler gameChannel_InvalidPasswordEntered_NewGameFunc;
        private EventHandler gameChannel_InviteOnlyErrorOnJoinFunc;
        private EventHandler gameChannel_ChannelFullFunc;
        private EventHandler<MessageEventArgs> gameChannel_TargetChangeTooFastFunc;

        private void GameList_ClientRectangleUpdated(object sender, EventArgs e)
        {
            panelGameFilters.ClientRectangle = lbGameList.ClientRectangle;
        }

        private void LogoutEvent(object sender, EventArgs e)
        {
            isJoiningGame = false;
        }

        public override void Initialize()
        {
            invitationIndex = new InvitationIndex();

            ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX - 64,
                WindowManager.RenderResolutionY - 64);

            Name = nameof(CnCNetLobby);
            BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbybg.png");
            localGameID = ClientConfiguration.Instance.LocalGame;
            localGame = gameCollection.GameList.Find(g => g.InternalName.ToUpper() == localGameID.ToUpper());

            btnNewGame = new XNAClientButton(WindowManager);
            btnNewGame.Name = nameof(btnNewGame);
            btnNewGame.ClientRectangle = new Rectangle(12, Height - 29, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnNewGame.Text = "Create Game".L10N("Client:Main:CreateGame");
            btnNewGame.AllowClick = false;
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnJoinGame = new XNAClientButton(WindowManager);
            btnJoinGame.Name = nameof(btnJoinGame);
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.Right + 12,
                btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnJoinGame.Text = "Join Game".L10N("Client:Main:JoinGame");
            btnJoinGame.AllowClick = false;
            btnJoinGame.LeftClick += (_, _) => JoinSelectedGameAsync().HandleTask();

            btnLogout = new XNAClientButton(WindowManager);
            btnLogout.Name = nameof(btnLogout);
            btnLogout.ClientRectangle = new Rectangle(Width - 145, btnNewGame.Y,
                UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLogout.Text = "Log Out".L10N("Client:Main:ButtonLogOut");
            btnLogout.LeftClick += (_, _) => BtnLogout_LeftClickAsync().HandleTask();

            var gameListRectangle = new Rectangle(
                btnNewGame.X, 41,
                btnJoinGame.Right - btnNewGame.X, btnNewGame.Y - 47);

            panelGameFilters = new GameFiltersPanel(WindowManager);
            panelGameFilters.Name = nameof(panelGameFilters);
            panelGameFilters.ClientRectangle = gameListRectangle;
            panelGameFilters.Disable();

            lbGameList = new GameListBox(WindowManager, mapLoader, localGameID, HostedGameMatches);
            lbGameList.Name = nameof(lbGameList);
            lbGameList.ClientRectangle = gameListRectangle;
            lbGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameList.DoubleLeftClick += (_, _) => JoinSelectedGameAsync().HandleTask();
            lbGameList.AllowMultiLineItems = false;
            lbGameList.ClientRectangleUpdated += GameList_ClientRectangleUpdated;

            lbPlayerList = new PlayerListBox(WindowManager, gameCollection);
            lbPlayerList.Name = nameof(lbPlayerList);
            lbPlayerList.ClientRectangle = new Rectangle(Width - 202,
                20, 190,
                btnLogout.Y - 26);
            lbPlayerList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbPlayerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbPlayerList.LineHeight = 16;
            lbPlayerList.DoubleLeftClick += LbPlayerList_DoubleLeftClick;
            lbPlayerList.RightClick += LbPlayerList_RightClick;

            globalContextMenu = new GlobalContextMenu(WindowManager, connectionManager, cncnetUserData, pmWindow);
            globalContextMenu.JoinEvent += (_, args) => JoinUserAsync(args.IrcUser, connectionManager.MainChannel).HandleTask();

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = nameof(lbChatMessages);
            lbChatMessages.ClientRectangle = new Rectangle(lbGameList.Right + 12, lbGameList.Y,
                lbPlayerList.X - lbGameList.Right - 24, lbPlayerList.Height);
            lbChatMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;
            lbChatMessages.LeftClick += (_, _) => lbGameList.SelectedIndex = -1;
            lbChatMessages.RightClick += LbChatMessages_RightClick;

            tbChatInput = new XNAChatTextBox(WindowManager);
            tbChatInput.Name = nameof(tbChatInput);
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                btnNewGame.Y, lbChatMessages.Width,
                btnNewGame.Height);
            tbChatInput.Suggestion = "Type here to chat...".L10N("Client:Main:ChatHere");
            tbChatInput.Enabled = false;
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += (_, _) => TbChatInput_EnterPressedAsync().HandleTask();

            lblColor = new XNALabel(WindowManager);
            lblColor.Name = nameof(lblColor);
            lblColor.ClientRectangle = new Rectangle(lbChatMessages.X, 14, 0, 0);
            lblColor.FontIndex = 1;
            lblColor.Text = "YOUR COLOR:".L10N("Client:Main:YourColor");

            ddColor = new XNAClientDropDown(WindowManager);
            ddColor.Name = nameof(ddColor);
            ddColor.ClientRectangle = new Rectangle(lblColor.X + 95, 12,
                150, 21);

            foreach (IRCColor color in connectionManager.GetIRCColors())
            {
                if (!color.Selectable)
                    continue;

                XNADropDownItem ddItem = new XNADropDownItem();
                ddItem.Text = color.Name;
                ddItem.TextColor = color.XnaColor;
                ddItem.Tag = color;

                ddColor.AddItem(ddItem);
            }

            int selectedColor = UserINISettings.Instance.ChatColor;

            ddColor.SelectedIndex = selectedColor >= ddColor.Items.Count || selectedColor < 0
                ? ClientConfiguration.Instance.DefaultPersonalChatColorIndex :
                selectedColor;
            SetChatColor();
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;

            ddCurrentChannel = new XNAClientDropDown(WindowManager);
            ddCurrentChannel.Name = nameof(ddCurrentChannel);
            ddCurrentChannel.ClientRectangle = new Rectangle(
                lbChatMessages.Right - 200,
                ddColor.Y, 200, 21);
            ddCurrentChannel.SelectedIndexChanged += (_, _) => DdCurrentChannel_SelectedIndexChangedAsync().HandleTask();
            ddCurrentChannel.AllowDropDown = false;

            lblCurrentChannel = new XNALabel(WindowManager);
            lblCurrentChannel.Name = nameof(lblCurrentChannel);
            lblCurrentChannel.ClientRectangle = new Rectangle(
                ddCurrentChannel.X - 150,
                ddCurrentChannel.Y + 2, 0, 0);
            lblCurrentChannel.FontIndex = 1;
            lblCurrentChannel.Text = "CURRENT CHANNEL:".L10N("Client:Main:CurrentChannel");

            lblOnline = new XNALabel(WindowManager);
            lblOnline.Name = nameof(lblOnline);
            lblOnline.ClientRectangle = new Rectangle(310, 14, 0, 0);
            lblOnline.Text = "Online:".L10N("Client:Main:OnlineLabel");
            lblOnline.FontIndex = 1;
            lblOnline.Disable();

            lblOnlineCount = new XNALabel(WindowManager);
            lblOnlineCount.Name = nameof(lblOnlineCount);
            lblOnlineCount.ClientRectangle = new Rectangle(lblOnline.X + 50, 14, 0, 0);
            lblOnlineCount.FontIndex = 1;
            lblOnlineCount.Disable();

            tbGameSearch = new XNASuggestionTextBox(WindowManager);
            tbGameSearch.Name = nameof(tbGameSearch);
            tbGameSearch.ClientRectangle = new Rectangle(lbGameList.X, 12, lbGameList.Width - 62, 21);
            tbGameSearch.Suggestion = "Filter by name, map, game mode, player...".L10N("Client:Main:FilterByBlahBlah");
            tbGameSearch.MaximumTextLength = 64;
            tbGameSearch.InputReceived += TbGameSearch_InputReceived;
            tbGameSearch.Disable();

            btnGameSortAlpha = new XNAClientStateButton<SortDirection>(WindowManager, new Dictionary<SortDirection, Texture2D>()
            {
                { SortDirection.None, AssetLoader.LoadTexture("sortAlphaNone.png") },
                { SortDirection.Asc, AssetLoader.LoadTexture("sortAlphaAsc.png") },
                { SortDirection.Desc, AssetLoader.LoadTexture("sortAlphaDesc.png") },
            });
            btnGameSortAlpha.Name = nameof(btnGameSortAlpha);
            btnGameSortAlpha.ClientRectangle = new Rectangle(
                tbGameSearch.X + tbGameSearch.Width + 10, tbGameSearch.Y,
                21, 21);
            btnGameSortAlpha.LeftClick += BtnGameSortAlpha_LeftClick;
            btnGameSortAlpha.SetToolTipText("Sort Games Alphabetically".L10N("Client:Main:SortAlphabet"));
            RefreshGameSortAlphaBtn();

            btnGameFilterOptions = new XNAClientToggleButton(WindowManager);
            btnGameFilterOptions.Name = nameof(btnGameFilterOptions);
            btnGameFilterOptions.ClientRectangle = new Rectangle(
                btnGameSortAlpha.X + btnGameSortAlpha.Width + 10, tbGameSearch.Y,
                21, 21);
            btnGameFilterOptions.CheckedTexture = AssetLoader.LoadTexture("filterActive.png");
            btnGameFilterOptions.UncheckedTexture = AssetLoader.LoadTexture("filterInactive.png");
            btnGameFilterOptions.LeftClick += BtnGameFilterOptions_LeftClick;
            btnGameFilterOptions.SetToolTipText("Game Filters".L10N("Client:Main:GameFilters"));
            RefreshGameFiltersBtn();

            InitializeGameList();

            AddChild(btnNewGame);
            AddChild(btnJoinGame);
            AddChild(btnLogout);
            AddChild(lbPlayerList);
            AddChild(lbChatMessages);
            AddChild(lbGameList);
            AddChild(panelGameFilters);
            AddChild(tbChatInput);
            AddChild(lblColor);
            AddChild(ddColor);
            AddChild(lblCurrentChannel);
            AddChild(ddCurrentChannel);
            AddChild(globalContextMenu);
            AddChild(lblOnline);
            AddChild(lblOnlineCount);
            AddChild(tbGameSearch);
            AddChild(btnGameSortAlpha);
            AddChild(btnGameFilterOptions);

            panelGameFilters.VisibleChanged += GameFiltersPanel_VisibleChanged;

            CnCNetPlayerCountTask.CnCNetGameCountUpdated += OnCnCNetGameCountUpdated;

            gameChannel_UserAddedFunc = (sender, e) => GameChannel_UserAddedAsync(sender, e).HandleTask();
            gameChannel_InvalidPasswordEntered_LoadedGameFunc = (sender, _) => GameChannel_InvalidPasswordEntered_LoadedGameAsync(sender).HandleTask();
            gameLoadingChannel_UserAddedFunc = (sender, e) => GameLoadingChannel_UserAddedAsync(sender, e).HandleTask();
            gameChannel_InvalidPasswordEntered_NewGameFunc = (sender, _) => GameChannel_InvalidPasswordEntered_NewGameAsync(sender).HandleTask();
            gameChannel_InviteOnlyErrorOnJoinFunc = (sender, _) => OnGameLocked(sender).HandleTask();
            gameChannel_ChannelFullFunc = (sender, _) => OnGameLocked(sender).HandleTask();
            gameChannel_TargetChangeTooFastFunc = (sender, e) => GameChannel_TargetChangeTooFastAsync(sender, e).HandleTask();

            pmWindow.SetJoinUserAction((user, messageView) => JoinUserAsync(user, messageView).HandleTask());

            base.Initialize();

            WindowManager.CenterControlOnScreen(this);

            PostUIInit();
        }

        private void BtnGameSortAlpha_LeftClick(object sender, EventArgs e)
        {
            UserINISettings.Instance.SortState.Value = (int)btnGameSortAlpha.GetState();

            RefreshGameSortAlphaBtn();
            SortAndRefreshHostedGames();
            UserINISettings.Instance.SaveSettings();
        }

        private void SortAndRefreshHostedGames()
        {
            lbGameList.SortAndRefreshHostedGames();
        }

        private void BtnGameFilterOptions_LeftClick(object sender, EventArgs e)
        {
            if (panelGameFilters.Visible)
                panelGameFilters.Cancel();
            else
                panelGameFilters.Show();
        }

        private void RefreshGameSortAlphaBtn()
        {
            if (Enum.IsDefined(typeof(SortDirection), UserINISettings.Instance.SortState.Value))
                btnGameSortAlpha.SetState((SortDirection)UserINISettings.Instance.SortState.Value);
        }

        private void RefreshGameFiltersBtn()
        {
            btnGameFilterOptions.Checked = UserINISettings.Instance.IsGameFiltersApplied();
        }

        private void GameFiltersPanel_VisibleChanged(object sender, EventArgs e)
        {
            if (panelGameFilters.Visible)
                return;

            RefreshGameFiltersBtn();
            SortAndRefreshHostedGames();
        }

        private void TbGameSearch_InputReceived(object sender, EventArgs e)
        {
            SortAndRefreshHostedGames();
            lbGameList.ViewTop = 0;
        }

        private bool HostedGameMatches(GenericHostedGame hg)
        {
            // friends list takes priority over other filters below
            if (UserINISettings.Instance.ShowFriendGamesOnly)
                return hg.Players.Any(cncnetUserData.IsFriend);

            if (UserINISettings.Instance.HideLockedGames.Value && hg.Locked)
                return false;

            if (UserINISettings.Instance.HideIncompatibleGames.Value && hg.Incompatible)
                return false;

            if (UserINISettings.Instance.HidePasswordedGames.Value && hg.Passworded)
                return false;

            if (hg.MaxPlayers > UserINISettings.Instance.MaxPlayerCount.Value)
                return false;

            string textUpper = tbGameSearch?.Text?.ToUpperInvariant();

            string translatedGameMode = hg.GameMode.L10N($"INI:GameModes:{hg.GameMode}:UIName", notify: false);

            string translatedMapName = mapLoader.TranslatedMapNames.ContainsKey(hg.Map)
                ? mapLoader.TranslatedMapNames[hg.Map]
                : null;

            return
                string.IsNullOrWhiteSpace(tbGameSearch?.Text) ||
                tbGameSearch.Text == tbGameSearch.Suggestion ||
                hg.RoomName.ToUpperInvariant().Contains(textUpper) ||
                hg.GameMode.ToUpperInvariant().Equals(textUpper, StringComparison.Ordinal) ||
                translatedGameMode.ToUpperInvariant().Equals(textUpper, StringComparison.Ordinal) ||
                hg.Map.ToUpperInvariant().Contains(textUpper) ||
                (translatedMapName is not null && translatedMapName.ToUpperInvariant().Contains(textUpper)) ||
                hg.Players.Any(pl => pl.ToUpperInvariant().Equals(textUpper, StringComparison.Ordinal));
        }


        private void OnCnCNetGameCountUpdated(object sender, PlayerCountEventArgs e) => UpdateOnlineCount(e.PlayerCount);

        private void UpdateOnlineCount(int playerCount) => lblOnlineCount.Text = playerCount.ToString();

        private void InitializeGameList()
        {
            int i = 0;

            foreach (var game in gameCollection.GameList)
            {
                if (!game.Supported || string.IsNullOrEmpty(game.ChatChannel))
                {
                    i++;
                    continue;
                }

                var item = new XNADropDownItem();
                item.Text = game.UIName;
                item.Texture = game.Texture;

                ddCurrentChannel.AddItem(item);

                var chatChannel = connectionManager.FindChannel(game.ChatChannel);

                if (chatChannel == null)
                {
                    chatChannel = connectionManager.CreateChannel(game.UIName, game.ChatChannel,
                        true, true, "ra1-derp");
                    connectionManager.AddChannel(chatChannel);
                }

                item.Tag = chatChannel;

                if (!string.IsNullOrEmpty(game.GameBroadcastChannel))
                {
                    var gameBroadcastChannel = connectionManager.FindChannel(game.GameBroadcastChannel);

                    if (gameBroadcastChannel == null)
                    {
                        gameBroadcastChannel = connectionManager.CreateChannel(
                            string.Format("{0} Broadcast Channel".L10N("Client:Main:BroadcastChannel"), game.UIName),
                            game.GameBroadcastChannel, true, false, null);
                        connectionManager.AddChannel(gameBroadcastChannel);
                    }

                    gameBroadcastChannel.CTCPReceived += GameBroadcastChannel_CTCPReceived;
                    gameBroadcastChannel.UserLeft += GameBroadcastChannel_UserLeftOrQuit;
                    gameBroadcastChannel.UserQuitIRC += GameBroadcastChannel_UserLeftOrQuit;
                    gameBroadcastChannel.UserKicked += GameBroadcastChannel_UserLeftOrQuit;
                }

                if (game.InternalName.ToUpper() == localGameID.ToUpper())
                {
                    ddCurrentChannel.SelectedIndex = i;
                }

                i++;
            }

            if (connectionManager.MainChannel == null)
            {
                // Set CnCNet channel as main channel if no channel found
                ddCurrentChannel.SelectedIndex = ddCurrentChannel.Items.Count - 1;
            }
        }

        private void PostUIInit()
        {
            sndGameCreated = new EnhancedSoundEffect("gamecreated.wav");
            sndGameInviteReceived = new EnhancedSoundEffect("pm.wav");

            var assembly = Assembly.GetAssembly(typeof(GameCollection));
            using Stream unknownIconStream = assembly.GetManifestResourceStream("ClientCore.Resources.unknownicon.png");

            unknownGameIcon = AssetLoader.TextureFromImage(Image.Load(unknownIconStream));

            connectionManager.WelcomeMessageReceived += (_, _) => ConnectionManager_WelcomeMessageReceivedAsync().HandleTask();
            connectionManager.Disconnected += ConnectionManager_Disconnected;
            connectionManager.PrivateCTCPReceived += ConnectionManager_PrivateCTCPReceived;

            cncnetUserData.UserFriendToggled += RefreshPlayerList;
            cncnetUserData.UserIgnoreToggled += RefreshPlayerList;

            gameCreationPanel = new DarkeningPanel(WindowManager);
            AddChild(gameCreationPanel);

            GameCreationWindow gcw = new GameCreationWindow(WindowManager, tunnelHandler);
            gameCreationPanel.AddChild(gcw);
            gameCreationPanel.Tag = gcw;
            gcw.Cancelled += Gcw_Cancelled;
            gcw.GameCreated += (_, e) => Gcw_GameCreatedAsync(e).HandleTask();
            gcw.LoadedGameCreated += (_, e) => Gcw_LoadedGameCreatedAsync(e).HandleTask();

            gameCreationPanel.Hide();

            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, Renderer.GetSafeString(
                    string.Format("*** CnCNet Client version {0} ***".L10N("Client:Main:CnCNetClientVersionMessage"), Assembly.GetAssembly(typeof(CnCNetLobby)).GetName().Version),
                    lbChatMessages.FontIndex)));

            connectionManager.BannedFromChannel += (_, e) => ConnectionManager_BannedFromChannelAsync(e).HandleTask();

            loginWindow = new CnCNetLoginWindow(WindowManager);
            loginWindow.Connect += LoginWindow_Connect;
            loginWindow.Cancelled += LoginWindow_Cancelled;

            var loginWindowPanel = new DarkeningPanel(WindowManager);
            loginWindowPanel.Alpha = 0.0f;

            AddChild(loginWindowPanel);
            loginWindowPanel.AddChild(loginWindow);
            loginWindow.Disable();

            passwordRequestWindow = new PasswordRequestWindow(WindowManager, pmWindow);
            passwordRequestWindow.PasswordEntered += (_, hostedGame) => JoinGameAsync(hostedGame.HostedGame, hostedGame.Password).HandleTask();

            var passwordRequestWindowPanel = new DarkeningPanel(WindowManager);
            passwordRequestWindowPanel.Alpha = 0.0f;
            AddChild(passwordRequestWindowPanel);
            passwordRequestWindowPanel.AddChild(passwordRequestWindow);
            passwordRequestWindow.Disable();

            gameLobby.GameLeft += GameLobby_GameLeft;
            gameLoadingLobby.GameLeft += GameLoadingLobby_GameLeft;

            UserINISettings.Instance.SettingsSaved += (_, _) => Instance_SettingsSavedAsync().HandleTask();
            GameProcessLogic.GameProcessStarted += () => SharedUILogic_GameProcessStartedAsync().HandleTask();
            GameProcessLogic.GameProcessExited += () => SharedUILogic_GameProcessExitedAsync().HandleTask();
        }

        /// <summary>
        /// Displays a message when the IRC server has informed that the local user
        /// has been banned from a channel that they're attempting to join.
        /// </summary>
        private async ValueTask ConnectionManager_BannedFromChannelAsync(ChannelEventArgs e)
        {
            var game = lbGameList.HostedGames.Find(hg => ((HostedCnCNetGame)hg).ChannelName == e.ChannelName);

            if (game == null)
            {
                var chatChannel = connectionManager.FindChannel(e.ChannelName);
                chatChannel?.AddMessage(new ChatMessage(Color.White, string.Format(
                    "Cannot join chat channel {0}, you're banned!".L10N("Client:Main:PlayerBannedByChannel"), chatChannel.UIName)));
                return;
            }

            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, string.Format(
                "Cannot join game {0}, you've been banned by the game host!".L10N("Client:Main:PlayerBannedByHost"), game.RoomName)));

            isJoiningGame = false;
            if (gameOfLastJoinAttempt != null)
            {
                if (gameOfLastJoinAttempt.IsLoadedGame)
                    await gameLoadingLobby.ClearAsync().ConfigureAwait(false);
                else
                    await gameLobby.ClearAsync(false).ConfigureAwait(false);
            }
        }

        private ValueTask SharedUILogic_GameProcessStartedAsync()
            => connectionManager.SendCustomMessageAsync(new QueuedMessage(
                IRCCommands.AWAY + " " + (char)58 + "In-game",
                QueuedMessageType.SYSTEM_MESSAGE,
                0));

        private ValueTask SharedUILogic_GameProcessExitedAsync()
            => connectionManager.SendCustomMessageAsync(new QueuedMessage(IRCCommands.AWAY, QueuedMessageType.SYSTEM_MESSAGE, 0));

        private async ValueTask Instance_SettingsSavedAsync()
        {
            if (!connectionManager.IsConnected)
                return;

            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported)
                    continue;

                if (game.InternalName.ToUpper() == localGameID)
                    continue;

                if (followedGames.Contains(game.InternalName) &&
                    !UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
                {
                    await connectionManager.FindChannel(game.GameBroadcastChannel).LeaveAsync().ConfigureAwait(false);
                    followedGames.Remove(game.InternalName);
                }
                else if (!followedGames.Contains(game.InternalName) &&
                    UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
                {
                    await connectionManager.FindChannel(game.GameBroadcastChannel).JoinAsync().ConfigureAwait(false);
                    followedGames.Add(game.InternalName);
                }
            }
        }

        private void LbPlayerList_RightClick(object sender, EventArgs e)
        {
            lbPlayerList.SelectedIndex = lbPlayerList.HoveredIndex;

            if (lbPlayerList.SelectedIndex < 0 ||
                lbPlayerList.SelectedIndex >= lbPlayerList.Items.Count)
            {
                return;
            }

            var user = (ChannelUser)lbPlayerList.SelectedItem.Tag;

            globalContextMenu.Show(user, GetCursorPoint());
        }

        private void LbChatMessages_RightClick(object sender, EventArgs e)
        {
            var item = lbChatMessages.HoveredItem;
            var chatMessage = item?.Tag as ChatMessage;

            ShowPlayerMessageContextMenu(chatMessage);
        }

        private void ShowPlayerMessageContextMenu(ChatMessage chatMessage)
        {
            lbChatMessages.SelectedIndex = lbChatMessages.HoveredIndex;

            globalContextMenu.Show(chatMessage, GetCursorPoint());
        }

        /// <summary>
        /// Enables private messaging by PM'ing a user in the player list.
        /// </summary>
        private void LbPlayerList_DoubleLeftClick(object sender, EventArgs e)
        {
            if (lbPlayerList.SelectedItem == null)
                return;

            var channelUser = (ChannelUser)lbPlayerList.SelectedItem.Tag;

            pmWindow.InitPM(channelUser.IRCUser.Name);
        }

        /// <summary>
        /// Hides the login dialog once the user has hit Connect on that dialog.
        /// </summary>
        private void LoginWindow_Connect(object sender, EventArgs e)
        {
            connectionManager.Connect();
            loginWindow.Disable();

            SetLogOutButtonText();
        }

        /// <summary>
        /// Hides the login window and the CnCNet lobby if the user
        /// cancels connecting to CnCNet in the login dialog.
        /// </summary>
        private void LoginWindow_Cancelled(object sender, EventArgs e)
        {
            topBar.SwitchToPrimary();
            loginWindow.Disable();
        }

        private void GameLoadingLobby_GameLeft(object sender, EventArgs e)
        {
            topBar.SwitchToSecondary();
            isInGameRoom = false;
            SetLogOutButtonText();

            // keep the friends window up to date so it can disable the Invite option
            pmWindow.ClearInviteChannelInfo();
        }

        private void GameLobby_GameLeft(object sender, EventArgs e)
        {
            topBar.SwitchToSecondary();
            isInGameRoom = false;
            SetLogOutButtonText();

            // keep the friends window up to date so it can disable the Invite option
            pmWindow.ClearInviteChannelInfo();
        }

        private void SetLogOutButtonText()
        {
            if (isInGameRoom)
            {
                btnLogout.Text = "Game Lobby".L10N("Client:Main:GameLobby");
                return;
            }

            if (UserINISettings.Instance.PersistentMode)
            {
                btnLogout.Text = "Main Menu".L10N("Client:Main:MainMenu");
                return;
            }

            btnLogout.Text = "Log Out".L10N("Client:Main:LogOut");
        }

        private string GetJoinGameErrorBase()
        {
            if (isJoiningGame)
                return "Cannot join game - joining game in progress. If you believe this is an error, please log out and back in.".L10N("Client:Main:JoinGameErrorInProgress");

            if (ProgramConstants.IsInGame)
                return "Cannot join game while the main game executable is running.".L10N("Client:Main:JoinGameErrorGameRunning");

            return null;
        }
        /// <summary>
        /// Checks if the user can join a game.
        /// Returns null if the user can, otherwise returns an error message
        /// that tells the reason why the user cannot join the game.
        /// </summary>
        /// <param name="gameIndex">The index of the game in the game list box.</param>
        private string GetJoinGameErrorByIndex(int gameIndex)
        {
            if (gameIndex < 0 || gameIndex >= lbGameList.HostedGames.Count)
                return "Invalid game index".L10N("Client:Main:InvalidGameIndex");

            return GetJoinGameErrorBase();
        }

        /// <summary>
        /// Returns an error message if game is not join-able, otherwise null.
        /// </summary>
        /// <param name="hg"></param>
        /// <returns></returns>
        private string GetJoinGameError(HostedCnCNetGame hg)
        {
            if (hg.Game.InternalName.ToUpper() != localGameID.ToUpper())
                return string.Format("The selected game is for {0}!".L10N("Client:Main:GameIsOfPurpose"), gameCollection.GetGameNameFromInternalName(hg.Game.InternalName));

            if (hg.Incompatible && ClientConfiguration.Instance.DisallowJoiningIncompatibleGames)
                return "Cannot join game. The host is on a different game version than you.".L10N("Client:Main:DisallowJoiningIncompatibleGames");

            if (hg.Locked)
                return "The selected game is locked!".L10N("Client:Main:GameLocked");

            if (hg.IsLoadedGame && !hg.Players.Contains(ProgramConstants.PLAYERNAME))
                return "You do not exist in the saved game!".L10N("Client:Main:NotInSavedGame");

            return GetJoinGameErrorBase();
        }

        private async ValueTask JoinSelectedGameAsync()
        {
            var listedGame = (HostedCnCNetGame)lbGameList.SelectedItem?.Tag;
            if (listedGame == null)
                return;
            var hostedGameIndex = lbGameList.HostedGames.IndexOf(listedGame);
            await JoinGameByIndexAsync(hostedGameIndex, string.Empty).ConfigureAwait(false);
        }

        private async ValueTask<bool> JoinGameByIndexAsync(int gameIndex, string password)
        {
            string error = GetJoinGameErrorByIndex(gameIndex);
            if (!string.IsNullOrEmpty(error))
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, error));
                return false;
            }

            return await JoinGameAsync((HostedCnCNetGame)lbGameList.HostedGames[gameIndex], password, connectionManager.MainChannel).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempt to join a game.
        /// </summary>
        /// <param name="hg">The game to join.</param>
        /// <param name="password">The password to join with.</param>
        /// <param name="messageView">The message view/list to write error messages to.</param>
        private async ValueTask<bool> JoinGameAsync(HostedCnCNetGame hg, string password, IMessageView messageView)
        {
            string error = GetJoinGameError(hg);
            if (!string.IsNullOrEmpty(error))
            {
                messageView.AddMessage(new ChatMessage(Color.White, error));
                return false;
            }

            if (isInGameRoom)
            {
                topBar.SwitchToPrimary();
                return false;
            }

            // if (hg.GameVersion != ProgramConstants.GAME_VERSION)
            // TODO Show warning

            if (hg.Passworded)
            {
                // only display password dialog if we've not been supplied with a password (invite)
                if (string.IsNullOrEmpty(password))
                {
                    passwordRequestWindow.SetHostedGame(hg);
                    passwordRequestWindow.Enable();
                    return true;
                }
            }
            else
            {
                if (!hg.IsLoadedGame)
                {
                    password = Utilities.CalculateSHA1ForString
                        (hg.ChannelName + hg.RoomName)[..10];
                }
                else
                {
                    IniFile spawnSGIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));
                    password = Utilities.CalculateSHA1ForString(
                        spawnSGIni.GetStringValue("Settings", "GameID", string.Empty))[..10];
                }
            }

            await JoinGameAsync(hg, password).ConfigureAwait(false);

            return true;
        }

        private async ValueTask JoinGameAsync(HostedCnCNetGame hg, string password)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White,
                string.Format("Attempting to join game {0} ...".L10N("Client:Main:AttemptJoin"), hg.RoomName)));
            isJoiningGame = true;
            gameOfLastJoinAttempt = hg;

            Channel gameChannel = connectionManager.CreateChannel(hg.RoomName, hg.ChannelName, false, true, password);
            connectionManager.AddChannel(gameChannel);

            if (hg.IsLoadedGame)
            {
                gameLoadingLobby.SetUp(false, hg.TunnelServer, gameChannel, hg.HostName);
                gameChannel.UserAdded += gameLoadingChannel_UserAddedFunc;
                gameChannel.InvalidPasswordEntered += gameChannel_InvalidPasswordEntered_LoadedGameFunc;
            }
            else
            {
                await gameLobby.SetUpAsync(gameChannel, false, hg.MaxPlayers, hg.TunnelServer, hg.HostName, hg.Passworded).ConfigureAwait(false);
                gameChannel.UserAdded += gameChannel_UserAddedFunc;
                gameChannel.InvalidPasswordEntered += gameChannel_InvalidPasswordEntered_NewGameFunc;
                gameChannel.InviteOnlyErrorOnJoin += gameChannel_InviteOnlyErrorOnJoinFunc;
                gameChannel.ChannelFull += gameChannel_ChannelFullFunc;
                gameChannel.TargetChangeTooFast += gameChannel_TargetChangeTooFastFunc;
            }

            await connectionManager.SendCustomMessageAsync(new QueuedMessage(IRCCommands.JOIN + " " + hg.ChannelName + " " + password,
                QueuedMessageType.INSTANT_MESSAGE, 0)).ConfigureAwait(false);
        }

        private async ValueTask GameChannel_TargetChangeTooFastAsync(object sender, MessageEventArgs e)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, e.Message));
            await ClearGameJoinAttemptAsync((Channel)sender).ConfigureAwait(false);
        }

        private async ValueTask OnGameLocked(object sender)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, "The selected game is locked!".L10N("Client:Main:GameLocked")));
            var channel = (Channel)sender;

            var game = FindGameByChannelName(channel.ChannelName);
            if (game != null)
            {
                game.Locked = true;
                SortAndRefreshHostedGames();
            }

            await ClearGameJoinAttemptAsync((Channel)sender).ConfigureAwait(false);
        }

        private HostedCnCNetGame FindGameByChannelName(string channelName)
        {
            var game = lbGameList.HostedGames.Find(hg => ((HostedCnCNetGame)hg).ChannelName == channelName);
            if (game == null)
                return null;

            return (HostedCnCNetGame)game;
        }

        private async ValueTask GameChannel_InvalidPasswordEntered_NewGameAsync(object sender)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, "Incorrect password!".L10N("Client:Main:PasswordWrong")));
            await ClearGameJoinAttemptAsync((Channel)sender).ConfigureAwait(false);
        }

        private async ValueTask GameChannel_UserAddedAsync(object sender, ChannelUserEventArgs e)
        {
            Channel gameChannel = (Channel)sender;

            if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
            {
                ClearGameChannelEvents(gameChannel);
                await gameLobby.OnJoinedAsync().ConfigureAwait(false);
                isInGameRoom = true;
                SetLogOutButtonText();
            }
        }

        private async ValueTask ClearGameJoinAttemptAsync(Channel channel)
        {
            ClearGameChannelEvents(channel);
            await gameLobby.ClearAsync(false).ConfigureAwait(false);
        }

        private void ClearGameChannelEvents(Channel channel)
        {
            channel.UserAdded -= gameChannel_UserAddedFunc;
            channel.InvalidPasswordEntered -= gameChannel_InvalidPasswordEntered_NewGameFunc;
            channel.InviteOnlyErrorOnJoin -= gameChannel_InviteOnlyErrorOnJoinFunc;
            channel.ChannelFull -= gameChannel_ChannelFullFunc;
            channel.TargetChangeTooFast -= gameChannel_TargetChangeTooFastFunc;
            isJoiningGame = false;
        }

        private void BtnNewGame_LeftClick(object sender, EventArgs e)
        {
            if (isInGameRoom)
            {
                topBar.SwitchToPrimary();
                return;
            }

            gameCreationPanel.Show();
            var gcw = (GameCreationWindow)gameCreationPanel.Tag;

            gcw.Refresh();
        }

        private async ValueTask Gcw_GameCreatedAsync(GameCreationEventArgs e)
        {
            if (gameLobby.Enabled || gameLoadingLobby.Enabled)
                return;

            string channelName = RandomizeChannelName();
            string password = e.Password;
            bool isCustomPassword = true;
            if (string.IsNullOrEmpty(password))
            {
                password = Utilities.CalculateSHA1ForString(channelName + e.GameRoomName)[..10];
                isCustomPassword = false;
            }

            Channel gameChannel = connectionManager.CreateChannel(e.GameRoomName, channelName, false, true, password);
            connectionManager.AddChannel(gameChannel);
            await gameLobby.SetUpAsync(gameChannel, true, e.MaxPlayers, e.Tunnel, ProgramConstants.PLAYERNAME, isCustomPassword).ConfigureAwait(false);
            gameChannel.UserAdded += gameChannel_UserAddedFunc;
            await connectionManager.SendCustomMessageAsync(new QueuedMessage(IRCCommands.JOIN + " " + channelName + " " + password,
                QueuedMessageType.INSTANT_MESSAGE, 0)).ConfigureAwait(false);
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White,
               string.Format("Creating a game named {0} ...".L10N("Client:Main:CreateGameNamed"), e.GameRoomName)));

            gameCreationPanel.Hide();

            // update the friends window so it can enable the Invite option
            pmWindow.SetInviteChannelInfo(channelName, e.GameRoomName, string.IsNullOrEmpty(e.Password) ? string.Empty : e.Password);
        }

        private async ValueTask Gcw_LoadedGameCreatedAsync(GameCreationEventArgs e)
        {
            if (gameLobby.Enabled || gameLoadingLobby.Enabled)
                return;

            string channelName = RandomizeChannelName();

            Channel gameLoadingChannel = connectionManager.CreateChannel(e.GameRoomName, channelName, false, true, e.Password);
            connectionManager.AddChannel(gameLoadingChannel);
            gameLoadingLobby.SetUp(true, e.Tunnel, gameLoadingChannel, ProgramConstants.PLAYERNAME);
            gameLoadingChannel.UserAdded += gameLoadingChannel_UserAddedFunc;
            await connectionManager.SendCustomMessageAsync(new QueuedMessage(IRCCommands.JOIN + " " + channelName + " " + e.Password,
                QueuedMessageType.INSTANT_MESSAGE, 0)).ConfigureAwait(false);
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White,
               string.Format("Creating a game named {0} ...".L10N("Client:Main:CreateGameNamed"), e.GameRoomName)));

            gameCreationPanel.Hide();

            // update the friends window so it can enable the Invite option
            pmWindow.SetInviteChannelInfo(channelName, e.GameRoomName, string.IsNullOrEmpty(e.Password) ? string.Empty : e.Password);
        }

        private async ValueTask GameChannel_InvalidPasswordEntered_LoadedGameAsync(object sender)
        {
            var channel = (Channel)sender;
            channel.UserAdded -= gameLoadingChannel_UserAddedFunc;
            channel.InvalidPasswordEntered -= gameChannel_InvalidPasswordEntered_LoadedGameFunc;
            await gameLoadingLobby.ClearAsync().ConfigureAwait(false);
            isJoiningGame = false;
        }

        private async ValueTask GameLoadingChannel_UserAddedAsync(object sender, ChannelUserEventArgs e)
        {
            Channel gameLoadingChannel = (Channel)sender;

            if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
            {
                gameLoadingChannel.UserAdded -= gameLoadingChannel_UserAddedFunc;
                gameLoadingChannel.InvalidPasswordEntered -= gameChannel_InvalidPasswordEntered_LoadedGameFunc;

                await gameLoadingLobby.OnJoinedAsync().ConfigureAwait(false);
                isInGameRoom = true;
                isJoiningGame = false;
            }
        }

        /// <summary>
        /// Generates and returns a random, unused cannel name.
        /// </summary>
        /// <returns>A random channel name based on the currently played game.</returns>
        private string RandomizeChannelName()
        {
            while (true)
            {
                string channelName = string.Format("{0}-game{1}".L10N("Client:Main:RamdomChannelName"), gameCollection.GetGameChatChannelNameFromIdentifier(localGameID), new Random().Next(1000000, 9999999));
                int index = lbGameList.HostedGames.FindIndex(c => ((HostedCnCNetGame)c).ChannelName == channelName);
                if (index == -1)
                    return channelName;
            }
        }

        private void Gcw_Cancelled(object sender, EventArgs e) => gameCreationPanel.Hide();

        private async ValueTask TbChatInput_EnterPressedAsync()
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            IRCColor selectedColor = (IRCColor)ddColor.SelectedItem.Tag;

            await currentChatChannel.SendChatMessageAsync(tbChatInput.Text, selectedColor).ConfigureAwait(false);

            tbChatInput.Text = string.Empty;
        }

        private void SetChatColor()
        {
            IRCColor selectedColor = (IRCColor)ddColor.SelectedItem.Tag;
            tbChatInput.TextColor = selectedColor.XnaColor;
            gameLobby.ChangeChatColor(selectedColor);
            gameLoadingLobby.ChangeChatColor(selectedColor);
            UserINISettings.Instance.ChatColor.Value = ddColor.SelectedIndex;
        }

        private void DdColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetChatColor();
            UserINISettings.Instance.SaveSettings();
        }

        private void ConnectionManager_Disconnected(object sender, EventArgs e)
        {
            btnNewGame.AllowClick = false;
            btnJoinGame.AllowClick = false;
            ddCurrentChannel.AllowDropDown = false;
            tbChatInput.Enabled = false;
            lbPlayerList.Clear();

            lbGameList.ClearGames();
            followedGames.Clear();

            gameCreationPanel.Hide();

            // Switch channel to default
            if (localGame != null)
            {
                int gameIndex = ddCurrentChannel.Items.FindIndex(i => i.Text == localGame.UIName);
                if (gameIndex > -1)
                    ddCurrentChannel.SelectedIndex = gameIndex;
            }

            gameCheckCancellation?.Cancel();
            gameCheckCancellation?.Dispose();
        }

        private async ValueTask ConnectionManager_WelcomeMessageReceivedAsync()
        {
            btnNewGame.AllowClick = true;
            btnJoinGame.AllowClick = true;
            ddCurrentChannel.AllowDropDown = true;
            tbChatInput.Enabled = true;

            Channel cncnetChannel = connectionManager.FindChannel("#cncnet");
            await cncnetChannel.JoinAsync().ConfigureAwait(false);

            string localGameChatChannelName = gameCollection.GetGameChatChannelNameFromIdentifier(localGameID);
            await connectionManager.FindChannel(localGameChatChannelName).JoinAsync().ConfigureAwait(false);

            string localGameBroadcastChannel = gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGameID);
            await connectionManager.FindChannel(localGameBroadcastChannel).JoinAsync().ConfigureAwait(false);

            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported)
                    continue;

                if (game.InternalName.ToUpper() != localGameID)
                {
                    if (UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
                    {
                        await connectionManager.FindChannel(game.GameBroadcastChannel).JoinAsync().ConfigureAwait(false);
                        followedGames.Add(game.InternalName);
                    }
                }
            }

            gameCheckCancellation = new CancellationTokenSource();
            CnCNetGameCheck.RunServiceAsync(gameCheckCancellation.Token).HandleTask();
        }

        private void ConnectionManager_PrivateCTCPReceived(object sender, PrivateCTCPEventArgs e)
        {
            foreach (CommandHandlerBase cmdHandler in ctcpCommandHandlers)
            {
                if (cmdHandler.Handle(e.Sender, e.Message))
                    return;
            }

            Logger.Log("Unhandled private CTCP command: " + e.Message + " from " + e.Sender);
        }

        private async ValueTask HandleGameInviteCommandAsync(string sender, string argumentsString)
        {
            // arguments are semicolon-delimited
            var arguments = argumentsString.Split(';');

            // we expect to be given a channel name, a (human-friendly) game name and optionally a password
            if (arguments.Length < 2 || arguments.Length > 3)
                return;

            string channelName = arguments[0];
            string gameName = arguments[1];
            string password = (arguments.Length == 3) ? arguments[2] : string.Empty;

            if (!CanReceiveInvitationMessagesFrom(sender))
                return;

            var gameIndex = lbGameList.HostedGames.FindIndex(hg => ((HostedCnCNetGame)hg).ChannelName == channelName);

            // also enforce user preference on whether to accept invitations from non-friends
            // this is kept separate from CanReceiveInvitationMessagesFrom() as we still
            // want to let the host know that we couldn't receive the invitation
            if (!string.IsNullOrEmpty(GetJoinGameErrorByIndex(gameIndex)) ||
                (UserINISettings.Instance.AllowGameInvitesFromFriendsOnly &&
                !cncnetUserData.IsFriend(sender)))
            {
                // let the host know that we can't accept
                // note this is not reached for the rejection case
                await connectionManager.SendCustomMessageAsync(new QueuedMessage(IRCCommands.PRIVMSG + " " + sender + " :\u0001" +
                    CnCNetCommands.GAME_INVITATION_FAILED + "\u0001",
                    QueuedMessageType.CHAT_MESSAGE, 0)).ConfigureAwait(false);

                return;
            }

            // if there's already an outstanding invitation from this user/channel combination,
            // we don't want to display another
            // we won't bother telling the host though, since their old invitation is still
            // available to us
            var invitationIdentity = new UserChannelPair(sender, channelName);

            if (invitationIndex.ContainsKey(invitationIdentity))
            {
                return;
            }

            var gameInviteChoiceBox = new ChoiceNotificationBox(WindowManager);

            WindowManager.AddAndInitializeControl(gameInviteChoiceBox);

            // show the invitation at top left; it will remain until it is acted upon or the target game is closed
            gameInviteChoiceBox.Show(
                "GAME INVITATION".L10N("Client:Main:GameInviteTitle"),
                GetUserTexture(sender),
                sender,
                string.Format("Join {0}?".L10N("Client:Main:GameInviteText"), gameName),
                "Yes".L10N("Client:Main:ButtonYes"), "No".L10N("Client:Main:ButtonNo"), 0);

            // add the invitation to the index so we can remove it if the target game is closed
            // also lets us silently ignore new invitations from the same person while this one is still outstanding
            invitationIndex[invitationIdentity] = new WeakReference(gameInviteChoiceBox);

            gameInviteChoiceBox.AffirmativeClickedAction = _ => AffirmativeClickedActionAsync(channelName, password, sender, invitationIdentity).HandleTask();

            // clean up the index as this invitation no longer exists
            gameInviteChoiceBox.NegativeClickedAction = _ => invitationIndex.Remove(invitationIdentity);

            sndGameInviteReceived.Play();
        }

        private async ValueTask AffirmativeClickedActionAsync(string channelName, string password, string sender, UserChannelPair invitationIdentity)
        {
            // if we're currently in a game lobby, first leave that channel
            if (isInGameRoom)
            {
                await gameLobby.LeaveGameLobbyAsync().ConfigureAwait(false);
            }

            // JoinGameByIndex does bounds checking so we're safe to pass -1 if the game doesn't exist
            if (!await JoinGameByIndexAsync(lbGameList.HostedGames.FindIndex(hg => ((HostedCnCNetGame)hg).ChannelName == channelName), password).ConfigureAwait(false))
            {
                XNAMessageBox.Show(WindowManager,
                    "Failed to join".L10N("Client:Main:JoinFailedTitle"),
                    string.Format("Unable to join {0}'s game. The game may be locked or closed.".L10N("Client:Main:JoinFailedText"), sender));
            }

            // clean up the index as this invitation no longer exists
            invitationIndex.Remove(invitationIdentity);
        }

        private void HandleGameInvitationFailedNotification(string sender)
        {
            if (!CanReceiveInvitationMessagesFrom(sender))
                return;

            if (isInGameRoom && !ProgramConstants.IsInGame)
            {
                gameLobby.AddWarning(
                    string.Format(("{0} could not receive your invitation. They might be in game " +
                    "or only accepting invitations from friends. Ensure your game is " +
                    "unlocked and visible in the lobby before trying again.").L10N("Client:Main:InviteNotDelivered"), sender));
            }
        }

        private async ValueTask DdCurrentChannel_SelectedIndexChangedAsync()
        {
            if (currentChatChannel != null)
            {
                currentChatChannel.UserAdded -= RefreshPlayerList;
                currentChatChannel.UserLeft -= RefreshPlayerList;
                currentChatChannel.UserQuitIRC -= RefreshPlayerList;
                currentChatChannel.UserKicked -= RefreshPlayerList;
                currentChatChannel.UserListReceived -= RefreshPlayerList;
                currentChatChannel.MessageAdded -= CurrentChatChannel_MessageAdded;
                currentChatChannel.UserGameIndexUpdated -= CurrentChatChannel_UserGameIndexUpdated;

                if (currentChatChannel.ChannelName != "#cncnet" &&
                    currentChatChannel.ChannelName != gameCollection.GetGameChatChannelNameFromIdentifier(localGameID))
                {
                    // Remove the assigned channels from the users so we don't have ghost users on the PM user list
                    currentChatChannel.Users.DoForAllUsers(user =>
                    {
                        connectionManager.RemoveChannelFromUser(user.IRCUser.Name, currentChatChannel.ChannelName);
                    });

                    await currentChatChannel.LeaveAsync().ConfigureAwait(false);
                }
            }

            currentChatChannel = (Channel)ddCurrentChannel.SelectedItem.Tag;
            currentChatChannel.UserAdded += RefreshPlayerList;
            currentChatChannel.UserLeft += RefreshPlayerList;
            currentChatChannel.UserQuitIRC += RefreshPlayerList;
            currentChatChannel.UserKicked += RefreshPlayerList;
            currentChatChannel.UserListReceived += RefreshPlayerList;
            currentChatChannel.MessageAdded += CurrentChatChannel_MessageAdded;
            currentChatChannel.UserGameIndexUpdated += CurrentChatChannel_UserGameIndexUpdated;
            connectionManager.SetMainChannel(currentChatChannel);

            lbPlayerList.TopIndex = 0;

            lbChatMessages.TopIndex = 0;
            lbChatMessages.Clear();
            currentChatChannel.Messages.ForEach(msg => AddMessageToChat(msg));

            RefreshPlayerList(this, EventArgs.Empty);

            if (currentChatChannel.ChannelName != "#cncnet" &&
                currentChatChannel.ChannelName != gameCollection.GetGameChatChannelNameFromIdentifier(localGameID))
            {
                await currentChatChannel.JoinAsync().ConfigureAwait(false);
            }
        }

        private void RefreshPlayerList(object sender, EventArgs e)
        {
            string selectedUserName = lbPlayerList.SelectedItem == null ?
                string.Empty : lbPlayerList.SelectedItem.Text;

            lbPlayerList.Clear();

            var current = currentChatChannel.Users.GetFirst();
            while (current != null)
            {
                var user = current.Value;
                user.IRCUser.IsFriend = cncnetUserData.IsFriend(user.IRCUser.Name);
                user.IRCUser.IsIgnored = cncnetUserData.IsIgnored(user.IRCUser.Ident);
                lbPlayerList.AddUser(user);
                current = current.Next;
            }

            if (selectedUserName != string.Empty)
            {
                lbPlayerList.SelectedIndex = lbPlayerList.Items.FindIndex(
                    i => i.Text == selectedUserName);
            }
        }

        /// <summary>
        /// Refreshes a single user's info on the player list.
        /// </summary>
        /// <param name="user">User on the current chat channel.</param>
        private void RefreshPlayerListUser(ChannelUser user)
        {
            user.IRCUser.IsFriend = cncnetUserData.IsFriend(user.IRCUser.Name);
            user.IRCUser.IsIgnored = cncnetUserData.IsIgnored(user.IRCUser.Ident);
            lbPlayerList.UpdateUserInfo(user);
        }

        private void CurrentChatChannel_UserGameIndexUpdated(object sender, ChannelUserEventArgs e)
        {
            var ircUser = e.User.IRCUser;
            var item = lbPlayerList.Items.Find(i => i.Text.StartsWith(ircUser.Name));

            if (ircUser.GameID < 0 || ircUser.GameID >= gameCollection.GameList.Count)
                item.Texture = unknownGameIcon;
            else
                item.Texture = gameCollection.GameList[ircUser.GameID].Texture;
        }

        private void AddMessageToChat(ChatMessage message)
        {
            if (!string.IsNullOrEmpty(message.SenderIdent) &&
                cncnetUserData.IsIgnored(message.SenderIdent) &&
                !message.SenderIsAdmin)
            {
                lbChatMessages.AddMessage(new ChatMessage(Color.Silver, string.Format("Message blocked from - {0}".L10N("Client:Main:PMBlockedFrom"), message.SenderName)));
            }
            else
            {
                lbChatMessages.AddMessage(message);
            }
        }

        private void CurrentChatChannel_MessageAdded(object sender, IRCMessageEventArgs e) =>
            AddMessageToChat(e.Message);

        /// <summary>
        /// Removes a game from the list when the host quits CnCNet or
        /// leaves the game broadcast channel.
        /// </summary>
        private void GameBroadcastChannel_UserLeftOrQuit(object sender, UserNameEventArgs e)
        {
            int gameIndex = lbGameList.HostedGames.FindIndex(hg => hg.HostName == e.UserName);

            if (gameIndex > -1)
            {
                lbGameList.RemoveGame(gameIndex);

                // dismiss any outstanding invitations that are no longer valid
                DismissInvalidInvitations();
            }
        }

        private void GameBroadcastChannel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
        {
            var channel = (Channel)sender;

            var channelUser = channel.Users.Find(e.UserName);

            if (channelUser == null)
                return;

            if (localGame != null &&
                channel.ChannelName == localGame.GameBroadcastChannel &&
                !updateDenied &&
                channelUser.IsAdmin &&
                !isInGameRoom &&
                e.Message.StartsWith(CnCNetCommands.UPDATE + " ") &&
                e.Message.Length > 7)
            {
                string version = e.Message[7..];
                if (version != ProgramConstants.GAME_VERSION)
                {
                    var updateMessageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "Update available".L10N("Client:Main:UpdateAvailableTitle"),
                        "An update is available. Do you want to perform the update now?".L10N("Client:Main:UpdateAvailableText"));
                    updateMessageBox.NoClickedAction = UpdateMessageBox_NoClicked;
                    updateMessageBox.YesClickedAction = UpdateMessageBox_YesClicked;
                }
            }

            if (!e.Message.StartsWith(CnCNetCommands.GAME + " "))
                return;

            string msg = e.Message[5..]; // Cut out GAME part
            string[] splitMessage = msg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (splitMessage.Length != 11)
            {
                Logger.Log("Ignoring CTCP game message because of an invalid amount of parameters.");
                return;
            }

            try
            {
                string revision = splitMessage[0];

                if (revision != ProgramConstants.CNCNET_PROTOCOL_REVISION)
                    return;

                string gameVersion = splitMessage[1];
                int maxPlayers = Conversions.IntFromString(splitMessage[2], 0);
                string gameRoomChannelName = splitMessage[3];
                string gameRoomDisplayName = splitMessage[4];
                bool locked = Conversions.BooleanFromString(splitMessage[5][..1], true);
                bool isCustomPassword = Conversions.BooleanFromString(splitMessage[5].Substring(1, 1), false);
                bool isClosed = Conversions.BooleanFromString(splitMessage[5].Substring(2, 1), true);
                bool isLoadedGame = Conversions.BooleanFromString(splitMessage[5].Substring(3, 1), false);
                bool isLadder = Conversions.BooleanFromString(splitMessage[5].Substring(4, 1), false);
                string[] players = splitMessage[6].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string mapName = splitMessage[7];
                string gameMode = splitMessage[8];
                string tunnelHash = splitMessage[9];
                string loadedGameId = splitMessage[10];

                CnCNetGame cncnetGame = gameCollection.GameList.Find(g => g.GameBroadcastChannel == channel.ChannelName);

                if (cncnetGame == null)
                    return;

                CnCNetTunnel tunnel = null;

                if (!ProgramConstants.CNCNET_DYNAMIC_TUNNELS.Equals(tunnelHash, StringComparison.OrdinalIgnoreCase))
                {
                    tunnel = tunnelHandler.Tunnels.Find(t => t.Hash.Equals(tunnelHash, StringComparison.OrdinalIgnoreCase));

                    if (tunnel == null)
                        return;
                }

                var game = new HostedCnCNetGame(gameRoomChannelName, revision, gameVersion, maxPlayers,
                    gameRoomDisplayName, isCustomPassword, true, players, e.UserName, mapName, gameMode);

                game.IsLoadedGame = isLoadedGame;
                game.MatchID = loadedGameId;
                game.LastRefreshTime = DateTime.Now;
                game.IsLadder = isLadder;
                game.Game = cncnetGame;
                game.Locked = locked || (game.IsLoadedGame && !game.Players.Contains(ProgramConstants.PLAYERNAME));
                game.Incompatible = cncnetGame == localGame && game.GameVersion != ProgramConstants.GAME_VERSION;
                game.TunnelServer = tunnel;

                if (isClosed)
                {
                    int index = lbGameList.HostedGames.FindIndex(hg => hg.HostName == e.UserName);

                    if (index > -1)
                    {
                        lbGameList.RemoveGame(index);

                        // dismiss any outstanding invitations that are no longer valid
                        DismissInvalidInvitations();
                    }

                    return;
                }

                // Seek for the game in the internal game list based on the name of its host;
                // if found, then refresh that game's information, otherwise add as new game
                int gameIndex = lbGameList.HostedGames.FindIndex(hg => hg.HostName == e.UserName);

                if (gameIndex > -1)
                {
                    lbGameList.HostedGames[gameIndex] = game;
                }
                else
                {
                    if (UserINISettings.Instance.PlaySoundOnGameHosted &&
                        cncnetGame.InternalName == localGameID.ToLower() &&
                        !ProgramConstants.IsInGame && !game.Locked)
                    {
                        SoundPlayer.Play(sndGameCreated);
                    }

                    lbGameList.AddGame(game);
                }

                SortAndRefreshHostedGames();
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Game parsing error");
            }
        }

        private void UpdateMessageBox_YesClicked(XNAMessageBox messageBox) =>
            UpdateCheck?.Invoke(this, EventArgs.Empty);

        private void UpdateMessageBox_NoClicked(XNAMessageBox messageBox) => updateDenied = true;

        private async ValueTask BtnLogout_LeftClickAsync()
        {
            if (isInGameRoom)
            {
                topBar.SwitchToPrimary();
                return;
            }

            if (connectionManager.IsConnected &&
                !UserINISettings.Instance.PersistentMode)
            {
                await connectionManager.DisconnectAsync().ConfigureAwait(false);
            }

            topBar.SwitchToPrimary();
        }

        public void SwitchOn()
        {
            Enable();

            if (!connectionManager.IsConnected && !connectionManager.IsAttemptingConnection)
            {
                loginWindow.Enable();
                loginWindow.LoadSettings();
            }

            SetLogOutButtonText();
        }

        public void SwitchOff() => Disable();

        public string GetSwitchName() => "CnCNet Lobby".L10N("Client:Main:CnCNetLobby");

        private bool CanReceiveInvitationMessagesFrom(string username)
        {
            IRCUser iu = connectionManager.UserList.Find(u => u.Name == username);

            // We don't accept invitation messages from people who we don't share any channels with
            if (iu == null)
            {
                return false;
            }

            // Invitation messages from users we've blocked are not wanted
            if (cncnetUserData.IsIgnored(iu.Ident))
            {
                return false;
            }

            return true;
        }

        private Texture2D GetUserTexture(string username)
        {
            Texture2D senderGameIcon = unknownGameIcon;

            IRCUser iu = connectionManager.UserList.Find(u => u.Name == username);

            if (iu != null && iu.GameID >= 0 && iu.GameID < gameCollection.GameList.Count)
            {
                senderGameIcon = gameCollection.GameList[iu.GameID].Texture;
            }

            return senderGameIcon;
        }

        private void DismissInvalidInvitations()
        {
            var toDismiss = new List<UserChannelPair>();

            foreach (KeyValuePair<UserChannelPair, WeakReference> invitation in invitationIndex)
            {
                var gameIndex =
                    lbGameList.HostedGames.FindIndex(hg =>
                    ((HostedCnCNetGame)hg).HostName == invitation.Key.Item1 &&
                    ((HostedCnCNetGame)hg).ChannelName == invitation.Key.Item2);

                if (gameIndex == -1)
                {
                    toDismiss.Add(invitation.Key);
                }
            }

            foreach (UserChannelPair invitationIdentity in toDismiss)
            {
                DismissInvitation(invitationIdentity);
            }
        }

        private void DismissInvitation(UserChannelPair invitationIdentity)
        {
            if (invitationIndex.TryGetValue(invitationIdentity, out WeakReference _))
            {
                if (invitationIndex[invitationIdentity].Target is ChoiceNotificationBox invitationNotification)
                    WindowManager.RemoveControl(invitationNotification);

                invitationIndex.Remove(invitationIdentity);
            }
        }

        /// <summary>
        /// Attempts to find a hosted game that the specified user is in
        /// </summary>
        /// <param name="user">The user to find a game for.</param>
        /// <returns></returns>
        private HostedCnCNetGame GetHostedGameForUser(IRCUser user)
        {
            return lbGameList.HostedGames.Select(g => (HostedCnCNetGame)g).FirstOrDefault(g => g.Players.Contains(user.Name));
        }

        /// <summary>
        /// Joins a specified user's game depending on whether or not
        /// they are currently in one.
        /// </summary>
        /// <param name="user">The user to join.</param>
        /// <param name="messageView">The message view/list to write error messages to.</param>
        private async ValueTask JoinUserAsync(IRCUser user, IMessageView messageView)
        {
            if (user == null)
            {
                // can happen if a user is selected while offline
                messageView.AddMessage(new ChatMessage(Color.White, "User is not currently available!".L10N("Client:Main:UserNotAvailable")));
                return;
            }
            var game = GetHostedGameForUser(user);
            if (game == null)
            {
                messageView.AddMessage(new ChatMessage(Color.White, string.Format("{0} is not in a game!".L10N("Client:Main:UserNotInGame"), user.Name)));
                return;
            }

            await JoinGameAsync(game, string.Empty, messageView).ConfigureAwait(false);
        }
    }
}