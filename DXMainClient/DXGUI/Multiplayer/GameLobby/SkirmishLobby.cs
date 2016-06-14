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

        protected override void ChangeMap(GameMode gameMode, Map map)
        {
            base.ChangeMap(gameMode, map);

            CheckGameValidity();
        }

        protected override void CopyPlayerDataToUI()
        {
            base.CopyPlayerDataToUI();

            CheckGameValidity();
        }

        private void CheckGameValidity()
        {
            int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
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
            StartGame();
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            this.Enabled = false;
            this.Visible = false;
        }

        protected override bool AllowPlayerOptionsChange()
        {
            return true;
        }
    }
}
