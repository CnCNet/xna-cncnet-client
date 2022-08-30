using ClientCore;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using DTAClient.DXGUI;
using Localization;

namespace DTAClient.Online
{
    public class Channel : IMessageView
    {
        const int MESSAGE_LIMIT = 1024;

        public event EventHandler<ChannelUserEventArgs> UserAdded;
        public event EventHandler<UserNameEventArgs> UserLeft;
        public event EventHandler<UserNameEventArgs> UserKicked;
        public event EventHandler<UserNameEventArgs> UserQuitIRC;
        public event EventHandler<ChannelUserEventArgs> UserGameIndexUpdated;
        public event EventHandler<UserNameChangedEventArgs> UserNameChanged;
        public event EventHandler UserListReceived;
        public event EventHandler UserListCleared;

        public event EventHandler<IRCMessageEventArgs> MessageAdded;
        public event EventHandler<ChannelModeEventArgs> ChannelModesChanged;
        public event EventHandler<ChannelCTCPEventArgs> CTCPReceived;
        public event EventHandler InvalidPasswordEntered;
        public event EventHandler InviteOnlyErrorOnJoin;

        /// <summary>
        /// Raised when the server informs the client that it's is unable to
        /// join the channel because it's full.
        /// </summary>
        public event EventHandler ChannelFull;

        /// <summary>
        /// Raised when the server informs the client that it's is unable to
        /// join the channel because the client has attempted to join too many
        /// channels too quickly.
        /// </summary>
        public event EventHandler<MessageEventArgs> TargetChangeTooFast;

        public Channel(string uiName, string channelName, bool persistent, bool isChatChannel, string password, Connection connection)
        {
            if (isChatChannel)
                users = new SortedUserCollection<ChannelUser>(ChannelUser.ChannelUserComparison);
            else
                users = new UnsortedUserCollection<ChannelUser>();

            UIName = uiName;
            ChannelName = channelName.ToLowerInvariant();
            Persistent = persistent;
            IsChatChannel = isChatChannel;
            Password = password;
            this.connection = connection;

            if (persistent)
            {
                Instance_SettingsSaved(null, EventArgs.Empty);
                UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;
            }
        }

        #region Public members

        public string UIName { get; }

        public string ChannelName { get; }

        public bool Persistent { get; }

        public bool IsChatChannel { get; }

        public string Password { get; private set; }

        private readonly Connection connection;

        string _topic;
        public string Topic
        {
            get { return _topic; }
            set
            {
                _topic = value;
                if (Persistent)
                    AddMessage(new ChatMessage(
                        string.Format("Topic for {0} is: {1}".L10N("UI:Main:ChannelTopic"), UIName, _topic)));
            }
        }

        List<ChatMessage> messages = new List<ChatMessage>();
        public List<ChatMessage> Messages => messages;

        IUserCollection<ChannelUser> users;
        public IUserCollection<ChannelUser> Users => users;

        #endregion

        bool notifyOnUserListChange = true;

        private void Instance_SettingsSaved(object sender, EventArgs e)
        {
#if YR
            notifyOnUserListChange = false;
#else
            notifyOnUserListChange = UserINISettings.Instance.NotifyOnUserListChange;
#endif
        }

        public void AddUser(ChannelUser user)
        {
            users.Add(user.IRCUser.Name, user);
            UserAdded?.Invoke(this, new ChannelUserEventArgs(user));
        }

        public void OnUserJoined(ChannelUser user)
        {
            AddUser(user);

            if (notifyOnUserListChange)
            {
                AddMessage(new ChatMessage(
                    string.Format("{0} has joined {1}.".L10N("UI:Main:PlayerJoinChannel"), user.IRCUser.Name, UIName)));
            }

#if !YR
            if (Persistent && IsChatChannel && user.IRCUser.Name == ProgramConstants.PLAYERNAME)
                RequestUserInfo();
#endif
        }

        public void OnUserListReceived(List<ChannelUser> userList)
        {
            for (int i = 0; i < userList.Count; i++)
            {
                ChannelUser user = userList[i];
                var existingUser = users.Find(user.IRCUser.Name);
                if (existingUser == null)
                {
                    users.Add(user.IRCUser.Name, user);
                }
                else if (IsChatChannel)
                {
                    if (existingUser.IsAdmin != user.IsAdmin)
                    {
                        existingUser.IsAdmin = user.IsAdmin;
                        existingUser.IsFriend = user.IsFriend;
                        users.Reinsert(user.IRCUser.Name);
                    }
                }
            }

            UserListReceived?.Invoke(this, EventArgs.Empty);
        }

        public void OnUserKicked(string userName)
        {
            if (users.Remove(userName))
            {
                if (userName == ProgramConstants.PLAYERNAME)
                {
                    users.Clear();
                }

                AddMessage(new ChatMessage(
                    string.Format("{0} has been kicked from {1}.".L10N("UI:Main:PlayerKickedFromChannel"), userName, UIName)));

                UserKicked?.Invoke(this, new UserNameEventArgs(userName));
            }
        }

        public void OnUserLeft(string userName)
        {
            if (users.Remove(userName))
            {
                if (notifyOnUserListChange)
                {
                    AddMessage(new ChatMessage(
                         string.Format("{0} has left from {1}.".L10N("UI:Main:PlayerLeftFromChannel"), userName, UIName)));
                }

                UserLeft?.Invoke(this, new UserNameEventArgs(userName));
            }
        }

        public void OnUserQuitIRC(string userName)
        {
            if (users.Remove(userName))
            {
                if (notifyOnUserListChange)
                {
                    AddMessage(new ChatMessage(
                        string.Format("{0} has quit from CnCNet.".L10N("UI:Main:PlayerQuitCncNet"), userName)));
                }

                UserQuitIRC?.Invoke(this, new UserNameEventArgs(userName));
            }
        }

        public void UpdateGameIndexForUser(string userName)
        {
            var user = users.Find(userName);
            if (user != null)
                UserGameIndexUpdated?.Invoke(this, new ChannelUserEventArgs(user));
        }

        public void OnUserNameChanged(string oldUserName, string newUserName)
        {
            var user = users.Find(oldUserName);
            if (user != null)
            {
                users.Remove(oldUserName);
                users.Add(newUserName, user);
                UserNameChanged?.Invoke(this, new UserNameChangedEventArgs(oldUserName, user.IRCUser));
            }
        }

        public void OnChannelModesChanged(string sender, string modes)
        {
            ChannelModesChanged?.Invoke(this, new ChannelModeEventArgs(sender, modes));
        }

        public void OnCTCPReceived(string userName, string message)
        {
            CTCPReceived?.Invoke(this, new ChannelCTCPEventArgs(userName, message));
        }

        public void OnInvalidJoinPassword()
        {
            InvalidPasswordEntered?.Invoke(this, EventArgs.Empty);
        }

        public void OnInviteOnlyOnJoin()
        {
            InviteOnlyErrorOnJoin?.Invoke(this, EventArgs.Empty);
        }

        public void OnChannelFull()
        {
            ChannelFull?.Invoke(this, EventArgs.Empty);
        }

        public void OnTargetChangeTooFast(string message)
        {
            TargetChangeTooFast?.Invoke(this, new MessageEventArgs(message));
        }

        public void AddMessage(ChatMessage message)
        {
            if (messages.Count == MESSAGE_LIMIT)
                messages.RemoveAt(0);

            messages.Add(message);

            MessageAdded?.Invoke(this, new IRCMessageEventArgs(message));
        }

        public void SendChatMessage(string message, IRCColor color)
        {
            AddMessage(new ChatMessage(ProgramConstants.PLAYERNAME, color.XnaColor, DateTime.Now, message));

            string colorString = ((char)03).ToString() + color.IrcColorId.ToString("D2");

            connection.QueueMessage(QueuedMessageType.CHAT_MESSAGE, 0,
                "PRIVMSG " + ChannelName + " :" + colorString + message);
        }

        /// <param name="message"></param>
        /// <param name="qmType"></param>
        /// <param name="priority"></param>
        /// <param name="replace">
        ///     This can be used to help prevent flooding for multiple options that are changed quickly. It allows for a single message
        ///     for multiple changes.
        /// </param>
        public void SendCTCPMessage(string message, QueuedMessageType qmType, int priority, bool replace = false)
        {
            char CTCPChar1 = (char)58;
            char CTCPChar2 = (char)01;

            connection.QueueMessage(qmType, priority,
                "NOTICE " + ChannelName + " " + CTCPChar1 + CTCPChar2 + message + CTCPChar2, replace);
        }

        /// <summary>
        /// Sends a "kick user" message to the channel.
        /// </summary>
        /// <param name="userName">The name of the user that should be kicked.</param>
        /// <param name="priority">The priority of the message in the send queue.</param>
        public void SendKickMessage(string userName, int priority)
        {
            connection.QueueMessage(QueuedMessageType.INSTANT_MESSAGE, priority, "KICK " + ChannelName + " " + userName);
        }

        /// <summary>
        /// Sends a "ban host" message to the channel.
        /// </summary>
        /// <param name="host">The host that should be banned.</param>
        /// <param name="priority">The priority of the message in the send queue.</param>
        public void SendBanMessage(string host, int priority)
        {
            connection.QueueMessage(QueuedMessageType.INSTANT_MESSAGE, priority,
                string.Format("MODE {0} +b *!*@{1}", ChannelName, host));
        }

        public void Join()
        {
            // Wait a random amount of time before joining to prevent join/part floods
            if (Persistent)
            {
                int rn = connection.Rng.Next(1, 10000);

                if (string.IsNullOrEmpty(Password))
                    connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, rn, "JOIN " + ChannelName);
                else
                    connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, rn, "JOIN " + ChannelName + " " + Password);
            }
            else
            {
                if (string.IsNullOrEmpty(Password))
                    connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, "JOIN " + ChannelName);
                else
                    connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, "JOIN " + ChannelName + " " + Password);
            }
        }

        public void RequestUserInfo()
        {
            connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, "WHO " + ChannelName);
        }

        public void Leave()
        {
            // Wait a random amount of time before joining to prevent join/part floods
            if (Persistent)
            {
                int rn = connection.Rng.Next(1, 10000);
                connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, rn, "PART " + ChannelName);
            }
            else
            {
                connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, "PART " + ChannelName);
            }
            ClearUsers();
        }

        public void ClearUsers()
        {
            users.Clear();
            UserListCleared?.Invoke(this, EventArgs.Empty);
        }
    }

    public class ChannelUserEventArgs : EventArgs
    {
        public ChannelUserEventArgs(ChannelUser user)
        {
            User = user;
        }

        public ChannelUser User { get; private set; }
    }

    public class UserNameIndexEventArgs : EventArgs
    {
        public UserNameIndexEventArgs(int index, string userName)
        {
            UserIndex = index;
            UserName = userName;
        }

        public int UserIndex { get; private set; }
        public string UserName { get; private set; }
    }

    public class UserNameEventArgs : EventArgs
    {
        public UserNameEventArgs(string userName)
        {
            UserName = userName;
        }

        public string UserName { get; private set; }
    }

    public class IRCMessageEventArgs : EventArgs
    {
        public IRCMessageEventArgs(ChatMessage ircMessage)
        {
            Message = ircMessage;
        }

        public ChatMessage Message { get; private set; }
    }

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
