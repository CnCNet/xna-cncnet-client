using ClientGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.DXControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using ClientCore;
using DTAClient.domain.CnCNet;
using System.IO;

namespace DTAClient.DXGUI.GameLobby
{
    /// <summary>
    /// A generic base for all game lobbies (Skirmish, LAN and CnCNet).
    /// Contains the common logic for parsing game options and handling player info.
    /// </summary>
    public abstract class GameLobbyBase : DXWindow
    {
        const int PLAYER_COUNT = 8;
        const int PLAYER_OPTION_VERTICAL_MARGIN = 3;
        const int PLAYER_OPTION_HORIZONTAL_MARGIN = 5;
        const int PLAYER_OPTION_CAPTION_Y = 5;
        const int DROP_DOWN_HEIGHT = 21;

        /// <summary>
        /// Creates a new instance of the game lobby base.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="iniName">The name of the lobby in GameOptions.ini.</param>
        public GameLobbyBase(WindowManager windowManager, string iniName, List<GameMode> GameModes) : base(windowManager)
        {
            _iniSectionName = iniName;
            this.GameModes = GameModes;
        }

        string _iniSectionName;

        DXPanel PlayerOptionsPanel;

        DXPanel GameOptionsPanel;

        protected List<MultiplayerColor> MPColors = new List<MultiplayerColor>();

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

        protected DXDropDown[] ddPlayerNames;
        protected DXDropDown[] ddPlayerSides;
        protected DXDropDown[] ddPlayerColors;
        protected DXDropDown[] ddPlayerStarts;
        protected DXDropDown[] ddPlayerTeams;

        protected DXLabel lblName;
        protected DXLabel lblSide;
        protected DXLabel lblColor;
        protected DXLabel lblStart;
        protected DXLabel lblTeam;

        protected DXButton btnLeaveGame;
        protected DXButton btnLaunchGame;
        protected DXLabel lblMapName;
        protected DXLabel lblMapAuthor;

        protected MapPreviewBox MapPreviewBox;

        protected List<PlayerInfo> Players = new List<PlayerInfo>();
        protected List<PlayerInfo> AIPlayers = new List<PlayerInfo>();

        protected bool PlayerUpdatingInProgress { get; set; }

        /// <summary>
        /// The seed used for randomizing player options.
        /// </summary>
        protected int RandomSeed { get; set; }

        private int _sideCount;

        IniFile _gameOptionsIni;
        protected IniFile GameOptionsIni
        {
            get { return _gameOptionsIni; }
        }

        public override void Initialize()
        {
            Name = _iniSectionName;
            ClientRectangle = new Rectangle(0, 0, WindowManager.Instance.RenderResolutionX, WindowManager.Instance.RenderResolutionY);

            btnLeaveGame = new DXButton(WindowManager);
            btnLeaveGame.Name = "btnLeaveGame";
            btnLeaveGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnLeaveGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnLeaveGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnLeaveGame.ClientRectangle = new Rectangle(ClientRectangle.Width - 5, ClientRectangle.Height - 28, 133, 23);
            btnLeaveGame.FontIndex = 1;
            btnLeaveGame.Text = "Leave Game";
            btnLeaveGame.LeftClick += BtnLeaveGame_LeftClick;

            btnLaunchGame = new DXButton(WindowManager);
            btnLaunchGame.Name = "btnLaunchGame";
            btnLaunchGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnLaunchGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnLaunchGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnLaunchGame.ClientRectangle = new Rectangle(1, btnLeaveGame.ClientRectangle.Y, 133, 23);
            btnLaunchGame.FontIndex = 1;
            btnLaunchGame.Text = "Launch Game";
            btnLaunchGame.LeftClick += BtnLaunchGame_LeftClick;

            GameOptionsPanel = new DXPanel(WindowManager);
            GameOptionsPanel.Name = "GameOptionsPanel";
            GameOptionsPanel.BackgroundTexture = AssetLoader.LoadTexture("gamelobbyoptionspanelbg.png");
            GameOptionsPanel.ClientRectangle = new Rectangle(1, 1, 433, 235);

            PlayerOptionsPanel = new DXPanel(WindowManager);
            PlayerOptionsPanel.Name = "PlayerOptionsPanel";
            PlayerOptionsPanel.BackgroundTexture = AssetLoader.LoadTexture("gamelobbypanelbg.png");
            PlayerOptionsPanel.ClientRectangle = new Rectangle(441, 1, 553, 235);

            MapPreviewBox = new MapPreviewBox(WindowManager, Players, AIPlayers, MPColors);
            MapPreviewBox.Name = "MapPreviewBox";
            MapPreviewBox.ClientRectangle = new Rectangle(PlayerOptionsPanel.ClientRectangle.X,
                PlayerOptionsPanel.ClientRectangle.Bottom + 30,
                WindowManager.RenderResolutionX - PlayerOptionsPanel.ClientRectangle.X - 10,
                WindowManager.RenderResolutionY - PlayerOptionsPanel.ClientRectangle.Bottom - 60);

            lblMapName = new DXLabel(WindowManager);
            lblMapName.Name = "lblMapName";
            lblMapName.ClientRectangle = new Rectangle(MapPreviewBox.ClientRectangle.X,
                MapPreviewBox.ClientRectangle.Bottom + 3, 0, 0);
            lblMapName.Text = "Map:";

            lblMapAuthor = new DXLabel(WindowManager);
            lblMapAuthor.Name = "lblMapAuthor";
            lblMapAuthor.ClientRectangle = new Rectangle(MapPreviewBox.ClientRectangle.Right,
                lblMapName.ClientRectangle.Y, 0, 0);
            lblMapAuthor.Text = "By ";

            _gameOptionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + "GameOptions.ini");

            // Load multiplayer colors
            List<string> colorKeys = GameOptionsIni.GetSectionKeys("MPColors");

            foreach (string key in colorKeys)
            {
                string[] values = GameOptionsIni.GetStringValue("MPColors", key, "255,255,255,0").Split(',');

                try
                {
                    MultiplayerColor mpColor = MultiplayerColor.CreateFromStringArray(key, values);

                    MPColors.Add(mpColor);
                }
                catch
                {
                    throw new Exception("Invalid MPColor specified in GameOptions.ini: " + key);
                }
            }

            string[] checkBoxes = GameOptionsIni.GetStringValue(_iniSectionName, "CheckBoxes", String.Empty).Split(',');

            foreach (string chkName in checkBoxes)
            {
                GameLobbyCheckBox chkBox = new GameLobbyCheckBox(WindowManager);
                chkBox.Name = chkName;
                chkBox.GetAttributes(GameOptionsIni);
                CheckBoxes.Add(chkBox);
                GameOptionsPanel.AddChild(chkBox);
            }

            string[] labels = GameOptionsIni.GetStringValue(_iniSectionName, "Labels", String.Empty).Split(',');

            foreach (string labelName in labels)
            {
                DXLabel label = new DXLabel(WindowManager);
                label.Name = labelName;
                label.GetAttributes(GameOptionsIni);
                GameOptionsPanel.AddChild(label);
            }

            string[] dropDowns = GameOptionsIni.GetStringValue(_iniSectionName, "DropDowns", String.Empty).Split(',');

            foreach (string ddName in dropDowns)
            {
                GameLobbyDropDown dropdown = new GameLobbyDropDown(WindowManager);
                dropdown.Name = ddName;
                dropdown.GetAttributes(GameOptionsIni);
                DropDowns.Add(dropdown);
                GameOptionsPanel.AddChild(dropdown);
            }

            AddChild(GameOptionsPanel);
            AddChild(PlayerOptionsPanel);

            base.Initialize();
        }

        /// <summary>
        /// Initializes the player option drop-down controls.
        /// </summary>
        /// <param name="playerNameWidth">The width of the "player name" drop-down control.</param>
        /// <param name="sideWidth">The width of the "player side" drop-down control.</param>
        /// <param name="colorWidth">The width of the "player color" drop-down control.</param>
        /// <param name="startWidth">The width of the "player starting location" drop-down control.</param>
        /// <param name="teamWidth">The width of the "player team" drop-down control.</param>
        /// <param name="optionsPosition">The top-left base position of the player option controls.</param>
        protected void InitPlayerOptionDropdowns(int playerNameWidth, int sideWidth,
            int colorWidth, int startWidth, int teamWidth, Point optionsPosition)
        {
            ddPlayerNames = new DXDropDown[PLAYER_COUNT];
            ddPlayerSides = new DXDropDown[PLAYER_COUNT];
            ddPlayerColors = new DXDropDown[PLAYER_COUNT];
            ddPlayerStarts = new DXDropDown[PLAYER_COUNT];
            ddPlayerTeams = new DXDropDown[PLAYER_COUNT];

            string[] sides = GameOptionsIni.GetStringValue("General", "Sides", String.Empty).Split(',');
            _sideCount = sides.Length;

            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                DXDropDown ddPlayerName = new DXDropDown(WindowManager);
                ddPlayerName.Name = "ddPlayerName" + i;
                ddPlayerName.ClientRectangle = new Rectangle(optionsPosition.X,
                    optionsPosition.Y + (DROP_DOWN_HEIGHT + PLAYER_OPTION_VERTICAL_MARGIN) * i,
                    playerNameWidth, DROP_DOWN_HEIGHT);
                ddPlayerName.AddItem(String.Empty);
                ddPlayerName.AddItem("Easy AI");
                ddPlayerName.AddItem("Medium AI");
                ddPlayerName.AddItem("Hard AI");
                ddPlayerName.Enabled = false;
                ddPlayerName.SelectedIndexChanged += CopyPlayerDataFromUI;

                DXDropDown ddPlayerSide = new DXDropDown(WindowManager);
                ddPlayerSide.Name = "ddPlayerSide" + i;
                ddPlayerSide.ClientRectangle = new Rectangle(
                    ddPlayerName.ClientRectangle.Right + PLAYER_OPTION_HORIZONTAL_MARGIN,
                    ddPlayerName.ClientRectangle.Y, sideWidth, DROP_DOWN_HEIGHT);
                ddPlayerSide.AddItem("Random", AssetLoader.LoadTexture("randomicon.png"));
                foreach (string sideName in sides)
                    ddPlayerSide.AddItem(sideName, AssetLoader.LoadTexture(sideName + ".png"));
                ddPlayerSide.Enabled = false;
                ddPlayerSide.SelectedIndexChanged += CopyPlayerDataFromUI;

                DXDropDown ddPlayerColor = new DXDropDown(WindowManager);
                ddPlayerColor.Name = "ddPlayerColor" + i;
                ddPlayerColor.ClientRectangle = new Rectangle(
                    ddPlayerSide.ClientRectangle.Right + PLAYER_OPTION_HORIZONTAL_MARGIN,
                    ddPlayerName.ClientRectangle.Y, colorWidth, DROP_DOWN_HEIGHT);
                foreach (MultiplayerColor mpColor in MPColors)
                    ddPlayerColor.AddItem(mpColor.Name, mpColor.XnaColor);
                ddPlayerColor.Enabled = false;
                ddPlayerColor.SelectedIndexChanged += CopyPlayerDataFromUI;

                DXDropDown ddPlayerStart = new DXDropDown(WindowManager);
                ddPlayerStart.Name = "ddPlayerStart" + i;
                ddPlayerStart.ClientRectangle = new Rectangle(
                    ddPlayerColor.ClientRectangle.Right + PLAYER_OPTION_HORIZONTAL_MARGIN,
                    ddPlayerName.ClientRectangle.Y, startWidth, DROP_DOWN_HEIGHT);
                for (int j = 1; j < 9; j++)
                    ddPlayerStart.AddItem(j.ToString());
                ddPlayerStart.Enabled = false;
                ddPlayerStart.SelectedIndexChanged += CopyPlayerDataFromUI;

                DXDropDown ddPlayerTeam = new DXDropDown(WindowManager);
                ddPlayerTeam.Name = "ddPlayerTeam" + i;
                ddPlayerTeam.ClientRectangle = new Rectangle(
                    ddPlayerStart.ClientRectangle.Right + PLAYER_OPTION_HORIZONTAL_MARGIN,
                    ddPlayerName.ClientRectangle.Y, teamWidth, DROP_DOWN_HEIGHT);
                ddPlayerTeam.AddItem("-");
                ddPlayerTeam.AddItem("A");
                ddPlayerTeam.AddItem("B");
                ddPlayerTeam.AddItem("C");
                ddPlayerTeam.AddItem("D");
                ddPlayerTeam.Enabled = false;
                ddPlayerTeam.SelectedIndexChanged += CopyPlayerDataFromUI;

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

            lblName = new DXLabel(WindowManager);
            lblName.Name = "lblName";
            lblName.Text = "NAME";
            lblName.ClientRectangle = new Rectangle(ddPlayerNames[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            lblSide = new DXLabel(WindowManager);
            lblSide.Name = "lblSide";
            lblSide.Text = "SIDE";
            lblSide.ClientRectangle = new Rectangle(ddPlayerSides[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            lblColor = new DXLabel(WindowManager);
            lblColor.Name = "lblColor";
            lblColor.Text = "COLOR";
            lblColor.ClientRectangle = new Rectangle(ddPlayerColors[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            lblStart = new DXLabel(WindowManager);
            lblStart.Name = "lblStart";
            lblStart.Text = "START";
            lblStart.ClientRectangle = new Rectangle(ddPlayerStarts[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            lblTeam = new DXLabel(WindowManager);
            lblTeam.Name = "lblTeam";
            lblTeam.Text = "TEAM";
            lblTeam.ClientRectangle = new Rectangle(ddPlayerTeams[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            PlayerOptionsPanel.AddChild(lblName);
            PlayerOptionsPanel.AddChild(lblSide);
            PlayerOptionsPanel.AddChild(lblColor);
            PlayerOptionsPanel.AddChild(lblStart);
            PlayerOptionsPanel.AddChild(lblTeam);
        }

        protected abstract void BtnLaunchGame_LeftClick(object sender, EventArgs e);

        protected abstract void BtnLeaveGame_LeftClick(object sender, EventArgs e);

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

            foreach (PlayerInfo player in Players)
                freeColors.Remove(player.ColorId - 1); // The first color is Random

            foreach (PlayerInfo aiPlayer in AIPlayers)
                freeColors.Remove(aiPlayer.ColorId - 1);

            // Gather list of available starting locations

            List<int> freeStartingLocs = new List<int>();

            for (int i = 0; i < Map.MaxPlayers; i++)
                freeStartingLocs.Add(i);

            for (int i = 0; i < Players.Count; i++)
            {
                if (!houseInfos[i].IsSpectator)
                    freeStartingLocs.Remove(Players[i].StartingLocation - 1);
            }

            // Randomize options

            Random random = new Random(RandomSeed);

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

                if (pInfo.SideId == 0)
                {
                    // Randomize side

                    int sideId;

                    while (true)
                    {
                        sideId = random.Next(0, _sideCount);

                        if (Map.CoopInfo == null || !Map.CoopInfo.DisallowedPlayerSides.Contains(sideId))
                            break;
                    }

                    pHouseInfo.SideIndex = sideId;
                }
                else
                    pHouseInfo.SideIndex = pInfo.SideId - 1;

                if (pInfo.ColorId == 0)
                {
                    // Randomize color

                    int randomizedColorIndex = random.Next(0, freeColors.Count);
                    int actualColorId = freeColors[randomizedColorIndex];

                    pHouseInfo.ColorIndex = MPColors[actualColorId].GameColorIndex;
                    freeColors.RemoveAt(randomizedColorIndex);
                }
                else
                    pHouseInfo.ColorIndex = MPColors[pInfo.ColorId - 1].GameColorIndex;

                if (pInfo.StartingLocation == 0)
                {
                    // Randomize starting location

                    if (freeStartingLocs.Count == 0)
                        pHouseInfo.StartingWaypoint = random.Next(0, Map.MaxPlayers);
                    else
                    {
                        int waypointIndex = random.Next(0, freeStartingLocs.Count);
                        pHouseInfo.StartingWaypoint = freeStartingLocs[waypointIndex];
                        freeStartingLocs.RemoveAt(waypointIndex);
                    }
                }
                else
                    pHouseInfo.StartingWaypoint = pInfo.StartingLocation - 1;
            }

            return houseInfos;
        }

        /// <summary>
        /// Writes spawn.ini.
        /// </summary>
        protected virtual void WriteSpawnIni()
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
            Map.ApplySpawnIniCode(spawnIni); // Forced options from the map

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
                int multiId = multiCmbIndexes.Count + 1;

                string keyName = "Multi" + multiId;

                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
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
                int multiIndex = pId + 1;
                spawnIni.SetIntValue("SpawnLocations", "Multi" + multiIndex,
                    houseInfos[multiCmbIndexes[pId]].StartingWaypoint);
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                int multiIndex = Players.Count + aiId + 1;
                spawnIni.SetIntValue("SpawnLocations", "Multi" + multiIndex,
                    houseInfos[multiCmbIndexes[Players.Count + aiId]].StartingWaypoint);
            }

            spawnIni.WriteIniFile();
        }

        /// <summary>
        /// Writes spawnmap.ini.
        /// </summary>
        protected virtual void WriteMap()
        {
            File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);

            Logger.Log("Writing map.");

            IniFile mapIni = new IniFile(ProgramConstants.GamePath + Map.BaseFilePath + ".map");

            IniFile globalCodeIni = new IniFile(ProgramConstants.GamePath + "INI\\GlobalCode.ini");

            IniFile.ConsolidateIniFiles(mapIni, GameMode.GetMapRulesIniFile());
            IniFile.ConsolidateIniFiles(mapIni, globalCodeIni);

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                checkBox.ApplyMapCode(mapIni);

            mapIni.MoveSectionToFirst("MultiplayerDialogSettings"); // Required by YR

            mapIni.WriteIniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);
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
        protected abstract void WriteSpawnIniAdditions(IniFile iniFile);

        /// <summary>
        /// "Copies" player information from the UI to internal memory,
        /// applying users' player options changes.
        /// </summary>
        protected virtual void CopyPlayerDataFromUI(object sender, EventArgs e)
        {
            if (PlayerUpdatingInProgress)
                return;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                pInfo.ColorId = ddPlayerColors[pId].SelectedIndex;
                pInfo.SideId = ddPlayerSides[pId].SelectedIndex;
                pInfo.StartingLocation = ddPlayerStarts[pId].SelectedIndex;
                pInfo.TeamId = ddPlayerTeams[pId].SelectedIndex;
            }

            AIPlayers.Clear();
            for (int cmbId = Players.Count; cmbId < 8; cmbId++)
            {
                DXDropDown dd = ddPlayerNames[cmbId];
                dd.Items[0].Text = "-";

                if (dd.SelectedIndex < 1)
                    continue;

                PlayerInfo aiPlayer = new PlayerInfo();
                aiPlayer.Name = dd.Items[dd.SelectedIndex].Text;
                aiPlayer.SideId = Math.Max(ddPlayerSides[cmbId].SelectedIndex, 0);
                aiPlayer.ColorId = Math.Max(ddPlayerColors[cmbId].SelectedIndex, 0);
                aiPlayer.StartingLocation = Math.Max(ddPlayerStarts[cmbId].SelectedIndex, 0);
                aiPlayer.TeamId = Math.Max(ddPlayerTeams[cmbId].SelectedIndex, 0);

                AIPlayers.Add(aiPlayer);
            }

            CopyPlayerDataToUI();
        }

        /// <summary>
        /// Applies player information changes done in memory to the UI.
        /// </summary>
        protected virtual void CopyPlayerDataToUI()
        {
            PlayerUpdatingInProgress = true;

            // Human players
            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                DXDropDown ddPlayerName = ddPlayerNames[pId];
                ddPlayerName.Items[0].Text = pInfo.Name;
                ddPlayerName.SelectedIndex = 0;
                ddPlayerName.AllowDropDown = false;
                ddPlayerSides[pId].SelectedIndex = pInfo.SideId;
                ddPlayerSides[pId].AllowDropDown = true;
                ddPlayerColors[pId].SelectedIndex = pInfo.ColorId;
                ddPlayerColors[pId].AllowDropDown = true;
                ddPlayerStarts[pId].SelectedIndex = pInfo.StartingLocation;
                ddPlayerStarts[pId].AllowDropDown = true;
                ddPlayerTeams[pId].SelectedIndex = pInfo.TeamId;
                ddPlayerTeams[pId].AllowDropDown = true;
            }

            // AI players
            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                PlayerInfo aiInfo = AIPlayers[aiId];

                int index = Players.Count + aiId;
                DXDropDown ddPlayerName = ddPlayerNames[index];
                ddPlayerName.Items[0].Text = "-";
                ddPlayerName.SelectedIndex = 3 - aiInfo.AILevel;
                ddPlayerName.AllowDropDown = true;
                ddPlayerSides[index].SelectedIndex = aiInfo.SideId;
                ddPlayerSides[index].AllowDropDown = true;
                ddPlayerColors[index].SelectedIndex = aiInfo.ColorId;
                ddPlayerColors[index].AllowDropDown = true;
                ddPlayerStarts[index].SelectedIndex = aiInfo.StartingLocation;
                ddPlayerStarts[index].AllowDropDown = true;
                ddPlayerTeams[index].SelectedIndex = aiInfo.TeamId;
                ddPlayerTeams[index].AllowDropDown = true;
            }

            // Unused player slots
            for (int ddIndex = Players.Count + AIPlayers.Count; ddIndex < PLAYER_COUNT; ddIndex++)
            {
                DXDropDown ddPlayerName = ddPlayerNames[ddIndex];
                ddPlayerName.AllowDropDown = false;
                ddPlayerName.Items[0].Text = string.Empty;
                ddPlayerName.SelectedIndex = -1;

                ddPlayerSides[ddIndex].SelectedIndex = -1;
                ddPlayerSides[ddIndex].AllowDropDown = false;

                ddPlayerColors[ddIndex].SelectedIndex = -1;
                ddPlayerColors[ddIndex].AllowDropDown = false;

                ddPlayerStarts[ddIndex].SelectedIndex = -1;
                ddPlayerStarts[ddIndex].AllowDropDown = false;

                ddPlayerTeams[ddIndex].SelectedIndex = -1;
                ddPlayerTeams[ddIndex].AllowDropDown = false;
            }

            if (Players.Count + AIPlayers.Count < PLAYER_COUNT)
                ddPlayerNames[Players.Count + AIPlayers.Count].AllowDropDown = true;

            MapPreviewBox.UpdateStartingLocationTexts();

            PlayerUpdatingInProgress = false;
        }

        /// <summary>
        /// Changes the current map and game mode.
        /// </summary>
        /// <param name="gameMode">The new game mode.</param>
        /// <param name="map">The new map.</param>
        protected virtual void ChangeMap(GameMode gameMode, Map map)
        {
            if (GameMode == null || !object.ReferenceEquals(gameMode, GameMode))
            {
                // TODO: Load the new game mode's default settings
            }

            GameMode = gameMode;

            Map = map;

            // We could either pass the CheckBoxes and DropDowns of this class
            // to the Map and GameMode instances and let them apply their forced
            // options, or we could do it in this class with helper functions.
            // I think the second approach is clearer.

            ApplyForcedCheckBoxOptions(gameMode.ForcedCheckBoxValues);
            ApplyForcedCheckBoxOptions(map.ForcedCheckBoxValues);

            ApplyForcedDropDownOptions(gameMode.ForcedDropDownValues);
            ApplyForcedDropDownOptions(map.ForcedDropDownValues);

            // Enable all sides by default
            foreach (DXDropDown ddSide in ddPlayerSides)
            {
                foreach (DXDropDownItem item in ddSide.Items)
                    item.Selectable = true;
            }

            // Enable all colors by default
            foreach (DXDropDown ddColor in ddPlayerColors)
            {
                foreach (DXDropDownItem item in ddColor.Items)
                    item.Selectable = true;
            }

            if (map.CoopInfo != null)
            {
                // Co-Op map disallowed side logic

                List<int> disallowedSides = map.CoopInfo.DisallowedPlayerSides;

                bool disallowRandom = _sideCount == disallowedSides.Count + 1;
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

                    foreach (DXDropDown dd in ddPlayerSides)
                        dd.Items[0].Selectable = false;
                }

                foreach (int disallowedSideIndex in disallowedSides)
                {
                    if (disallowedSideIndex >= _sideCount)
                        continue; // Let's not crash the client

                    foreach (DXDropDown ddSide in ddPlayerSides)
                    {
                        ddSide.Items[disallowedSideIndex + 1].Selectable = false;
                    }

                    foreach (PlayerInfo pInfo in Players)
                    {
                        if (pInfo.SideId == disallowedSideIndex + 1)
                            pInfo.SideId = defaultSideIndex;
                    }

                    foreach (PlayerInfo aiInfo in AIPlayers)
                    {
                        if (aiInfo.SideId == disallowedSideIndex + 1)
                            aiInfo.SideId = defaultSideIndex;
                    }
                }

                // Co-Op map disallowed color logic
                foreach (int disallowedColorIndex in map.CoopInfo.DisallowedPlayerColors)
                {
                    if (disallowedColorIndex >= MPColors.Count)
                        continue;

                    foreach (DXDropDown ddColor in ddPlayerColors)
                        ddColor.Items[disallowedColorIndex + 1].Selectable = false;

                    foreach (PlayerInfo pInfo in Players)
                    {
                        if (pInfo.ColorId == disallowedColorIndex + 1)
                            pInfo.ColorId = 0;
                    }

                    foreach (PlayerInfo aiInfo in AIPlayers)
                    {
                        if (aiInfo.ColorId == disallowedColorIndex + 1)
                            aiInfo.ColorId = 0;
                    }
                }

                CopyPlayerDataToUI();
            }

            MapPreviewBox.Map = map;
        }

        protected void ApplyForcedCheckBoxOptions(List<KeyValuePair<string, bool>> forcedOptions)
        {
            foreach (KeyValuePair<string, bool> option in forcedOptions)
            {
                GameLobbyCheckBox checkBox = CheckBoxes.Find(chk => chk.Name == option.Key);
                if (checkBox != null)
                    checkBox.Checked = option.Value;
            }
        }

        protected void ApplyForcedDropDownOptions(List<KeyValuePair<string, int>> forcedOptions)
        {
            foreach (KeyValuePair<string, int> option in forcedOptions)
            {
                GameLobbyDropDown dropDown = DropDowns.Find(dd => dd.Name == option.Key);
                if (dropDown != null)
                    dropDown.SelectedIndex = option.Value;
            }
        }
    }
}
