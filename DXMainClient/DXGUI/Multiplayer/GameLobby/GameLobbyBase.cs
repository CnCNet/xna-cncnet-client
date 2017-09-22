using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A generic base for all game lobbies (Skirmish, LAN and CnCNet).
    /// Contains the common logic for parsing game options and handling player info.
    /// </summary>
    public abstract class GameLobbyBase : XNAWindow
    {
        protected const int MAX_PLAYER_COUNT = 8;
        protected const int PLAYER_OPTION_VERTICAL_MARGIN = 12;
        protected const int PLAYER_OPTION_HORIZONTAL_MARGIN = 3;
        protected const int PLAYER_OPTION_CAPTION_Y = 6;
        private const int DROP_DOWN_HEIGHT = 21;

        /// <summary>
        /// Creates a new instance of the game lobby base.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="iniName">The name of the lobby in GameOptions.ini.</param>
        public GameLobbyBase(WindowManager windowManager, string iniName,
            List<GameMode> GameModes, bool isMultiplayer) : base(windowManager)
        {
            _iniSectionName = iniName;
            this.GameModes = GameModes;
            this.isMultiplayer = isMultiplayer;
        }

        private string _iniSectionName;

        protected XNAPanel PlayerOptionsPanel;

        protected XNAPanel GameOptionsPanel;

        protected List<MultiplayerColor> MPColors;

        protected List<GameLobbyCheckBox> CheckBoxes = new List<GameLobbyCheckBox>();
        protected List<GameLobbyDropDown> DropDowns = new List<GameLobbyDropDown>();

        /// <summary>
        /// The list of multiplayer game modes.
        /// </summary>
        protected List<GameMode> GameModes;

        /// <summary>
        /// The currently selected game mode.
        /// </summary>
        protected GameMode GameMode { get; set; }

        /// <summary>
        /// The currently selected map.
        /// </summary>
        protected Map Map { get; set; }

        protected XNAClientDropDown[] ddPlayerNames;
        protected XNAClientDropDown[] ddPlayerSides;
        protected XNAClientDropDown[] ddPlayerColors;
        protected XNAClientDropDown[] ddPlayerStarts;
        protected XNAClientDropDown[] ddPlayerTeams;

        protected XNALabel lblName;
        protected XNALabel lblSide;
        protected XNALabel lblColor;
        protected XNALabel lblStart;
        protected XNALabel lblTeam;

        protected XNAClientButton btnLeaveGame;
        protected XNAClientButton btnLaunchGame;
        protected XNALabel lblMapName;
        protected XNALabel lblMapAuthor;
        protected XNALabel lblGameMode;

        protected MapPreviewBox MapPreviewBox;

        protected XNAMultiColumnListBox lbMapList;
        protected XNAClientDropDown ddGameMode;
        protected XNALabel lblGameModeSelect;

        protected XNASuggestionTextBox tbMapSearch;

        protected List<PlayerInfo> Players = new List<PlayerInfo>();
        protected List<PlayerInfo> AIPlayers = new List<PlayerInfo>();

        protected bool PlayerUpdatingInProgress { get; set; }

        protected Texture2D[] RankTextures;

        /// <summary>
        /// The seed used for randomizing player options.
        /// </summary>
        protected int RandomSeed { get; set; }

        /// <summary>
        /// An unique identifier for this game.
        /// </summary>
        protected int UniqueGameID { get; set; }

        private int _sideCount;
        protected int SideCount { get { return _sideCount; } }

#if YR
        /// <summary>
        /// Controls whether Red Alert 2 mode is enabled for CnCNet YR. 
        /// </summary>
        protected bool RA2Mode = false;
#endif

        private bool isMultiplayer = false;

        private MatchStatistics matchStatistics;

        private bool mapChangeInProgress = false;

        IniFile _gameOptionsIni;
        protected IniFile GameOptionsIni
        {
            get { return _gameOptionsIni; }
        }

        public override void Initialize()
        {
            Name = _iniSectionName;
            //if (WindowManager.RenderResolutionY < 800)
            //    ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);
            //else
                ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX - 60, WindowManager.RenderResolutionY - 32);
            WindowManager.CenterControlOnScreen(this);
            BackgroundTexture = AssetLoader.LoadTexture("gamelobbybg.png");

            RankTextures = new Texture2D[4]
            {
                AssetLoader.LoadTexture("rankNone.png"),
                AssetLoader.LoadTexture("rankEasy.png"),
                AssetLoader.LoadTexture("rankNormal.png"),
                AssetLoader.LoadTexture("rankHard.png")
            };

            MPColors = MultiplayerColor.LoadColors();

            _gameOptionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + "GameOptions.ini");

            GameOptionsPanel = new XNAPanel(WindowManager);
            GameOptionsPanel.Name = "GameOptionsPanel";
            GameOptionsPanel.ClientRectangle = new Rectangle(ClientRectangle.Width - 411, 12, 399, 289);
            GameOptionsPanel.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 192), 1, 1);
            GameOptionsPanel.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            PlayerOptionsPanel = new XNAPanel(WindowManager);
            PlayerOptionsPanel.Name = "PlayerOptionsPanel";
            PlayerOptionsPanel.ClientRectangle = new Rectangle(GameOptionsPanel.ClientRectangle.Left - 401, 12, 395, GameOptionsPanel.ClientRectangle.Height);
            PlayerOptionsPanel.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 192), 1, 1);
            PlayerOptionsPanel.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            btnLeaveGame = new XNAClientButton(WindowManager);
            btnLeaveGame.Name = "btnLeaveGame";
            btnLeaveGame.ClientRectangle = new Rectangle(ClientRectangle.Width - 143, ClientRectangle.Height - 28, 133, 23);
            btnLeaveGame.Text = "Leave Game";
            btnLeaveGame.LeftClick += BtnLeaveGame_LeftClick;

            btnLaunchGame = new XNAClientButton(WindowManager);
            btnLaunchGame.Name = "btnLaunchGame";
            btnLaunchGame.ClientRectangle = new Rectangle(12, btnLeaveGame.ClientRectangle.Y, 133, 23);
            btnLaunchGame.Text = "Launch Game";
            btnLaunchGame.LeftClick += BtnLaunchGame_LeftClick;

            MapPreviewBox = new MapPreviewBox(WindowManager, Players, AIPlayers, MPColors, 
                _gameOptionsIni.GetStringValue("General", "Sides", String.Empty).Split(','),
                _gameOptionsIni);
            MapPreviewBox.Name = "MapPreviewBox";
            MapPreviewBox.ClientRectangle = new Rectangle(PlayerOptionsPanel.ClientRectangle.X,
                PlayerOptionsPanel.ClientRectangle.Bottom + 6,
                GameOptionsPanel.ClientRectangle.Right - PlayerOptionsPanel.ClientRectangle.Left,
                ClientRectangle.Height - PlayerOptionsPanel.ClientRectangle.Bottom - 65);
            MapPreviewBox.FontIndex = 1;
            MapPreviewBox.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            MapPreviewBox.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);

            lblMapName = new XNALabel(WindowManager);
            lblMapName.Name = "lblMapName";
            lblMapName.ClientRectangle = new Rectangle(MapPreviewBox.ClientRectangle.X,
                MapPreviewBox.ClientRectangle.Bottom + 3, 0, 0);
            lblMapName.FontIndex = 1;
            lblMapName.Text = "Map:";

            lblMapAuthor = new XNALabel(WindowManager);
            lblMapAuthor.Name = "lblMapAuthor";
            lblMapAuthor.ClientRectangle = new Rectangle(MapPreviewBox.ClientRectangle.Right,
                lblMapName.ClientRectangle.Y, 0, 0);
            lblMapAuthor.FontIndex = 1;
            lblMapAuthor.Text = "By ";

            lblGameMode = new XNALabel(WindowManager);
            lblGameMode.Name = "lblGameMode";
            lblGameMode.ClientRectangle = new Rectangle(lblMapName.ClientRectangle.X,
                lblMapName.ClientRectangle.Bottom + 3, 0, 0);
            lblGameMode.FontIndex = 1;
            lblGameMode.Text = "Game mode:";

            lbMapList = new XNAMultiColumnListBox(WindowManager);
            lbMapList.Name = "lbMapList";
            lbMapList.ClientRectangle = new Rectangle(btnLaunchGame.ClientRectangle.X, GameOptionsPanel.ClientRectangle.Y + 23,
                MapPreviewBox.ClientRectangle.X - btnLaunchGame.ClientRectangle.X - 6,
                MapPreviewBox.ClientRectangle.Bottom - 23 - GameOptionsPanel.ClientRectangle.Y);
            lbMapList.SelectedIndexChanged += LbMapList_SelectedIndexChanged;
            lbMapList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbMapList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 192), 1, 1);
            lbMapList.LineHeight = 16;
            lbMapList.DrawListBoxBorders = true;

            XNAPanel rankHeader = new XNAPanel(WindowManager);
            rankHeader.BackgroundTexture = AssetLoader.LoadTexture("rank.png");
            rankHeader.ClientRectangle = new Rectangle(0, 0, rankHeader.BackgroundTexture.Width,
                19);

            XNAListBox rankListBox = new XNAListBox(WindowManager);
            rankListBox.TextBorderDistance = 2;

            lbMapList.AddColumn(rankHeader, rankListBox);

            lbMapList.AddColumn("MAP NAME", lbMapList.ClientRectangle.Width - RankTextures[1].Width - 3);

            ddGameMode = new XNAClientDropDown(WindowManager);
            ddGameMode.Name = "ddGameMode";
            ddGameMode.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.Right - 150, GameOptionsPanel.ClientRectangle.Y, 150, 21);
            ddGameMode.SelectedIndexChanged += DdGameMode_SelectedIndexChanged;

            foreach (GameMode gm in GameModes)
                ddGameMode.AddItem(gm.UIName);

            lblGameModeSelect = new XNALabel(WindowManager);
            lblGameModeSelect.Name = "lblGameModeSelect";
            lblGameModeSelect.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.X, ddGameMode.ClientRectangle.Top + 2, 0, 0);
            lblGameModeSelect.FontIndex = 1;
            lblGameModeSelect.Text = "GAME MODE:";

            tbMapSearch = new XNASuggestionTextBox(WindowManager);
            tbMapSearch.Name = "tbMapSearch";
            tbMapSearch.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.X,
                lbMapList.ClientRectangle.Bottom + 3, lbMapList.ClientRectangle.Width, 21);
            tbMapSearch.Suggestion = "Search map..";
            tbMapSearch.MaximumTextLength = 64;
            tbMapSearch.InputReceived += TbMapSearch_InputReceived;

            AddChild(lblMapName);
            AddChild(lblMapAuthor);
            AddChild(lblGameMode);
            AddChild(MapPreviewBox);

            AddChild(lbMapList);
            AddChild(tbMapSearch);
            AddChild(lblGameModeSelect);
            AddChild(ddGameMode);

            AddChild(GameOptionsPanel);

            string[] checkBoxes = GameOptionsIni.GetStringValue(_iniSectionName, "CheckBoxes", String.Empty).Split(',');

            foreach (string chkName in checkBoxes)
            {
                GameLobbyCheckBox chkBox = new GameLobbyCheckBox(WindowManager);
                chkBox.Name = chkName;
                AddChild(chkBox);
                chkBox.GetAttributes(GameOptionsIni);
                CheckBoxes.Add(chkBox);
                chkBox.CheckedChanged += ChkBox_CheckedChanged;
            }

            string[] labels = GameOptionsIni.GetStringValue(_iniSectionName, "Labels", String.Empty).Split(',');

            foreach (string labelName in labels)
            {
                XNALabel label = new XNALabel(WindowManager);
                label.Name = labelName;
                AddChild(label);
                label.GetAttributes(GameOptionsIni);
            }

            string[] dropDowns = GameOptionsIni.GetStringValue(_iniSectionName, "DropDowns", String.Empty).Split(',');

            foreach (string ddName in dropDowns)
            {
                GameLobbyDropDown dropdown = new GameLobbyDropDown(WindowManager);
                dropdown.Name = ddName;
                AddChild(dropdown);
                dropdown.GetAttributes(GameOptionsIni);
                DropDowns.Add(dropdown);
                dropdown.SelectedIndexChanged += Dropdown_SelectedIndexChanged;
            }

            AddChild(PlayerOptionsPanel);
            AddChild(btnLaunchGame);
            AddChild(btnLeaveGame);
        }

        private void TbMapSearch_InputReceived(object sender, EventArgs e)
        {
            ListMaps();
        }

        private void Dropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mapChangeInProgress)
                return;

            var dd = (GameLobbyDropDown)sender;
            dd.UserDefinedIndex = dd.SelectedIndex;
            OnGameOptionChanged();
        }

        private void ChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (mapChangeInProgress)
                return;

            var checkBox = (GameLobbyCheckBox)sender;
            checkBox.UserDefinedValue = checkBox.Checked;
            OnGameOptionChanged();
        }

        /// <summary>
        /// Initializes the underlying window class.
        /// </summary>
        protected void InitializeWindow()
        {
            base.Initialize();
        }

        protected virtual void OnGameOptionChanged()
        {
            CheckDisallowedSides();
        }

        protected void DdGameMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            GameMode = GameModes[ddGameMode.SelectedIndex];

            tbMapSearch.Text = string.Empty;
            tbMapSearch.OnSelectedChanged();

            ListMaps();

            if (lbMapList.SelectedIndex == -1)
                lbMapList.SelectedIndex = 0; // Select default map
            else
                ChangeMap(GameMode, Map);
        }

        private void ListMaps()
        {
            lbMapList.SelectedIndexChanged -= LbMapList_SelectedIndexChanged;

            lbMapList.ClearItems();
            lbMapList.SetTopIndex(0);

            lbMapList.SelectedIndex = -1;

            foreach (Map map in GameMode.Maps)
            {
                if (tbMapSearch.Text != tbMapSearch.Suggestion)
                {
                    if (!map.Name.ToUpper().Contains(tbMapSearch.Text.ToUpper()))
                        continue;
                }

                XNAListBoxItem rankItem = new XNAListBoxItem();
                if (map.IsCoop)
                {
                    if (StatisticsManager.Instance.HasBeatCoOpMap(map.Name, GameMode.UIName))
                        rankItem.Texture = RankTextures[Math.Abs(2 - GameMode.CoopDifficultyLevel) + 1];
                    else
                        rankItem.Texture = RankTextures[0];
                }
                else
                    rankItem.Texture = RankTextures[GetDefaultMapRankIndex(map) + 1];

                XNAListBoxItem mapNameItem = new XNAListBoxItem();
                mapNameItem.Text = Renderer.GetSafeString(map.Name, lbMapList.FontIndex);
                if ((map.MultiplayerOnly || GameMode.MultiplayerOnly) && !isMultiplayer)
                    mapNameItem.TextColor = UISettings.DisabledButtonColor;
                else
                    mapNameItem.TextColor = UISettings.AltColor;
                mapNameItem.Tag = map;

                XNAListBoxItem[] mapInfoArray = new XNAListBoxItem[]
                {
                    rankItem,
                    mapNameItem,
                };

                lbMapList.AddItem(mapInfoArray);

                if (map == Map)
                    lbMapList.SelectedIndex = lbMapList.ItemCount - 1;
            }

            lbMapList.SelectedIndexChanged += LbMapList_SelectedIndexChanged;
        }

        protected abstract int GetDefaultMapRankIndex(Map map);

        private void LbMapList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbMapList.SelectedIndex < 0 || lbMapList.SelectedIndex >= lbMapList.ItemCount)
                return;

            XNAListBoxItem item = lbMapList.GetItem(1, lbMapList.SelectedIndex);

            Map map = (Map)item.Tag;

            ChangeMap(GameMode, map);
        }

        /// <summary>
        /// Refreshes the map selection UI to match the currently selected map
        /// and game mode.
        /// </summary>
        protected void RefreshMapSelectionUI()
        {
            if (GameMode == null)
                return;

            int gameModeIndex = ddGameMode.Items.FindIndex(i => i.Text == GameMode.UIName);

            if (gameModeIndex == -1)
                return;

            if (ddGameMode.SelectedIndex == gameModeIndex)
                DdGameMode_SelectedIndexChanged(this, EventArgs.Empty);

            ddGameMode.SelectedIndex = gameModeIndex;
        }

        /// <summary>
        /// Initializes the player option drop-down controls.
        /// </summary>
        protected void InitPlayerOptionDropdowns()
        {
            ddPlayerNames = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerSides = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerColors = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerStarts = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerTeams = new XNAClientDropDown[MAX_PLAYER_COUNT];

            int playerOptionVecticalMargin = GameOptionsIni.GetIntValue(Name, "PlayerOptionVerticalMargin", PLAYER_OPTION_VERTICAL_MARGIN);
            int playerOptionHorizontalMargin = GameOptionsIni.GetIntValue(Name, "PlayerOptionHorizontalMargin", PLAYER_OPTION_HORIZONTAL_MARGIN);
            int playerOptionCaptionLocationY = GameOptionsIni.GetIntValue(Name, "PlayerOptionCaptionLocationY", PLAYER_OPTION_CAPTION_Y);
            int playerNameWidth = GameOptionsIni.GetIntValue(Name, "PlayerNameWidth", 136);
            int sideWidth = GameOptionsIni.GetIntValue(Name, "SideWidth", 91);
            int colorWidth = GameOptionsIni.GetIntValue(Name, "ColorWidth", 79);
            int startWidth = GameOptionsIni.GetIntValue(Name, "StartWidth", 49);
            int teamWidth = GameOptionsIni.GetIntValue(Name, "TeamWidth", 46);
            int locationX = GameOptionsIni.GetIntValue(Name, "PlayerOptionLocationX", 25);
            int locationY = GameOptionsIni.GetIntValue(Name, "PlayerOptionLocationY", 24);

            // InitPlayerOptionDropdowns(136, 91, 79, 49, 46, new Point(25, 24));

            string[] sides = ClientConfiguration.Instance.GetSides().Split(',');
            _sideCount = sides.Length;

            string randomColor = GameOptionsIni.GetStringValue("General", "RandomColor", "255,255,255");

            for (int i = MAX_PLAYER_COUNT - 1; i > -1; i--)
            {
                var ddPlayerName = new XNAClientDropDown(WindowManager);
                ddPlayerName.Name = "ddPlayerName" + i;
                ddPlayerName.ClientRectangle = new Rectangle(locationX,
                    locationY + (DROP_DOWN_HEIGHT + playerOptionVecticalMargin) * i,
                    playerNameWidth, DROP_DOWN_HEIGHT);
                ddPlayerName.AddItem(String.Empty);
                ddPlayerName.AddItem("Easy AI");
                ddPlayerName.AddItem("Medium AI");
                ddPlayerName.AddItem("Hard AI");
                ddPlayerName.AllowDropDown = true;
                ddPlayerName.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerName.Tag = true;

                var ddPlayerSide = new XNAClientDropDown(WindowManager);
                ddPlayerSide.Name = "ddPlayerSide" + i;
                ddPlayerSide.ClientRectangle = new Rectangle(
                    ddPlayerName.ClientRectangle.Right + playerOptionHorizontalMargin,
                    ddPlayerName.ClientRectangle.Y, sideWidth, DROP_DOWN_HEIGHT);
                ddPlayerSide.AddItem("Random", AssetLoader.LoadTexture("randomicon.png"));
                foreach (string sideName in sides)
                    ddPlayerSide.AddItem(sideName, AssetLoader.LoadTexture(sideName + "icon.png"));
                ddPlayerSide.AllowDropDown = false;
                ddPlayerSide.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerSide.Tag = true;

                var ddPlayerColor = new XNAClientDropDown(WindowManager);
                ddPlayerColor.Name = "ddPlayerColor" + i;
                ddPlayerColor.ClientRectangle = new Rectangle(
                    ddPlayerSide.ClientRectangle.Right + playerOptionHorizontalMargin,
                    ddPlayerName.ClientRectangle.Y, colorWidth, DROP_DOWN_HEIGHT);
                ddPlayerColor.AddItem("Random", AssetLoader.GetColorFromString(randomColor));
                foreach (MultiplayerColor mpColor in MPColors)
                    ddPlayerColor.AddItem(mpColor.Name, mpColor.XnaColor);
                ddPlayerColor.AllowDropDown = false;
                ddPlayerColor.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerColor.Tag = false;

                var ddPlayerStart = new XNAClientDropDown(WindowManager);
                ddPlayerStart.Name = "ddPlayerStart" + i;
                ddPlayerStart.ClientRectangle = new Rectangle(
                    ddPlayerColor.ClientRectangle.Right + playerOptionHorizontalMargin,
                    ddPlayerName.ClientRectangle.Y, startWidth, DROP_DOWN_HEIGHT);
                for (int j = 1; j < 9; j++)
                    ddPlayerStart.AddItem(j.ToString());
                ddPlayerStart.AllowDropDown = false;
                ddPlayerStart.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerStart.Visible = false;
                ddPlayerStart.Enabled = false;
                ddPlayerStart.Tag = true;

                var ddPlayerTeam = new XNAClientDropDown(WindowManager);
                ddPlayerTeam.Name = "ddPlayerTeam" + i;
                ddPlayerTeam.ClientRectangle = new Rectangle(
                    ddPlayerColor.ClientRectangle.Right + playerOptionHorizontalMargin,
                    ddPlayerName.ClientRectangle.Y, teamWidth, DROP_DOWN_HEIGHT);
                ddPlayerTeam.AddItem("-");
                ddPlayerTeam.AddItem("A");
                ddPlayerTeam.AddItem("B");
                ddPlayerTeam.AddItem("C");
                ddPlayerTeam.AddItem("D");
                ddPlayerTeam.AllowDropDown = false;
                ddPlayerTeam.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerTeam.Tag = true;

                ddPlayerNames[i] = ddPlayerName;
                ddPlayerSides[i] = ddPlayerSide;
                ddPlayerColors[i] = ddPlayerColor;
                ddPlayerStarts[i] = ddPlayerStart;
                ddPlayerTeams[i] = ddPlayerTeam;

                PlayerOptionsPanel.AddChild(ddPlayerName);
                PlayerOptionsPanel.AddChild(ddPlayerSide);
                PlayerOptionsPanel.AddChild(ddPlayerColor);
                PlayerOptionsPanel.AddChild(ddPlayerStart);
                PlayerOptionsPanel.AddChild(ddPlayerTeam);
            }

            lblName = new XNALabel(WindowManager);
            lblName.Name = "lblName";
            lblName.Text = "PLAYER";
            lblName.FontIndex = 1;
            lblName.ClientRectangle = new Rectangle(ddPlayerNames[0].ClientRectangle.X, playerOptionCaptionLocationY, 0, 0);

            lblSide = new XNALabel(WindowManager);
            lblSide.Name = "lblSide";
            lblSide.Text = "SIDE";
            lblSide.FontIndex = 1;
            lblSide.ClientRectangle = new Rectangle(ddPlayerSides[0].ClientRectangle.X, playerOptionCaptionLocationY, 0, 0);

            lblColor = new XNALabel(WindowManager);
            lblColor.Name = "lblColor";
            lblColor.Text = "COLOR";
            lblColor.FontIndex = 1;
            lblColor.ClientRectangle = new Rectangle(ddPlayerColors[0].ClientRectangle.X, playerOptionCaptionLocationY, 0, 0);

            lblStart = new XNALabel(WindowManager);
            lblStart.Name = "lblStart";
            lblStart.Text = "START";
            lblStart.FontIndex = 1;
            lblStart.ClientRectangle = new Rectangle(ddPlayerStarts[0].ClientRectangle.X, playerOptionCaptionLocationY, 0, 0);
            lblStart.Visible = false;

            lblTeam = new XNALabel(WindowManager);
            lblTeam.Name = "lblTeam";
            lblTeam.Text = "TEAM";
            lblTeam.FontIndex = 1;
            lblTeam.ClientRectangle = new Rectangle(ddPlayerTeams[0].ClientRectangle.X, playerOptionCaptionLocationY, 0, 0);

            PlayerOptionsPanel.AddChild(lblName);
            PlayerOptionsPanel.AddChild(lblSide);
            PlayerOptionsPanel.AddChild(lblColor);
            PlayerOptionsPanel.AddChild(lblStart);
            PlayerOptionsPanel.AddChild(lblTeam);

            CheckDisallowedSides();
        }

        protected abstract void BtnLaunchGame_LeftClick(object sender, EventArgs e);

        protected abstract void BtnLeaveGame_LeftClick(object sender, EventArgs e);

        protected void LoadDefaultMap()
        {
            if (ddGameMode.Items.Count > 0)
            {
                ddGameMode.SelectedIndex = 0;

                lbMapList.SelectedIndex = 0;
            }
        }

#if YR
        protected void CheckRa2Mode()
        {
            // TODO obsolete, remove when it's certain that this is not needed anywhere

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                if (checkBox.Name == "chkRA2Mode" && checkBox.Checked)
                {
                    RA2Mode = true;
                }
                else if (checkBox.Name == "chkRA2Mode" && !checkBox.Checked)
                {
                    RA2Mode = false;
                }
            }
        }
#endif

        /// <summary>
        /// Applies disallowed side indexes to the side option drop-downs
        /// and player options.
        /// </summary>
        protected void CheckDisallowedSides()
        {
            var disallowedSideArray = GetDisallowedSides();

            for (int i = 0; i < disallowedSideArray.Length; i++)
            {
                bool disabled = disallowedSideArray[i];

                if (disabled)
                {
                    foreach (XNADropDown dd in ddPlayerSides)
                    {
                        dd.Items[i + 1].Selectable = false;
                    }

                    var concatPlayerList = Players.Concat(AIPlayers);
                    foreach (PlayerInfo pInfo in concatPlayerList)
                    {
                        if (pInfo.SideId == i + 1)
                            pInfo.SideId = 0;
                    }
                }
                else
                {
                    foreach (XNADropDown dd in ddPlayerSides)
                    {
                        dd.Items[i + 1].Selectable = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a list of side indexes that are disallowed because of the current
        /// game options.
        /// </summary>
        /// <returns>A list of disallowed side indexes.</returns>
        protected bool[] GetDisallowedSides()
        {
            var returnValue = new bool[SideCount];

            foreach (var checkBox in CheckBoxes)
                checkBox.ApplyDisallowedSideIndex(returnValue);

            return returnValue;
        }

        /// <summary>
        /// Randomizes options of both human and AI players
        /// and returns the options as an array of PlayerHouseInfos.
        /// </summary>
        /// <returns>An array of PlayerHouseInfos.</returns>
        protected virtual PlayerHouseInfo[] Randomize()
        {
            int totalPlayerCount = Players.Count + AIPlayers.Count;
            PlayerHouseInfo[] houseInfos = new PlayerHouseInfo[totalPlayerCount];

            for (int i = 0; i < totalPlayerCount; i++)
                houseInfos[i] = new PlayerHouseInfo();

            // Gather list of spectators
            for (int i = 0; i < Players.Count; i++)
            {
                houseInfos[i].IsSpectator = Players[i].SideId == _sideCount + 1;
            }

            // Gather list of available colors

            List<int> freeColors = new List<int>();

            for (int cId = 0; cId < MPColors.Count; cId++)
                freeColors.Add(cId);

            if (Map.CoopInfo != null)
            {
                foreach (int colorIndex in Map.CoopInfo.DisallowedPlayerColors)
                    freeColors.Remove(colorIndex);
            }

            foreach (PlayerInfo player in Players)
                freeColors.Remove(player.ColorId - 1); // The first color is Random

            foreach (PlayerInfo aiPlayer in AIPlayers)
                freeColors.Remove(aiPlayer.ColorId - 1);

            // Gather list of available starting locations

            List<int> freeStartingLocations = new List<int>();
            List<int> takenStartingLocations = new List<int>();

            for (int i = 0; i < Map.MaxPlayers; i++)
                freeStartingLocations.Add(i);

            for (int i = 0; i < Players.Count; i++)
            {
                if (!houseInfos[i].IsSpectator)
                {
                    freeStartingLocations.Remove(Players[i].StartingLocation - 1);
                    //takenStartingLocations.Add(Players[i].StartingLocation - 1);
                    // ^ Gives everyone with a selected location a completely random
                    // location in-game, because PlayerHouseInfo.RandomizeStart already
                    // fills the list itself
                }
            }

            for (int i = 0; i < AIPlayers.Count; i++)
            {
                freeStartingLocations.Remove(AIPlayers[i].StartingLocation - 1);
            }

            // Randomize options

            Random random = new Random(RandomSeed);

            int fakeStartingLocationCount = 0;

            for (int i = 0; i < totalPlayerCount; i++)
            {
                PlayerInfo pInfo;
                PlayerHouseInfo pHouseInfo = houseInfos[i];

                if (i < Players.Count)
                {
                    pInfo = Players[i];
                }
                else
                    pInfo = AIPlayers[i - Players.Count];

                pHouseInfo.RandomizeSide(pInfo, Map, _sideCount, random, GetDisallowedSides());

                pHouseInfo.RandomizeColor(pInfo, freeColors, MPColors, random);
                if (pHouseInfo.RandomizeStart(pInfo, Map, freeStartingLocations, random,
                    fakeStartingLocationCount, takenStartingLocations))
                {
                    fakeStartingLocationCount++;
                }
            }

            return houseInfos;
        }

        /// <summary>
        /// Writes spawn.ini. Returns the player house info returned from the randomizer.
        /// </summary>
        private PlayerHouseInfo[] WriteSpawnIni()
        {
            Logger.Log("Writing spawn.ini");

            File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);

            if (Map.IsCoop)
            {
                foreach (PlayerInfo pInfo in Players)
                    pInfo.TeamId = 1;

                foreach (PlayerInfo pInfo in AIPlayers)
                    pInfo.TeamId = 1;
            }

            PlayerHouseInfo[] houseInfos = Randomize();

            IniFile spawnIni = new IniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);

            spawnIni.SetStringValue("Settings", "Name", ProgramConstants.PLAYERNAME);
            spawnIni.SetStringValue("Settings", "Scenario", ProgramConstants.SPAWNMAP_INI);
            spawnIni.SetStringValue("Settings", "UIGameMode", GameMode.UIName);
            spawnIni.SetStringValue("Settings", "UIMapName", Map.Name);
            spawnIni.SetIntValue("Settings", "PlayerCount", Players.Count);
            int myIndex = Players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME);
            spawnIni.SetIntValue("Settings", "Side", houseInfos[myIndex].SideIndex);
            spawnIni.SetBooleanValue("Settings", "IsSpectator", houseInfos[myIndex].IsSpectator);
            spawnIni.SetIntValue("Settings", "Color", houseInfos[myIndex].ColorIndex);
            spawnIni.SetStringValue("Settings", "CustomLoadScreen", LoadingScreenController.GetLoadScreenName(houseInfos[myIndex].SideIndex));
            spawnIni.SetIntValue("Settings", "AIPlayers", AIPlayers.Count);
            spawnIni.SetIntValue("Settings", "Seed", RandomSeed);
            WriteSpawnIniAdditions(spawnIni);

            foreach (GameLobbyCheckBox chkBox in CheckBoxes)
            {
                chkBox.ApplySpawnINICode(spawnIni);
            }

            foreach (GameLobbyDropDown dd in DropDowns)
            {
                dd.ApplySpawnIniCode(spawnIni);
            }

            // Apply forced options from GameOptions.ini

            List<string> forcedKeys = GameOptionsIni.GetSectionKeys("ForcedSpawnIniOptions");

            if (forcedKeys != null)
            {
                foreach (string key in forcedKeys)
                {
                    spawnIni.SetStringValue("Settings", key,
                        GameOptionsIni.GetStringValue("ForcedSpawnIniOptions", key, String.Empty));
                }
            }

            GameMode.ApplySpawnIniCode(spawnIni); // Forced options from the game mode
            Map.ApplySpawnIniCode(spawnIni, Players.Count + AIPlayers.Count, 
                AIPlayers.Count, GameMode.CoopDifficultyLevel); // Forced options from the map

            // Player options

            int otherId = 1;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];
                PlayerHouseInfo pHouseInfo = houseInfos[pId];

                if (pInfo.Name == ProgramConstants.PLAYERNAME)
                    continue;

                string sectionName = "Other" + otherId;

                spawnIni.SetStringValue(sectionName, "Name", pInfo.Name);
                spawnIni.SetIntValue(sectionName, "Side", pHouseInfo.SideIndex);
                spawnIni.SetBooleanValue(sectionName, "IsSpectator", pHouseInfo.IsSpectator);
                spawnIni.SetIntValue(sectionName, "Color", pHouseInfo.ColorIndex);
                spawnIni.SetStringValue(sectionName, "Ip", GetIPAddressForPlayer(pInfo));
                spawnIni.SetIntValue(sectionName, "Port", pInfo.Port);

                otherId++;
            }

            List<int> multiCmbIndexes = new List<int>();

            for (int cId = 0; cId < MPColors.Count; cId++)
            {
                for (int pId = 0; pId < Players.Count; pId++)
                {
                    if (houseInfos[pId].ColorIndex == MPColors[cId].GameColorIndex)
                        multiCmbIndexes.Add(pId);
                }
            }

            if (AIPlayers.Count > 0)
            {
                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
                    int multiId = multiCmbIndexes.Count + aiId + 1;

                    string keyName = "Multi" + multiId;

                    spawnIni.SetIntValue("HouseHandicaps", keyName, AIPlayers[aiId].AILevel);
                    spawnIni.SetIntValue("HouseCountries", keyName, houseInfos[Players.Count + aiId].SideIndex);
                    spawnIni.SetIntValue("HouseColors", keyName, houseInfos[Players.Count + aiId].ColorIndex);
                }
            }

            for (int multiId = 0; multiId < multiCmbIndexes.Count; multiId++)
            {
                int pIndex = multiCmbIndexes[multiId];
                if (houseInfos[pIndex].IsSpectator)
                    spawnIni.SetBooleanValue("IsSpectator", "Multi" + (multiId + 1), true);
            }

            // Write alliances, the code is pretty big so let's take it to another class
            AllianceHolder.WriteInfoToSpawnIni(Players, AIPlayers, multiCmbIndexes, spawnIni);

            for (int pId = 0; pId < Players.Count; pId++)
            {
                int startingWaypoint = houseInfos[multiCmbIndexes[pId]].StartingWaypoint;

                // -1 means no starting location at all - let the game itself pick the starting location
                // using its own logic
                if (startingWaypoint > -1)
                {
                    int multiIndex = pId + 1;
                    spawnIni.SetIntValue("SpawnLocations", "Multi" + multiIndex,
                        startingWaypoint);
                }
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                int startingWaypoint = houseInfos[Players.Count + aiId].StartingWaypoint;

                if (startingWaypoint > -1)
                {
                    int multiIndex = Players.Count + aiId + 1;
                    spawnIni.SetIntValue("SpawnLocations", "Multi" + multiIndex,
                        startingWaypoint);
                }
            }

            spawnIni.WriteIniFile();

            return houseInfos;
        }

        protected virtual string GetIPAddressForPlayer(PlayerInfo player)
        {
            return "0.0.0.0";
        }

        /// <summary>
        /// Override this in a derived class to write game lobby specific code to
        /// spawn.ini. For example, CnCNet game lobbies should write tunnel info
        /// in this method.
        /// </summary>
        /// <param name="iniFile">The spawn INI file.</param>
        protected virtual void WriteSpawnIniAdditions(IniFile iniFile)
        {
            // Do nothing by default
        }

        private void InitializeMatchStatistics(PlayerHouseInfo[] houseInfos)
        {
            matchStatistics = new MatchStatistics(ProgramConstants.GAME_VERSION, UniqueGameID,
                Map.Name, GameMode.UIName, Players.Count);

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];
                matchStatistics.AddPlayer(pInfo.Name, pInfo.Name == ProgramConstants.PLAYERNAME,
                    false, pInfo.SideId == _sideCount + 1, houseInfos[pId].SideIndex + 1, pInfo.TeamId, 
                    MPColors.FindIndex(c => c.GameColorIndex == houseInfos[pId].ColorIndex), 10);
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                var pHouseInfo = houseInfos[Players.Count + aiId];
                PlayerInfo aiInfo = AIPlayers[aiId];
                matchStatistics.AddPlayer("Computer", false, true, false, 
                    pHouseInfo.SideIndex + 1, aiInfo.TeamId, 
                    MPColors.FindIndex(c => c.GameColorIndex == pHouseInfo.ColorIndex),
                    aiInfo.ReversedAILevel);
            }
        }

        /// <summary>
        /// Writes spawnmap.ini.
        /// </summary>
        private void WriteMap(PlayerHouseInfo[] houseInfos)
        {
            File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);

            Logger.Log("Writing map.");

            Logger.Log("Loading map INI from " +
                ProgramConstants.GamePath + Map.BaseFilePath + ".map");

            IniFile mapIni = Map.GetMapIni();

            IniFile globalCodeIni = new IniFile(ProgramConstants.GamePath + "INI\\Map Code\\GlobalCode.ini");

            IniFile.ConsolidateIniFiles(mapIni, GameMode.GetMapRulesIniFile());
            IniFile.ConsolidateIniFiles(mapIni, globalCodeIni);

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                checkBox.ApplyMapCode(mapIni);

            mapIni.MoveSectionToFirst("MultiplayerDialogSettings"); // Required by YR

            // Add "fake" starting locations if needed, makes it possible to have multiple
            // players start on the same location in DTA and all TS mods with more than 2 sides
            foreach (PlayerHouseInfo houseInfo in houseInfos)
            {
                if (houseInfo.StartingWaypoint > -1 && 
                    houseInfo.RealStartingWaypoint != houseInfo.StartingWaypoint)
                {
                    mapIni.SetIntValue("Waypoints", houseInfo.StartingWaypoint.ToString(),
                        mapIni.GetIntValue("Waypoints", houseInfo.RealStartingWaypoint.ToString(), 0));
                }
            }

            mapIni.WriteIniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);
        }

        /// <summary>
        /// Writes spawn.ini, writes the map file, initializes statistics and
        /// starts the game process.
        /// </summary>
        protected virtual void StartGame()
        {
            PlayerHouseInfo[] houseInfos = WriteSpawnIni();
            InitializeMatchStatistics(houseInfos);
            WriteMap(houseInfos);

            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess();
        }

        private void GameProcessExited_Callback()
        {
            AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

            Logger.Log("GameProcessExited: Parsing statistics.");

            matchStatistics.ParseStatistics(ProgramConstants.GamePath, ClientConfiguration.Instance.LocalGame);

            Logger.Log("GameProcessExited: Adding match to statistics.");

            StatisticsManager.Instance.AddMatchAndSaveDatabase(true, matchStatistics);

            ClearReadyStatuses();

            CopyPlayerDataToUI();
        }

        /// <summary>
        /// "Copies" player information from the UI to internal memory,
        /// applying users' player options changes.
        /// </summary>
        protected virtual void CopyPlayerDataFromUI(object sender, EventArgs e)
        {
            if (PlayerUpdatingInProgress)
                return;

            var senderDropDown = (XNADropDown)sender;
            if ((bool)senderDropDown.Tag)
                ClearReadyStatuses();

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                pInfo.ColorId = ddPlayerColors[pId].SelectedIndex;
                pInfo.SideId = ddPlayerSides[pId].SelectedIndex;
                pInfo.StartingLocation = ddPlayerStarts[pId].SelectedIndex;
                pInfo.TeamId = ddPlayerTeams[pId].SelectedIndex;

                if (pInfo.SideId == _sideCount + 1)
                    pInfo.StartingLocation = 0;

                XNADropDown ddName = ddPlayerNames[pId];
                
                switch (ddName.SelectedIndex)
                {
                    case 0:
                        break;
                    case 1:
                        ddName.SelectedIndex = 0;
                        break;
                    case 2:
                        KickPlayer(pId);
                        break;
                    case 3:
                        BanPlayer(pId);
                        break;
                }
            }

            AIPlayers.Clear();
            for (int cmbId = Players.Count; cmbId < 8; cmbId++)
            {
                XNADropDown dd = ddPlayerNames[cmbId];
                dd.Items[0].Text = "-";

                if (dd.SelectedIndex < 1)
                    continue;

                PlayerInfo aiPlayer = new PlayerInfo
                {
                    Name = dd.Items[dd.SelectedIndex].Text,
                    AILevel = 2 - (dd.SelectedIndex - 1),
                    SideId = Math.Max(ddPlayerSides[cmbId].SelectedIndex, 0),
                    ColorId = Math.Max(ddPlayerColors[cmbId].SelectedIndex, 0),
                    StartingLocation = Math.Max(ddPlayerStarts[cmbId].SelectedIndex, 0),
                    TeamId = Math.Max(ddPlayerTeams[cmbId].SelectedIndex, 0),
                    IsAI = true
                };

                AIPlayers.Add(aiPlayer);
            }

            CopyPlayerDataToUI();
        }

        /// <summary>
        /// Sets the ready status of all non-host human players to false.
        /// </summary>
        protected void ClearReadyStatuses()
        {
            for (int i = 1; i < Players.Count; i++)
                Players[i].Ready = false;
        }

        /// <summary>
        /// Applies player information changes done in memory to the UI.
        /// </summary>
        protected virtual void CopyPlayerDataToUI()
        {
            PlayerUpdatingInProgress = true;

            bool allowOptionsChange = AllowPlayerOptionsChange();

            // Human players
            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                pInfo.Index = pId;

                XNADropDown ddPlayerName = ddPlayerNames[pId];
                ddPlayerName.Items[0].Text = pInfo.Name;
                ddPlayerName.Items[1].Text = string.Empty;
                ddPlayerName.Items[2].Text = "Kick";
                ddPlayerName.Items[3].Text = "Ban";
                ddPlayerName.SelectedIndex = 0;
                ddPlayerName.AllowDropDown = false;

                bool allowPlayerOptionsChange = allowOptionsChange || pInfo.Name == ProgramConstants.PLAYERNAME;

                ddPlayerSides[pId].SelectedIndex = pInfo.SideId;
                ddPlayerSides[pId].AllowDropDown = allowPlayerOptionsChange;

                ddPlayerColors[pId].SelectedIndex = pInfo.ColorId;
                ddPlayerColors[pId].AllowDropDown = allowPlayerOptionsChange;

                ddPlayerStarts[pId].SelectedIndex = pInfo.StartingLocation;
                ddPlayerStarts[pId].AllowDropDown = allowPlayerOptionsChange;

                ddPlayerTeams[pId].SelectedIndex = pInfo.TeamId;
                if (Map != null)
                    ddPlayerTeams[pId].AllowDropDown = allowPlayerOptionsChange && !Map.IsCoop;
            }

            // AI players
            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                PlayerInfo aiInfo = AIPlayers[aiId];

                int index = Players.Count + aiId;

                aiInfo.Index = index;

                XNADropDown ddPlayerName = ddPlayerNames[index];
                ddPlayerName.Items[0].Text = "-";
                ddPlayerName.Items[1].Text = "Easy AI";
                ddPlayerName.Items[2].Text = "Medium AI";
                ddPlayerName.Items[3].Text = "Hard AI";
                ddPlayerName.SelectedIndex = 3 - aiInfo.AILevel;
                ddPlayerName.AllowDropDown = allowOptionsChange;

                ddPlayerSides[index].SelectedIndex = aiInfo.SideId;
                ddPlayerSides[index].AllowDropDown = allowOptionsChange;

                ddPlayerColors[index].SelectedIndex = aiInfo.ColorId;
                ddPlayerColors[index].AllowDropDown = allowOptionsChange;

                ddPlayerStarts[index].SelectedIndex = aiInfo.StartingLocation;
                ddPlayerStarts[index].AllowDropDown = allowOptionsChange;

                ddPlayerTeams[index].SelectedIndex = aiInfo.TeamId;

                if (Map != null)
                    ddPlayerTeams[index].AllowDropDown = allowOptionsChange && !Map.IsCoop;
            }

            // Unused player slots
            for (int ddIndex = Players.Count + AIPlayers.Count; ddIndex < MAX_PLAYER_COUNT; ddIndex++)
            {
                XNADropDown ddPlayerName = ddPlayerNames[ddIndex];
                ddPlayerName.AllowDropDown = false;
                ddPlayerName.Items[0].Text = string.Empty;
                ddPlayerName.Items[1].Text = "Easy AI";
                ddPlayerName.Items[2].Text = "Medium AI";
                ddPlayerName.Items[3].Text = "Hard AI";
                ddPlayerName.SelectedIndex = 0;

                ddPlayerSides[ddIndex].SelectedIndex = -1;
                ddPlayerSides[ddIndex].AllowDropDown = false;

                ddPlayerColors[ddIndex].SelectedIndex = -1;
                ddPlayerColors[ddIndex].AllowDropDown = false;

                ddPlayerStarts[ddIndex].SelectedIndex = -1;
                ddPlayerStarts[ddIndex].AllowDropDown = false;

                ddPlayerTeams[ddIndex].SelectedIndex = -1;
                ddPlayerTeams[ddIndex].AllowDropDown = false;
            }

            if (allowOptionsChange && Players.Count + AIPlayers.Count < MAX_PLAYER_COUNT)
                ddPlayerNames[Players.Count + AIPlayers.Count].AllowDropDown = true;

            MapPreviewBox.UpdateStartingLocationTexts();

            PlayerUpdatingInProgress = false;
        }

        /// <summary>
        /// Override this in a derived class to kick players.
        /// </summary>
        /// <param name="playerIndex">The index of the player that should be kicked.</param>
        protected virtual void KickPlayer(int playerIndex)
        {
            // Do nothing by default
        }

        /// <summary>
        /// Override this in a derived class to ban players.
        /// </summary>
        /// <param name="playerIndex">The index of the player that should be banned.</param>
        protected virtual void BanPlayer(int playerIndex)
        {
            // Do nothing by default
        }

        /// <summary>
        /// Changes the current map and game mode.
        /// </summary>
        /// <param name="gameMode">The new game mode.</param>
        /// <param name="map">The new map.</param>
        protected virtual void ChangeMap(GameMode gameMode, Map map)
        {
            var oldGameMode = GameMode;
            GameMode = gameMode;

            Map = map;

            if (GameMode == null || Map == null)
            {
                lblMapName.Text = "Map: Unknown";
                lblMapAuthor.Text = "By Unknown Author";
                lblGameMode.Text = "Game mode: Unknown";

                lblMapAuthor.ClientRectangle = new Rectangle(MapPreviewBox.ClientRectangle.Right - lblMapAuthor.ClientRectangle.Width,
                    lblMapAuthor.ClientRectangle.Y, lblMapAuthor.ClientRectangle.Width, lblMapAuthor.ClientRectangle.Height);

                MapPreviewBox.Map = null;

                return;
            }

            lblMapName.Text = "Map: " + Renderer.GetSafeString(map.Name, lblMapName.FontIndex);
            lblMapAuthor.Text = "By " + Renderer.GetSafeString(map.Author, lblMapAuthor.FontIndex);
            lblGameMode.Text = "Game mode: " + gameMode.UIName;

            lblMapAuthor.ClientRectangle = new Rectangle(MapPreviewBox.ClientRectangle.Right - lblMapAuthor.ClientRectangle.Width,
                lblMapAuthor.ClientRectangle.Y, lblMapAuthor.ClientRectangle.Width, lblMapAuthor.ClientRectangle.Height);

            mapChangeInProgress = true;

            // Clear forced options
            foreach (var ddGameOption in DropDowns)
                ddGameOption.AllowDropDown = true;

            foreach (var checkBox in CheckBoxes)
                checkBox.AllowChecking = true;

            // Apply default options if we should

            //if (GameMode.LoadDefaultSettingsOnMapChange ||
            //    (oldGameMode != null && oldGameMode.LoadDefaultSettingsOnMapChange))
            //{
            //    foreach (var ddGameOption in DropDowns)
            //        ddGameOption.SetDefaultValue();

            //    foreach (var checkBox in CheckBoxes)
            //        checkBox.SetDefaultValue();
            //}

            // We could either pass the CheckBoxes and DropDowns of this class
            // to the Map and GameMode instances and let them apply their forced
            // options, or we could do it in this class with helper functions.
            // The second approach is probably clearer.

            // We use these temp lists to determine which options WERE NOT forced
            // by the map. We then return these to user-defined settings.
            // This prevents forced options from one map getting carried
            // to other maps.

            var checkBoxListClone = new List<GameLobbyCheckBox>(CheckBoxes);
            var dropDownListClone = new List<GameLobbyDropDown>(DropDowns);

            ApplyForcedCheckBoxOptions(checkBoxListClone, gameMode.ForcedCheckBoxValues);
            ApplyForcedCheckBoxOptions(checkBoxListClone, map.ForcedCheckBoxValues);

            ApplyForcedDropDownOptions(dropDownListClone, gameMode.ForcedDropDownValues);
            ApplyForcedDropDownOptions(dropDownListClone, map.ForcedDropDownValues);

            foreach (var chkBox in checkBoxListClone)
                chkBox.Checked = chkBox.UserDefinedValue;

            foreach (var dd in dropDownListClone)
                dd.SelectedIndex = dd.UserDefinedIndex;

            // Enable all sides by default
            foreach (var ddSide in ddPlayerSides)
            {
                ddSide.Items.ForEach(item => item.Selectable = true);
            }

            // Enable all colors by default
            foreach (var ddColor in ddPlayerColors)
            {
                ddColor.Items.ForEach(item => item.Selectable = true);
            }

            // Apply starting locations
            foreach (var ddStart in ddPlayerStarts)
            {
                ddStart.Items.Clear();

                ddStart.AddItem("???");

                for (int i = 1; i <= Map.MaxPlayers; i++)
                    ddStart.AddItem(i.ToString());
            }

            IEnumerable<PlayerInfo> concatPlayerList = Players.Concat(AIPlayers);

            foreach (PlayerInfo pInfo in concatPlayerList)
            {
                if (pInfo.StartingLocation > Map.MaxPlayers)
                    pInfo.StartingLocation = 0;
            }

            if (map.CoopInfo != null)
            {
                // Co-Op map disallowed side logic

                List<int> disallowedSides = new List<int>(map.CoopInfo.DisallowedPlayerSides);
                disallowedSides.Add(_sideCount); // Disallow spectator

                bool disallowRandom = _sideCount == disallowedSides.Count; // Disallow Random if only 1 side is allowed
                int defaultSideIndex = 0; // The side to switch to if we're currently using a disallowed side. 0 = random

                if (disallowRandom)
                {
                    for (int sideIndex = 0; sideIndex < _sideCount; sideIndex++)
                    {
                        if (!disallowedSides.Contains(sideIndex))
                        {
                            defaultSideIndex = sideIndex + 1;
                            break;
                        }
                    }

                    foreach (XNADropDown dd in ddPlayerSides)
                    {
                        dd.Items[0].Selectable = false;
                    }

                    foreach (PlayerInfo pInfo in concatPlayerList)
                    {
                        if (pInfo.SideId == 0)
                            pInfo.SideId = defaultSideIndex;
                    }
                }

                foreach (int disallowedSideIndex in disallowedSides)
                {
                    foreach (XNADropDown ddSide in ddPlayerSides)
                    {
                        if (ddSide.Items.Count - 1 <= disallowedSideIndex)
                            continue;

                        ddSide.Items[disallowedSideIndex + 1].Selectable = false;
                    }

                    foreach (PlayerInfo pInfo in concatPlayerList)
                    {
                        if (pInfo.SideId == disallowedSideIndex + 1)
                            pInfo.SideId = defaultSideIndex;
                    }
                }

                // Co-Op map disallowed color logic
                foreach (int disallowedColorIndex in map.CoopInfo.DisallowedPlayerColors)
                {
                    if (disallowedColorIndex >= MPColors.Count)
                        continue;

                    foreach (XNADropDown ddColor in ddPlayerColors)
                        ddColor.Items[disallowedColorIndex + 1].Selectable = false;

                    foreach (PlayerInfo pInfo in concatPlayerList)
                    {
                        if (pInfo.ColorId == disallowedColorIndex + 1)
                            pInfo.ColorId = 0;
                    }
                }

                // Force teams
                foreach (PlayerInfo pInfo in concatPlayerList)
                    pInfo.TeamId = 1;
            }

            OnGameOptionChanged();

            MapPreviewBox.Map = map;
            CopyPlayerDataToUI();

            mapChangeInProgress = false;
        }

        private void ApplyForcedCheckBoxOptions(List<GameLobbyCheckBox> optionList,
            List<KeyValuePair<string, bool>> forcedOptions)
        {
            foreach (KeyValuePair<string, bool> option in forcedOptions)
            {
                GameLobbyCheckBox checkBox = CheckBoxes.Find(chk => chk.Name == option.Key);
                if (checkBox != null)
                {
                    checkBox.Checked = option.Value;
                    checkBox.AllowChecking = false;
                    optionList.Remove(checkBox);
                }
            }
        }

        private void ApplyForcedDropDownOptions(List<GameLobbyDropDown> optionList,
            List<KeyValuePair<string, int>> forcedOptions)
        {
            foreach (KeyValuePair<string, int> option in forcedOptions)
            {
                GameLobbyDropDown dropDown = DropDowns.Find(dd => dd.Name == option.Key);
                if (dropDown != null)
                {
                    dropDown.SelectedIndex = option.Value;
                    dropDown.AllowDropDown = false;
                    optionList.Remove(dropDown);
                }
            }
        }

        protected string AILevelToName(int aiLevel)
        {
            switch (aiLevel)
            {
                case 0:
                    return "Hard AI";
                case 1:
                    return "Medium AI";
                case 2:
                    return "Easy AI";
            }

            return string.Empty;
        }

        protected abstract bool AllowPlayerOptionsChange();
    }
}
