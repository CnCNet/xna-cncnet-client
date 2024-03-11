using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.Statistics;

using ClientGUI;

using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Generic;

using Microsoft.Xna.Framework;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class SkirmishLobby : GameLobbyBase, ISwitchable
{
    private const string SETTINGS_PATH = "Client/SkirmishSettings.ini";

    public SkirmishLobby(WindowManager windowManager, TopBar topBar, MapLoader mapLoader, DiscordHandler discordHandler)
        : base(windowManager, "SkirmishLobby", mapLoader, false, discordHandler)
    {
        this.topBar = topBar;
    }

    public event EventHandler Exited;

    private readonly TopBar topBar;

    public override void Initialize()
    {
        base.Initialize();

        RandomSeed = new Random().Next();

        //InitPlayerOptionDropdowns(128, 98, 90, 48, 55, new Point(6, 24));
        InitPlayerOptionDropdowns();

        btnLeaveGame.Text = "Main Menu".L10N("Client:Main:MainMenu");

        //MapPreviewBox.EnableContextMenu = true;

        const string spectatorName = "Spectator";
        AddSideToDropDown(ddPlayerSides[0], spectatorName, spectatorName.L10N("Client:Sides:SpectatorSide"), AssetLoader.LoadTexture("spectatoricon.png"));

        MapPreviewBox.LocalStartingLocationSelected += MapPreviewBox_LocalStartingLocationSelected;
        MapPreviewBox.StartingLocationApplied += MapPreviewBox_StartingLocationApplied;

        WindowManager.CenterControlOnScreen(this);

        LoadSettings();

        CheckDisallowedSides();

        CopyPlayerDataToUI();

        ProgramConstants.PlayerNameChanged += ProgramConstants_PlayerNameChanged;
        ddPlayerSides[0].SelectedIndexChanged += PlayerSideChanged;

        PlayerExtraOptionsPanel?.SetIsHost(true);
    }

    protected override void ToggleFavoriteMap()
    {
        base.ToggleFavoriteMap();

        if (GameModeMap.IsFavorite)
        {
            return;
        }

        RefreshForFavoriteMapRemoved();
    }

    protected override void AddNotice(string message, Color color)
    {
        XNAMessageBox.Show(WindowManager, "Message".L10N("Client:Main:MessageTitle"), message);
    }

    protected override void OnEnabledChanged(object sender, EventArgs args)
    {
        base.OnEnabledChanged(sender, args);
        if (Enabled)
        {
            UpdateDiscordPresence(true);
        }
        else
        {
            ResetDiscordPresence();
        }
    }

    private void ProgramConstants_PlayerNameChanged(object sender, EventArgs e)
    {
        Players[0].Name = ProgramConstants.PLAYERNAME;
        CopyPlayerDataToUI();
    }

    private void MapPreviewBox_StartingLocationApplied(object sender, EventArgs e)
    {
        CopyPlayerDataToUI();
    }

    private void MapPreviewBox_LocalStartingLocationSelected(object sender, LocalStartingLocationEventArgs e)
    {
        Players[0].StartingLocation = e.StartingLocationIndex + 1;
        CopyPlayerDataToUI();
    }

    private string CheckGameValidity()
    {
        int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
            + AIPlayers.Count;

        if (GameMode.MultiplayerOnly)
        {
            return string.Format("{0} can only be played on CnCNet and LAN.".L10N("Client:Main:GameModeMultiplayerOnly"),
                GameMode.UIName);
        }

        if (GameMode.MinPlayersOverride > -1 && totalPlayerCount < GameMode.MinPlayersOverride)
        {
            return string.Format("{0} cannot be played with less than {1} players.".L10N("Client:Main:GameModeInsufficientPlayers"),
                     GameMode.UIName, GameMode.MinPlayersOverride);
        }

        if (Map.MultiplayerOnly)
        {
            return "The selected map can only be played on CnCNet and LAN.".L10N("Client:Main:MapMultiplayerOnly");
        }

        if (totalPlayerCount < Map.MinPlayers)
        {
            return string.Format("The selected map cannot be played with less than {0} players.".L10N("Client:Main:MapInsufficientPlayers"),
                Map.MinPlayers);
        }

        if (Map.EnforceMaxPlayers)
        {
            if (totalPlayerCount > Map.MaxPlayers)
            {
                return string.Format("The selected map cannot be played with more than {0} players.".L10N("Client:Main:MapTooManyPlayers"),
                    Map.MaxPlayers);
            }

            IEnumerable<PlayerInfo> concatList = Players.Concat(AIPlayers);

            foreach (PlayerInfo pInfo in concatList)
            {
                if (pInfo.StartingLocation == 0)
                {
                    continue;
                }

                if (concatList.Count(p => p.StartingLocation == pInfo.StartingLocation) > 1)
                {
                    return "Multiple players cannot share the same starting location on the selected map.".L10N("Client:Main:StartLocationOccupied");
                }
            }
        }

        if (Map.IsCoop && Players[0].SideId == ddPlayerSides[0].Items.Count - 1)
        {
            return "Co-op missions cannot be spectated. You'll have to show a bit more effort to cheat here.".L10N("Client:Main:CoOpMissionSpectatorPrompt");
        }

        string teamMappingsError = GetTeamMappingsError();
        return !string.IsNullOrEmpty(teamMappingsError) ? teamMappingsError : null;
    }

    protected override void BtnLaunchGame_LeftClick(object sender, EventArgs e)
    {
        string error = CheckGameValidity();

        if (error == null)
        {
            SaveSettings();
            StartGame();
            return;
        }

        XNAMessageBox.Show(WindowManager, "Cannot launch game".L10N("Client:Main:LaunchGameErrorTitle"), error);
    }

    protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
    {
        Enabled = false;
        Visible = false;

        Exited?.Invoke(this, EventArgs.Empty);

        topBar.RemovePrimarySwitchable(this);
        ResetDiscordPresence();
    }

    private void PlayerSideChanged(object sender, EventArgs e)
    {
        UpdateDiscordPresence();
    }

    protected override void UpdateDiscordPresence(bool resetTimer = false)
    {
        if (discordHandler == null || Map == null || GameMode == null || !Initialized)
        {
            return;
        }

        int playerIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);
        if (playerIndex is >= MAX_PLAYER_COUNT or < 0)
        {
            return;
        }

        XNAClientDropDown sideDropDown = ddPlayerSides[playerIndex];
        if (sideDropDown.SelectedItem == null)
        {
            return;
        }

        string side = (string)sideDropDown.SelectedItem.Tag;
        string currentState = ProgramConstants.IsInGame ? "In Game" : "Setting Up";

        discordHandler.UpdatePresence(
            Map.UntranslatedName, GameMode.UntranslatedUIName, currentState, side, resetTimer);
    }

    protected override bool AllowPlayerOptionsChange()
    {
        return true;
    }

    protected override int GetDefaultMapRankIndex(GameModeMap gameModeMap)
    {
        return StatisticsManager.Instance.GetSkirmishRankForDefaultMap(gameModeMap.Map.UntranslatedName, gameModeMap.Map.MaxPlayers);
    }

    protected override void GameProcessExited()
    {
        base.GameProcessExited();

        DdGameModeMapFilter_SelectedIndexChanged(null, EventArgs.Empty); // Refresh ranks

        RandomSeed = new Random().Next();
    }

    public void Open()
    {
        topBar.AddPrimarySwitchable(this);
        Enable();
    }

    public void SwitchOn()
    {
        Enable();
    }

    public void SwitchOff()
    {
        Disable();
    }

    public string GetSwitchName()
    {
        return "Skirmish Lobby".L10N("Client:Main:SkirmishLobby");
    }

    /// <summary>
    /// Saves skirmish settings to an INI file on the file system.
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            FileInfo settingsFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH);

            // Delete the file so we don't keep potential extra AI players that already exist in the file
            settingsFileInfo.Delete();

            IniFile skirmishSettingsIni = new(settingsFileInfo.FullName);

            skirmishSettingsIni.SetStringValue("Player", "Info", Players[0].ToString());

            for (int i = 0; i < AIPlayers.Count; i++)
            {
                skirmishSettingsIni.SetStringValue("AIPlayers", i.ToString(), AIPlayers[i].ToString());
            }

            skirmishSettingsIni.SetStringValue("Settings", "Map", Map.SHA1);
            skirmishSettingsIni.SetStringValue("Settings", "GameModeMapFilter", ddGameModeMapFilter.SelectedItem?.Text);

            if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
            {
                foreach (GameLobbyDropDown dd in DropDowns)
                {
                    skirmishSettingsIni.SetStringValue("GameOptions", dd.Name, dd.UserSelectedIndex + "");
                }

                foreach (GameLobbyCheckBox cb in CheckBoxes)
                {
                    skirmishSettingsIni.SetStringValue("GameOptions", cb.Name, cb.Checked.ToString());
                }
            }

            skirmishSettingsIni.WriteIniFile();
        }
        catch (Exception ex)
        {
            Logger.Log("Saving skirmish settings failed! Reason: " + ex.Message);
        }
    }

    /// <summary>
    /// Loads skirmish settings from an INI file on the file system.
    /// </summary>
    private void LoadSettings()
    {
        if (!SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH).Exists)
        {
            InitDefaultSettings();
            return;
        }

        IniFile skirmishSettingsIni = new(SafePath.CombineFilePath(ProgramConstants.GamePath, SETTINGS_PATH));

        string gameModeMapFilterName = skirmishSettingsIni.GetStringValue("Settings", "GameModeMapFilter", string.Empty);
        if (string.IsNullOrEmpty(gameModeMapFilterName))
        {
            gameModeMapFilterName = skirmishSettingsIni.GetStringValue("Settings", "GameMode", string.Empty); // legacy
        }

        if (ddGameModeMapFilter.Items.Find(i => i.Text == gameModeMapFilterName)?.Tag is not GameModeMapFilter gameModeMapFilter || !gameModeMapFilter.Any())
        {
            gameModeMapFilter = GetDefaultGameModeMapFilter();
        }

        GameModeMap gameModeMap = gameModeMapFilter.GetGameModeMaps().First();

        if (gameModeMap != null)
        {
            GameModeMap = gameModeMap;

            ddGameModeMapFilter.SelectedIndex = ddGameModeMapFilter.Items.FindIndex(i => i.Tag == gameModeMapFilter);

            string mapSHA1 = skirmishSettingsIni.GetStringValue("Settings", "Map", string.Empty);

            int gameModeMapIndex = gameModeMapFilter.GetGameModeMaps().FindIndex(gmm => gmm.Map.SHA1 == mapSHA1);

            if (gameModeMapIndex > -1)
            {
                lbGameModeMapList.SelectedIndex = gameModeMapIndex;

                while (gameModeMapIndex > lbGameModeMapList.LastIndex)
                {
                    lbGameModeMapList.TopIndex++;
                }
            }
        }
        else
        {
            LoadDefaultGameModeMap();
        }

        PlayerInfo player = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("Player", "Info", string.Empty));

        if (player == null)
        {
            Logger.Log("Failed to load human player information from skirmish settings!");
            InitDefaultSettings();
            return;
        }

        CheckLoadedPlayerVariableBounds(player);

        player.Name = ProgramConstants.PLAYERNAME;
        Players.Add(player);

        List<string> keys = skirmishSettingsIni.GetSectionKeys("AIPlayers");

        keys ??= []; // No point skip parsing all settings if only AI info is missing.

        bool AIAllowed = !(Map.MultiplayerOnly || GameMode.MultiplayerOnly) || !(Map.HumanPlayersOnly || GameMode.HumanPlayersOnly);
        foreach (string key in keys)
        {
            if (!AIAllowed)
            {
                break;
            }

            PlayerInfo aiPlayer = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("AIPlayers", key, string.Empty));

            CheckLoadedPlayerVariableBounds(aiPlayer, true);

            if (aiPlayer == null)
            {
                Logger.Log("Failed to load AI player information from skirmish settings!");
                InitDefaultSettings();
                return;
            }

            if (AIPlayers.Count < MAX_PLAYER_COUNT - 1)
            {
                AIPlayers.Add(aiPlayer);
            }
        }

        if (ClientConfiguration.Instance.SaveSkirmishGameOptions)
        {
            foreach (GameLobbyDropDown dd in DropDowns)
            {
                // Maybe we should build an union of the game mode and map
                // forced options, we'd have less repetitive code that way

                if (GameMode != null)
                {
                    int gameModeMatchIndex = GameMode.ForcedDropDownValues.FindIndex(p => p.Key.Equals(dd.Name));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Dropdown '" + dd.Name + "' has forced value in gamemode - saved settings ignored.");
                        continue;
                    }
                }

                if (Map != null)
                {
                    int gameModeMatchIndex = Map.ForcedDropDownValues.FindIndex(p => p.Key.Equals(dd.Name));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Dropdown '" + dd.Name + "' has forced value in map - saved settings ignored.");
                        continue;
                    }
                }

                dd.UserSelectedIndex = skirmishSettingsIni.GetIntValue("GameOptions", dd.Name, dd.UserSelectedIndex);

                if (dd.UserSelectedIndex > -1 && dd.UserSelectedIndex < dd.Items.Count)
                {
                    dd.SelectedIndex = dd.UserSelectedIndex;
                }
            }

            foreach (GameLobbyCheckBox cb in CheckBoxes)
            {
                if (GameMode != null)
                {
                    int gameModeMatchIndex = GameMode.ForcedCheckBoxValues.FindIndex(p => p.Key.Equals(cb.Name));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Checkbox '" + cb.Name + "' has forced value in gamemode - saved settings ignored.");
                        continue;
                    }
                }

                if (Map != null)
                {
                    int gameModeMatchIndex = Map.ForcedCheckBoxValues.FindIndex(p => p.Key.Equals(cb.Name));
                    if (gameModeMatchIndex > -1)
                    {
                        Logger.Log("Checkbox '" + cb.Name + "' has forced value in map - saved settings ignored.");
                        continue;
                    }
                }

                cb.Checked = skirmishSettingsIni.GetBooleanValue("GameOptions", cb.Name, cb.Checked);
            }
        }
    }

    /// <summary>
    /// Checks that a player's color, team and starting location
    /// don't exceed allowed bounds.
    /// </summary>
    /// <param name="pInfo">The PlayerInfo.</param>
    private void CheckLoadedPlayerVariableBounds(PlayerInfo pInfo, bool isAIPlayer = false)
    {
        int sideCount = SideCount + RandomSelectorCount;
        if (isAIPlayer)
        {
            sideCount--;
        }

        if (pInfo.SideId < 0 || pInfo.SideId > sideCount)
        {
            pInfo.SideId = 0;
        }

        if (pInfo.ColorId < 0 || pInfo.ColorId > MPColors.Count)
        {
            pInfo.ColorId = 0;
        }

        if (pInfo.TeamId < 0 || pInfo.TeamId >= ddPlayerTeams[0].Items.Count ||
            (!Map.IsCoop && (Map.ForceNoTeams || GameMode.ForceNoTeams)))
        {
            pInfo.TeamId = 0;
        }

        if (pInfo.StartingLocation < 0 || pInfo.StartingLocation > MAX_PLAYER_COUNT ||
            (!Map.IsCoop && (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations)))
        {
            pInfo.StartingLocation = 0;
        }
    }

    private void InitDefaultSettings()
    {
        Players.Clear();
        AIPlayers.Clear();

        Players.Add(new PlayerInfo(ProgramConstants.PLAYERNAME, 0, 0, 0, 0));
        PlayerInfo aiPlayer = new(ProgramConstants.AI_PLAYER_NAMES[0], 0, 0, 0, 0)
        {
            IsAI = true,
            AILevel = 0
        };
        AIPlayers.Add(aiPlayer);

        LoadDefaultGameModeMap();
    }

    protected override void UpdateMapPreviewBoxEnabledStatus()
    {
        MapPreviewBox.EnableContextMenu = !((Map != null && Map.ForceRandomStartLocations) || (GameMode != null && GameMode.ForceRandomStartLocations) || GetPlayerExtraOptions().IsForceRandomStarts);
        MapPreviewBox.EnableStartLocationSelection = MapPreviewBox.EnableContextMenu;
    }

    protected override bool UpdateLaunchGameButtonStatus()
    {
        btnLaunchGame.Enabled = base.UpdateLaunchGameButtonStatus() && GameMode != null && Map != null;
        return btnLaunchGame.Enabled;
    }
}