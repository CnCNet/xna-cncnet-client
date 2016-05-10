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

namespace DTAClient.DXGUI.GameLobby
{
    public class SkirmishLobby : GameLobbyBase
    {
        public SkirmishLobby(WindowManager windowManager, List<GameMode> GameModes) : base(windowManager, "SkirmishLobby", GameModes)
        {
        }

        DXMultiColumnListBox lbMapList;
        DXDropDown ddGameMode;
        DXLabel lblGameModeSelect;
        GameInProgressWindow gameInProgressWindow;

        Texture2D[] rankTextures;

        public override void Initialize()
        {
            base.Initialize();

            RandomSeed = new Random().Next();

            rankTextures = new Texture2D[4]
            {
                AssetLoader.LoadTexture("rankNone.png"),
                AssetLoader.LoadTexture("rankEasy.png"),
                AssetLoader.LoadTexture("rankNormal.png"),
                AssetLoader.LoadTexture("rankHard.png")
            };

            btnLeaveGame.Text = "Main Menu";

            InitPlayerOptionDropdowns(128, 102, 90, 76, 65, new Point(13, 24));

            lbMapList = new DXMultiColumnListBox(WindowManager);
            lbMapList.Name = "lbMapList";
            lbMapList.ClientRectangle = new Rectangle(btnLaunchGame.ClientRectangle.X, GameOptionsPanel.ClientRectangle.Y + 23, 
                MapPreviewBox.ClientRectangle.X - btnLaunchGame.ClientRectangle.X - 6, 
                MapPreviewBox.ClientRectangle.Bottom - 23 - GameOptionsPanel.ClientRectangle.Y);
            lbMapList.SelectedIndexChanged += LbMapList_SelectedIndexChanged;
            lbMapList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbMapList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 192), 1, 1);
            lbMapList.LineHeight = 16;
            lbMapList.DrawListBoxBorders = true;

            DXPanel rankHeader = new DXPanel(WindowManager);
            rankHeader.BackgroundTexture = AssetLoader.LoadTexture("rank.png");
            rankHeader.ClientRectangle = new Rectangle(0, 0, rankHeader.BackgroundTexture.Width,
                19);

            DXListBox rankListBox = new DXListBox(WindowManager);
            rankListBox.TextBorderDistance = 2;

            lbMapList.AddColumn(rankHeader, rankListBox);
            lbMapList.AddColumn("MAP NAME", lbMapList.ClientRectangle.Width - 67 - rankHeader.ClientRectangle.Width + 1);
            lbMapList.AddColumn("PLAYERS", 67);

            ddGameMode = new DXDropDown(WindowManager);
            ddGameMode.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.Right - 150, GameOptionsPanel.ClientRectangle.Y, 150, 21);
            ddGameMode.ClickSoundEffect = AssetLoader.LoadSound("dropdown.wav");
            ddGameMode.SelectedIndexChanged += DdGameMode_SelectedIndexChanged;

            lblGameModeSelect = new DXLabel(WindowManager);
            lblGameModeSelect.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.X, ddGameMode.ClientRectangle.Top + 2, 0, 0);
            lblGameModeSelect.FontIndex = 1;
            lblGameModeSelect.Text = "GAME MODE:";

            MapPreviewBox.StartingLocationSelected += MapPreviewBox_StartingLocationSelected;

            AddChild(lbMapList);
            AddChild(ddGameMode);
            AddChild(lblGameModeSelect);

            gameInProgressWindow = new GameInProgressWindow(WindowManager);
            AddChild(gameInProgressWindow);
            gameInProgressWindow.CenterOnParent();
            gameInProgressWindow.Enabled = false;
            gameInProgressWindow.Visible = false;
            gameInProgressWindow.Focused = true;

            foreach (GameMode gm in GameModes)
                ddGameMode.AddItem(gm.UIName);

            Players.Add(new PlayerInfo(ProgramConstants.PLAYERNAME, 0, 0, 0, 0));

            if (ddGameMode.Items.Count > 0)
            {
                ddGameMode.SelectedIndex = 0;

                lbMapList.SelectedIndex = 0;
            }
        }

        private void MapPreviewBox_StartingLocationSelected(object sender, StartingLocationEventArgs e)
        {
            Players[0].StartingLocation = e.StartingLocationIndex + 1;
            CopyPlayerDataToUI();
        }

        private void DdGameMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            GameMode = GameModes[ddGameMode.SelectedIndex];

            lbMapList.ClearItems();

            foreach (Map map in GameMode.Maps)
            {
                DXListBoxItem rankItem = new DXListBoxItem();
                if (map.IsCoop)
                {
                    if (StatisticsManager.Instance.HasBeatCoOpMap(map.Name, GameMode.UIName))
                        rankItem.Texture = rankTextures[Math.Abs(2 - GameMode.CoopDifficultyLevel) + 1];
                    else
                        rankItem.Texture = rankTextures[0];
                }
                else
                    rankItem.Texture = rankTextures[StatisticsManager.Instance.GetSkirmishRankForDefaultMap(map.Name, map.MaxPlayers) + 1];

                DXListBoxItem mapNameItem = new DXListBoxItem();
                mapNameItem.Text = map.Name;
                mapNameItem.TextColor = UISettings.AltColor;

                DXListBoxItem playerCountItem = new DXListBoxItem();
                playerCountItem.TextColor = UISettings.AltColor;
                playerCountItem.Text = map.MaxPlayers.ToString();

                DXListBoxItem[] mapInfoArray = new DXListBoxItem[]
                {
                    rankItem,
                    mapNameItem,
                    playerCountItem
                };

                lbMapList.AddItem(mapInfoArray);
            }
        }

        private void LbMapList_SelectedIndexChanged(object sender, EventArgs e)
        {
            GameMode gm = GameModes[ddGameMode.SelectedIndex];
            Map map = gm.Maps[lbMapList.SelectedIndex];

            ChangeMap(gm, map);
            CheckPlayerCountRequirement();
        }

        protected override void CopyPlayerDataToUI()
        {
            base.CopyPlayerDataToUI();

            CheckPlayerCountRequirement();
        }

        private void CheckPlayerCountRequirement()
        {
            btnLaunchGame.Enabled = Players.Count + AIPlayers.Count > Map.MinPlayers &&
                (!Map.EnforceMaxPlayers || (Players.Count + AIPlayers.Count <= Map.MaxPlayers));
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
    }
}
