using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.CnCNet5.Games;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online
{
    public class CnCNetManager : IConnectionManager
    {
        public EventHandler<ServerMessageEventArgs> WelcomeMessageReceived;
        public EventHandler<ServerMessageEventArgs> GenericServerMessageReceived;
        public EventHandler<UserAwayEventArgs> AwayMessageReceived;
        public EventHandler<ChannelTopicEventArgs> ChannelTopicReceived;
        public EventHandler<UserListEventArgs> UserListReceived;
        public EventHandler<WhoEventArgs> WhoReplyReceived;
        public EventHandler<ChannelEventArgs> ChannelFull;
        public EventHandler<ChannelEventArgs> IncorrectChannelPassword;
        public EventHandler<ChannelModeEventArgs> ChannelModesChanged;
        public EventHandler<CTCPEventArgs> CTCPMessageReceived;
        public EventHandler<KickEventArgs> UserKickedFromChannel;

        public event EventHandler<AttemptedServerEventArgs> AttemptedServerChanged;
        public event EventHandler ConnectAttemptFailed;
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        public event EventHandler ReconnectAttempt;
        public event EventHandler Disconnected;
        public event EventHandler Connected;

        public CnCNetManager()
        {
            connection = new Connection(this);
            gameCollection = new GameCollection();
            gameCollection.Initialize();
        }

        Connection connection;

        List<Channel> Channels = new List<Channel>();
        List<HostedGame> HostedGames = new List<HostedGame>();

        GameCollection gameCollection;

        Channel currentMainChannel;

        Color cDefaultChatColor;
        Color[] IRCChatColors;

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

        public void LeaveFromChannel(Channel channel)
        {
            connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 10, "PART " + channel.ChannelName);

            if (!channel.Persistent)
                Channels.Remove(channel);
        }

        public void SetMainChannel(string channelName)
        {
            currentMainChannel = Channels.Find(c => c.ChannelName == channelName);
        }

        public void OnAttemptedServerChanged(string serverName)
        {
            AttemptedServerChanged?.Invoke(this, new AttemptedServerEventArgs(serverName));
        }

        public void OnAwayMessageReceived(string userName, string reason)
        {
            AwayMessageReceived?.Invoke(this, new UserAwayEventArgs(userName, reason));
        }

        public void OnChannelFull(string channelName)
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

            currentMainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now,
                message));
        }

        public void OnChannelModesChanged(string userName, string channelName, string modeString)
        {
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.OnChannelModesChanged(userName, modeString);
        }

        public void OnChannelTopicReceived(string channelName, string topic)
        {
            //ChannelTopicReceived?.Invoke(this, new ChannelTopicEventArgs(channelName, topic));

            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.Topic = topic;
        }

        public void OnChatMessageReceived(string receiver, string sender, string message)
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
                            foreColor = IRCChatColors[colorIndex];
                        else
                            foreColor = cDefaultChatColor;
                    }
                }
                else
                    foreColor = cDefaultChatColor;
            }

            channel.AddMessage(new IRCMessage(sender, foreColor, DateTime.Now, message));
        }

        public void OnConnectAttemptFailed()
        {
            ConnectAttemptFailed?.Invoke(this, EventArgs.Empty);
        }

        public void OnConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        public void OnConnectionLost(string reason)
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
        }

        public void OnCTCPParsed(string channelName, string userName, string message)
        {
            CTCPMessageReceived?.Invoke(this, new CTCPEventArgs(userName, channelName, message));
        }

        public void OnDisconnected()
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
        }

        public void OnErrorReceived(string errorMessage)
        {
            Logger.Log("ERROR Received: " + errorMessage);
        }

        public void OnGenericServerMessageReceived(string message)
        {
            currentMainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, message));
        }

        public void OnIncorrectChannelPassword(string channelName)
        {
            currentMainChannel.AddMessage(new IRCMessage(null, Color.White, DateTime.Now, "Incorrect password!"));
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
            ReconnectAttempt?.Invoke(this, EventArgs.Empty);
            connection.ConnectAsync();
        }

        public void OnUserJoinedChannel(string channelName, string userName, string userAddress)
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
            user.GameID = 0;

            // TODO Parse identifier and assign gameid
            //string identd = userAddress.Split('@')[0].Replace("~", "");

            channel.AddUser(user);
        }

        public void OnUserKicked(string channelName, string userName)
        {
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.OnUserKicked(userName);

            if (userName == ProgramConstants.PLAYERNAME && !channel.Persistent)
            {
                Channels.Remove(channel);
            }
        }

        public void OnUserLeftChannel(string channelName, string userName)
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
            Channel channel = Channels.Find(c => c.ChannelName == channelName);

            if (channel == null)
                return;

            channel.OnUserListReceived(userList);
        }

        public void OnUserQuitIRC(string userName)
        {
            foreach (Channel channel in Channels)
                channel.OnUserQuitIRC(userName);
        }

        public void OnWelcomeMessageReceived(string message)
        {
            foreach (Channel channel in Channels)
                channel.Messages.Add(new IRCMessage(null, Color.White, DateTime.Now, message));

            WelcomeMessageReceived?.Invoke(this, new ServerMessageEventArgs(message));
        }

        public void OnWhoReplyReceived(string userName, string extraInfo)
        {
            WhoReplyReceived?.Invoke(this, new WhoEventArgs(userName, extraInfo));

            string[] eInfoParts = extraInfo.Split(' ');

            if (eInfoParts.Length < 3)
                return;

            string gameName = eInfoParts[2];

            int gameIndex = gameCollection.GetGameIndexFromInternalName(gameName);

            if (gameIndex == -1)
                return;

            foreach (Channel channel in Channels)
            {
                channel.ApplyGameIndexForUser(userName, gameIndex);
            }
        }
    }
}
