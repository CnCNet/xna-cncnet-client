using ClientGUI;
using DTAClient.Online;
using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using ClientCore;
using DTAClient.Online.EventArguments;
using Rampastring.Tools;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using DTAClient.Properties;
using DTAClient.domain.CnCNet;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI.Input;
using HostedGame = DTAClient.domain.CnCNet.HostedGame;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.DXGUI.Generic;

namespace DTAClient.DXGUI.Multiplayer
{
    public class CnCNetLobby : DXWindow
    {
        const int GAME_REFRESH_RATE = 120;
        const double GAME_LIFETIME = 35.0;

        public CnCNetLobby(WindowManager windowManager, CnCNetManager connectionManager,
            CnCNetGameLobby gameLobby, TunnelHandler tunnelHandler)
            : base(windowManager)
        {
            this.connectionManager = connectionManager;
            ClientRectangle = new Rectangle(0, 0, 1200, 720);
            this.gameLobby = gameLobby;
            this.tunnelHandler = tunnelHandler;
        }

        CnCNetManager connectionManager;

        DXListBox lbPlayerList;
        ChatListBox lbChatMessages;
        GameListBox lbGameList;

        LinkButton btnForums;
        LinkButton btnTwitter;
        LinkButton btnGooglePlus;
        LinkButton btnYoutube;
        LinkButton btnFacebook;
        LinkButton btnModDB;
        LinkButton btnHomepage;

        DXButton btnLogout;
        DXButton btnNewGame;
        DXButton btnJoinGame;

        DXTextBox tbChatInput;

        List<CnCNetTunnel> tunnelList = new List<CnCNetTunnel>();

        DXLabel lblColor;
        DXLabel lblCurrentChannel;
        //DXLabel lblGameInformation;

        DarkeningPanel gameCreationPanel;

        GameCollection gameCollection;

        DXDropDown ddColor;
        DXDropDown ddCurrentChannel;
        Channel currentChatChannel;

        Color cAdminNameColor;

        Texture2D unknownGameIcon;
        Texture2D adminGameIcon;

        List<HostedGame> hostedGames = new List<HostedGame>();

        SoundEffectInstance sndGameCreated;

        IRCColor[] chatColors;

        CnCNetGameLobby gameLobby;

        TunnelHandler tunnelHandler;

        int framesSinceGameRefresh;

        string localGame;

        public override void Initialize()
        {
            Name = "CnCNetLobby";
            BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbybg.png");
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            localGame = DomainController.Instance().GetDefaultGame();

            btnNewGame = new DXButton(WindowManager);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.ClientRectangle = new Rectangle(12, ClientRectangle.Height - 29, 133, 23);
            btnNewGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnNewGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnNewGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnNewGame.FontIndex = 1;
            btnNewGame.Text = "Create Game";
            btnNewGame.AllowClick = false;
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnJoinGame = new DXButton(WindowManager);
            btnJoinGame.Name = "btnJoinGame";
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.ClientRectangle.Right + 12,
                btnNewGame.ClientRectangle.Y, 133, 23);
            btnJoinGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnJoinGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnJoinGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnJoinGame.FontIndex = 1;
            btnJoinGame.Text = "Join Game";
            btnJoinGame.AllowClick = false;

            btnLogout = new DXButton(WindowManager);
            btnLogout.Name = "btnLogout";
            btnLogout.ClientRectangle = new Rectangle(ClientRectangle.Width - 145, btnNewGame.ClientRectangle.Y,
                133, 23);
            btnLogout.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnLogout.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnLogout.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnLogout.FontIndex = 1;
            btnLogout.Text = "Main Menu";
            btnLogout.LeftClick += BtnLogout_LeftClick;

            btnForums = new LinkButton(WindowManager);
            btnForums.Name = "btnForums";
            btnForums.ClientRectangle = new Rectangle(ClientRectangle.Width - 33, 12, 21, 21);
            btnForums.IdleTexture = AssetLoader.LoadTexture("forumsInactive.png");
            btnForums.HoverTexture = AssetLoader.LoadTexture("forumsActive.png");
            btnForums.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnForums.URL = DomainController.Instance().GetForumURL();

            btnTwitter = new LinkButton(WindowManager);
            btnTwitter.Name = "btnTwitter";
            btnTwitter.ClientRectangle = new Rectangle(ClientRectangle.Width - 61, 12, 21, 21);
            btnTwitter.IdleTexture = AssetLoader.LoadTexture("twitterInactive.png");
            btnTwitter.HoverTexture = AssetLoader.LoadTexture("twitterActive.png");
            btnTwitter.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnTwitter.URL = DomainController.Instance().GetTwitterURL();

            btnGooglePlus = new LinkButton(WindowManager);
            btnGooglePlus.Name = "btnGooglePlus";
            btnGooglePlus.ClientRectangle = new Rectangle(ClientRectangle.Width - 89, 12, 21, 21);
            btnGooglePlus.IdleTexture = AssetLoader.LoadTexture("googlePlusInactive.png");
            btnGooglePlus.HoverTexture = AssetLoader.LoadTexture("googlePlusActive.png");
            btnGooglePlus.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnGooglePlus.URL = DomainController.Instance().GetGooglePlusURL();

            btnYoutube = new LinkButton(WindowManager);
            btnYoutube.Name = "btnYoutube";
            btnYoutube.ClientRectangle = new Rectangle(ClientRectangle.Width - 117, 12, 21, 21);
            btnYoutube.IdleTexture = AssetLoader.LoadTexture("youtubeInactive.png");
            btnYoutube.HoverTexture = AssetLoader.LoadTexture("youtubeActive.png");
            btnYoutube.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnYoutube.URL = DomainController.Instance().GetYoutubeURL();

            btnFacebook = new LinkButton(WindowManager);
            btnFacebook.Name = "btnFacebook";
            btnFacebook.ClientRectangle = new Rectangle(ClientRectangle.Width - 145, 12, 21, 21);
            btnFacebook.IdleTexture = AssetLoader.LoadTexture("facebookInactive.png");
            btnFacebook.HoverTexture = AssetLoader.LoadTexture("facebookActive.png");
            btnFacebook.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnFacebook.URL = DomainController.Instance().GetFacebookURL();

            btnModDB = new LinkButton(WindowManager);
            btnModDB.Name = "btnModDB";
            btnModDB.ClientRectangle = new Rectangle(ClientRectangle.Width - 173, 12, 21, 21);
            btnModDB.IdleTexture = AssetLoader.LoadTexture("moddbInactive.png");
            btnModDB.HoverTexture = AssetLoader.LoadTexture("moddbActive.png");
            btnModDB.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnModDB.URL = DomainController.Instance().GetModDBURL();

            btnHomepage = new LinkButton(WindowManager);
            btnHomepage.Name = "btnHomepage";
            btnHomepage.ClientRectangle = new Rectangle(ClientRectangle.Width - 201, 12, 21, 21);
            btnHomepage.IdleTexture = AssetLoader.LoadTexture("homepageInactive.png");
            btnHomepage.HoverTexture = AssetLoader.LoadTexture("homepageActive.png");
            btnHomepage.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnHomepage.URL = DomainController.Instance().GetHomepageURL();

            lbGameList = new GameListBox(WindowManager, hostedGames, localGame);
            lbGameList.Name = "lbGameList";
            lbGameList.ClientRectangle = new Rectangle(btnNewGame.ClientRectangle.X,
                41, btnJoinGame.ClientRectangle.Right - btnNewGame.ClientRectangle.X,
                btnNewGame.ClientRectangle.Top - 47);
            lbGameList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);

            lbPlayerList = new DXListBox(WindowManager);
            lbPlayerList.Name = "lbPlayerList";
            lbPlayerList.ClientRectangle = new Rectangle(ClientRectangle.Width - 202,
                btnForums.ClientRectangle.Bottom + 8, 190, 
                btnLogout.ClientRectangle.Top - btnForums.ClientRectangle.Bottom - 14);
            lbPlayerList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbPlayerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbPlayerList.LineHeight = 16;

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.ClientRectangle = new Rectangle(lbGameList.ClientRectangle.Right + 9, lbGameList.ClientRectangle.Y,
                lbPlayerList.ClientRectangle.Left - lbGameList.ClientRectangle.Right - 18, lbPlayerList.ClientRectangle.Height);
            lbChatMessages.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;

            tbChatInput = new DXTextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X,
                btnNewGame.ClientRectangle.Y, lbChatMessages.ClientRectangle.Width, 
                btnNewGame.ClientRectangle.Height);
            tbChatInput.Enabled = false;
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            lblColor = new DXLabel(WindowManager);
            lblColor.Name = "lblColor";
            lblColor.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X, 14, 0, 0);
            lblColor.FontIndex = 1;
            lblColor.Text = "YOUR COLOR:";

            ddColor = new DXDropDown(WindowManager);
            ddColor.Name = "ddColor";
            ddColor.ClientRectangle = new Rectangle(lblColor.ClientRectangle.X + 95, btnForums.ClientRectangle.Y,
                150, 21);
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;

            chatColors = connectionManager.GetIRCColors();

            foreach (IRCColor color in connectionManager.GetIRCColors())
            {
                if (!color.Selectable)
                    continue;

                DXDropDownItem ddItem = new DXDropDownItem();
                ddItem.Text = color.Name;
                ddItem.TextColor = color.XnaColor;
                ddItem.Tag = color;

                ddColor.AddItem(ddItem);
            }

            int selectedColor = DomainController.Instance().GetCnCNetChatColor();

            ddColor.SelectedIndex = selectedColor >= ddColor.Items.Count || selectedColor < 0 
                ? DomainController.Instance().GetDefaultPersonalChatColor() : 
                selectedColor;

            ddCurrentChannel = new DXDropDown(WindowManager);
            ddCurrentChannel.Name = "ddCurrentChannel";
            ddCurrentChannel.ClientRectangle = new Rectangle(
                lbChatMessages.ClientRectangle.Right - 200,
                ddColor.ClientRectangle.Y, 200, 21);
            ddCurrentChannel.SelectedIndexChanged += DdCurrentChannel_SelectedIndexChanged;
            ddCurrentChannel.AllowDropDown = false;

            gameCollection = connectionManager.GetGameCollection();

            int i = 0;

            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported)
                {
                    i++;
                    continue;
                }

                DXDropDownItem item = new DXDropDownItem();
                item.Text = game.UIName;
                item.TextColor = UISettings.AltColor;
                item.Texture = game.Texture;

                ddCurrentChannel.AddItem(item);

                Channel chatChannel = connectionManager.GetChannel(game.ChatChannel);

                if (chatChannel == null)
                {
                    chatChannel = connectionManager.CreateChannel(game.UIName, game.ChatChannel,
                        true, null);
                    connectionManager.AddChannel(chatChannel);
                }

                item.Tag = chatChannel;

                Channel gameBroadcastChannel = connectionManager.GetChannel(game.GameBroadcastChannel);

                if (gameBroadcastChannel == null)
                {
                    gameBroadcastChannel = connectionManager.CreateChannel(game.UIName + " Broadcast Channel",
                        game.GameBroadcastChannel, true, null);
                    connectionManager.AddChannel(gameBroadcastChannel);
                }

                gameBroadcastChannel.CTCPReceived += GameBroadcastChannel_CTCPReceived;

                if (game.InternalName.ToUpper() == localGame)
                {
                    ddCurrentChannel.SelectedIndex = i;
                    connectionManager.SetMainChannel(chatChannel);
                }

                i++;
            }

            lblCurrentChannel = new DXLabel(WindowManager);
            lblCurrentChannel.Name = "lblCurrentChannel";
            lblCurrentChannel.ClientRectangle = new Rectangle(
                ddCurrentChannel.ClientRectangle.X - 150,
                ddCurrentChannel.ClientRectangle.Y + 2, 0, 0);
            lblCurrentChannel.FontIndex = 1;
            lblCurrentChannel.Text = "CURRENT CHANNEL:";

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

            SoundEffect gameCreatedSoundEffect = AssetLoader.LoadSound("gamecreated.wav");

            if (gameCreatedSoundEffect != null)
                sndGameCreated = gameCreatedSoundEffect.CreateInstance();

            cAdminNameColor = AssetLoader.GetColorFromString(DomainController.Instance().GetAdminNameColor());
            unknownGameIcon = AssetLoader.TextureFromImage(Resources.unknownicon);
            adminGameIcon = AssetLoader.TextureFromImage(Resources.cncneticon);

            connectionManager.WelcomeMessageReceived += ConnectionManager_WelcomeMessageReceived;
            connectionManager.Disconnected += ConnectionManager_Disconnected;

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;

            base.Initialize();

            WindowManager.CenterControlOnScreen(this);

            gameCreationPanel = new DarkeningPanel(WindowManager);
            AddChild(gameCreationPanel);

            GameCreationWindow gcw = new GameCreationWindow(WindowManager, tunnelHandler);
            gameCreationPanel.AddChild(gcw);
            gcw.Cancelled += Gcw_Cancelled;
            gcw.GameCreated += Gcw_GameCreated;

            gameCreationPanel.Hide();
        }

        private void BtnNewGame_LeftClick(object sender, EventArgs e)
        {
            gameCreationPanel.Show();
        }

        private void Gcw_GameCreated(object sender, GameCreationEventArgs e)
        {
            if (gameLobby.Enabled)
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
                QueuedMessageType.GAME_HOSTING_MESSAGE, 9));
            connectionManager.MainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now,
                "Creating a game named " + e.GameRoomName + "..."));

            gameCreationPanel.Hide();
        }

        private void GameChannel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            Channel gameChannel = (Channel)sender;

            gameChannel.UserAdded -= GameChannel_UserAdded;
            gameChannel.MessageAdded -= GameChannel_MessageAdded;
        }

        private void GameChannel_UserAdded(object sender, UserEventArgs e)
        {
            Channel gameChannel = (Channel)sender;

            gameChannel.UserAdded -= GameChannel_UserAdded;
            gameChannel.MessageAdded -= GameChannel_MessageAdded;

            if (e.User.Name == ProgramConstants.PLAYERNAME)
            {
                gameLobby.OnJoined();
                gameLobby.Visible = true;
                gameLobby.Enabled = true;
                Visible = false;
                Enabled = false;
                // TODO enter persistent mode
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
                string channelName = "#cncnet-" + localGame.ToLower() + "-game" + new Random().Next(1000000, 9999999);
                int index = hostedGames.FindIndex(c => c.ChannelName == channelName);
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

        private void DdColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            IRCColor selectedColor = (IRCColor)ddColor.SelectedItem.Tag;
            tbChatInput.TextColor = selectedColor.XnaColor;
            gameLobby.ChangeChatColor(selectedColor);
        }

        private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (e.PressedKey == Keys.C && Keyboard.IsKeyHeldDown(Keys.RightShift))
                connectionManager.Connect();
        }

        private void ConnectionManager_Disconnected(object sender, EventArgs e)
        {
            btnNewGame.AllowClick = false;
            btnJoinGame.AllowClick = false;
            ddCurrentChannel.AllowDropDown = false;
            tbChatInput.Enabled = false;
            gameCreationPanel.Hide();
        }

        private void ConnectionManager_WelcomeMessageReceived(object sender, EventArgs e)
        {
            btnNewGame.AllowClick = true;
            btnJoinGame.AllowClick = true;
            ddCurrentChannel.AllowDropDown = true;
            tbChatInput.Enabled = true;

            Channel cncnetChannel = connectionManager.GetChannel("#cncnet");
            cncnetChannel.Join();

            string localGameChatChannelName = gameCollection.GetGameChatChannelNameFromIdentifier(localGame);
            Channel localGameChatChannel = connectionManager.GetChannel(localGameChatChannelName);
            localGameChatChannel.Join();

            string localGameBroadcastChannel = gameCollection.GetGameBroadcastingChannelNameFromIdentifier(localGame);
            connectionManager.GetChannel(localGameBroadcastChannel).Join();

            cncnetChannel.RequestUserInfo();
            localGameChatChannel.RequestUserInfo();

            foreach (CnCNetGame game in gameCollection.GameList)
            {
                if (!game.Supported)
                    continue;

                if (game.InternalName != localGame.ToLower())
                {
                    if (DomainController.Instance().GetGameEnabledStatus(game.InternalName))
                    {
                        connectionManager.GetChannel(game.GameBroadcastChannel).Join();
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
                currentChatChannel.UserListReceived -= RefreshPlayerList;
                currentChatChannel.MessageAdded -= CurrentChatChannel_MessageAdded;
                currentChatChannel.UserGameIndexUpdated -= CurrentChatChannel_UserGameIndexUpdated;

                if (currentChatChannel.ChannelName != "#cncnet" &&
                    currentChatChannel.ChannelName != string.Format("#cncnet-{0}", localGame.ToLower()))
                {
                    currentChatChannel.Leave();
                }
            }

            currentChatChannel = (Channel)ddCurrentChannel.SelectedItem.Tag;
            currentChatChannel.UserAdded += RefreshPlayerList;
            currentChatChannel.UserLeft += RefreshPlayerList;
            currentChatChannel.UserQuitIRC += RefreshPlayerList;
            currentChatChannel.UserListReceived += RefreshPlayerList;
            currentChatChannel.MessageAdded += CurrentChatChannel_MessageAdded;
            currentChatChannel.UserGameIndexUpdated += CurrentChatChannel_UserGameIndexUpdated;
            connectionManager.SetMainChannel(currentChatChannel);

            lbChatMessages.TopIndex = 0;
            lbChatMessages.Clear();
            currentChatChannel.Messages.ForEach(msg => AddMessageToChat(msg));

            RefreshPlayerList(this, EventArgs.Empty);

            if (currentChatChannel.ChannelName != "#cncnet" &&
                currentChatChannel.ChannelName != string.Format("#cncnet-{0}", localGame.ToLower()))
            {
                currentChatChannel.Join();
                currentChatChannel.RequestUserInfo();
            }
        }

        private void RefreshPlayerList(object sender, EventArgs e)
        {
            lbPlayerList.Clear();

            foreach (IRCUser user in currentChatChannel.Users)
            {
                AddUser(user);
            }
        }

        private void CurrentChatChannel_UserGameIndexUpdated(object sender, UserEventArgs e)
        {
            DXListBoxItem item = lbPlayerList.Items.Find(i => i.Text.StartsWith(e.User.Name));

            if (e.User.GameID < 0 || e.User.GameID >= gameCollection.GameList.Count)
                item.Texture = unknownGameIcon;
            else
                item.Texture = gameCollection.GameList[e.User.GameID].Texture;
        }

        private void AddMessageToChat(IRCMessage message)
        {
            if (message.Sender == null)
                lbChatMessages.AddItem(string.Format("[{0}] {1}",
                    message.DateTime.ToShortTimeString(),
                    Renderer.GetSafeString(message.Message, lbChatMessages.FontIndex)),
                    message.Color, true);
            else
                lbChatMessages.AddItem(string.Format("[{0}] {1}: {2}", 
                    message.DateTime.ToShortTimeString(), message.Sender, 
                    Renderer.GetSafeString(message.Message, lbChatMessages.FontIndex)),
                    message.Color, true);

            if (lbChatMessages.GetLastDisplayedItemIndex() == lbChatMessages.Items.Count - 2)
            {
                lbChatMessages.ScrollToBottom();
            }
        }

        private void CurrentChatChannel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            AddMessageToChat(e.Message);
        }

        private void AddUser(IRCUser user)
        {
            DXListBoxItem item = new DXListBoxItem();

            item.Tag = user;

            if (user.IsAdmin)
            {
                item.Text = user.Name + " (Admin)";
                item.TextColor = cAdminNameColor;
                item.Texture = adminGameIcon;
            }
            else
            {
                item.Text = user.Name;
                item.TextColor = UISettings.AltColor;

                if (user.GameID < 0 || user.GameID >= gameCollection.GameList.Count)
                    item.Texture = unknownGameIcon;
                else
                    item.Texture = gameCollection.GameList[user.GameID].Texture;
            }

            lbPlayerList.AddItem(item);
        }

        private void GameBroadcastChannel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
        {
            if (!e.Message.StartsWith("GAME "))
                return;

            string msg = e.Message.Substring(5); // Cut out GAME part
            string[] splitMessage = msg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            Channel channel = (Channel)sender;

            if (splitMessage.Length != 10)
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
                string loadedGameId = splitMessage[9];

                CnCNetGame cncnetGame = gameCollection.GameList.Find(g => g.GameBroadcastChannel == channel.ChannelName);

                HostedGame game = new HostedGame(gameRoomChannelName, revision, cncnetGame.InternalName, gameVersion, maxPlayers,
                    gameRoomDisplayName, isCustomPassword, false, locked, true, false, false, players,
                    e.UserName, mapName, gameMode);
                game.IsLoadedGame = isLoadedGame;
                game.MatchID = loadedGameId;
                game.LastRefreshTime = DateTime.Now;
                game.IsLadder = isLadder;
                game.GameTexture = cncnetGame.Texture;
                game.IsLocked = game.Started || (game.IsLoadedGame && !game.Players.Contains(ProgramConstants.PLAYERNAME));
                game.IsIncompatible = game.Version != ProgramConstants.GAME_VERSION;

                if (isClosed)
                {
                    int index = hostedGames.FindIndex(hg => hg.Admin == e.UserName);

                    if (index > -1)
                    {
                        hostedGames.RemoveAt(index);
                        lbGameList.Refresh();
                    }

                    return;
                }

                // Seek for the game in the internal game list based on its channel name;
                // if found, then refresh that game's information, otherwise add as new game
                int gameIndex = hostedGames.FindIndex(hg => hg.Admin == e.UserName);

                if (gameIndex > -1)
                {
                    hostedGames[gameIndex] = game;
                }
                else
                {
                    if (cncnetGame.InternalName == localGame.ToLower() &&
                        !ProgramConstants.IsInGame &&
                        DomainController.Instance().GetGameHostedSoundEnabledStatus())
                    {
                        sndGameCreated.Play();
                    }

                    hostedGames.Insert(0, game);
                }

                lbGameList.Refresh();
            }
            catch (Exception ex)
            {
                Logger.Log("Game parsing error:" + ex.Message);
            }
        }

        private void RefreshGameList()
        {
            lbGameList.Clear();

            foreach (HostedGame game in hostedGames)
            {
                
            }
        }

        private void BtnLogout_LeftClick(object sender, EventArgs e)
        {
            this.Visible = false;
            this.Enabled = false;

            connectionManager.Disconnect();
        }

        public override void Update(GameTime gameTime)
        {
            framesSinceGameRefresh++;

            if (framesSinceGameRefresh > GAME_REFRESH_RATE)
            {
                for (int i = 0; i < hostedGames.Count; i++)
                {
                    if (DateTime.Now - hostedGames[i].LastRefreshTime > TimeSpan.FromSeconds(GAME_LIFETIME))
                    {
                        hostedGames.RemoveAt(i);
                        i--;

                        if (lbGameList.SelectedIndex == i)
                            lbGameList.SelectedIndex = -1;
                        else if (lbGameList.SelectedIndex > i)
                            lbGameList.SelectedIndex--;
                    }
                }

                framesSinceGameRefresh = 0;
            }

            base.Update(gameTime);
        }
    }
}
