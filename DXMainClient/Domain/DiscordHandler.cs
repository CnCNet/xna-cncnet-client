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
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class DiscordHandler : GameComponent
    {
        public DiscordRpcClient client;


        public DiscordHandler(WindowManager wm) : base(wm.Game)
        {
            this.wm = wm;

            wm.Game.Components.Add(this);

            Enabled = false;
        }

        WindowManager wm;

        // Overrides

        public override void Initialize()
        {
            client = new DiscordRpcClient(ClientConfiguration.Instance.DiscordAppId);

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

        public void SetPresence(string details, string state)
        {
            client.SetPresence(new RichPresence()
            {
                Assets = new Assets()
                {
                    LargeImageKey = "logo",
                    LargeImageText = "Logo"
                }
            });

            if (!string.IsNullOrEmpty(details))
                client.UpdateDetails(details);

            if (!string.IsNullOrEmpty(state))
                client.UpdateState(state);

        }


        // Event handlers

        private void OnReady(object sender, ReadyMessage args)
        {
            Enabled = true;
            SetPresence(null, "In client");
            Logger.Log("Discord: Received Ready from user {0}", args.User.Username);
        }
        private void OnClose(object sender, CloseMessage args)
        {
            Enabled = false;
            Logger.Log("Discord: Lost Connection with client because of '{0}'", args.Reason);
        }
        private void OnError(object sender, ErrorMessage args)
        {
            Logger.Log("Discord: Error occured. ({1}) {0}", args.Message, args.Code);
        }

        private void OnConnectionEstablished(object sender, ConnectionEstablishedMessage args)
        {
            Logger.Log("Discord: Pipe Connection Established. Valid on pipe #{0}", args.ConnectedPipe);
        }
        private void OnConnectionFailed(object sender, ConnectionFailedMessage args)
        {
            Logger.Log("Discord: Pipe Connection Failed. Could not connect to pipe #{0}", args.FailedPipe);
        }
        private void OnPresenceUpdate(object sender, PresenceMessage args)
        {
            Logger.Log("Discord: Rich Presence Updated. Playing {0}", args.Presence == null ? "Nothing (NULL)" : args.Presence.State);
        }

        private void OnSubscribe(object sender, SubscribeMessage args)
        {
            Logger.Log("Discord: Subscribed: {0}", args.Event);
        }
        private void OnUnsubscribe(object sender, UnsubscribeMessage args)
        {
            Logger.Log("Discord: Unsubscribed: {0}", args.Event);
        }

    }
}
