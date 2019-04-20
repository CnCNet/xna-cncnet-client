﻿using ClientGUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using ClientCore;
using Rampastring.Tools;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using DTAClient.Domain.Multiplayer;
using ClientCore.Statistics;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// An abstract base class for a multiplayer game loading lobby.
    /// </summary>
    public abstract class GameLoadingLobbyBase : XNAWindow, ISwitchable
    {
        public GameLoadingLobbyBase(WindowManager windowManager) : base(windowManager)
        {
        }

        public event EventHandler GameLeft;

        /// <summary>
        /// The list of players in the current saved game.
        /// </summary>
        protected List<SavedGamePlayer> SGPlayers = new List<SavedGamePlayer>();

        /// <summary>
        /// The list of players in the game lobby.
        /// </summary>
        protected List<PlayerInfo> Players = new List<PlayerInfo>();

        protected bool IsHost = false;

        protected XNAClientDropDown ddSavedGame;

        protected ChatListBox lbChatMessages;
        protected XNATextBox tbChatInput;

        protected EnhancedSoundEffect sndGetReady;
        protected EnhancedSoundEffect sndJoinSound;
        protected EnhancedSoundEffect sndLeaveSound;
        protected EnhancedSoundEffect sndMessageSound;

        private XNALabel lblDescription;
        private XNAPanel panelPlayers;
        private XNALabel[] lblPlayerNames;

        private XNALabel lblMapName;
        protected XNALabel lblMapNameValue;
        private XNALabel lblGameMode;
        protected XNALabel lblGameModeValue;
        private XNALabel lblSavedGameTime;

        private XNAClientButton btnLoadGame;
        private XNAClientButton btnLeaveGame;

        private List<MultiplayerColor> MPColors = new List<MultiplayerColor>();

        private string loadedGameID;

        private bool isSettingUp = false;
        private FileSystemWatcher fsw;

        private int uniqueGameId = 0;
        private DateTime gameLoadTime;

        public override void Initialize()
        {
            Name = "GameLoadingLobby";
            ClientRectangle = new Rectangle(0, 0, 590, 510);
            BackgroundTexture = AssetLoader.LoadTexture("loadmpsavebg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblDescription.Text = "Wait for all players to join and get ready, then click Load Game to load the saved multiplayer game.";

            panelPlayers = new XNAPanel(WindowManager);
            panelPlayers.ClientRectangle = new Rectangle(12, 32, 373, 125);
            panelPlayers.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            panelPlayers.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            AddChild(lblDescription);
            AddChild(panelPlayers);

            lblPlayerNames = new XNALabel[8];
            for (int i = 0; i < 4; i++)
            {
                XNALabel lblPlayerName = new XNALabel(WindowManager);
                lblPlayerName.Name = "lblPlayerName" + i;
                lblPlayerName.ClientRectangle = new Rectangle(9, 9 + 30 * i, 0, 0);
                lblPlayerName.Text = "Player " + i;
                panelPlayers.AddChild(lblPlayerName);
                lblPlayerNames[i] = lblPlayerName;
            }

            for (int i = 4; i < 8; i++)
            {
                XNALabel lblPlayerName = new XNALabel(WindowManager);
                lblPlayerName.Name = "lblPlayerName" + i;
                lblPlayerName.ClientRectangle = new Rectangle(190, 9 + 30 * (i - 4), 0, 0);
                lblPlayerName.Text = "Player " + i;
                panelPlayers.AddChild(lblPlayerName);
                lblPlayerNames[i] = lblPlayerName;
            }

            lblMapName = new XNALabel(WindowManager);
            lblMapName.Name = "lblMapName";
            lblMapName.FontIndex = 1;
            lblMapName.ClientRectangle = new Rectangle(panelPlayers.Right + 12,
                panelPlayers.Y, 0, 0);
            lblMapName.Text = "MAP:";

            lblMapNameValue = new XNALabel(WindowManager);
            lblMapNameValue.Name = "lblMapNameValue";
            lblMapNameValue.ClientRectangle = new Rectangle(lblMapName.X,
                lblMapName.Y + 18, 0, 0);
            lblMapNameValue.Text = "Map name";

            lblGameMode = new XNALabel(WindowManager);
            lblGameMode.Name = "lblGameMode";
            lblGameMode.ClientRectangle = new Rectangle(lblMapName.X,
                panelPlayers.Y + 40, 0, 0);
            lblGameMode.FontIndex = 1;
            lblGameMode.Text = "GAME MODE:";

            lblGameModeValue = new XNALabel(WindowManager);
            lblGameModeValue.Name = "lblGameModeValue";
            lblGameModeValue.ClientRectangle = new Rectangle(lblGameMode.X,
                lblGameMode.Y + 18, 0, 0);
            lblGameModeValue.Text = "Game mode";

            lblSavedGameTime = new XNALabel(WindowManager);
            lblSavedGameTime.Name = "lblSavedGameTime";
            lblSavedGameTime.ClientRectangle = new Rectangle(lblMapName.X,
                panelPlayers.Bottom - 40, 0, 0);
            lblSavedGameTime.FontIndex = 1;
            lblSavedGameTime.Text = "SAVED GAME:";

            ddSavedGame = new XNAClientDropDown(WindowManager);
            ddSavedGame.Name = "ddSavedGame";
            ddSavedGame.ClientRectangle = new Rectangle(lblSavedGameTime.X,
                panelPlayers.Bottom - 21,
                Width - lblSavedGameTime.X - 12, 21);
            ddSavedGame.SelectedIndexChanged += DdSavedGame_SelectedIndexChanged;

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.ClientRectangle = new Rectangle(12, panelPlayers.Bottom + 12,
                Width - 24, 
                Height - panelPlayers.Bottom - 12 - 29 - 34);

            tbChatInput = new XNATextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                lbChatMessages.Bottom + 3, lbChatMessages.Width, 19);
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            btnLoadGame = new XNAClientButton(WindowManager);
            btnLoadGame.Name = "btnLoadGame";
            btnLoadGame.ClientRectangle = new Rectangle(lbChatMessages.X,
                tbChatInput.Bottom + 6, 133, 23);
            btnLoadGame.Text = "Load Game";
            btnLoadGame.LeftClick += BtnLoadGame_LeftClick;

            btnLeaveGame = new XNAClientButton(WindowManager);
            btnLeaveGame.Name = "btnLeaveGame";
            btnLeaveGame.ClientRectangle = new Rectangle(Width - 145,
                btnLoadGame.Y, 133, 23);
            btnLeaveGame.Text = "Leave Game";
            btnLeaveGame.LeftClick += BtnLeaveGame_LeftClick;

            AddChild(lblMapName);
            AddChild(lblMapNameValue);
            AddChild(lblGameMode);
            AddChild(lblGameModeValue);
            AddChild(lblSavedGameTime);
            AddChild(lbChatMessages);
            AddChild(tbChatInput);
            AddChild(btnLoadGame);
            AddChild(btnLeaveGame);
            AddChild(ddSavedGame);

            base.Initialize();

            sndGetReady = new EnhancedSoundEffect("getready.wav");
            sndJoinSound = new EnhancedSoundEffect("joingame.wav");
            sndLeaveSound = new EnhancedSoundEffect("leavegame.wav");
            sndMessageSound = new EnhancedSoundEffect("message.wav");

            MPColors = MultiplayerColor.LoadColors();

            WindowManager.CenterControlOnScreen(this);

            if (SavedGameManager.AreSavedGamesAvailable())
            {
                fsw = new FileSystemWatcher(ProgramConstants.GamePath + "Saved Games", "*.NET");
                fsw.EnableRaisingEvents = false;
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Created;
            }
        }

        private void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            LeaveGame();
        }

        protected virtual void LeaveGame()
        {
            GameLeft?.Invoke(this, EventArgs.Empty);
        }

        private void fsw_Created(object sender, FileSystemEventArgs e)
        {
            AddCallback(new Action<FileSystemEventArgs>(HandleFSWEvent), e);
        }

        private void HandleFSWEvent(FileSystemEventArgs e)
        {
            Logger.Log("FSW Event: " + e.FullPath);

            if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
            {
                SavedGameManager.RenameSavedGame();
            }
        }

        private void BtnLoadGame_LeftClick(object sender, EventArgs e)
        {
            if (!IsHost)
            {
                RequestReadyStatus();
                return;
            }

            if (Players.Find(p => !p.Ready) != null)
            {
                GetReadyNotification();
                return;
            }

            if (Players.Count != SGPlayers.Count)
            {
                NotAllPresentNotification();
                return;
            }

            HostStartGame();
        }

        protected abstract void RequestReadyStatus();

        protected virtual void GetReadyNotification()
        {
            AddNotice("The game host wants to load the game but cannot because not all players are ready!");
            SoundPlayer.Play(sndGetReady);

            WindowManager.FlashWindow();
        }

        protected virtual void NotAllPresentNotification()
        {
            AddNotice("You cannot load the game before all players are present.");
        }

        protected abstract void HostStartGame();

        protected void LoadGame()
        {
            File.Delete(ProgramConstants.GamePath + "spawn.ini");

            File.Copy(ProgramConstants.GamePath + "Saved Games\\spawnSG.ini", ProgramConstants.GamePath + "spawn.ini");

            IniFile spawnIni = new IniFile(ProgramConstants.GamePath + "spawn.ini");

            int sgIndex = (ddSavedGame.Items.Count - 1) - ddSavedGame.SelectedIndex;

            spawnIni.SetStringValue("Settings", "SaveGameName",
                string.Format("SVGM_{0}.NET", sgIndex.ToString("D3")));
            spawnIni.SetBooleanValue("Settings", "LoadSaveGame", true);

            PlayerInfo localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

            if (localPlayer == null)
                return;

            spawnIni.SetIntValue("Settings", "Port", localPlayer.Port);

            for (int i = 1; i < Players.Count; i++)
            {
                string otherName = spawnIni.GetStringValue("Other" + i, "Name", string.Empty);

                if (string.IsNullOrEmpty(otherName))
                    continue;

                PlayerInfo otherPlayer = Players.Find(p => p.Name == otherName);

                if (otherPlayer == null)
                    continue;

                spawnIni.SetStringValue("Other" + i, "Ip", otherPlayer.IPAddress);
                spawnIni.SetIntValue("Other" + i, "Port", otherPlayer.Port);
            }

            WriteSpawnIniAdditions(spawnIni);
            spawnIni.WriteIniFile();

            File.Delete(ProgramConstants.GamePath + "spawnmap.ini");
            StreamWriter sw = new StreamWriter(ProgramConstants.GamePath + "spawnmap.ini");
            sw.WriteLine("[Map]");
            sw.WriteLine("Size=0,0,50,50");
            sw.WriteLine("LocalSize=0,0,50,50");
            sw.WriteLine();
            sw.Close();

            gameLoadTime = DateTime.Now;

            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;
            GameProcessLogic.StartGameProcess();
            
            fsw.EnableRaisingEvents = true;
        }

        private void SharedUILogic_GameProcessExited()
        {
            AddCallback(new Action(HandleGameProcessExited), null);
        }

        protected virtual void HandleGameProcessExited()
        {
            fsw.EnableRaisingEvents = false;

            GameProcessLogic.GameProcessExited -= SharedUILogic_GameProcessExited;

            var matchStatistics = StatisticsManager.Instance.GetMatchWithGameID(uniqueGameId);

            if (matchStatistics != null)
            {
                int oldLength = matchStatistics.LengthInSeconds;
                int newLength = matchStatistics.LengthInSeconds + 
                    (int)(DateTime.Now - gameLoadTime).TotalSeconds;

                matchStatistics.ParseStatistics(ProgramConstants.GamePath,
                    ClientConfiguration.Instance.LocalGame, true);

                matchStatistics.LengthInSeconds = newLength;

                StatisticsManager.Instance.SaveDatabase();
            }
        }

        protected virtual void WriteSpawnIniAdditions(IniFile spawnIni)
        {
            // Do nothing by default
        }

        protected void AddNotice(string notice)
        {
            AddNotice(notice, Color.White);
        }

        protected abstract void AddNotice(string message, Color color);

        /// <summary>
        /// Refreshes the UI  based on the latest saved game
        /// and information in the saved spawn.ini file, as well
        /// as information on whether the local player is the host of the game.
        /// </summary>
        public void Refresh(bool isHost)
        {
            isSettingUp = true;
            IsHost = isHost;

            SGPlayers.Clear();
            Players.Clear();
            ddSavedGame.Items.Clear();
            lbChatMessages.Clear();
            lbChatMessages.TopIndex = 0;

            ddSavedGame.AllowDropDown = isHost;
            btnLoadGame.Text = isHost ? "Load Game" : "I'm Ready";

            IniFile spawnSGIni = new IniFile(ProgramConstants.GamePath + "Saved Games\\spawnSG.ini");

            loadedGameID = spawnSGIni.GetStringValue("Settings", "GameID", "0");
            lblMapNameValue.Text = spawnSGIni.GetStringValue("Settings", "UIMapName", string.Empty);
            lblGameModeValue.Text = spawnSGIni.GetStringValue("Settings", "UIGameMode", string.Empty);

            uniqueGameId = spawnSGIni.GetIntValue("Settings", "GameID", -1);

            int playerCount = spawnSGIni.GetIntValue("Settings", "PlayerCount", 0);

            SavedGamePlayer localPlayer = new SavedGamePlayer();
            localPlayer.Name = ProgramConstants.PLAYERNAME;
            localPlayer.ColorIndex = MPColors.FindIndex(
                c => c.GameColorIndex == spawnSGIni.GetIntValue("Settings", "Color", 0));

            SGPlayers.Add(localPlayer);

            for (int i = 1; i < playerCount; i++)
            {
                string sectionName = "Other" + i;

                SavedGamePlayer sgPlayer = new SavedGamePlayer();
                sgPlayer.Name = spawnSGIni.GetStringValue(sectionName, "Name", "Unknown player");
                sgPlayer.ColorIndex = MPColors.FindIndex(
                    c => c.GameColorIndex == spawnSGIni.GetIntValue(sectionName, "Color", 0));

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

            CopyPlayerDataToUI();
            isSettingUp = false;
        }

        protected void CopyPlayerDataToUI()
        {
            for (int i = 0; i < SGPlayers.Count; i++)
            {
                SavedGamePlayer sgPlayer = SGPlayers[i];

                PlayerInfo pInfo = Players.Find(p => p.Name == SGPlayers[i].Name);

                XNALabel playerLabel = lblPlayerNames[i];

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
        }

        protected virtual string GetIPAddressForPlayer(PlayerInfo pInfo)
        {
            return "0.0.0.0";
        }

        private void DdSavedGame_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsHost)
                return;

            for (int i = 1; i < Players.Count; i++)
                Players[i].Ready = false;

            CopyPlayerDataToUI();

            if (!isSettingUp)
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

        public void SwitchOn()
        {
            Visible = true;
            Enabled = true;
        }

        public void SwitchOff()
        {
            Visible = false;
            Enabled = false;
        }

        public abstract string GetSwitchName();
    }
}
