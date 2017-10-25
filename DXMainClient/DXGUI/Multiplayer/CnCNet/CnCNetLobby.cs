using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    internal class CnCNetLobby : XNAWindow, ISwitchable
    {
        public event EventHandler UpdateCheck;

        public CnCNetLobby(WindowManager windowManager, CnCNetManager connectionManager,
            CnCNetGameLobby gameLobby, CnCNetGameLoadingLobby gameLoadingLobby,
            TopBar topBar, PrivateMessagingWindow pmWindow, TunnelHandler tunnelHandler,
            GameCollection gameCollection)
            : base(windowManager)
        {
            this.connectionManager = connectionManager;
            this.gameLobby = gameLobby;
            this.gameLoadingLobby = gameLoadingLobby;
            this.tunnelHandler = tunnelHandler;
            this.topBar = topBar;
            this.pmWindow = pmWindow;
            this.gameCollection = gameCollection;
        }

        private CnCNetManager connectionManager;

        private XNAListBox lbPlayerList;
        private ChatListBox lbChatMessages;
        private GameListBox lbGameList;
        private PlayerContextMenu playerContextMenu;

        private LinkButton btnForums;
        private LinkButton btnTwitter;
        private LinkButton btnGooglePlus;
        private LinkButton btnYoutube;
        private LinkButton btnFacebook;
        private LinkButton btnModDB;
        private LinkButton btnHomepage;

        private XNAClientButton btnLogout;
        private XNAClientButton btnNewGame;
        private XNAClientButton btnJoinGame;

        private XNATextBox tbChatInput;

        private XNALabel lblColor;
        private XNALabel lblCurrentChannel;

        private XNAClientDropDown ddColor;
        private XNAClientDropDown ddCurrentChannel;

        private DarkeningPanel gameCreationPanel;

        private Channel currentChatChannel;

        private GameCollection gameCollection;

        private Color cAdminNameColor;

        private Texture2D unknownGameIcon;
        private Texture2D adminGameIcon;

        private SoundEffectInstance sndGameCreated;

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

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX - 64,
                WindowManager.RenderResolutionY - 64);

            Name = "CnCNetLobby";
            BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbybg.png");
            localGameID = ClientConfiguration.Instance.LocalGame;
            localGame = gameCollection.GameList.Find(g => g.InternalName.ToUpper() == localGameID.ToUpper());

            btnNewGame = new XNAClientButton(WindowManager);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.ClientRectangle = new Rectangle(12, ClientRectangle.Height - 29, 133, 23);
            btnNewGame.Text = "Create Game";
            btnNewGame.AllowClick = false;
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnJoinGame = new XNAClientButton(WindowManager);
            btnJoinGame.Name = "btnJoinGame";
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.ClientRectangle.Right + 12,
                btnNewGame.ClientRectangle.Y, 133, 23);
            btnJoinGame.Text = "Join Game";
            btnJoinGame.AllowClick = false;
            btnJoinGame.LeftClick += BtnJoinGame_LeftClick;

            btnLogout = new XNAClientButton(WindowManager);
            btnLogout.Name = "btnLogout";
            btnLogout.ClientRectangle = new Rectangle(ClientRectangle.Width - 145, btnNewGame.ClientRectangle.Y,
                133, 23);
            btnLogout.Text = "Log Out";
            btnLogout.LeftClick += BtnLogout_LeftClick;

            btnForums = new LinkButton(WindowManager)
            {
                Name = "btnForums",
                ClientRectangle = new Rectangle(ClientRectangle.Width - 33, 12, 21, 21),
                IdleTexture = AssetLoader.LoadTexture("forumsInactive.png"),
                HoverTexture = AssetLoader.LoadTexture("forumsActive.png"),
                HoverSoundEffect = AssetLoader.LoadSound("button.wav"),
                URL = ClientConfiguration.Instance.ForumURL
            };

            btnTwitter = new LinkButton(WindowManager)
            {
                Name = "btnTwitter",
                ClientRectangle = new Rectangle(ClientRectangle.Width - 61, 12, 21, 21),
                IdleTexture = AssetLoader.LoadTexture("twitterInactive.png"),
                HoverTexture = AssetLoader.LoadTexture("twitterActive.png"),
                HoverSoundEffect = AssetLoader.LoadSound("button.wav"),
                URL = ClientConfiguration.Instance.TwitterURL
            };

            btnGooglePlus = new LinkButton(WindowManager)
            {
                Name = "btnGooglePlus",
                ClientRectangle = new Rectangle(ClientRectangle.Width - 89, 12, 21, 21),
                IdleTexture = AssetLoader.LoadTexture("googlePlusInactive.png"),
                HoverTexture = AssetLoader.LoadTexture("googlePlusActive.png"),
                HoverSoundEffect = AssetLoader.LoadSound("button.wav"),
                URL = ClientConfiguration.Instance.GooglePlusURL
            };

            btnYoutube = new LinkButton(WindowManager)
            {
                Name = "btnYoutube",
                ClientRectangle = new Rectangle(ClientRectangle.Width - 117, 12, 21, 21),
                IdleTexture = AssetLoader.LoadTexture("youtubeInactive.png"),
                HoverTexture = AssetLoader.LoadTexture("youtubeActive.png"),
                HoverSoundEffect = AssetLoader.LoadSound("button.wav"),
                URL = ClientConfiguration.Instance.YoutubeURL
            };

            btnFacebook = new LinkButton(WindowManager)
            {
                Name = "btnFacebook",
                ClientRectangle = new Rectangle(ClientRectangle.Width - 145, 12, 21, 21),
                IdleTexture = AssetLoader.LoadTexture("facebookInactive.png"),
                HoverTexture = AssetLoader.LoadTexture("facebookActive.png"),
                HoverSoundEffect = AssetLoader.LoadSound("button.wav"),
                URL = ClientConfiguration.Instance.FacebookURL
            };

            btnModDB = new LinkButton(WindowManager)
            {
                Name = "btnModDB",
                ClientRectangle = new Rectangle(ClientRectangle.Width - 173, 12, 21, 21),
                IdleTexture = AssetLoader.LoadTexture("moddbInactive.png"),
                HoverTexture = AssetLoader.LoadTexture("moddbActive.png"),
                HoverSoundEffect = AssetLoader.LoadSound("button.wav"),
                URL = ClientConfiguration.Instance.ModDBURL
            };

            btnHomepage = new LinkButton(WindowManager)
            {
                Name = "btnHomepage",
                ClientRectangle = new Rectangle(ClientRectangle.Width - 201, 12, 21, 21),
                IdleTexture = AssetLoader.LoadTexture("homepageInactive.png"),
                HoverTexture = AssetLoader.LoadTexture("homepageActive.png"),
                HoverSoundEffect = AssetLoader.LoadSound("button.wav"),
                URL = ClientConfiguration.Instance.HomepageURL
            };

            lbGameList = new GameListBox(WindowManager, localGameID);
            lbGameList.Name = "lbGameList";
            lbGameList.ClientRectangle = new Rectangle(btnNewGame.ClientRectangle.X,
                41, btnJoinGame.ClientRectangle.Right - btnNewGame.ClientRectangle.X,
                btnNewGame.ClientRectangle.Top - 47);
            lbGameList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameList.DoubleLeftClick += LbGameList_DoubleLeftClick;
            lbGameList.AllowMultiLineItems = false;

            lbPlayerList = new XNAListBox(WindowManager);
            lbPlayerList.Name = "lbPlayerList";
            lbPlayerList.ClientRectangle = new Rectangle(ClientRectangle.Width - 202,
                btnForums.ClientRectangle.Bottom + 8, 190,
                btnLogout.ClientRectangle.Top - btnForums.ClientRectangle.Bottom - 14);
            lbPlayerList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbPlayerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbPlayerList.LineHeight = 16;
            lbPlayerList.DoubleLeftClick += LbPlayerList_DoubleLeftClick;
            lbPlayerList.RightClick += LbPlayerList_RightClick;

            playerContextMenu = new PlayerContextMenu(WindowManager);
            playerContextMenu.Name = "playerContextMenu";
            playerContextMenu.ClientRectangle = new Rectangle(0, 0, 150, 2);
            playerContextMenu.Enabled = false;
            playerContextMenu.Visible = false;
            playerContextMenu.AddItem("Private Message");
            playerContextMenu.AddItem("Add Friend");
            playerContextMenu.OptionSelected += PlayerContextMenu_OptionSelected;

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.ClientRectangle = new Rectangle(lbGameList.ClientRectangle.Right + 12, lbGameList.ClientRectangle.Y,
                lbPlayerList.ClientRectangle.Left - lbGameList.ClientRectangle.Right - 24, lbPlayerList.ClientRectangle.Height);
            lbChatMessages.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;

            tbChatInput = new XNATextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X,
                btnNewGame.ClientRectangle.Y, lbChatMessages.ClientRectangle.Width,
                btnNewGame.ClientRectangle.Height);
            tbChatInput.Enabled = false;
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            lblColor = new XNALabel(WindowManager);
            lblColor.Name = "lblColor";
            lblColor.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X, 14, 0, 0);
            lblColor.FontIndex = 1;
            lblColor.Text = "YOUR COLOR:";

            ddColor = new XNAClientDropDown(WindowManager);
            ddColor.Name = "ddColor";
            ddColor.ClientRectangle = new Rectangle(lblColor.ClientRectangle.X + 95, btnForums.ClientRectangle.Y,
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
            ddCurrentChannel.Name = "ddCurrentChannel";
            ddCurrentChannel.ClientRectangle = new Rectangle(
                lbChatMessages.ClientRectangle.Right - 200,
                ddColor.ClientRectangle.Y, 200, 21);
            ddCurrentChannel.SelectedIndexChanged += DdCurrentChannel_SelectedIndexChanged;
            ddCurrentChannel.AllowDropDown = false;

            lblCurrentChannel = new XNALabel(WindowManager);
            lblCurrentChannel.Name = "lblCurrentChannel";
            lblCurrentChannel.ClientRectangle = new Rectangle(
                ddCurrentChannel.ClientRectangle.X - 150,
                ddCurrentChannel.ClientRectangle.Y + 2, 0, 0);
            lblCurrentChannel.FontIndex = 1;
            lblCurrentChannel.Text = "CURRENT CHANNEL:";

            InitializeGameList();

            AddChild(btnNewGame);
            AddChild(btnJoinGame);
            AddChild(btnLogout);

            AddChild(btnForums);
            AddChild(btnTwitter);
            AddChild(btnGooglePlus);
            AddChild(btnYoutube);
            AddChild(btnFacebook);
            AddChild(btnModDB);
            AddChild(btnHomepage);
            AddChild(lbPlayerList);
            AddChild(lbChatMessages);
            AddChild(lbGameList);
            AddChild(tbChatInput);
            AddChild(lblColor);
            AddChild(ddColor);
            AddChild(lblCurrentChannel);
            AddChild(ddCurrentChannel);
            AddChild(playerContextMenu);

            base.Initialize();

            WindowManager.CenterControlOnScreen(this);

            PostUIInit();
        }

        private void InitializeGameList()
        {
            int i = 0;

            foreach (var game in gameCollection.GameList)
            {
                if (!game.Supported)
                {
                    i++;
                    continue;
                }

                var item = new XNADropDownItem();
                item.Text = game.UIName;
                item.TextColor = UISettings.AltColor;
                item.Texture = game.Texture;

                ddCurrentChannel.AddItem(item);

                var chatChannel = connectionManager.GetChannel(game.ChatChannel);

                if (chatChannel == null)
                {
                    chatChannel = connectionManager.CreateChannel(game.UIName, game.ChatChannel,
                        true, "ra1-derp");
                    connectionManager.AddChannel(chatChannel);
                }

                item.Tag = chatChannel;

                var gameBroadcastChannel = connectionManager.GetChannel(game.GameBroadcastChannel);

                if (gameBroadcastChannel == null)
                {
                    gameBroadcastChannel = connectionManager.CreateChannel(game.UIName + " Broadcast Channel",
                        game.GameBroadcastChannel, true, null);
                    connectionManager.AddChannel(gameBroadcastChannel);
                }

                gameBroadcastChannel.CTCPReceived += GameBroadcastChannel_CTCPReceived;

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
            SoundEffect gameCreatedSoundEffect = AssetLoader.LoadSound("gamecreated.wav");

            if (gameCreatedSoundEffect != null)
                sndGameCreated = gameCreatedSoundEffect.CreateInstance();

            cAdminNameColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AdminNameColor);
            unknownGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.unknownicon);
            adminGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.cncneticon);

            connectionManager.WelcomeMessageReceived += ConnectionManager_WelcomeMessageReceived;
            connectionManager.Disconnected += ConnectionManager_Disconnected;

            gameCreationPanel = new DarkeningPanel(WindowManager);
            AddChild(gameCreationPanel);

            GameCreationWindow gcw = new GameCreationWindow(WindowManager, tunnelHandler);
            gameCreationPanel.AddChild(gcw);
            gameCreationPanel.Tag = gcw;
            gcw.Cancelled += Gcw_Cancelled;
            gcw.GameCreated += Gcw_GameCreated;
            gcw.LoadedGameCreated += Gcw_LoadedGameCreated;

            gameCreationPanel.Hide();

            connectionManager.MainChannel.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                Renderer.GetSafeString(
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
                    connectionManager.GetChannel(game.GameBroadcastChannel).Leave();
                    followedGames.Remove(game.InternalName);
                }
                else if (!followedGames.Contains(game.InternalName) &&
                    UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
                {
                    connectionManager.GetChannel(game.GameBroadcastChannel).Join();
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

            string userName = currentChatChannel.Users[lbPlayerList.SelectedIndex].IRCUser.Name;

            playerContextMenu.Items[1].Text = pmWindow.IsFriend(userName) ? "Remove Friend" : "Add Friend";

            playerContextMenu.Show();
        }

        private void PlayerContextMenu_OptionSelected(object sender, ContextMenuOptionEventArgs e)
        {
            if (lbPlayerList.SelectedIndex < 0 ||
                lbPlayerList.SelectedIndex >= lbPlayerList.Items.Count)
            {
                return;
            }

            string userName = currentChatChannel.Users[lbPlayerList.SelectedIndex].IRCUser.Name;

            switch (e.Index)
            {
                case 0:
                    pmWindow.InitPM(userName);
                    break;
                case 1:
                    pmWindow.ToggleFriend(userName);
                    break;
            }
        }

        /// <summary>
        /// Enables private messaging by PM'ing a user in the player list.
        /// </summary>
        private void LbPlayerList_DoubleLeftClick(object sender, EventArgs e)
        {
            if (lbPlayerList.SelectedItem == null)
            {
                return;
            }

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
            try
            {
                StatisticsSender.Instance.SendCnCNet();
            } catch { }
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
        }

        private void GameLobby_GameLeft(object sender, EventArgs e)
        {
            topBar.SwitchToSecondary();
            isInGameRoom = false;
            SetLogOutButtonText();
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

        private void BtnJoinGame_LeftClick(object sender, EventArgs e)
        {
            LbGameList_DoubleLeftClick(this, EventArgs.Empty);
        }

        private void LbGameList_DoubleLeftClick(object sender, EventArgs e)
        {
            if (isInGameRoom)
            {
                topBar.SwitchToPrimary();
                return;
            }

            if (lbGameList.SelectedIndex < 0 || lbGameList.SelectedIndex >= lbGameList.Items.Count)
                return;

            var mainChannel = connectionManager.MainChannel;

            HostedCnCNetGame hg = (HostedCnCNetGame)lbGameList.Items[lbGameList.SelectedIndex].Tag;

            if (hg.Game.InternalName.ToUpper() != localGameID.ToUpper())
            {
                mainChannel.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                    "The selected game is for " +
                    gameCollection.GetGameNameFromInternalName(hg.Game.InternalName) + "!"));
                return;
            }

            if (hg.Locked)
            {
                mainChannel.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                    "The selected game is locked!"));
                return;
            }

            if (hg.IsLoadedGame)
            {
                if (!hg.Players.Contains(ProgramConstants.PLAYERNAME))
                {
                    mainChannel.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                        "You do not exist in the saved game!"));
                    return;
                }
            }

            if (hg.GameVersion != ProgramConstants.GAME_VERSION)
            {
                // TODO Show warning
            }

            string password = string.Empty;

            if (hg.Passworded)
            {
                passwordRequestWindow.SetHostedGame(hg);
                passwordRequestWindow.Enable();
                return;
            }
            else
            {
                if (!hg.IsLoadedGame)
                {
                    password = Rampastring.Tools.Utilities.CalculateSHA1ForString
                        (hg.ChannelName + hg.RoomName).Substring(0, 10);
                }
                else
                {
                    IniFile spawnSGIni = new IniFile(ProgramConstants.GamePath + "Saved Games\\spawnSG.ini");
                    password = Rampastring.Tools.Utilities.CalculateSHA1ForString(
                        spawnSGIni.GetStringValue("Settings", "GameID", string.Empty)).Substring(0, 10);
                }
            }

            JoinGame(hg, password);
        }

        private void PasswordRequestWindow_PasswordEntered(object sender, PasswordEventArgs e)
        {
            JoinGame(e.HostedGame, e.Password);
        }

        private void JoinGame(HostedCnCNetGame hg, string password)
        {
            connectionManager.MainChannel.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                "Attempting to join game " + hg.RoomName + "..."));

            Channel gameChannel = connectionManager.CreateChannel(hg.RoomName, hg.ChannelName, false, password);
            connectionManager.AddChannel(gameChannel);

            if (hg.IsLoadedGame)
            {
                gameLoadingLobby.SetUp(false, hg.TunnelServer, gameChannel, hg.HostName);
                gameChannel.UserAdded += GameLoadingChannel_UserAdded;
                gameChannel.MessageAdded += GameLoadingChannel_MessageAdded;
            }
            else
            {
                gameLobby.SetUp(gameChannel, false, hg.MaxPlayers, hg.TunnelServer, hg.HostName, hg.Passworded);
                gameChannel.UserAdded += GameChannel_UserAdded;
                gameChannel.MessageAdded += GameChannel_MessageAdded;
            }

            connectionManager.SendCustomMessage(new QueuedMessage("JOIN " + hg.ChannelName + " " + password,
                QueuedMessageType.INSTANT_MESSAGE, 0));
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

            Channel gameChannel = connectionManager.CreateChannel(e.GameRoomName, channelName, false, password);
            connectionManager.AddChannel(gameChannel);
            gameLobby.SetUp(gameChannel, true, e.MaxPlayers, e.Tunnel, ProgramConstants.PLAYERNAME, isCustomPassword);
            gameChannel.UserAdded += GameChannel_UserAdded;
            gameChannel.MessageAdded += GameChannel_MessageAdded;
            connectionManager.SendCustomMessage(new QueuedMessage("JOIN " + channelName + " " + password,
                QueuedMessageType.INSTANT_MESSAGE, 0));
            connectionManager.MainChannel.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                "Creating a game named " + e.GameRoomName + "..."));

            gameCreationPanel.Hide();
        }

        private void GameChannel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            Channel gameChannel = (Channel)sender;

            gameChannel.UserAdded -= GameChannel_UserAdded;
            gameChannel.MessageAdded -= GameChannel_MessageAdded;
        }

        private void GameChannel_UserAdded(object sender, Online.ChannelUserEventArgs e)
        {
            Channel gameChannel = (Channel)sender;

            if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
            {
                gameChannel.UserAdded -= GameChannel_UserAdded;
                gameChannel.MessageAdded -= GameChannel_MessageAdded;

                gameLobby.OnJoined();
                isInGameRoom = true;
                SetLogOutButtonText();
            }
        }

        private void Gcw_LoadedGameCreated(object sender, GameCreationEventArgs e)
        {
            if (gameLobby.Enabled || gameLoadingLobby.Enabled)
                return;

            string channelName = RandomizeChannelName();

            Channel gameLoadingChannel = connectionManager.CreateChannel(e.GameRoomName, channelName, false, e.Password);
            connectionManager.AddChannel(gameLoadingChannel);
            gameLoadingLobby.SetUp(true, e.Tunnel, gameLoadingChannel, ProgramConstants.PLAYERNAME);
            gameLoadingChannel.UserAdded += GameLoadingChannel_UserAdded;
            gameLoadingChannel.MessageAdded += GameLoadingChannel_MessageAdded;
            connectionManager.SendCustomMessage(new QueuedMessage("JOIN " + channelName + " " + e.Password,
                QueuedMessageType.INSTANT_MESSAGE, 0));
            connectionManager.MainChannel.AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                "Creating a game named " + e.GameRoomName + "..."));

            gameCreationPanel.Hide();
        }

        private void GameLoadingChannel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            Channel gameLoadingChannel = (Channel)sender;

            gameLoadingChannel.UserAdded -= GameLoadingChannel_UserAdded;
            gameLoadingChannel.MessageAdded -= GameLoadingChannel_MessageAdded;
        }

        private void GameLoadingChannel_UserAdded(object sender, ChannelUserEventArgs e)
        {
            Channel gameLoadingChannel = (Channel)sender;

            if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
            {
                gameLoadingChannel.UserAdded -= GameLoadingChannel_UserAdded;
                gameLoadingChannel.MessageAdded -= GameLoadingChannel_MessageAdded;

                gameLoadingLobby.OnJoined();
                isInGameRoom = true;
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
                string channelName = "#cncnet-" + localGameID.ToLower() + "-game" + new Random().Next(1000000, 9999999);
                int index = lbGameList.HostedGames.FindIndex(c => ((HostedCnCNetGame)c).ChannelName == channelName);
                if (index == -1)
                    return channelName;
            }
        }

        private void Gcw_Cancelled(object sender, EventArgs e)
        {
            gameCreationPanel.Hide();
        }

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
        }

        private void ConnectionManager_WelcomeMessageReceived(object sender, EventArgs e)
        {
            btnNewGame.AllowClick = true;
            btnJoinGame.AllowClick = true;
            ddCurrentChannel.AllowDropDown = true;
            tbChatInput.Enabled = true;

            Channel cncnetChannel = connectionManager.GetChannel("#cncnet");
            cncnetChannel.Join();

            string localGameChatChannelName = gameCollection.GetGameChatChannelNameFromIdentifier(localGameID);
            Channel localGameChatChannel = connectionManager.GetChannel(localGameChatChannelName);
            localGameChatChannel.Join();

            string localGameBroadcastChannel = gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGameID);
            connectionManager.GetChannel(localGameBroadcastChannel).Join();

            cncnetChannel.RequestUserInfo();
            localGameChatChannel.RequestUserInfo();

            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported)
                    continue;

                if (game.InternalName.ToUpper() != localGameID)
                {
                    if (UserINISettings.Instance.IsGameFollowed(game.InternalName.ToUpper()))
                    {
                        connectionManager.GetChannel(game.GameBroadcastChannel).Join();
                        followedGames.Add(game.InternalName);
                    }
                }
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
                    currentChatChannel.ChannelName != string.Format("#cncnet-{0}", localGameID.ToLower()))
                {
                    // Remove the assigned channels from the users so we don't have ghost users on the PM user list
                    foreach (var user in currentChatChannel.Users)
                        connectionManager.RemoveChannelFromUser(user.IRCUser.Name, currentChatChannel.ChannelName);

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
                currentChatChannel.ChannelName != string.Format("#cncnet-{0}", localGameID.ToLower()))
            {
                currentChatChannel.Join();
                currentChatChannel.RequestUserInfo();
            }
        }

        private void RefreshPlayerList(object sender, EventArgs e)
        {
            string selectedUserName = lbPlayerList.SelectedItem == null ?
                string.Empty : lbPlayerList.SelectedItem.Text;
            lbPlayerList.Clear();

            foreach (ChannelUser user in currentChatChannel.Users)
            {
                AddUser(user);
            }

            if (selectedUserName != string.Empty)
            {
                lbPlayerList.SelectedIndex = lbPlayerList.Items.FindIndex(
                    i => i.Text == selectedUserName);
            }
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
            lbChatMessages.AddMessage(message);
        }

        private void CurrentChatChannel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            AddMessageToChat(e.Message);
        }

        private void AddUser(ChannelUser user)
        {
            XNAListBoxItem item = new XNAListBoxItem();

            item.Tag = user;

            if (user.IsAdmin)
            {
                item.Text = user.IRCUser.Name + " (Admin)";
                item.TextColor = cAdminNameColor;
                item.Texture = adminGameIcon;
            }
            else
            {
                item.Text = user.IRCUser.Name;
                item.TextColor = UISettings.AltColor;

                if (user.IRCUser.GameID < 0 || user.IRCUser.GameID >= gameCollection.GameList.Count)
                    item.Texture = unknownGameIcon;
                else
                    item.Texture = gameCollection.GameList[user.IRCUser.GameID].Texture;
            }

            lbPlayerList.AddItem(item);
        }

        private void GameBroadcastChannel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
        {
            var channel = (Channel)sender;

            var channelUser = channel.Users.Find(u => u.IRCUser.Name == e.UserName);

            if (channelUser == null)
                return;

            if (localGame != null &&
                channel.ChannelName == localGame.GameBroadcastChannel &&
                !updateDenied &&
                channelUser.IsAdmin &&
                e.Message.StartsWith("UPDATE ") &&
                e.Message.Length > 7)
            {
                string version = e.Message.Substring(7);
                if (version != ProgramConstants.GAME_VERSION)
                {
                    var updateMessageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "Update available",
                        "An update is available. Do you want to perform the update now?");
                    updateMessageBox.NoClicked += UpdateMessageBox_NoClicked;
                    updateMessageBox.YesClicked += UpdateMessageBox_YesClicked;
                }
            }

            if (!e.Message.StartsWith("GAME "))
                return;

            string msg = e.Message.Substring(5); // Cut out GAME part
            string[] splitMessage = msg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (splitMessage.Length < 11 || splitMessage.Length > 12) // Support for optional isRA2Mode param
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
                string tunnelAddress = splitMessage[9];
                string loadedGameId = splitMessage[10];
                bool isRA2Mode = 11 < splitMessage.Length ? Conversions.BooleanFromString(splitMessage[11], false) : false;

                CnCNetGame cncnetGame = gameCollection.GameList.Find(g => g.GameBroadcastChannel == channel.ChannelName);

                CnCNetTunnel tunnel = tunnelHandler.Tunnels.Find(t => t.Address == tunnelAddress);

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
                    }

                    return;
                }

                // Seek for the game in the internal game list based on its channel name;
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
                        !ProgramConstants.IsInGame)
                    {
                        AudioMaster.PlaySound(sndGameCreated);
                    }

                    lbGameList.AddGame(game);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Game parsing error:" + ex.Message);
            }
        }

        private void UpdateMessageBox_YesClicked(object sender, EventArgs e)
        {
            UpdateCheck?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateMessageBox_NoClicked(object sender, EventArgs e)
        {
            updateDenied = true;
        }

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

        protected override void OnVisibleChanged(object sender, EventArgs args)
        {
            base.OnVisibleChanged(sender, args);
        }

        public void SwitchOn()
        {
            Visible = true;
            Enabled = true;

            if (!connectionManager.IsConnected && !connectionManager.IsAttemptingConnection)
            {
                loginWindow.Enable();
                loginWindow.LoadSettings();
            }

            SetLogOutButtonText();
        }

        public void SwitchOff()
        {
            Visible = false;
            Enabled = false;
        }

        public string GetSwitchName()
        {
            return "CnCNet Lobby";
        }
    }
}
