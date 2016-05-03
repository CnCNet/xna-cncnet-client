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
        DXLabel lblGameMode;

        public override void Initialize()
        {
            base.Initialize();

            InitPlayerOptionDropdowns(118, 92, 88, 56, 53, new Point(13, 24));

            lblGameMode = new DXLabel(WindowManager);
            lblGameMode.ClientRectangle = new Rectangle(6, 245, 0, 0);
            lblGameMode.FontIndex = 1;
            lblGameMode.Text = "GAME MODE:";

            lbMapList = new DXListBox(WindowManager);
            lbMapList.ClientRectangle = new Rectangle(6, MapPreviewBox.ClientRectangle.Y, 433, MapPreviewBox.ClientRectangle.Height);
            lbMapList.SelectedIndexChanged += LbMapList_SelectedIndexChanged;
            lbMapList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbMapList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);

            ddGameMode = new DXDropDown(WindowManager);
            ddGameMode.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.Right - 150, 242, 150, 21);
            ddGameMode.ClickSoundEffect = AssetLoader.LoadSound("button.wav");
            ddGameMode.SelectedIndexChanged += DdGameMode_SelectedIndexChanged;

            AddChild(lblGameMode);
            AddChild(lbMapList);
            AddChild(ddGameMode);

            foreach (GameMode gm in GameModes)
                ddGameMode.AddItem(gm.UIName);

            Players.Add(new PlayerInfo(ProgramConstants.PLAYERNAME, 0, 0, 0, 0));
            

            if (ddGameMode.Items.Count > 0)
            {
                ddGameMode.SelectedIndex = 0;

                lbMapList.SelectedIndex = 0;
            }
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
            throw new NotImplementedException();
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            this.Enabled = false;
            this.Visible = false;
        }
    }
}
