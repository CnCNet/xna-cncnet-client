/// @author Rampastring
/// http://www.moddb.com/members/rampastring

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.IO;
using ClientCore;
using ClientCore.LAN;
using ClientCore.CnCNet5;
using ClientCore.CnCNet5.Games;
using DTAConfig;
using Rampastring.Tools;

namespace ClientGUI.LAN
{
    public partial class LANLobby : MovableForm
    {
        /// <summary>
        /// Creates a new instance of the LAN lobby.
        /// Called by the main client executable when connecting to LAN.
        /// </summary>
        /// <param name="gameVersion">The version of the game.</param>
        public LANLobby(string gameVersion)
        {
            Application.VisualStyleState = System.Windows.Forms.VisualStyles.VisualStyleState.NonClientAreaEnabled;
            InitializeComponent();
            ProgramConstants.GAME_VERSION = gameVersion;
        }

        public delegate void ColorChangedEventHandler(int colorId);
        public static event ColorChangedEventHandler OnColorChanged;

        delegate void PlayerDelegate(LANPlayer p);
        delegate void ChatDelegate(string sender, string message, int color);
        delegate void EventDelegate(object sender, EventArgs eventargs);
        delegate void StringDelegate(string str);
        delegate void NoticeDelegate(string message, Color color);

        FormWindowState oldWindowState = FormWindowState.Normal;

        /// <summary>
        /// Used for informing the Game Lobby about the user changing their color.
        /// </summary>
        /// <param name="colorId"></param>
        public static void DoColorChanged(int colorId)
        {
            if (OnColorChanged != null)
                OnColorChanged(colorId);
        }

        // Various callbacks needed for thread-safety

        List<MessageInfo> MessageInfos = new List<MessageInfo>();
        List<LANPlayer> Players = new List<LANPlayer>();
        List<Color> ChatColors = new List<Color>();

        List<LANGame> Games = new List<LANGame>();

        /// <summary>
        /// List of colors that the current games are displayed in.
        /// </summary>
        List<Color> GameColors = new List<Color>();

        Color cPlayerNameColor;
        Color cDefaultChatColor;
        Color cPmOtherUserColor;
        Color cLockedGameColor;
        Color cListBoxFocusColor;

        /// <summary>
        /// The ID of the currently viewed chat channel.
        /// </summary>
        int currentChannelId = 0;

        SoundPlayer sndGameCreated;

        Image lockedGameIcon;
        Image incompatibleGameIcon;
        Image passwordedGameIcon;

        Image[] gameIcons;

        /// <summary>
        /// The ID string of the current game.
        /// </summary>
        string myGame = "DTA";

        bool gameInProgress = false;

        /// <summary>
        /// The game lobby form.
        /// </summary>
        LANGameLobby gameLobbyForm;

        /// <summary>
        /// The game loading lobby form.
        /// </summary>
        LANLoadingLobby gameLoadingLobbyForm;

        string chatTipText;

        string lastInputCommand = String.Empty;

        Socket socket;
        IPEndPoint endPoint;

        private static readonly object gameLocker = new object();

        /// <summary>
        /// Initializes the lobby. Loads graphics and subscribes to events triggered
        /// by the networking part of the client.
        /// </summary>
        private void NCnCNetLobby_Load(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += Application_ThreadException;

            ProgramConstants.PLAYERNAME = ProgramConstants.PLAYERNAME.Replace('^', '¨').Replace('~', '-').
                Replace(';', 'x').Replace(':', 'x').Replace(',', '.');

            myGame = DomainController.Instance().GetDefaultGame();

            int locY = DomainController.Instance().GetLobbyLocationY();
            int locX = DomainController.Instance().GetLobbyLocationX();

            Point location = new Point(locX, locY);

            // Check so we don't move the window outside of the screen
            // (could happen if the user has for example changed their monitor or resolution from the last run)
            Screen screen = Screen.FromPoint(location);
            if (screen.Bounds.Contains(location))
                this.Location = location;

            this.Font = SharedLogic.GetCommonFont();

            chatTipText = DomainController.Instance().GetChatTipText();
            tbChatInput.Text = chatTipText;

            Font listBoxFont = SharedLogic.GetListBoxFont();

            lbChatMessages.Font = listBoxFont;
            lbGameList.Font = listBoxFont;
            lbPlayerList.Font = listBoxFont;

            gameIcons = GameCollection.Instance.GetGameImages();

            sndGameCreated = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "gamecreated.wav");

            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.GetBaseResourcePath() + "clienticon.ico");
            this.BackgroundImage = SharedUILogic.LoadImage("cncnetlobbybg.png");

            string backgroundImageLayout = DomainController.Instance().GetCnCNetLobbyBackgroundImageLayout();
            switch (backgroundImageLayout)
            {
                case "Center":
                    this.BackgroundImageLayout = ImageLayout.Center;
                    break;
                case "Stretch":
                    this.BackgroundImageLayout = ImageLayout.Stretch;
                    break;
                case "Zoom":
                    this.BackgroundImageLayout = ImageLayout.Zoom;
                    break;
                default:
                case "Tile":
                    this.BackgroundImageLayout = ImageLayout.Tile;
                    break;
            }

            cPlayerNameColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetPlayerNameColor());

            cDefaultChatColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetDefaultChatColor());

            cPmOtherUserColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetReceivedPMColor());

            cLockedGameColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetLockedGameColor());

            cListBoxFocusColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetListBoxFocusColor());

            ChatColors.Add(cDefaultChatColor);
            ChatColors.Add(cDefaultChatColor);
            ChatColors.Add(Color.LightBlue);
            ChatColors.Add(Color.LimeGreen);
            ChatColors.Add(Color.IndianRed);
            ChatColors.Add(Color.Red);
            ChatColors.Add(Color.MediumOrchid);
            ChatColors.Add(Color.Orange);
            ChatColors.Add(Color.Yellow);
            ChatColors.Add(Color.Lime);
            ChatColors.Add(Color.Turquoise);
            ChatColors.Add(Color.LightCyan);
            ChatColors.Add(Color.LightSkyBlue);
            ChatColors.Add(Color.Fuchsia);
            ChatColors.Add(Color.Gray);
            ChatColors.Add(Color.Gray);

            cmbLANMessageColor.AddItem("Light Blue", ChatColors[2]);
            cmbLANMessageColor.AddItem("Green", ChatColors[3]);
            cmbLANMessageColor.AddItem("Dark Red", ChatColors[4]);
            cmbLANMessageColor.AddItem("Red", ChatColors[5]);
            cmbLANMessageColor.AddItem("Purple", ChatColors[6]);
            cmbLANMessageColor.AddItem("Orange", ChatColors[7]);
            cmbLANMessageColor.AddItem("Yellow", ChatColors[8]);
            cmbLANMessageColor.AddItem("Lime Green", ChatColors[9]);
            cmbLANMessageColor.AddItem("Turquoise", ChatColors[10]);
            cmbLANMessageColor.AddItem("Light Cyan", ChatColors[11]);
            cmbLANMessageColor.AddItem("Sky Blue", ChatColors[12]);
            cmbLANMessageColor.AddItem("Pink", ChatColors[13]);
            cmbLANMessageColor.AddItem("Light Gray", ChatColors[14]);

            cmbLANMessageColor.SelectedIndex = DomainController.Instance().GetCnCNetChatColor();

            Anticheat.Instance = new Anticheat();
            Anticheat.Instance.CalculateHashes();

            Color cLabelColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUILabelColor());

            Color cAltUiColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUIAltColor());

            Color cBackColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor());

            SharedUILogic.SetControlColor(cLabelColor, cBackColor, cAltUiColor, cListBoxFocusColor, this);

            toolTip1.BackColor = cBackColor;
            toolTip1.ForeColor = cLabelColor;

            panelGameInformation.BackgroundImage = SharedUILogic.LoadImage("cncnetlobbypanelbg.png");

            lockedGameIcon = SharedUILogic.LoadImage("lockedgame.png");
            incompatibleGameIcon = SharedUILogic.LoadImage("incompatible.png");
            passwordedGameIcon = SharedUILogic.LoadImage("passwordedgame.png");

            SoundPlayer sp = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "button.wav");

            Image btn92px = SharedUILogic.LoadImage("92pxbtn.png");
            Image btn92px_c = SharedUILogic.LoadImage("92pxbtn_c.png");

            Image btn142px = SharedUILogic.LoadImage("142pxbtn.png");
            Image btn142px_c = SharedUILogic.LoadImage("142pxbtn_c.png");

            btnNewGame.DefaultImage = btn92px;
            btnNewGame.HoveredImage = btn92px_c;
            btnNewGame.HoverSound = sp;
            btnJoinGame.DefaultImage = btn92px;
            btnJoinGame.HoveredImage = btn92px_c;
            btnJoinGame.HoverSound = sp;
            btnReturnToMenu.DefaultImage = btn142px;
            btnReturnToMenu.HoveredImage = btn142px_c;
            btnReturnToMenu.HoverSound = sp;
            btnLANOptions.DefaultImage = btn92px;
            btnLANOptions.HoveredImage = btn92px_c;
            btnLANOptions.HoverSound = sp;
            btnSend.HoverSound = sp;
            btnLANModdb.HoverSound = sp;
            btnLANFacebook.HoverSound = sp;
            btnLANYoutube.HoverSound = sp;
            btnLANTwitter.HoverSound = sp;
            btnLANGooglePlus.HoverSound = sp;
            btnLANForums.HoverSound = sp;

            int displayedItems = lbChatMessages.DisplayRectangle.Height / lbChatMessages.ItemHeight;

            sbChatMessages.ThumbBottomImage = SharedUILogic.LoadImage("sbThumbBottom.png");
            sbChatMessages.ThumbBottomSpanImage = SharedUILogic.LoadImage("sbThumbBottomSpan.png");
            sbChatMessages.ThumbMiddleImage = SharedUILogic.LoadImage("sbMiddle.png");
            sbChatMessages.ThumbTopImage = SharedUILogic.LoadImage("sbThumbTop.png");
            sbChatMessages.ThumbTopSpanImage = SharedUILogic.LoadImage("sbThumbTopSpan.png");
            sbChatMessages.UpArrowImage = SharedUILogic.LoadImage("sbUpArrow.png");
            sbChatMessages.DownArrowImage = SharedUILogic.LoadImage("sbDownArrow.png");
            sbChatMessages.BackgroundImage = SharedUILogic.LoadImage("sbBackground.png");
            sbChatMessages.Scroll += sbChat_Scroll;
            sbChatMessages.Maximum = lbChatMessages.Items.Count - Convert.ToInt32(displayedItems * 0.2);
            sbChatMessages.Minimum = 0;
            sbChatMessages.ChannelColor = cBackColor;
            sbChatMessages.LargeChange = 27;
            sbChatMessages.SmallChange = 9;
            sbChatMessages.Value = 0;

            lbChatMessages.MouseWheel += lbChatMessages_MouseWheel;

            int displayedPItems = lbPlayerList.DisplayRectangle.Height / lbPlayerList.ItemHeight;

            sbPlayerList.ThumbBottomImage = SharedUILogic.LoadImage("sbThumbBottom.png");
            sbPlayerList.ThumbBottomSpanImage = SharedUILogic.LoadImage("sbThumbBottomSpan.png");
            sbPlayerList.ThumbMiddleImage = SharedUILogic.LoadImage("sbMiddle.png");
            sbPlayerList.ThumbTopImage = SharedUILogic.LoadImage("sbThumbTop.png");
            sbPlayerList.ThumbTopSpanImage = SharedUILogic.LoadImage("sbThumbTopSpan.png");
            sbPlayerList.UpArrowImage = SharedUILogic.LoadImage("sbUpArrow.png");
            sbPlayerList.DownArrowImage = SharedUILogic.LoadImage("sbDownArrow.png");
            sbPlayerList.BackgroundImage = SharedUILogic.LoadImage("sbBackground.png");
            sbPlayerList.Scroll += sbPlayers_Scroll;
            sbPlayerList.Maximum = lbPlayerList.Items.Count - Convert.ToInt32(displayedPItems * 0.2);
            sbPlayerList.Minimum = 0;
            sbPlayerList.ChannelColor = cBackColor;
            sbPlayerList.LargeChange = 27;
            sbPlayerList.SmallChange = 9;
            sbPlayerList.Value = 0;

            lbPlayerList.MouseWheel += lbPlayerList_MouseWheel;

            lbGameList.SelectedIndex = -1;

            CnCNetData.OnGameStarted += new CnCNetData.GameStartedEventHandler(CnCNetData_OnGameStarted);
            CnCNetData.OnGameStopped += new CnCNetData.GameStoppedEventHandler(CnCNetData_OnGameStopped);
            CnCNetData.OnGameLobbyClosed += new CnCNetData.GameLobbyClosedEventHandler(CnCNetData_OnGameLobbyClosed);
            CnCNetData.OnGameLoadingLobbyClosed += CnCNetData_OnGameLoadingLobbyClosed;

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 2000;
            timer.Tick += new EventHandler(RefreshGameList);
            timer.Start();

            SharedUILogic.ParseClientThemeIni(this);

            UpdateTitle();

            try
            {
                Logger.Log("Creating LAN socket.");
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.EnableBroadcast = true;
                socket.Bind(new IPEndPoint(IPAddress.Any, 1233));
                endPoint = new IPEndPoint(IPAddress.Broadcast, 1233);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Creating socket failed! Message: " + ex.Message + Environment.NewLine + Environment.NewLine +
                    "Please check your firewall settings.", "LAN Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            Thread thread = new Thread(new ThreadStart(Listen));
            thread.Start();

            System.Windows.Forms.Timer broadcastTimer = new System.Windows.Forms.Timer();
            broadcastTimer.Interval = 5000;
            broadcastTimer.Tick += broadcastTimer_Tick;
            broadcastTimer.Start();

            SendMessage("ALIVE " + ProgramConstants.PLAYERNAME + "~" + myGame);
        }

        void broadcastTimer_Tick(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer t = (System.Windows.Forms.Timer)sender;

            SendMessage("ALIVE " + ProgramConstants.PLAYERNAME + "~" + myGame);

            t.Enabled = true;
        }

        public void SendMessage(string message)
        {
            byte[] buffer;

            buffer = Encoding.GetEncoding(1252).GetBytes(message);

            socket.SendTo(buffer, endPoint);
        }

        private void Listen()
        {
            while (true)
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 1233);
                byte[] buffer = new byte[4096];
                int receivedBytes = 0;
                try
                {
                    receivedBytes = socket.ReceiveFrom(buffer, ref ep);
                }
                catch (Exception ex)
                {
                    Logger.Log("LANLobby: Receiving from socket failed! " + ex.Message);
                }

                IPEndPoint iep = (IPEndPoint)ep;

                string data = Encoding.GetEncoding(1252).GetString(buffer, 0, receivedBytes);

                if (data.StartsWith("ALIVE "))
                {
                    string[] info = data.Substring(6).Split('~');
                    if (info.Length != 2)
                        continue;

                    string playerName = info[0];
                    string game = info[1];

                    int index = Players.FindIndex(p => p.EndPoint.Equals(iep));

                    if (index > -1)
                    {
                        Players[index].Name = playerName;
                        Players[index].GameIdentifier = game;
                    }
                    else
                    {
                        LANPlayer p = new LANPlayer();
                        p.Name = playerName;
                        p.GameIdentifier = game;
                        p.EndPoint = iep;

                        AddPlayer(p);
                    }
                }
                else if (data.StartsWith("CHAT "))
                {
                    int index = Players.FindIndex(p => p.EndPoint.Equals(iep));

                    if (index == -1)
                        continue;
                    
                    LANPlayer plr = Players[index];
                    string[] parts = data.Substring(5).Split(new char[] {'~'}, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                        continue;

                    AddChatMessage(plr.Name, parts[0], Convert.ToInt32(parts[1]));
                }
                else if (data == "QUIT")
                {
                    int index = Players.FindIndex(p => p.EndPoint.Equals(iep));

                    if (index == -1)
                        continue;

                    RemovePlayer(Players[index]);
                }
                else if (data.StartsWith("GAME "))
                {
                    string[] parts = data.Substring(5).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 10)
                    {
                        Logger.Log("Ignoring GAME message because of the amount of parameters is incorrect.");
                        continue;
                    }

                    if (parts[0] != ProgramConstants.LAN_PROTOCOL_REVISION)
                        continue;

                    string gameVersion = parts[1];
                    int maxPlayers = 0;
                    bool success = Int32.TryParse(parts[2], out maxPlayers);
                    string gameRoomName = parts[3];
                    if (parts[4].Length != 3)
                        continue;

                    int lockedInt = 1;
                    success = Int32.TryParse(parts[4].Substring(0, 1), out lockedInt);
                    bool locked = Convert.ToBoolean(lockedInt);

                    int closedInt = 1;
                    success = Int32.TryParse(parts[4].Substring(1, 1), out closedInt);
                    bool closed = Convert.ToBoolean(closedInt);

                    int loadedGameInt = 0;
                    success = Int32.TryParse(parts[4].Substring(2, 1), out loadedGameInt);
                    bool isLoadedGame = Convert.ToBoolean(loadedGameInt);

                    string[] players = parts[5].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    string mapName = parts[6];
                    string gameMode = parts[7];
                    string loadedGameId = parts[8];
                    string gameId = parts[9];

                    LANGame lg = new LANGame()
                    {
                        Version = gameVersion,
                        MaxPlayers = maxPlayers,
                        RoomName = gameRoomName,
                        Started = locked,
                        Closed = closed,
                        IsLoadedGame = isLoadedGame,
                        MapName = mapName,
                        GameMode = gameMode,
                        MatchID = loadedGameId,
                        GameIdentifier = gameId
                    };

                    foreach (string plr in players)
                        lg.Players.Add(plr);

                    lg.LastRefreshTime = DateTime.Now;
                    lg.EndPoint = iep;

                    int index = Games.FindIndex(g => g.EndPoint.Equals(iep));

                    lock (gameLocker)
                    {
                        if (index > -1)
                        {
                            Games[index] = lg;
                        }
                        else if (!closed)
                            Games.Add(lg);
                    }

                    RefreshGameList(null, EventArgs.Empty);
                }
            }
        }

        void AddPlayer(LANPlayer p)
        {
            if (lbPlayerList.InvokeRequired)
            {
                PlayerDelegate d = new PlayerDelegate(AddPlayer);
                BeginInvoke(d, p);
                return;
            }

            Players.Add(p);
            lbPlayerList.Items.Add(p.Name);

            UpdateTitle();
        }

        void RemovePlayer(LANPlayer p)
        {
            if (lbPlayerList.InvokeRequired)
            {
                PlayerDelegate d = new PlayerDelegate(RemovePlayer);
                BeginInvoke(d, p);
                return;
            }

            Players.Remove(p);
            lbPlayerList.Items.Remove(p.Name);

            UpdateTitle();
        }

        void AddChatMessage(string sender, string message, int color)
        {
            if (lbChatMessages.InvokeRequired)
            {
                ChatDelegate d = new ChatDelegate(AddChatMessage);
                BeginInvoke(d, sender, message, color);
                return;
            }

            MessageInfos.Add(new MessageInfo(ChatColors[color], sender + ": " + message));
            lbChatMessages.Items.Add("[" + DateTime.Now.ToShortTimeString() + "] " + sender + ": " + message);
        }

        private void UpdateTitle()
        {
            this.Text = string.Format("[{0}] {1} LAN Lobby: {2} ({3} / {4})",
                Players.Count, myGame,
                ProgramConstants.PLAYERNAME,
                ProgramConstants.GAME_VERSION, Application.ProductVersion);
        }

        /// <summary>
        /// Executed when the user scrolls the player list with the mouse wheel.
        /// </summary>
        private void lbPlayerList_MouseWheel(object sender, MouseEventArgs e)
        {
            sbPlayerList.Value += e.Delta / -40;
            sbPlayers_Scroll(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Executed when the user scrolls the chat with the mouse wheel.
        /// </summary>
        private void lbChatMessages_MouseWheel(object sender, MouseEventArgs e)
        {
            sbChatMessages.Value += e.Delta / -40;
            sbChat_Scroll(sender, EventArgs.Empty);
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Exception ex = e.Exception;

            DisplayUnhandledExceptionMessage(sender, ex);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            DisplayUnhandledExceptionMessage(sender, ex);
        }

        /// <summary>
        /// Handles otherwise uncaught exceptions occuring in the UI thread.
        /// </summary>
        private static void DisplayUnhandledExceptionMessage(object sender, Exception ex)
        {
            Logger.Log("Unhandled exception!!!");
            Logger.Log(ex.Message);
            Logger.Log(ex.Source);
            Logger.Log(ex.StackTrace);
            MessageBox.Show("The CnCNet Client has crashed. Error Message: " + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                "See cncnetclient.log for further info. If you can reproduce this crash, " + Environment.NewLine +
                "please report about it to your mod's authors or directly to Rampastring " + Environment.NewLine +
                "(the creator of this client) at " + Environment.NewLine +
                "http://www.moddb.com/members/rampastring", "KABOOOOOOOM22");
            Environment.Exit(0);
        }

        /// <summary>
        /// Executed when the Game Lobby window is closed.
        /// </summary>
        private void CnCNetData_OnGameLobbyClosed()
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = oldWindowState;

            gameLobbyForm.Dispose();
        }

        /// <summary>
        /// Called when the Game Loading lobby is closed.
        /// </summary>
        void CnCNetData_OnGameLoadingLobbyClosed()
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = oldWindowState;

            gameLoadingLobbyForm.Dispose();
        }

        /// <summary>
        /// Executed when a game is finished.
        /// </summary>
        private void CnCNetData_OnGameStopped()
        {
            if (DomainController.Instance().GetWindowMinimizingStatus())
            {
                if (CnCNetData.IsGameLobbyOpen)
                    gameLobbyForm.Show();
                else
                    gameLoadingLobbyForm.Show();
            }

            gameInProgress = false;
        }

        /// <summary>
        /// Executed when a game is started.
        /// </summary>
        private void CnCNetData_OnGameStarted()
        {
            if (DomainController.Instance().GetWindowMinimizingStatus())
            {
                if (CnCNetData.IsGameLobbyOpen)
                    gameLobbyForm.Hide();
                else
                    gameLoadingLobbyForm.Hide();
            }

            gameInProgress = true;
        }

        /// <summary>
        /// Refreshes the internal game list, applying statuses to the displayed names
        /// of all games and removing games that haven't been updated for too long by their authors.
        /// </summary>
        private void RefreshGameList(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                EventDelegate d = new EventDelegate(RefreshGameList);
                BeginInvoke(d, sender, e);
                return;
            }

            lock (gameLocker)
            {
                int sIndex = lbGameList.SelectedIndex;
                int topIndex = lbGameList.TopIndex;
                lbGameList.Items.Clear();
                GameColors.Clear();

                for (int gId = 0; gId < Games.Count; gId++)
                {
                    if (DateTime.Now - Games[gId].LastRefreshTime > TimeSpan.FromSeconds(10.0))
                    {
                        Games.RemoveAt(gId);
                        if (sIndex == gId)
                            sIndex = -1;
                        else if (sIndex > gId)
                            sIndex--;
                    }
                }

                Games = Games.OrderBy(g => g.Version != ProgramConstants.GAME_VERSION).ToList();
                Games = Games.OrderBy(g => g.GameIdentifier != myGame).ToList();
                Games = Games.OrderBy(g => g.Started).ToList();

                foreach (LANGame game in Games)
                {
                    Color foreColor = lbGameList.ForeColor;

                    string item = game.RoomName;

                    if (game.Started)
                    {
                        foreColor = cLockedGameColor;
                    }
                    else if (game.Version != ProgramConstants.GAME_VERSION && game.GameIdentifier == myGame.ToLower())
                    {
                        foreColor = cLockedGameColor;
                    }
                    else if (game.IsLoadedGame && !game.Players.Contains(ProgramConstants.PLAYERNAME))
                    {
                        foreColor = cLockedGameColor;
                    }

                    if (game.IsLoadedGame)
                        item = item + " (Loaded Game)";

                    GameColors.Add(foreColor);
                    lbGameList.Items.Add(item);
                }

                if (sIndex >= lbGameList.Items.Count)
                    lbGameList.SelectedIndex = -1;
                else
                    lbGameList.SelectedIndex = sIndex;
                lbGameList_SelectedIndexChanged(sender, e);
                lbGameList.TopIndex = topIndex;
            }
        }

        /// <summary>
        /// Refreshes the list of chat messages displayed in the UI.
        /// </summary>
        private void UpdateMessages()
        {
            lbChatMessages.Items.Clear();

            foreach (MessageInfo msgInfo in MessageInfos)
            {
                lbChatMessages.Items.Add("[" + msgInfo.Time.ToShortTimeString() + "] " + msgInfo.Message);
            }

            ScrollChatListbox();
        }

        /// <summary>
        /// Executed when the user clicks the "Return to Main Menu" button.
        /// </summary>
        private void btnReturnToMenu_Click(object sender, EventArgs e)
        {
            if (CnCNetData.IsGameLobbyOpen)
            {
                DialogResult dr = MsgBoxForm.Show("You are currently in a game. Are you sure you wish to quit the LAN lobby?",
                    "Are you sure?", MessageBoxButtons.YesNo);

                if (dr != System.Windows.Forms.DialogResult.OK)
                    return;
            }

            SendMessage("QUIT");

            if (btnNewGame.Enabled)
                SaveSettings();

            Application.DoEvents();
            Environment.Exit(0);
        }

        /// <summary>
        /// Saves user settings.
        /// </summary>
        private void SaveSettings()
        {
            Logger.Log("Saving settings.");

            DomainController.Instance().SaveLobbyPosition(this.Location.X, this.Location.Y);

            ProgramConstants.PLAYERNAME = DomainController.Instance().GetMpHandle();

            DomainController.Instance().SaveCnCNetColorSetting(cmbLANMessageColor.SelectedIndex);
        }

        /// <summary>
        /// Used for automatically scaling UI components as the window's size is changed.
        /// TODO: Is this actually necessary with anchors properly set?
        /// </summary>
        private void NCnCNetLobby_SizeChanged(object sender, EventArgs e)
        {
            lbChatMessages.Refresh();
        }

        /// <summary>
        /// Used for measuring the size of drawn chat messages.
        /// </summary>
        private void lbChatMessages_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)e.Graphics.MeasureString(lbChatMessages.Items[e.Index].ToString(),
                lbChatMessages.Font, lbChatMessages.Width - sbChatMessages.Width).Height;
            e.ItemWidth = lbChatMessages.Width - sbChatMessages.Width;
        }

        /// <summary>
        /// Used for manually drawing chat messages in the chat message list box.
        /// </summary>
        private void lbChatMessages_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1 && e.Index < lbChatMessages.Items.Count)
            {
                Color foreColor;

                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    e = new DrawItemEventArgs(e.Graphics,
                                              e.Font,
                                              e.Bounds,
                                              e.Index,
                                              e.State ^ DrawItemState.Selected,
                                              e.ForeColor,
                                              cListBoxFocusColor);

                e.DrawBackground();
                e.DrawFocusRectangle();

                if (MessageInfos.Count <= e.Index)
                    foreColor = Color.White;
                else
                    foreColor = MessageInfos[e.Index].Color;
                e.Graphics.DrawString(lbChatMessages.Items[e.Index].ToString(), e.Font, new SolidBrush(foreColor), e.Bounds);
            }
        }

        /// <summary>
        /// Used for manually drawing items of the "Your Chat Color" combo box.
        /// </summary>
        private void cmbMessageColor_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            e.DrawFocusRectangle();
            if (e.Index > -1 && e.Index < cmbLANMessageColor.Items.Count)
            {
                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    e = new DrawItemEventArgs(e.Graphics,
                                              e.Font,
                                              e.Bounds,
                                              e.Index,
                                              e.State ^ DrawItemState.Selected,
                                              e.ForeColor,
                                              cListBoxFocusColor);

                e.DrawBackground();
                e.DrawFocusRectangle();

                e.Graphics.DrawString(cmbLANMessageColor.Items[e.Index].ToString(), e.Font, new SolidBrush(ChatColors[e.Index + 2]), e.Bounds);
            }
        }

        /// <summary>
        /// Used for measuring the size of items in the player list box.
        /// </summary>
        private void lbPlayerList_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)e.Graphics.MeasureString(lbPlayerList.Items[e.Index].ToString(),
                lbPlayerList.Font, lbPlayerList.Width).Height;
        }
        
        /// <summary>
        /// Used for manually drawing items of the player list box.
        /// </summary>
        private void lbPlayerList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1 && e.Index < lbPlayerList.Items.Count)
            {
                Color foreColor = cPlayerNameColor;

                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    e = new DrawItemEventArgs(e.Graphics,
                                              e.Font,
                                              e.Bounds,
                                              e.Index,
                                              e.State ^ DrawItemState.Selected,
                                              e.ForeColor,
                                              cListBoxFocusColor);

                e.DrawBackground();
                e.DrawFocusRectangle();

                int GAME_ICON_SIZE = gameIcons[0].Size.Width;

                Rectangle gameIconRect = new Rectangle(e.Bounds.X, e.Bounds.Y, GAME_ICON_SIZE, GAME_ICON_SIZE);

                int gameIconIndex = GameCollection.Instance.GetGameIndexFromInternalName(Players[e.Index].GameIdentifier);

                if (gameIconIndex == -1)
                    gameIconIndex = 0;

                string nameToDraw = lbPlayerList.Items[e.Index].ToString();

                e.Graphics.DrawImage(gameIcons[gameIconIndex], gameIconRect);
                e.Graphics.DrawString(nameToDraw, e.Font, new SolidBrush(foreColor), 
                    new Rectangle(e.Bounds.X + gameIconRect.Width + 1,
                        e.Bounds.Y, e.Bounds.Width - gameIconRect.Width - 1, e.Bounds.Height));
            }
        }

        /// <summary>
        /// Executed when the form is closed.
        /// </summary>
        private void NCnCNetLobby_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// Sends a chat message to the IRC server.
        /// </summary>
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(tbChatInput.Text))
                return;

            lastInputCommand = tbChatInput.Text;

            string channel = GameCollection.Instance.GetGameChatChannelNameFromIndex(currentChannelId);
            int colorId = cmbLANMessageColor.SelectedIndex + 2;

            SendMessage("CHAT " + tbChatInput.Text.Replace('~', '-') + "~" + colorId);
            tbChatInput.Text = String.Empty;
        }

        /// <summary>
        /// Lets the user host a new game.
        /// </summary>
        private void btnNewGame_Click(object sender, EventArgs e)
        {
            if (CnCNetData.IsGameLobbyOpen)
            {
                // Prevent the user from hosting a game if they already are in a game
                // and flash the game lobby window
                WindowFlasher.FlashWindowEx(gameLobbyForm);
                AddNotice("You already are in a game!");
                return;
            }

            if (CnCNetData.IsGameLoadingLobbyOpen)
            {
                WindowFlasher.FlashWindowEx(gameLoadingLobbyForm);
                AddNotice("You already are in a game!");
                return;
            }

            //CreateGameForm cgf = new CreateGameForm(false);
            //DialogResult dr = cgf.ShowDialog();

            //if (dr != System.Windows.Forms.DialogResult.OK)
            //{
            //    return;
            //}

            //for (int gId = 0; gId < CnCNetData.Games.Count; gId++)
            //{
            //    if (CnCNetData.Games[gId].RoomName == cgf.rtnGameRoomName)
            //    {
            //        MessageBox.Show("A game room with the specified name already exists.");
            //        cgf.Dispose();
            //        return;
            //    }
            //}

            //Logger.Log("Creating a game named " + cgf.rtnGameRoomName);

            //if (!cgf.rtnLoadSavedGame)
            //{
            //    gameLobbyForm = new LANGameLobby(this, true, "127.0.0.1", cgf.rtnMaxPlayers,
            //        ProgramConstants.PLAYERNAME, cgf.rtnGameRoomName, ChatColors, cDefaultChatColor, cmbLANMessageColor.SelectedIndex + 2);
            //    CnCNetData.IsGameLobbyOpen = true;
            //    gameLobbyForm.Show();
            //}
            //else
            //{
            //    gameLoadingLobbyForm = new LANLoadingLobby(this, true, ProgramConstants.PLAYERNAME, IPAddress.Loopback,
            //        cgf.rtnGameRoomName, ChatColors, cDefaultChatColor, cmbLANMessageColor.SelectedIndex + 2);
            //    CnCNetData.IsGameLoadingLobbyOpen = true;
            //    gameLoadingLobbyForm.Show();
            //}

            //oldWindowState = this.WindowState;
            //this.WindowState = FormWindowState.Minimized;
            //cgf.Dispose();
        }

        public void AddNotice(string notice)
        {
            if (lbChatMessages.InvokeRequired)
            {
                StringDelegate d = new StringDelegate(AddNotice);
                BeginInvoke(d, notice);
                return;
            }

            MessageInfos.Add(new MessageInfo(Color.White, notice));
            lbChatMessages.Items.Add(notice);
        }

        /// <summary>
        /// Updates game information displayed in the UI.
        /// Called whenever the selected index of lbGameList is changed.
        /// </summary>
        private void lbGameList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbGameList.SelectedIndex == -1)
            {
                // No game selected - clear info

                lblMapName.Visible = false;
                lblGameMode.Visible = false;
                lblVersion.Visible = false;
                lblHost.Visible = false;
                lblPlayers.Visible = false;
                lblPlayer1.Visible = false;
                lblPlayer2.Visible = false;
                lblPlayer3.Visible = false;
                lblPlayer4.Visible = false;
                lblPlayer5.Visible = false;
                lblPlayer6.Visible = false;
                lblPlayer7.Visible = false;
                lblPlayer8.Visible = false;
            }
            else
            {
                // Game selected - fill info

                lblMapName.Visible = true;
                lblGameMode.Visible =true;
                lblVersion.Visible = true;
                lblHost.Visible = true;
                lblPlayers.Visible = true;
                lblPlayer1.Visible = true;
                lblPlayer2.Visible = true;
                lblPlayer3.Visible = true;
                lblPlayer4.Visible = true;
                lblPlayer5.Visible = true;
                lblPlayer6.Visible = true;
                lblPlayer7.Visible = true;
                lblPlayer8.Visible = true;

                LANGame game = Games[lbGameList.SelectedIndex];

                lblGameMode.Text = "Game Mode: " + game.GameMode;
                lblMapName.Text = "Map: " + game.MapName;

                if (game.GameIdentifier == myGame)
                {
                    if (game.Version == ProgramConstants.GAME_VERSION)
                        lblVersion.Text = "Game Version: " + game.Version + " (compatible)";
                    else
                        lblVersion.Text = "Game Version: " + game.Version + " (incompatible)";
                }
                else
                    lblVersion.Text = "Game Version: " + game.Version;


                lblHost.Text = "Host: " + game.Host;

                lblPlayers.Text = "Players (" + game.Players.Count + " / " + game.MaxPlayers + ") :";
                for (int pId = 0; pId < game.Players.Count; pId++)
                {
                    Label playerLabel = getPlayerLabelFromId(pId);
                    playerLabel.Text = game.Players[pId];
                }
                
                for (int pId = game.Players.Count; pId < 8; pId++)
                {
                    Label playerLabel = getPlayerLabelFromId(pId);
                    playerLabel.Text = String.Empty;
                }
            }
        }

        private Label getPlayerLabelFromId(int id)
        {
            switch (id)
            {
                case 0:
                    return lblPlayer1;
                case 1:
                    return lblPlayer2;
                case 2:
                    return lblPlayer3;
                case 3:
                    return lblPlayer4;
                case 4:
                    return lblPlayer5;
                case 5:
                    return lblPlayer6;
                case 6:
                    return lblPlayer7;
                case 7:
                    return lblPlayer8;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Executed when the user attempts to join a game.
        /// </summary>
        private void btnJoinGame_Click(object sender, EventArgs e)
        {
            if (lbGameList.SelectedIndex == -1)
            {
                AddNotice("No game selected!");
                return;
            }

            if (CnCNetData.IsGameLobbyOpen)
            {
                WindowFlasher.FlashWindowEx(gameLobbyForm);
                AddNotice("You cannot join a game while already being in a game!");
                return;
            }

            if (CnCNetData.IsGameLoadingLobbyOpen)
            {
                WindowFlasher.FlashWindowEx(gameLoadingLobbyForm);
                AddNotice("You cannot join a game while already being in a game!");
                return;
            }

            LANGame lg = Games[lbGameList.SelectedIndex];

            if (lg.Started)
            {
                AddNotice("The selected game is locked!");
                return;
            }

            if (lg.GameIdentifier.ToLower() != myGame.ToLower())
            {
                // If the game we're trying to join is for a different game, let's
                // check if the other game is installed and if yes, then launch that
                // game's client; otherwise prompt the user for installation
                // DTA and TI multiplayer affiliation \ o /

                string installPath = getInstallPath(lg.GameIdentifier);

                if (!String.IsNullOrEmpty(installPath))
                {
                    ClientSwitchConfirmation(lg.GameIdentifier, installPath);
                    return;
                }
                else
                {
                    InstallConfirmation(lg.GameIdentifier);
                    return;
                }
            }

            if (lg.Version != ProgramConstants.GAME_VERSION)
            {
                DialogResult dr = MsgBoxForm.Show("The selected game is incompatible with your game version." + Environment.NewLine +
                    "This could result in you being unable to play in the game." + Environment.NewLine + Environment.NewLine +
                    "Are you sure you wish to join the game?", "Incompatible Game", MessageBoxButtons.YesNo);

                if (dr != System.Windows.Forms.DialogResult.OK)
                    return;
            }

            if (lg.EndPoint.Address.ToString() == "127.0.0.1")
            {
                AddNotice("You cannot join your own game.");
                return;
            }

            if (lg.IsLoadedGame)
            {
                if (!lg.Players.Contains(ProgramConstants.PLAYERNAME))
                {
                    AddNotice("You are not eligible for joining the selected game.");
                    return;
                }

                IniFile iniFile = new IniFile(ProgramConstants.GamePath + "Saved Games\\spawnSG.ini");

                if (iniFile.GetStringValue("Settings", "GameID", String.Empty) != lg.MatchID)
                {
                    AddNotice("You are not eligible for joining the selected game.");
                    return;
                }

                gameLoadingLobbyForm = new LANLoadingLobby(this, false, lg.Host, lg.EndPoint.Address,
                    lg.RoomName, ChatColors, cDefaultChatColor, cmbLANMessageColor.SelectedIndex + 2);

                CnCNetData.IsGameLoadingLobbyOpen = true;

                gameLoadingLobbyForm.Show();
                return;
            }

            if (lg.Players.Contains(ProgramConstants.PLAYERNAME))
            {
                AddNotice("Your name is already taken in the game.");
                return;
            }

            gameLobbyForm = new LANGameLobby(this, false, lg.EndPoint.Address.ToString(), lg.MaxPlayers,
                lg.Host, lg.RoomName, ChatColors, cDefaultChatColor, cmbLANMessageColor.SelectedIndex + 2);
            CnCNetData.IsGameLobbyOpen = true;
            gameLobbyForm.Show();
        }

        /// <summary>
        /// Gets the installation path for a supported game.
        /// Returns an empty string if the specified game isn't installed.
        /// </summary>
        /// <param name="gameId">The ID of the game.</param>
        /// <returns>The installation path of the game. An empty string if the game isn't found.</returns>
        private string getInstallPath(string gameId)
        {
            Logger.Log(string.Format("Detecting whether {0} is installed.", gameId));

            Microsoft.Win32.RegistryKey key;
            switch (gameId.ToUpper())
            {
                case "DTA":
                    key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\TheDawnOfTheTiberiumAge");
                    break;
                case "TI":
                    key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\TwistedInsurrection");
                    break;
                case "TO":
                    key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\TiberianOdyssey");
                    break;
                case "TS":
                    key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\TiberianSun");
                    break;
                case "YR":
                    key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Westwood\\Wow6432Node\\Yuri's Revenge");
                    if (key == null)
                        key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Westwood\\Yuri's Revenge");
                    break;
                default:
                    return String.Empty;
            }

            if (key == null)
                return String.Empty;
            
            object value = key.GetValue("InstallPath");
            if (value == null)
                return String.Empty;

            string valueString = (string)value;

            if (gameId.ToUpper() == "YR")
                valueString = Path.GetDirectoryName(valueString) + "\\";

            if (System.IO.Directory.Exists(valueString))
                return valueString;

            return String.Empty;
        }

        /// <summary>
        /// Asks the user if they want to switch the client for a different game.
        /// If the confirmation is positive, then starts the other game's client and exits.
        /// </summary>
        /// <param name="gameId">The internal ID (short name) of the supported game.</param>
        /// <param name="installPath">The installation directory of the selected game.</param>
        private void ClientSwitchConfirmation(string gameId, string installPath)
        {
            string fullName = String.Empty;
            string executableName = String.Empty;

            switch (gameId.ToUpper())
            {
                case "DTA":
                    fullName = "The Dawn of the Tiberium Age";
                    executableName = "DTA.exe";
                    break;
                case "TI":
                    fullName = "Twisted Insurrection";
                    executableName = "TI_Launcher.exe";
                    break;
                case "TO":
                    fullName = "Tiberian Odyssey";
                    executableName = "TiberianOdyssey.exe";
                    break;
                case "TS":
                    fullName = "Tiberian Sun";
                    executableName = "TiberianSun.exe";
                    break;
                case "YR":
                    fullName = "Yuri's Revenge";
                    executableName = "CnCNetClientYR.exe";
                    break;
                default:
                    fullName = "Unknown Game " + gameId;
                    break;
            }

            DialogResult dr = MessageBox.Show(string.Format(
                "The selected game is for {0}. Do you wish to switch to the {0} client in order to join the game?", fullName),
                string.Format("{0} game", fullName), MessageBoxButtons.YesNo);

            if (dr == DialogResult.No)
            {
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(installPath + executableName);
            startInfo.Arguments = "-RUNLAN -NOUACPOPUP";
            startInfo.WorkingDirectory = installPath;
            Process process = new Process();
            process.StartInfo = startInfo;

            Logger.Log(string.Format("Starting the {0} client and exiting.", fullName));

            SaveSettings();

            process.Start();

            CnCNetData.ConnectionBridge.SendMessage("QUIT");

            // Set exit code which the main client can read and quit accordingly
            Environment.Exit(1337);
        }

        /// <summary>
        /// Asks the user if they want to install a different supported game than what is
        /// currently being run.
        /// </summary>
        /// <param name="gameId">The internal ID (short name) of the supported game.</param>
        private void InstallConfirmation(string gameId)
        {
            string fullName = String.Empty;
            string pageUrl = String.Empty;

            switch (gameId.ToUpper())
            {
                case "DTA":
                    fullName = "The Dawn of the Tiberium Age";
                    pageUrl = "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age";
                    break;
                case "TI":
                    fullName = "Twisted Insurrection";
                    pageUrl = "http://www.moddb.com/mods/twisted-insurrection";
                    break;
                case "TO":
                    fullName = "Tiberian Odyssey";
                    pageUrl = "http://www.moddb.com/mods/tiberian-odyssey";
                    break;
                case "TS":
                    fullName = "Tiberian Sun";
                    pageUrl = "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age";
                    break;
                case "YR":
                    Logger.Log("InstallConfirmation: YR not installed!");
                    MessageBox.Show("The selected game is for C&C Yuri's Revenge. You do not have Yuri's Revenge installed " +
                        "and so you are not able to join the selected game.");
                    return;
                default:
                    fullName = "Unknown Game " + gameId;
                    Logger.Log(string.Format("InstallConfirmation: Unknown gameId {0}", gameId));
                    return;
            }

            Logger.Log(string.Format("Offering the installation of {0} to the user.", gameId));

            DialogResult dr = MessageBox.Show(string.Format("The selected game room is for {0}." +
                Environment.NewLine + Environment.NewLine +
                "Would you like to visit the home page of {0} where you can download and install it?", fullName),
                string.Format("{0} game", fullName), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr == DialogResult.No)
                return;

            Logger.Log(string.Format("Opening the home page of {0}.", gameId));

            ParameterizedThreadStart ts = new ParameterizedThreadStart(OpenPage);
            Thread thread = new Thread(ts);
            thread.Start(pageUrl);
        }

        /// <summary>
        /// Opens a URL in the default web browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        private void OpenPage(object url)
        {
            string sUrl = (string)url;

            Process.Start(sUrl);
        }

        /// <summary>
        /// Executed when the user changes their chat color via cmbMessageColor.
        /// Used to transfer the new color to the possibly running game lobby.
        /// </summary>
        private void cmbMessageColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            DoColorChanged(cmbLANMessageColor.SelectedIndex + 2);
        }

        /// <summary>
        /// Makes it possible for an user to join a game by double-clicking on it.
        /// </summary>
        private void lbGameList_DoubleClick(object sender, EventArgs e)
        {
            btnJoinGame.PerformClick();
        }

        /// <summary>
        /// Enables Ctrl + C for copying entries in the chat message list box.
        /// </summary>
        private void lbChatMessages_KeyDown(object sender, KeyEventArgs e)
        {
            if (lbChatMessages.SelectedIndex > -1)
            {
                if (e.KeyCode == Keys.C && e.Control)
                    Clipboard.SetText(lbChatMessages.SelectedItem.ToString());
            }
        }

        private void sbChat_Scroll(object sender, EventArgs e)
        {
            lbChatMessages.TopIndex = sbChatMessages.Value;
        }

        private void sbPlayers_Scroll(object sender, EventArgs e)
        {
            lbPlayerList.TopIndex = sbPlayerList.Value;
        }

        /// <summary>
        /// Used for automatically scrolling the chat listbox when entries are added/removed.
        /// </summary>
        private void ScrollChatListbox()
        {
            int displayedItems = lbChatMessages.DisplayRectangle.Height / lbChatMessages.ItemHeight;
            sbChatMessages.Maximum = lbChatMessages.Items.Count - Convert.ToInt32(displayedItems * 0.2);
            if (sbChatMessages.Maximum < 0)
                sbChatMessages.Maximum = 1;
            sbChatMessages.Value++;
            //sbChat.Value = sbChat.Maximum;
            //lbChatMessages.SelectedIndex = lbChatMessages.Items.Count - 1;
            //lbChatMessages.SelectedIndex = -1;
            lbChatMessages.TopIndex++;
        }

        /// <summary>
        /// Used for automatically scrolling the players list box when entries are added/removed.
        /// </summary>
        private void ScrollPlayersListbox()
        {
            int displayedItems = lbPlayerList.DisplayRectangle.Height / lbPlayerList.ItemHeight;
            sbPlayerList.Maximum = lbPlayerList.Items.Count - Convert.ToInt32(displayedItems * 0.2);
            if (sbPlayerList.Maximum < 0)
                sbPlayerList.Maximum = 1;
            sbPlayerList.Value = 0;
            if (lbPlayerList.Items.Count > 0)
                lbPlayerList.SelectedIndex = 0;
            lbPlayerList.SelectedIndex = -1;
        }

        /// <summary>
        /// Draws entries in the list of games.
        /// </summary>
        private void lbGameList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1 && e.Index < lbGameList.Items.Count)
            {
                Color foreColor = GameColors[e.Index];

                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    e = new DrawItemEventArgs(e.Graphics,
                                              e.Font,
                                              e.Bounds,
                                              e.Index,
                                              e.State ^ DrawItemState.Selected,
                                              e.ForeColor,
                                              cListBoxFocusColor);

                e.DrawBackground();
                e.DrawFocusRectangle();

                LANGame game = Games[e.Index];

                int GAME_ICON_SIZE = gameIcons[0].Size.Width;

                Rectangle gameIconRect = new Rectangle(e.Bounds.X, e.Bounds.Y, GAME_ICON_SIZE, GAME_ICON_SIZE);

                int gameIndex = GameCollection.Instance.GetGameIndexFromInternalName(game.GameIdentifier);

                e.Graphics.DrawImage(gameIcons[gameIndex], gameIconRect);

                int multiplier = 1;

                if (game.Started || (game.IsLoadedGame && !game.Players.Contains(ProgramConstants.PLAYERNAME)))
                {
                    // Draw game locked icon
                    e.Graphics.DrawImage(lockedGameIcon, new Rectangle(e.Bounds.X + GAME_ICON_SIZE + 1, e.Bounds.Y,
                        GAME_ICON_SIZE, GAME_ICON_SIZE));
                    multiplier++;
                }

                if (game.GameIdentifier == myGame && game.Version != ProgramConstants.GAME_VERSION)
                {
                    // Draw game incompatible icon
                    e.Graphics.DrawImage(incompatibleGameIcon, new Rectangle(e.Bounds.X + (GAME_ICON_SIZE * multiplier) + 1, e.Bounds.Y,
                        GAME_ICON_SIZE, GAME_ICON_SIZE));
                    multiplier++;
                }

                Rectangle rectangle = new Rectangle(e.Bounds.X + GAME_ICON_SIZE * multiplier,
                    e.Bounds.Y, e.Bounds.Width - GAME_ICON_SIZE * multiplier, e.Bounds.Height + 2);

                string text = lbGameList.Items[e.Index].ToString();

                // Draw game name
                e.Graphics.DrawString(text, e.Font, new SolidBrush(foreColor), rectangle);

                SizeF size = e.Graphics.MeasureString(text, e.Font);

                if (game.Passworded)
                {
                    // Draw game passworded icon
                    int x = e.Bounds.X + (GAME_ICON_SIZE * multiplier) + 1 + Convert.ToInt32(size.Width);

                    if (x < lbGameList.Width - GAME_ICON_SIZE)
                    {
                        e.Graphics.DrawImage(passwordedGameIcon,
                            new Rectangle(e.Bounds.X + (GAME_ICON_SIZE * multiplier) + Convert.ToInt32(size.Width),
                                e.Bounds.Y,
                            GAME_ICON_SIZE, GAME_ICON_SIZE));
                    }
                    else
                    {
                        // If the password icon would go off the listbox's bounds, draw it at the end of the game name
                        e.Graphics.DrawImage(passwordedGameIcon,
                            new Rectangle(e.Bounds.X + lbGameList.Width - GAME_ICON_SIZE,
                            e.Bounds.Y,
                            GAME_ICON_SIZE, GAME_ICON_SIZE));
                    }
                }
            }
        }

        private void lbGameList_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)e.Graphics.MeasureString("@g",
                lbGameList.Font, lbGameList.Width).Height;
            e.ItemWidth = lbGameList.Width;
        }

        /// <summary>
        /// Opens a URL from the chat message list box if the double-clicked item contains a URL.
        /// </summary>
        private void lbChatMessages_DoubleClick(object sender, EventArgs e)
        {
            if (lbChatMessages.SelectedIndex < 0)
                return;

            string selectedItem = lbChatMessages.SelectedItem.ToString();

            int index = selectedItem.IndexOf("http://");
            if (index == -1)
                index = selectedItem.IndexOf("ftp://");

            if (index == -1)
                return; // No link found

            string link = selectedItem.Substring(index);
            link = link.Split(' ')[0]; // Nuke any words coming after the link

            Process.Start(link);
        }

        /// <summary>
        /// Used for changing the mouse cursor image when it enters a URL
        /// in the chat message list box.
        /// </summary>
        private void lbChatMessages_MouseMove(object sender, MouseEventArgs e)
        {
            // Determine hovered item index
            Point mousePosition = lbChatMessages.PointToClient(ListBox.MousePosition);
            int hoveredIndex = lbChatMessages.IndexFromPoint(mousePosition);

            if (hoveredIndex == -1 || hoveredIndex >= lbChatMessages.Items.Count)
            {
                lbChatMessages.Cursor = Cursors.Default;
                return;
            }

            string item = lbChatMessages.Items[hoveredIndex].ToString();

            int urlStartIndex = item.IndexOf("http://");
            if (urlStartIndex == -1)
                urlStartIndex = item.IndexOf("ftp://");

            if (urlStartIndex > -1)
                lbChatMessages.Cursor = Cursors.Hand;
            else
                lbChatMessages.Cursor = Cursors.Default;
        }

        private void btnOptions_Click(object sender, EventArgs e)
        {
            new OptionsForm().ShowDialog();

            DomainController.Instance().ReloadSettings();

            string newHandle = DomainController.Instance().GetMpHandle();

            ProgramConstants.PLAYERNAME = newHandle.Replace('^', '-').Replace('~', '-');

            cmbLANMessageColor.SelectedIndex = DomainController.Instance().GetCnCNetChatColor();
        }

        private void btnModdb_Click(object sender, EventArgs e)
        {
            OpenURL(DomainController.Instance().GetModDBURL());
        }

        private void btnFacebook_Click(object sender, EventArgs e)
        {
            OpenURL(DomainController.Instance().GetFacebookURL());
        }

        private void btnYoutube_Click(object sender, EventArgs e)
        {
            OpenURL(DomainController.Instance().GetYoutubeURL());
        }

        private void btnTwitter_Click(object sender, EventArgs e)
        {
            OpenURL(DomainController.Instance().GetTwitterURL());
        }

        private void btnGooglePlus_Click(object sender, EventArgs e)
        {
            OpenURL(DomainController.Instance().GetGooglePlusURL());
        }

        private void btnForums_Click(object sender, EventArgs e)
        {
            OpenURL(DomainController.Instance().GetForumURL());
        }

        private void OpenURL(string url)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(OpenURLInThread));
            thread.Start(url);
        }

        private void OpenURLInThread(object url)
        {
            Process.Start(((string)url));
        }

        private void tbChatInput_Click(object sender, EventArgs e)
        {
            if (tbChatInput.Text == chatTipText)
                tbChatInput.Text = String.Empty;
        }

        private void tbChatInput_Leave(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(tbChatInput.Text))
                tbChatInput.Text = chatTipText;
        }

        private void tbChatInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                btnSend.PerformClick();
                e.Handled = true;
            }
        }

        private void tbChatInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
                tbChatInput.Text = lastInputCommand;
        }

        private void tbChatInput_Enter(object sender, EventArgs e)
        {
            if (tbChatInput.Text == chatTipText)
                tbChatInput.Text = String.Empty;
        }

        private void NCnCNetLobby_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing)
                return;

            if (CnCNetData.IsGameLobbyOpen)
            {
                DialogResult dr = MsgBoxForm.Show("You are currently in a game. Are you sure you wish to quit the LAN lobby?",
                    "Are you sure?", MessageBoxButtons.YesNo);

                if (dr != System.Windows.Forms.DialogResult.OK)
                    e.Cancel = true;
            }
        }
    }
}
