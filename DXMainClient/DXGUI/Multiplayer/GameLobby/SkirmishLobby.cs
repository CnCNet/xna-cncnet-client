using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTAClient.domain.CnCNet;
using Rampastring.XNAUI;
using Rampastring.XNAUI.DXControls;
using Microsoft.Xna.Framework;
using ClientCore;
using Microsoft.Xna.Framework.Graphics;
using ClientCore.Statistics;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class SkirmishLobby : GameLobbyBase
    {
        public SkirmishLobby(WindowManager windowManager, List<GameMode> GameModes) : base(windowManager, "SkirmishLobby", GameModes)
        {
        }

        GameInProgressWindow gameInProgressWindow;

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

            gameInProgressWindow = new GameInProgressWindow(WindowManager);
            AddChild(gameInProgressWindow);
            gameInProgressWindow.CenterOnParent();
            gameInProgressWindow.Enabled = false;
            gameInProgressWindow.Visible = false;
            gameInProgressWindow.Focused = true;

            InitializeWindow();

            foreach (GameMode gm in GameModes)
                ddGameMode.AddItem(gm.UIName);

            Players.Add(new PlayerInfo(ProgramConstants.PLAYERNAME, 0, 0, 0, 0));
            PlayerInfo aiPlayer = new PlayerInfo("Easy AI", 0, 0, 0, 0);
            aiPlayer.IsAI = true;
            aiPlayer.AILevel = 2;
            AIPlayers.Add(aiPlayer);

            WindowManager.CenterControlOnScreen(this);

            if (ddGameMode.Items.Count > 0)
            {
                ddGameMode.SelectedIndex = 0;

                lbMapList.SelectedIndex = 0;
            }
        }

        private void MapPreviewBox_LocalStartingLocationSelected(object sender, LocalStartingLocationEventArgs e)
        {
            Players[0].StartingLocation = e.StartingLocationIndex + 1;
            CopyPlayerDataToUI();
        }

        protected override void LbMapList_SelectedIndexChanged(object sender, EventArgs e)
        {
            base.LbMapList_SelectedIndexChanged(sender, e);

            CheckGameValidity();
        }

        protected override void CopyPlayerDataToUI()
        {
            base.CopyPlayerDataToUI();

            CheckGameValidity();
        }

        private void CheckGameValidity()
        {
            int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count)
                + AIPlayers.Count;

            if (totalPlayerCount < Map.MinPlayers ||
                (Map.EnforceMaxPlayers && (totalPlayerCount > Map.MaxPlayers)))
            {
                btnLaunchGame.AllowClick = false;
                return;
            }

            if (Map.IsCoop && Players[0].SideId == ddPlayerSides[0].Items.Count - 1)
            {
                btnLaunchGame.AllowClick = false;
                return;
            }

            btnLaunchGame.AllowClick = true;
            return;
        }

        protected override void BtnLaunchGame_LeftClick(object sender, EventArgs e)
        {
            gameInProgressWindow.Visible = true;

            StartGame();
        }

        protected override void GameProcessExited()
        {
            gameInProgressWindow.Visible = false;

            RandomSeed = new Random().Next();

            base.GameProcessExited();

            DdGameMode_SelectedIndexChanged(null, EventArgs.Empty); // Refresh ranks
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            this.Enabled = false;
            this.Visible = false;
        }

        protected override bool AllowPlayerDropdown()
        {
            return false;
        }
    }
}
