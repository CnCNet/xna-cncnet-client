using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClientCore.Extensions;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// An abstract base class for a multiplayer game loading lobby.
    /// </summary>
    internal abstract class GameLoadingLobbyBase : XNAWindow, ISwitchable
    {
        public GameLoadingLobbyBase(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
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

        protected DiscordHandler discordHandler;

        protected XNAClientDropDown ddSavedGame;

        protected ChatListBox lbChatMessages;
        protected XNATextBox tbChatInput;

        protected EnhancedSoundEffect sndGetReadySound;
        protected EnhancedSoundEffect sndJoinSound;
        protected EnhancedSoundEffect sndLeaveSound;
        protected EnhancedSoundEffect sndMessageSound;

        protected XNALabel lblDescription;
        protected XNAPanel panelPlayers;
        protected XNALabel[] lblPlayerNames;

        private XNALabel lblMapName;
        protected XNALabel lblMapNameValue;
        private XNALabel lblGameMode;
        protected XNALabel lblGameModeValue;
        private XNALabel lblSavedGameTime;

        protected XNAClientButton btnLoadGame;
        protected XNAClientButton btnLeaveGame;

        private List<MultiplayerColor> MPColors = new List<MultiplayerColor>();

        private bool isSettingUp;
        private FileSystemWatcher fsw;

        private int uniqueGameId;
        private DateTime gameLoadTime;

        public override void Initialize()
        {
            Name = "GameLoadingLobby";
            ClientRectangle = new Rectangle(0, 0, 590, 510);
            BackgroundTexture = AssetLoader.LoadTexture("loadmpsavebg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = nameof(lblDescription);
            lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblDescription.Text = "Wait for all players to join and get ready, then click Load Game to load the saved multiplayer game.".L10N("Client:Main:LobbyInitialTip");

            panelPlayers = new XNAPanel(WindowManager);
            panelPlayers.ClientRectangle = new Rectangle(12, 32, 373, 125);
            panelPlayers.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            panelPlayers.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            AddChild(lblDescription);
            AddChild(panelPlayers);

            lblPlayerNames = new XNALabel[8];
            for (int i = 0; i < 8; i++)
            {
                XNALabel lblPlayerName = new XNALabel(WindowManager);
                lblPlayerName.Name = nameof(lblPlayerName) + i;

                if (i < 4)
                    lblPlayerName.ClientRectangle = new Rectangle(9, 9 + 30 * i, 0, 0);
                else
                    lblPlayerName.ClientRectangle = new Rectangle(190, 9 + 30 * (i - 4), 0, 0);

                lblPlayerName.Text = string.Format("Player {0}".L10N("Client:Main:PlayerX"), i) + " ";
                panelPlayers.AddChild(lblPlayerName);
                lblPlayerNames[i] = lblPlayerName;
            }

            lblMapName = new XNALabel(WindowManager);
            lblMapName.Name = nameof(lblMapName);
            lblMapName.FontIndex = 1;
            lblMapName.ClientRectangle = new Rectangle(panelPlayers.Right + 12,
                panelPlayers.Y, 0, 0);
            lblMapName.Text = "MAP:".L10N("Client:Main:MapLabel");

            lblMapNameValue = new XNALabel(WindowManager);
            lblMapNameValue.Name = nameof(lblMapNameValue);
            lblMapNameValue.ClientRectangle = new Rectangle(lblMapName.X,
                lblMapName.Y + 18, 0, 0);
            lblMapNameValue.Text = "Map name".L10N("Client:Main:MapName");

            lblGameMode = new XNALabel(WindowManager);
            lblGameMode.Name = nameof(lblGameMode);
            lblGameMode.ClientRectangle = new Rectangle(lblMapName.X,
                panelPlayers.Y + 40, 0, 0);
            lblGameMode.FontIndex = 1;
            lblGameMode.Text = "GAME MODE:".L10N("Client:Main:GameMode");

            lblGameModeValue = new XNALabel(WindowManager);
            lblGameModeValue.Name = nameof(lblGameModeValue);
            lblGameModeValue.ClientRectangle = new Rectangle(lblGameMode.X,
                lblGameMode.Y + 18, 0, 0);
            lblGameModeValue.Text = "Game mode".L10N("Client:Main:GameModeValueText");

            lblSavedGameTime = new XNALabel(WindowManager);
            lblSavedGameTime.Name = nameof(lblSavedGameTime);
            lblSavedGameTime.ClientRectangle = new Rectangle(lblMapName.X,
                panelPlayers.Bottom - 40, 0, 0);
            lblSavedGameTime.FontIndex = 1;
            lblSavedGameTime.Text = "SAVED GAME:".L10N("Client:Main:SavedGame");

            ddSavedGame = new XNAClientDropDown(WindowManager);
            ddSavedGame.Name = nameof(ddSavedGame);
            ddSavedGame.ClientRectangle = new Rectangle(lblSavedGameTime.X,
                panelPlayers.Bottom - 21,
                Width - lblSavedGameTime.X - 12, 21);
            ddSavedGame.SelectedIndexChanged += (_, _) => DdSavedGame_SelectedIndexChangedAsync().HandleTask();

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = nameof(lbChatMessages);
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.ClientRectangle = new Rectangle(12, panelPlayers.Bottom + 12,
                Width - 24,
                Height - panelPlayers.Bottom - 12 - 29 - 34);

            tbChatInput = new XNATextBox(WindowManager);
            tbChatInput.Name = nameof(tbChatInput);
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                lbChatMessages.Bottom + 3, lbChatMessages.Width, 19);
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += (_, _) => TbChatInput_EnterPressedAsync().HandleTask();

            btnLoadGame = new XNAClientButton(WindowManager);
            btnLoadGame.Name = nameof(btnLoadGame);
            btnLoadGame.ClientRectangle = new Rectangle(lbChatMessages.X,
                tbChatInput.Bottom + 6, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLoadGame.Text = "Load Game".L10N("Client:Main:LoadGame");
            btnLoadGame.LeftClick += (_, _) => BtnLoadGame_LeftClickAsync().HandleTask();

            btnLeaveGame = new XNAClientButton(WindowManager);
            btnLeaveGame.Name = nameof(btnLeaveGame);
            btnLeaveGame.ClientRectangle = new Rectangle(Width - 145,
                btnLoadGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLeaveGame.Text = "Leave Game".L10N("Client:Main:LeaveGame");
            btnLeaveGame.LeftClick += (_, _) => LeaveGameAsync().HandleTask();

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

            sndJoinSound = new EnhancedSoundEffect("joingame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyJoinCooldown);
            sndLeaveSound = new EnhancedSoundEffect("leavegame.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyLeaveCooldown);
            sndMessageSound = new EnhancedSoundEffect("message.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundMessageCooldown);
            sndGetReadySound = new EnhancedSoundEffect("getready.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundGameLobbyGetReadyCooldown);

            MPColors = MultiplayerColor.LoadColors();

            WindowManager.CenterControlOnScreen(this);

            if (SavedGameManager.AreSavedGamesAvailable())
            {
                fsw = new FileSystemWatcher(SafePath.CombineDirectoryPath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAMES_DIRECTORY), "*.NET");
                fsw.EnableRaisingEvents = false;
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Created;
            }
        }

        /// <summary>
        /// Updates Discord Rich Presence with actual information.
        /// </summary>
        /// <param name="resetTimer">Whether to restart the "Elapsed" timer or not</param>
        protected abstract void UpdateDiscordPresence(bool resetTimer = false);

        /// <summary>
        /// Resets Discord Rich Presence to default state.
        /// </summary>
        private void ResetDiscordPresence() => discordHandler.UpdatePresence();

        protected virtual ValueTask LeaveGameAsync()
        {
            GameLeft?.Invoke(this, EventArgs.Empty);
            ResetDiscordPresence();

            return ValueTask.CompletedTask;
        }

        private void fsw_Created(object sender, FileSystemEventArgs e) =>
            AddCallback(() => HandleFSWEventAsync(e).HandleTask());

        private static async ValueTask HandleFSWEventAsync(FileSystemEventArgs e)
        {
            Logger.Log("FSW Event: " + e.FullPath);

            if (Path.GetFileName(e.FullPath) == "SAVEGAME.NET")
                await SavedGameManager.RenameSavedGameAsync().ConfigureAwait(false);
        }

        private async ValueTask BtnLoadGame_LeftClickAsync()
        {
            if (!IsHost)
            {
                await RequestReadyStatusAsync().ConfigureAwait(false);
                return;
            }

            if (Players.Find(p => !p.Ready) != null)
            {
                await GetReadyNotificationAsync().ConfigureAwait(false);
                return;
            }

            if (Players.Count != SGPlayers.Count)
            {
                await NotAllPresentNotificationAsync().ConfigureAwait(false);
                return;
            }

            await HostStartGameAsync().ConfigureAwait(false);
        }

        protected abstract ValueTask RequestReadyStatusAsync();

        protected virtual ValueTask GetReadyNotificationAsync()
        {
            AddNotice("The game host wants to load the game but cannot because not all players are ready!".L10N("Client:Main:GetReadyPlease"));

            if (!IsHost && !Players.Find(p => p.Name == ProgramConstants.PLAYERNAME).Ready)
                sndGetReadySound.Play();
#if WINFORMS

            WindowManager.FlashWindow();
#endif
            return ValueTask.CompletedTask;
        }

        protected virtual ValueTask NotAllPresentNotificationAsync()
        {
            AddNotice("You cannot load the game before all players are present.".L10N("Client:Main:NotAllPresent"));
            return ValueTask.CompletedTask;
        }

        protected abstract ValueTask HostStartGameAsync();

        protected async ValueTask LoadGameAsync()
        {
            FileInfo spawnFileInfo = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

            spawnFileInfo.Delete();

            File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI), spawnFileInfo.FullName);

            IniFile spawnIni = new IniFile(spawnFileInfo.FullName);

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

                spawnIni.SetStringValue("Other" + i, "Ip", otherPlayer.IPAddress.ToString());
                spawnIni.SetIntValue("Other" + i, "Port", otherPlayer.Port);
            }

            WriteSpawnIniAdditions(spawnIni);
            spawnIni.WriteIniFile();

            FileInfo spawnMapFileInfo = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNMAP_INI);

            spawnMapFileInfo.Delete();
            var spawnMapStreamWriter = new StreamWriter(spawnMapFileInfo.FullName);

            await using (spawnMapStreamWriter.ConfigureAwait(false))
            {
                await spawnMapStreamWriter.WriteLineAsync("[Map]").ConfigureAwait(false);
                await spawnMapStreamWriter.WriteLineAsync("Size=0,0,50,50").ConfigureAwait(false);
                await spawnMapStreamWriter.WriteLineAsync("LocalSize=0,0,50,50").ConfigureAwait(false);
                await spawnMapStreamWriter.WriteLineAsync().ConfigureAwait(false);
            }

            gameLoadTime = DateTime.Now;

            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;
            await GameProcessLogic.StartGameProcessAsync(WindowManager).ConfigureAwait(false);

            fsw.EnableRaisingEvents = true;
            UpdateDiscordPresence(true);
        }

        private void SharedUILogic_GameProcessExited() => AddCallback(() => HandleGameProcessExitedAsync().HandleTask());

        protected virtual async ValueTask HandleGameProcessExitedAsync()
        {
            fsw.EnableRaisingEvents = false;

            GameProcessLogic.GameProcessExited -= SharedUILogic_GameProcessExited;

            var matchStatistics = StatisticsManager.Instance.GetMatchWithGameID(uniqueGameId);

            if (matchStatistics != null)
            {
                int newLength = matchStatistics.LengthInSeconds +
                    (int)(DateTime.Now - gameLoadTime).TotalSeconds;

                await matchStatistics.ParseStatisticsAsync(ProgramConstants.GamePath, true).ConfigureAwait(false);

                matchStatistics.LengthInSeconds = newLength;

                await StatisticsManager.Instance.SaveDatabaseAsync().ConfigureAwait(false);
            }

            UpdateDiscordPresence(true);
        }

        protected virtual void WriteSpawnIniAdditions(IniFile spawnIni)
        {
            // Do nothing by default
        }

        protected void AddNotice(string notice) => AddNotice(notice, Color.White);

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
            btnLoadGame.Text = isHost ? "Load Game".L10N("Client:Main:ButtonLoadGame") : "I'm Ready".L10N("Client:Main:ButtonGetReady");

            IniFile spawnSGIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SAVED_GAME_SPAWN_INI));

            lblMapNameValue.Tag = spawnSGIni.GetStringValue("Settings", "UIMapName", string.Empty);
            lblMapNameValue.Text = ((string)lblGameModeValue.Tag).L10N($"INI:Maps:{spawnSGIni.GetStringValue("Settings", "MapID", string.Empty)}:Description");
            lblGameModeValue.Tag = spawnSGIni.GetStringValue("Settings", "UIGameMode", string.Empty);
            lblGameModeValue.Text = ((string)lblGameModeValue.Tag).L10N($"INI:GameModes:{(string)lblGameModeValue.Tag}:UIName");

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
                sgPlayer.Name = spawnSGIni.GetStringValue(sectionName, "Name", "Unknown player".L10N("Client:Main:UnknownPlayer"));
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
                    playerLabel.Text = sgPlayer.Name + " " + "(Not present)".L10N("Client:Main:NotPresentSuffix");
                    continue;
                }

                playerLabel.RemapColor = sgPlayer.ColorIndex > -1 ? MPColors[sgPlayer.ColorIndex].XnaColor
                    : Color.White;
                playerLabel.Text = pInfo.Ready ? sgPlayer.Name : sgPlayer.Name + " " + "(Not Ready)".L10N("Client:Main:NotReadySuffix");
            }
        }

        private async ValueTask DdSavedGame_SelectedIndexChangedAsync()
        {
            if (!IsHost)
                return;

            for (int i = 1; i < Players.Count; i++)
                Players[i].Ready = false;

            CopyPlayerDataToUI();

            if (!isSettingUp)
                await BroadcastOptionsAsync().ConfigureAwait(false);
            UpdateDiscordPresence();
        }

        private async ValueTask TbChatInput_EnterPressedAsync()
        {
            if (string.IsNullOrEmpty(tbChatInput.Text))
                return;

            await SendChatMessageAsync(tbChatInput.Text).ConfigureAwait(false);
            tbChatInput.Text = string.Empty;
        }

        /// <summary>
        /// Override in a derived class to broadcast player ready statuses and the selected
        /// saved game to players.
        /// </summary>
        protected abstract ValueTask BroadcastOptionsAsync();

        protected abstract ValueTask SendChatMessageAsync(string message);

        public override void Draw(GameTime gameTime)
        {
            Renderer.FillRectangle(new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY),
                new Color(0, 0, 0, 255));

            base.Draw(gameTime);
        }

        public void SwitchOn() => Enable();

        public void SwitchOff() => Disable();

        public abstract string GetSwitchName();
    }
}