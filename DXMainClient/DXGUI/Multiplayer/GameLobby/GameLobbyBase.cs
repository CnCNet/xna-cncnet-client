using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain;
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
using ClientCore.Enums;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.Online.EventArguments;
using ClientCore.Extensions;

using DTAClient.DXGUI.Generic;

using TextCopy;


namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A generic base for all game lobbies (Skirmish, LAN and CnCNet).
    /// Contains the common logic for parsing game options and handling player info.
    /// </summary>
    public abstract class GameLobbyBase : INItializableWindow
    {
        protected record Rank
        {
            private readonly int rank;

            public static readonly Rank None = 0;
            public static readonly Rank Easy = 1;
            public static readonly Rank Medium = 2;
            public static readonly Rank Hard = 3;

            private Rank(int rank) => this.rank = rank;

            public static implicit operator int(Rank value) => value.rank;

            public static implicit operator Rank(int value) => new Rank(value);
        }

        protected const int MAX_PLAYER_COUNT = 8;
        protected const int PLAYER_OPTION_VERTICAL_MARGIN = 12;
        protected const int PLAYER_OPTION_HORIZONTAL_MARGIN = 3;
        protected const int PLAYER_OPTION_CAPTION_Y = 6;
        private const int DROP_DOWN_HEIGHT = 21;
        protected readonly string BTN_LAUNCH_GAME = "Launch Game".L10N("Client:Main:ButtonLaunchGame");
        protected readonly string BTN_LAUNCH_READY = "I'm Ready".L10N("Client:Main:ButtonIAmReady");
        protected readonly string BTN_LAUNCH_NOT_READY = "Not Ready".L10N("Client:Main:ButtonNotReady");

        private readonly string FavoriteMapsLabel = "Favorite Maps".L10N("Client:Main:FavoriteMaps");

        /// <summary>
        /// Creates a new instance of the game lobby base.
        /// </summary>
        /// <param name="windowManager"></param>
        /// <param name="iniName">The name of the lobby in GameOptions.ini.</param>
        /// <param name="mapLoader"></param>
        /// <param name="isMultiplayer"></param>
        /// <param name="discordHandler"></param>
        public GameLobbyBase(
            WindowManager windowManager,
            string iniName,
            MapLoader mapLoader,
            bool isMultiplayer,
            DiscordHandler discordHandler,
            Random random
        ) : base(windowManager)
        {
            _iniSectionName = iniName;
            MapLoader = mapLoader;
            this.isMultiplayer = isMultiplayer;
            this.discordHandler = discordHandler;
            this.random = random;
        }

        private string _iniSectionName;

        private Random random;

        protected XNAPanel PlayerOptionsPanel;

        protected List<MultiplayerColor> MPColors;

        public List<GameLobbyCheckBox> CheckBoxes { get; } = new();
        public List<GameLobbyDropDown> DropDowns { get; } = new();

        protected DiscordHandler discordHandler;

        protected MapLoader MapLoader;
        /// <summary>
        /// The list of multiplayer game mode maps.
        /// Each is an instance of a map for a specific game mode.
        /// </summary>
        protected GameModeMapCollection GameModeMaps => MapLoader.GameModeMaps;

        protected GameModeMapFilter gameModeMapFilter;

        private GameModeMap _gameModeMap;

        /// <summary>
        /// The currently selected game mode.
        /// </summary>
        protected GameModeMap GameModeMap
        {
            get => _gameModeMap;
            set
            {
                var oldGameModeMap = _gameModeMap;
                _gameModeMap = value;
                if (value != null && oldGameModeMap != value)
                    UpdateDiscordPresence();
            }
        }

        protected Map Map => GameModeMap?.Map;
        protected GameMode GameMode => GameModeMap?.GameMode;

        protected XNAClientDropDown[] ddPlayerNames;
        protected XNAClientDropDown[] ddPlayerSides;
        protected XNAClientColorDropDown[] ddPlayerColors;
        protected XNAClientDropDown[] ddPlayerStarts;
        protected XNAClientDropDown[] ddPlayerTeams;

        protected XNAClientButton btnPlayerExtraOptionsOpen;
        protected PlayerExtraOptionsPanel PlayerExtraOptionsPanel;

        protected XNAClientButton btnLeaveGame;
        protected GameLaunchButton btnLaunchGame;
        protected XNAClientButton btnPickRandomMap;
        protected XNALabel lblMapName;
        protected XNALabel lblMapAuthor;
        protected XNALabel lblGameMode;
        protected XNALabel lblMapSize;

        protected MapPreviewBox MapPreviewBox;

        protected XNAMultiColumnListBox lbGameModeMapList;
        protected ToolTip mapListTooltip;
        protected XNAClientDropDown ddGameModeMapFilter;
        protected XNALabel lblGameModeSelect;
        protected XNAContextMenu mapContextMenu;
        private XNAContextMenuItem toggleFavoriteItem;

        protected XNAClientStateButton<SortDirection> btnMapSortAlphabetically;

        protected XNASuggestionTextBox tbMapSearch;
        protected XNAContextMenu searchContextMenu;
        private XNAContextMenuItem searchCurrentModeItem;
        private XNAContextMenuItem searchAllModesItem;
        private bool searchAllGameModes = false;

        protected List<PlayerInfo> Players = new List<PlayerInfo>();
        protected List<PlayerInfo> AIPlayers = new List<PlayerInfo>();

        protected virtual PlayerInfo FindLocalPlayer() => Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

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
        protected int SideCount { get; private set; }
        protected int RandomSelectorCount { get; private set; } = 1;

        protected List<int[]> RandomSelectors = new List<int[]>();

        private readonly bool isMultiplayer = false;

        private MatchStatistics matchStatistics;

        private bool disableGameOptionUpdateBroadcast = false;

        protected EventHandler<MultiplayerNameRightClickedEventArgs> MultiplayerNameRightClicked;

        /// <summary>
        /// If set, the client will remove all starting waypoints from the map
        /// before launching it.
        /// </summary>
        protected bool RemoveStartingLocations { get; set; } = false;
        protected IniFile GameOptionsIni { get; private set; }

        protected XNAClientButton btnSaveLoadGameOptions { get; set; }

        private XNAContextMenu loadSaveGameOptionsMenu { get; set; }

        private LoadOrSaveGameOptionPresetWindow loadOrSaveGameOptionPresetWindow;

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

            GameOptionsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), ClientConfiguration.GAME_OPTIONS));

            base.Initialize();

            try
            {
                PlayerOptionsPanel = FindChild<XNAPanel>(nameof(PlayerOptionsPanel));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(("It seems the client configuration was not migrated to accommodate " +
                                                   "for the 'Tiberian Sun Client v6 Changes'.\n\n" +
                                                   "Please refer to documentation of the client {0} for more details. This link can also be found in the log file.\n\n" +
                                                   "Error message: {1}").L10N("Client:Main:NotMigratedClientException"),
                                                   "https://github.com/CnCNet/xna-cncnet-client/",
                                                   ex.Message));
            }

            btnLeaveGame = FindChild<XNAClientButton>(nameof(btnLeaveGame));
            btnLeaveGame.LeftClick += BtnLeaveGame_LeftClick;

            btnLaunchGame = FindChild<GameLaunchButton>(nameof(btnLaunchGame));
            btnLaunchGame.LeftClick += BtnLaunchGame_LeftClick;
            btnLaunchGame.InitStarDisplay(RankTextures);

            MapPreviewBox = FindChild<MapPreviewBox>("MapPreviewBox");
            MapPreviewBox.SetFields(Players, AIPlayers, MPColors, GameOptionsIni.GetStringValue("General", "Sides", String.Empty).Split(','), GameOptionsIni);
            MapPreviewBox.ToggleFavorite += MapPreviewBox_ToggleFavorite;

            lblMapName = FindChild<XNALabel>(nameof(lblMapName));
            lblMapAuthor = FindChild<XNALabel>(nameof(lblMapAuthor));
            lblGameMode = FindChild<XNALabel>(nameof(lblGameMode));
            lblMapSize = FindChild<XNALabel>(nameof(lblMapSize));

            lbGameModeMapList = FindChild<XNAMultiColumnListBox>("lbMapList"); // lbMapList for backwards compatibility
            lbGameModeMapList.SelectedIndexChanged += LbGameModeMapList_SelectedIndexChanged;
            lbGameModeMapList.RightClick += LbGameModeMapList_RightClick;
            lbGameModeMapList.AllowKeyboardInput = true; //!isMultiplayer

            mapListTooltip = new(WindowManager, masterControl: lbGameModeMapList);
            mapListTooltip.FollowCursor = true;
            lbGameModeMapList.HoveredIndexChanged += LbGameModeMapList_HoveredIndexChanged;

            mapContextMenu = new XNAContextMenu(WindowManager);
            mapContextMenu.Name = nameof(mapContextMenu);
            mapContextMenu.Width = 192; // TODO autosizing

            mapContextMenu.AddItem("Favorite".L10N("Client:Main:Favorite"),
                selectAction: ToggleFavoriteMap);
            toggleFavoriteItem = mapContextMenu.Items.First();

            mapContextMenu.AddItem("Copy Map Name".L10N("Client:Main:CopyMapName"),
                selectAction: () => ClipboardService.SetText(Map?.Name));
            mapContextMenu.AddItem("Copy Original Name".L10N("Client:Main:CopyOriginalMapName"),
                selectAction: () => ClipboardService.SetText(Map?.UntranslatedName),
                visibilityChecker: () => Map?.UntranslatedName != Map?.Name);
            mapContextMenu.AddItem("Delete Map".L10N("Client:Main:DeleteMap"),
                selectAction: DeleteMapConfirmation,
                visibilityChecker: CanDeleteMap);

            AddChild(mapContextMenu);

            XNAPanel rankHeader = new XNAPanel(WindowManager);
            rankHeader.BackgroundTexture = AssetLoader.LoadTexture("rank.png");
            rankHeader.ClientRectangle = new Rectangle(0, 0, rankHeader.BackgroundTexture.Width,
                19);

            XNAListBox rankListBox = new XNAListBox(WindowManager);
            rankListBox.TextBorderDistance = 2;

            lbGameModeMapList.AddColumn(rankHeader, rankListBox);
            lbGameModeMapList.AddColumn("MAP NAME".L10N("Client:Main:MapNameHeader"), lbGameModeMapList.Width - RankTextures[1].Width - 3);

            ddGameModeMapFilter = FindChild<XNAClientDropDown>("ddGameMode"); // ddGameMode for backwards compatibility
            ddGameModeMapFilter.SelectedIndexChanged += DdGameModeMapFilter_SelectedIndexChanged;

            ddGameModeMapFilter.AddItem(CreateGameFilterItem(FavoriteMapsLabel, new GameModeMapFilter(GetFavoriteGameModeMaps)));
            foreach (GameMode gm in GameModeMaps.GameModes)
                ddGameModeMapFilter.AddItem(CreateGameFilterItem(gm.UIName, new GameModeMapFilter(GetGameModeMaps(gm))));

            lblGameModeSelect = FindChild<XNALabel>(nameof(lblGameModeSelect));

            InitBtnMapSort();

            tbMapSearch = FindChild<XNASuggestionTextBox>(nameof(tbMapSearch));
            tbMapSearch.InputReceived += TbMapSearch_InputReceived;
            tbMapSearch.RightClick += TbMapSearch_RightClick;

            searchContextMenu = new XNAContextMenu(WindowManager);
            searchContextMenu.Name = nameof(searchContextMenu);
            searchContextMenu.Width = 150;

            searchCurrentModeItem = new XNAContextMenuItem()
            {
                Text = "Search current mode".L10N("Client:Main:SearchCurrentMode"),
                SelectAction = () => SetSearchAllGameModes(false),
                HintTextGenerator = () => !searchAllGameModes ? "< " : null
            };
            searchContextMenu.AddItem(searchCurrentModeItem);

            searchAllModesItem = new XNAContextMenuItem()
            {
                Text = "Search all modes".L10N("Client:Main:SearchAllModes"),
                SelectAction = () => SetSearchAllGameModes(true),
                HintTextGenerator = () => searchAllGameModes ? "< " : null
            };
            searchContextMenu.AddItem(searchAllModesItem);
            searchAllGameModes = UserINISettings.Instance.SearchAllGameModes.Value;

            AddChild(searchContextMenu);


            btnPickRandomMap = FindChild<XNAClientButton>(nameof(btnPickRandomMap));
            btnPickRandomMap.LeftClick += BtnPickRandomMap_LeftClick;

            CheckBoxes.ForEach(chk => chk.CheckedChanged += ChkBox_CheckedChanged);
            DropDowns.ForEach(dd => dd.SelectedIndexChanged += Dropdown_SelectedIndexChanged);

            InitializeGameOptionPresetUI();
        }

        /// <summary>
        /// Until the GUICreator can handle typed classes, this must remain manually done.
        /// </summary>
        private void InitBtnMapSort()
        {
            btnMapSortAlphabetically = new XNAClientStateButton<SortDirection>(WindowManager, new Dictionary<SortDirection, Texture2D>()
            {
                { SortDirection.None, AssetLoader.LoadTexture("sortAlphaNone.png") },
                { SortDirection.Asc, AssetLoader.LoadTexture("sortAlphaAsc.png") },
                { SortDirection.Desc, AssetLoader.LoadTexture("sortAlphaDesc.png") },
            });
            btnMapSortAlphabetically.Name = nameof(btnMapSortAlphabetically);
            btnMapSortAlphabetically.ClientRectangle = new Rectangle(
                ddGameModeMapFilter.X + -ddGameModeMapFilter.Height - 4, ddGameModeMapFilter.Y,
                ddGameModeMapFilter.Height, ddGameModeMapFilter.Height
            );
            btnMapSortAlphabetically.LeftClick += BtnMapSortAlphabetically_LeftClick;
            btnMapSortAlphabetically.SetToolTipText("Sort Maps Alphabetically".L10N("Client:Main:MapSortAlphabeticallyToolTip"));
            RefreshMapSortAlphabeticallyBtn();
            AddChild(btnMapSortAlphabetically);

            // Allow repositioning / disabling in INI.
            ReadINIForControl(btnMapSortAlphabetically);

            MapLoader.MapChanged += MapLoader_MapChanged;
        }

        private void InitializeGameOptionPresetUI()
        {
            btnSaveLoadGameOptions = FindChild<XNAClientButton>(nameof(btnSaveLoadGameOptions), true);

            if (btnSaveLoadGameOptions != null)
            {
                loadOrSaveGameOptionPresetWindow = new LoadOrSaveGameOptionPresetWindow(WindowManager);
                loadOrSaveGameOptionPresetWindow.Name = nameof(loadOrSaveGameOptionPresetWindow);
                loadOrSaveGameOptionPresetWindow.PresetLoaded += (sender, s) => HandleGameOptionPresetLoadCommand(s);
                loadOrSaveGameOptionPresetWindow.PresetSaved += (sender, s) => HandleGameOptionPresetSaveCommand(s);
                loadOrSaveGameOptionPresetWindow.Disable();
                var loadConfigMenuItem = new XNAContextMenuItem()
                {
                    Text = "Load".L10N("Client:Main:ButtonLoad"),
                    SelectAction = () => loadOrSaveGameOptionPresetWindow.Show(true)
                };
                var saveConfigMenuItem = new XNAContextMenuItem()
                {
                    Text = "Save".L10N("Client:Main:ButtonSave"),
                    SelectAction = () => loadOrSaveGameOptionPresetWindow.Show(false)
                };

                loadSaveGameOptionsMenu = new XNAContextMenu(WindowManager);
                loadSaveGameOptionsMenu.Name = nameof(loadSaveGameOptionsMenu);
                loadSaveGameOptionsMenu.ClientRectangle = new Rectangle(0, 0, 75, 0);
                loadSaveGameOptionsMenu.Items.Add(loadConfigMenuItem);
                loadSaveGameOptionsMenu.Items.Add(saveConfigMenuItem);

                btnSaveLoadGameOptions.LeftClick += (sender, args) =>
                    loadSaveGameOptionsMenu.Open(GetCursorPoint());

                AddChild(loadSaveGameOptionsMenu);
                AddChild(loadOrSaveGameOptionPresetWindow);
            }
        }

        private void BtnMapSortAlphabetically_LeftClick(object sender, EventArgs e)
        {
            UserINISettings.Instance.MapSortState.Value = (int)btnMapSortAlphabetically.GetState();

            RefreshMapSortAlphabeticallyBtn();
            UserINISettings.Instance.SaveSettings();
            ListMaps();
        }

        private void RefreshMapSortAlphabeticallyBtn()
        {
            if (Enum.IsDefined(typeof(SortDirection), UserINISettings.Instance.MapSortState.Value))
                btnMapSortAlphabetically.SetState((SortDirection)UserINISettings.Instance.MapSortState.Value);
        }

        private void MapLoader_MapChanged(object sender, MapChangedEventArgs e)
        {
            WindowManager.AddCallback(() =>
            {
                switch (e.ChangeType)
                {
                    case MapChangeType.Added:
                        HandleMapAdded(e.Map);
                        break;
                    case MapChangeType.Updated:
                        HandleMapUpdated(e.Map, e.PreviousMapSHA1);
                        break;
                    case MapChangeType.Removed:
                        HandleMapRemoved(e.Map);
                        break;
                }
            }, null);
        }

        protected virtual void HandleMapAdded(Map addedMap)
        {
            RefreshGameModeFilter();

            if (ShouldShowMapInCurrentFilter(addedMap))
                ListMaps();
        }

        protected virtual void HandleMapUpdated(Map updatedMap, string previousSHA1)
        {
            // If the currently selected map was updated, refresh the UI
            if (Map != null && (Map.SHA1 == previousSHA1 || Map.SHA1 == updatedMap.SHA1))
            {
                // Find the new GameModeMap for the updated map
                var updatedGameModeMap = GameModeMaps
                    .FirstOrDefault(gmm => gmm.Map.SHA1 == updatedMap.SHA1);

                if (updatedGameModeMap != null)
                    ChangeMap(updatedGameModeMap);
            }

            ListMaps();
        }

        private void HandleMapRemoved(Map removedMap)
        {
            // If the currently selected map was removed, select a different one
            if (Map != null && Map.SHA1 == removedMap.SHA1)
            {
                var availableMaps = GameModeMaps.Where(gmm => gmm.GameMode == GameMode).ToList();
                if (availableMaps.Any())
                {
                    ChangeMap(availableMaps.First());
                }
                else
                {
                    // No maps available for current game mode, change to a different one
                    var firstAvailableGameModeMap = GameModeMaps.FirstOrDefault();
                    if (firstAvailableGameModeMap != null)
                    {
                        ChangeMap(firstAvailableGameModeMap);
                        RefreshMapSelectionUI();
                    }
                }
            }

            RefreshGameModeFilter();
            ListMaps();
        }

        private bool ShouldShowMapInCurrentFilter(Map map)
        {
            if (map?.GameModes == null || gameModeMapFilter == null)
                return false;

            return map.GameModes.Any(gameModeName =>
            {
                var gameMode = MapLoader.GameModes.FirstOrDefault(gm => gm.Name == gameModeName);
                if (gameMode == null) return false;

                return gameModeMapFilter.GetGameModeMaps().Any(gmm =>
                    gmm.GameMode.Name == gameMode.Name && gmm.Map.SHA1 == map.SHA1);
            });
        }

        private static XNADropDownItem CreateGameFilterItem(string text, GameModeMapFilter filter)
        {
            return new XNADropDownItem
            {
                Text = text,
                Tag = filter
            };
        }

        protected bool IsFavoriteMapsSelected() => ddGameModeMapFilter.SelectedItem?.Text == FavoriteMapsLabel;

        private List<GameModeMap> GetFavoriteGameModeMaps() =>
            GameModeMaps.Where(gmm => gmm.IsFavorite).ToList();

        private Func<List<GameModeMap>> GetGameModeMaps(GameMode gm) => () =>
            GameModeMaps.Where(gmm => gmm.GameMode == gm).ToList();

        private void RefreshBtnPlayerExtraOptionsOpenTexture()
        {
            if (btnPlayerExtraOptionsOpen != null)
            {
                var textureName = GetPlayerExtraOptions().IsDefault() ? "optionsButton.png" : "optionsButtonActive.png";
                var hoverTextureName = GetPlayerExtraOptions().IsDefault() ? "optionsButton_c.png" : "optionsButtonActive_c.png";
                var hoverTexture = AssetLoader.AssetExists(hoverTextureName) ? AssetLoader.LoadTexture(hoverTextureName) : null;
                btnPlayerExtraOptionsOpen.IdleTexture = AssetLoader.LoadTexture(textureName);
                btnPlayerExtraOptionsOpen.HoverTexture = hoverTexture;
            }
        }

        protected void HandleGameOptionPresetSaveCommand(GameOptionPresetEventArgs e) => HandleGameOptionPresetSaveCommand(e.PresetName);

        protected void HandleGameOptionPresetSaveCommand(string presetName)
        {
            string error = AddGameOptionPreset(presetName);
            if (!string.IsNullOrEmpty(error))
                AddNotice(error);
        }

        protected void HandleGameOptionPresetLoadCommand(GameOptionPresetEventArgs e) => HandleGameOptionPresetLoadCommand(e.PresetName);

        protected void HandleGameOptionPresetLoadCommand(string presetName)
        {
            if (LoadGameOptionPreset(presetName))
                AddNotice("Game option preset loaded succesfully.".L10N("Client:Main:PresetLoaded"));
            else
                AddNotice(string.Format("Preset {0} not found!".L10N("Client:Main:PresetNotFound"), presetName));
        }

        protected void AddNotice(string message) => AddNotice(message, Color.White);

        protected abstract void AddNotice(string message, Color color);

        private void BtnPickRandomMap_LeftClick(object sender, EventArgs e) => PickRandomMap();

        private void TbMapSearch_InputReceived(object sender, EventArgs e) => ListMaps();

        private void TbMapSearch_RightClick(object sender, EventArgs e) => searchContextMenu.Open(GetCursorPoint());

        private void SetSearchAllGameModes(bool value)
        {
            searchAllGameModes = value;
            UserINISettings.Instance.SearchAllGameModes.Value = value;
            UserINISettings.Instance.SaveSettings();
            ListMaps();
        }

        private void Dropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (disableGameOptionUpdateBroadcast)
                return;

            var dd = (GameLobbyDropDown)sender;
            dd.HostSelectedIndex = dd.SelectedIndex;
            OnGameOptionChanged();
        }

        private void ChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (disableGameOptionUpdateBroadcast)
                return;

            var checkBox = (GameLobbyCheckBox)sender;
            checkBox.HostChecked = checkBox.Checked;
            OnGameOptionChanged();
        }

        protected virtual void OnGameOptionChanged()
        {
            CheckDisallowedSides();

            btnLaunchGame.SetRank(GetRank());
        }

        protected void DdGameModeMapFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            gameModeMapFilter = ddGameModeMapFilter.SelectedItem.Tag as GameModeMapFilter;

            tbMapSearch.Text = string.Empty;
            tbMapSearch.OnSelectedChanged();

            ListMaps();

            if (lbGameModeMapList.SelectedIndex == -1)
                lbGameModeMapList.SelectedIndex = 0; // Select default GameModeMap
            else
                ChangeMap(GameModeMap);
        }

        protected void BtnPlayerExtraOptions_LeftClick(object sender, EventArgs e)
        {
            if (PlayerExtraOptionsPanel.Enabled)
                PlayerExtraOptionsPanel.Disable();
            else
                PlayerExtraOptionsPanel.Enable();
        }

        protected void ApplyPlayerExtraOptions(string sender, string message)
        {
            var playerExtraOptions = PlayerExtraOptions.FromMessage(message);

            if (PlayerExtraOptionsPanel != null)
            {
                if (playerExtraOptions.IsForceRandomSides != PlayerExtraOptionsPanel.IsForcedRandomSides())
                    AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomSides, "side selection".L10N("Client:Main:SideAsANoun"));

                if (playerExtraOptions.IsForceRandomColors != PlayerExtraOptionsPanel.IsForcedRandomColors())
                    AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomColors, "color selection".L10N("Client:Main:ColorAsANoun"));

                if (playerExtraOptions.IsForceRandomStarts != PlayerExtraOptionsPanel.IsForcedRandomStarts())
                    AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomStarts, "start selection".L10N("Client:Main:StartPositionAsANoun"));

                if (playerExtraOptions.IsForceRandomTeams != PlayerExtraOptionsPanel.IsForcedRandomTeams())
                    AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomTeams, "team selection".L10N("Client:Main:TeamAsANoun"));

                if (playerExtraOptions.IsUseTeamStartMappings != PlayerExtraOptionsPanel.IsUseTeamStartMappings())
                    AddPlayerExtraOptionForcedNotice(!playerExtraOptions.IsUseTeamStartMappings, "auto ally".L10N("Client:Main:AutoAllyAsANoun"));
            }

            SetPlayerExtraOptions(playerExtraOptions);
            UpdateMapPreviewBoxEnabledStatus();
        }

        private void AddPlayerExtraOptionForcedNotice(bool disabled, string type)
            => AddNotice(disabled ?
                string.Format("The game host has disabled {0}".L10N("Client:Main:HostDisableSection"), type) :
                string.Format("The game host has enabled {0}".L10N("Client:Main:HostEnableSection"), type));

        protected List<GameModeMap> GetSortedGameModeMaps()
        {
            var gameModeMaps = searchAllGameModes ? GameModeMaps.ToList() : gameModeMapFilter.GetGameModeMaps();

            // Only apply sort if the map list sort button is available.
            if (btnMapSortAlphabetically.Enabled && btnMapSortAlphabetically.Visible)
            {
                switch ((SortDirection)UserINISettings.Instance.MapSortState.Value)
                {
                    case SortDirection.Asc:
                        gameModeMaps = gameModeMaps.OrderBy(gmm => gmm.Map.Name).ToList();
                        break;
                    case SortDirection.Desc:
                        gameModeMaps = gameModeMaps.OrderByDescending(gmm => gmm.Map.Name).ToList();
                        break;
                }
            }

            return gameModeMaps;
        }

        protected void ListMaps()
        {
            lbGameModeMapList.SelectedIndexChanged -= LbGameModeMapList_SelectedIndexChanged;

            lbGameModeMapList.ClearItems();
            lbGameModeMapList.SetTopIndex(0);

            lbGameModeMapList.SelectedIndex = -1;

            int mapIndex = -1;

            var isFavoriteMapsSelected = IsFavoriteMapsSelected();
            var maps = GetSortedGameModeMaps();

            bool gameModeMapChanged = false;

            List<GameModeMap> filteredMaps;

            if (tbMapSearch.Text != tbMapSearch.Suggestion)
            {
                string search = tbMapSearch.Text.Trim();
                string[] searchWords = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // Equals entire search string
                var exactMatches = maps.Where(gmm =>
                    gmm.Map.Name.Equals(search, StringComparison.CurrentCultureIgnoreCase) ||
                    gmm.Map.UntranslatedName.Equals(search, StringComparison.InvariantCultureIgnoreCase)).ToList();

                // Contains entire search string
                var substringMatches = maps.Except(exactMatches).Where(gmm =>
                    gmm.Map.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    gmm.Map.UntranslatedName.Contains(search, StringComparison.InvariantCultureIgnoreCase)).ToList();

                // Contains all search words. It matches with "AND" logic: Word1 AND Word2 AND Word3
                var multiWordMatches = maps.Except(exactMatches).Except(substringMatches).Where(gmm =>
                {
                    bool allInTranslated = searchWords.All(word =>
                        gmm.Map.Name.Contains(word, StringComparison.CurrentCultureIgnoreCase));

                    bool allInUntranslated = searchWords.All(word =>
                        gmm.Map.UntranslatedName.Contains(word, StringComparison.InvariantCultureIgnoreCase));

                    return allInTranslated || allInUntranslated;
                }).ToList();

                filteredMaps = [.. exactMatches, .. substringMatches, .. multiWordMatches];
            }
            else
            {
                filteredMaps = maps;
            }

            for (int i = 0; i < filteredMaps.Count; i++)
            {
                var gameModeMap = filteredMaps[i];

                XNAListBoxItem rankItem = new XNAListBoxItem();
                if (gameModeMap.Map.IsCoop)
                {
                    // Note: StatisticsManager.Statistics must be initialized to call `HasBeatCoOpMap()`. This means StatisticsWindow must be initialized before any lobbies extending GameLobbyBase.
                    if (StatisticsManager.Instance.HasBeatCoOpMap(gameModeMap.Map.UntranslatedName, gameModeMap.GameMode.UntranslatedUIName))
                        rankItem.Texture = RankTextures[Math.Abs(2 - gameModeMap.GameMode.CoopDifficultyLevel) + 1];
                    else
                        rankItem.Texture = RankTextures[0];
                }
                else
                    rankItem.Texture = RankTextures[GetDefaultMapRankIndex(gameModeMap) + 1];

                XNAListBoxItem mapNameItem = new XNAListBoxItem();
                var mapNameText = gameModeMap.Map.Name;
                if (isFavoriteMapsSelected || searchAllGameModes)
                    mapNameText += $" - {gameModeMap.GameMode.UIName}";

                mapNameItem.Text = Renderer.GetSafeString(mapNameText, lbGameModeMapList.FontIndex);

                if ((gameModeMap.Map.MultiplayerOnly || gameModeMap.GameMode.MultiplayerOnly) && !isMultiplayer)
                    mapNameItem.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                mapNameItem.Tag = gameModeMap;

                XNAListBoxItem[] mapInfoArray = {
                    rankItem,
                    mapNameItem,
                };

                lbGameModeMapList.AddItem(mapInfoArray);

                // Preserve the selected map
                if (gameModeMap == GameModeMap)
                {
                    mapIndex = i;
                    gameModeMapChanged = false;
                }

                if (mapIndex == -1 && (gameModeMap?.Map?.Equals(GameModeMap?.Map) ?? false))
                {
                    mapIndex = i;
                    gameModeMapChanged = true;
                }
            }

            if (mapIndex > -1)
            {
                lbGameModeMapList.SelectedIndex = mapIndex;
                while (mapIndex > lbGameModeMapList.LastIndex)
                    lbGameModeMapList.TopIndex++;
            }

            lbGameModeMapList.SelectedIndexChanged += LbGameModeMapList_SelectedIndexChanged;

            // Trigger the event manually to update GameModeMap
            if (gameModeMapChanged)
                LbGameModeMapList_SelectedIndexChanged();
        }

        protected abstract int GetDefaultMapRankIndex(GameModeMap gameModeMap);

        private void LbGameModeMapList_RightClick(object sender, EventArgs e)
        {
            if (lbGameModeMapList.HoveredIndex < 0 || lbGameModeMapList.HoveredIndex >= lbGameModeMapList.ItemCount)
                return;

            lbGameModeMapList.SelectedIndex = lbGameModeMapList.HoveredIndex;

            if (!mapContextMenu.Items.Any(i => i.VisibilityChecker == null || i.VisibilityChecker()))
                return;

            toggleFavoriteItem.Text = GameModeMap.IsFavorite ? "Remove Favorite".L10N("Client:Main:RemoveFavorite") : "Add Favorite".L10N("Client:Main:AddFavorite");

            mapContextMenu.Open(GetCursorPoint());
        }

        private bool CanDeleteMap()
        {
            return Map != null && !Map.Official && !isMultiplayer;
        }

        private void DeleteMapConfirmation()
        {
            if (Map == null)
                return;

            var messageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "Delete Confirmation".L10N("Client:Main:DeleteMapConfirmTitle"),
                string.Format("Are you sure you wish to delete the custom map {0}?".L10N("Client:Main:DeleteMapConfirmText"), Map.Name));
            messageBox.YesClickedAction = DeleteSelectedMap;
        }

        private void MapPreviewBox_ToggleFavorite(object sender, EventArgs e) =>
            ToggleFavoriteMap();

        protected virtual void ToggleFavoriteMap()
        {
            if (GameModeMap != null)
            { 
                GameModeMap.IsFavorite = UserINISettings.Instance.ToggleFavoriteMap(Map.SHA1, GameMode.Name, GameModeMap.IsFavorite);
                MapPreviewBox.RefreshFavoriteBtn();
            }
        }

        protected void RefreshForFavoriteMapRemoved()
        {
            if (!gameModeMapFilter.GetGameModeMaps().Any())
            {
                LoadDefaultGameModeMap();
                return;
            }

            ListMaps();
            if (IsFavoriteMapsSelected())
                lbGameModeMapList.SelectedIndex = 0; // the map was removed while viewing favorites
        }

        private void DeleteSelectedMap(XNAMessageBox messageBox)
        {
            try
            {
                MapLoader.DeleteCustomMap(GameModeMap);

                tbMapSearch.Text = string.Empty;
                if (GameMode.Maps.Count == 0)
                {
                    // this will trigger another GameMode to be selected
                    GameModeMap = GameModeMaps.Find(gm => gm.GameMode.Maps.Count > 0);
                }
                else
                {
                    // this will trigger another Map to be selected
                    lbGameModeMapList.SelectedIndex = lbGameModeMapList.SelectedIndex == 0 ? 1 : lbGameModeMapList.SelectedIndex - 1;
                }

                ListMaps();
                ChangeMap(GameModeMap);
            }
            catch (IOException ex)
            {
                Logger.Log($"Deleting map {Map.BaseFilePath} failed! Message: {ex.ToString()}");
                XNAMessageBox.Show(WindowManager, "Deleting Map Failed".L10N("Client:Main:DeleteMapFailedTitle"),
                    "Deleting map failed! Reason:".L10N("Client:Main:DeleteMapFailedText") + " " + ex.Message);
            }
        }

        private void LbGameModeMapList_SelectedIndexChanged()
        {
            if (lbGameModeMapList.SelectedIndex < 0 || lbGameModeMapList.SelectedIndex >= lbGameModeMapList.ItemCount)
            {
                ChangeMap(null);
                return;
            }

            XNAListBoxItem item = lbGameModeMapList.GetItem(1, lbGameModeMapList.SelectedIndex);

            GameModeMap gameModeMap = (GameModeMap)item.Tag;

            ChangeMap(gameModeMap);
        }

        private void LbGameModeMapList_SelectedIndexChanged(object sender, EventArgs e)
            => LbGameModeMapList_SelectedIndexChanged();

        private void LbGameModeMapList_HoveredIndexChanged(object sender, EventArgs e)
        {
            if (lbGameModeMapList.HoveredIndex < 0 || lbGameModeMapList.HoveredIndex >= lbGameModeMapList.ItemCount)
            {
                mapListTooltip.Text = string.Empty;
                return;
            }

            var gmm = (GameModeMap)lbGameModeMapList.GetItem(1, lbGameModeMapList.HoveredIndex).Tag;

            if (gmm.Map.UntranslatedName != gmm.Map.Name)
                mapListTooltip.Text = "Original name:".L10N("Client:Main:OriginalMapName") + " " + gmm.Map.UntranslatedName;
            else
                mapListTooltip.Text = string.Empty;
        }

        private void PickRandomMap()
        {
            int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
                   + AIPlayers.Count;
            List<Map> maps = GetMapList(totalPlayerCount);
            if (maps.Count < 1)
                return;

            int randomValue = random.Next(0, maps.Count);
            bool isFavoriteMapsSelected = IsFavoriteMapsSelected();
            GameModeMap = GameModeMaps.Find(gmm => (gmm.GameMode == GameMode || gmm.IsFavorite && isFavoriteMapsSelected) && gmm.Map == maps[randomValue]);
            Logger.Log("PickRandomMap: Rolled " + randomValue + " out of " + maps.Count + ". Picked map: " + Map.Name);

            ChangeMap(GameModeMap);
            tbMapSearch.Text = string.Empty;
            tbMapSearch.OnSelectedChanged();
            ListMaps();
        }

        private List<Map> GetMapList(int playerCount)
        {
            List<Map> maps = IsFavoriteMapsSelected()
                ? GetFavoriteGameModeMaps().Select(gmm => gmm.Map).ToList()
                : GameMode?.Maps.ToList() ?? new List<Map>();

            if (playerCount != 1)
            {
                maps = maps.Where(x => x.MaxPlayers == playerCount).ToList();
                if (maps.Count < 1 && playerCount <= MAX_PLAYER_COUNT)
                    return GetMapList(playerCount + 1);
            }

            return maps;
        }

        /// <summary>
        /// Refreshes the game mode filter dropdown to include all current game modes.
        /// </summary>
        protected void RefreshGameModeFilter()
        {
            string currentSelection = ddGameModeMapFilter.SelectedItem?.Text;

            ddGameModeMapFilter.SelectedIndexChanged -= DdGameModeMapFilter_SelectedIndexChanged;
            ddGameModeMapFilter.Items.Clear();

            ddGameModeMapFilter.AddItem(CreateGameFilterItem(FavoriteMapsLabel, new GameModeMapFilter(GetFavoriteGameModeMaps)));
            foreach (GameMode gm in GameModeMaps.GameModes)
                ddGameModeMapFilter.AddItem(CreateGameFilterItem(gm.UIName, new GameModeMapFilter(GetGameModeMaps(gm))));

            int selectedIndex = ddGameModeMapFilter.Items.FindIndex(i => i.Text == currentSelection);
            ddGameModeMapFilter.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

            ddGameModeMapFilter.SelectedIndexChanged += DdGameModeMapFilter_SelectedIndexChanged;
            gameModeMapFilter = ddGameModeMapFilter.SelectedItem.Tag as GameModeMapFilter;
        }

        /// <summary>
        /// Refreshes the map selection UI to match the currently selected map
        /// and game mode.
        /// </summary>
        protected void RefreshMapSelectionUI()
        {
            if (GameMode == null)
                return;

            int gameModeMapFilterIndex = ddGameModeMapFilter.Items.FindIndex(i => i.Text == GameMode.UIName);

            if (gameModeMapFilterIndex == -1)
                return;

            if (ddGameModeMapFilter.SelectedIndex == gameModeMapFilterIndex)
                DdGameModeMapFilter_SelectedIndexChanged(this, EventArgs.Empty);

            ddGameModeMapFilter.SelectedIndex = gameModeMapFilterIndex;
        }

        protected void AddSideToDropDown(XNADropDown dd, string name, string? uiName = null, Texture2D? texture = null)
        {
            XNADropDownItem item = new()
            {
                Text = uiName ?? name.L10N($"INI:Sides:{name}"),
                Tag = name,
                Texture = texture ?? LoadTextureOrNull(name + "icon.png"),
            };
            dd.AddItem(item);
        }

        /// <summary>
        /// Initializes the player option drop-down controls.
        /// </summary>
        protected void InitPlayerOptionDropdowns()
        {
            ddPlayerNames = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerSides = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerColors = new XNAClientColorDropDown[MAX_PLAYER_COUNT];
            ddPlayerStarts = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerTeams = new XNAClientDropDown[MAX_PLAYER_COUNT];

            int playerOptionVecticalMargin = ConfigIni.GetIntValue(Name, "PlayerOptionVerticalMargin", PLAYER_OPTION_VERTICAL_MARGIN);
            int playerOptionHorizontalMargin = ConfigIni.GetIntValue(Name, "PlayerOptionHorizontalMargin", PLAYER_OPTION_HORIZONTAL_MARGIN);
            int playerOptionCaptionLocationY = ConfigIni.GetIntValue(Name, "PlayerOptionCaptionLocationY", PLAYER_OPTION_CAPTION_Y);
            int playerNameWidth = ConfigIni.GetIntValue(Name, "PlayerNameWidth", 136);
            int sideWidth = ConfigIni.GetIntValue(Name, "SideWidth", 91);
            int colorWidth = ConfigIni.GetIntValue(Name, "ColorWidth", 79);
            int startWidth = ConfigIni.GetIntValue(Name, "StartWidth", 49);
            int teamWidth = ConfigIni.GetIntValue(Name, "TeamWidth", 46);
            int locationX = ConfigIni.GetIntValue(Name, "PlayerOptionLocationX", 25);
            int locationY = ConfigIni.GetIntValue(Name, "PlayerOptionLocationY", 24);

            // InitPlayerOptionDropdowns(136, 91, 79, 49, 46, new Point(25, 24));

            string[] sides = ClientConfiguration.Instance.Sides.Split(',').ToArray();
            SideCount = sides.Length;

            List<string> selectorNames = new();
            GetRandomSelectors(selectorNames, RandomSelectors);
            RandomSelectorCount = RandomSelectors.Count + 1;
            MapPreviewBox.RandomSelectorCount = RandomSelectorCount;

            string randomColor = GameOptionsIni.GetStringValue("General", "RandomColor", "255,255,255");

            for (int i = MAX_PLAYER_COUNT - 1; i > -1; i--)
            {
                var ddPlayerName = new XNAClientDropDown(WindowManager);
                ddPlayerName.Name = "ddPlayerName" + i;
                ddPlayerName.ClientRectangle = new Rectangle(locationX,
                    locationY + (DROP_DOWN_HEIGHT + playerOptionVecticalMargin) * i,
                    playerNameWidth, DROP_DOWN_HEIGHT);
                ddPlayerName.AddItem(String.Empty);
                ProgramConstants.AI_PLAYER_NAMES.ForEach(ddPlayerName.AddItem);
                ddPlayerName.AllowDropDown = true;
                ddPlayerName.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerName.RightClick += MultiplayerName_RightClick;
                ddPlayerName.Tag = true;

                var ddPlayerSide = new XNAClientDropDown(WindowManager);
                ddPlayerSide.Name = "ddPlayerSide" + i;
                ddPlayerSide.ClientRectangle = new Rectangle(
                    ddPlayerName.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, sideWidth, DROP_DOWN_HEIGHT);

                const string randomName = "Random";
                AddSideToDropDown(ddPlayerSide, randomName, randomName.L10N("Client:Sides:RandomSide"), LoadTextureOrNull("randomicon.png"));

                foreach (string randomSelector in selectorNames)
                    AddSideToDropDown(ddPlayerSide, randomSelector);
                foreach (string sideName in sides)
                    AddSideToDropDown(ddPlayerSide, sideName);

                ddPlayerSide.AllowDropDown = false;
                ddPlayerSide.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerSide.Tag = true;

                var ddPlayerColor = new XNAClientColorDropDown(WindowManager);
                ddPlayerColor.Name = "ddPlayerColor" + i;
                ddPlayerColor.ClientRectangle = new Rectangle(
                    ddPlayerSide.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, colorWidth, DROP_DOWN_HEIGHT);
                ddPlayerColor.AddItem("Random".L10N("Client:Main:RandomColor"), AssetLoader.GetColorFromString(randomColor));
                foreach (MultiplayerColor mpColor in MPColors)
                    ddPlayerColor.AddItem(mpColor.Name, mpColor.XnaColor);
                ddPlayerColor.AllowDropDown = false;
                ddPlayerColor.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerColor.Tag = false;

                var ddPlayerTeam = new XNAClientDropDown(WindowManager);
                ddPlayerTeam.Name = "ddPlayerTeam" + i;
                ddPlayerTeam.ClientRectangle = new Rectangle(
                    ddPlayerColor.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, teamWidth, DROP_DOWN_HEIGHT);
                ddPlayerTeam.AddItem("-");
                ProgramConstants.TEAMS.ForEach(ddPlayerTeam.AddItem);
                ddPlayerTeam.AllowDropDown = false;
                ddPlayerTeam.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerTeam.Tag = true;

                var ddPlayerStart = new XNAClientDropDown(WindowManager);
                ddPlayerStart.Name = "ddPlayerStart" + i;
                ddPlayerStart.ClientRectangle = new Rectangle(
                    ddPlayerTeam.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, startWidth, DROP_DOWN_HEIGHT);
                for (int j = 1; j < 9; j++)
                    ddPlayerStart.AddItem(j.ToString());
                ddPlayerStart.AllowDropDown = false;
                ddPlayerStart.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerStart.Visible = false;
                ddPlayerStart.Enabled = false;
                ddPlayerStart.Tag = true;

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

                ReadINIForControl(ddPlayerName);
                ReadINIForControl(ddPlayerSide);
                ReadINIForControl(ddPlayerColor);
                ReadINIForControl(ddPlayerStart);
                ReadINIForControl(ddPlayerTeam);
            }

            var lblName = GeneratePlayerOptionCaption("lblName", "PLAYER".L10N("Client:Main:PlayerOptionPlayer"), ddPlayerNames[0].X, playerOptionCaptionLocationY);
            var lblSide = GeneratePlayerOptionCaption("lblSide", "SIDE".L10N("Client:Main:PlayerOptionSide"), ddPlayerSides[0].X, playerOptionCaptionLocationY);
            var lblColor = GeneratePlayerOptionCaption("lblColor", "COLOR".L10N("Client:Main:PlayerOptionColor"), ddPlayerColors[0].X, playerOptionCaptionLocationY);

            var lblStart = GeneratePlayerOptionCaption("lblStart", "START".L10N("Client:Main:PlayerOptionStart"), ddPlayerStarts[0].X, playerOptionCaptionLocationY);
            lblStart.Visible = false;

            var lblTeam = GeneratePlayerOptionCaption("lblTeam", "TEAM".L10N("Client:Main:PlayerOptionTeam"), ddPlayerTeams[0].X, playerOptionCaptionLocationY);

            ReadINIForControl(lblName);
            ReadINIForControl(lblSide);
            ReadINIForControl(lblColor);
            ReadINIForControl(lblStart);
            ReadINIForControl(lblTeam);

            btnPlayerExtraOptionsOpen = FindChild<XNAClientButton>(nameof(btnPlayerExtraOptionsOpen), true);
            if (btnPlayerExtraOptionsOpen != null)
            {
                PlayerExtraOptionsPanel = FindChild<PlayerExtraOptionsPanel>(nameof(PlayerExtraOptionsPanel));

                foreach (var child in PlayerExtraOptionsPanel.Children)
                {
                    ReadINIForControl(child);
                }

                PlayerExtraOptionsPanel.Disable();
                PlayerExtraOptionsPanel.OptionsChanged += PlayerExtraOptions_OptionsChanged;
                btnPlayerExtraOptionsOpen.LeftClick += BtnPlayerExtraOptions_LeftClick;
            }

            CheckDisallowedSides();
        }

        private XNALabel GeneratePlayerOptionCaption(string name, string text, int x, int y)
        {
            var label = new XNALabel(WindowManager);
            label.Name = name;
            label.Text = text;
            label.FontIndex = 1;
            label.ClientRectangle = new Rectangle(x, y, 0, 0);
            PlayerOptionsPanel.AddChild(label);

            return label;
        }

        protected virtual void PlayerExtraOptions_OptionsChanged(object sender, EventArgs e)
        {
            var playerExtraOptions = GetPlayerExtraOptions();

            for (int i = 0; i < ddPlayerSides.Length; i++)
                EnablePlayerOptionDropDown(ddPlayerSides[i], i, !playerExtraOptions.IsForceRandomSides);

            for (int i = 0; i < ddPlayerTeams.Length; i++)
                EnablePlayerOptionDropDown(ddPlayerTeams[i], i, !playerExtraOptions.IsForceRandomTeams);

            for (int i = 0; i < ddPlayerColors.Length; i++)
                EnablePlayerOptionDropDown(ddPlayerColors[i], i, !playerExtraOptions.IsForceRandomColors);

            for (int i = 0; i < ddPlayerStarts.Length; i++)
                EnablePlayerOptionDropDown(ddPlayerStarts[i], i, !playerExtraOptions.IsForceRandomStarts);

            UpdateMapPreviewBoxEnabledStatus();
            RefreshBtnPlayerExtraOptionsOpenTexture();
        }

        private void EnablePlayerOptionDropDown(XNAClientDropDown clientDropDown, int playerIndex, bool enable)
        {
            var pInfo = GetPlayerInfoForIndex(playerIndex);
            var allowOtherPlayerOptionsChange = AllowPlayerOptionsChange() && pInfo != null;
            clientDropDown.AllowDropDown = enable && (allowOtherPlayerOptionsChange || pInfo?.Name == ProgramConstants.PLAYERNAME);
            if (!clientDropDown.AllowDropDown)
                clientDropDown.SelectedIndex = clientDropDown.SelectedIndex > 0 ? 0 : clientDropDown.SelectedIndex;
        }

        protected PlayerInfo GetPlayerInfoForIndex(int playerIndex)
        {
            if (playerIndex < Players.Count)
                return Players[playerIndex];

            if (playerIndex < Players.Count + AIPlayers.Count)
                return AIPlayers[playerIndex - Players.Count];

            return null;
        }

        protected PlayerExtraOptions GetPlayerExtraOptions() =>
            PlayerExtraOptionsPanel == null ? new PlayerExtraOptions() : PlayerExtraOptionsPanel.GetPlayerExtraOptions();

        protected void SetPlayerExtraOptions(PlayerExtraOptions playerExtraOptions) => PlayerExtraOptionsPanel?.SetPlayerExtraOptions(playerExtraOptions);

        protected string GetTeamMappingsError() => GetPlayerExtraOptions()?.GetTeamMappingsError();

        private Texture2D LoadTextureOrNull(string name) =>
            AssetLoader.AssetExists(name) ? AssetLoader.LoadTexture(name) : null;

        /// <summary>
        /// Loads random side selectors from GameOptions.ini.
        /// </summary>
        /// <param name="selectorNames">The UI names of random selectors.</param>
        /// <param name="selectorSides">The side IDs to choose from for the selectors.</param>
        private void GetRandomSelectors(List<string> selectorNames, List<int[]> selectorSides)
        {
            List<string> keys = GameOptionsIni.GetSectionKeys("RandomSelectors");

            if (keys == null)
                return;

            foreach (string randomSelector in keys)
            {
                List<int> randomSides = new List<int>();
                try
                {
                    string[] tmp = GameOptionsIni.GetStringListValue("RandomSelectors", randomSelector, string.Empty);
                    randomSides = Array.ConvertAll(tmp, int.Parse).ToList();
                    randomSides.RemoveAll(x => (x >= SideCount || x < 0));
                }
                catch (FormatException) { }

                if (randomSides.Count > 1)
                {
                    selectorNames.Add(randomSelector);
                    selectorSides.Add(randomSides.ToArray());
                }
            }
        }

        protected abstract void BtnLaunchGame_LeftClick(object sender, EventArgs e);

        protected abstract void BtnLeaveGame_LeftClick(object sender, EventArgs e);

        /// <summary>
        /// Updates Discord Rich Presence with actual information.
        /// </summary>
        /// <param name="resetTimer">Whether to restart the "Elapsed" timer or not</param>
        protected abstract void UpdateDiscordPresence(bool resetTimer = false);

        /// <summary>
        /// Resets Discord Rich Presence to default state.
        /// </summary>
        protected void ResetDiscordPresence() => discordHandler.UpdatePresence();

        protected void LoadDefaultGameModeMap()
        {
            if (ddGameModeMapFilter.Items.Count > 0)
            {
                ddGameModeMapFilter.SelectedIndex = GetDefaultGameModeMapFilterIndex();

                lbGameModeMapList.SelectedIndex = 0;
            }
        }

        protected int GetDefaultGameModeMapFilterIndex()
        {
            return ddGameModeMapFilter.Items.FindIndex(i => (i.Tag as GameModeMapFilter)?.Any() ?? false);
        }

        protected GameModeMapFilter GetDefaultGameModeMapFilter()
        {
            return ddGameModeMapFilter.Items[GetDefaultGameModeMapFilterIndex()].Tag as GameModeMapFilter;
        }

        private int GetSpectatorSideIndex() => SideCount + RandomSelectorCount;

        /// <summary>
        /// Applies disallowed side indexes to the side option drop-downs
        /// and player options for human or computer players.
        /// </summary>
        protected void CheckDisallowedSidesForGroup(bool forHumanPlayers)
        {
            var disallowedSideArray = GetDisallowedSidesForGroup(forHumanPlayers);
            var playerInfos = forHumanPlayers ? Players : AIPlayers;
            int defaultSide = 0;
            int allowedSideCount = disallowedSideArray.Count(b => b == false);

            if (allowedSideCount == 1)
            {
                // Disallow Random

                for (int i = 0; i < disallowedSideArray.Length; i++)
                {
                    if (!disallowedSideArray[i])
                        defaultSide = i + RandomSelectorCount;
                }

                foreach (PlayerInfo pInfo in playerInfos)
                {
                    var dd = ddPlayerSides[pInfo.Index];
                    for (int i = 0; i < RandomSelectorCount; i++)
                        dd.Items[i].Selectable = false;
                }
            }
            else
            {
                foreach (PlayerInfo pInfo in playerInfos)
                {
                    var dd = ddPlayerSides[pInfo.Index];
                    for (int i = 0; i < RandomSelectorCount; i++)
                        dd.Items[i].Selectable = true;
                }
            }

            // Disable custom random groups if all or all except one of included sides are unavailable.
            int c = 0;
            foreach (int[] randomSides in RandomSelectors)
            {
                int disableCount = 0;

                foreach (int side in randomSides)
                {
                    if (disallowedSideArray[side])
                        disableCount++;
                }

                bool disabled = disableCount >= randomSides.Length - 1;

                foreach (PlayerInfo pInfo in playerInfos)
                {
                    var dd = ddPlayerSides[pInfo.Index];
                    dd.Items[1 + c].Selectable = !disabled;

                    if (pInfo.SideId == 1 + c && disabled)
                        pInfo.SideId = defaultSide;
                }

                c++;
            }

            // Go over the side array and either disable or enable the side
            // dropdown options depending on whether the side is available
            for (int i = 0; i < disallowedSideArray.Length; i++)
            {
                bool disabled = disallowedSideArray[i];

                if (disabled)
                {
                    // Change the sides of players that use the disabled
                    // side to the default side
                    foreach (PlayerInfo pInfo in playerInfos)
                    {
                        var dd = ddPlayerSides[pInfo.Index];
                        dd.Items[i + RandomSelectorCount].Selectable = false;

                        if (pInfo.SideId == i + RandomSelectorCount)
                            pInfo.SideId = defaultSide;
                    }
                }
                else
                {
                    foreach (PlayerInfo pInfo in playerInfos)
                    {
                        var dd = ddPlayerSides[pInfo.Index];
                        dd.Items[i + RandomSelectorCount].Selectable = true;
                    }
                }
            }

            // If only 1 side is allowed, change all players' sides to that
            if (allowedSideCount == 1)
            {
                foreach (PlayerInfo pInfo in playerInfos)
                {
                    if (pInfo.SideId == 0)
                        pInfo.SideId = defaultSide;
                }
            }

            if (Map != null && Map.CoopInfo != null)
            {
                // Disallow spectator

                foreach (PlayerInfo pInfo in playerInfos)
                {
                    if (pInfo.SideId == GetSpectatorSideIndex())
                        pInfo.SideId = defaultSide;
                }

                foreach (PlayerInfo pInfo in playerInfos)
                {
                    var dd = ddPlayerSides[pInfo.Index];
                    if (dd.Items.Count > GetSpectatorSideIndex())
                        dd.Items[SideCount + RandomSelectorCount].Selectable = false;
                }
            }
            else
            {
                foreach (PlayerInfo pInfo in playerInfos)
                {
                    var dd = ddPlayerSides[pInfo.Index];
                    if (dd.Items.Count > SideCount + RandomSelectorCount)
                        dd.Items[SideCount + RandomSelectorCount].Selectable = true;
                }
            }
        }

        /// <summary>
        /// Applies disallowed side indexes to the side option drop-downs
        /// and player options.
        /// </summary>
        protected void CheckDisallowedSides()
        {
            CheckDisallowedSidesForGroup(forHumanPlayers:false);
            CheckDisallowedSidesForGroup(forHumanPlayers:true);
        }

        /// <summary>
        /// Gets a list of side indexes that are disallowed for human or computer players.
        /// </summary>
        /// <returns>A list of disallowed side indexes.</returns>
        protected bool[] GetDisallowedSidesForGroup(bool forHumanPlayers)
        {
            var returnValue = GetDisallowedSides();
            var sides = forHumanPlayers ? GameMode?.DisallowedHumanPlayerSides : GameMode?.DisallowedComputerPlayerSides;
            if (sides != null)
            {
                foreach (int i in sides)
                    returnValue[i] = true;
            }

            return returnValue;
        }

        /// <summary>
        /// Gets a list of side indexes that are disallowed.
        /// </summary>
        /// <returns>A list of disallowed side indexes.</returns>
        protected bool[] GetDisallowedSides()
        {
            var returnValue = new bool[SideCount];

            if (Map != null && Map.CoopInfo != null)
            {
                // Co-Op map disallowed side logic

                foreach (int disallowedSideIndex in Map.CoopInfo.DisallowedPlayerSides)
                    returnValue[disallowedSideIndex] = true;
            }

            if (GameMode != null)
            {
                foreach (int disallowedSideIndex in GameMode.DisallowedPlayerSides)
                    returnValue[disallowedSideIndex] = true;
            }

            foreach (var checkBox in CheckBoxes)
                checkBox.ApplyDisallowedSideIndex(returnValue);

            return returnValue;
        }

        /// <summary>
        /// Randomizes options of both human and AI players
        /// and returns the options as an array of PlayerHouseInfos.
        /// </summary>
        /// <returns>An array of PlayerHouseInfos.</returns>
        protected virtual PlayerHouseInfo[] Randomize(List<TeamStartMapping> teamStartMappings)
        {
            int totalPlayerCount = Players.Count + AIPlayers.Count;
            PlayerHouseInfo[] houseInfos = new PlayerHouseInfo[totalPlayerCount];

            for (int i = 0; i < totalPlayerCount; i++)
                houseInfos[i] = new PlayerHouseInfo();

            // Gather list of spectators
            for (int i = 0; i < Players.Count; i++)
                houseInfos[i].IsSpectator = Players[i].SideId == GetSpectatorSideIndex();

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
                freeStartingLocations.Remove(AIPlayers[i].StartingLocation - 1);

            foreach (var teamStartMapping in teamStartMappings.Where(mapping => mapping.IsBlock))
                freeStartingLocations.Remove(teamStartMapping.StartingWaypoint);

            // Randomize options

            Random pseudoRandom = new Random(RandomSeed);

            for (int i = 0; i < totalPlayerCount; i++)
            {
                PlayerInfo pInfo;
                PlayerHouseInfo pHouseInfo = houseInfos[i];
                bool[] disallowedSides;

                if (i < Players.Count)
                {
                    pInfo = Players[i];
                    disallowedSides = GetDisallowedSidesForGroup(forHumanPlayers:true);
                }
                else
                {
                    pInfo = AIPlayers[i - Players.Count];
                    disallowedSides = GetDisallowedSidesForGroup(forHumanPlayers:false);
                }

                pHouseInfo.RandomizeSide(pInfo, SideCount, pseudoRandom, disallowedSides, RandomSelectors, RandomSelectorCount);

                pHouseInfo.RandomizeColor(pInfo, freeColors, MPColors, pseudoRandom);
                pHouseInfo.RandomizeStart(pInfo, pseudoRandom, freeStartingLocations, takenStartingLocations, teamStartMappings.Any());
            }

            return houseInfos;
        }

        /// <summary>
        /// Writes spawn.ini. Returns the player house info returned from the randomizer.
        /// </summary>
        private PlayerHouseInfo[] WriteSpawnIni()
        {
            Logger.Log("Writing spawn.ini");

            FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

            spawnerSettingsFile.Delete();

            if (Map.IsCoop)
            {
                foreach (PlayerInfo pInfo in Players)
                    pInfo.TeamId = 1;

                foreach (PlayerInfo pInfo in AIPlayers)
                    pInfo.TeamId = 1;
            }

            var teamStartMappings = new List<TeamStartMapping>(0);
            if (PlayerExtraOptionsPanel != null)
            {
                teamStartMappings = PlayerExtraOptionsPanel.GetTeamStartMappings();
            }

            PlayerHouseInfo[] houseInfos = Randomize(teamStartMappings);

            IniFile spawnIni = new IniFile(spawnerSettingsFile.FullName);

            IniSection settings = new IniSection("Settings");

            settings.SetStringValue("Name", ProgramConstants.PLAYERNAME);
            settings.SetStringValue("Scenario", ProgramConstants.SPAWNMAP_INI);
            settings.SetStringValue("UIGameMode", GameMode.UntranslatedUIName);
            settings.SetStringValue("UIMapName", Map.UntranslatedName);

            // needed for translation in game loading lobbies
            if (Map.Official)
                settings.SetStringValue("MapID", Map.BaseFilePath);

            settings.SetIntValue("PlayerCount", Players.Count);
            int myIndex = Players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME);
            settings.SetIntValue("Side", houseInfos[myIndex].InternalSideIndex);
            settings.SetBooleanValue("IsSpectator", houseInfos[myIndex].IsSpectator);
            settings.SetIntValue("Color", houseInfos[myIndex].ColorIndex);
            settings.SetStringValue("CustomLoadScreen", LoadingScreenController.GetLoadScreenName(houseInfos[myIndex].InternalSideIndex.ToString()));
            settings.SetIntValue("AIPlayers", AIPlayers.Count);
            settings.SetIntValue("Seed", RandomSeed);
            if (GetPvPTeamCount() > 1)
                settings.SetBooleanValue("CoachMode", true);
            if (GetGameType() == GameType.Coop)
                settings.SetBooleanValue("AutoSurrender", false);
            spawnIni.AddSection(settings);
            WriteSpawnIniAdditions(spawnIni);

            foreach (GameLobbyCheckBox chkBox in CheckBoxes)
                chkBox.ApplySpawnIniCode(spawnIni);

            foreach (GameLobbyDropDown dd in DropDowns)
                dd.ApplySpawnIniCode(spawnIni);

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
                spawnIni.SetIntValue(sectionName, "Side", pHouseInfo.InternalSideIndex);
                spawnIni.SetBooleanValue(sectionName, "IsSpectator", pHouseInfo.IsSpectator);
                spawnIni.SetIntValue(sectionName, "Color", pHouseInfo.ColorIndex);
                spawnIni.SetStringValue(sectionName, "Ip", GetIPAddressForPlayer(pInfo));
                spawnIni.SetIntValue(sectionName, "Port", pInfo.Port);

                otherId++;
            }

            // The spawner assigns players to SpawnX houses based on their in-game color index
            List<int> multiCmbIndexes = new List<int>();
            var sortedColorList = MPColors.OrderBy(mpc => mpc.GameColorIndex).ToList();

            for (int cId = 0; cId < sortedColorList.Count; cId++)
            {
                for (int pId = 0; pId < Players.Count; pId++)
                {
                    if (houseInfos[pId].ColorIndex == sortedColorList[cId].GameColorIndex)
                        multiCmbIndexes.Add(pId);
                }
            }

            if (AIPlayers.Count > 0)
            {
                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
                    int multiId = multiCmbIndexes.Count + aiId + 1;

                    string keyName = "Multi" + multiId;

                    spawnIni.SetIntValue("HouseHandicaps", keyName, AIPlayers[aiId].HouseHandicapAILevel);
                    spawnIni.SetIntValue("HouseCountries", keyName, houseInfos[Players.Count + aiId].InternalSideIndex);
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
            AllianceHolder.WriteInfoToSpawnIni(Players, AIPlayers, multiCmbIndexes, houseInfos.ToList(), teamStartMappings, spawnIni);

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

        /// <summary>
        /// Returns the number of teams with human players in them.
        /// Does not count spectators and human players that don't have a team set.
        /// </summary>
        /// <returns>The number of human player teams in the game.</returns>
        private int GetPvPTeamCount()
        {
            int[] teamPlayerCounts = new int[4];
            int playerTeamCount = 0;

            foreach (PlayerInfo pInfo in Players)
            {
                if (pInfo.IsAI || IsPlayerSpectator(pInfo))
                    continue;

                if (pInfo.TeamId > 0)
                {
                    teamPlayerCounts[pInfo.TeamId - 1]++;
                    if (teamPlayerCounts[pInfo.TeamId - 1] == 2)
                        playerTeamCount++;
                }
            }

            return playerTeamCount;
        }

        /// <summary>
        /// Checks whether the specified player has selected Spectator as their side.
        /// </summary>
        /// <param name="pInfo">The player.</param>
        /// <returns>True if the player is a spectator, otherwise false.</returns>
        protected bool IsPlayerSpectator(PlayerInfo pInfo)
        {
            if (pInfo.SideId == GetSpectatorSideIndex())
                return true;

            return false;
        }

        protected virtual string GetIPAddressForPlayer(PlayerInfo player) => "0.0.0.0";

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
                Map.UntranslatedName, GameMode.UntranslatedUIName, Players.Count, Map.IsCoop);

            bool isValidForStar = true;
            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                if (!checkBox.AllowScoring)
                {
                    isValidForStar = false;
                    break;
                }
            }
            foreach (GameLobbyDropDown dropDown in DropDowns)
            {
                if (!dropDown.AllowScoring)
                {
                    isValidForStar = false;
                    break;
                }
            }

            matchStatistics.IsValidForStar = isValidForStar;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];
                matchStatistics.AddPlayer(pInfo.Name, pInfo.Name == ProgramConstants.PLAYERNAME,
                    false, pInfo.SideId == SideCount + RandomSelectorCount, houseInfos[pId].SideIndex + 1, pInfo.TeamId,
                    MPColors.FindIndex(c => c.GameColorIndex == houseInfos[pId].ColorIndex), 10);
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                var pHouseInfo = houseInfos[Players.Count + aiId];
                PlayerInfo aiInfo = AIPlayers[aiId];
                matchStatistics.AddPlayer("Computer", false, true, false,
                    pHouseInfo.SideIndex + 1, aiInfo.TeamId,
                    MPColors.FindIndex(c => c.GameColorIndex == pHouseInfo.ColorIndex),
                    aiInfo.AILevel);
            }
        }

        /// <summary>
        /// Writes spawnmap.ini.
        /// </summary>
        private void WriteMap(PlayerHouseInfo[] houseInfos)
        {
            FileInfo spawnMapIniFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNMAP_INI);

            DeleteSupplementalMapFiles();
            spawnMapIniFile.Delete();

            Logger.Log("Writing map.");

            Logger.Log("Loading map INI from " + Map.CompleteFilePath);

            IniFile mapIni = Map.GetMapIni();

            IniFile globalCodeIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "Map Code", "GlobalCode.ini"));

            MapCodeHelper.ApplyMapCode(mapIni, GameMode.GetMapRulesIniFile());
            MapCodeHelper.ApplyMapCode(mapIni, globalCodeIni);

            if (isMultiplayer)
            {
                IniFile mpGlobalCodeIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "Map Code", "MultiplayerGlobalCode.ini"));
                MapCodeHelper.ApplyMapCode(mapIni, mpGlobalCodeIni);
            }
            else
            {
                // Avoid writing the original filename to spawnmap.ini MP games, as it may vary between systems, e.g., when a host uploads a map while other players in game might download it with a diffrent filename.
                // This inconsistency can result in differing spawnmap.ini files among players, causing desyncs in CnCNet YR games.
                // Theoretically it can be useful for some singleplayer campaign tracking
                // But it isn't currently used by any CnCNet game or mod
                // The code below only applies to the single player case
                string mapIniFileName = Path.GetFileName(mapIni.FileName);
                mapIni.SetStringValue("Basic", "OriginalFilename", mapIniFileName);
            }

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                checkBox.ApplyMapCode(mapIni, GameMode);

            foreach (GameLobbyDropDown dropDown in DropDowns)
                dropDown.ApplyMapCode(mapIni, GameMode);

            mapIni.MoveSectionToFirst("MultiplayerDialogSettings"); // Required by YR

            CopySupplementalMapFiles(mapIni);

            ManipulateStartingLocations(mapIni, houseInfos);

            mapIni.WriteIniFile(spawnMapIniFile.FullName);
        }

        /// <summary>
        /// Some mods require that .map files also have supplemental files copied over with the spawnmap.ini.
        /// 
        /// This function scans the directory containing the map file and looks for other files with the
        /// same base filename as the map file that are allowed by the client configuration.
        /// Those files are then copied to the game base path with the base filename of "spawnmap.EXT".
        /// </summary>
        /// <param name="mapIni"></param>
        private void CopySupplementalMapFiles(IniFile mapIni)
        {
            var mapFileInfo = new FileInfo(mapIni.FileName);
            string mapFileBaseName = Path.GetFileNameWithoutExtension(mapFileInfo.Name);

            IEnumerable<string> supplementalMapFiles = GetSupplementalMapFiles(mapFileInfo.DirectoryName, mapFileBaseName).ToList();
            if (!supplementalMapFiles.Any())
                return;

            List<string> supplementalFileNames = new();
            foreach (string file in supplementalMapFiles)
            {
                try
                {
                    // Copy each supplemental file
                    string supplementalFileName = $"spawnmap{Path.GetExtension(file)}";
                    File.Copy(file, SafePath.CombineFilePath(ProgramConstants.GamePath, supplementalFileName), true);
                    supplementalFileNames.Add(supplementalFileName);
                }
                catch (Exception ex)
                {
                    string errorMessage = "Unable to copy supplemental map file".L10N("Client:Main:SupplementalFileCopyError") + $" {file}";
                    Logger.Log(errorMessage);
                    Logger.Log(ex.ToString());
                    XNAMessageBox.Show(WindowManager, "Error".L10N("Client:Main:Error"), errorMessage);
                    
                }
            }
            
            // Write the supplemental map files to the INI (eventual spawnmap.ini)
            mapIni.SetStringValue("Basic", "SupplementalFiles", string.Join(",", supplementalFileNames));
        }

        /// <summary>
        /// Delete all supplemental map files from last spawn
        /// </summary>
        private void DeleteSupplementalMapFiles()
        {
            IEnumerable<string> supplementalMapFilePaths = GetSupplementalMapFiles(ProgramConstants.GamePath, "spawnmap").ToList();
            if (!supplementalMapFilePaths.Any())
                return;

            foreach (string supplementalMapFilename in supplementalMapFilePaths)
            {
                try
                {
                    File.Delete(supplementalMapFilename);
                }
                catch (Exception ex)
                {
                    string errorMessage = "Unable to delete supplemental map file".L10N("Client:Main:SupplementalFileDeleteError") + $" {supplementalMapFilename}";
                    Logger.Log(errorMessage);
                    Logger.Log(ex.ToString());
                    XNAMessageBox.Show(WindowManager, "Error".L10N("Client:Main:Error"), errorMessage);
                }
            }
        }

        private static IEnumerable<string> GetSupplementalMapFiles(string basePath, string baseFileName)
        {
            // Get the supplemental file names for allowable extensions
            var supplementalMapFileNames = ClientConfiguration.Instance.SupplementalMapFileExtensions
                .Select(ext => $"{baseFileName}.{ext}".ToUpperInvariant())
                .ToList();

            if (!supplementalMapFileNames.Any())
                return new List<string>();

            // Get full file paths for all possible supplemental files
            return Directory.GetFiles(basePath, $"{baseFileName}.*")
                .Where(f => supplementalMapFileNames.Contains(Path.GetFileName(f).ToUpperInvariant()));
        }

        private void ManipulateStartingLocations(IniFile mapIni, PlayerHouseInfo[] houseInfos)
        {
            if (RemoveStartingLocations)
            {
                if (Map.EnforceMaxPlayers)
                    return;

                // All random starting locations given by the game
                IniSection waypointSection = mapIni.GetSection("Waypoints");
                if (waypointSection == null)
                    return;

                // TODO implement IniSection.RemoveKey in Rampastring.Tools, then
                // remove implementation that depends on internal implementation
                // of IniSection
                for (int i = 0; i <= 7; i++)
                {
                    int index = waypointSection.Keys.FindIndex(k => !string.IsNullOrEmpty(k.Key) && k.Key == i.ToString());
                    if (index > -1)
                        waypointSection.Keys.RemoveAt(index);
                }
            }

            // Multiple players cannot properly share the same starting location
            // without breaking the SpawnX house logic that pre-placed objects depend on

            // To work around this, we add new starting locations that just point
            // to the same cell coordinates as existing stacked starting locations
            // and make additional players in the same start loc start from the new
            // starting locations instead.

            // As an additional restriction, players can only start from waypoints 0 to 7.
            // That means that if the map already has too many starting waypoints,
            // we need to move existing (but un-occupied) starting waypoints to point
            // to the stacked locations so we can spawn the players there.


            // Check for stacked starting locations (locations with more than 1 player on it)
            bool[] startingLocationUsed = new bool[MAX_PLAYER_COUNT];
            bool stackedStartingLocations = false;
            foreach (PlayerHouseInfo houseInfo in houseInfos)
            {
                if (houseInfo.RealStartingWaypoint > -1)
                {
                    startingLocationUsed[houseInfo.RealStartingWaypoint] = true;

                    // If assigned starting waypoint is unknown while the real
                    // starting location is known, it means that
                    // the location is shared with another player
                    if (houseInfo.StartingWaypoint == -1)
                    {
                        stackedStartingLocations = true;
                    }
                }
            }

            // If any starting location is stacked, re-arrange all starting locations
            // so that unused starting locations are removed and made to point at used
            // starting locations
            if (!stackedStartingLocations)
                return;

            // We also need to modify spawn.ini because WriteSpawnIni
            // doesn't handle stacked positions.
            // We could move this code there, but then we'd have to process
            // the stacked locations in two places (here and in WriteSpawnIni)
            // because we'd need to modify the map anyway.
            // Not sure whether having it like this or in WriteSpawnIni
            // is better, but this implementation is quicker to write for now.
            IniFile spawnIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS));

            // For each player, check if they're sharing the starting location
            // with someone else
            // If they are, find an unused waypoint and assign their
            // starting location to match that
            for (int pId = 0; pId < houseInfos.Length; pId++)
            {
                PlayerHouseInfo houseInfo = houseInfos[pId];

                if (houseInfo.RealStartingWaypoint > -1 &&
                    houseInfo.StartingWaypoint == -1)
                {
                    // Find first unused starting location index
                    int unusedLocation = -1;
                    for (int i = 0; i < startingLocationUsed.Length; i++)
                    {
                        if (!startingLocationUsed[i])
                        {
                            unusedLocation = i;
                            startingLocationUsed[i] = true;
                            break;
                        }
                    }

                    houseInfo.StartingWaypoint = unusedLocation;
                    mapIni.SetIntValue("Waypoints", unusedLocation.ToString(),
                        mapIni.GetIntValue("Waypoints", houseInfo.RealStartingWaypoint.ToString(), 0));
                    spawnIni.SetIntValue("SpawnLocations", $"Multi{pId + 1}", unusedLocation);
                }
            }

            spawnIni.WriteIniFile();
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

            GameProcessLogic.StartGameProcess(WindowManager);
            UpdateDiscordPresence(true);
        }

        private void GameProcessExited_Callback() => AddCallback(new Action(GameProcessExited), null);

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

            Logger.Log("GameProcessExited: Parsing statistics.");

            matchStatistics?.ParseStatistics(ProgramConstants.GamePath, ClientConfiguration.Instance.LocalGame, false);

            Logger.Log("GameProcessExited: Adding match to statistics.");

            StatisticsManager.Instance.AddMatchAndSaveDatabase(true, matchStatistics);

            ClearReadyStatuses();

            CopyPlayerDataToUI();

            UpdateDiscordPresence(true);
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

            var oldSideId = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                pInfo.ColorId = ddPlayerColors[pId].SelectedIndex;
                pInfo.SideId = ddPlayerSides[pId].SelectedIndex;
                pInfo.StartingLocation = ddPlayerStarts[pId].SelectedIndex;
                pInfo.TeamId = ddPlayerTeams[pId].SelectedIndex;

                if (pInfo.SideId == SideCount + RandomSelectorCount)
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
                    AILevel = dd.SelectedIndex - 1,
                    SideId = Math.Max(ddPlayerSides[cmbId].SelectedIndex, 0),
                    ColorId = Math.Max(ddPlayerColors[cmbId].SelectedIndex, 0),
                    StartingLocation = Math.Max(ddPlayerStarts[cmbId].SelectedIndex, 0),
                    TeamId = Map != null && Map.IsCoop ? 1 : Math.Max(ddPlayerTeams[cmbId].SelectedIndex, 0),
                    IsAI = true
                };

                AIPlayers.Add(aiPlayer);
            }

            CopyPlayerDataToUI();
            btnLaunchGame.SetRank(GetRank());

            if (oldSideId != Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId)
                UpdateDiscordPresence();
        }

        /// <summary>
        /// Sets the ready status of all non-host human players to false.
        /// </summary>
        /// <param name="resetAutoReady">If set, players with autoready enabled are reset as well.</param>
        protected void ClearReadyStatuses(bool resetAutoReady = false)
        {
            for (int i = 1; i < Players.Count; i++)
            {
                if (resetAutoReady || !Players[i].AutoReady || Players[i].IsInGame)
                    Players[i].Ready = false;
            }
        }

        private bool CanRightClickMultiplayer(XNADropDownItem selectedPlayer)
        {
            return selectedPlayer != null &&
                   selectedPlayer.Text != ProgramConstants.PLAYERNAME &&
                   !ProgramConstants.AI_PLAYER_NAMES.Contains(selectedPlayer.Text);
        }

        private void MultiplayerName_RightClick(object sender, EventArgs e)
        {
            var selectedPlayer = ((XNADropDown)sender).SelectedItem;
            if (!CanRightClickMultiplayer(selectedPlayer))
                return;

            if (selectedPlayer == null ||
                selectedPlayer.Text == ProgramConstants.PLAYERNAME)
            {
                return;
            }

            MultiplayerNameRightClicked?.Invoke(this, new MultiplayerNameRightClickedEventArgs(selectedPlayer.Text));
        }

        /// <summary>
        /// Applies player information changes done in memory to the UI.
        /// </summary>
        protected virtual void CopyPlayerDataToUI()
        {
            PlayerUpdatingInProgress = true;

            bool allowOptionsChange = AllowPlayerOptionsChange();
            var playerExtraOptions = GetPlayerExtraOptions();

            // Human players
            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                pInfo.Index = pId;

                XNADropDown ddPlayerName = ddPlayerNames[pId];
                ddPlayerName.Items[0].Text = pInfo.Name;
                ddPlayerName.Items[1].Text = string.Empty;
                ddPlayerName.Items[2].Text = "Kick".L10N("Client:Main:Kick");
                ddPlayerName.Items[3].Text = "Ban".L10N("Client:Main:Ban");
                ddPlayerName.SelectedIndex = 0;
                ddPlayerName.AllowDropDown = false;

                bool allowPlayerOptionsChange = allowOptionsChange || pInfo.Name == ProgramConstants.PLAYERNAME;

                ddPlayerSides[pId].SelectedIndex = pInfo.SideId;
                ddPlayerSides[pId].AllowDropDown = !playerExtraOptions.IsForceRandomSides && allowPlayerOptionsChange;

                ddPlayerColors[pId].SelectedIndex = pInfo.ColorId;
                ddPlayerColors[pId].AllowDropDown = !playerExtraOptions.IsForceRandomColors && allowPlayerOptionsChange;

                ddPlayerStarts[pId].SelectedIndex = pInfo.StartingLocation;

                ddPlayerTeams[pId].SelectedIndex = pInfo.TeamId;
                if (GameModeMap != null)
                {
                    ddPlayerTeams[pId].AllowDropDown = !playerExtraOptions.IsForceRandomTeams && allowPlayerOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                    ddPlayerStarts[pId].AllowDropDown = !playerExtraOptions.IsForceRandomStarts && allowPlayerOptionsChange && (Map.IsCoop || !Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations);
                }
            }

            // AI players
            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                PlayerInfo aiInfo = AIPlayers[aiId];

                int index = Players.Count + aiId;

                aiInfo.Index = index;

                XNADropDown ddPlayerName = ddPlayerNames[index];
                ddPlayerName.Items[0].Text = "-";
                ddPlayerName.Items[1].Text = ProgramConstants.AI_PLAYER_NAMES[0];
                ddPlayerName.Items[2].Text = ProgramConstants.AI_PLAYER_NAMES[1];
                ddPlayerName.Items[3].Text = ProgramConstants.AI_PLAYER_NAMES[2];
                ddPlayerName.SelectedIndex = 1 + aiInfo.AILevel;
                ddPlayerName.AllowDropDown = allowOptionsChange;

                ddPlayerSides[index].SelectedIndex = aiInfo.SideId;
                ddPlayerSides[index].AllowDropDown = !playerExtraOptions.IsForceRandomSides && allowOptionsChange;

                ddPlayerColors[index].SelectedIndex = aiInfo.ColorId;
                ddPlayerColors[index].AllowDropDown = !playerExtraOptions.IsForceRandomColors && allowOptionsChange;

                ddPlayerStarts[index].SelectedIndex = aiInfo.StartingLocation;

                ddPlayerTeams[index].SelectedIndex = aiInfo.TeamId;

                if (GameModeMap != null)
                {
                    ddPlayerTeams[index].AllowDropDown = !playerExtraOptions.IsForceRandomTeams && allowOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                    ddPlayerStarts[index].AllowDropDown = !playerExtraOptions.IsForceRandomStarts && allowOptionsChange && (Map.IsCoop || !Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations);
                }
            }

            // Unused player slots
            for (int ddIndex = Players.Count + AIPlayers.Count; ddIndex < MAX_PLAYER_COUNT; ddIndex++)
            {
                XNADropDown ddPlayerName = ddPlayerNames[ddIndex];
                ddPlayerName.AllowDropDown = false;
                ddPlayerName.Items[0].Text = string.Empty;
                ddPlayerName.Items[1].Text = ProgramConstants.AI_PLAYER_NAMES[0];
                ddPlayerName.Items[2].Text = ProgramConstants.AI_PLAYER_NAMES[1];
                ddPlayerName.Items[3].Text = ProgramConstants.AI_PLAYER_NAMES[2];
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
            UpdateMapPreviewBoxEnabledStatus();

            CheckDisallowedSides();

            PlayerUpdatingInProgress = false;
        }

        /// <summary>
        /// Updates the enabled status of starting location selectors
        /// in the map preview box.
        /// </summary>
        protected abstract void UpdateMapPreviewBoxEnabledStatus();

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
        /// Updates the map information labels such as name and author.
        /// </summary>
        protected virtual void SetMapLabels()
        {
            if (GameMode == null || Map == null)
            {
                lblMapName.Text = "Map: Unknown".L10N("Client:Main:MapUnknown");
                lblMapAuthor.Text = "By Unknown Author".L10N("Client:Main:AuthorByUnknown");
                lblGameMode.Text = "Game mode: Unknown".L10N("Client:Main:GameModeUnknown");
                lblMapSize.Text = "Size: Not available".L10N("Client:Main:MapSizeUnknown");
                return;
            }

            lblMapName.Text = "Map:".L10N("Client:Main:Map") + " " + Renderer.GetSafeString(Map.Name, lblMapName.FontIndex);
            lblMapAuthor.Text = "By".L10N("Client:Main:AuthorBy") + " " + Renderer.GetSafeString(Map.Author, lblMapAuthor.FontIndex);
            lblGameMode.Text = "Game mode:".L10N("Client:Main:GameModeLabel") + " " + GameMode.UIName;
            lblMapSize.Text = "Size:".L10N("Client:Main:MapSize") + " " + Map.GetSizeString();
        }

        /// <summary>
        /// Changes the current map and game mode.
        /// </summary>
        /// <param name="gameModeMap">The new game mode map.</param>
        protected virtual void ChangeMap(GameModeMap gameModeMap)
        {
            GameModeMap = gameModeMap;

            _ = UpdateLaunchGameButtonStatus();

            SetMapLabels();

            if (GameMode == null || Map == null)
            {
                MapPreviewBox.GameModeMap = null;
                OnGameOptionChanged();
                return;
            }

            disableGameOptionUpdateBroadcast = true;

            // Clear forced options
            foreach (var ddGameOption in DropDowns)
                ddGameOption.AllowDropDown = true;

            foreach (var checkBox in CheckBoxes)
                checkBox.AllowChecking = true;

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

            ApplyForcedCheckBoxOptions(checkBoxListClone, GameMode.ForcedCheckBoxValues);
            ApplyForcedCheckBoxOptions(checkBoxListClone, Map.ForcedCheckBoxValues);

            ApplyForcedDropDownOptions(dropDownListClone, GameMode.ForcedDropDownValues);
            ApplyForcedDropDownOptions(dropDownListClone, Map.ForcedDropDownValues);

            foreach (var chkBox in checkBoxListClone)
                chkBox.Checked = chkBox.HostChecked;

            foreach (var dd in dropDownListClone)
                dd.SelectedIndex = dd.HostSelectedIndex;

            // Enable all sides by default
            foreach (var ddSide in ddPlayerSides)
            {
                ddSide.Items.ForEach(item => item.Selectable = true);
            }

            // Enable all colors by default
            foreach (var ddColor in ddPlayerColors)
            {
                for (int i = 0; i < ddColor.Items.Count; i++)
                {
                    ddColor.Items[i].Selectable = true;
                    ddColor.SetItemColorEnabled(i, true);
                }
            }

            // Apply starting locations
            foreach (var ddStart in ddPlayerStarts)
            {
                ddStart.Items.Clear();

                ddStart.AddItem("???");

                for (int i = 1; i <= Map.MaxPlayers; i++)
                    ddStart.AddItem(i.ToString());
            }


            // Check if AI players allowed
            bool AIAllowed = !(Map.HumanPlayersOnly || GameMode.HumanPlayersOnly);
            foreach (var ddName in ddPlayerNames)
            {
                if (ddName.Items.Count > 3)
                {
                    ddName.Items[1].Selectable = AIAllowed;
                    ddName.Items[2].Selectable = AIAllowed;
                    ddName.Items[3].Selectable = AIAllowed;
                }
            }

            if (!AIAllowed) AIPlayers.Clear();
            IEnumerable<PlayerInfo> concatPlayerList = Players.Concat(AIPlayers).ToList();

            foreach (PlayerInfo pInfo in concatPlayerList)
            {
                if (pInfo.StartingLocation > Map.MaxPlayers ||
                    (!Map.IsCoop && (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations)))
                    pInfo.StartingLocation = 0;
                if (!Map.IsCoop && (Map.ForceNoTeams || GameMode.ForceNoTeams))
                    pInfo.TeamId = 0;
            }


            if (Map.CoopInfo != null)
            {
                // Co-Op map disallowed color logic
                foreach (int disallowedColorIndex in Map.CoopInfo.DisallowedPlayerColors)
                {
                    if (disallowedColorIndex >= MPColors.Count)
                        continue;

                    foreach (var ddColor in ddPlayerColors)
                    {
                        ddColor.Items[disallowedColorIndex + 1].Selectable = false;
                        ddColor.SetItemColorEnabled(disallowedColorIndex + 1, false);
                    }

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

            MapPreviewBox.GameModeMap = GameModeMap;
            CopyPlayerDataToUI();

            disableGameOptionUpdateBroadcast = false;

            PlayerExtraOptionsPanel?.UpdateForMap(Map);
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
            return ProgramConstants.GetAILevelName(aiLevel);
        }

        protected GameType GetGameType()
        {
            int teamCount = GetPvPTeamCount();

            if (teamCount == 0)
                return GameType.FFA;

            if (teamCount == 1)
                return GameType.Coop;

            return GameType.TeamGame;
        }

        protected Rank GetRank()
        {
            if (GameMode == null || Map == null)
                return Rank.None;

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                if (checkBox.AllowScoring)
                    return Rank.None;
            }
            
            foreach (GameLobbyDropDown dropDown in DropDowns)
            {
                if (dropDown.AllowScoring)
                    return Rank.None;
            }

            PlayerInfo localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

            if (localPlayer == null)
                return Rank.None;

            if (IsPlayerSpectator(localPlayer))
                return Rank.None;

            // These variables are used by both the skirmish and multiplayer code paths
            int[] teamMemberCounts = new int[5];
            int lowestEnemyAILevel = 2;
            int highestAllyAILevel = 0;

            foreach (PlayerInfo aiPlayer in AIPlayers)
            {
                teamMemberCounts[aiPlayer.TeamId]++;

                if (aiPlayer.TeamId > 0 && aiPlayer.TeamId == localPlayer.TeamId)
                {
                    if (aiPlayer.AILevel > highestAllyAILevel)
                        highestAllyAILevel = aiPlayer.AILevel;
                }
                else
                {
                    if (aiPlayer.AILevel < lowestEnemyAILevel)
                        lowestEnemyAILevel = aiPlayer.AILevel;
                }
            }

            if (isMultiplayer)
            {
                if (Players.Count == 1)
                    return Rank.None;

                // PvP stars for 2-player and 3-player maps
                if (Map.MaxPlayers <= 3)
                {
                    List<PlayerInfo> filteredPlayers = Players.Where(p => !IsPlayerSpectator(p)).ToList();

                    if (AIPlayers.Count > 0)
                        return Rank.None;

                    if (filteredPlayers.Count != Map.MaxPlayers)
                        return Rank.None;

                    int localTeamIndex = localPlayer.TeamId;
                    if (localTeamIndex > 0 && filteredPlayers.Count(p => p.TeamId == localTeamIndex) > 1)
                        return Rank.None;

                    return Rank.Hard;
                }

                // Coop stars for maps with 4 or more players
                // See the code in StatisticsManager.GetRankForCoopMatch for the conditions

                if (Players.Find(p => IsPlayerSpectator(p)) != null)
                    return Rank.None;

                if (AIPlayers.Count == 0)
                    return Rank.None;

                if (Players.Find(p => p.TeamId != localPlayer.TeamId) != null)
                    return Rank.None;

                if (Players.Find(p => p.TeamId == 0) != null)
                    return Rank.None;

                if (AIPlayers.Find(p => p.TeamId == 0) != null)
                    return Rank.None;

                teamMemberCounts[localPlayer.TeamId] += Players.Count;

                if (lowestEnemyAILevel < highestAllyAILevel)
                {
                    // Check that the player's AI allies aren't stronger
                    return Rank.None;
                }

                // Check that all teams have at least as many players
                // as the human players' team
                int allyCount = teamMemberCounts[localPlayer.TeamId];

                for (int i = 1; i < 5; i++)
                {
                    if (i == localPlayer.TeamId)
                        continue;

                    if (teamMemberCounts[i] > 0)
                    {
                        if (teamMemberCounts[i] < allyCount)
                            return Rank.None;
                    }
                }

                return lowestEnemyAILevel + 1;
            }

            // *********
            // Skirmish!
            // *********

            if (AIPlayers.Count != Map.MaxPlayers - 1)
                return Rank.None;

            teamMemberCounts[localPlayer.TeamId]++;

            if (lowestEnemyAILevel < highestAllyAILevel)
            {
                // Check that the player's AI allies aren't stronger
                return Rank.None;
            }

            if (localPlayer.TeamId > 0)
            {
                // Check that all teams have at least as many players
                // as the local player's team
                int allyCount = teamMemberCounts[localPlayer.TeamId];

                for (int i = 1; i < 5; i++)
                {
                    if (i == localPlayer.TeamId)
                        continue;

                    if (teamMemberCounts[i] > 0)
                    {
                        if (teamMemberCounts[i] < allyCount)
                            return Rank.None;
                    }
                }

                // Check that there is a team other than the players' team that is at least as large
                bool pass = false;
                for (int i = 1; i < 5; i++)
                {
                    if (i == localPlayer.TeamId)
                        continue;

                    if (teamMemberCounts[i] >= allyCount)
                    {
                        pass = true;
                        break;
                    }
                }

                if (!pass)
                    return Rank.None;
            }

            return lowestEnemyAILevel + 1;
        }

        protected string AddGameOptionPreset(string name)
        {
            string error = GameOptionPreset.IsNameValid(name);
            if (!string.IsNullOrEmpty(error))
                return error;

            GameOptionPreset preset = new GameOptionPreset(name);
            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                preset.AddCheckBoxValue(checkBox.Name, checkBox.Checked);
            }

            foreach (GameLobbyDropDown dropDown in DropDowns)
            {
                preset.AddDropDownValue(dropDown.Name, dropDown.SelectedIndex);
            }

            GameOptionPresets.Instance.AddPreset(preset);
            return null;
        }

        public bool LoadGameOptionPreset(string name)
        {
            GameOptionPreset preset = GameOptionPresets.Instance.GetPreset(name);
            if (preset == null)
                return false;

            disableGameOptionUpdateBroadcast = true;

            var checkBoxValues = preset.GetCheckBoxValues();
            foreach (var kvp in checkBoxValues)
            {
                GameLobbyCheckBox checkBox = CheckBoxes.Find(c => c.Name == kvp.Key);
                if (checkBox != null && checkBox.AllowChanges && checkBox.AllowChecking)
                {
                    checkBox.Checked = kvp.Value;
                    checkBox.HostChecked = kvp.Value;
                }
            }

            var dropDownValues = preset.GetDropDownValues();
            foreach (var kvp in dropDownValues)
            {
                GameLobbyDropDown dropDown = DropDowns.Find(d => d.Name == kvp.Key);
                if (dropDown != null && dropDown.AllowDropDown)
                {
                    dropDown.SelectedIndex = kvp.Value;
                    dropDown.HostSelectedIndex = kvp.Value;
                }
            }

            disableGameOptionUpdateBroadcast = false;
            OnGameOptionChanged();
            return true;
        }

        /// <summary>
        /// Checks if launch game button can stay enabled or not and updates the state accordingly.
        /// </summary>
        /// <returns>True if launch game button is enabled, false if not.</returns>
        protected virtual bool UpdateLaunchGameButtonStatus()
        {
            return true;
        }

        protected abstract bool AllowPlayerOptionsChange();
    }
}
