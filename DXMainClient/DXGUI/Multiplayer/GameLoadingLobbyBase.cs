using ClientGUI;
using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.domain.CnCNet;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.DXGUI.Multiplayer
{
    public abstract class GameLoadingLobbyBase : DXWindow
    {
        public GameLoadingLobbyBase(WindowManager windowManager) : base(windowManager)
        {
        }

        protected List<SavedGamePlayer> SGPlayers = new List<SavedGamePlayer>();

        protected List<PlayerInfo> Players = new List<PlayerInfo>();

        protected bool IsHost = false;

        protected DXDropDown ddSavedGame;

        protected ChatListBox lbChatMessages;
        protected DXTextBox tbChatInput;

        private DXLabel lblDescription;
        private DXPanel panelPlayers;
        private DXLabel[] lblPlayerNames;

        private DXLabel lblMapName;
        private DXLabel lblMapNameValue;
        private DXLabel lblGameMode;
        private DXLabel lblGameModeValue;
        private DXLabel lblSavedGameTime;

        private DXButton btnLoadGame;
        private DXButton btnLeaveGame;

        private List<MultiplayerColor> MPColors = new List<MultiplayerColor>();

        private string localGame;
        private string loadedGameID;

        public override void Initialize()
        {
            Name = "GameLoadingLobby";
            ClientRectangle = new Rectangle(0, 0, 590, 510);

            lblDescription = new DXLabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblDescription.Text = "Wait for all players to join and get ready, then click Load Game to load the saved multiplayer game.";

            panelPlayers = new DXPanel(WindowManager);
            panelPlayers.ClientRectangle = new Rectangle(12, 32, 373, 125);
            panelPlayers.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            panelPlayers.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            AddChild(lblDescription);
            AddChild(panelPlayers);

            lblPlayerNames = new DXLabel[8];
            for (int i = 0; i < 4; i++)
            {
                DXLabel lblPlayerName = new DXLabel(WindowManager);
                lblPlayerName.Name = "lblPlayerName" + i;
                lblPlayerName.ClientRectangle = new Rectangle(9, 9 + 30 * i, 0, 0);
                lblPlayerName.Text = "Player " + i;
                panelPlayers.AddChild(lblPlayerName);
                lblPlayerNames[i] = lblPlayerName;
            }

            for (int i = 4; i < 8; i++)
            {
                DXLabel lblPlayerName = new DXLabel(WindowManager);
                lblPlayerName.Name = "lblPlayerName" + i;
                lblPlayerName.ClientRectangle = new Rectangle(190, 9 + 30 * (i - 4), 0, 0);
                lblPlayerName.Text = "Player " + i;
                panelPlayers.AddChild(lblPlayerName);
                lblPlayerNames[i] = lblPlayerName;
            }

            lblMapName = new DXLabel(WindowManager);
            lblMapName.Name = "lblMapName";
            lblMapName.FontIndex = 1;
            lblMapName.ClientRectangle = new Rectangle(panelPlayers.ClientRectangle.Right + 12,
                panelPlayers.ClientRectangle.Y, 0, 0);
            lblMapName.Text = "MAP:";

            lblMapNameValue = new DXLabel(WindowManager);
            lblMapNameValue.Name = "lblMapNameValue";
            lblMapNameValue.ClientRectangle = new Rectangle(lblMapName.ClientRectangle.X,
                lblMapName.ClientRectangle.Y + 20, 0, 0);
            lblMapNameValue.Text = "Map name";

            lblGameMode = new DXLabel(WindowManager);
            lblGameMode.Name = "lblGameMode";
            lblGameMode.ClientRectangle = new Rectangle(lblMapName.ClientRectangle.X,
                panelPlayers.ClientRectangle.Y + 40, 0, 0);
            lblGameMode.FontIndex = 1;
            lblGameMode.Text = "GAME MODE:";

            lblGameModeValue = new DXLabel(WindowManager);
            lblGameModeValue.Name = "lblGameModeValue";
            lblGameModeValue.ClientRectangle = new Rectangle(lblGameMode.ClientRectangle.X,
                lblGameMode.ClientRectangle.Y + 20, 0, 0);
            lblGameModeValue.Text = "Game mode";

            lblSavedGameTime = new DXLabel(WindowManager);
            lblSavedGameTime.Name = "lblSavedGameTime";
            lblSavedGameTime.ClientRectangle = new Rectangle(lblMapName.ClientRectangle.X,
                panelPlayers.ClientRectangle.Bottom - 40, 0, 0);
            lblSavedGameTime.FontIndex = 1;
            lblSavedGameTime.Text = "SAVED GAME:";

            ddSavedGame = new DXDropDown(WindowManager);
            ddSavedGame.Name = "ddSavedGame";
            ddSavedGame.ClientRectangle = new Rectangle(lblSavedGameTime.ClientRectangle.X,
                panelPlayers.ClientRectangle.Bottom - 21,
                ClientRectangle.Width - lblSavedGameTime.ClientRectangle.X - 12, 21);
            ddSavedGame.SelectedIndexChanged += DdSavedGame_SelectedIndexChanged;

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.ClientRectangle = new Rectangle(12, panelPlayers.ClientRectangle.Bottom + 12,
                ClientRectangle.Width - 24, 
                ClientRectangle.Height - panelPlayers.ClientRectangle.Bottom - 12 - 29 - 34);

            tbChatInput = new DXTextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X,
                lbChatMessages.ClientRectangle.Bottom + 3, lbChatMessages.ClientRectangle.Width, 19);
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            btnLoadGame = new DXButton(WindowManager);
            btnLoadGame.Name = "btnLoadGame";
            btnLoadGame.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X,
                tbChatInput.ClientRectangle.Bottom + 6, 133, 23);
            btnLoadGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnLoadGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnLoadGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnLoadGame.FontIndex = 1;
            btnLoadGame.Text = "Load Game";

            btnLeaveGame = new DXButton(WindowManager);
            btnLeaveGame.Name = "btnLeaveGame";
            btnLeaveGame.ClientRectangle = new Rectangle(ClientRectangle.Width - 145,
                btnLoadGame.ClientRectangle.Y, 133, 23);
            btnLeaveGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnLeaveGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnLeaveGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnLeaveGame.FontIndex = 1;
            btnLeaveGame.Text = "Leave Game";

            AddChild(lblMapName);
            AddChild(lblMapNameValue);
            AddChild(lblGameMode);
            AddChild(lblGameModeValue);
            AddChild(lblSavedGameTime);
            AddChild(ddSavedGame);
            AddChild(lbChatMessages);
            AddChild(tbChatInput);
            AddChild(btnLoadGame);
            AddChild(btnLeaveGame);

            base.Initialize();

            MPColors = MultiplayerColor.LoadColors();
            localGame = DomainController.Instance().GetDefaultGame();

            WindowManager.CenterControlOnScreen(this);
        }

        public void Refresh(bool isHost)
        {
            IsHost = isHost;

            SGPlayers.Clear();
            Players.Clear();
            ddSavedGame.Items.Clear();

            btnLoadGame.Text = isHost ? "Load Game" : "I'm Ready";

            IniFile spawnSGIni = new IniFile(ProgramConstants.GamePath + "Saved Games\\spawnSG.ini");

            loadedGameID = spawnSGIni.GetStringValue("Settings", "GameID", "0");
            lblMapNameValue.Text = spawnSGIni.GetStringValue("Settings", "UIMapName", string.Empty);
            lblGameModeValue.Text = spawnSGIni.GetStringValue("Settings", "UIGameMode", string.Empty);

            int playerCount = spawnSGIni.GetIntValue("Settings", "PlayerCount", 0);

            SavedGamePlayer localPlayer = new SavedGamePlayer();
            localPlayer.Name = ProgramConstants.PLAYERNAME;
            localPlayer.ColorIndex = MPColors.FindIndex(
                c => c.GameColorIndex == spawnSGIni.GetIntValue("Settings", "Color", 0));

            SGPlayers.Add(localPlayer);

            for (int i = 1; i < playerCount; i++)
            {
                SavedGamePlayer sgPlayer = new SavedGamePlayer();
                sgPlayer.Name = spawnSGIni.GetStringValue("Other" + i, "Name", "Unknown player");
                sgPlayer.ColorIndex = MPColors.FindIndex(
                    c => c.GameColorIndex == spawnSGIni.GetIntValue("Other " + i, "Color", 0));

                SGPlayers.Add(sgPlayer);
            }

            for (int i = 0; i < SGPlayers.Count; i++)
            {
                lblPlayerNames[i].Enabled = true;
                lblPlayerNames[i].Visible = true;
            }

            for (int i = SGPlayers.Count; i < 8; i++)
            {
                lblPlayerNames[i].Enabled = false;
                lblPlayerNames[i].Visible = false;
            }

            List<string> timestamps = SavedGameManager.GetSaveGameTimestamps();
            timestamps.Reverse(); // Most recent saved game first

            timestamps.ForEach(ts => ddSavedGame.AddItem(ts));

            if (ddSavedGame.Items.Count > 0)
                ddSavedGame.SelectedIndex = 0;
        }

        protected void CopyPlayerDataToUI()
        {
            for (int i = 0; i < SGPlayers.Count; i++)
            {
                SavedGamePlayer sgPlayer = SGPlayers[i];

                PlayerInfo pInfo = Players.Find(p => p.Name == SGPlayers[i].Name);

                DXLabel playerLabel = lblPlayerNames[i];

                if (pInfo == null)
                {
                    playerLabel.RemapColor = Color.Gray;
                    playerLabel.Text = sgPlayer.Name + " (Not present)";
                    continue;
                }

                playerLabel.RemapColor = sgPlayer.ColorIndex > -1 ? MPColors[sgPlayer.ColorIndex].XnaColor
                    : Color.White;
                playerLabel.Text = pInfo.Ready ? sgPlayer.Name : sgPlayer.Name + " (Not Ready)";
            }

            if (IsHost)
                BroadcastOptions();
        }

        private void DdSavedGame_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsHost)
                return;

            BroadcastOptions();
        }

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            SendChatMessage(tbChatInput.Text);
            tbChatInput.Text = string.Empty;
        }

        /// <summary>
        /// Override in a derived class to broadcast player ready statuses and the selected
        /// saved game to players.
        /// </summary>
        protected abstract void BroadcastOptions();

        protected abstract void SendChatMessage(string message);

        public override void Draw(GameTime gameTime)
        {
            Renderer.FillRectangle(new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY),
                new Color(0, 0, 0, 255));

            base.Draw(gameTime);
        }
    }
}
