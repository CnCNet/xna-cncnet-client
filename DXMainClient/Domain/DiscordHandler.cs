using ClientCore;
using DiscordRPC;
using DiscordRPC.Message;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System.Text.RegularExpressions;

namespace DTAClient.Domain
{
    /// <summary>
    /// A class for handling Discord integration.
    /// </summary>
    public class DiscordHandler : GameComponent
    {
        private DiscordRpcClient client;

        private RichPresence _currentPresence;

        /// <summary>
        /// RichPresence instance that is currently being displayed.
        /// </summary>
        public RichPresence CurrentPresence
        {
            get
            {
                return _currentPresence;
            }
            set
            {
                if (_currentPresence == null || !_currentPresence.Equals(PreviousPresence))
                {
                    PreviousPresence = _currentPresence;
                    _currentPresence = value;
                    client.SetPresence(_currentPresence);
                }
            }
        }

        /// <summary>
        /// RichPresence instance that was last displayed before the current one.
        /// </summary>
        public RichPresence PreviousPresence { get; private set; }

        /// <summary>
        /// Creates a new instance of Discord handler.
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        public DiscordHandler(WindowManager windowManager) : base(windowManager.Game)
        {
            windowManager.Game.Components.Add(this);
        }

        #region overrides

        public override void Initialize()
        {
            InitializeClient();
            UpdatePresence();

            if (UserINISettings.Instance.DiscordIntegration)
                Connect();

            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (client.IsInitialized)
                client.ClearPresence();

            client.Dispose();
            base.Dispose(disposing);
        }

        #endregion

        #region methods

        /// <summary>
        /// Initializes or reinitializes Discord RPC client object & event handlers.
        /// </summary>
        private void InitializeClient()
        {
            if (client != null && client.IsInitialized)
            {
                client.ClearPresence();
                client.Dispose();
            }

            client = new DiscordRpcClient(ClientConfiguration.Instance.DiscordAppId);
            client.OnReady += OnReady;
            client.OnClose += OnClose;
            client.OnError += OnError;
            client.OnConnectionEstablished += OnConnectionEstablished;
            client.OnConnectionFailed += OnConnectionFailed;
            client.OnPresenceUpdate += OnPresenceUpdate;
            client.OnSubscribe += OnSubscribe;
            client.OnUnsubscribe += OnUnsubscribe;

            if (CurrentPresence != null)
                client.SetPresence(CurrentPresence);
        }

        /// <summary>
        /// Connects to Discord.
        /// Does not do anything if the Discord RPC client has not been initialized or is already connected.
        /// </summary>
        public void Connect()
        {
            if (client == null || client != null && client.IsInitialized)
                return;

            bool success = client.Initialize();

            if (success)
                Logger.Log("DiscordHandler: Connected Discord RPC client.");
            else
                Logger.Log("DiscordHandler: Failed to connect Discord RPC client.");
        }

        /// <summary>
        /// Disconnects from Discord.
        /// Does not do anything if the Discord RPC client has not been initialized or is not connected.
        /// </summary>
        public void Disconnect()
        {
            if (client == null || !client.IsInitialized)
                return;

            // HACK warning
            // Currently DiscordRpcClient does not appear to have any way to reliably disconnect and reconnect using same client object.
            // Deinitialize does not appear to completely reset connection state & resources and any attempts to call Initialize afterwards will fail.
            // A hacky solution is to dispose current client object and create and initialize a new one.
            InitializeClient(); //client.Deinitialize();

            Logger.Log("DiscordHandler: Disconnected Discord RPC client.");
        }

        /// <summary>
        /// Updates Discord Rich Presence with default info.
        /// </summary>
        public void UpdatePresence()
        {
            CurrentPresence = new RichPresence()
            {
                Details = "In Client",
                Assets = new Assets()
                {
                    LargeImageKey = "logo"
                }
            };
        }

        /// <summary>
        /// Updates Discord Rich Presence with simple state and details info.
        /// </summary>
        public void UpdatePresence(string state, string details)
        {
            CurrentPresence = new RichPresence()
            {
                State = state,
                Details = details,
                Assets = new Assets()
                {
                    LargeImageKey = "logo"
                }
            };
        }

        /// <summary>
        /// Updates Discord Rich Presence with info from game lobbies.
        /// </summary>
        public void UpdatePresence(string map, string mode, string type, string state,
            int players, int maxPlayers, string side, string roomName,
            bool isHost = false, bool isPassworded = false,
            bool isLocked = false, bool resetTimer = false)
        {
            string sideKey = new Regex("[^a-zA-Z0-9]").Replace(side.ToLower(), "");
            string stateString = $"{state} [{players}/{maxPlayers}] • {roomName}";
            if (isHost)
                stateString += "👑";
            if (isPassworded)
                stateString += "🔑";
            if (isLocked)
                stateString += "🔒";
            CurrentPresence = new RichPresence()
            {
                State = stateString,
                Details = $"{type} • {map} • {mode}",
                Assets = new Assets()
                {
                    LargeImageKey = "logo",
                    SmallImageKey = sideKey,
                    SmallImageText = side
                },
                Timestamps = (client.CurrentPresence.HasTimestamps() && !resetTimer) ?
                    client.CurrentPresence.Timestamps : Timestamps.Now
            };
        }

        /// <summary>
        /// Updates Discord Rich Presence with info from game loading lobbies.
        /// </summary>
        public void UpdatePresence(string map, string mode, string type, string state,
            int players, int maxPlayers, string roomName,
            bool isHost = false, bool resetTimer = false)
        {
            string stateString = $"{state} [{players}/{maxPlayers}] • {roomName}";
            stateString += "💾";
            if (isHost)
                stateString += "👑";
            CurrentPresence = new RichPresence()
            {
                State = stateString,
                Details = $"{type} • {map} • {mode}",
                Assets = new Assets()
                {
                    LargeImageKey = "logo"
                },
                Timestamps = (client.CurrentPresence.HasTimestamps() && !resetTimer) ?
                    client.CurrentPresence.Timestamps : Timestamps.Now
            };
        }

        /// <summary>
        /// Updates Discord Rich Presence with info from skirmish "lobby".
        /// </summary>
        public void UpdatePresence(string map, string mode, string state, string side, bool resetTimer = false)
        {
            string sideKey = new Regex("[^a-zA-Z0-9]").Replace(side.ToLower(), "");
            CurrentPresence = new RichPresence()
            {
                State = $"{state}",
                Details = $"Skirmish • {map} • {mode}",
                Assets = new Assets()
                {
                    LargeImageKey = "logo",
                    SmallImageKey = sideKey,
                    SmallImageText = side
                },
                Timestamps = (client.CurrentPresence.HasTimestamps() && !resetTimer) ?
                    client.CurrentPresence.Timestamps : Timestamps.Now
            };
        }

        /// <summary>
        /// Updates Discord Rich Presence with info from campaign screen.
        /// </summary>
        public void UpdatePresence(string mission, string difficulty, string side, bool resetTimer = false)
        {
            string sideKey = new Regex("[^a-zA-Z0-9]").Replace(side.ToLower(), "");
            CurrentPresence = new RichPresence()
            {
                State = "Playing Mission",
                Details = $"{mission} • {difficulty}",
                Assets = new Assets()
                {
                    LargeImageKey = "logo",
                    SmallImageKey = sideKey,
                    SmallImageText = side
                },
                Timestamps = (client.CurrentPresence.HasTimestamps() && !resetTimer) ?
                    client.CurrentPresence.Timestamps : Timestamps.Now
            };
        }

        /// <summary>
        /// Updates Discord Rich Presence with info from game loading screen.
        /// </summary>
        public void UpdatePresence(string save, bool resetTimer = false)
        {
            CurrentPresence = new RichPresence()
            {
                State = "Playing Saved Game",
                Details = $"{save}",
                Assets = new Assets()
                {
                    LargeImageKey = "logo"
                },
                Timestamps = (client.CurrentPresence.HasTimestamps() && !resetTimer) ?
                    client.CurrentPresence.Timestamps : Timestamps.Now
            };
        }

        #endregion

        #region eventhandlers

        private void OnReady(object sender, ReadyMessage args)
        {
            Logger.Log($"Discord: Received Ready from user {args.User.Username}");
            client.SetPresence(CurrentPresence);
        }

        private void OnClose(object sender, CloseMessage args)
        {
            Logger.Log($"Discord: Lost Connection with client because of '{args.Reason}'");
        }

        private void OnError(object sender, ErrorMessage args)
        {
            Logger.Log($"Discord: Error occured. ({args.Code}) {args.Message}");
        }

        private void OnConnectionEstablished(object sender, ConnectionEstablishedMessage args)
        {
            Logger.Log($"Discord: Pipe Connection Established. Valid on pipe #{args.ConnectedPipe}");
        }

        private void OnConnectionFailed(object sender, ConnectionFailedMessage args)
        {
            Logger.Log($"Discord: Pipe Connection Failed. Could not connect to pipe #{args.FailedPipe}");
        }

        private void OnPresenceUpdate(object sender, PresenceMessage args)
        {
            Logger.Log($"Discord: Rich Presence Updated. State: {args.Presence?.State}; Details: {args.Presence?.Details}");
        }

        private void OnSubscribe(object sender, SubscribeMessage args)
        {
            Logger.Log($"Discord: Subscribed: {args.Event}");
        }

        private void OnUnsubscribe(object sender, UnsubscribeMessage args)
        {
            Logger.Log($"Discord: Unsubscribed: {args.Event}");
        }

        #endregion
    }
}
