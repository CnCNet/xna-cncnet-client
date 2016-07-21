using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.XNAUI;
using ClientCore;
using ClientCore.Statistics;
using DTAClient.DXGUI.Generic;
using DTAClient.domain.Multiplayer;
using ClientGUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class SkirmishLobby : GameLobbyBase, ISwitchable
    {
        public SkirmishLobby(WindowManager windowManager, TopBar topBar, List<GameMode> GameModes) : base(windowManager, "SkirmishLobby", GameModes)
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

            InitializeWindow();

            // To move the lblMapAuthor label into its correct position
            // if it was moved in the theme description INI file
            LoadDefaultMap();

            Players.Add(new PlayerInfo(ProgramConstants.PLAYERNAME, 0, 0, 0, 0));
            PlayerInfo aiPlayer = new PlayerInfo("Easy AI", 0, 0, 0, 0);
            aiPlayer.IsAI = true;
            aiPlayer.AILevel = 2;
            AIPlayers.Add(aiPlayer);

            CopyPlayerDataToUI();

            WindowManager.CenterControlOnScreen(this);
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

            if (totalPlayerCount < Map.MinPlayers)
            {
                return "The selected map cannot be played with less than " + Map.MinPlayers + " players.";
            }

            if (Map.MultiplayerOnly)
            {
                return "The selected map can only be played on CnCNet and LAN.";
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
    }
}
