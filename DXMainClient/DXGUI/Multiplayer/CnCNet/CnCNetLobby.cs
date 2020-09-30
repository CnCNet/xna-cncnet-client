using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    using UserChannelPair = Tuple<string, string>;
    using InvitationIndex = Dictionary<Tuple<string, string>, WeakReference>;

    internal class CnCNetLobby : XNAWindow, ISwitchable
    {
        public event EventHandler UpdateCheck;

        public CnCNetLobby(WindowManager windowManager, CnCNetManager connectionManager,
            CnCNetGameLobby gameLobby, CnCNetGameLoadingLobby gameLoadingLobby,
            TopBar topBar, PrivateMessagingWindow pmWindow, TunnelHandler tunnelHandler,
            GameCollection gameCollection, CnCNetUserData cncnetUserData)
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

            ctcpCommandHandlers = new CommandHandlerBase[]
            {
                new StringCommandHandler(ProgramConstants.GAME_INVITE_CTCP_COMMAND, HandleGameInviteCommand),
                new NoParamCommandHandler(ProgramConstants.GAME_INVITATION_FAILED_CTCP_COMMAND, HandleGameInvitationFailedNotification)
            };
        }

        private CnCNetManager connectionManager;
        private CnCNetUserData cncnetUserData;

        private PlayerListBox lbPlayerList;
        private ChatListBox lbChatMessages;
        private GameListBox lbGameList;
        private XNAContextMenu playerContextMenu;

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

        private DarkeningPanel gameCreationPanel;

        private Channel currentChatChannel;

        private GameCollection gameCollection;

        private Color cAdminNameColor;

        private Texture2D unknownGameIcon;
        private Texture2D adminGameIcon;

        private EnhancedSoundEffect sndGameCreated;
        private EnhancedSoundEffect sndGameInviteReceived;

        private IRCColor[] chatColors;

        private CnCNetGameLobby gameLobby;
        private CnCNetGameLoadingLobby gameLoadingLobby;

        private TunnelHandler tunnelHandler;

        private CnCNetLoginWindow loginWindow;

        private TopBar topBar;

        private PrivateMessagingWindow pmWindow;

        private PasswordRequestWindow passwordRequestWindow;

        private bool isInGameRoom = false;
        private bool updateDenied = false;

        private string localGameID;
        private CnCNetGame localGame;

        private List<string> followedGames = new List<string>();

        private bool isJoiningGame = false;
        private HostedCnCNetGame gameOfLastJoinAttempt;

        private CancellationTokenSource gameCheckCancellation;

        private CommandHandlerBase[] ctcpCommandHandlers;

        private InvitationIndex invitationIndex;

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
            btnNewGame.ClientRectangle = new Rectangle(12, Height - 29, 133, 23);
            btnNewGame.Text = "Create Game";
            btnNewGame.AllowClick = false;
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnJoinGame = new XNAClientButton(WindowManager);
            btnJoinGame.Name = nameof(btnJoinGame);
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.Right + 12,
                btnNewGame.Y, 133, 23);
            btnJoinGame.Text = "Join Game";
            btnJoinGame.AllowClick = false;
            btnJoinGame.LeftClick += BtnJoinGame_LeftClick;

            btnLogout = new XNAClientButton(WindowManager);
            btnLogout.Name = nameof(btnLogout);
            btnLogout.ClientRectangle = new Rectangle(Width - 145, btnNewGame.Y,
                133, 23);
            btnLogout.Text = "Log Out";
            btnLogout.LeftClick += BtnLogout_LeftClick;

            lbGameList = new GameListBox(WindowManager, localGameID, HostedGameMatches);
            lbGameList.Name = nameof(lbGameList);
            lbGameList.ClientRectangle = new Rectangle(btnNewGame.X,
                41, btnJoinGame.Right - btnNewGame.X,
                btnNewGame.Y - 47);
            lbGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameList.DoubleLeftClick += LbGameList_DoubleLeftClick;
            lbGameList.AllowMultiLineItems = false;

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

            playerContextMenu = new XNAContextMenu(WindowManager);
            playerContextMenu.Name = nameof(playerContextMenu);
            playerContextMenu.ClientRectangle = new Rectangle(0, 0, 150, 2);
            playerContextMenu.Enabled = false;
            playerContextMenu.Visible = false;
            playerContextMenu.AddItem("Private Message", () => 
                PerformUserListContextMenuAction(iu => pmWindow.InitPM(iu.Name)));
            playerContextMenu.AddItem("Add Friend", () => 
                PerformUserListContextMenuAction(iu => cncnetUserData.ToggleFriend(iu.Name)));
            playerContextMenu.AddItem("Ignore User", () => 
                PerformUserListContextMenuAction(iu => cncnetUserData.ToggleIgnoreUser(iu.Ident)));

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = nameof(lbChatMessages);
            lbChatMessages.ClientRectangle = new Rectangle(lbGameList.Right + 12, lbGameList.Y,
                lbPlayerList.X - lbGameList.Right - 24, lbPlayerList.Height);
            lbChatMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;

            tbChatInput = new XNAChatTextBox(WindowManager);
            tbChatInput.Name = nameof(tbChatInput);
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                btnNewGame.Y, lbChatMessages.Width,
                btnNewGame.Height);
            tbChatInput.Suggestion = "Type here to chat...";
            tbChatInput.Enabled = false;
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            lblColor = new XNALabel(WindowManager);
            lblColor.Name = nameof(lblColor);
            lblColor.ClientRectangle = new Rectangle(lbChatMessages.X, 14, 0, 0);
            lblColor.FontIndex = 1;
            lblColor.Text = "YOUR COLOR:";

            ddColor = new XNAClientDropDown(WindowManager);
            ddColor.Name = nameof(ddColor);
            ddColor.ClientRectangle = new Rectangle(lblColor.X + 95, 12,
                150, 21);

            chatColors = connectionManager.GetIRCColors();

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
                ? ClientConfiguration.Instance.DefaultPersonalChatColorIndex:
                selectedColor;
            SetChatColor();
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;

            ddCurrentChannel = new XNAClientDropDown(WindowManager);
            ddCurrentChannel.Name = nameof(ddCurrentChannel);
            ddCurrentChannel.ClientRectangle = new Rectangle(
                lbChatMessages.Right - 200,
                ddColor.Y, 200, 21);
            ddCurrentChannel.SelectedIndexChanged += DdCurrentChannel_SelectedIndexChanged;
            ddCurrentChannel.AllowDropDown = false;

            lblCurrentChannel = new XNALabel(WindowManager);
            lblCurrentChannel.Name = nameof(lblCurrentChannel);
            lblCurrentChannel.ClientRectangle = new Rectangle(
                ddCurrentChannel.X - 150,
                ddCurrentChannel.Y + 2, 0, 0);
            lblCurrentChannel.FontIndex = 1;
            lblCurrentChannel.Text = "CURRENT CHANNEL:";

            lblOnline = new XNALabel(WindowManager);
            lblOnline.Name = nameof(lblOnline);
            lblOnline.ClientRectangle = new Rectangle(310, 14, 0, 0);
            lblOnline.Text = "Online:";
            lblOnline.FontIndex = 1;
            lblOnline.Disable();

            lblOnlineCount = new XNALabel(WindowManager);
            lblOnlineCount.Name = nameof(lblOnlineCount);
            lblOnlineCount.ClientRectangle = new Rectangle(lblOnline.X + 50, 14, 0, 0);
            lblOnlineCount.FontIndex = 1;
            lblOnlineCount.Disable();

            tbGameSearch = new XNASuggestionTextBox(WindowManager);
            tbGameSearch.Name = nameof(tbGameSearch);
            tbGameSearch.ClientRectangle = new Rectangle(lbGameList.X,
                12, lbGameList.Width, 21);
            tbGameSearch.Suggestion = "Filter by name, map, game mode, player...";
            tbGameSearch.MaximumTextLength = 64;
            tbGameSearch.InputReceived += TbGameSearch_InputReceived;
            tbGameSearch.Disable();

            InitializeGameList();

            AddChild(btnNewGame);
            AddChild(btnJoinGame);
            AddChild(btnLogout);
            AddChild(lbPlayerList);
            AddChild(lbChatMessages);
            AddChild(lbGameList);
            AddChild(tbChatInput);
            AddChild(lblColor);
            AddChild(ddColor);
            AddChild(lblCurrentChannel);
            AddChild(ddCurrentChannel);
            AddChild(playerContextMenu);
            AddChild(lblOnline);
            AddChild(lblOnlineCount);
            AddChild(tbGameSearch);

            CnCNetPlayerCountTask.CnCNetGameCountUpdated += OnCnCNetGameCountUpdated;
            UpdateOnlineCount(CnCNetPlayerCountTask.PlayerCount);

            base.Initialize();

            WindowManager.CenterControlOnScreen(this);

            PostUIInit();
        }

        private void TbGameSearch_InputReceived(object sender, EventArgs e)
        {
            lbGameList.SortAndRefreshHostedGames();
            lbGameList.ViewTop = 0;
        }

        private bool HostedGameMatches(GenericHostedGame hg) => 
            string.IsNullOrWhiteSpace(tbGameSearch?.Text) ||
            tbGameSearch.Text == tbGameSearch.Suggestion ||
            hg.RoomName.ToUpper().Contains(tbGameSearch.Text.ToUpper()) ||
            hg.GameMode.ToUpper().Equals(tbGameSearch.Text.ToUpper()) ||
            hg.Map.ToUpper().Contains(tbGameSearch.Text.ToUpper()) ||
            hg.Players.Where(pl => pl.ToUpper().Equals(tbGameSearch.Text.ToUpper())).Any();

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
                        gameBroadcastChannel = connectionManager.CreateChannel(game.UIName + " Broadcast Channel",
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

            cAdminNameColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AdminNameColor);
            unknownGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.unknownicon);
            adminGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.cncneticon);

            connectionManager.WelcomeMessageReceived += ConnectionManager_WelcomeMessageReceived;
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
            gcw.GameCreated += Gcw_GameCreated;
            gcw.LoadedGameCreated += Gcw_LoadedGameCreated;

            gameCreationPanel.Hide();

            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, Renderer.GetSafeString(
                    "*** DTA CnCNet Client version " +
                    System.Windows.Forms.Application.ProductVersion + " ***", lbChatMessages.FontIndex)));

            connectionManager.BannedFromChannel += ConnectionManager_BannedFromChannel;

            loginWindow = new CnCNetLoginWindow(WindowManager);
            loginWindow.Connect += LoginWindow_Connect;
            loginWindow.Cancelled += LoginWindow_Cancelled;

            var loginWindowPanel = new DarkeningPanel(WindowManager);
            loginWindowPanel.Alpha = 0.0f;

            AddChild(loginWindowPanel);
            loginWindowPanel.AddChild(loginWindow);
            loginWindow.Disable();

            passwordRequestWindow = new PasswordRequestWindow(WindowManager);
            passwordRequestWindow.PasswordEntered += PasswordRequestWindow_PasswordEntered;

            var passwordRequestWindowPanel = new DarkeningPanel(WindowManager);
            passwordRequestWindowPanel.Alpha = 0.0f;
            AddChild(passwordRequestWindowPanel);
            passwordRequestWindowPanel.AddChild(passwordRequestWindow);
            passwordRequestWindow.Disable();

            gameLobby.GameLeft += GameLobby_GameLeft;
            gameLoadingLobby.GameLeft += GameLoadingLobby_GameLeft;

            UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;

            GameProcessLogic.GameProcessStarted += SharedUILogic_GameProcessStarted;
            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;
        }

        /// <summary>
        /// Displays a message when the IRC server has informed that the local user
        /// has been banned from a channel that they're attempting to join.
        /// </summary>
        private void ConnectionManager_BannedFromChannel(object sender, ChannelEventArgs e)
        {
            var game = lbGameList.HostedGames.Find(hg => ((HostedCnCNetGame)hg).ChannelName == e.ChannelName);

            if (game == null)
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    Color.White, "Cannot join channel " + e.ChannelName + ", you're banned!"));
            }
            else
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(
                    Color.White, "Cannot join game " + game.RoomName + ", you've been banned by the game host!"));
            }

            isJoiningGame = false;
            if (gameOfLastJoinAttempt != null)
            {
                if (gameOfLastJoinAttempt.IsLoadedGame)
                    gameLoadingLobby.Clear();
                else
                    gameLobby.Clear();
            }
        }

        private void SharedUILogic_GameProcessStarted()
        {
            connectionManager.SendCustomMessage(new QueuedMessage("AWAY " + (char)58 + "In-game",
                QueuedMessageType.SYSTEM_MESSAGE, 0));
        }

        private void SharedUILogic_GameProcessExited()
        {
            connectionManager.SendCustomMessage(new QueuedMessage("AWAY",
                QueuedMessageType.SYSTEM_MESSAGE, 0));
        }

        private void Instance_SettingsSaved(object sender, EventArgs e)
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
                    connectionManager.FindChannel(game.GameBroadcastChannel).Leave();
                    followedGames.Remove(game.InternalName);
                }
                else if (!followedGames.Contains(game.InternalName) &&
                    UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
                {
                    connectionManager.FindChannel(game.GameBroadcastChannel).Join();
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
            bool isAdmin = user.IsAdmin;

            playerContextMenu.Items[1].Text = cncnetUserData.IsFriend(user.IRCUser.Name) ? "Remove Friend" : "Add Friend";
            playerContextMenu.Items[2].Text = cncnetUserData.IsIgnored(user.IRCUser.Ident) && !isAdmin ? "Unblock" : "Block";
            playerContextMenu.Items[2].Selectable = !isAdmin;

            playerContextMenu.Open(GetCursorPoint());
        }

        private void PerformUserListContextMenuAction(Action<IRCUser> action)
        {
            if (lbPlayerList.SelectedIndex < 0 ||
                lbPlayerList.SelectedIndex >= lbPlayerList.Items.Count)
            {
                return;
            }

            var user = (ChannelUser)lbPlayerList.SelectedItem.Tag;
            IRCUser ircUser = user.IRCUser;

            action(ircUser);
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
            StatisticsSender.Instance.SendCnCNet();
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
                btnLogout.Text = "Game Lobby";
                return;
            }

            if (UserINISettings.Instance.PersistentMode)
            {
                btnLogout.Text = "Main Menu";
                return;
            }

            btnLogout.Text = "Log Out";
        }

        private void BtnJoinGame_LeftClick(object sender, EventArgs e) => LbGameList_DoubleLeftClick(this, EventArgs.Empty);

        private void LbGameList_DoubleLeftClick(object sender, EventArgs e) => JoinGameByIndex(lbGameList.SelectedIndex, string.Empty);

        private void PasswordRequestWindow_PasswordEntered(object sender, PasswordEventArgs e) => JoinGame(e.HostedGame, e.Password);

        /// <summary>
        /// Checks if the user can join a game.
        /// Returns null if the user can, otherwise returns an error message
        /// that tells the reason why the user cannot join the game.
        /// </summary>
        /// <param name="gameIndex">The index of the game in the game list box.</param>
        private string CanJoinGameByIndex(int gameIndex)
        {
            if (gameIndex < 0 || gameIndex >= lbGameList.Items.Count)
                return "Invalid game index";

            if (isJoiningGame)
                return "Cannot join game - joining game in progress";

            if (ProgramConstants.IsInGame)
                return "Cannot join game while the main game executable is running.";

            HostedCnCNetGame hg = (HostedCnCNetGame)lbGameList.Items[gameIndex].Tag;

            if (hg.Game.InternalName.ToUpper() != localGameID.ToUpper())
                return "The selected game is for " + gameCollection.GetGameNameFromInternalName(hg.Game.InternalName);

            if (hg.Locked)
                return "The selected game is locked!";

            if (hg.IsLoadedGame && !hg.Players.Contains(ProgramConstants.PLAYERNAME))
                return "You do not exist in the saved game!";

            return null;
        }

        private bool JoinGameByIndex(int gameIndex, string password)
        {
            string error = CanJoinGameByIndex(gameIndex);
            if (!string.IsNullOrEmpty(error))
            {
                connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, error));
                return false;
            }

            if (isInGameRoom)
            {
                topBar.SwitchToPrimary();
                return false;
            }

            HostedCnCNetGame hg = (HostedCnCNetGame)lbGameList.Items[gameIndex].Tag;

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
                        (hg.ChannelName + hg.RoomName).Substring(0, 10);
                }
                else
                {
                    IniFile spawnSGIni = new IniFile(ProgramConstants.GamePath + "Saved Games/spawnSG.ini");
                    password = Utilities.CalculateSHA1ForString(
                        spawnSGIni.GetStringValue("Settings", "GameID", string.Empty)).Substring(0, 10);
                }
            }

            JoinGame(hg, password);

            return true;
        }

        private void JoinGame(HostedCnCNetGame hg, string password)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White,
                "Attempting to join game " + hg.RoomName + "..."));
            isJoiningGame = true;
            gameOfLastJoinAttempt = hg;

            Channel gameChannel = connectionManager.CreateChannel(hg.RoomName, hg.ChannelName, false, true, password);
            connectionManager.AddChannel(gameChannel);

            if (hg.IsLoadedGame)
            {
                gameLoadingLobby.SetUp(false, hg.TunnelServer, gameChannel, hg.HostName);
                gameChannel.UserAdded += GameLoadingChannel_UserAdded;
                //gameChannel.MessageAdded += GameLoadingChannel_MessageAdded;
                gameChannel.InvalidPasswordEntered += GameChannel_InvalidPasswordEntered_LoadedGame;
            }
            else
            {
                gameLobby.SetUp(gameChannel, false, hg.MaxPlayers, hg.TunnelServer, hg.HostName, hg.Passworded);
                gameChannel.UserAdded += GameChannel_UserAdded;
                gameChannel.InvalidPasswordEntered += GameChannel_InvalidPasswordEntered_NewGame;
                gameChannel.InviteOnlyErrorOnJoin += GameChannel_InviteOnlyErrorOnJoin;
                gameChannel.ChannelFull += GameChannel_ChannelFull;
                gameChannel.TargetChangeTooFast += GameChannel_TargetChangeTooFast;
            }

            connectionManager.SendCustomMessage(new QueuedMessage("JOIN " + hg.ChannelName + " " + password,
                QueuedMessageType.INSTANT_MESSAGE, 0));
        }

        private void GameChannel_TargetChangeTooFast(object sender, MessageEventArgs e)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, e.Message));
            ClearGameJoinAttempt((Channel)sender);
        }

        private void GameChannel_ChannelFull(object sender, EventArgs e) =>
            // We'd do the exact same things here, so we can just call the method below
            GameChannel_InviteOnlyErrorOnJoin(sender, e);

        private void GameChannel_InviteOnlyErrorOnJoin(object sender, EventArgs e)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, "The selected game is locked!"));
            var channel = (Channel)sender;

            var game = FindGameByChannelName(channel.ChannelName);
            if (game != null)
            {
                game.Locked = true;
                lbGameList.SortAndRefreshHostedGames();
            }

            ClearGameJoinAttempt((Channel)sender);
        }

        private HostedCnCNetGame FindGameByChannelName(string channelName)
        {
            var game = lbGameList.HostedGames.Find(hg => ((HostedCnCNetGame)hg).ChannelName == channelName);
            if (game == null)
                return null;

            return (HostedCnCNetGame)game;
        }

        private void GameChannel_InvalidPasswordEntered_NewGame(object sender, EventArgs e)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White, "Incorrect password!"));
            ClearGameJoinAttempt((Channel)sender);
        }

        private void GameChannel_UserAdded(object sender, Online.ChannelUserEventArgs e)
        {
            Channel gameChannel = (Channel)sender;

            if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
            {
                ClearGameChannelEvents(gameChannel);
                gameLobby.OnJoined();
                isInGameRoom = true;
                SetLogOutButtonText();
            }
        }

        private void ClearGameJoinAttempt(Channel channel)
        {
            ClearGameChannelEvents(channel);
            gameLobby.Clear();
        }

        private void ClearGameChannelEvents(Channel channel)
        {
            channel.UserAdded -= GameChannel_UserAdded;
            channel.InvalidPasswordEntered -= GameChannel_InvalidPasswordEntered_NewGame;
            channel.InviteOnlyErrorOnJoin -= GameChannel_InviteOnlyErrorOnJoin;
            channel.ChannelFull -= GameChannel_ChannelFull;
            channel.TargetChangeTooFast -= GameChannel_TargetChangeTooFast;
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

        private void Gcw_GameCreated(object sender, GameCreationEventArgs e)
        {
            if (gameLobby.Enabled || gameLoadingLobby.Enabled)
                return;

            string channelName = RandomizeChannelName();
            string password = e.Password;
            bool isCustomPassword = true;
            if (string.IsNullOrEmpty(password))
            {
                password = Rampastring.Tools.Utilities.CalculateSHA1ForString(
                    channelName + e.GameRoomName).Substring(0, 10);
                isCustomPassword = false;
            }

            Channel gameChannel = connectionManager.CreateChannel(e.GameRoomName, channelName, false, true, password);
            connectionManager.AddChannel(gameChannel);
            gameLobby.SetUp(gameChannel, true, e.MaxPlayers, e.Tunnel, ProgramConstants.PLAYERNAME, isCustomPassword);
            gameChannel.UserAdded += GameChannel_UserAdded;
            //gameChannel.MessageAdded += GameChannel_MessageAdded;
            connectionManager.SendCustomMessage(new QueuedMessage("JOIN " + channelName + " " + password,
                QueuedMessageType.INSTANT_MESSAGE, 0));
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White,
                "Creating a game named " + e.GameRoomName + "..."));

            gameCreationPanel.Hide();

            // update the friends window so it can enable the Invite option
            pmWindow.SetInviteChannelInfo(channelName, e.GameRoomName, string.IsNullOrEmpty(e.Password) ? string.Empty : e.Password);
        }

        private void Gcw_LoadedGameCreated(object sender, GameCreationEventArgs e)
        {
            if (gameLobby.Enabled || gameLoadingLobby.Enabled)
                return;

            string channelName = RandomizeChannelName();

            Channel gameLoadingChannel = connectionManager.CreateChannel(e.GameRoomName, channelName, false, true, e.Password);
            connectionManager.AddChannel(gameLoadingChannel);
            gameLoadingLobby.SetUp(true, e.Tunnel, gameLoadingChannel, ProgramConstants.PLAYERNAME);
            gameLoadingChannel.UserAdded += GameLoadingChannel_UserAdded;
            connectionManager.SendCustomMessage(new QueuedMessage("JOIN " + channelName + " " + e.Password,
                QueuedMessageType.INSTANT_MESSAGE, 0));
            connectionManager.MainChannel.AddMessage(new ChatMessage(Color.White,
                "Creating a game named " + e.GameRoomName + "..."));

            gameCreationPanel.Hide();

            // update the friends window so it can enable the Invite option
            pmWindow.SetInviteChannelInfo(channelName, e.GameRoomName, string.IsNullOrEmpty(e.Password) ? string.Empty : e.Password);
        }

        private void GameChannel_InvalidPasswordEntered_LoadedGame(object sender, EventArgs e)
        {
            var channel = (Channel)sender;
            channel.UserAdded -= GameLoadingChannel_UserAdded;
            channel.InvalidPasswordEntered -= GameChannel_InvalidPasswordEntered_LoadedGame;
            gameLoadingLobby.Clear();
            isJoiningGame = false;
        }

        private void GameLoadingChannel_UserAdded(object sender, ChannelUserEventArgs e)
        {
            Channel gameLoadingChannel = (Channel)sender;

            if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
            {
                gameLoadingChannel.UserAdded -= GameLoadingChannel_UserAdded;
                gameLoadingChannel.InvalidPasswordEntered -= GameChannel_InvalidPasswordEntered_LoadedGame;

                gameLoadingLobby.OnJoined();
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
                string channelName = gameCollection.GetGameChatChannelNameFromIdentifier(localGameID) + "-game" + new Random().Next(1000000, 9999999);
                int index = lbGameList.HostedGames.FindIndex(c => ((HostedCnCNetGame)c).ChannelName == channelName);
                if (index == -1)
                    return channelName;
            }
        }

        private void Gcw_Cancelled(object sender, EventArgs e) => gameCreationPanel.Hide();

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            IRCColor selectedColor = (IRCColor)ddColor.SelectedItem.Tag;

            currentChatChannel.SendChatMessage(tbChatInput.Text, selectedColor);

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

            if (gameCheckCancellation != null)
                gameCheckCancellation.Cancel();
        }

        private void ConnectionManager_WelcomeMessageReceived(object sender, EventArgs e)
        {
            btnNewGame.AllowClick = true;
            btnJoinGame.AllowClick = true;
            ddCurrentChannel.AllowDropDown = true;
            tbChatInput.Enabled = true;

            Channel cncnetChannel = connectionManager.FindChannel("#cncnet");
            cncnetChannel.Join();

            string localGameChatChannelName = gameCollection.GetGameChatChannelNameFromIdentifier(localGameID);
            connectionManager.FindChannel(localGameChatChannelName).Join();

            string localGameBroadcastChannel = gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGameID);
            connectionManager.FindChannel(localGameBroadcastChannel).Join();

            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported)
                    continue;

                if (game.InternalName.ToUpper() != localGameID)
                {
                    if (UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
                    {
                        connectionManager.FindChannel(game.GameBroadcastChannel).Join();
                        followedGames.Add(game.InternalName);
                    }
                }
            }

            gameCheckCancellation = new CancellationTokenSource();
            CnCNetGameCheck gameCheck = new CnCNetGameCheck();
            gameCheck.InitializeService(gameCheckCancellation);
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

        private void HandleGameInviteCommand(string sender, string argumentsString)
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
            if (!string.IsNullOrEmpty(CanJoinGameByIndex(gameIndex)) ||
                (UserINISettings.Instance.AllowGameInvitesFromFriendsOnly &&
                !cncnetUserData.IsFriend(sender)))
            {
                // let the host know that we can't accept
                // note this is not reached for the rejection case
                connectionManager.SendCustomMessage(new QueuedMessage("PRIVMSG " + sender + " :\u0001" +
                    ProgramConstants.GAME_INVITATION_FAILED_CTCP_COMMAND + "\u0001",
                    QueuedMessageType.CHAT_MESSAGE, 0));

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
                "GAME INVITATION",
                GetUserTexture(sender),
                sender,
                "Join " + gameName + "?",
                "Yes", "No", 0);

            // add the invitation to the index so we can remove it if the target game is closed
            // also lets us silently ignore new invitations from the same person while this one is still outstanding
            invitationIndex[invitationIdentity] =
                new WeakReference(gameInviteChoiceBox);

            gameInviteChoiceBox.AffirmativeClickedAction = delegate (ChoiceNotificationBox choiceBox)
            {
                // if we're currently in a game lobby, first leave that channel
                if (isInGameRoom)
                {
                    gameLobby.LeaveGameLobby();
                }

                // JoinGameByIndex does bounds checking so we're safe to pass -1 if the game doesn't exist
                if (!JoinGameByIndex(lbGameList.HostedGames.FindIndex(hg => ((HostedCnCNetGame)hg).ChannelName == channelName), password))
                {
                    XNAMessageBox.Show(WindowManager,
                        "Failed to join",
                        "Unable to join " + sender + "'s game. The game may be locked or closed.");
                }

                // clean up the index as this invitation no longer exists
                invitationIndex.Remove(invitationIdentity);
            };

            gameInviteChoiceBox.NegativeClickedAction = delegate (ChoiceNotificationBox choiceBox)
            {
                // clean up the index as this invitation no longer exists
                invitationIndex.Remove(invitationIdentity);
            };

            sndGameInviteReceived.Play();
        }

        private void HandleGameInvitationFailedNotification(string sender)
        {
            if (!CanReceiveInvitationMessagesFrom(sender))
                return;

            if (isInGameRoom && !ProgramConstants.IsInGame)
            {
                gameLobby.AddWarning(
                    sender + " could not receive your invitation. They might be in game " +
                    "or only accepting invitations from friends. Ensure your game is " +
                    "unlocked and visible in the lobby before trying again.");
            }
        }

        private void DdCurrentChannel_SelectedIndexChanged(object sender, EventArgs e)
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

                    currentChatChannel.Leave();
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
                currentChatChannel.Join();
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
                user.IRCUser.IsIgnored = cncnetUserData.IsIgnored(user.IRCUser.Name);
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
                lbChatMessages.AddMessage(new ChatMessage(Color.Silver, "Message blocked from - " + message.SenderName));
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
                e.Message.StartsWith("UPDATE ") &&
                e.Message.Length > 7)
            {
                string version = e.Message.Substring(7);
                if (version != ProgramConstants.GAME_VERSION)
                {
                    var updateMessageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "Update available",
                        "An update is available. Do you want to perform the update now?");
                    updateMessageBox.NoClickedAction = UpdateMessageBox_NoClicked;
                    updateMessageBox.YesClickedAction = UpdateMessageBox_YesClicked;
                }
            }

            if (!e.Message.StartsWith("GAME "))
                return;

            string msg = e.Message.Substring(5); // Cut out GAME part
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
                bool locked = Conversions.BooleanFromString(splitMessage[5].Substring(0, 1), true);
                bool isCustomPassword = Conversions.BooleanFromString(splitMessage[5].Substring(1, 1), false);
                bool isClosed = Conversions.BooleanFromString(splitMessage[5].Substring(2, 1), true);
                bool isLoadedGame = Conversions.BooleanFromString(splitMessage[5].Substring(3, 1), false);
                bool isLadder = Conversions.BooleanFromString(splitMessage[5].Substring(4, 1), false);
                string[] players = splitMessage[6].Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> playerNames = players.ToList();
                string mapName = splitMessage[7];
                string gameMode = splitMessage[8];

                string[] tunnelAddressAndPort = splitMessage[9].Split(':');
                string tunnelAddress = tunnelAddressAndPort[0];
                int tunnelPort = int.Parse(tunnelAddressAndPort[1]);

                string loadedGameId = splitMessage[10];

                CnCNetGame cncnetGame = gameCollection.GameList.Find(g => g.GameBroadcastChannel == channel.ChannelName);

                CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress && t.Port == tunnelPort);

                if (tunnel == null)
                    return;

                if (cncnetGame == null)
                    return;

                HostedCnCNetGame game = new HostedCnCNetGame(gameRoomChannelName, revision, gameVersion, maxPlayers,
                    gameRoomDisplayName, isCustomPassword, true, players,
                    e.UserName, mapName, gameMode);
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
                    lbGameList.SortAndRefreshHostedGames();
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
            }
            catch (Exception ex)
            {
                Logger.Log("Game parsing error: " + ex.Message);
            }
        }

        private void UpdateMessageBox_YesClicked(XNAMessageBox messageBox) =>
            UpdateCheck?.Invoke(this, EventArgs.Empty);

        private void UpdateMessageBox_NoClicked(XNAMessageBox messageBox) => updateDenied = true;

        private void BtnLogout_LeftClick(object sender, EventArgs e)
        {
            if (isInGameRoom)
            {
                topBar.SwitchToPrimary();
                return;
            }

            if (connectionManager.IsConnected &&
                !UserINISettings.Instance.PersistentMode)
            {
                connectionManager.Disconnect();
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

        public string GetSwitchName() => "CnCNet Lobby";

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

            if (iu != null && iu.GameID >= 0 || iu.GameID < gameCollection.GameList.Count)
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
            if (invitationIndex.ContainsKey(invitationIdentity))
            {
                var invitationNotification = invitationIndex[invitationIdentity].Target as ChoiceNotificationBox;

                if (invitationNotification != null)
                {
                    WindowManager.RemoveControl(invitationNotification);
                }

                invitationIndex.Remove(invitationIdentity);
            }
        }
    }
}
