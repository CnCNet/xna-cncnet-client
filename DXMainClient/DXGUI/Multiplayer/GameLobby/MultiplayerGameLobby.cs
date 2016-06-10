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

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public abstract class MultiplayerGameLobby : GameLobbyBase
    {
        public MultiplayerGameLobby(WindowManager windowManager, string iniName, List<GameMode> GameModes) : base(windowManager, iniName, GameModes)
        {
        }

        protected DXCheckBox[] ReadyBoxes;

        protected ChatListBox lbChatMessages;
        protected DXSuggestionTextBox tbChatInput;
        protected DXButton btnLockGame;

        protected bool IsHost = false;

        protected bool Locked = false;

        bool mapChangeInProgress = false;

        //protected DXLabel lblReady;

        public override void Initialize()
        {
            Name = "MultiplayerGameLobby";

            base.Initialize();

            DrawMode = PanelBackgroundImageDrawMode.STRETCHED; // **** TODO REMOVE ****

            InitPlayerOptionDropdowns();

            ddGameMode.ClientRectangle = new Rectangle(
                MapPreviewBox.ClientRectangle.X - 12 - ddGameMode.ClientRectangle.Width,
                MapPreviewBox.ClientRectangle.Y, ddGameMode.ClientRectangle.Width,
                ddGameMode.ClientRectangle.Height);

            lblGameModeSelect.ClientRectangle = new Rectangle(
                btnLaunchGame.ClientRectangle.X, ddGameMode.ClientRectangle.Y + 1,
                lblGameModeSelect.ClientRectangle.Width, lblGameModeSelect.ClientRectangle.Height);

            lbMapList.ClientRectangle = new Rectangle(btnLaunchGame.ClientRectangle.X, 
                MapPreviewBox.ClientRectangle.Y + 23,
                MapPreviewBox.ClientRectangle.X - btnLaunchGame.ClientRectangle.X - 12,
                MapPreviewBox.ClientRectangle.Height - 23);

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.Left, 
                GameOptionsPanel.ClientRectangle.Y,
               lbMapList.ClientRectangle.Width, GameOptionsPanel.ClientRectangle.Height - 24);
            lbChatMessages.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;

            tbChatInput = new DXSuggestionTextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.Suggestion = "Type here to chat..";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.Left, 
                lbChatMessages.ClientRectangle.Bottom + 3,
                lbChatMessages.ClientRectangle.Width, 21);
            tbChatInput.MaximumTextLength = 100;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            AddChild(lbChatMessages);
            AddChild(tbChatInput);

            MapPreviewBox.LocalStartingLocationSelected += MapPreviewBox_LocalStartingLocationSelected;

            ReadyBoxes = new DXCheckBox[PLAYER_COUNT];

            int readyBoxX = GameOptionsIni.GetIntValue(Name, "PlayerReadyBoxX", 7);

            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                DXCheckBox chkPlayerReady = new DXCheckBox(WindowManager);
                chkPlayerReady.Name = "chkPlayerReady" + i;
                chkPlayerReady.Checked = false;
                chkPlayerReady.AllowChecking = false;
                chkPlayerReady.ClientRectangle = new Rectangle(readyBoxX, ddPlayerTeams[i].ClientRectangle.Y + 4,
                    0, 0);

                PlayerOptionsPanel.AddChild(chkPlayerReady);

                chkPlayerReady.DisabledClearTexture = chkPlayerReady.ClearTexture;
                chkPlayerReady.DisabledCheckedTexture = chkPlayerReady.CheckedTexture;

                ReadyBoxes[i] = chkPlayerReady;
            }

            InitializeWindow();
        }

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            SendChatMessage(tbChatInput.Text);
            tbChatInput.Text = string.Empty;
        }

        protected abstract void SendChatMessage(string message);

        public virtual void Refresh(bool isHost)
        {
            IsHost = isHost;

            MapPreviewBox.EnableContextMenu = IsHost;

            btnLaunchGame.Text = IsHost ? "Launch Game" : "I'm Ready";

            if (IsHost)
            {
                lbMapList.ClientRectangle = new Rectangle(btnLaunchGame.ClientRectangle.X,
                    MapPreviewBox.ClientRectangle.Y + 23,
                    MapPreviewBox.ClientRectangle.X - btnLaunchGame.ClientRectangle.X - 12,
                    MapPreviewBox.ClientRectangle.Height - 23);

                lbChatMessages.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.Left,
                    GameOptionsPanel.ClientRectangle.Y,
                    lbMapList.ClientRectangle.Width, GameOptionsPanel.ClientRectangle.Height - 26);

                ddGameMode.Visible = true;
                ddGameMode.Enabled = true;
                lblGameModeSelect.Visible = true;
                lblGameModeSelect.Enabled = true;
                lbMapList.Visible = true;
                lbMapList.Enabled = true;

                foreach (GameLobbyDropDown dd in DropDowns)
                    dd.InputEnabled = true;

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                    checkBox.InputEnabled = true;
            }
            else
            {
                lbChatMessages.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.Left,
                    PlayerOptionsPanel.ClientRectangle.Y,
                    lbMapList.ClientRectangle.Width, 
                    MapPreviewBox.ClientRectangle.Bottom - PlayerOptionsPanel.ClientRectangle.Y);

                ddGameMode.Visible = false;
                ddGameMode.Enabled = false;
                lblGameModeSelect.Visible = false;
                lblGameModeSelect.Enabled = false;
                lbMapList.Visible = false;
                lbMapList.Enabled = false;

                foreach (GameLobbyDropDown dd in DropDowns)
                    dd.InputEnabled = false;

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                    checkBox.InputEnabled = false;
            }

            lbChatMessages.Clear();
        }

        private void MapPreviewBox_LocalStartingLocationSelected(object sender, LocalStartingLocationEventArgs e)
        {
            int myIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);

            if (myIndex == -1)
                return;

            ddPlayerStarts[myIndex].SelectedIndex = e.StartingLocationIndex;
        }

        protected override void BtnLaunchGame_LeftClick(object sender, EventArgs e)
        {
            if (!IsHost)
            {
                RequestReadyStatus();
                return;
            }

            if (!Locked)
            {
                LockGameNotification();
                return;
            }

            List<int> occupiedColorIds = new List<int>();
            foreach (PlayerInfo player in Players)
            {
                if (occupiedColorIds.Contains(player.ColorId) && player.ColorId > 0)
                {
                    SharedColorsNotification();
                    return;
                }

                occupiedColorIds.Add(player.ColorId);
            }

            if (AIPlayers.Count(pInfo => pInfo.SideId == ddPlayerSides[0].Items.Count - 1) > 0)
            {
                AISpectatorsNotification();
                return;
            }

            if (Map.EnforceMaxPlayers)
            {
                foreach (PlayerInfo pInfo in Players)
                {
                    if (pInfo.StartingLocation == 0)
                        continue;

                    if (Players.Concat(AIPlayers).ToList().Find(
                        p => p.StartingLocation == pInfo.StartingLocation && 
                        p.Name != pInfo.Name) != null)
                    {
                        SharedStartingLocationNotification();
                        return;
                    }
                }

                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
                    int startingLocation = AIPlayers[aiId].StartingLocation;

                    if (startingLocation == 0)
                        continue;

                    int index = AIPlayers.FindIndex(aip => aip.StartingLocation == startingLocation);

                    if (index > -1 && index != aiId)
                    {
                        SharedStartingLocationNotification();
                        return;
                    }
                }

                int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count)
                    + AIPlayers.Count;

                if (totalPlayerCount < Map.MinPlayers)
                {
                    InsufficientPlayersNotification();
                    return;
                }

                if (Map.EnforceMaxPlayers && totalPlayerCount > Map.MaxPlayers)
                {
                    TooManyPlayersNotification();
                    return;
                }
            }

            int iId = 0;
            foreach (PlayerInfo player in Players)
            {
                iId++;

                if (player.Name == ProgramConstants.PLAYERNAME)
                    continue;

                if (!player.Verified)
                {
                    NotVerifiedNotification(iId - 1);
                    return;
                }

                if (!player.Ready)
                {
                    if (player.IsInGame)
                    {
                        StillInGameNotification(iId - 1);
                    }
                    else
                    {
                        GetReadyNotification();
                    }

                    return;
                }
            }

            HostLaunchGame();
        }

        protected virtual void LockGameNotification()
        {
            AddNotice("You need to lock the game room before launching the game.");
        }

        protected virtual void SharedColorsNotification()
        {
            AddNotice("Multiple human players cannot share the same color.");
        }

        protected virtual void AISpectatorsNotification()
        {
            AddNotice("AI players don't enjoy spectating matches. They want some action!");
        }

        protected virtual void SharedStartingLocationNotification()
        {
            AddNotice("Multiple players cannot share the same starting location on this map.");
        }

        protected virtual void NotVerifiedNotification(int playerIndex)
        {
            if (playerIndex > -1 && playerIndex < Players.Count)
            {
                AddNotice(string.Format("Unable to launch game; player {0} hasn't been verified.", Players[playerIndex].Name));
            }
        }

        protected virtual void StillInGameNotification(int playerIndex)
        {
            if (playerIndex > -1 && playerIndex < Players.Count)
            {
                AddNotice("Unable to launch game; player " + Players[playerIndex].Name + " is still playing the game you started previously.");
            }
        }

        protected virtual void GetReadyNotification()
        {
            AddNotice("The host wants to start the game but cannot because not all players are ready!");
        }

        protected virtual void InsufficientPlayersNotification()
        {
            if (Map != null)
                AddNotice("Unable to launch game: this map cannot be played with fewer than " + Map.MinPlayers + " players.");
        }

        protected virtual void TooManyPlayersNotification()
        {
            if (Map != null)
                AddNotice("Unable to launch game: this map cannot be played with more than " + Map.MaxPlayers + " players.");
        }

        protected abstract void HostLaunchGame();

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        protected override void CopyPlayerDataFromUI(object sender, EventArgs e)
        {
            if (PlayerUpdatingInProgress)
                return;

            if (IsHost)
            {
                base.CopyPlayerDataFromUI(sender, e);
                BroadcastPlayerOptions();
                return;
            }

            int myIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);

            if (myIndex == -1)
                return;

            int requestedSide = ddPlayerSides[myIndex].SelectedIndex;
            int requestedColor = ddPlayerColors[myIndex].SelectedIndex;
            int requestedStart = ddPlayerStarts[myIndex].SelectedIndex;
            int requestedTeam = ddPlayerTeams[myIndex].SelectedIndex;

            RequestPlayerOptions(requestedSide, requestedColor, requestedStart, requestedTeam);
        }

        protected override void CopyPlayerDataToUI()
        {
            base.CopyPlayerDataToUI();

            if (IsHost)
            {
                for (int pId = 1; pId < Players.Count; pId++)
                {
                    ddPlayerNames[pId].AllowDropDown = true;
                }
            }
        }

        protected abstract void BroadcastPlayerOptions();

        protected abstract void RequestPlayerOptions(int side, int color, int start, int team);

        protected abstract void RequestReadyStatus();

        protected void AddNotice(string message)
        {
            AddNotice(message, Color.White);
        }

        protected abstract void AddNotice(string message, Color color);

        protected override bool AllowPlayerOptionsChange()
        {
            return IsHost;
        }

        protected void ClearReadyStatuses()
        {
            for (int i = 1; i < Players.Count; i++)
                Players[i].Ready = false;
        }

        protected override void ChangeMap(GameMode gameMode, Map map)
        {
            mapChangeInProgress = true;
            base.ChangeMap(gameMode, map);
            mapChangeInProgress = false;

            ClearReadyStatuses();

            if (IsHost)
                OnGameOptionChanged();
        }
    }
}
