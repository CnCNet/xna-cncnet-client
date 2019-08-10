using ClientCore;
using DiscordRPC;
using DiscordRPC.Message;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DTAClient.Domain
{
    public class DiscordHandler : GameComponent
    {
        public DiscordRpcClient client;

        private RichPresence currentPresence;
        public RichPresence CurrentPresence
        {
            get
            {
                return currentPresence;
            }
            set
            {
                PreviousPresence = CurrentPresence;
                currentPresence = value;
                if (!currentPresence.Equals(PreviousPresence))
                    client.SetPresence(currentPresence);
            }
        }

        public RichPresence PreviousPresence { get; private set; }
        public DiscordHandler(WindowManager wm) : base(wm.Game)
        {
            this.wm = wm;

            wm.Game.Components.Add(this);
        }

        WindowManager wm;

        // Overrides

        public override void Initialize()
        {
            client = new DiscordRpcClient(ClientConfiguration.Instance.DiscordAppId);

            UpdatePresence();
            client.OnReady += OnReady;
            client.OnClose += OnClose;
            client.OnError += OnError;
            client.OnConnectionEstablished += OnConnectionEstablished;
            client.OnConnectionFailed += OnConnectionFailed;
            client.OnPresenceUpdate += OnPresenceUpdate;
            client.OnSubscribe += OnSubscribe;
            client.OnUnsubscribe += OnUnsubscribe;

            client.Initialize();
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            client.Invoke();
            base.Update(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            client.Dispose();
            base.Dispose(disposing);
        }

        // Methods

        public void UpdatePresence()
        {
            CurrentPresence = new RichPresence()
            {
                State = "Idle",
                Details = "In Client",
                Assets = new Assets()
                {
                    LargeImageKey = "logo"
                }
            };
        }
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
                Details = $"{map} • {mode} • {type}",
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


        public void UpdatePresence(string map, string mode, string state, string side, bool resetTimer = false)
        {
            string sideKey = new Regex("[^a-zA-Z0-9]").Replace(side.ToLower(), "");
            CurrentPresence = new RichPresence()
            {
                State = $"{state}",
                Details = $"{map} • {mode} • Skirmish",
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

        public void UpdatePresence(string mission, string difficulty, bool resetTimer = false)
        {
            CurrentPresence = new RichPresence()
            {
                State = "Playing Mission",
                Details = $"{mission} • {difficulty}",
                Assets = new Assets()
                {
                    LargeImageKey = "logo"
                },
                Timestamps = (client.CurrentPresence.HasTimestamps() && !resetTimer) ?
                    client.CurrentPresence.Timestamps : Timestamps.Now
            };
        }

        // Event handlers

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

    }
}
