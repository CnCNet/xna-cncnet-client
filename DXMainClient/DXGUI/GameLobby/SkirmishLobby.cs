using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTAClient.domain.CnCNet;
using Rampastring.XNAUI;
using Rampastring.XNAUI.DXControls;
using Microsoft.Xna.Framework;
using ClientCore;

namespace DTAClient.DXGUI.GameLobby
{
    public class SkirmishLobby : GameLobbyBase
    {
        public SkirmishLobby(WindowManager windowManager, List<GameMode> GameModes) : base(windowManager, "SkirmishLobby", GameModes)
        {
        }

        DXListBox lbMapList;
        DXDropDown ddGameMode;
        DXLabel lblGameModeSelect;
        GameInProgressWindow gameInProgressWindow;

        public override void Initialize()
        {
            base.Initialize();

            InitPlayerOptionDropdowns(118, 92, 88, 56, 53, new Point(13, 24));

            lblGameModeSelect = new DXLabel(WindowManager);
            lblGameModeSelect.ClientRectangle = new Rectangle(6, 250, 0, 0);
            lblGameModeSelect.FontIndex = 1;
            lblGameModeSelect.Text = "GAME MODE:";

            lbMapList = new DXListBox(WindowManager);
            lbMapList.ClientRectangle = new Rectangle(6, MapPreviewBox.ClientRectangle.Y, 433, MapPreviewBox.ClientRectangle.Height);
            lbMapList.SelectedIndexChanged += LbMapList_SelectedIndexChanged;
            lbMapList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbMapList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 192), 1, 1);

            ddGameMode = new DXDropDown(WindowManager);
            ddGameMode.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.Right - 150, 247, 150, 21);
            ddGameMode.ClickSoundEffect = AssetLoader.LoadSound("dropdown.wav");
            ddGameMode.SelectedIndexChanged += DdGameMode_SelectedIndexChanged;

            MapPreviewBox.StartingLocationSelected += MapPreviewBox_StartingLocationSelected;

            AddChild(lblGameModeSelect);
            AddChild(lbMapList);
            AddChild(ddGameMode);

            gameInProgressWindow = new GameInProgressWindow(WindowManager);
            gameInProgressWindow.CenterOnParent();
            AddChild(gameInProgressWindow);
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

            lbMapList.Clear();

            foreach (Map map in GameMode.Maps)
                lbMapList.AddItem(map.Name);
        }

        private void LbMapList_SelectedIndexChanged(object sender, EventArgs e)
        {
            GameMode gm = GameModes[ddGameMode.SelectedIndex];
            Map map = gm.Maps[lbMapList.SelectedIndex];

            ChangeMap(gm, map);
        }

        protected override void BtnLaunchGame_LeftClick(object sender, EventArgs e)
        {
            gameInProgressWindow.Visible = true;

            StartGame();
        }

        protected override void GameProcessExited()
        {
            gameInProgressWindow.Visible = false;

            base.GameProcessExited();
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            this.Enabled = false;
            this.Visible = false;
        }
    }
}
