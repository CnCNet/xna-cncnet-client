using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.XNAUI;
using ClientCore;
using ClientCore.Statistics;
using DTAClient.DXGUI.Generic;
using DTAClient.Domain.Multiplayer;
using ClientGUI;
using Rampastring.Tools;
using System.IO;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class SkirmishLobby : GameLobbyBase, ISwitchable
    {
        private const string SETTINGS_PATH = "Client\\SkirmishSettings.ini";

        public SkirmishLobby(WindowManager windowManager, TopBar topBar, List<GameMode> GameModes)
            : base(windowManager, "SkirmishLobby", GameModes, false)
        {
            this.topBar = topBar;
        }

        public event EventHandler Exited;

        TopBar topBar;

        public override void Initialize()
        {
            base.Initialize();

            RandomSeed = new Random().Next();

            //InitPlayerOptionDropdowns(128, 98, 90, 48, 55, new Point(6, 24));
            InitPlayerOptionDropdowns();

            btnLeaveGame.Text = "Main Menu";

            MapPreviewBox.EnableContextMenu = true;

            ddPlayerSides[0].AddItem("Spectator", AssetLoader.LoadTexture("spectatoricon.png"));

            MapPreviewBox.LocalStartingLocationSelected += MapPreviewBox_LocalStartingLocationSelected;
            MapPreviewBox.StartingLocationApplied += MapPreviewBox_StartingLocationApplied;

            InitializeWindow();

            WindowManager.CenterControlOnScreen(this);

            LoadSettings();

            CopyPlayerDataToUI();
        }

        private void MapPreviewBox_StartingLocationApplied(object sender, EventArgs e)
        {
            CopyPlayerDataToUI();
        }

        public void RefreshPlayerName()
        {
            Players[0].Name = ProgramConstants.PLAYERNAME;
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
                return GameMode.UIName + " can only be played on CnCNet and LAN.";
            }

            if (Map.MultiplayerOnly)
            {
                return "The selected map can only be played on CnCNet and LAN.";
            }

            if (totalPlayerCount < Map.MinPlayers)
            {
                return "The selected map cannot be played with less than " + Map.MinPlayers + " players.";
            }

            if (Map.EnforceMaxPlayers)
            {
                if (totalPlayerCount > Map.MaxPlayers)
                {
                    return "The selected map cannot be played with more than " + Map.MaxPlayers + " players.";
                }

                IEnumerable<PlayerInfo> concatList = Players.Concat(AIPlayers);

                foreach (PlayerInfo pInfo in concatList)
                {
                    if (pInfo.StartingLocation == 0)
                        continue;

                    if (concatList.Count(p => p.StartingLocation == pInfo.StartingLocation) > 1)
                    {
                        return "Multiple players cannot share the same starting location on the selected map.";
                    }
                }
            }

            if (Map.IsCoop && Players[0].SideId == ddPlayerSides[0].Items.Count - 1)
            {
                return "Co-op missions cannot be spectated. You'll have to show a bit more effort to cheat here.";
            }

            return null;
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

            XNAMessageBox.Show(WindowManager, "Cannot launch game", error);
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            this.Enabled = false;
            this.Visible = false;

            Exited?.Invoke(this, EventArgs.Empty);

            topBar.RemovePrimarySwitchable(this);
        }

        protected override bool AllowPlayerOptionsChange()
        {
            return true;
        }

        protected override int GetDefaultMapRankIndex(Map map)
        {
            return StatisticsManager.Instance.GetSkirmishRankForDefaultMap(map.Name, map.MaxPlayers);
        }

        protected override void GameProcessExited()
        {
            base.GameProcessExited();

            DdGameMode_SelectedIndexChanged(null, EventArgs.Empty); // Refresh ranks

            RandomSeed = new Random().Next();
        }

        public void Open()
        {
            topBar.AddPrimarySwitchable(this);
            SwitchOn();
        }

        public void SwitchOn()
        {
            Enabled = true;
            Visible = true;
        }

        public void SwitchOff()
        {
            Enabled = false;
            Visible = false;
        }

        public string GetSwitchName()
        {
            return "Skirmish Lobby";
        }

        private void SaveSettings()
        {
            try
            {
                // Delete the file so we don't keep potential extra AI players that already exist in the file
                File.Delete(ProgramConstants.GamePath + SETTINGS_PATH);

                var skirmishSettingsIni = new IniFile(ProgramConstants.GamePath + SETTINGS_PATH);

                skirmishSettingsIni.SetStringValue("Player", "Info", Players[0].ToString());

                for (int i = 0; i < AIPlayers.Count; i++)
                {
                    skirmishSettingsIni.SetStringValue("AIPlayers", i.ToString(), AIPlayers[i].ToString());
                }

                skirmishSettingsIni.SetStringValue("Settings", "Map", Map.SHA1);
                skirmishSettingsIni.SetStringValue("Settings", "GameMode", GameMode.Name);

                skirmishSettingsIni.WriteIniFile();
            }
            catch (Exception ex)
            {
                Logger.Log("Saving skirmish settings failed! Reason: " + ex.Message);
            }
        }

        private void LoadSettings()
        {
            if (!File.Exists(ProgramConstants.GamePath + SETTINGS_PATH))
            {
                InitDefaultSettings();
                return;
            }

            var skirmishSettingsIni = new IniFile(ProgramConstants.GamePath + SETTINGS_PATH);

            var player = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("Player", "Info", string.Empty));

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

            if (keys == null)
            {
                Logger.Log("AI player information doesn't exist in skirmish settings!");
                InitDefaultSettings();
                return;
            }

            foreach (string key in keys)
            {
                var aiPlayer = PlayerInfo.FromString(skirmishSettingsIni.GetStringValue("AIPlayers", key, string.Empty));

                CheckLoadedPlayerVariableBounds(aiPlayer);

                if (aiPlayer == null)
                {
                    Logger.Log("Failed to load AI player information from skirmish settings!");
                    InitDefaultSettings();
                    return;
                }

                if (AIPlayers.Count < MAX_PLAYER_COUNT - 1)
                    AIPlayers.Add(aiPlayer);
            }

            string gameModeName = skirmishSettingsIni.GetStringValue("Settings", "GameMode", string.Empty);

            int gameModeIndex = GameModes.FindIndex(g => g.Name == gameModeName);

            if (gameModeIndex > -1)
            {
                GameMode gm = GameModes[gameModeIndex];

                string mapSHA1 = skirmishSettingsIni.GetStringValue("Settings", "Map", string.Empty);

                int mapIndex = gm.Maps.FindIndex(m => m.SHA1 == mapSHA1);

                if (mapIndex > -1)
                {
                    ddGameMode.SelectedIndex = gameModeIndex;
                    lbMapList.SelectedIndex = mapIndex;

                    while (mapIndex > lbMapList.LastIndex)
                        lbMapList.TopIndex++;

                    return;
                }
            }

            LoadDefaultMap();
        }

        /// <summary>
        /// Checks that a player's color, team and starting location
        /// don't exceed allowed bounds.
        /// </summary>
        /// <param name="pInfo">The PlayerInfo.</param>
        private void CheckLoadedPlayerVariableBounds(PlayerInfo pInfo)
        {
            if (pInfo.ColorId < 0 || pInfo.ColorId > MPColors.Count)
            {
                pInfo.ColorId = 0;
            }

            if (pInfo.TeamId < 0 || pInfo.TeamId >= ddPlayerTeams[0].Items.Count)
            {
                pInfo.TeamId = 0;
            }

            if (pInfo.StartingLocation < 0 || pInfo.StartingLocation > MAX_PLAYER_COUNT)
            {
                pInfo.StartingLocation = 0;
            }
        }

        private void InitDefaultSettings()
        {
            Players.Clear();
            AIPlayers.Clear();

            Players.Add(new PlayerInfo(ProgramConstants.PLAYERNAME, 0, 0, 0, 0));
            PlayerInfo aiPlayer = new PlayerInfo("Easy AI", 0, 0, 0, 0);
            aiPlayer.IsAI = true;
            aiPlayer.AILevel = 2;
            AIPlayers.Add(aiPlayer);

            LoadDefaultMap();
        }
    }
}
