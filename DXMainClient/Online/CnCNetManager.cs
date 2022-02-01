using ClientCore;
using ClientCore.CnCNet5;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.Online.EventArguments;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online
{
    /// <summary>
    /// Acts as an interface between the CnCNet connection class
    /// and the user-interface's classes.
    /// </summary>
    public class CnCNetManager : IConnectionManager
    {
        // When implementing IConnectionManager functions, pay special attention
        // to thread-safety.
        // The functions in IConnectionManager are usually called from the networking
        // thread, so if they affect anything in the UI or affect data that the 
        // UI thread might be reading, use WindowManager.AddCallback to execute a function
        // on the UI thread instead of modifying the data or raising events directly.

        public delegate void UserListDelegate(string channelName, string[] userNames);

        public event EventHandler<ServerMessageEventArgs> WelcomeMessageReceived;
        public event EventHandler<UserAwayEventArgs> AwayMessageReceived;
        public event EventHandler<WhoEventArgs> WhoReplyReceived;
        public event EventHandler<CnCNetPrivateMessageEventArgs> PrivateMessageReceived;
        public event EventHandler<PrivateCTCPEventArgs> PrivateCTCPReceived;
        public event EventHandler<ChannelEventArgs> BannedFromChannel;

        public event EventHandler<AttemptedServerEventArgs> AttemptedServerChanged;
        public event EventHandler ConnectAttemptFailed;
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        public event EventHandler ReconnectAttempt;
        public event EventHandler Disconnected;
        public event EventHandler Connected;

        public event EventHandler<UserEventArgs> UserAdded;
        public event EventHandler<UserEventArgs> UserGameIndexUpdated;
        public event EventHandler<UserNameIndexEventArgs> UserRemoved;
        public event EventHandler MultipleUsersAdded;

        public CnCNetManager(WindowManager wm, GameCollection gc, CnCNetUserData cncNetUserData)
        {
            gameCollection = gc;
            this.cncNetUserData = cncNetUserData;
            connection = new Connection(this);

            this.wm = wm;

            cDefaultChatColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.DefaultChatColor);

            ircChatColors = new IRCColor[]
            {
                new IRCColor("Default color".L10N("UI:Main:ColorDefault"), false, cDefaultChatColor, 0),
                new IRCColor("Default color #2".L10N("UI:Main:ColorDefault2"), false, cDefaultChatColor, 1),
                new IRCColor("Light Blue".L10N("UI:Main:ColorLightBlue"), true, Color.LightBlue, 2),
                new IRCColor("Green".L10N("UI:Main:ColorForestGreen"), true, Color.ForestGreen, 3),
                new IRCColor("Dark Red".L10N("UI:Main:ColorDarkRed"), true, new Color(180, 0, 0, 255), 4),
                new IRCColor("Red".L10N("UI:Main:ColorRed"), true, Color.Red, 5),
                new IRCColor("Purple".L10N("UI:Main:ColorMediumOrchid"), true, Color.MediumOrchid, 6),
                new IRCColor("Orange".L10N("UI:Main:ColorOrange"), true, Color.Orange, 7),
                new IRCColor("Yellow".L10N("UI:Main:ColorYellow"), true, Color.Yellow, 8),
                new IRCColor("Lime Green".L10N("UI:Main:ColorLime"), true, Color.Lime, 9),
                new IRCColor("Turquoise".L10N("UI:Main:ColorTurquoise"), true, Color.Turquoise, 10),
                new IRCColor("Sky Blue".L10N("UI:Main:ColorLightSkyBlue"), true, Color.LightSkyBlue, 11),
                new IRCColor("Blue".L10N("UI:Main:ColorRoyalBlue"), true, Color.RoyalBlue, 12),
                new IRCColor("Pink".L10N("UI:Main:ColorFuchsia"), true, Color.Fuchsia, 13),
                new IRCColor("Gray".L10N("UI:Main:ColorLightGray"), true, Color.LightGray, 14),
                new IRCColor("Gray #2".L10N("UI:Main:ColorGray2"), false, Color.Gray, 15)
            };
        }

        public Channel MainChannel { get; private set; }

        private bool connected = false;

        /// <summary>
        /// Gets a value that determines whether the client is 
        /// currently connected to CnCNet.
        /// </summary>
        public bool IsConnected
        {
            get { return connected; }
        }

        public bool IsAttemptingConnection
        {
            get { return connection.AttemptingConnection; }
        }

        /// <summary>
        /// The list of all users that we can see on the IRC network.
        /// </summary>
        public List<IRCUser> UserList = new List<IRCUser>();

        private Connection connection;

        private List<Channel> channels = new List<Channel>();

        private GameCollection gameCollection;
        private readonly CnCNetUserData cncNetUserData;

        private Color cDefaultChatColor;
        private IRCColor[] ircChatColors;

        private WindowManager wm;

        private bool disconnect = false;

        public bool IsCnCNetInitialized()
        {
            return Connection.IsIdSet();
        }

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
            bool persistent, bool isChatChannel, string password)
        {
            return new Channel(uiName, channelName, persistent, isChatChannel, password, connection);
        }

        public void AddChannel(Channel channel)
        {
            if (FindChannel(channel.ChannelName) != null)
                throw new ArgumentException("The channel already exists!".L10N("UI:Main:ChannelExist"), "channel");

            channels.Add(channel);
        }

        public void RemoveChannel(Channel channel)
        {
            if (channel.Persistent)
                throw new ArgumentException("Persistent channels cannot be removed.".L10N("UI:Main:PersistentChannelRemove"), "channel");

            channels.Remove(channel);
        }

        public IRCColor[] GetIRCColors()
        {
            return ircChatColors;
        }

        public void LeaveFromChannel(Channel channel)
        {
            connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 10, "PART " + channel.ChannelName);

            if (!channel.Persistent)
                channels.Remove(channel);
        }

        public void SetMainChannel(Channel channel)
        {
            MainChannel = channel;
        }

        public void SendCustomMessage(QueuedMessage qm)
        {
            connection.QueueMessage(qm);
        }

        public void SendWhoIsMessage(string nick)
        {
            SendCustomMessage(new QueuedMessage($"WHOIS {nick}", QueuedMessageType.WHOIS_MESSAGE, 0));
        }

        public void OnAttemptedServerChanged(string serverName)
        {
            // AddCallback is necessary for thread-safety; OnAttemptedServerChanged
            // is called by the networking thread, and AddCallback schedules DoAttemptedServerChanged
            // to be executed on the main (UI) thread.
            wm.AddCallback(new Action<string>(DoAttemptedServerChanged), serverName);
        }

        private void DoAttemptedServerChanged(string serverName)
        {
            MainChannel.AddMessage(new ChatMessage(
                string.Format("Attempting connection to {0}".L10N("UI:Main:AttemptConnectToServer"), serverName)));
            AttemptedServerChanged?.Invoke(this, new AttemptedServerEventArgs(serverName));
        }

        public void OnAwayMessageReceived(string userName, string reason)
        {
            wm.AddCallback(new Action<string, string>(DoAwayMessageReceived), userName, reason);
        }

        private void DoAwayMessageReceived(string userName, string reason)
        {
            AwayMessageReceived?.Invoke(this, new UserAwayEventArgs(userName, reason));
        }

        public void OnChannelFull(string channelName)
        {
            wm.AddCallback(new Action<string>(DoChannelFull), channelName);
        }

        private void DoChannelFull(string channelName)
        {
            var channel = FindChannel(channelName);

            if (channel != null)
                channel.OnChannelFull();
        }

        public void OnTargetChangeTooFast(string channelName, string message)
        {
            wm.AddCallback(new Action<string, string>(DoTargetChangeTooFast), channelName, message);
        }

        private void DoTargetChangeTooFast(string channelName, string message)
        {
            var channel = FindChannel(channelName);

            if (channel != null)
                channel.OnTargetChangeTooFast(message);
        }

        public void OnChannelInviteOnly(string channelName)
        {
            wm.AddCallback(new Action<string>(DoChannelInviteOnly), channelName);
        }

        private void DoChannelInviteOnly(string channelName)
        {
            var channel = FindChannel(channelName);

            if (channel != null)
                channel.OnInviteOnlyOnJoin();
        }

        public void OnChannelModesChanged(string userName, string channelName, string modeString, List<string> modeParameters)
        {
            wm.AddCallback(new Action<string, string, string, List<string>>(DoChannelModesChanged),
                userName, channelName, modeString, modeParameters);
        }

        private void DoChannelModesChanged(string userName, string channelName, string modeString, List<string> modeParameters)
        {
            Channel channel = FindChannel(channelName);

            if (channel == null)
                return;

            ApplyChannelModes(channel, modeString, modeParameters);

            channel.OnChannelModesChanged(userName, modeString);
        }

        private void ApplyChannelModes(Channel channel, string modeString, List<string> modeParameters)
        {
            bool addMode = true;
            int parameterCount = 0;
            foreach (char modeChar in modeString)
            {
                if (modeChar == '+')
                    addMode = true;
                else if (modeChar == '-')
                    addMode = false;
                else
                {
                    switch (modeChar)
                    {
                        // Add/remove channel operator status on user.
                        case 'o':
                            if (parameterCount >= modeParameters.Count)
                                break;
                            string parameter = modeParameters[parameterCount++];
                            ChannelUser user = channel.Users.Find(parameter);
                            if (user == null)
                                break;
                            user.IsAdmin = addMode;
                            break;
                    }
                }
            }
        }

        public void OnChannelTopicReceived(string channelName, string topic)
        {
            wm.AddCallback(new Action<string, string>(DoChannelTopicReceived), channelName, topic);
        }

        private void DoChannelTopicReceived(string channelName, string topic)
        {
            Channel channel = FindChannel(channelName);

            if (channel == null)
                return;

            channel.Topic = topic;
        }

        public void OnChannelTopicChanged(string userName, string channelName, string topic)
        {
            wm.AddCallback(new Action<string, string>(DoChannelTopicReceived), channelName, topic);
        }

        public void OnChatMessageReceived(string receiver, string senderName, string ident, string message)
        {
            wm.AddCallback(new Action<string, string, string, string>(DoChatMessageReceived),
                receiver, senderName, ident, message);
        }

        private void DoChatMessageReceived(string receiver, string senderName, string ident, string message)
        {
            Channel channel = FindChannel(receiver);

            if (channel == null)
                return;

            Color foreColor;

            // Handle ACTION
            if (message.Contains("ACTION"))
            {
                message = message.Remove(0, 7);
                message = "====> " + senderName + " " + message;
                senderName = String.Empty;

                // Replace Funky's game identifiers with real game names
                for (int i = 0; i < gameCollection.GameList.Count; i++)
                    // TODO localize this or not?
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
                        if (colorIndex < ircChatColors.Length && colorIndex > -1)
                            foreColor = ircChatColors[colorIndex].XnaColor;
                        else
                            foreColor = cDefaultChatColor;
                    }
                }
                else
                    foreColor = cDefaultChatColor;
            }

            if (message.Length > 1 && message[message.Length - 1] == '\u001f')
                message = message.Remove(message.Length - 1);

            ChannelUser user = channel.Users.Find(senderName);
            bool senderIsAdmin = user != null && user.IsAdmin;

            channel.AddMessage(new ChatMessage(senderName, ident, senderIsAdmin, foreColor, DateTime.Now, message.Replace('\r', ' ')));
        }

        public void OnCTCPParsed(string channelName, string userName, string message)
        {
            wm.AddCallback(new Action<string, string, string>(DoCTCPParsed),
                channelName, userName, message);
        }

        private void DoCTCPParsed(string channelName, string userName, string message)
        {
            Channel channel = FindChannel(channelName);

            // it's possible that we received this CTCP via PRIVMSG, in which case we
            // expect our username instead of a channel as the first parameter
            if (channel == null)
            {
                if (channelName == ProgramConstants.PLAYERNAME)
                {
                    PrivateCTCPEventArgs e = new PrivateCTCPEventArgs(userName, message);

                    PrivateCTCPReceived?.Invoke(this, e);
                }

                return;
            }

            channel.OnCTCPReceived(userName, message);
        }

        public void OnConnectAttemptFailed()
        {
            wm.AddCallback(new Action(DoConnectAttemptFailed), null);
        }

        private void DoConnectAttemptFailed()
        {
            ConnectAttemptFailed?.Invoke(this, EventArgs.Empty);

            MainChannel.AddMessage(new ChatMessage(Color.Red, "Connecting to CnCNet failed!".L10N("UI:Main:ConnectToCncNetFailed")));
        }

        public void OnConnected()
        {
            wm.AddCallback(new Action(DoConnected), null);
        }

        private void DoConnected()
        {
            connected = true;
            Connected?.Invoke(this, EventArgs.Empty);
            MainChannel.AddMessage(new ChatMessage("Connection to CnCNet established.".L10N("UI:Main:ConnectToCncNetSuccess")));
        }

        /// <summary>
        /// Called when the connection has got cut un-intentionally.
        /// </summary>
        /// <param name="reason"></param>
        public void OnConnectionLost(string reason)
        {
            wm.AddCallback(new Action<string>(DoConnectionLost), reason);
        }

        private void DoConnectionLost(string reason)
        {
            ConnectionLost?.Invoke(this, new ConnectionLostEventArgs(reason));

            for (int i = 0; i < channels.Count; i++)
            {
                if (!channels[i].Persistent)
                {
                    channels.RemoveAt(i);
                    i--;
                }
                else
                {
                    channels[i].ClearUsers();
                }
            }

            UserList.Clear();

            MainChannel.AddMessage(new ChatMessage(Color.Red, "Connection to CnCNet has been lost.".L10N("UI:Main:ConnectToCncNetHasLost")));
            connected = false;
        }

        /// <summary>
        /// Disconnects from CnCNet.
        /// </summary>
        public void Disconnect()
        {
            connection.Disconnect();
            disconnect = true;
        }

        /// <summary>
        /// Connects to CnCNet.
        /// </summary>
        public void Connect()
        {
            disconnect = false;
            MainChannel.AddMessage(new ChatMessage("Connecting to CnCNet...".L10N("UI:Main:ConnectingToCncNet")));
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
            for (int i = 0; i < channels.Count; i++)
            {
                if (!channels[i].Persistent)
                {
                    channels.RemoveAt(i);
                    i--;
                }
                else
                {
                    channels[i].ClearUsers();
                }
            }

            MainChannel.AddMessage(new ChatMessage("You have disconnected from CnCNet.".L10N("UI:Main:CncNetDisconnected")));
            connected = false;

            UserList.Clear();

            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public void OnErrorReceived(string errorMessage)
        {
            MainChannel.AddMessage(new ChatMessage(Color.Red, errorMessage));
        }

        public void OnGenericServerMessageReceived(string message)
        {
            wm.AddCallback(new Action<string>(DoGenericServerMessageReceived), message);
        }

        private void DoGenericServerMessageReceived(string message)
        {
            MainChannel.AddMessage(new ChatMessage(message));
        }

        public void OnIncorrectChannelPassword(string channelName)
        {
            wm.AddCallback(new Action<string>(DoIncorrectChannelPassword), channelName);
        }

        private void DoIncorrectChannelPassword(string channelName)
        {
            var channel = FindChannel(channelName);
            if (channel != null)
                channel.OnInvalidJoinPassword();
        }

        public void OnNoticeMessageParsed(string notice, string userName)
        {
            // TODO Parse as private message
        }

        public void OnPrivateMessageReceived(string sender, string message)
        {
            wm.AddCallback(new Action<string, string>(DoPrivateMessageReceived),
                sender, message);
        }

        private void DoPrivateMessageReceived(string sender, string message)
        {
            CnCNetPrivateMessageEventArgs e = new CnCNetPrivateMessageEventArgs(sender, message);

            PrivateMessageReceived?.Invoke(this, e);
        }

        public void OnReconnectAttempt()
        {
            wm.AddCallback(new Action(DoReconnectAttempt), null);
        }

        private void DoReconnectAttempt()
        {
            ReconnectAttempt?.Invoke(this, EventArgs.Empty);

            MainChannel.AddMessage(new ChatMessage("Attempting to reconnect to CnCNet...".L10N("UI:Main:ReconnectingCncNet")));

            connection.ConnectAsync();
        }

        public void OnUserJoinedChannel(string channelName, string host, string userName, string ident)
        {
            wm.AddCallback(new Action<string, string, string, string>(DoUserJoinedChannel),
                channelName, host, userName, ident);
        }

        private void DoUserJoinedChannel(string channelName, string host, string userName, string userAddress)
        {
            Channel channel = FindChannel(channelName);

            if (channel == null)
                return;

            bool isAdmin = false;
            string name = userName;

            if (userName.StartsWith("@"))
            {
                isAdmin = true;
                name = userName.Remove(0, 1);
            }

            IRCUser ircUser = null;

            // Check if we already know this user from another channel
            // Avoid LINQ here for performance reasons
            foreach (var user in UserList)
            {
                if (user.Name == name)
                {
                    ircUser = (IRCUser)user.Clone();
                    break;
                }
            }

            // If we don't know the user, create a new one
            if (ircUser == null)
            {
                string identifier = userAddress.Split('@')[0];
                string[] parts = identifier.Split('.');
                ircUser = new IRCUser(name, identifier, host);

                if (parts.Length > 1)
                {
                    ircUser.GameID = gameCollection.GameList.FindIndex(g => g.InternalName.ToUpper() == parts[0].Replace("~", string.Empty));
                }

                AddUserToGlobalUserList(ircUser);
            }

            var channelUser = new ChannelUser(ircUser);
            channelUser.IsAdmin = isAdmin;
            channelUser.IsFriend = cncNetUserData.IsFriend(channelUser.IRCUser.Name);

            ircUser.Channels.Add(channelName);
            channel.OnUserJoined(channelUser);

            //UserJoinedChannel?.Invoke(this, new ChannelUserEventArgs(channelName, userName));
        }

        private void AddUserToGlobalUserList(IRCUser user)
        {
            UserList.Add(user);
            UserList = UserList.OrderBy(u => u.Name).ToList();
            UserAdded?.Invoke(this, new UserEventArgs(user));
        }

        public void OnUserKicked(string channelName, string userName)
        {
            wm.AddCallback(new Action<string, string>(DoUserKicked),
                channelName, userName);
        }

        private void DoUserKicked(string channelName, string userName)
        {
            Channel channel = FindChannel(channelName);

            if (channel == null)
                return;

            channel.OnUserKicked(userName);

            if (userName == ProgramConstants.PLAYERNAME)
            {
                channel.Users.DoForAllUsers(user =>
                {
                    RemoveChannelFromUser(user.IRCUser.Name, channelName);
                });

                if (!channel.Persistent)
                    channels.Remove(channel);

                channel.ClearUsers();
                return;
            }

            RemoveChannelFromUser(userName, channelName);
        }

        public void OnUserLeftChannel(string channelName, string userName)
        {
            wm.AddCallback(new Action<string, string>(DoUserLeftChannel),
                channelName, userName);
        }

        private void DoUserLeftChannel(string channelName, string userName)
        {
            Channel channel = FindChannel(channelName);

            if (channel == null)
                return;

            channel.OnUserLeft(userName);

            if (userName == ProgramConstants.PLAYERNAME)
            {
                channel.Users.DoForAllUsers(user =>
                {
                    RemoveChannelFromUser(user.IRCUser.Name, channelName);
                });

                if (!channel.Persistent)
                    channels.Remove(channel);

                channel.ClearUsers();

                return;
            }

            RemoveChannelFromUser(userName, channelName);
        }

        /// <summary>
        /// Looks up an user in the global user list and removes a channel from the user.
        /// If the user is left with 0 channels (meaning we have no common channel with the user),
        /// the user is removed from the global user list.
        /// </summary>
        /// <param name="userName">The name of the user.</param>
        /// <param name="channelName">The name of the channel.</param>
        public void RemoveChannelFromUser(string userName, string channelName)
        {
            var userIndex = UserList.FindIndex(user => user.Name.ToLower() == userName.ToLower());
            if (userIndex > -1)
            {
                var ircUser = UserList[userIndex];
                ircUser.Channels.Remove(channelName);

                if (ircUser.Channels.Count == 0)
                {
                    UserList.RemoveAt(userIndex);
                    UserRemoved?.Invoke(this, new UserNameIndexEventArgs(userIndex, userName));
                }
            }
        }

        public void OnUserListReceived(string channelName, string[] userList)
        {
            wm.AddCallback(new UserListDelegate(DoUserListReceived),
                channelName, userList);
        }

        private void DoUserListReceived(string channelName, string[] userList)
        {
            Channel channel = FindChannel(channelName);

            if (channel == null)
                return;

            var channelUserList = new List<ChannelUser>();

            foreach (string userName in userList)
            {
                string name = userName;
                bool isAdmin = false;

                if (userName.StartsWith("@"))
                {
                    isAdmin = true;
                    name = userName.Substring(1);
                }
                else if (userName.StartsWith("+"))
                    name = userName.Substring(1);

                // Check if we already know the IRC user from another channel
                IRCUser ircUser = UserList.Find(u => u.Name == name);

                // If the user isn't familiar to us already,
                // create a new user instance and add it to the global user list
                if (ircUser == null)
                {
                    ircUser = new IRCUser(name);
                    UserList.Add(ircUser);
                }

                var channelUser = new ChannelUser(ircUser);
                channelUser.IsAdmin = isAdmin;
                channelUser.IsFriend = cncNetUserData.IsFriend(channelUser.IRCUser.Name);

                channelUserList.Add(channelUser);
            }

            UserList = UserList.OrderBy(u => u.Name).ToList();
            MultipleUsersAdded?.Invoke(this, EventArgs.Empty);

            channel.OnUserListReceived(channelUserList);
        }

        public void OnUserQuitIRC(string userName)
        {
            wm.AddCallback(new Action<string>(DoUserQuitIRC), userName);
        }

        private void DoUserQuitIRC(string userName)
        {
            new List<Channel>(channels).ForEach(ch => ch.OnUserQuitIRC(userName));

            int userIndex = UserList.FindIndex(user => user.Name == userName);

            if (userIndex > -1)
            {
                UserList.RemoveAt(userIndex);
                UserRemoved?.Invoke(this, new UserNameIndexEventArgs(userIndex, userName));
            }
        }

        public void OnWelcomeMessageReceived(string message)
        {
            wm.AddCallback(new Action<string>(DoWelcomeMessageReceived), message);
        }


        /// <summary>
        /// Finds a channel with the specified internal name, case-insensitively.
        /// </summary>
        /// <param name="channelName">The internal name of the channel.</param>
        /// <returns>A channel if one matching the name is found, otherwise null.</returns>
        public Channel FindChannel(string channelName)
        {
            channelName = channelName.ToLower();

            foreach (var channel in channels)
            {
                if (channel.ChannelName.ToLower() == channelName)
                    return channel;
            }

            return null;
        }

        private void DoWelcomeMessageReceived(string message)
        {
            channels.ForEach(ch => ch.AddMessage(new ChatMessage(message)));

            WelcomeMessageReceived?.Invoke(this, new ServerMessageEventArgs(message));
        }

        public void OnWhoReplyReceived(string ident, string hostName, string userName, string extraInfo)
        {
            wm.AddCallback(new Action<string, string, string, string>(DoWhoReplyReceived),
                ident, hostName, userName, extraInfo);
        }

        private void DoWhoReplyReceived(string ident, string hostName, string userName, string extraInfo)
        {
            WhoReplyReceived?.Invoke(this, new WhoEventArgs(ident, userName, extraInfo));

            string[] eInfoParts = extraInfo.Split(' ');

            int gameIndex = -1;
            if (eInfoParts.Length > 2)
            {
                string gameName = eInfoParts[2];

                gameIndex = gameCollection.GetGameIndexFromInternalName(gameName);

                if (gameIndex == -1)
                    return;
            }

            var user = UserList.Find(u => u.Name == userName);
            if (user != null)
            {
                user.GameID = gameIndex;
                user.Ident = ident;
                user.Hostname = hostName;

                if (gameIndex != -1)
                {
                    channels.ForEach(ch => ch.UpdateGameIndexForUser(userName));
                    UserGameIndexUpdated?.Invoke(this, new UserEventArgs(user));
                }
            }
        }

        public bool GetDisconnectStatus()
        {
            return disconnect;
        }

        public void OnNameAlreadyInUse()
        {
            wm.AddCallback(new Action(DoNameAlreadyInUse), null);
        }

        /// <summary>
        /// Handles situations when the requested name is already in use by another
        /// IRC user. Adds additional underscores to the name or replaces existing
        /// characters with underscores.
        /// </summary>
        private void DoNameAlreadyInUse()
        {
            var charList = ProgramConstants.PLAYERNAME.ToList();
            int maxNameLength = ClientConfiguration.Instance.MaxNameLength;

            if (charList.Count < maxNameLength)
                charList.Add('_');
            else
            {
                int lastNonUnderscoreIndex = charList.FindLastIndex(c => c != '_');

                if (lastNonUnderscoreIndex == -1)
                {
                    MainChannel.AddMessage(new ChatMessage(Color.White,
                        "Your nickname is invalid or already in use. Please change your nickname in the login screen.".L10N("UI:Main:PickAnotherNickName")));
                    UserINISettings.Instance.SkipConnectDialog.Value = false;
                    Disconnect();
                    return;
                }

                charList[lastNonUnderscoreIndex] = '_';
            }

            var sb = new StringBuilder();
            foreach (char c in charList)
                sb.Append(c);

            MainChannel.AddMessage(new ChatMessage(Color.White,
                string.Format("Your name is already in use. Retrying with {0}...".L10N("UI:Main:NameInUseRetry"), sb.ToString())));

            ProgramConstants.PLAYERNAME = sb.ToString();
            connection.ChangeNickname();
        }

        public void OnBannedFromChannel(string channelName)
        {
            wm.AddCallback(new Action<string>(DoBannedFromChannel), channelName);
        }

        private void DoBannedFromChannel(string channelName)
        {
            BannedFromChannel?.Invoke(this, new ChannelEventArgs(channelName));
        }

        public void OnUserNicknameChange(string oldNickname, string newNickname)
            => wm.AddCallback(new Action<string, string>(DoUserNicknameChange), oldNickname, newNickname);

        private void DoUserNicknameChange(string oldNickname, string newNickname)
        {
            IRCUser user = UserList.Find(u => u.Name.ToUpper() == oldNickname.ToUpper());
            if (user == null)
            {
                Logger.Log("DoUserNicknameChange: Failed to find user with nickname " + oldNickname);
                return;
            }
            string realOldNickname = user.Name; // To make sure that case matches
            user.Name = newNickname;

            channels.ForEach(ch => ch.OnUserNameChanged(realOldNickname, newNickname));
        }
    }

    public class UserEventArgs : EventArgs
    {
        public UserEventArgs(IRCUser ircUser)
        {
            User = ircUser;
        }

        public IRCUser User { get; private set; }
    }

    public class IndexEventArgs : EventArgs
    {
        public IndexEventArgs(int index)
        {
            Index = index;
        }

        public int Index { get; private set; }
    }

    public class UserNameChangedEventArgs : EventArgs
    {
        public UserNameChangedEventArgs(string oldUserName, IRCUser user)
        {
            OldUserName = oldUserName;
            User = user;
        }

        public string OldUserName { get; }
        public IRCUser User { get; }
    }
}
