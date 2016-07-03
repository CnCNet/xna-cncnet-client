/// @author Rampastring
/// http://www.moddb.com/members/rampastring

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Media;
using System.Reflection;
using System.Threading;
using Timer = System.Windows.Forms.Timer;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.LAN;
using ClientCore.Statistics;
using Rampastring.Tools;
using Utilities = ClientCore.Utilities;

namespace ClientGUI.LAN
{
    /// <summary>
    /// The CnCNet Game Lobby.
    /// </summary>
    public partial class LANGameLobby : MovableForm
    {
        private delegate void NoParamCallback();
        delegate void EventCallback(object sender, EventArgs e);
        delegate void ChatDelegate(int color, string message, string sender);
        delegate void NoticeDelegate(string message, Color color);
        delegate void IntDelegate(int i);
        private delegate void StringCallback(string str);
        private delegate void DualStringCallback(string str1, string str2);
        private delegate void TripleStringCallback(string str1, string str2, string str3);
        private delegate void FileSystemWatcherCallback(object sender, FileSystemEventArgs fsea);

        public delegate void MapCallback(Map map);

        /// <summary>
        /// Creates a new instance of the game lobby.
        /// </summary>
        /// <param name="channelName">The name of the game room channel.</param>
        /// <param name="isAdmin">True if we are the admin of the game room, otherwise false.</param>
        /// <param name="maxPlayers">The maximum player count of the game room.</param>
        /// <param name="adminName">The name of the game room's admin.</param>
        /// <param name="gameRoomName">The UI name of the game room.</param>
        /// <param name="password">The password (custom or automatically generated) of the game room.</param>
        /// <param name="customPassword">True if the game room uses a custom password, otherwise false.</param>
        /// <param name="chatColorList">The list of IRC chat colors.</param>
        /// <param name="defChatColor">The default chat color.</param>
        /// <param name="myColorId">The chat color ID used by the local user.</param>
        public LANGameLobby(LANLobby lobby, bool isAdmin, string serverAddress, int maxPlayers, 
            string adminName, string gameRoomName, List<Color> chatColorList, Color defChatColor, int myColorId)
        {
            Logger.Log("Creating game lobby.");

            InitializeComponent();
            isHost = isAdmin;
            this.serverAddress = serverAddress;
            playerLimit = maxPlayers;
            GameRoomName = gameRoomName;
            AdminName = adminName;
            this.Text = "Game Lobby: " + ProgramConstants.PLAYERNAME;
            ChatColors = chatColorList;
            defaultChatColor = defChatColor;
            myChatColorId = myColorId;
            Lobby = lobby;
            if (isAdmin)
            {
                AdminName = ProgramConstants.PLAYERNAME;

                Seed = new Random().Next(10000, 100000);
            }
            LANLobby.OnColorChanged += new LANLobby.ColorChangedEventHandler(LANLobby_OnColorChanged);
        }

        /// <summary>
        /// Called whenever the user changes their color on the main lobby.
        /// </summary>
        /// <param name="colorId"></param>
        private void LANLobby_OnColorChanged(int colorId)
        {
            myChatColorId = colorId;
        }

        string defaultGame = "dta";

        string GameRoomName = "DTA_DefaultName";
        string AdminName = "";
        bool isHost = false;
        int playerLimit = 8;
        int Seed = 99999;
        int coopDifficultyLevel = 0;
        bool Locked = false;
        bool leaving = false;
        //bool isHostInGame = false;
        string serverAddress = String.Empty;

        List<PlayerInfo> Players = new List<PlayerInfo>();
        List<PlayerInfo> AIPlayers = new List<PlayerInfo>();

        List<Color> MPColors = new List<Color>();
        List<UserCheckBox> CheckBoxes = new List<UserCheckBox>();
        List<string> AssociatedCheckBoxSpawnIniOptions = new List<string>();
        List<string> AssociatedCheckBoxCustomInis = new List<string>();
        List<LimitedComboBox> ComboBoxes = new List<LimitedComboBox>();
        List<string> AssociatedComboBoxSpawnIniOptions = new List<string>();
        List<string> ComboBoxSidePrereqErrorDescriptions = new List<string>();
        List<DataWriteMode> ComboBoxDataWriteModes = new List<DataWriteMode>();
        List<SideComboboxPrerequisite> SideComboboxPrerequisites = new List<SideComboboxPrerequisite>();
        SideCheckboxPrerequisite[] SideCheckboxPrerequisites;

        TextBox[] pNameTextBoxes;
        TextBox[] pSideLabels;

        UserCheckBox chkDisableSounds;

        UserCheckBox chkP1Ready;
        UserCheckBox chkP2Ready;
        UserCheckBox chkP3Ready;
        UserCheckBox chkP4Ready;
        UserCheckBox chkP5Ready;
        UserCheckBox chkP6Ready;
        UserCheckBox chkP7Ready;
        UserCheckBox chkP8Ready;

        Map currentMap;
        string currentSHA1 = String.Empty;
        string currentGameMode = String.Empty;

        bool isManualClose = false;

        Timer timer;

        bool updatePlayers = true;
        bool updateGameOptions = true;

        List<Color> MessageColors = new List<Color>();
        List<Color> ChatColors;
        Color defaultChatColor;
        Color cListBoxFocusColor;

        int myChatColorId = 0;

        SoundPlayer sndButtonSound;
        SoundPlayer sndJoinSound;
        SoundPlayer sndLeaveSound;
        SoundPlayer sndMessageSound;

        Image btn133px;
        Image btn133px_c;

        Image imgKick;
        Image imgKick_c;
        Image imgBan;
        Image imgBan_c;

        Image[] startingLocationIndicators;
        Image enemyStartingLocationIndicator;
        double previewRatioX = 1.0;
        double previewRatioY = 1.0;

        System.Windows.Forms.Timer resizeTimer;

        List<string>[] PlayerNamesOnPlayerLocations;
        List<int>[] PlayerColorsOnPlayerLocations;
        Font playerNameOnPlayerLocationFont;
        string[] TeamIdentifiers;

        bool sharpenPreview = true;
        bool displayCoopBriefing = true;
        Color coopBriefingForeColor;
        Timer gameOptionRefreshTimer;

        MatchStatistics ms;

        LabelInfoStyle mapInfoStyle = LabelInfoStyle.STANDARD;

        FileSystemWatcher fsw;

        bool gameSaved = false;

        LANLobby Lobby;

        TcpListener listener;
        TcpClient serverClient;

        string unhandledServerMessage = String.Empty;

        List<LANPlayer> Clients = new List<LANPlayer>();

        private static readonly object locker = new object();

        /// <summary>
        /// Sets up the theme of the game lobby and performs initialization.
        /// </summary>
        private void NGameLobby_Load(object sender, EventArgs e)
        {
            SharedUILogic.GameProcessExited += GameProcessExited;

            defaultGame = DomainController.Instance().GetDefaultGame();

            this.Font = SharedLogic.GetCommonFont();
            lbGameLobbyChat.Font = SharedLogic.GetListBoxFont();

            if (DomainController.Instance().GetMapInfoStyle() == "ALLCAPS")
            {
                mapInfoStyle = LabelInfoStyle.ALLCAPS;
            }

            startingLocationIndicators = SharedUILogic.LoadStartingLocationIndicators();

            enemyStartingLocationIndicator = SharedUILogic.LoadImage("enemyslocindicator.png");

            PlayerNamesOnPlayerLocations = new List<string>[8];
            PlayerColorsOnPlayerLocations = new List<int>[8];
            for (int id = 0; id < 8; id++)
            {
                PlayerNamesOnPlayerLocations[id] = new List<string>();
                PlayerColorsOnPlayerLocations[id] = new List<int>();
            }

            playerNameOnPlayerLocationFont = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            TeamIdentifiers = SharedUILogic.GetTeamIdentifiers();

            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.GetBaseResourcePath() + "clienticon.ico");
            this.BackgroundImage = SharedUILogic.LoadImage("gamelobbybg.png");
            playerPanel.BackgroundImage = SharedUILogic.LoadImage("gamelobbypanelbg.png");
            gameOptionsPanel.BackgroundImage = SharedUILogic.LoadImage("gamelobbyoptionspanelbg.png");

            sndButtonSound = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "button.wav");
            sndJoinSound = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "joingame.wav");
            sndLeaveSound = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "leavegame.wav");
            sndMessageSound = new SoundPlayer(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "message.wav");

            btn133px = SharedUILogic.LoadImage("133pxbtn.png");
            btn133px_c = SharedUILogic.LoadImage("133pxbtn_c.png");

            btnLaunchGame.DefaultImage = btn133px;
            btnLaunchGame.HoveredImage = btn133px_c;
            btnLaunchGame.HoverSound = sndButtonSound;
            btnLockGame.DefaultImage = btn133px;
            btnLockGame.HoveredImage = btn133px_c;
            btnLockGame.HoverSound = sndButtonSound;
            btnChangeMap.DefaultImage = btn133px;
            btnChangeMap.HoveredImage = btn133px_c;
            btnChangeMap.HoverSound = sndButtonSound;
            btnLeaveGame.DefaultImage = btn133px;
            btnLeaveGame.HoveredImage = btn133px_c;
            btnLeaveGame.HoverSound = sndButtonSound;

            InitKickButtons();

            SharedUILogic.SetBackgroundImageLayout(this);

            sharpenPreview = DomainController.Instance().GetImageSharpeningCnCNetStatus();

            string[] mpColorNames = DomainController.Instance().GetMPColorNames().Split(',');
            for (int cmbId = 1; cmbId < 9; cmbId++)
            {
                getPlayerColorCMBFromId(cmbId).AddItem("Random", Color.White);
            }

            MPColors = SharedUILogic.GetMPColors();

            cListBoxFocusColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetListBoxFocusColor());

            for (int colorId = 1; colorId < 9; colorId++)
            {
                for (int cmbId = 1; cmbId < 9; cmbId++)
                {
                    getPlayerColorCMBFromId(cmbId).AddItem(mpColorNames[colorId - 1], MPColors[colorId]);
                }
            }

            string[] sides = DomainController.Instance().GetSides().Split(',');
            SideCheckboxPrerequisites = new SideCheckboxPrerequisite[sides.Length];

            for (int sideId = 0; sideId < sides.Length; sideId++)
            {
                SideComboboxPrerequisites.Add(new SideComboboxPrerequisite());
                SideCheckboxPrerequisites[sideId] = new SideCheckboxPrerequisite();
            }

            Image[] sideImages = SharedUILogic.LoadSideImages();

            for (int pId = 1; pId < 9; pId++)
            {
                LimitedComboBox cmb = getPlayerSideCMBFromId(pId);
                cmb.Items.Add(new DropDownItem("Random", sideImages[0]));
                cmb.FocusColor = cListBoxFocusColor;
            }
            int i = 1;
            foreach (string sideName in sides)
            {
                for (int pId = 1; pId < 9; pId++)
                {
                    getPlayerSideCMBFromId(pId).Items.Add(new DropDownItem(sideName, sideImages[i]));
                }

                i++;
            }
            for (int pId = 1; pId < 9; pId++)
            {
                getPlayerSideCMBFromId(pId).Items.Add(new DropDownItem("Spectator", sideImages[i]));
            }

            string panelBorderStyle = DomainController.Instance().GetPanelBorderStyle();
            if (panelBorderStyle == "FixedSingle")
            {
                playerPanel.BorderStyle = BorderStyle.FixedSingle;
                gameOptionsPanel.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (panelBorderStyle == "Fixed3D")
            {
                playerPanel.BorderStyle = BorderStyle.Fixed3D;
                gameOptionsPanel.BorderStyle = BorderStyle.Fixed3D;
            }
            else
            {
                playerPanel.BorderStyle = BorderStyle.None;
                gameOptionsPanel.BorderStyle = BorderStyle.None;
            }

            IniFile clIni = new IniFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "GameOptions.ini");

            Color cLabelColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUILabelColor());

            Color cAltUiColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUIAltColor());

            Color cBackColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor());

            toolTip1.BackColor = cBackColor;
            toolTip1.ForeColor = cLabelColor;

            coopBriefingForeColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetBriefingForeColor());

            int displayedItems = lbGameLobbyChat.DisplayRectangle.Height / lbGameLobbyChat.ItemHeight;

            sbGameLobbyChat.ThumbBottomImage = SharedUILogic.LoadImage("sbThumbBottom.png");
            sbGameLobbyChat.ThumbBottomSpanImage = SharedUILogic.LoadImage("sbThumbBottomSpan.png");
            sbGameLobbyChat.ThumbMiddleImage = SharedUILogic.LoadImage("sbMiddle.png");
            sbGameLobbyChat.ThumbTopImage = SharedUILogic.LoadImage("sbThumbTop.png");
            sbGameLobbyChat.ThumbTopSpanImage = SharedUILogic.LoadImage("sbThumbTopSpan.png");
            sbGameLobbyChat.UpArrowImage = SharedUILogic.LoadImage("sbUpArrow.png");
            sbGameLobbyChat.DownArrowImage = SharedUILogic.LoadImage("sbDownArrow.png");
            sbGameLobbyChat.BackgroundImage = SharedUILogic.LoadImage("sbBackground.png");
            sbGameLobbyChat.Scroll += customScrollbar1_Scroll;
            sbGameLobbyChat.Maximum = lbGameLobbyChat.Items.Count - Convert.ToInt32(displayedItems * 0.2);
            sbGameLobbyChat.Minimum = 0;
            sbGameLobbyChat.ChannelColor = cBackColor;
            sbGameLobbyChat.LargeChange = 27;
            sbGameLobbyChat.SmallChange = 9;
            sbGameLobbyChat.Value = 0;

            lbGameLobbyChat.MouseWheel += lbChatBox_MouseWheel;

            pNameTextBoxes = new TextBox[8];
            for (int tbId = 0; tbId < pNameTextBoxes.Length; tbId++)
            {
                pNameTextBoxes[tbId] = new TextBox();
                TextBox pNameTextBox = pNameTextBoxes[tbId];
                pNameTextBox.Location = getPlayerNameCMBFromId(tbId + 1).Location;
                pNameTextBox.Size = getPlayerNameCMBFromId(tbId + 1).Size;
                pNameTextBox.BorderStyle = BorderStyle.FixedSingle;
                pNameTextBox.Font = cmbP1Name.Font;
                pNameTextBox.GotFocus += playerNameTextBox_GotFocus;
                pNameTextBox.ForeColor = cAltUiColor;
                pNameTextBox.BackColor = cBackColor;
                pNameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                playerPanel.Controls.Add(pNameTextBox);
                pNameTextBox.Visible = false;
            }

            Font gameOptionFont = SharedLogic.GetFont(DomainController.Instance().GetGameOptionFont());

            string[] checkBoxes = clIni.GetStringValue("GameLobby", "CheckBoxes", "none").Split(',');
            foreach (string checkBoxName in checkBoxes)
            {
                if (clIni.SectionExists(checkBoxName))
                {
                    string chkText = clIni.GetStringValue(checkBoxName, "Text", "No description");
                    string associatedSpawnIniOption = clIni.GetStringValue(checkBoxName, "AssociateSpawnIniOption", "none");
                    AssociatedCheckBoxSpawnIniOptions.Add(associatedSpawnIniOption);
                    string associatedCustomIni = clIni.GetStringValue(checkBoxName, "AssociateCustomIni", "none");
                    AssociatedCheckBoxCustomInis.Add(associatedCustomIni);
                    bool defaultValue = clIni.GetBooleanValue(checkBoxName, "DefaultValue", false);
                    string[] location = clIni.GetStringValue(checkBoxName, "Location", "0,0").Split(',');
                    Point pLocation = new Point(Convert.ToInt32(location[0]), Convert.ToInt32(location[1]));
                    string toolTip = clIni.GetStringValue(checkBoxName, "ToolTip", String.Empty);

                    UserCheckBox chkBox = new UserCheckBox(cLabelColor, cAltUiColor, chkText);
                    chkBox.AutoSize = true;
                    chkBox.Location = pLocation;
                    chkBox.Name = checkBoxName;
                    if (defaultValue)
                        chkBox.Checked = true;
                    else
                        chkBox.Checked = false;
                    if (!isHost)
                        chkBox.IsEnabled = false;
                    chkBox.CheckedChanged += new UserCheckBox.OnCheckedChanged(GenericChkBox_CheckedChanged);
                    chkBox.Font = gameOptionFont;

                    if (!String.IsNullOrEmpty(toolTip))
                    {
                        toolTip1.SetToolTip(chkBox, toolTip);
                        toolTip1.SetToolTip(chkBox.label1, toolTip);
                        toolTip1.SetToolTip(chkBox.button1, toolTip);
                    }

                    chkBox.Reversed = clIni.GetBooleanValue(checkBoxName, "Reversed", false);

                    CheckBoxes.Add(chkBox);
                    if (pLocation.X < 435 && pLocation.Y < 236)
                    {
                        CheckBoxes[CheckBoxes.Count - 1].Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        this.gameOptionsPanel.Controls.Add(CheckBoxes[CheckBoxes.Count - 1]);
                    }
                    else
                    {
                        UserCheckBox nChkBox = CheckBoxes[CheckBoxes.Count - 1];
                        nChkBox.Anchor = AnchorStyles.None;
                        string anchor = clIni.GetStringValue(checkBoxName, "Anchors", "none");

                        if (anchor == "Bottom,Left")
                            CheckBoxes[CheckBoxes.Count - 1].Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                        else if (anchor == "Right")
                            CheckBoxes[CheckBoxes.Count - 1].Anchor = nChkBox.Anchor | AnchorStyles.Right;
                        else if (anchor == "Top,Left")
                            CheckBoxes[CheckBoxes.Count - 1].Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        else if (anchor == "Bottom")
                            CheckBoxes[CheckBoxes.Count - 1].Anchor = nChkBox.Anchor | AnchorStyles.Bottom;

                        this.Controls.Add(CheckBoxes[CheckBoxes.Count - 1]);
                    }

                    chkBox.Initialize();
                }
                else
                    throw new Exception("No data exists for CheckBox " + checkBoxName + "!");
            }

            Color comboBoxNondefaultColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetComboBoxNondefaultColor());

            if ((comboBoxNondefaultColor.R == 0 &&
                comboBoxNondefaultColor.G == 0 &&
                comboBoxNondefaultColor.B == 0) || isHost)
                comboBoxNondefaultColor = cAltUiColor;

            string[] comboBoxes = clIni.GetStringValue("GameLobby", "ComboBoxes", "none").Split(',');
            foreach (string comboBoxName in comboBoxes)
            {
                if (clIni.SectionExists(comboBoxName))
                {
                    LimitedComboBox cmbBox = SharedUILogic.SetUpComboBoxFromIni(comboBoxName, clIni, toolTip1,
                        cmbP1Name.Font, cAltUiColor, comboBoxNondefaultColor);
                    string sideErrorSetDescr = clIni.GetStringValue(comboBoxName, "SideErrorSetDescr", "none");
                    string associateSpawnIniOption = clIni.GetStringValue(comboBoxName, "AssociateSpawnIniOption", "none");

                    if (!isHost)
                    {
                        cmbBox.CanDropDown = false;
                    }

                    cmbBox.UseCustomDrawingCode = true;
                    cmbBox.SelectedIndexChanged += new EventHandler(GenericGameOptionChanged);

                    ComboBoxes.Add(cmbBox);
                    if (cmbBox.Location.X < 436 && cmbBox.Location.Y < 237)
                        this.gameOptionsPanel.Controls.Add(cmbBox);
                    else
                        this.Controls.Add(cmbBox);
                    AssociatedComboBoxSpawnIniOptions.Add(associateSpawnIniOption);
                    ComboBoxSidePrereqErrorDescriptions.Add(sideErrorSetDescr);
                    ComboBoxDataWriteModes.Add((DataWriteMode)cmbBox.Tag);
                }
                else
                    throw new Exception("No data exists for ComboBox " + comboBoxName + "!");
            }

            string sideOptionPrerequisites = clIni.GetStringValue("GameLobby", "SideOptionPrerequisites", "none");
            if (sideOptionPrerequisites != "none")
            {
                string[] sideOptionPrereqArray = sideOptionPrerequisites.Split(',');
                int numSides = sideOptionPrereqArray.Length / 3;

                for (int sId = 0; sId < numSides; sId++)
                {
                    string sideName = sideOptionPrereqArray[sId * 3];
                    string comboBoxName = sideOptionPrereqArray[sId * 3 + 1];
                    int requiredIndex = Convert.ToInt32(sideOptionPrereqArray[sId * 3 + 2]);

                    int sideId = GetSideIndexByName(sides, sideName);
                    int comboBoxId = ComboBoxes.FindIndex(c => c.Name == comboBoxName);

                    if (sideId == -1)
                        throw new Exception("Non-existent side name: " + sideName);
                    if (comboBoxId == -1)
                        throw new Exception("Non-existent ComboBox name: " + comboBoxName);

                    SideComboboxPrerequisites[sideId].SetData(comboBoxId, requiredIndex);
                }
            }

            string sideCheckboxPrerequisites = clIni.GetStringValue("GameLobby", "SideCheckboxPrerequisites", String.Empty);
            if (!String.IsNullOrEmpty(sideCheckboxPrerequisites))
            {
                string[] sideOptionPrereqArray = sideCheckboxPrerequisites.Split(',');
                int numSides = sideOptionPrereqArray.Length / 3;

                for (int sId = 0; sId < numSides; sId++)
                {
                    string sideName = sideOptionPrereqArray[sId * 3];
                    string checkBoxName = sideOptionPrereqArray[sId * 3 + 1];
                    bool requiredValue = Convert.ToBoolean(sideOptionPrereqArray[sId * 3 + 2]);

                    int sideIndex = GetSideIndexByName(sides, sideName);
                    int checkBoxIndex = CheckBoxes.FindIndex(cb => cb.Name == checkBoxName);

                    if (sideIndex == -1)
                        throw new InvalidDataException("Non-existent side name in SideCheckboxPrerequisites: " + sideName);
                    if (checkBoxIndex == -1)
                        throw new InvalidDataException("Non-existent check box name in SideCheckboxPrerequisites: " + checkBoxName);

                    SideCheckboxPrerequisites[sideIndex].SetData(checkBoxIndex, requiredValue);
                    CheckBoxes[checkBoxIndex].CheckedChanged += SidePrereqCheckBox_CheckedChanged;
                }
            }

            string[] labels = clIni.GetStringValue("GameLobby", "Labels", "none").Split(',');
            foreach (string labelName in labels)
            {
                if (clIni.SectionExists(labelName))
                {
                    string text = clIni.GetStringValue(labelName, "Text", "no text defined here!");
                    string[] location = clIni.GetStringValue(labelName, "Location", "0,0").Split(',');
                    Point pLocation = new Point(Convert.ToInt32(location[0]), Convert.ToInt32(location[1]));
                    string toolTip = clIni.GetStringValue(labelName, "ToolTip", String.Empty);

                    Label label = new Label();
                    label.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    label.AutoSize = true;
                    label.BackColor = Color.Transparent;
                    label.Location = pLocation;
                    label.Name = labelName;
                    label.Text = text;
                    label.ForeColor = cLabelColor;
                    label.Font = gameOptionFont;

                    if (!String.IsNullOrEmpty(toolTip))
                    {
                        toolTip1.SetToolTip(label, toolTip);
                    }

                    if (pLocation.X < 436 && pLocation.Y < 237)
                        this.gameOptionsPanel.Controls.Add(label);
                    else
                        this.Controls.Add(label);
                }
                else
                    throw new Exception("No data exists for label " + labelName + "!");
            }

            InitReadyBoxes(cLabelColor, cAltUiColor);

            SharedUILogic.SetControlColor(cLabelColor, cBackColor, cAltUiColor, cListBoxFocusColor, this);

            chkDisableSounds = new UserCheckBox(cLabelColor, cAltUiColor, "Disable sounds");
            chkDisableSounds.Location = new Point(325, 243);
            chkDisableSounds.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            chkDisableSounds.AutoSize = true;
            chkDisableSounds.Name = "chkDisableSounds";
            chkDisableSounds.Checked = !DomainController.Instance().GetMessageSoundStatus();
            this.Controls.Add(chkDisableSounds);

            currentMap = CnCNetData.MapList[0];
            currentGameMode = currentMap.GameModes[0];
            SetMapInfo(0);
            LoadPreview();

            if (isHost)
            {
                updatePlayers = false;
                CopyPlayerDataToUI();
                updatePlayers = true;

                // Set up the timer used for automatic refreshing of the game listing
                timer = new Timer();
                timer.Interval = 5000;
                timer.Tick += new EventHandler(UpdateGameListing);
                timer.Start();

                // Set up the timer used for automatically refreshing game options to other players
                gameOptionRefreshTimer = new Timer();
                gameOptionRefreshTimer.Interval = 5000;
                gameOptionRefreshTimer.Tick += new EventHandler(GenericGameOptionChanged);
                gameOptionRefreshTimer.Tick += new EventHandler(CopyPlayerDataFromUI);
            }
            else
            {
                btnChangeMap.Enabled = false;
                btnLockGame.Enabled = false;
                btnLaunchGame.Text = "I'm Ready";

                btnP1Kick.Enabled = false;
                btnP2Kick.Enabled = false;
                btnP3Kick.Enabled = false;
                btnP4Kick.Enabled = false;
                btnP5Kick.Enabled = false;
                btnP6Kick.Enabled = false;
                btnP7Kick.Enabled = false;
                btnP8Kick.Enabled = false;
            }

            if (mapInfoStyle == LabelInfoStyle.ALLCAPS)
                btnLaunchGame.Text = btnLaunchGame.Text.ToUpper();

            UpdateGameListing(null, EventArgs.Empty);

            resizeTimer = new System.Windows.Forms.Timer();
            resizeTimer.Interval = 500;
            resizeTimer.Tick += new EventHandler(resizeTimer_Tick);

            SharedUILogic.ParseClientThemeIni(this);

            int sideWidth = DomainController.Instance().GetSideComboboxWidth();
            playerPanel.Width += sideWidth - 92;
            cmbP1Side.Width = sideWidth;
            cmbP2Side.Width = sideWidth;
            cmbP3Side.Width = sideWidth;
            cmbP4Side.Width = sideWidth;
            cmbP5Side.Width = sideWidth;
            cmbP6Side.Width = sideWidth;
            cmbP7Side.Width = sideWidth;
            cmbP8Side.Width = sideWidth;

            pSideLabels = new TextBox[8];
            for (int labelId = 0; labelId < pSideLabels.Length; labelId++)
            {
                pSideLabels[labelId] = new TextBox();
                TextBox forcedSideBox = pSideLabels[labelId];
                forcedSideBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                forcedSideBox.BackColor = cBackColor;
                forcedSideBox.BorderStyle = BorderStyle.FixedSingle;
                forcedSideBox.ForeColor = cLabelColor;
                Point sLocation = getPlayerSideCMBFromId(labelId + 1).Location;
                forcedSideBox.Location = sLocation;
                forcedSideBox.Size = getPlayerSideCMBFromId(labelId + 1).Size;
                playerPanel.Controls.Add(forcedSideBox);
                forcedSideBox.Visible = false;
                forcedSideBox.GotFocus += playerNameTextBox_GotFocus;
            }

            string[] windowSize = DomainController.Instance().GetWindowSizeCnCNet().Split('x');
            int sizeX = Convert.ToInt32(windowSize[0]);
            if (sizeX > Screen.PrimaryScreen.Bounds.Width)
                sizeX = Screen.PrimaryScreen.Bounds.Width;
            int sizeY = Convert.ToInt32(windowSize[1]);
            if (sizeY > Screen.PrimaryScreen.Bounds.Height - 40)
                sizeY = Screen.PrimaryScreen.Bounds.Height - 40;

            string[] minimumWindowSize = DomainController.Instance().GetMinimumWindowSizeCnCNet().Split('x');
            this.MinimumSize = new Size(Int32.Parse(minimumWindowSize[0]), Int32.Parse(minimumWindowSize[1]));

            this.ClientSize = new Size(sizeX, sizeY);
            this.Location = new Point((Screen.PrimaryScreen.Bounds.Width - this.Size.Width) / 2,
                (Screen.PrimaryScreen.Bounds.Height - this.Size.Height) / 2);

            this.WindowState = DomainController.Instance().GetGameLobbyWindowState();

            tbGameLobbyChatInput.Select();

            if (isHost)
            {
                Logger.Log("Starting listener.");

                Thread thread = new Thread(new ThreadStart(ListenForClients));
                thread.Start();
            }

            serverClient = new TcpClient();
            try
            {
                serverClient.Connect(serverAddress, ProgramConstants.LAN_PORT);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connecting to the game host failed! " + ex.Message,
                    "Connecting failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnLeaveGame.PerformClick();
                return;
            }
            Thread communicationThread = new Thread(new ThreadStart(HandleServerConnection));
            communicationThread.Start();
        }

        void HandleServerConnection()
        {
            SendToServer("NAME " + ProgramConstants.PLAYERNAME);
            SendToServer("VERSION " + ProgramConstants.GAME_VERSION);
            SendToServer("FHASH " + Anticheat.Instance.GetCompleteHash());

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
                Lobby.AddNotice("You've been disconnected from the server.");
                btnLeaveGame.PerformClick();
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
                else if (cmd == "GETREADY")
                {
                    WindowFlasher.FlashWindowEx(this);
                    AddNotice("The game host wants to start the game but cannot because not all players are ready!");
                }
                else if (cmd.StartsWith("POPTS "))
                {
                    RefreshPlayersFromMessage(cmd.Substring(6));
                }
                else if (cmd == "LOCK")
                {
                    AddNotice("The game room has been locked.");
                }
                else if (cmd == "UNLOCK")
                {
                    AddNotice("The game room has been unlocked.");
                }
                else if (cmd == "SAMECOLOR")
                {
                    AddNotice("Multiple human players cannot share the same color.");
                }
                else if (cmd == "AISPECS")
                {
                    AddNotice("Why would you want an AI player to spectate your match?");
                }
                else if (cmd == "COOPSPECS")
                {
                    AddNotice("Co-Op Missions cannot be spectated. You'll have to show a bit more effort to cheat here.");
                }
                else if (cmd == "SAMESTARTLOC")
                {
                    AddNotice("Multiple human players cannot share the same starting location on this map.");
                }
                else if (cmd.StartsWith("NOVERIFY "))
                {
                    AddNotice("Cannot start game: player " + cmd.Substring(9) + " hasn't been verified.");
                }
                else if (cmd.StartsWith("INGAME "))
                {
                    AddNotice("Cannot start game: player " + cmd.Substring(7) + " is still playing the game that you started previously.");
                }
                else if (cmd == "TMPLAYERS")
                {
                    AddNotice("You have too many players for this map.");
                }
                else if (cmd == "INFSPLAYERS")
                {
                    AddNotice("You need more players for playing this map.");
                }
                else if (cmd.StartsWith("INVSTART "))
                {
                    AddNotice("Cannot start game: player " + cmd.Substring(9) + " has an invalid starting location.");
                }
                else if (cmd.StartsWith("INVAISTART "))
                {
                    AddNotice("Cannot start game: AI player " + cmd.Substring(11) + " has an invalid starting location.");
                }
                else if (cmd.StartsWith("GOPTS "))
                {
                    RefreshGameOptionsFromMessage(cmd.Substring(6));
                }
                else if (cmd.StartsWith("START "))
                {
                    int gameId = Int32.Parse(cmd.Substring(6));

                    Players[0].IPAddress = serverAddress;

                    StartGame(gameId);
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
                else if (cmd.StartsWith("KICK "))
                {
                    AddNotice(cmd.Substring(5) + " has been kicked from the game.");
                }
            }
        }

        void RefreshPlayersFromMessage(string message)
        {
            if (this.InvokeRequired)
            {
                StringCallback d = new StringCallback(RefreshPlayersFromMessage);
                BeginInvoke(d, message);
                return;
            }

            Players.Clear();
            AIPlayers.Clear();

            string[] parts = message.Split(';');

            int pCount = parts.Length / 8;

            for (int i = 0; i < pCount; i++)
            {
                int startIndex = i * 8;

                string name = parts[startIndex];
                int side = Int32.Parse(parts[startIndex + 1]);
                int color = Int32.Parse(parts[startIndex + 2]);
                int sloc = Int32.Parse(parts[startIndex + 3]);
                int team = Int32.Parse(parts[startIndex + 4]);
                string ipAddress = parts[startIndex + 5];
                bool ready = Convert.ToBoolean(Int32.Parse(parts[startIndex + 6]));
                bool isAI = Convert.ToBoolean(Int32.Parse(parts[startIndex + 7]));

                PlayerInfo pInfo = new PlayerInfo()
                {
                    Name = name,
                    SideId = side,
                    ColorId = color,
                    StartingLocation = sloc,
                    TeamId = team,
                    IPAddress = ipAddress,
                    Ready = ready,
                    IsAI = isAI
                };

                if (isAI)
                    AIPlayers.Add(pInfo);
                else
                    Players.Add(pInfo);
            }

            CopyPlayerDataToUI();
        }

        void RefreshGameOptionsFromMessage(string msg)
        {
            if (this.InvokeRequired)
            {
                StringCallback d = new StringCallback(RefreshGameOptionsFromMessage);
                BeginInvoke(d, msg);
                return;
            }

            string[] parts = msg.Split(';');

            if (parts.Length != CheckBoxes.Count + ComboBoxes.Count + 3)
            {
                AddNotice("The game host has sent an invalid game options message. They could be running a different game version!");
                return;
            }

            updateGameOptions = false;

            for (int i = 0; i < CheckBoxes.Count; i++)
            {
                bool isChecked = Convert.ToBoolean(Int32.Parse(parts[i]));

                CheckBoxes[i].Checked = isChecked;
            }

            for (int i = 0; i < ComboBoxes.Count; i++)
            {
                int index = Int32.Parse(parts[i + CheckBoxes.Count]);

                ComboBoxes[i].SelectedIndex = index;
            }

            currentSHA1 = parts[CheckBoxes.Count + ComboBoxes.Count];
            currentGameMode = parts[CheckBoxes.Count + ComboBoxes.Count + 1];

            int mapIndex = GetMapIndexFromSHA1(currentSHA1);
            Map map = null;

            if (mapIndex == -1)
            {
                SendToServer("INVMAP");
                AddNotice("The game host has selected a map that doesn't exist on your system. The host needs to change the map " +
                    "or you'll be unable to participare in this match.");
            }
            else
                map = CnCNetData.MapList[mapIndex];

            currentMap = map;

            SetMapInfo(mapIndex);

            LoadPreview();

            LockOptions();

            Seed = Int32.Parse(parts[CheckBoxes.Count + ComboBoxes.Count + 2]);

            updateGameOptions = true;
        }

        void AddChatMessage(int color, string message, string sender)
        {
            if (lbGameLobbyChat.InvokeRequired)
            {
                ChatDelegate d = new ChatDelegate(AddChatMessage);
                BeginInvoke(d, color, message, sender);
                return;
            }

            MessageColors.Add(ChatColors[color]);
            lbGameLobbyChat.Items.Add("[" + DateTime.Now.ToShortTimeString() + "] " + sender + ": " + message);
        }

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
                AddNotice("New client connected from " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());

                if (Clients.Count >= playerLimit)
                {
                    Logger.Log("Dropping client because of player limit.");
                    AddNotice("Dropping client because of player limit.");
                    client.Client.Disconnect(false);
                    client.Close();
                    continue;
                }

                if (Locked)
                {
                    Logger.Log("Dropping client because the game room is locked.");
                    AddNotice("Dropping client because the game room is locked.");
                    client.Client.Disconnect(false);
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
                            lock (locker)
                            {
                                if (Clients.FindIndex(l => l.Name == cmd.Substring(5)) > -1)
                                {
                                    RemovePlayer(lp);
                                    return;
                                }
                            }

                            lp.Name = cmd.Substring(5);
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
                            RefreshPlayers();
                        }
                        else if (cmd.StartsWith("OPTS "))
                        {
                            string[] parts = cmd.Substring(5).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length != 4)
                                continue;

                            int sideId = 0;
                            int colorId = 0;
                            int startId = 0;
                            int teamId = 0;

                            bool success = Int32.TryParse(parts[0], out sideId);
                            if (!success)
                                continue;
                            success = Int32.TryParse(parts[1], out colorId);
                            if (!success)
                                continue;
                            success = Int32.TryParse(parts[2], out startId);
                            if (!success)
                                continue;
                            success = Int32.TryParse(parts[3], out teamId);
                            if (!success)
                                continue;

                            lp.Side = sideId;
                            lp.Color = colorId;
                            lp.Start = startId;
                            lp.Team = teamId;

                            lock (locker)
                            {
                                foreach (LANPlayer p in Clients)
                                    p.Ready = false;
                            }

                            RefreshPlayers();
                        }
                        else if (cmd == "RETURN")
                        {
                            lp.IsInGame = false;
                        }
                    }
                }
            }

            if (leaving)
                return;

            RemovePlayer(lp);
            if (!lp.Verified && !String.IsNullOrEmpty(lp.Name))
                Broadcast("QUIT " + lp.Name);
        }

        private void AddPlayer(LANPlayer lp)
        {
            Logger.Log("Adding player.");

            lock (locker)
            {
                Clients.Add(lp);
            }

            Broadcast("JOIN " + lp.Name);
            RefreshPlayers();
        }

        private void RemovePlayer(LANPlayer lp)
        {
            Logger.Log("Removing player.");

            if (lp.Client == null)
                return;

            lock (locker)
            {
                lp.Client.Close();
                Clients.Remove(lp);
            }

            RefreshPlayers();
        }

        void RefreshPlayers()
        {
            lock (locker)
            {
                Players.Clear();

                foreach (LANPlayer lp in Clients)
                {
                    PlayerInfo pInfo = new PlayerInfo(lp.Name, lp.Side, lp.Start, lp.Color, lp.Team);
                    pInfo.IPAddress = lp.Address.ToString();
                    pInfo.Ready = lp.Ready;
                    Players.Add(pInfo);
                }

                CopyPlayerDataToUI();
                CopyPlayerDataFromUI(null, EventArgs.Empty);
            }
        }


        void SidePrereqCheckBox_CheckedChanged(object sender)
        {
            if (isHost)
                CopyPlayerDataFromUI(sender, EventArgs.Empty);
        }

        private void lbChatBox_MouseWheel(object sender, MouseEventArgs e)
        {
            sbGameLobbyChat.Value += e.Delta / -40;
            customScrollbar1_Scroll(sender, EventArgs.Empty);
        }

        private void customScrollbar1_Scroll(object sender, EventArgs e)
        {
            lbGameLobbyChat.TopIndex = sbGameLobbyChat.Value;
        }

        private void resizeTimer_Tick(object sender, EventArgs e)
        {
            LoadPreview();
            resizeTimer.Stop();
        }

        private void SendToServer(string message)
        {
            if (serverClient == null || !serverClient.Connected)
                return;

            Logger.Log("SM: " + message);

            byte[] buffer = Encoding.GetEncoding(1252).GetBytes(message + "^");
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

        private void Broadcast(string message)
        {
            Logger.Log("BC: " + message);

            lock (locker)
            {
                foreach (LANPlayer lp in Clients)
                {
                    lp.SendMessage(message);
                }
            }
        }

        /// <summary>
        /// Gets a map from the internal map list based on the map's SHA1.
        /// </summary>
        /// <param name="sha1">The MD5 of the map to search for.</param>
        /// <returns>The map if a matching MD5 was found, otherwise null.</returns>
        private Map getMapFromSHA1(string sha1)
        {
            foreach (Map map in CnCNetData.MapList)
            {
                if (map.SHA1 == sha1)
                    return map;
            }

            return null;
        }

        int GetMapIndexFromSHA1(string sha1)
        {
            int index = CnCNetData.MapList.FindIndex(m => m.SHA1 == sha1);

            return index;
        }

        /// <summary>
        /// Adds a white notice message into the chat list box.
        /// </summary>
        /// <param name="message">The message to add.</param>
        private void AddNotice(string message)
        {
            AddNotice(message, Color.White);
        }

        /// <summary>
        /// Adds a notice message into the chat list box.
        /// </summary>
        /// <param name="noticeColor">The color of the message.</param>
        /// <param name="message">The message to add.</param>
        private void AddNotice(string message, Color noticeColor)
        {
            if (lbGameLobbyChat.InvokeRequired)
            {
                NoticeDelegate d = new NoticeDelegate(AddNotice);
                BeginInvoke(d, message, noticeColor);
                return;
            }

            MessageColors.Add(noticeColor);
            lbGameLobbyChat.Items.Add(message);
            ScrollListbox(message);
        }

        /// <summary>
        /// Refreshes this game room's listing on CnCNet.
        /// </summary>
        private void UpdateGameListing(object sender, EventArgs e)
        {
            if (!isHost)
                return;

            StringBuilder sb;
            
            sb = new StringBuilder("GAME ");

            sb.Append(ProgramConstants.LAN_PROTOCOL_REVISION + ";");
            sb.Append(ProgramConstants.GAME_VERSION);
            sb.Append(";");
            sb.Append(playerLimit);
            sb.Append(";");
            sb.Append(GameRoomName);
            sb.Append(";");
            if (Locked)
                sb.Append("1");
            else
                sb.Append("0");
            if (leaving)
                sb.Append("1");
            else
                sb.Append("0");
            sb.Append("0"); // isLoadedGame
            sb.Append(";");
            foreach (PlayerInfo player in Players)
            {
                sb.Append(player.Name);
                sb.Append(",");
            }
            sb.Append(";");
            if (currentMap != null)
                sb.Append(currentMap.Name);
            else
                sb.Append("None");
            sb.Append(";");
            sb.Append(currentGameMode);
            sb.Append(";");
            sb.Append("1"); // loadedGameId
            sb.Append(";");
            sb.Append(defaultGame);
            sb.Append(";");

            Lobby.SendMessage(sb.ToString());

            if (!leaving)
                timer.Enabled = true;
        }

        private int GetSideIndexByName(string[] sides, string sideName)
        {
            for (int sId = 0; sId < sides.Length; sId++)
            {
                if (sideName == sides[sId])
                    return sId;
            }

            return -1;
        }

        private void InitKickButtons()
        {
            imgKick = SharedUILogic.LoadImage("kick.png");
            imgKick_c = SharedUILogic.LoadImage("kick_c.png");
            imgBan = SharedUILogic.LoadImage("ban.png");
            imgBan_c = SharedUILogic.LoadImage("ban_c.png");

            btnP1Kick.DefaultImage = imgKick;
            btnP1Kick.HoveredImage = imgKick_c;
            btnP1Kick.HoverSound = sndButtonSound;
            btnP2Kick.DefaultImage = imgKick;
            btnP2Kick.HoveredImage = imgKick_c;
            btnP2Kick.HoverSound = sndButtonSound;
            btnP3Kick.DefaultImage = imgKick;
            btnP3Kick.HoveredImage = imgKick_c;
            btnP3Kick.HoverSound = sndButtonSound;
            btnP4Kick.DefaultImage = imgKick;
            btnP4Kick.HoveredImage = imgKick_c;
            btnP4Kick.HoverSound = sndButtonSound;
            btnP5Kick.DefaultImage = imgKick;
            btnP5Kick.HoveredImage = imgKick_c;
            btnP5Kick.HoverSound = sndButtonSound;
            btnP6Kick.DefaultImage = imgKick;
            btnP6Kick.HoveredImage = imgKick_c;
            btnP6Kick.HoverSound = sndButtonSound;
            btnP7Kick.DefaultImage = imgKick;
            btnP7Kick.HoveredImage = imgKick_c;
            btnP7Kick.HoverSound = sndButtonSound;
            btnP8Kick.DefaultImage = imgKick;
            btnP8Kick.HoveredImage = imgKick_c;
            btnP8Kick.HoverSound = sndButtonSound;
        }

        /// <summary>
        /// Creates new player ready boxes and adds them to the player options panel.
        /// </summary>
        /// <param name="cLabelColor">Color of labels in the UI.</param>
        /// <param name="cAltUiColor">Color of highlighted items in the UI.</param>
        private void InitReadyBoxes(Color cLabelColor, Color cAltUiColor)
        {
            int x = DomainController.Instance().GetReadyBoxXCoordinate();

            string readyBoxText = DomainController.Instance().GetReadyBoxText();

            chkP1Ready = new UserCheckBox(cLabelColor, cAltUiColor, readyBoxText);
            chkP1Ready.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkP1Ready.AutoSize = true;
            chkP1Ready.Location = new Point(x, 26);
            chkP1Ready.Name = "chkP1Ready";
            playerPanel.Controls.Add(chkP1Ready);

            chkP2Ready = new UserCheckBox(cLabelColor, cAltUiColor, readyBoxText);
            chkP2Ready.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkP2Ready.AutoSize = true;
            chkP2Ready.Location = new Point(x, 52);
            chkP2Ready.Name = "chkP2Ready";
            playerPanel.Controls.Add(chkP2Ready);

            chkP3Ready = new UserCheckBox(cLabelColor, cAltUiColor, readyBoxText);
            chkP3Ready.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkP3Ready.AutoSize = true;
            chkP3Ready.Location = new Point(x, 78);
            chkP3Ready.Name = "chkP3Ready";
            playerPanel.Controls.Add(chkP3Ready);

            chkP4Ready = new UserCheckBox(cLabelColor, cAltUiColor, readyBoxText);
            chkP4Ready.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkP4Ready.AutoSize = true;
            chkP4Ready.Location = new Point(x, 105);
            chkP4Ready.Name = "chkP4Ready";
            playerPanel.Controls.Add(chkP4Ready);

            chkP5Ready = new UserCheckBox(cLabelColor, cAltUiColor, readyBoxText);
            chkP5Ready.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkP5Ready.AutoSize = true;
            chkP5Ready.Location = new Point(x, 131);
            chkP5Ready.Name = "chkP5Ready";
            playerPanel.Controls.Add(chkP5Ready);

            chkP6Ready = new UserCheckBox(cLabelColor, cAltUiColor, readyBoxText);
            chkP6Ready.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkP6Ready.AutoSize = true;
            chkP6Ready.Location = new Point(x, 157);
            chkP6Ready.Name = "chkP6Ready";
            playerPanel.Controls.Add(chkP6Ready);

            chkP7Ready = new UserCheckBox(cLabelColor, cAltUiColor, readyBoxText);
            chkP7Ready.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkP7Ready.AutoSize = true;
            chkP7Ready.Location = new Point(x, 183);
            chkP7Ready.Name = "chkP7Ready";
            playerPanel.Controls.Add(chkP7Ready);

            chkP8Ready = new UserCheckBox(cLabelColor, cAltUiColor, readyBoxText);
            chkP8Ready.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            chkP8Ready.AutoSize = true;
            chkP8Ready.Location = new Point(x, 209);
            chkP8Ready.Name = "chkP8Ready";
            playerPanel.Controls.Add(chkP8Ready);
        }

        /// <summary>
        /// Generic function for customized drawing of items in player color combo boxes.
        /// </summary>
        private void cmbPXColor_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1 && e.Index < cmbP1Color.Items.Count)
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

                e.Graphics.DrawString(cmbP1Color.Items[e.Index].ToString(), e.Font, new SolidBrush(MPColors[e.Index]), e.Bounds);
            }
        }

        /// <summary>
        /// Returns a player's name combo box based on the player's index.
        /// </summary>
        /// <param name="id">The index of the player, starting from 1.</param>
        private LimitedComboBox getPlayerNameCMBFromId(int id)
        {
            switch (id)
            {
                case 1:
                    return cmbP1Name;
                case 2:
                    return cmbP2Name;
                case 3:
                    return cmbP3Name;
                case 4:
                    return cmbP4Name;
                case 5:
                    return cmbP5Name;
                case 6:
                    return cmbP6Name;
                case 7:
                    return cmbP7Name;
                case 8:
                    return cmbP8Name;
            }

            return null;
        }

        /// <summary>
        /// Returns a player's side combo box based on the player's index.
        /// </summary>
        /// <param name="id">The index of the player, starting from 1.</param>
        private LimitedComboBox getPlayerSideCMBFromId(int id)
        {
            switch (id)
            {
                case 1:
                    return cmbP1Side;
                case 2:
                    return cmbP2Side;
                case 3:
                    return cmbP3Side;
                case 4:
                    return cmbP4Side;
                case 5:
                    return cmbP5Side;
                case 6:
                    return cmbP6Side;
                case 7:
                    return cmbP7Side;
                case 8:
                    return cmbP8Side;
            }

            return null;
        }

        /// <summary>
        /// Returns a player's color combo box based on the player's index.
        /// </summary>
        /// <param name="id">The index of the player, starting from 1.</param>
        private LimitedComboBox getPlayerColorCMBFromId(int id)
        {
            switch (id)
            {
                case 1:
                    return cmbP1Color;
                case 2:
                    return cmbP2Color;
                case 3:
                    return cmbP3Color;
                case 4:
                    return cmbP4Color;
                case 5:
                    return cmbP5Color;
                case 6:
                    return cmbP6Color;
                case 7:
                    return cmbP7Color;
                case 8:
                    return cmbP8Color;
            }

            return null;
        }

        /// <summary>
        /// Returns a player's starting location combo box based on the player's index.
        /// </summary>
        /// <param name="id">The index of the player, starting from 1.</param>
        private LimitedComboBox getPlayerStartCMBFromId(int id)
        {
            switch (id)
            {
                case 1:
                    return cmbP1Start;
                case 2:
                    return cmbP2Start;
                case 3:
                    return cmbP3Start;
                case 4:
                    return cmbP4Start;
                case 5:
                    return cmbP5Start;
                case 6:
                    return cmbP6Start;
                case 7:
                    return cmbP7Start;
                case 8:
                    return cmbP8Start;
            }

            return null;
        }

        /// <summary>
        /// Returns a player's team combo box based on the player's index.
        /// </summary>
        /// <param name="id">The index of the player, starting from 1.</param>
        private LimitedComboBox getPlayerTeamCMBFromId(int id)
        {
            switch (id)
            {
                case 1:
                    return cmbP1Team;
                case 2:
                    return cmbP2Team;
                case 3:
                    return cmbP3Team;
                case 4:
                    return cmbP4Team;
                case 5:
                    return cmbP5Team;
                case 6:
                    return cmbP6Team;
                case 7:
                    return cmbP7Team;
                case 8:
                    return cmbP8Team;
            }

            return null;
        }

        /// <summary>
        /// Returns a player's ready check box based on the player's index.
        /// </summary>
        /// <param name="id">The index of the player, starting from 1.</param>
        private UserCheckBox getPlayerReadyBoxFromId(int id)
        {
            switch (id)
            {
                case 1:
                    return chkP1Ready;
                case 2:
                    return chkP2Ready;
                case 3:
                    return chkP3Ready;
                case 4:
                    return chkP4Ready;
                case 5:
                    return chkP5Ready;
                case 6:
                    return chkP6Ready;
                case 7:
                    return chkP7Ready;
                case 8:
                    return chkP8Ready;
            }

            return null;
        }

        /// <summary>
        /// Leaves the game room.
        /// </summary>
        private void btnLeaveGame_Click(object sender, EventArgs e)
        {
            leaving = true;
            if (isHost)
            {
                listener.Stop();
                foreach (LANPlayer lp in Clients)
                {
                    if (lp.Client != null && lp.Client.Connected)
                        lp.Client.Close();
                }
                UpdateGameListing(null, EventArgs.Empty);
                timer.Stop();
                timer.Dispose();
            }
            else
                SendToServer("QUIT");

            try
            {
                serverClient.Close();
            }
            catch
            { }

            CnCNetData.IsGameLobbyOpen = false;
            isManualClose = true;
            UpdateGameListing(btnLeaveGame, EventArgs.Empty);
            DomainController.Instance().SaveGameLobbySettings(!chkDisableSounds.Checked, this.WindowState);
            Unsubscribe();
            this.Close();
            CnCNetData.DoGameLobbyClosed();
        }

        /// <summary>
        /// Copies the player data in the UI to the internal player data in memory.
        /// Also broadcasts player options to all players as host and requests option changes
        /// as a non-host player.
        /// </summary>
        private void CopyPlayerDataFromUI(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                EventCallback d = new EventCallback(CopyPlayerDataFromUI);
                BeginInvoke(d, sender, e);
                return;
            }

            if (!updatePlayers)
                return;

            if (!isHost)
            {
                int index = Players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME);
                if (index > -1)
                {
                    int sideId = getPlayerSideCMBFromId(index + 1).SelectedIndex;

                    if (!isSideAllowed(sideId))
                    {
                        sideId = Players[index].SideId;
                    }

                    int colorId = getPlayerColorCMBFromId(index + 1).SelectedIndex;
                    int startId = getPlayerStartCMBFromId(index + 1).SelectedIndex;
                    int teamId = getPlayerTeamCMBFromId(index + 1).SelectedIndex;

                    StringBuilder sbc = new StringBuilder("OPTS ");
                    sbc.Append(sideId);
                    sbc.Append(";");
                    sbc.Append(colorId);
                    sbc.Append(";");
                    sbc.Append(startId);
                    sbc.Append(";");
                    sbc.Append(teamId);
                    SendToServer(sbc.ToString());
                }

                CopyPlayerDataToUI();
                return;
            }

            lock (locker)
            {
                if (Clients.Count < 1)
                    return;

                LANPlayer lp = Clients[0];
                lp.Color = cmbP1Color.SelectedIndex;
                int sideId = cmbP1Side.SelectedIndex;

                if (!isSideAllowed(sideId))
                    sideId = lp.Side;

                lp.Side = sideId;
                lp.Start = cmbP1Start.SelectedIndex;
                lp.Team = cmbP1Team.SelectedIndex;
            }

            StringBuilder sb = new StringBuilder("POPTS ");

            // Human players
            for (int pId = 0; pId < Players.Count; pId++)
            {
                Players[pId].Name = getPlayerNameCMBFromId(pId + 1).Text;
                getPlayerNameCMBFromId(pId + 1).CanDropDown = false;
                Players[pId].ColorId = getPlayerColorCMBFromId(pId + 1).SelectedIndex;
                int sideId = getPlayerSideCMBFromId(pId + 1).SelectedIndex;

                if (sideId < 0 || !isSideAllowed(sideId))
                    Players[pId].SideId = 0;
                else
                    Players[pId].SideId = sideId;
                Players[pId].StartingLocation = getPlayerStartCMBFromId(pId + 1).SelectedIndex;
                Players[pId].TeamId = getPlayerTeamCMBFromId(pId + 1).SelectedIndex;

                sb.Append(Players[pId].Name);
                sb.Append(";");
                sb.Append(Players[pId].SideId);
                sb.Append(";");
                sb.Append(Players[pId].ColorId);
                sb.Append(";");
                sb.Append(Players[pId].StartingLocation);
                sb.Append(";");
                sb.Append(Players[pId].TeamId);
                sb.Append(";");
                sb.Append(Players[pId].IPAddress);
                sb.Append(";");
                sb.Append(Convert.ToInt32(Players[pId].Ready));
                sb.Append(";");
                sb.Append("0");
                sb.Append(";");
            }

            // AI players
            AIPlayers.Clear();
            int playerCount = Players.Count;
            for (int cmbId = Players.Count; cmbId < 8; cmbId++)
            {
                LimitedComboBox cmb = getPlayerNameCMBFromId(cmbId + 1);

                if (cmb.SelectedIndex < 1)
                    continue;

                int sideId = getPlayerSideCMBFromId(cmbId + 1).SelectedIndex;
                AIPlayers.Add(new PlayerInfo(cmb.Text,
                sideId,
                getPlayerStartCMBFromId(cmbId + 1).SelectedIndex,
                getPlayerColorCMBFromId(cmbId + 1).SelectedIndex,
                getPlayerTeamCMBFromId(cmbId + 1).SelectedIndex));

                int aiIndex = AIPlayers.Count - 1;
                PlayerInfo aiPlayer = AIPlayers[aiIndex];

                if (sideId < 0 || !isSideAllowed(sideId))
                    aiPlayer.SideId = 0;
                else
                    aiPlayer.SideId = sideId;

                if (aiPlayer.SideId == -1)
                    aiPlayer.SideId = 0;
                if (aiPlayer.StartingLocation == -1)
                    aiPlayer.StartingLocation = 0;
                if (aiPlayer.ColorId == -1)
                    aiPlayer.ColorId = 0;
                if (aiPlayer.TeamId == -1)
                    aiPlayer.TeamId = 0;

                sb.Append(aiPlayer.Name);
                sb.Append(";");
                sb.Append(aiPlayer.SideId);
                sb.Append(";");
                sb.Append(aiPlayer.ColorId);
                sb.Append(";");
                sb.Append(aiPlayer.StartingLocation);
                sb.Append(";");
                sb.Append(aiPlayer.TeamId);
                sb.Append(";");
                sb.Append("0.0.0.0");
                sb.Append(";");
                sb.Append("1");
                sb.Append(";");
                sb.Append("1");
                sb.Append(";");
            }

            Broadcast(sb.ToString());

            CopyPlayerDataToUI();
        }

        /// <summary>
        /// Checks if a side is allowed to be used.
        /// </summary>
        /// <param name="sideId">The index of the side in the side list combobox.
        /// Side indexes start from 1 (0 = random).</param>
        private bool isSideAllowed(int sideId)
        {
            if (sideId != 0 && sideId != SideComboboxPrerequisites.Count + 1)
            {
                SideComboboxPrerequisite prereq = SideComboboxPrerequisites[sideId - 1];
                if (prereq.Valid)
                {
                    int comboBoxId = prereq.ComboBoxId;
                    int requiredIndexId = prereq.RequiredIndexId;

                    if (ComboBoxes[comboBoxId].SelectedIndex != requiredIndexId)
                    {
                        AddNotice(ComboBoxSidePrereqErrorDescriptions[comboBoxId] + " must be set for playing as " +
                            cmbP1Side.Items[sideId].ToString() + " to be allowed.");
                        return false;
                    }
                }

                SideCheckboxPrerequisite chkPrereq = SideCheckboxPrerequisites[sideId - 1];
                if (chkPrereq.Valid)
                {
                    int checkBoxIndex = chkPrereq.CheckBoxIndex;
                    bool requiredValue = chkPrereq.RequiredValue;

                    string sideName = cmbP1Side.Items[sideId].ToString();
                    string labelText = CheckBoxes[checkBoxIndex].LabelText;

                    if (CheckBoxes[checkBoxIndex].Checked != requiredValue)
                    {
                        if (requiredValue)
                            AddNotice(labelText + " must be set for playing as " + sideName + " to be allowed.");
                        else
                            AddNotice(labelText + " must be disabled for playing as " + sideName + " to be allowed.");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Applies all player data stored in memory to the user-interface.
        /// </summary>
        private void CopyPlayerDataToUI()
        {
            if (this.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(CopyPlayerDataToUI);
                BeginInvoke(d, null);
                return;
            }

            updatePlayers = false;

            // Clear dynamic preview display
            for (int id = 0; id < 8; id++)
            {
                PlayerColorsOnPlayerLocations[id].Clear();
                PlayerNamesOnPlayerLocations[id].Clear();
            }

            // Human players
            for (int pId = 0; pId < Players.Count; pId++)
            {
                LimitedComboBox lcb = getPlayerNameCMBFromId(pId + 1);
                lcb.DropDownStyle = ComboBoxStyle.DropDown;
                lcb.Text = Players[pId].Name;
                lcb.CanDropDown = false;
                lcb.Enabled = false;
                lcb.Visible = false;
                pNameTextBoxes[pId].Visible = true;
                pNameTextBoxes[pId].Enabled = true;
                pNameTextBoxes[pId].Text = Players[pId].Name;
                LimitedComboBox sideBox = getPlayerSideCMBFromId(pId + 1);
                sideBox.SelectedIndex = Players[pId].SideId;
                sideBox.Enabled = true;
                LimitedComboBox colorBox = getPlayerColorCMBFromId(pId + 1);
                colorBox.SelectedIndex = Players[pId].ColorId;
                colorBox.Enabled = true;
                LimitedComboBox startBox = getPlayerStartCMBFromId(pId + 1);
                startBox.SelectedIndex = Players[pId].StartingLocation;
                if (startBox.SelectedIndex > 0)
                {
                    // Add info to the dynamic preview display
                    if (Players[pId].TeamId == 0)
                        PlayerNamesOnPlayerLocations[startBox.SelectedIndex - 1].Add(Players[pId].Name);
                    else
                        PlayerNamesOnPlayerLocations[startBox.SelectedIndex - 1].Add(TeamIdentifiers[Players[pId].TeamId - 1] + Players[pId].Name);

                    PlayerColorsOnPlayerLocations[startBox.SelectedIndex - 1].Add(Players[pId].ColorId);
                }
                startBox.Enabled = true;
                LimitedComboBox teamBox = getPlayerTeamCMBFromId(pId + 1);
                teamBox.SelectedIndex = Players[pId].TeamId;
                teamBox.Enabled = true;
                UserCheckBox readyBox = getPlayerReadyBoxFromId(pId + 1);
                readyBox.Checked = Players[pId].Ready;
                readyBox.Enabled = true;
                readyBox.IsEnabled = false;

                if (!isHost && lcb.Text != ProgramConstants.PLAYERNAME)
                {
                    sideBox.CanDropDown = false;
                    colorBox.CanDropDown = false;
                    startBox.CanDropDown = false;
                    teamBox.CanDropDown = false;
                }
                else
                {
                    sideBox.CanDropDown = true;
                    colorBox.CanDropDown = true;
                    startBox.CanDropDown = true;
                    teamBox.CanDropDown = true;
                }
            }

            // AI players
            int playerCount = Players.Count;
            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                int index = playerCount + aiId + 1;
                LimitedComboBox lcb = getPlayerNameCMBFromId(index);
                lcb.Text = AIPlayers[aiId].Name;
                lcb.Enabled = true;
                lcb.DropDownStyle = ComboBoxStyle.DropDownList;
                lcb.Visible = true;
                pNameTextBoxes[index - 1].Visible = false;
                pNameTextBoxes[index - 1].Enabled = false;
                LimitedComboBox sideBox = getPlayerSideCMBFromId(index);
                sideBox.SelectedIndex = AIPlayers[aiId].SideId;
                sideBox.Enabled = true;
                LimitedComboBox colorBox = getPlayerColorCMBFromId(index);
                colorBox.SelectedIndex = AIPlayers[aiId].ColorId;
                colorBox.Enabled = true;
                LimitedComboBox startBox = getPlayerStartCMBFromId(index);
                startBox.SelectedIndex = AIPlayers[aiId].StartingLocation;
                startBox.Enabled = true;
                if (startBox.SelectedIndex > 0)
                {
                    // Add info to the dynamic preview display
                    if (AIPlayers[aiId].TeamId == 0)
                        PlayerNamesOnPlayerLocations[startBox.SelectedIndex - 1].Add(AIPlayers[aiId].Name);
                    else
                        PlayerNamesOnPlayerLocations[startBox.SelectedIndex - 1].Add(TeamIdentifiers[AIPlayers[aiId].TeamId - 1] + AIPlayers[aiId].Name);

                    PlayerColorsOnPlayerLocations[startBox.SelectedIndex - 1].Add(AIPlayers[aiId].ColorId);
                }
                LimitedComboBox teamBox = getPlayerTeamCMBFromId(index);
                teamBox.SelectedIndex = AIPlayers[aiId].TeamId;
                teamBox.Enabled = true;
                UserCheckBox readyBox = getPlayerReadyBoxFromId(index);
                readyBox.Checked = true;
                readyBox.Enabled = false;

                if (!isHost)
                {
                    lcb.CanDropDown = false;
                    sideBox.CanDropDown = false;
                    colorBox.CanDropDown = false;
                    startBox.CanDropDown = false;
                    teamBox.CanDropDown = false;
                }
                else
                {
                    lcb.CanDropDown = true;
                    sideBox.CanDropDown = true;
                    colorBox.CanDropDown = true;
                    startBox.CanDropDown = true;
                    teamBox.CanDropDown = true;
                }
            }

            // Unused slots
            for (int cmbId = Players.Count + AIPlayers.Count + 1; cmbId < 9; cmbId++)
            {
                LimitedComboBox lcb = getPlayerNameCMBFromId(cmbId);
                lcb.Visible = true;
                if (cmbId == Players.Count + AIPlayers.Count + 1)
                {
                    if (isHost)
                    {
                        lcb.CanDropDown = true;
                        lcb.Enabled = true;
                    }
                    else
                    {
                        lcb.Enabled = false;
                    }
                }
                else
                {
                    lcb.Enabled = false;
                }

                lcb.DropDownStyle = ComboBoxStyle.DropDownList;
                lcb.SelectedIndex = -1;
                lcb.Text = String.Empty;

                pNameTextBoxes[cmbId - 1].Visible = false;
                pNameTextBoxes[cmbId - 1].Enabled = false;

                LimitedComboBox sideBox = getPlayerSideCMBFromId(cmbId);
                sideBox.SelectedIndex = -1;
                sideBox.Enabled = false;
                LimitedComboBox colorBox = getPlayerColorCMBFromId(cmbId);
                colorBox.SelectedIndex = -1;
                colorBox.Enabled = false;
                LimitedComboBox startBox = getPlayerStartCMBFromId(cmbId);
                startBox.SelectedIndex = -1;
                startBox.Enabled = false;
                LimitedComboBox teamBox = getPlayerTeamCMBFromId(cmbId);
                teamBox.SelectedIndex = -1;
                teamBox.Enabled = false;
                UserCheckBox readyBox = getPlayerReadyBoxFromId(cmbId);
                readyBox.Checked = false;
                readyBox.Enabled = false;
            }

            updatePlayers = true;

            // Re-draw the preview
            pbGameLobbyPreview.Refresh();
        }

        /// <summary>
        /// Broadcasts game options to all players whenever a check box's Checked status is changed.
        /// </summary>
        private void GenericChkBox_CheckedChanged(object sender)
        {
            if (updateGameOptions)
                GenericGameOptionChanged(null, EventArgs.Empty);
        }

        /// <summary>
        /// Broadcasts game options to all players.
        /// </summary>
        private void GenericGameOptionChanged(object sender, EventArgs e)
        {
            if (!isHost || !updateGameOptions)
                return;

            StringBuilder sb = new StringBuilder("GOPTS ");
            for (int chkId = 0; chkId < CheckBoxes.Count; chkId++)
            {
                sb.Append(Convert.ToInt32(CheckBoxes[chkId].Checked));
                sb.Append(";");
            }
            for (int cmbId = 0; cmbId < ComboBoxes.Count; cmbId++)
            {
                sb.Append(ComboBoxes[cmbId].SelectedIndex);
                sb.Append(";");
            }
            if (currentMap == null)
            {
                sb.Append("none;");
                sb.Append("none;");
            }
            else
            {
                sb.Append(currentMap.SHA1);
                sb.Append(";");
                sb.Append(currentGameMode);
                sb.Append(";");
            }
            sb.Append(Seed); // seed

            lock (locker)
            {
                for (int pId = 0; pId < Clients.Count; pId++)
                {
                    Clients[pId].Ready = false;
                }
            }

            RefreshPlayers();

            CopyPlayerDataToUI();

            Broadcast(sb.ToString());

            if (gameOptionRefreshTimer.Enabled)
                gameOptionRefreshTimer.Stop();
        }

        /// <summary>
        /// Called when the game lobby form is closed.
        /// </summary>
        private void NGameLobby_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!isManualClose)
            {
                leaving = true;
                if (isHost)
                {
                    listener.Stop();
                    foreach (LANPlayer lp in Clients)
                    {
                        if (lp.Client != null && lp.Client.Connected)
                            lp.Client.Close();
                    }
                    UpdateGameListing(null, EventArgs.Empty);
                    timer.Stop();
                    timer.Dispose();
                }
                else
                    SendToServer("QUIT");
                CnCNetData.IsGameLobbyOpen = false;
                UpdateGameListing(btnLeaveGame, EventArgs.Empty);
                DomainController.Instance().SaveGameLobbySettings(!chkDisableSounds.Checked, this.WindowState);
                Unsubscribe();
                CnCNetData.DoGameLobbyClosed();
            }
        }

        /// <summary>
        /// Unsubscribes from events and disposes resources. Call when the form is closed
        /// to prevent memory leaks.
        /// </summary>
        private void Unsubscribe()
        {
            SharedUILogic.GameProcessExited -= GameProcessExited;

            btn133px.Dispose();
            btn133px_c.Dispose();
            imgKick.Dispose();
            imgKick_c.Dispose();
            imgBan.Dispose();
            imgBan_c.Dispose();
        }

        /// <summary>
        /// Opens the map selection dialog.
        /// </summary>
        private void btnChangeMap_Click(object sender, EventArgs e)
        {
            //MapSelectionForm msf = new MapSelectionForm();
            //DialogResult dr = msf.ShowDialog();

            //if (dr == System.Windows.Forms.DialogResult.OK)
            //{
            //    currentMap = CnCNetData.MapList[msf.rtnMapIndex];
            //    currentGameMode = msf.rtnGameMode;

            //    SetMapInfo(msf.rtnMapIndex);

            //    LoadPreview();

            //    LockOptions();

            //    GenericGameOptionChanged(sender, e);
            //}
        }

        private void SetMapInfo(int mapIndex)
        {
            lblLobbyGameMode.Text = "Game Mode: " + currentGameMode;

            if (currentMap == null)
            {
                lblLobbyMapName.Text = "No map";
                lblLobbyMapAuthor.Text = "No author";
                return;
            }

            lblLobbyMapName.Text = "Map: " + currentMap.Name;
            lblLobbyMapAuthor.Text = "By " + currentMap.Author;

            // Performance doesn't matter here
            if (mapInfoStyle == LabelInfoStyle.ALLCAPS)
            {
                lblLobbyGameMode.Text = lblLobbyGameMode.Text.ToUpper();
                lblLobbyMapName.Text = lblLobbyMapName.Text.ToUpper();
                lblLobbyMapAuthor.Text = lblLobbyMapAuthor.Text.ToUpper();
            }

            Graphics g = lblLobbyMapAuthor.CreateGraphics();
            SizeF size = g.MeasureString(lblLobbyMapAuthor.Text, lblLobbyMapAuthor.Font);
            lblLobbyMapAuthor.Location = new Point(this.Size.Width - (int)(size.Width * 1.25f) - 5, lblLobbyMapAuthor.Location.Y);

            if (mapIndex >= CnCNetData.AmountOfOfficialMaps)
            {
                lblLobbyMapName.ForeColor = Color.Red;
                lblLobbyMapName.Text = lblLobbyMapName.Text + " (unofficial map)";
            }
            else
                lblLobbyMapName.ForeColor = lblLobbyMapAuthor.ForeColor;
        }

        /// <summary>
        /// Parses and applies forced game options. Called when the map is changed.
        /// </summary>
        private void LockOptions()
        {
            this.SuspendLayout();

            updatePlayers = false;

            foreach (UserCheckBox ucb in CheckBoxes)
            {
                ucb.Enabled = true;
            }

            foreach (LimitedComboBox lcb in ComboBoxes)
            {
                lcb.Enabled = true;
            }

            if (currentMap == null)
            {
                this.ResumeLayout();
                return;
            }

            foreach (PlayerInfo p in Players)
            {
                if (p.StartingLocation > currentMap.AmountOfPlayers)
                    p.StartingLocation = 0;
            }

            foreach (PlayerInfo p in AIPlayers)
            {
                if (p.StartingLocation > currentMap.AmountOfPlayers)
                    p.StartingLocation = 0;
            }

            for (int i = 1; i < 9; i++)
            {
                LimitedComboBox lcb = getPlayerStartCMBFromId(i);
                lcb.Items.Clear();
                lcb.Items.Add("Random");

                for (int a = 1; a <= currentMap.AmountOfPlayers; a++)
                    lcb.Items.Add(a);
            }

            IniFile lockedOptionsIni = new IniFile(ProgramConstants.GamePath + "INI\\" + currentGameMode + "_ForcedOptions.ini");
            ParseLockedOptionsFromIni(lockedOptionsIni);

            coopDifficultyLevel = lockedOptionsIni.GetIntValue("ForcedOptions", "CoopDifficultyLevel", 2);

            string mapPath = ProgramConstants.GamePath + currentMap.Path;
            string mapCodePath = mapPath.Substring(0, mapPath.Length - 3) + "ini";

            lockedOptionsIni = new IniFile(mapCodePath);
            ParseLockedOptionsFromIni(lockedOptionsIni);

            if (currentMap.IsCoop)
            {
                // If the coop mission forces players to have a specific side, let's show it in the UI
                int forcedPlayerSide = lockedOptionsIni.GetIntValue("CoopInfo", "ForcedPlayerSide", -1);
                if (forcedPlayerSide > -1)
                {
                    for (int i = 0; i < currentMap.AmountOfPlayers; i++)
                    {
                        pSideLabels[i].Text = cmbP1Side.Items[forcedPlayerSide + 1].ToString();
                        pSideLabels[i].Visible = true;
                    }

                    for (int i = currentMap.AmountOfPlayers; i < 8; i++)
                    {
                        pSideLabels[i].Text = String.Empty;
                        pSideLabels[i].Visible = true;
                    }

                    foreach (PlayerInfo player in Players)
                    {
                        if (player.SideId == cmbP1Side.Items.Count - 1)
                            player.SideId = 0;
                    }

                    foreach (PlayerInfo aiPlayer in AIPlayers)
                    {
                        if (aiPlayer.SideId == cmbP1Side.Items.Count - 1)
                            aiPlayer.SideId = 0;
                    }

                    if (isHost)
                        CopyPlayerDataToUI();

                    SetSideComboBoxesVisibility(false);
                }
                else
                {
                    // If the coop mission doesn't force a side, show the usual combo boxes

                    foreach (TextBox pSideLabel in pSideLabels)
                    {
                        pSideLabel.Visible = false;
                    }

                    SetSideComboBoxesVisibility(true);
                }

                lblPlayerTeam.Visible = false;
                cmbP1Team.Visible = false;
                cmbP2Team.Visible = false;
                cmbP3Team.Visible = false;
                cmbP4Team.Visible = false;
                cmbP5Team.Visible = false;
                cmbP6Team.Visible = false;
                cmbP7Team.Visible = false;
                cmbP8Team.Visible = false;
            }
            else
            {
                // For non-coop missions we'll show all options as usual

                foreach (TextBox pSideLabel in pSideLabels)
                {
                    pSideLabel.Visible = false;
                }

                SetSideComboBoxesVisibility(true);

                lblPlayerTeam.Visible = true;
                cmbP1Team.Visible = true;
                cmbP2Team.Visible = true;
                cmbP3Team.Visible = true;
                cmbP4Team.Visible = true;
                cmbP5Team.Visible = true;
                cmbP6Team.Visible = true;
                cmbP7Team.Visible = true;
                cmbP8Team.Visible = true;
            }

            this.ResumeLayout();
        }

        /// <summary>
        /// Sets the visibility of all player side combo boxes to true or false.
        /// </summary>
        /// <param name="visible">Whether player side combo boxes should be displayed or not.</param>
        private void SetSideComboBoxesVisibility(bool visible)
        {
            cmbP1Side.Visible = visible;
            cmbP2Side.Visible = visible;
            cmbP3Side.Visible = visible;
            cmbP4Side.Visible = visible;
            cmbP5Side.Visible = visible;
            cmbP6Side.Visible = visible;
            cmbP7Side.Visible = visible;
            cmbP8Side.Visible = visible;
        }

        /// <summary>
        /// Parses and applies forced game options from an INI file.
        /// </summary>
        /// <param name="lockedOptionsIni">The INI file to read and apply the forced game options from.</param>
        private void ParseLockedOptionsFromIni(IniFile lockedOptionsIni)
        {
            // 2. 11. 2014: prevent a million game option broadcasts when changing settings
            updateGameOptions = false;

            SharedUILogic.ParseLockedOptionsFromIni(CheckBoxes, ComboBoxes, lockedOptionsIni);

            updateGameOptions = true;
        }

        /// <summary>
        /// Loads the map preview, adjusts starting location indicator positions
        /// and displays the preview on the image box.
        /// </summary>
        private void LoadPreview()
        {
            if (currentMap == null)
            {
                pbGameLobbyPreview.Image = Image.FromFile(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "nopreview.png");
                return;
            }

            PictureBoxSizeMode sizeMode;
            bool success;

            Image previewImg = SharedLogic.LoadPreview(currentMap, out sizeMode, out success);

            pbGameLobbyPreview.SizeMode = sizeMode;

            if (!success)
            {
                pbGameLobbyPreview.Image = previewImg;
                return;
            }

            int x = previewImg.Width;
            int y = previewImg.Height;

            double prRatioX = pbGameLobbyPreview.Size.Width / Convert.ToDouble(x);
            double prRatioY = pbGameLobbyPreview.Size.Height / Convert.ToDouble(y);

            if (prRatioX > prRatioY)
            {
                previewRatioX = prRatioY;
                previewRatioY = prRatioY;
            }
            else
            {
                previewRatioX = prRatioX;
                previewRatioY = prRatioX;
            }

            if (sharpenPreview && !currentMap.StaticPreviewSize)
            {
                if (x < pbGameLobbyPreview.Size.Width && y < pbGameLobbyPreview.Size.Height)
                    pbGameLobbyPreview.Image = previewImg;
                else
                {
                    if (this.WindowState == FormWindowState.Minimized)
                    {
                        pbGameLobbyPreview.Image = previewImg;
                        return;
                    }

                    Image newImage = SharedLogic.ResizeImage(previewImg, pbGameLobbyPreview.Size.Width, pbGameLobbyPreview.Size.Height);

                    double factorX = x / Convert.ToDouble(pbGameLobbyPreview.Size.Width);
                    double factorY = y / Convert.ToDouble(pbGameLobbyPreview.Size.Height);

                    double sharpeningFactor = 1.0;
                    if (factorX > factorY)
                        sharpeningFactor = factorX;
                    else
                        sharpeningFactor = factorY;

                    pbGameLobbyPreview.Image = SharedLogic.Sharpen(newImage, sharpeningFactor);
                }
            }
            else
                pbGameLobbyPreview.Image = previewImg;
        }

        private void tbChatInputBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (string.IsNullOrEmpty(tbGameLobbyChatInput.Text))
                return;

            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Sends a chat message if the user presses the ENTER key on the chat input box.
        /// </summary>
        private void tbChatInputBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData != Keys.Return)
            {
                return;
            }

            e.Handled = true;

            if (string.IsNullOrEmpty(tbGameLobbyChatInput.Text))
            {
                return;
            }

            if (tbGameLobbyChatInput.Text[0] == '/')
            {
                try
                {
                    string originalCommand = tbGameLobbyChatInput.Text;
                    string commandToParse = originalCommand.Substring(1).ToUpper();

                    // Some quick, hacky text commands just for the fun of it
                    if (commandToParse == "UNLOCK" && Locked && isHost)
                    {
                        AddNotice("You've unlocked the game room.");
                        UnlockGame(false);
                    }
                    else if (commandToParse == "LOCK" && !Locked && isHost)
                    {
                        AddNotice("You've locked the game room.");
                        LockGame(false);
                    }
                    else if (commandToParse.StartsWith("PRIVMSG"))
                    {
                        string[] messageParts = originalCommand.Split(' ');
                        string receiver = messageParts[1];
                        int index = receiver.IndexOf(',');
                        if (index > -1)
                            receiver = receiver.Substring(0, index);

                        int pIndex = CnCNetData.players.FindIndex(c => c == receiver);
                        if (pIndex > -1)
                            CnCNetData.ConnectionBridge.SendMessage("PRIVMSG " + receiver + " " + messageParts[2]);
                    }
                    else if (commandToParse == "EXIT")
                    {
                        btnLeaveGame.PerformClick();
                    }
                    else if (commandToParse == "QUIT")
                    {
                        CnCNetData.ConnectionBridge.SendMessage("QUIT");
                        Environment.Exit(0);
                    }
                    else if (commandToParse == "ACCEPT" && !isHost)
                    {
                        btnLaunchGame.PerformClick();
                    }
                    else if (commandToParse == "?")
                    {
                        AddNotice("Commands:");
                        AddNotice("/ACCEPT: Ready yourself for the game (non-host players only)");
                        AddNotice("/QUIT: Instantly quits the CnCNet Client");
                        AddNotice("/EXIT: Exits the current game lobby");
                        AddNotice("/PRIVMSG <playername> <message>: Sends a private message to a player");
                        AddNotice("/LOCK: Locks the current game room");
                        AddNotice("/UNLOCK: Unlocks the current game room");
                    }
                    else
                    {
                        AddNotice("Unknown command " + commandToParse + ". Type /? to see the list of available commands.");
                    }

                    tbGameLobbyChatInput.Clear();

                    return;
                }
                catch
                {
                    AddNotice("Syntax error: an error occured while attempting to parse the given command.");
                    tbGameLobbyChatInput.Clear();
                    return;
                }
            }

            SendToServer("CHAT " + myChatColorId + "~" + tbGameLobbyChatInput.Text.Replace('^', '-').Replace('~', '-'));

            tbGameLobbyChatInput.Clear();

            if (!chkDisableSounds.Checked)
                sndMessageSound.Play();
        }

        /// <summary>
        /// Used for automatic scrolling of the chat list box as new entries are added.
        /// </summary>
        private void ScrollListbox(string text)
        {
            int displayedItems = lbGameLobbyChat.DisplayRectangle.Height / lbGameLobbyChat.ItemHeight;
            sbGameLobbyChat.Maximum = lbGameLobbyChat.Items.Count - Convert.ToInt32(displayedItems * 0.2);
            if (sbGameLobbyChat.Maximum < 0)
                sbGameLobbyChat.Maximum = 1;
            double multi = CreateGraphics().MeasureString(text, lbGameLobbyChat.Font, lbGameLobbyChat.Width).Height /
                CreateGraphics().MeasureString("@", lbGameLobbyChat.Font).Height;
            int x = 0;
            while (x < multi)
            {
                sbGameLobbyChat.Value++;
                lbGameLobbyChat.TopIndex++;
                x++;
            }
        }

        /// <summary>
        /// Measures entries in the chat message list box.
        /// </summary>
        private void lbChatBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)e.Graphics.MeasureString(lbGameLobbyChat.Items[e.Index].ToString(),
                lbGameLobbyChat.Font, lbGameLobbyChat.Width - 30).Height;
            e.ItemWidth = lbGameLobbyChat.Width - 30;
        }

        /// <summary>
        /// Used for manually drawing chat messages in the chat message list box.
        /// </summary>
        private void lbChatBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1 && e.Index < lbGameLobbyChat.Items.Count)
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

                Color foreColor = MessageColors[e.Index];
                e.Graphics.DrawString(lbGameLobbyChat.Items[e.Index].ToString(), e.Font, new SolidBrush(foreColor), e.Bounds);
            }
        }

        /// <summary>
        /// Disallows changing the visible player name.
        /// </summary>
        private void cmbPXName_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Allows the game host to launch the game (and non-hosting players to accept).
        /// Performs various checks related to starting the game, and if the checks pass,
        /// gathers the information necessary for starting the game and starts it.
        /// </summary>
        private void btnLaunchGame_Click(object sender, EventArgs e)
        {
            if (!isHost)
            {
                SendToServer("READY 1");
                return;
            }

            if (!Locked)
            {
                AddNotice("You need to lock the game room before launching the game.");
                return;
            }

            // Various failsafe checks

            List<int> occupiedColorIds = new List<int>();
            foreach (PlayerInfo player in Players)
            {
                if (!occupiedColorIds.Contains(player.ColorId) || player.ColorId == 0)
                    occupiedColorIds.Add(player.ColorId);
                else
                {
                    Broadcast("SAMECOLOR");
                    return;
                }
            }

            // Prevent AI players from being spectators
            foreach (PlayerInfo aiPlayer in AIPlayers)
            {
                if (aiPlayer.SideId == SideComboboxPrerequisites.Count + 1)
                {
                    Broadcast("AISPECS");
                    return;
                }
            }

            // Prevent spectating in co-op missions
            if (currentMap.IsCoop)
            {
                foreach (PlayerInfo player in Players)
                {
                    if (player.SideId == SideComboboxPrerequisites.Count + 1)
                    {
                        Broadcast("COOPSPECS");
                        return;
                    }
                }
            }

            // Prevent multiple players from sharing the same starting location
            if (currentMap.EnforceMaxPlayers)
            {
                foreach (PlayerInfo player in Players)
                {
                    if (player.StartingLocation == 0)
                        continue;

                    int index = Players.FindIndex(p => p.StartingLocation == player.StartingLocation && p.Name != player.Name);
                    if (index > -1)
                    {
                        Broadcast("SAMESTARTLOC");
                        return;
                    }

                    index = AIPlayers.FindIndex(p => p.StartingLocation == player.StartingLocation);

                    if (index > -1)
                    {
                        Broadcast("SAMESTARTLOC");
                        return;
                    }
                }

                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
                    int startingLocation = AIPlayers[aiId].StartingLocation;

                    if (startingLocation == 0)
                        continue;

                    int index = AIPlayers.FindIndex(aip => aip.StartingLocation == startingLocation);

                    if (index > -1 && index != aiId)
                    {
                        Broadcast("SAMESTARTLOC");
                        return;
                    }
                }
            }

            lock (locker)
            {
                foreach (LANPlayer lp in Clients)
                {
                    if (lp.IsInGame)
                    {
                        Broadcast("INGAME " + lp.Name);
                        return;
                    }
                }
            }

            int iId = 0;
            foreach (PlayerInfo player in Players)
            {
                iId++;

                if (player.Name == ProgramConstants.PLAYERNAME)
                    continue;

                if (!player.Ready)
                {
                    Broadcast("GETREADY");
                    return;
                }
            }

            if (currentMap.EnforceMaxPlayers)
            {
                if (Players.Count + AIPlayers.Count -
                    SharedUILogic.GetSpectatorCount(SideComboboxPrerequisites.Count, Players) 
                    > currentMap.AmountOfPlayers)
                {
                    Broadcast("TMPLAYERS");
                    return;
                }
            }

            if (currentMap.MinPlayers > Players.Count + AIPlayers.Count -
                SharedUILogic.GetSpectatorCount(SideComboboxPrerequisites.Count, Players))
            {
                Broadcast("INFSPLAYERS");
                return;
            }

            if (currentMap == null)
            {
                MessageBox.Show("Unable to start the game: the selected map is invalid!", "Invalid map", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            for (int pId = 0; pId < Players.Count; pId++)
            {
                if (Players[pId].StartingLocation > currentMap.AmountOfPlayers)
                {
                    Broadcast("INVSTART " + Players[pId].Name);
                    return;
                }
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                if (AIPlayers[aiId].StartingLocation > currentMap.AmountOfPlayers)
                {
                    Broadcast("INVAISTART " + (aiId + 1));
                    return;
                }
            }

            if (Players.Count == 1)
            {
                DialogResult dr = MessageBox.Show("It would surely be more fun to have others playing with you."
                    + Environment.NewLine + Environment.NewLine +
                    "Do you really want to play alone?", "Only one player?", MessageBoxButtons.YesNo);

                if (dr == System.Windows.Forms.DialogResult.No)
                    return;
            }

            List<int> playerPorts;

            int gameId = Int32.Parse(DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + new Random().Next(1, 1000).ToString());

            if (Players.Count > 1)
            {
                Broadcast("START " + gameId);
            }
            else
            {
                Logger.Log("One player MP -- starting!");
                playerPorts = new List<int>();
            }

            for (int pId = 0; pId < Players.Count; pId++)
            {
                Players[pId].IsInGame = true;
            }

            StartGame(gameId);
        }

        /// <summary>
        /// Writes spawn.ini and spawnmap.ini and starts the game.
        /// </summary>
        private void StartGame(int gameId)
        {
            if (this.InvokeRequired)
            {
                IntDelegate d = new IntDelegate(StartGame);
                BeginInvoke(d, gameId);
                return;
            }

            string mapPath = ProgramConstants.GamePath + currentMap.Path;
            if (!File.Exists(mapPath))
            {
                MessageBox.Show("Unable to locate scenario map!" + Environment.NewLine + mapPath,
                    "Cannot read scenario", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLeaveGame.PerformClick();
                return;
            }

            // 28. 12. 2014 No editing the map after accepting on it!
            string mapSHA1 = Utilities.CalculateSHA1ForFile(mapPath);
            if (mapSHA1 != currentMap.SHA1)
            {
                MessageBox.Show("Map modification detected! Please restart the Client." + Environment.NewLine + mapPath,
                    "Map modification detected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SendToServer("MAPMOD");
                btnLeaveGame.PerformClick();
                return;
            }

            string currentHash = Anticheat.Instance.GetCompleteHash();

            Anticheat.Instance.CalculateHashes();

            if (currentHash != Anticheat.Instance.GetCompleteHash())
            {
                SendToServer("FHASH " + Anticheat.Instance.GetCompleteHash());
            }

            string mapCodePath = currentMap.Path.Substring(0, currentMap.Path.Length - 3) + "ini";

            List<int> playerSides = new List<int>();
            List<bool> isPlayerSpectator = new List<bool>();
            List<int> playerColors = new List<int>();
            List<int> playerStartingLocations = new List<int>();

            SharedUILogic.Randomize(Players, AIPlayers, currentMap, Seed, playerSides,
                isPlayerSpectator, playerColors, playerStartingLocations,
                SharedUILogic.GetAllowedSides(ComboBoxes, CheckBoxes,
                SideComboboxPrerequisites, SideCheckboxPrerequisites),
                SideComboboxPrerequisites.Count);

            List<int> MultiCmbIndexes;

            List<int> playerPorts = new List<int>();
            foreach (PlayerInfo player in Players)
                playerPorts.Add(1235);

            SharedUILogic.WriteSpawnIni(Players, AIPlayers, currentMap, currentGameMode, Seed,
                isHost, playerPorts, String.Empty, -1, ComboBoxes,
                CheckBoxes, AssociatedCheckBoxSpawnIniOptions,
                AssociatedComboBoxSpawnIniOptions, ComboBoxDataWriteModes,
                playerSides, isPlayerSpectator, playerColors, playerStartingLocations,
                out MultiCmbIndexes, gameId);

            int forcedSideIndex = SharedLogic.WriteCoopDataToSpawnIni(currentMap, Players, AIPlayers, MultiCmbIndexes,
                coopDifficultyLevel, SideComboboxPrerequisites.Count, mapCodePath, Seed);

            File.Copy(mapPath, ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);

            IniFile mapIni = new IniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);

            SharedUILogic.WriteMap(currentGameMode, CheckBoxes, AssociatedCheckBoxCustomInis, mapIni);

            ms = new MatchStatistics(ProgramConstants.GAME_VERSION, currentMap.Name, currentGameMode, Players.Count);
            int id = 0;
            foreach (PlayerInfo p in Players)
            {
                int sideIndex = playerSides[id] + 1;

                if (forcedSideIndex > -1)
                    sideIndex = forcedSideIndex + 1;

                if (p.Name == ProgramConstants.PLAYERNAME)
                    ms.AddPlayer(p.Name, true, false, p.SideId == SideComboboxPrerequisites.Count + 1, sideIndex, p.TeamId, 10);
                else
                    ms.AddPlayer(p.Name, false, false, p.SideId == SideComboboxPrerequisites.Count + 1, sideIndex, p.TeamId, 10);

                id++;
            }

            foreach (PlayerInfo ai in AIPlayers)
            {
                int sideIndex = playerSides[id] + 1;

                if (forcedSideIndex > -1)
                    sideIndex = forcedSideIndex + 1;

                ms.AddPlayer("Computer", false, true, false, sideIndex, ai.TeamId, GetAILevel(ai));
                id++;
            }

            if (defaultGame.ToUpper() == "YR")
            {
                // This is pretty ugly, but oh well - game options shouldn't be configured in RA2MD.ini anyway,
                // so that makes it ugly to begin with
                UserCheckBox ra2ModeChk = CheckBoxes.Find(chk => chk.Name == "chkRA2Mode");

                if (ra2ModeChk != null)
                {
                    Logger.Log("Writing RA2 mode setting.");

                    IniFile settingsIni = new IniFile(ProgramConstants.GamePath + DomainController.Instance().GetSettingsIniName());

                    File.Delete(ProgramConstants.GamePath + "ClassicMode.mix");

                    if (ra2ModeChk.Checked)
                    {
                        settingsIni.SetStringValue("Files", "AIMD.INI", "AI.INI");
                        settingsIni.SetStringValue("Options", "ClassicMode", "Yes");
                        File.Copy(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "ClassicMode.mix", ProgramConstants.GamePath + "ClassicMode.mix");
                    }
                    else
                    {
                        settingsIni.SetStringValue("Files", "AIMD.INI", "AIMD.INI");
                        settingsIni.SetStringValue("Options", "ClassicMode", "No");
                    }

                    settingsIni.WriteIniFile();
                }
            }

            if (SavedGameManager.AreSavedGamesAvailable())
            { 
                fsw = new FileSystemWatcher(ProgramConstants.GamePath + "Saved Games", "*.NET");
                fsw.EnableRaisingEvents = true;
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Created;
            }

            if (isHost)
            {
                lock (locker)
                {
                    foreach (LANPlayer lp in Clients)
                        lp.IsInGame = true;
                }
            }

            Logger.Log("About to launch main executable.");

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
                if (!gameSaved)
                {
                    bool success = SavedGameManager.InitSavedGames();

                    if (!success)
                        return;
                }

                gameSaved = true;

                SavedGameManager.RenameSavedGame();
            }
        }

        private int GetAILevel(PlayerInfo aiInfo)
        {
            if (aiInfo.Name == "Hard AI")
                return 2;
            if (aiInfo.Name == "Medium AI")
                return 1;

            return 0;
        }

        /// <summary>
        /// Starts the game process and changes some internal variables so other client components know it as well.
        /// </summary>
        private void StartGameProcess()
        {
            ProgramConstants.IsInGame = true;

            SharedUILogic.StartGameProcess(0);

            if (isHost)
            {
                timer.Stop();
                timer.Interval = 15000;
                timer.Start();
            }

            CnCNetData.DoGameStarted();
        }

        /// <summary>
        /// Executed when the 'game process' (typically game.exe in fullscreen mode, qres.dat in windowed) has exited.
        /// </summary>
        private void GameProcessExited()
        {
            Logger.Log("GameProcessExited: Begin");

            if (cmbP1Name.InvokeRequired)
            {
                Logger.Log("GameProcessExited: Invoking.");
                NoParamCallback d = new NoParamCallback(GameProcessExited);
                this.BeginInvoke(d, null);
                return;
            }

            if (defaultGame.ToUpper() != "YR")
            {
                Logger.Log("GameProcessExited: Parsing statistics.");

                ms.ParseStatistics(ProgramConstants.GamePath, defaultGame);

                Logger.Log("GameProcessExited: Adding match to statistics.");

                StatisticsManager.Instance.AddMatchAndSaveDatabase(true, ms);

                if (SavedGameManager.AreSavedGamesAvailable())
                {
                    fsw.Created -= fsw_Created;
                    fsw.Changed -= fsw_Created;
                    fsw.Dispose();
                }
            }

            Logger.Log("The game process has exited; displaying game lobby.");

            DomainController.Instance().ReloadSettings();

            ProgramConstants.IsInGame = false;
            CnCNetData.DoGameStopped();

            SendToServer("RETURN");

            if (isHost)
            {
                Seed = new Random().Next(10000, 99999);
                GenericGameOptionChanged(null, EventArgs.Empty);

                if (Players.Count < playerLimit)
                {
                    UnlockGame(true);
                }

                timer.Stop();
                timer.Interval = 5000;
                timer.Start();

                UpdateGameListing(null, EventArgs.Empty);
            }
        }

        private void DisplayHostInGameBox()
        {
            if (this.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(DisplayHostInGameBox);
                this.BeginInvoke(d, null);
                return;
            }

            AddNotice("The game host is still playing the game you previously started. " +
                "You can either wait for the host to return or leave the game room " +
                "by clicking Leave Game.");
        }

        /// <summary>
        /// Automatically scrolls the chat list box when the user resizes
        /// the game lobby window.
        /// </summary>
        private void NGameLobby_SizeChanged(object sender, EventArgs e)
        {
            lbGameLobbyChat.SelectedIndex = lbGameLobbyChat.Items.Count - 1;
            lbGameLobbyChat.SelectedIndex = -1;
        }

        private void btnP1Kick_Click(object sender, EventArgs e)
        {
            AddNotice("You can't kick yourself! You might develop serious self-esteem problems.");
        }

        /// <summary>
        /// Kicks a player from the game room.
        /// </summary>
        /// <param name="index">The index of the player to be kicked.</param>
        private void KickPlayer(int index)
        {
            if (Clients.Count > index)
            {
                LANPlayer lp = Clients[index];
                AddNotice("Kicking " + lp.Name + " from the game...");
                Broadcast("KICK " + lp.Name);
                RemovePlayer(lp);
            }
            else if (Clients.Count + AIPlayers.Count > index)
            {
                // If the host is kicking an AI player, just remove it
                AIPlayers.RemoveAt(index - Players.Count);
                CopyPlayerDataToUI();
                CopyPlayerDataFromUI(null, EventArgs.Empty);
            }
        }

        private void btnP2Kick_Click(object sender, EventArgs e)
        {
            KickPlayer(1);
        }

        private void btnP3Kick_Click(object sender, EventArgs e)
        {
            KickPlayer(2);
        }

        private void btnP4Kick_Click(object sender, EventArgs e)
        {
            KickPlayer(3);
        }

        private void btnP5Kick_Click(object sender, EventArgs e)
        {
            KickPlayer(4);
        }

        private void btnP6Kick_Click(object sender, EventArgs e)
        {
            KickPlayer(5);
        }

        private void btnP7Kick_Click(object sender, EventArgs e)
        {
            KickPlayer(6);
        }

        private void btnP8Kick_Click(object sender, EventArgs e)
        {
            KickPlayer(7);
        }

        private void pbPreview_SizeChanged(object sender, EventArgs e)
        {
            if (resizeTimer == null)
                return;

            resizeTimer.Stop();
            resizeTimer.Start();
        }

        PropertyInfo imageRectangleProperty = typeof(PictureBox).GetProperty("ImageRectangle", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Used for painting starting locations to the map preview box.
        /// http://stackoverflow.com/questions/18210030/get-pixelvalue-when-click-on-a-picturebox
        /// </summary>
        private void pbPreview_Paint(object sender, PaintEventArgs e)
        {
            Rectangle rectangle = (Rectangle)imageRectangleProperty.GetValue(pbGameLobbyPreview, null);

            SharedUILogic.PaintPreview(currentMap, rectangle, e, 
                playerNameOnPlayerLocationFont, coopBriefingForeColor, displayCoopBriefing,
                previewRatioY, previewRatioX, PlayerNamesOnPlayerLocations, MPColors,
                PlayerColorsOnPlayerLocations, startingLocationIndicators, enemyStartingLocationIndicator);
        }

        private void cmbPXName_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Makes it possible to copy text to the clipboard from the chat box by Ctrl + C.
        /// </summary>
        private void lbChatBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (lbGameLobbyChat.SelectedIndex > -1)
            {
                if (e.KeyCode == Keys.C && e.Control)
                    Clipboard.SetText(lbGameLobbyChat.SelectedItem.ToString());
            }
        }

        private void pbPreview_MouseEnter(object sender, EventArgs e)
        {
            displayCoopBriefing = false;
            pbGameLobbyPreview.Refresh();
        }

        private void pbPreview_MouseLeave(object sender, EventArgs e)
        {
            displayCoopBriefing = true;
            pbGameLobbyPreview.Refresh();
        }

        /// <summary>
        /// Prevents the player from changing their name.
        /// </summary>
        private void playerNameTextBox_GotFocus(object sender, EventArgs e)
        {
            tbGameLobbyChatInput.Focus();
        }

        /// <summary>
        /// (Un)locks the game room if possible.
        /// </summary>
        private void btnLockGame_Click(object sender, EventArgs e)
        {
            if (!Locked)
            {
                LockGame(true);
            }
            else
            {
                if (Players.Count < playerLimit)
                {
                    UnlockGame(true);
                }
                else
                    AddNotice(string.Format("Cannot unlock game; the player limit ({0}) has been reached.", playerLimit));
            }
        }

        /// <summary>
        /// Locks the game room.
        /// </summary>
        /// <param name="announce">Whether to inform the player that the game room was locked.</param>
        private void LockGame(bool announce)
        {
            Locked = true;
            if (announce)
                Broadcast("LOCK");
            btnLockGame.Text = "Unlock Game";

            if (mapInfoStyle == LabelInfoStyle.ALLCAPS)
                btnLockGame.Text = btnLockGame.Text.ToUpper();
        }

        /// <summary>
        /// Unlocks the game room.
        /// </summary>
        /// <param name="announce">Whether to inform the player that the game room was unlocked.</param>
        private void UnlockGame(bool announce)
        {
            Locked = false;
            if (announce)
                Broadcast("UNLOCK");
            btnLockGame.Text = "Lock Game";

            if (mapInfoStyle == LabelInfoStyle.ALLCAPS)
                btnLockGame.Text = btnLockGame.Text.ToUpper();
        }

        /// <summary>
        /// Opens a link if there is one in the selected item index.
        /// </summary>
        private void lbChatBox_DoubleClick(object sender, EventArgs e)
        {
            if (lbGameLobbyChat.SelectedIndex < 0)
                return;

            string selectedItem = lbGameLobbyChat.SelectedItem.ToString();

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
        /// Used for changing the mouse cursor image when it enters a link.
        /// </summary>
        private void lbChatBox_MouseMove(object sender, MouseEventArgs e)
        {
            // Determine hovered item index
            Point mousePosition = lbGameLobbyChat.PointToClient(ListBox.MousePosition);
            int hoveredIndex = lbGameLobbyChat.IndexFromPoint(mousePosition);

            if (hoveredIndex == -1 || hoveredIndex >= lbGameLobbyChat.Items.Count)
            {
                lbGameLobbyChat.Cursor = Cursors.Default;
                return;
            }

            string item = lbGameLobbyChat.Items[hoveredIndex].ToString();

            int urlStartIndex = item.IndexOf("http://");
            if (urlStartIndex == -1)
                urlStartIndex = item.IndexOf("ftp://");

            if (urlStartIndex > -1)
                lbGameLobbyChat.Cursor = Cursors.Hand;
            else
                lbGameLobbyChat.Cursor = Cursors.Default;
        }
    }
}
