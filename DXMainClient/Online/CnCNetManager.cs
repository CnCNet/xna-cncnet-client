using ClientCore;
using ClientCore.CnCNet5;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameCollection = DTAClient.domain.CnCNet.GameCollection;

namespace DTAClient.Online
{
    public class CnCNetManager : IConnectionManager
    {
        public delegate void UserListDelegate(string channelName, string[] userNames);

        public event EventHandler<ServerMessageEventArgs> WelcomeMessageReceived;
        public event EventHandler<ServerMessageEventArgs> GenericServerMessageReceived;
        public event EventHandler<UserAwayEventArgs> AwayMessageReceived;
        public event EventHandler<ChannelTopicEventArgs> ChannelTopicReceived;
        public event EventHandler<UserListEventArgs> UserListReceived;
        public event EventHandler<WhoEventArgs> WhoReplyReceived;
        public event EventHandler<ChannelEventArgs> ChannelFull;
        public event EventHandler<ChannelEventArgs> IncorrectChannelPassword;
        public event EventHandler<ChannelModeEventArgs> ChannelModesChanged;
        public event EventHandler<CTCPEventArgs> CTCPMessageReceived;
        public event EventHandler<KickEventArgs> UserKickedFromChannel;
        public event EventHandler<ChannelUserEventArgs> UserJoinedChannel;

        public event EventHandler<AttemptedServerEventArgs> AttemptedServerChanged;
        public event EventHandler ConnectAttemptFailed;
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        public event EventHandler ReconnectAttempt;
        public event EventHandler Disconnected;
        public event EventHandler Connected;

        public CnCNetManager(WindowManager wm)
        {
            connection = new Connection(this);
            gameCollection = new GameCollection();
            gameCollection.Initialize(wm.GraphicsDevice);

            this.wm = wm;

            cDefaultChatColor = AssetLoader.GetColorFromString(DomainController.Instance().GetDefaultChatColor());

            IRCChatColors = new IRCColor[]
            {
                new IRCColor("Default color", false, cDefaultChatColor, 0),
                new IRCColor("Default color #2", false, cDefaultChatColor, 1),
                new IRCColor("Light Blue", true, Color.LightBlue, 2),
                new IRCColor("Green", true, Color.ForestGreen, 3),
                new IRCColor("Dark Red", true, new Color(180, 0, 0, 255), 4),
                new IRCColor("Red", true, Color.Red, 5),
                new IRCColor("Purple", true, Color.MediumOrchid, 6),
                new IRCColor("Orange", true, Color.Orange, 7),
                new IRCColor("Yellow", true, Color.Yellow, 8),
                new IRCColor("Lime Green", true, Color.Lime, 9),
                new IRCColor("Turquoise", true, Color.Turquoise, 10),
                new IRCColor("Sky Blue", true, Color.LightSkyBlue, 11),
                new IRCColor("Blue", true, Color.RoyalBlue, 12),
                new IRCColor("Pink", true, Color.Fuchsia, 13),
                new IRCColor("Gray", true, Color.LightGray, 14),
                new IRCColor("Gray #2", false, Color.Gray, 15)
            };
        }

        public Channel MainChannel { get; set; }

        bool connected = false;

        /// <summary>
        /// Gets a value that determines whether the client is 
        /// currently connected to CnCNet.
        /// </summary>
        public bool IsConnected
        {
            get { return connected; }
        }

        Connection connection;

        List<Channel> Channels = new List<Channel>();
        List<HostedGame> HostedGames = new List<HostedGame>();

        GameCollection gameCollection;

        Color cDefaultChatColor;
        IRCColor[] IRCChatColors;

        WindowManager wm;

        bool disconnect = false;

        /// <summary>
        /// Factory method for creating a new channel.
        /// </summary>
        /// <param name="uiName">The user-interface name of the channel.</param>
        /// <param name="channelName">The name of the channel.</param>
        /// <param name="persistent">Determines whether the channel's information 
        /// should remain in memory even after a disconnect.</param>
        /// <param name="password">The password for the channel. Use null for none.</param>
        /// <returns>A channel.</returns>
        public Channel CreateChannel(string uiName, string channelName, 
            bool persistent, string password)
        {
            return new Channel(uiName, channelName, persistent, password, connection);
        }

        public Channel GetChannel(string channelName)
        {
            return Channels.Find(c => c.ChannelName == channelName);
        }

        public void AddChannel(Channel channel)
        {
            Channels.Add(channel);
        }

        public IRCColor[] GetIRCColors()
        {
            return IRCChatColors;
        }

        public GameCollection GetGameCollection()
        {
            return gameCollection;
        }

        public void LeaveFromChannel(Channel channel)
        {
            connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 10, "PART " + channel.ChannelName);

            if (!channel.Persistent)
                Channels.Remove(channel);
        }

        public void SetMainChannel(Channel channel)
        {
            MainChannel = channel;
        }

        public void SendCustomMessage(QueuedMessage qm)
        {
            connection.QueueMessage(qm);
        }

        public void OnAttemptedServerChanged(string serverName)
        {
            wm.AddCallback(new Delegates.StringDelegate(DoAttemptedServerChanged), serverName);
        }

        private void DoAttemptedServerChanged(string serverName)
        {
            MainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, "Attempting connection to " + serverName));
            AttemptedServerChanged?.Invoke(this, new AttemptedServerEventArgs(serverName));
        }

        public void OnAwayMessageReceived(string userName, string reason)
        {
            wm.AddCallback(new Delegates.DualStringDelegate(DoAwayMessageReceived), userName, reason);
        }

        private void DoAwayMessageReceived(string userName, string reason)
        {
            AwayMessageReceived?.Invoke(this, new UserAwayEventArgs(userName, reason));
        }

        public void OnChannelFull(string channelName)
        {
            wm.AddCallback(new Delegates.StringDelegate(DoChannelFull), channelName);
        }

        private void DoChannelFull(string channelName)
        {
            ChannelFull?.Invoke(this, new ChannelEventArgs(channelName));

            int gameIndex = HostedGames.FindIndex(c => c.ChannelName == channelName);
            string gameName = null;

            if (gameIndex > -1)
                gameName = HostedGames[gameIndex].ChannelName;

            string message;

            message = string.IsNullOrEmpty(gameName) ?
                "The selected game is full! " + channelName :
                string.Format("Cannot join game {0}; it is full!", gameName);

            MainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now,
                message));
        }

        public void OnChannelModesChanged(string userName, string channelName, string modeString)
        {
            wm.AddCallback(new Delegates.TripleStringDelegate(DoChannelModesChanged),
                userName, channelName, modeString);
        }

        private void DoChannelModesChanged(string userName, string channelName, string modeString)
        {
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.OnChannelModesChanged(userName, modeString);
        }

        public void OnChannelTopicReceived(string channelName, string topic)
        {
            //ChannelTopicReceived?.Invoke(this, new ChannelTopicEventArgs(channelName, topic));

            wm.AddCallback(new Delegates.DualStringDelegate(DoChannelTopicReceived), channelName, topic);
        }

        private void DoChannelTopicReceived(string channelName, string topic)
        {
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.Topic = topic;
        }

        public void OnChatMessageReceived(string receiver, string sender, string message)
        {
            wm.AddCallback(new Delegates.TripleStringDelegate(DoChatMessageReceived),
                receiver, sender, message);
        }

        private void DoChatMessageReceived(string receiver, string sender, string message)
        {
            Channel channel = Channels.Find(c => c.ChannelName == receiver);

            if (channel == null)
                return;

            Color foreColor;

            // Handle ACTION
            if (message.Contains("ACTION"))
            {
                message = message.Remove(0, 7);
                message = "====> " + sender + message;
                sender = String.Empty;

                // Replace Funky's game identifiers with real game names
                for (int i = 0; i < gameCollection.GetGameCount(); i++)
                    message = message.Replace("new " + gameCollection.GetGameIdentifierFromIndex(i) + " game",
                        "new " + gameCollection.GetFullGameNameFromIndex(i) + " game");

                foreColor = Color.White;
            }
            else
            {
                // Color parsing
                if (message.Contains(Convert.ToString((char)03)))
                {
                    if (message.Length < 3)
                    {
                        foreColor = cDefaultChatColor;
                    }
                    else
                    {
                        string colorString = message.Substring(1, 2);
                        message = message.Remove(0, 3);
                        int colorIndex = Conversions.IntFromString(colorString, -1);
                        // Try to parse message color info; if fails, use default color
                        if (colorIndex < IRCChatColors.Length && colorIndex > -1)
                            foreColor = IRCChatColors[colorIndex].XnaColor;
                        else
                            foreColor = cDefaultChatColor;
                    }
                }
                else
                    foreColor = cDefaultChatColor;
            }

            channel.AddMessage(new IRCMessage(sender, foreColor, DateTime.Now, message.Replace('\r', ' ')));
        }

        public void OnCTCPParsed(string channelName, string userName, string message)
        {
            wm.AddCallback(new Delegates.TripleStringDelegate(DoCTCPParsed),
                channelName, userName, message);
        }

        private void DoCTCPParsed(string channelName, string userName, string message)
        {
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.OnCTCPReceived(userName, message);

            //CTCPMessageReceived?.Invoke(this, new CTCPEventArgs(userName, channelName, message));
        }

        public void OnConnectAttemptFailed()
        {
            wm.AddCallback(new Action(DoConnectAttemptFailed), null);
        }

        private void DoConnectAttemptFailed()
        {
            ConnectAttemptFailed?.Invoke(this, EventArgs.Empty);

            MainChannel.AddMessage(new IRCMessage(null, Color.Red, DateTime.Now, "Connecting to CnCNet failed!"));
        }

        public void OnConnected()
        {
            wm.AddCallback(new Action(DoConnected), null);
        }

        private void DoConnected()
        {
            connected = true;
            Connected?.Invoke(this, EventArgs.Empty);
            MainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, "Connection to CnCNet established."));
        }

        /// <summary>
        /// Called when the connection has got cut non-intentionally.
        /// </summary>
        /// <param name="reason"></param>
        public void OnConnectionLost(string reason)
        {
            wm.AddCallback(new Delegates.StringDelegate(DoConnectionLost), reason);
        }

        private void DoConnectionLost(string reason)
        {
            ConnectionLost?.Invoke(this, new ConnectionLostEventArgs(reason));

            for (int i = 0; i < Channels.Count; i++)
            {
                if (!Channels[i].Persistent)
                {
                    Channels.RemoveAt(i);
                    i--;
                }
            }

            MainChannel.AddMessage(new IRCMessage(null, Color.Red, DateTime.Now, "Connection to CnCNet has been lost."));
            connected = false;
        }

        public void Disconnect()
        {
            connection.Disconnect();
            disconnect = true;
        }

        public void Connect()
        {
            disconnect = false;
            MainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, "Connecting to CnCNet..."));
            connection.ConnectAsync();
        }

        /// <summary>
        /// Called when the connection has been aborted intentionally.
        /// </summary>
        public void OnDisconnected()
        {
            wm.AddCallback(new Action(DoDisconnected), null);
        }

        private void DoDisconnected()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);

            for (int i = 0; i < Channels.Count; i++)
            {
                if (!Channels[i].Persistent)
                {
                    Channels.RemoveAt(i);
                    i--;
                }
            }

            MainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, "You have disconnected from CnCNet."));
            connected = false;
        }

        public void OnErrorReceived(string errorMessage)
        {
            Logger.Log("ERROR Received: " + errorMessage);
        }

        public void OnGenericServerMessageReceived(string message)
        {
            wm.AddCallback(new Delegates.StringDelegate(DoGenericServerMessageReceived), message);
        }

        private void DoGenericServerMessageReceived(string message)
        {
            MainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, message));
        }

        public void OnIncorrectChannelPassword(string channelName)
        {
            wm.AddCallback(new Delegates.StringDelegate(DoIncorrectChannelPassword), channelName);
        }

        private void DoIncorrectChannelPassword(string channelName)
        {
            MainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, "Incorrect password!"));
        }

        public void OnNoticeMessageParsed(string notice, string userName)
        {
            // TODO Parse as private message
        }

        public void OnPrivateMessageReceived(string sender, string message)
        {
            // TODO Parse as private message
        }

        public void OnReconnectAttempt()
        {
            wm.AddCallback(new Action(DoReconnectAttempt), null);
        }

        private void DoReconnectAttempt()
        {
            ReconnectAttempt?.Invoke(this, EventArgs.Empty);

            MainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, "Attempting to reconnect to CnCNet..."));

            connection.ConnectAsync();
        }

        public void OnUserJoinedChannel(string channelName, string userName, string userAddress)
        {
            wm.AddCallback(new Delegates.TripleStringDelegate(DoUserJoinedChannel),
                channelName, userName, userAddress);
        }

        private void DoUserJoinedChannel(string channelName, string userName, string userAddress)
        {
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            bool isAdmin = false;
            string name = userName;

            if (userName.StartsWith("@"))
            {
                isAdmin = true;
                name = userName.Remove(0, 1);
            }

            IRCUser user = new IRCUser();
            user.IsAdmin = isAdmin;
            user.Name = name;
            user.GameID = -1;

            // TODO Parse identifier and assign gameid
            //string identd = userAddress.Split('@')[0].Replace("~", "");

            channel.OnUserJoined(user);

            UserJoinedChannel?.Invoke(this, new ChannelUserEventArgs(channelName, userName));
        }

        public void OnUserKicked(string channelName, string userName)
        {
            wm.AddCallback(new Delegates.DualStringDelegate(DoUserKicked),
                channelName, userName);
        }

        private void DoUserKicked(string channelName, string userName)
        {
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.OnUserKicked(userName);

            if (userName == ProgramConstants.PLAYERNAME)
            {
                if (!channel.Persistent)
                    Channels.Remove(channel);

                channel.ClearUsers();
            }
        }

        public void OnUserLeftChannel(string channelName, string userName)
        {
            wm.AddCallback(new Delegates.DualStringDelegate(DoUserLeftChannel),
                channelName, userName);
        }

        private void DoUserLeftChannel(string channelName, string userName)
        {
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.OnUserLeft(userName);

            if (userName == ProgramConstants.PLAYERNAME && !channel.Persistent)
            {
                Channels.Remove(channel);
            }
        }

        public void OnUserListReceived(string channelName, string[] userList)
        {
            wm.AddCallback(new UserListDelegate(DoUserListReceived), 
                channelName, userList);
        }

        private void DoUserListReceived(string channelName, string[] userList)
        {
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.OnUserListReceived(userList);
        }

        public void OnUserQuitIRC(string userName)
        {
            wm.AddCallback(new Delegates.StringDelegate(DoUserQuitIRC), userName);
        }

        private void DoUserQuitIRC(string userName)
        {
            Channels.ForEach(ch => ch.OnUserQuitIRC(userName));
        }

        public void OnWelcomeMessageReceived(string message)
        {
            wm.AddCallback(new Delegates.StringDelegate(DoWelcomeMessageReceived), message);
        }

        private void DoWelcomeMessageReceived(string message)
        {
            Channels.ForEach(ch => ch.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, message)));

            WelcomeMessageReceived?.Invoke(this, new ServerMessageEventArgs(message));
        }

        public void OnWhoReplyReceived(string userName, string extraInfo)
        {
            wm.AddCallback(new Delegates.DualStringDelegate(DoWhoReplyReceived),
                userName, extraInfo);
        }

        private void DoWhoReplyReceived(string userName, string extraInfo)
        {
            WhoReplyReceived?.Invoke(this, new WhoEventArgs(userName, extraInfo));

            string[] eInfoParts = extraInfo.Split(' ');

            if (eInfoParts.Length < 3)
                return;

            string gameName = eInfoParts[2];

            int gameIndex = gameCollection.GetGameIndexFromInternalName(gameName);

            if (gameIndex == -1)
                return;

            Channels.ForEach(ch => ch.ApplyGameIndexForUser(userName, gameIndex));
        }

        public bool GetDisconnectStatus()
        {
            return disconnect;
        }
    }
}
