using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using ClientCore;
using Microsoft.Xna.Framework.Audio;
using System.IO;
using Rampastring.Tools;
using ClientCore.Statistics;
using DTAClient.DXGUI.Generic;
using DTAClient.Domain.Multiplayer;
using ClientGUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A generic base class for multiplayer game lobbies (CnCNet and LAN).
    /// </summary>
    public abstract class MultiplayerGameLobby : GameLobbyBase, ISwitchable
    {
        public MultiplayerGameLobby(WindowManager windowManager, string iniName, 
            TopBar topBar, List<GameMode> GameModes)
            : base(windowManager, iniName, GameModes, true)
        {
            TopBar = topBar;
        }

        protected XNACheckBox[] ReadyBoxes;

        protected ChatListBox lbChatMessages;
        protected XNASuggestionTextBox tbChatInput;
        protected XNAClientButton btnLockGame;

        protected bool IsHost = false;

        protected bool Locked = false;

        protected SoundEffectInstance sndJoinSound;
        protected SoundEffectInstance sndLeaveSound;
        protected SoundEffectInstance sndMessageSound;

        protected TopBar TopBar;

        private FileSystemWatcher fsw;

        private bool gameSaved = false;

        private bool switched = false;

        public override void Initialize()
        {
            Name = "MultiplayerGameLobby";

            base.Initialize();

            InitPlayerOptionDropdowns();

            ReadyBoxes = new XNACheckBox[PLAYER_COUNT];

            int readyBoxX = GameOptionsIni.GetIntValue(Name, "PlayerReadyBoxX", 7);

            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                XNACheckBox chkPlayerReady = new XNACheckBox(WindowManager);
                chkPlayerReady.Name = "chkPlayerReady" + i;
                chkPlayerReady.Checked = false;
                chkPlayerReady.AllowChecking = false;
                chkPlayerReady.ClientRectangle = new Rectangle(readyBoxX, ddPlayerTeams[i].ClientRectangle.Y + 4,
                    0, 0);

                PlayerOptionsPanel.AddChild(chkPlayerReady);

                chkPlayerReady.DisabledClearTexture = chkPlayerReady.ClearTexture;
                chkPlayerReady.DisabledCheckedTexture = chkPlayerReady.CheckedTexture;

                ReadyBoxes[i] = chkPlayerReady;
                ddPlayerSides[i].AddItem("Spectator", AssetLoader.LoadTexture("spectatoricon.png"));
            }

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

            tbChatInput = new XNASuggestionTextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.Suggestion = "Type here to chat..";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.Left, 
                lbChatMessages.ClientRectangle.Bottom + 3,
                lbChatMessages.ClientRectangle.Width, 21);
            tbChatInput.MaximumTextLength = 150;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            btnLockGame = new XNAClientButton(WindowManager);
            btnLockGame.Name = "btnLockGame";
            btnLockGame.ClientRectangle = new Rectangle(btnLaunchGame.ClientRectangle.Right + 12,
                btnLaunchGame.ClientRectangle.Y, 133, 23);
            btnLockGame.Text = "Lock Game";
            btnLockGame.LeftClick += BtnLockGame_LeftClick;

            AddChild(lbChatMessages);
            AddChild(tbChatInput);
            AddChild(btnLockGame);

            MapPreviewBox.LocalStartingLocationSelected += MapPreviewBox_LocalStartingLocationSelected;
            MapPreviewBox.StartingLocationApplied += MapPreviewBox_StartingLocationApplied;

            InitializeWindow();

            SoundEffect seJoinSound = AssetLoader.LoadSound("joingame.wav");
            SoundEffect seLeaveSound = AssetLoader.LoadSound("leavegame.wav");
            SoundEffect seMessageSound = AssetLoader.LoadSound("message.wav");

            if (seJoinSound != null)
                sndJoinSound = seJoinSound.CreateInstance();

            if (seLeaveSound != null)
                sndLeaveSound = seLeaveSound.CreateInstance();

            if (seMessageSound != null)
                sndMessageSound = seMessageSound.CreateInstance();

            if (SavedGameManager.AreSavedGamesAvailable())
            {
                fsw = new FileSystemWatcher(ProgramConstants.GamePath + "Saved Games", "*.NET");
                fsw.EnableRaisingEvents = false;
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Created;
            }

            CenterOnParent();

            // To move the lblMapAuthor label into its correct position
            // if it was moved in the theme description INI file
            LoadDefaultMap();
        }

        private void fsw_Created(object sender, FileSystemEventArgs e)
        {
            AddCallback(new Action<FileSystemEventArgs>(FSWEvent), e);
        }

        private void FSWEvent(FileSystemEventArgs e)
        {
            Logger.Log("FSW Event: " + e.FullPath);

            if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
            {
                if (!gameSaved)
                {
                    bool success = SavedGameManager.InitSavedGames();

                    if (!success)
                        return;
                }

                gameSaved = true;

                SavedGameManager.RenameSavedGame();
            }
        }

        protected override void GameProcessExited()
        {
            gameSaved = false;

            if (IsHost)
            {
                GenerateGameID();
            }

            base.GameProcessExited();
        }

        private void GenerateGameID()
        {
            int i = 0;

            while (i < 20)
            {
                string s = DateTime.Now.Day.ToString() +
                    DateTime.Now.Month.ToString() +
                    DateTime.Now.Hour.ToString() +
                    DateTime.Now.Minute.ToString();

                UniqueGameID = int.Parse(i.ToString() + s);

                if (StatisticsManager.Instance.GetMatchWithGameID(UniqueGameID) == null)
                    break;

                i++;
            }
        }

        private void BtnLockGame_LeftClick(object sender, EventArgs e)
        {
            HandleLockGameButtonClick();
        }

        protected virtual void HandleLockGameButtonClick()
        {
            if (Locked)
                UnlockGame(true);
            else
                LockGame();
        }

        protected abstract void LockGame();

        protected abstract void UnlockGame(bool manual);

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            if (tbChatInput.Text.StartsWith("/"))
            {
                string command = tbChatInput.Text.ToUpper();

                tbChatInput.Text = string.Empty;

                if (command == "/HIDEMAPS")
                {
                    if (!IsHost)
                    {
                        AddNotice("/HIDEMAPS is for game hosts only.");
                        return;
                    }

                    HideMapList();
                }
                else if (command == "/SHOWMAPS")
                {
                    if (!IsHost)
                    {
                        AddNotice("/SHOWMAPS is for game hosts only.");
                        return;
                    }

                    ShowMapList();
                }
                else
                {
                    AddNotice("Possible commands:");
                    AddNotice("/HIDEMAPS: Hide map list (game host only)");
                    AddNotice("/SHOWMAPS: Show map list (game host only)");
                }

                return;
            }

            SendChatMessage(tbChatInput.Text);
            tbChatInput.Text = string.Empty;
        }

        protected abstract void SendChatMessage(string message);

        /// <summary>
        /// Changes the game lobby's UI depending on whether the local player is the host.
        /// </summary>
        /// <param name="isHost">Determines whether the local player is the host of the game.</param>
        protected void Refresh(bool isHost)
        {
            IsHost = isHost;
            switched = false;
            Locked = false;

            MapPreviewBox.EnableContextMenu = IsHost;

            btnLaunchGame.Text = IsHost ? "Launch Game" : "I'm Ready";

            if (IsHost)
            {
                ShowMapList();

                btnLockGame.Text = "Lock Game";
                btnLockGame.Enabled = true;
                btnLockGame.Visible = true;

                foreach (GameLobbyDropDown dd in DropDowns)
                    dd.InputEnabled = true;

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                    checkBox.InputEnabled = true;

                GenerateGameID();
            }
            else
            {
                HideMapList();

                btnLockGame.Enabled = false;
                btnLockGame.Visible = false;

                foreach (GameLobbyDropDown dd in DropDowns)
                    dd.InputEnabled = false;

                foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                    checkBox.InputEnabled = false;
            }

            LoadDefaultMap();

            lbChatMessages.Clear();
            lbChatMessages.TopIndex = 0;

            if (SavedGameManager.AreSavedGamesAvailable())
                fsw.EnableRaisingEvents = true;

            if (SavedGameManager.GetSaveGameCount() > 0)
            {
                lbChatMessages.AddItem("Multiplayer saved games from a previous match have been detected. " +
                    "The saved games of the previous match will be deleted if you create new saves during this match.",
                    Color.Yellow, true);
            }
        }

        private void HideMapList()
        {
            lbChatMessages.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.Left,
                PlayerOptionsPanel.ClientRectangle.Y,
                lbMapList.ClientRectangle.Width,
                MapPreviewBox.ClientRectangle.Bottom - PlayerOptionsPanel.ClientRectangle.Y);
            lbChatMessages.Name = "lbChatMessages_Player";

            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.Left,
                lbChatMessages.ClientRectangle.Bottom + 3,
                lbChatMessages.ClientRectangle.Width, 21);
            tbChatInput.Name = "tbChatInput_Player";

            ddGameMode.Visible = false;
            ddGameMode.Enabled = false;
            lblGameModeSelect.Visible = false;
            lblGameModeSelect.Enabled = false;
            lbMapList.Visible = false;
            lbMapList.Enabled = false;

            lbChatMessages.GetAttributes(ThemeIni);
            tbChatInput.GetAttributes(ThemeIni);
            lbMapList.GetAttributes(ThemeIni);
        }

        private void ShowMapList()
        {
            lbMapList.ClientRectangle = new Rectangle(btnLaunchGame.ClientRectangle.X,
                MapPreviewBox.ClientRectangle.Y + 23,
                MapPreviewBox.ClientRectangle.X - btnLaunchGame.ClientRectangle.X - 12,
                MapPreviewBox.ClientRectangle.Height - 23);

            lbChatMessages.ClientRectangle = new Rectangle(lbMapList.ClientRectangle.Left,
                GameOptionsPanel.ClientRectangle.Y,
                lbMapList.ClientRectangle.Width, GameOptionsPanel.ClientRectangle.Height - 26);
            lbChatMessages.Name = "lbChatMessages_Host";

            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.Left,
                lbChatMessages.ClientRectangle.Bottom + 3,
                lbChatMessages.ClientRectangle.Width, 21);
            tbChatInput.Name = "tbChatInput_Host";

            ddGameMode.Visible = true;
            ddGameMode.Enabled = true;
            lblGameModeSelect.Visible = true;
            lblGameModeSelect.Enabled = true;
            lbMapList.Visible = true;
            lbMapList.Enabled = true;

            lbChatMessages.GetAttributes(ThemeIni);
            tbChatInput.GetAttributes(ThemeIni);
            lbMapList.GetAttributes(ThemeIni);
        }

        private void MapPreviewBox_LocalStartingLocationSelected(object sender, LocalStartingLocationEventArgs e)
        {
            int myIndex = Players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);

            if (myIndex == -1 || Players[myIndex].SideId == ddPlayerSides[0].Items.Count - 1)
                return;

            ddPlayerStarts[myIndex].SelectedIndex = e.StartingLocationIndex;
        }

        private void MapPreviewBox_StartingLocationApplied(object sender, EventArgs e)
        {
            ClearReadyStatuses();
            BroadcastPlayerOptions();
        }

        /// <summary>
        /// Handles the user's click on the "Launch Game" / "I'm Ready" button.
        /// If the local player is the game host, checks if the game can be launched and then
        /// launches the game if it's allowed. If the local player isn't the game host,
        /// sends a ready request.
        /// </summary>
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

                int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
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

        public virtual void Clear()
        {
            fsw.EnableRaisingEvents = false;

            if (!IsHost)
                AIPlayers.Clear();

            Players.Clear();
        }

        protected override void OnGameOptionChanged()
        {
            base.OnGameOptionChanged();

            ClearReadyStatuses();
            CopyPlayerDataToUI();
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

            for (int pId = 0; pId < Players.Count; pId++)
            {
                ReadyBoxes[pId].Checked = Players[pId].Ready;
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                ReadyBoxes[aiId + Players.Count].Checked = true;
            }

            for (int i = AIPlayers.Count + Players.Count; i < PLAYER_COUNT; i++)
            {
                ReadyBoxes[i].Checked = false;
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

        protected override void ChangeMap(GameMode gameMode, Map map)
        {
            base.ChangeMap(gameMode, map);

            ClearReadyStatuses();

            //if (IsHost)
            //    OnGameOptionChanged();
        }

        protected override int GetDefaultMapRankIndex(Map map)
        {
            if (map.MaxPlayers > 3)
                return StatisticsManager.Instance.GetCoopRankForDefaultMap(map.Name, map.MaxPlayers);

            if (StatisticsManager.Instance.HasWonMapInPvP(map.Name, GameMode.UIName, map.MaxPlayers))
                return 2;

            return -1;
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

        public abstract string GetSwitchName();
    }
}
