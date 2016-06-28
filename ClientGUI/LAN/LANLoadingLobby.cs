using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using ClientCore;
using ClientCore.LAN;
using ClientCore.CnCNet5;
using Rampastring.Tools;

namespace ClientGUI.LAN
{
    public partial class LANLoadingLobby : MovableForm
    {
        delegate void StringDelegate(string str);
        delegate void BooleanDelegate(bool b);
        delegate void NoticeDelegate(string message, Color color);
        delegate void ChatDelegate(int color, string message, string sender);
        delegate void LANPlayerDelegate(LANPlayer lp);
        delegate void UserJoinedDelegate(string channelName, string userName, string ipAddress);
        delegate void UserLeftDelegate(string channelName, string userName);
        delegate void UserQuitDelegate(string userName);
        delegate void PrivmsgDelegate(string channelName, string message, string sender);
        delegate void UserKickedDelegate(string channelName, string userName);
        delegate void CTCPDelegate(string sender, string channelName, string message);
        delegate void NetworkMessageDelegate(string sender, string message, IPAddress ipAddress);
        delegate void NoParamCallback();
        delegate void FileSystemWatcherCallback(object sender, FileSystemEventArgs fsea);

        public LANLoadingLobby(LANLobby lobby, bool isHost, string hostName, IPAddress hostAddress,
            string gameRoomName, List<Color> chatColorList, Color defaultChatColor, int myColorId)
        {
            Logger.Log("Creating game loading lobby.");

            Lobby = lobby;
            this.isHost = isHost;
            this.hostName = hostName;
            this.hostAddress = hostAddress;
            this.gameRoomName = gameRoomName;
            chatColors = chatColorList;
            this.defaultChatColor = defaultChatColor;
            myChatColorId = myColorId;
            InitializeComponent();
        }

        private const int CP_NOCLOSE_BUTTON = 0x200;
        /// <summary>
        /// Disables the close button of the form control box.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }

        bool isHost = false;
        IPAddress hostAddress;
        string hostName = String.Empty;
        string gameRoomName = String.Empty;

        bool leaving = false;

        string myGame = "DTA";
        string loadedGameId = String.Empty;

        int playerCount = 0;

        Label[] joinedPlayerLabels = new Label[8];
        List<Color> mpColors = new List<Color>();
        List<string> playerNames = new List<string>();
        List<int> playerColors = new List<int>();

        Image btn133px;
        Image btn133px_c;
        SoundPlayer spButton;

        List<Color> messageColors = new List<Color>();
        List<Color> chatColors;
        Color defaultChatColor;
        Color cListBoxFocusColor;
        int myChatColorId = 0;

        List<LANPlayer> players = new List<LANPlayer>();
        List<LANPlayer> clients = new List<LANPlayer>();

        System.Windows.Forms.Timer timer;

        TcpListener listener;
        volatile TcpClient serverClient;

        LANLobby Lobby;

        private static readonly object locker = new object();

        string unhandledServerMessage = String.Empty;

        FileSystemWatcher fsw;

        private void GameLoadingLobby_Load(object sender, EventArgs e)
        {
            players.Clear();

            SharedUILogic.GameProcessExited += SharedUILogic_GameProcessExited;
            AssignControls();
            SetGraphics();

            this.Font = SharedLogic.GetCommonFont();

            mpColors = SharedUILogic.GetMPColors();

            myGame = DomainController.Instance().GetDefaultGame();

            IniFile loadSpawnIni = new IniFile(ProgramConstants.GamePath + "Saved Games\\spawnSG.ini");

            playerCount = loadSpawnIni.GetIntValue("Settings", "PlayerCount", 0);
            loadedGameId = loadSpawnIni.GetStringValue("Settings", "GameID", "1");
            lblMapNameValue.Text = loadSpawnIni.GetStringValue("Settings", "UIMapName", "No map");
            lblGameModeValue.Text = loadSpawnIni.GetStringValue("Settings", "UIGameMode", "No gamemode");

            for (int i = 0; i < playerCount; i++)
            {
                joinedPlayerLabels[i].Visible = true;
            }

            joinedPlayerLabels[0].Name = ProgramConstants.PLAYERNAME;
            playerNames.Add(ProgramConstants.PLAYERNAME);
            playerColors.Add(loadSpawnIni.GetIntValue("Settings", "Color", 0));
            if (loadSpawnIni.GetIntValue("Settings", "Color", 0) < mpColors.Count)
                joinedPlayerLabels[0].ForeColor = mpColors[loadSpawnIni.GetIntValue("Settings", "Color", 0)];

            for (int i = 1; i < playerCount; i++)
            {
                string sectionName = "Other" + i;

                if (!loadSpawnIni.SectionExists(sectionName))
                    break;

                playerNames.Add(loadSpawnIni.GetStringValue(sectionName, "Name", "Unknown player"));
                playerColors.Add(loadSpawnIni.GetIntValue(sectionName, "Color", 99));
                joinedPlayerLabels[i].Text = playerNames[playerNames.Count - 1];
                joinedPlayerLabels[i].Name = playerNames[playerNames.Count - 1];
                int foreColorIndex = playerColors[playerColors.Count - 1];

                if (foreColorIndex < mpColors.Count)
                    joinedPlayerLabels[i].ForeColor = mpColors[foreColorIndex];
            }

            if (isHost)
            {
                Thread thread = new Thread(new ThreadStart(ListenForClients));
                thread.Start();                

                UpdatePlayerList(true);

                timer = new System.Windows.Forms.Timer();
                timer.Interval = 5000;
                timer.Tick += UpdateGameListing;
                timer.Start();

                UpdateGameListing(this, null);
            }
            else
            {

                btnLoadMPGame.Text = "I'm Ready";
            }

            serverClient = new TcpClient();
            try
            {
                serverClient.Connect(hostAddress, ProgramConstants.LAN_PORT);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connecting to the game host failed! " + ex.Message,
                    "Connecting failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnLeaveLoadingLobby.PerformClick();
                return;
            }
            Thread communicationThread = new Thread(new ThreadStart(HandleServerConnection));
            communicationThread.Start();
        }

        void HandleServerConnection()
        {
            SendMessageToServer("NAME " + ProgramConstants.PLAYERNAME);
            SendMessageToServer("FHASH " + Anticheat.Instance.GetCompleteHash());
            SendMessageToServer("VERSION " + ProgramConstants.GAME_VERSION);

            NetworkStream ns = serverClient.GetStream();

            int bytesRead = 0;
            byte[] message = new byte[4096];

            Encoding encoding = Encoding.GetEncoding(1252);

            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = ns.Read(message, 0, 4096);
                }
                catch (Exception ex)
                {
                    Logger.Log("Client Socket error! Disconnecting. " + ex.Message);
                    break;
                }

                if (bytesRead == 0)
                {
                    Logger.Log("The server has been disconnected!");
                    break;
                }

                string msg = encoding.GetString(message, 0, bytesRead);

                HandleServerCommand(msg);
            }

            serverClient.Close();

            if (!isHost)
            {
                btnLeaveLoadingLobby.PerformClick();
            }
        }

        void HandleServerCommand(string message)
        {
            message = unhandledServerMessage + message;
            List<string> commands = new List<string>();

            while (true)
            {
                int index = message.IndexOf('^');

                if (index == -1)
                {
                    unhandledServerMessage = message;
                    break;
                }
                else
                {
                    commands.Add(message.Substring(0, index));
                    message = message.Substring(index + 1);
                }
            }

            foreach (string cmd in commands)
            {
                if (cmd.StartsWith("CHAT "))
                {
                    string commandPart = cmd.Substring(5);

                    string[] parts = commandPart.Split('~');
                    AddChatMessage(Convert.ToInt32(parts[1]), parts[2], parts[0]);
                }
                else if (cmd == "NOTPRESENT")
                {
                    AddNotice("You cannot start the game before all players are present.");
                }
                else if (cmd == "NOTREADY")
                {
                    WindowFlasher.FlashWindowEx(this);
                    AddNotice("The game host wants to start the game but cannot because not all players are ready!");
                }
                else if (cmd.StartsWith("START"))
                {
                    string[] parts = cmd.Substring(6).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    players[0].Address = hostAddress;
                    int pId = 1;
                    for (int i = 0; i < parts.Length / 2; i += 2)
                    {
                        string[] addressAndPort = parts[i + 1].Split(':');
                        players[pId].Address = IPAddress.Parse(addressAndPort[0]);
                        pId++;
                    }

                    StartGame();
                }
                else if (cmd.StartsWith("PLRS"))
                {
                    string[] parts = cmd.Substring(5).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    players.Clear();

                    for (int i = 0; i < parts.Length / 2; i += 2)
                    {
                        string name = parts[i];
                        bool ready = Convert.ToBoolean(Convert.ToInt32(parts[i + 1]));

                        LANPlayer lp = new LANPlayer();
                        lp.Name = name;
                        lp.Ready = ready;

                        players.Add(lp);
                    }

                    UpdatePlayerList(false);
                }
                else if (cmd.StartsWith("QUIT "))
                {
                    string name = cmd.Substring(5);

                    AddNotice(name + " has left the game.");
                }
                else if (cmd.StartsWith("JOIN "))
                {
                    string name = cmd.Substring(5);

                    AddNotice(name + " has joined the game.");
                }
            }
        }

        #region Server code

        void ListenForClients()
        {
            listener = new TcpListener(IPAddress.Any, 1234);
            listener.Start();

            while (true)
            {
                TcpClient client;

                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception ex)
                {
                    if (leaving)
                        return;
                    Logger.Log("Listener error: " + ex.Message);
                    AddNotice("Listener error: " + ex.Message);
                    AddNotice("No new players will be able to connect to this game.");
                    break;
                }

                Logger.Log("New client connected from " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());

                if (clients.Count >= playerCount)
                {
                    Logger.Log("Dropping client because of player limit.");
                    client.Client.Disconnect(false);
                    client.Close();
                    continue;
                }

                Thread thread = new Thread(new ParameterizedThreadStart(HandleClientConnection));
                thread.Start(client);
            }
        }

        void HandleClientConnection(object tcpclient)
        {
            TcpClient client = (TcpClient)tcpclient;

            byte[] message = new byte[4096];

            string msg = String.Empty;

            Encoding encoding = Encoding.GetEncoding(1252);

            int bytesRead = 0;

            LANPlayer lp = new LANPlayer();

            NetworkStream ns = client.GetStream();

            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = ns.Read(message, 0, 4096);
                }
                catch (Exception ex)
                {
                    //a socket error has occured
                    Logger.Log("Socket error with client " + lp.Name + "; removing. Message: " + ex.Message);
                    RemovePlayer(lp);
                    break;
                }

                if (bytesRead > 0)
                {
                    msg = encoding.GetString(message, 0, bytesRead);

                    msg = lp.UnhandledMessagePart + msg;
                    List<string> commands = new List<string>();

                    while (true)
                    {
                        int index = msg.IndexOf('^');

                        if (index == -1)
                        {
                            lp.UnhandledMessagePart = msg;
                            break;
                        }
                        else
                        {
                            commands.Add(msg.Substring(0, index));
                            msg = msg.Substring(index + 1);
                        }
                    }

                    foreach (string cmd in commands)
                    {
                        Logger.Log("HM: " + cmd);

                        if (cmd.StartsWith("CHAT "))
                        {
                            if (!lp.Verified)
                                continue;

                            string commandPart = cmd.Substring(5);

                            string[] parts = commandPart.Split('~');
                            lp.Stream = ns;
                            Broadcast("CHAT " + lp.Name + "~" + parts[0] + "~" + parts[1]);
                        }
                        else if (cmd.StartsWith("NAME ") && String.IsNullOrEmpty(lp.Name))
                        {
                            lp.Name = cmd.Substring(5);

                            if (!playerNames.Contains(lp.Name))
                                RemovePlayer(lp);

                            UpdatePlayerList(true);
                        }
                        else if (cmd.StartsWith("FHASH "))
                        {
                            if (Anticheat.Instance.GetCompleteHash() != cmd.Substring(6))
                            {
                                AddNotice("Player " + lp.Name + " - modified files detected! They could be cheating!", Color.Red);
                            }

                            if (lp.Verified)
                                continue;

                            lp.Verified = true;
                            lp.Stream = ns;
                            lp.Client = client;
                            lp.Address = ((IPEndPoint)client.Client.RemoteEndPoint).Address;

                            AddPlayer(lp);
                        }
                        else if (cmd.StartsWith("VERSION "))
                        {
                            string version = cmd.Substring(8);

                            if (version != ProgramConstants.GAME_VERSION)
                                AddNotice("Player " + lp.Name + " is running an incompatible version " + version + ". It could cause crashes or synchronization errors in-game.");
                        }
                        else if (cmd.StartsWith("READY"))
                        {
                            bool status = Convert.ToBoolean(Int32.Parse(cmd.Substring(6, 1)));

                            lp.Ready = status;
                            UpdatePlayerList(true);
                        }
                    }
                }
            }

            RemovePlayer(lp);
            if (!lp.Verified && !String.IsNullOrEmpty(lp.Name))
                Broadcast("QUIT " + lp.Name);
        }

        void RemovePlayer(LANPlayer p)
        {
            if (this.InvokeRequired)
            {
                LANPlayerDelegate d = new LANPlayerDelegate(RemovePlayer);
                BeginInvoke(d, p);
                return;
            }

            Logger.Log("Removing player.");

            if (p.Client == null)
                return;

            lock (locker)
            {
                p.Client.Close();
                clients.Remove(p);
            }

            UpdatePlayerList(true);
        }

        void AddPlayer(LANPlayer p)
        {
            if (this.InvokeRequired)
            {
                LANPlayerDelegate d = new LANPlayerDelegate(AddPlayer);
                BeginInvoke(d, p);
                return;
            }

            lock (locker)
            {
                clients.Add(p);
            }

            Broadcast("JOIN " + p.Name);
            UpdatePlayerList(true);
        }

        void Broadcast(string message)
        {
            if (this.InvokeRequired)
            {
                StringDelegate d = new StringDelegate(Broadcast);
                BeginInvoke(d, message);
                return;
            }

            foreach (LANPlayer lp in clients)
            {
                lp.SendMessage(message);
            }
        }

        #endregion

        void SendChatMessage(int color, string message)
        {
            SendMessageToServer("CHAT " + color + "~" + message);
        }

        void SendMessageToServer(string message)
        {
            if (!serverClient.Connected)
                return;

            Logger.Log("SM: " + message);

            Encoding encoder = Encoding.GetEncoding(1252);
            byte[] buffer = encoder.GetBytes(message + "^");

            NetworkStream ns = serverClient.GetStream();

            try
            {
                ns.Write(buffer, 0, buffer.Length);
                ns.Flush();
            }
            catch (Exception ex)
            {
                Logger.Log("Error sending data to server: " + ex.Message);
            }
        }

        void AddChatMessage(int color, string message, string sender)
        {
            if (lbLoadingLobbyChat.InvokeRequired)
            {
                ChatDelegate d = new ChatDelegate(AddChatMessage);
                BeginInvoke(d, color, message, sender);
                return;
            }

            if (color > -1 && color < chatColors.Count)
                messageColors.Add(chatColors[color]);
            else
                messageColors.Add(Color.White);

            lbLoadingLobbyChat.Items.Add(string.Format("[{0}] {1}: {2}", DateTime.Now.ToShortTimeString(),
                sender, message));
        }

        void SharedUILogic_GameProcessExited()
        {
            if (this.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(SharedUILogic_GameProcessExited);
                BeginInvoke(d, null);
                return;
            }

            if (SavedGameManager.AreSavedGamesAvailable())
            {
                fsw.Created -= fsw_Created;
                fsw.Changed -= fsw_Created;
                fsw.Dispose();
            }

            Logger.Log("The game process has exited; exiting game loading lobby.");

            DomainController.Instance().ReloadSettings();

            ProgramConstants.IsInGame = false;
            CnCNetData.DoGameStopped();

            btnLeaveLoadingLobby.PerformClick();
        }

        void AddNotice(string message)
        {
            AddNotice(message, Color.White);
        }

        void AddNotice(string message, Color color)
        {
            if (lbLoadingLobbyChat.InvokeRequired)
            {
                NoticeDelegate d = new NoticeDelegate(AddNotice);
                BeginInvoke(d, message, color);
                return;
            }

            messageColors.Add(color);
            lbLoadingLobbyChat.Items.Add(message);
        }

        void UpdateGameListing(object sender, EventArgs e)
        {
            if (!isHost)
                return;

            StringBuilder sb;

            sb = new StringBuilder("GAME ");

            sb.Append(ProgramConstants.LAN_PROTOCOL_REVISION + ";");
            sb.Append(ProgramConstants.GAME_VERSION);
            sb.Append(";");
            sb.Append(playerCount);
            sb.Append(";");
            sb.Append(gameRoomName);
            sb.Append(";");
            if (players.Count == playerCount)
                sb.Append("1");
            else
                sb.Append("0");
            if (leaving)
                sb.Append("1");
            else
                sb.Append("0");
            sb.Append("1"); // isLoadedGame
            sb.Append(";");
            foreach (string player in playerNames)
            {
                sb.Append(player);
                sb.Append(",");
            }
            sb.Append(";");
            sb.Append(lblMapNameValue.Text);
            sb.Append(";");
            sb.Append(lblGameModeValue.Text + " (Loaded Game)");
            sb.Append(";");
            sb.Append(loadedGameId); // loadedGameId
            sb.Append(";");
            sb.Append(myGame);
            sb.Append(";");

            Lobby.SendMessage(sb.ToString());

            if (!leaving)
                timer.Enabled = true;
        }

        private void AssignControls()
        {
            joinedPlayerLabels[0] = lblPlayerOne;
            joinedPlayerLabels[1] = lblPlayerTwo;
            joinedPlayerLabels[2] = lblPlayerThree;
            joinedPlayerLabels[3] = lblPlayerFour;
            joinedPlayerLabels[4] = lblPlayerFive;
            joinedPlayerLabels[5] = lblPlayerSix;
            joinedPlayerLabels[6] = lblPlayerSeven;
            joinedPlayerLabels[7] = lblPlayerEight;
        }

        private void SetGraphics()
        {
            btn133px = SharedUILogic.LoadImage("133pxbtn.png");
            btn133px_c = SharedUILogic.LoadImage("133pxbtn_c.png");
            spButton = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "button.wav");

            cListBoxFocusColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetListBoxFocusColor());

            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "clienticon.ico");

            this.BackgroundImage = SharedUILogic.LoadImage("loadmpsavebg.png");
            panelSGPlayers.BackgroundImage = SharedUILogic.LoadImage("loadmpsavepanelbg.png");

            btnLeaveLoadingLobby.DefaultImage = btn133px;
            btnLeaveLoadingLobby.HoveredImage = btn133px_c;
            btnLeaveLoadingLobby.HoverSound = spButton;
            btnLoadMPGame.DefaultImage = btn133px;
            btnLoadMPGame.HoveredImage = btn133px_c;
            btnLoadMPGame.HoverSound = spButton;

            Color cLabelColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUILabelColor());
            lblDescription.ForeColor = cLabelColor;
            lblGameMode.ForeColor = cLabelColor;
            lblMapName.ForeColor = cLabelColor;
            lblLoadingLobbyChat.ForeColor = cLabelColor;
            lblMapNameValue.ForeColor = cLabelColor;
            lblGameModeValue.ForeColor = cLabelColor;
            foreach (Label lbl in joinedPlayerLabels)
                lbl.ForeColor = cLabelColor;

            Color cAltUiColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUIAltColor());
            btnLoadMPGame.ForeColor = cAltUiColor;
            btnLeaveLoadingLobby.ForeColor = cAltUiColor;

            Color cBackColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor());

            this.BackColor = cBackColor;
            panelSGPlayers.BackColor = cBackColor;

            int displayedItems = lbLoadingLobbyChat.DisplayRectangle.Height / lbLoadingLobbyChat.ItemHeight;

            sbLoadingLobbyChat.ThumbBottomImage = SharedUILogic.LoadImage("sbThumbBottom.png");
            sbLoadingLobbyChat.ThumbBottomSpanImage = SharedUILogic.LoadImage("sbThumbBottomSpan.png");
            sbLoadingLobbyChat.ThumbMiddleImage = SharedUILogic.LoadImage("sbMiddle.png");
            sbLoadingLobbyChat.ThumbTopImage = SharedUILogic.LoadImage("sbThumbTop.png");
            sbLoadingLobbyChat.ThumbTopSpanImage = SharedUILogic.LoadImage("sbThumbTopSpan.png");
            sbLoadingLobbyChat.UpArrowImage = SharedUILogic.LoadImage("sbUpArrow.png");
            sbLoadingLobbyChat.DownArrowImage = SharedUILogic.LoadImage("sbDownArrow.png");
            sbLoadingLobbyChat.BackgroundImage = SharedUILogic.LoadImage("sbBackground.png");
            sbLoadingLobbyChat.Scroll += sbLoadingLobbyChat_Scroll;
            sbLoadingLobbyChat.Maximum = lbLoadingLobbyChat.Items.Count - Convert.ToInt32(displayedItems * 0.2);
            sbLoadingLobbyChat.Minimum = 0;
            sbLoadingLobbyChat.ChannelColor = cBackColor;
            sbLoadingLobbyChat.LargeChange = 27;
            sbLoadingLobbyChat.SmallChange = 9;
            sbLoadingLobbyChat.Value = 0;

            lbLoadingLobbyChat.MouseWheel += lbLoadingLobbyChat_MouseWheel;
            lbLoadingLobbyChat.BackColor = cBackColor;
            lbLoadingLobbyChat.ForeColor = cAltUiColor;
            lbLoadingLobbyChat.ListBoxFocusColor = cListBoxFocusColor;

            tbLoadingLobbyChatInput.ForeColor = chatColors[myChatColorId];

            SharedUILogic.ParseClientThemeIni(this);
        }

        private void lbLoadingLobbyChat_MouseWheel(object sender, MouseEventArgs e)
        {
            sbLoadingLobbyChat.Value += e.Delta / -40;
            sbLoadingLobbyChat_Scroll(sender, EventArgs.Empty);
        }

        private void sbLoadingLobbyChat_Scroll(object sender, EventArgs e)
        {
            lbLoadingLobbyChat.TopIndex = sbLoadingLobbyChat.Value;
        }

        void UpdatePlayerList(bool broadcast)
        {
            if (this.InvokeRequired)
            {
                BooleanDelegate d = new BooleanDelegate(UpdatePlayerList);
                this.BeginInvoke(d, broadcast);
                return;
            }

            int pId = 0;

            bool[] isPlayerPresent = new bool[playerCount];

            StringBuilder sb = new StringBuilder("PLRS ");

            List<LANPlayer> list = players;

            if (isHost)
                list = clients;

            for (pId = 0; pId < list.Count; pId++)
            {
                Label label = null;

                int pIndex = 0;

                LANPlayer lp = list[pId];

                for (int i = 0; i < joinedPlayerLabels.Length; i++)
                {
                    if (joinedPlayerLabels[i].Name == lp.Name)
                    {
                        label = joinedPlayerLabels[i];
                        pIndex = i;
                    }
                }

                if (label == null)
                    continue; // Something is very wrong at this point

                sb.Append(lp.Name);
                sb.Append(";");
                sb.Append(Convert.ToInt32(lp.Ready));
                sb.Append(";");

                isPlayerPresent[pIndex] = true;

                if (lp.Ready || lp.Address.Equals(hostAddress))
                    label.Text = lp.Name;
                else
                    label.Text = lp.Name + " (Not Ready)";

                if (playerColors[pIndex] + 1 < mpColors.Count)
                    label.ForeColor = mpColors[playerColors[pIndex] + 1];
                else
                    label.ForeColor = Color.White;
            }

            for (pId = 0; pId < playerCount; pId++)
            {
                if (isPlayerPresent[pId])
                    continue;

                joinedPlayerLabels[pId].Text = playerNames[pId] + " (Not present)";
                joinedPlayerLabels[pId].ForeColor = Color.Gray;
            }

            if (isHost && broadcast)
            {
                Broadcast(sb.ToString());
            }
        }

        private void lbLoadingLobbyChat_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)e.Graphics.MeasureString(lbLoadingLobbyChat.Items[e.Index].ToString(),
                lbLoadingLobbyChat.Font, lbLoadingLobbyChat.Width - 30).Height;
            e.ItemWidth = lbLoadingLobbyChat.Width - 30;
        }

        /// <summary>
        /// Used for manually drawing chat messages in the chat message list box.
        /// </summary>
        private void lbLoadingLobbyChat_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1 && e.Index < lbLoadingLobbyChat.Items.Count)
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

                foreColor = messageColors[e.Index];
                e.Graphics.DrawString(lbLoadingLobbyChat.Items[e.Index].ToString(), e.Font, new SolidBrush(foreColor), e.Bounds);
            }
        }

        private void tbLoadingLobbyChatInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData != Keys.Return)
            {
                return;
            }

            e.Handled = true;

            if (String.IsNullOrEmpty(tbLoadingLobbyChatInput.Text))
            {
                return;
            }

            tbLoadingLobbyChatInput.Text = tbLoadingLobbyChatInput.Text.Replace('~', '-');

            SendChatMessage(myChatColorId, tbLoadingLobbyChatInput.Text);
            //DateTime now = DateTime.Now;
            //string text = "[" + now.ToShortTimeString() + "] " + ProgramConstants.PLAYERNAME + ": " + tbLoadingLobbyChatInput.Text;
            //messageColors.Add(chatColors[myChatColorId]);
            //lbLoadingLobbyChat.Items.Add(text);
            tbLoadingLobbyChatInput.Clear();
            //ScrollListbox(text);
        }

        /// <summary>
        /// Used for automatic scrolling of the chat list box as new entries are added.
        /// </summary>
        private void ScrollListbox(string text)
        {
            int displayedItems = lbLoadingLobbyChat.DisplayRectangle.Height / lbLoadingLobbyChat.ItemHeight;
            sbLoadingLobbyChat.Maximum = lbLoadingLobbyChat.Items.Count - Convert.ToInt32(displayedItems * 0.2);
            if (sbLoadingLobbyChat.Maximum < 0)
                sbLoadingLobbyChat.Maximum = 1;
            double multi = CreateGraphics().MeasureString(text, lbLoadingLobbyChat.Font, lbLoadingLobbyChat.Width - 30).Height /
                CreateGraphics().MeasureString("@", lbLoadingLobbyChat.Font).Height;
            int x = 0;
            while (x < multi)
            {
                sbLoadingLobbyChat.Value++;
                lbLoadingLobbyChat.TopIndex++;
                x++;
            }
        }

        private void tbLoadingLobbyChatInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (string.IsNullOrEmpty(tbLoadingLobbyChatInput.Text))
                return;

            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
            }
        }

        private void btnLeaveLoadingLobby_Click(object sender, EventArgs e)
        {
            if (isHost)
            {
                leaving = true;
                UpdateGameListing(sender, e);

                foreach (LANPlayer lp in clients)
                {
                    if (lp.Client != null && lp.Client.Connected)
                        lp.Client.Close();
                }

                listener.Stop();
            }

            SendMessageToServer("QUIT");

            if (serverClient.Connected)
                serverClient.Close();

            Unsubsribe();
            this.Close();

            CnCNetData.IsGameLoadingLobbyOpen = false;
            CnCNetData.DoGameLoadingLobbyClosed();
        }

        private void Unsubsribe()
        {
            SharedUILogic.GameProcessExited -= SharedUILogic_GameProcessExited;

            btn133px.Dispose();
            btn133px_c.Dispose();
        }

        private void btnLoadMPGame_Click(object sender, EventArgs e)
        {
            if (!isHost)
            {
                SendMessageToServer("READY 1");
                return;
            }

            if (players.Count != playerCount)
            {
                Broadcast("NOTPRESENT");
                return;
            }

            for (int pId = 1; pId < players.Count; pId++)
            {
                if (!players[pId].Ready)
                {
                    Broadcast("NOTREADY");
                    return;
                }
            }

            StringBuilder sb = new StringBuilder("START ");
            for (int pId = 1; pId < players.Count; pId++)
            {
                sb.Append(";");
                sb.Append(players[pId].Name);
                sb.Append(";");
                sb.Append(players[pId].Address.ToString());
                sb.Append(":1235");
            }
            Broadcast(sb.ToString());
            StartGame();
            btnLoadMPGame.Enabled = false;
        }

        private void StartGame()
        {
            File.Delete(ProgramConstants.GamePath + "spawn.ini");
            File.Copy(ProgramConstants.GamePath + "Saved Games\\spawnSG.ini", ProgramConstants.GamePath + "spawn.ini");

            IniFile spawnIni = new IniFile(ProgramConstants.GamePath + "spawn.ini");

            spawnIni.SetStringValue("Settings", "SaveGameName", "SAVEGAME.NET");
            spawnIni.SetBooleanValue("Settings", "LoadSaveGame", true);

            spawnIni.SetIntValue("Settings", "Port", 1235);

            for (int pIndex = 0; pIndex < players.Count; pIndex++)
            {
                if (players[pIndex].Name == ProgramConstants.PLAYERNAME)
                    continue;

                for (int i = 1; i <= players.Count; i++)
                {
                    string sectionName = "Other" + i;

                    if (spawnIni.GetStringValue(sectionName, "Name", String.Empty) == players[pIndex].Name)
                    {
                        spawnIni.SetStringValue(sectionName, "Ip", players[pIndex].Address.ToString());
                        spawnIni.SetIntValue(sectionName, "Port", 1235);
                        break;
                    }
                }
            }

            spawnIni.WriteIniFile();

            if (SavedGameManager.AreSavedGamesAvailable())
            {
                fsw = new FileSystemWatcher(ProgramConstants.GamePath + "Saved Games", "*.NET");
                fsw.EnableRaisingEvents = true;
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Created;
            }

            StartGameProcess();
        }

        void fsw_Created(object sender, FileSystemEventArgs e)
        {
            if (this.InvokeRequired)
            {
                FileSystemWatcherCallback d = new FileSystemWatcherCallback(fsw_Created);
                BeginInvoke(d, sender, e);
                return;
            }

            Logger.Log("FSW Event: " + e.FullPath);

            if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
            {
                SavedGameManager.RenameSavedGame();
            }
        }

        /// <summary>
        /// Starts the game process and changes some internal variables so other client components know it as well.
        /// </summary>
        private void StartGameProcess()
        {
            ProgramConstants.IsInGame = true;

            SharedUILogic.StartGameProcess(0);

            CnCNetData.DoGameStarted();
        }
    }
}
