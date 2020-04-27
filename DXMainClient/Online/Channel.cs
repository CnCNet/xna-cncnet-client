using ClientCore;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DTAClient.Online
{
    public class Channel
    {
        const int MESSAGE_LIMIT = 1024;

        public event EventHandler<ChannelUserEventArgs> UserAdded;
        public event EventHandler<UserNameIndexEventArgs> UserLeft;
        public event EventHandler<UserNameIndexEventArgs> UserKicked;
        public event EventHandler<UserNameIndexEventArgs> UserQuitIRC;
        public event EventHandler<ChannelUserEventArgs> UserGameIndexUpdated;
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

        public Channel(string uiName, string channelName, bool persistent, string password, Connection connection)
        {
            UIName = uiName;
            ChannelName = channelName.ToLowerInvariant();
            Persistent = persistent;
            Password = password;
            this.connection = connection;

            if (persistent)
            {
                notifyOnUserListChange = UserINISettings.Instance.NotifyOnUserListChange;
                UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;
            }
        }

        #region Public members

        public string UIName { get; private set; }

        public string ChannelName { get; private set; }

        public bool Persistent { get; private set; }

        public string Password { get; private set; }

        Connection connection { get; set; }

        string _topic;
        public string Topic
        {
            get { return _topic; }
            set
            {
                _topic = value;
                if (Persistent)
                    AddMessage(new ChatMessage("Topic for " + UIName + " is: " + _topic));
            }
        }

        List<ChatMessage> messages = new List<ChatMessage>();
        public List<ChatMessage> Messages
        {
            get { return messages; }
        }

        List<ChannelUser> users = new List<ChannelUser>();
        public List<ChannelUser> Users
        {
            get { return users; }
        }

        #endregion

        bool notifyOnUserListChange = true;

        private void Instance_SettingsSaved(object sender, EventArgs e)
        {
            notifyOnUserListChange = UserINISettings.Instance.NotifyOnUserListChange;
        }

        public void AddUser(ChannelUser user)
        {
            users.Add(user);
            users = users.OrderBy(u => u.IRCUser.Name).OrderBy(u => !u.IsAdmin).ToList();
            UserAdded?.Invoke(this, new ChannelUserEventArgs(-1, user));
        }

        public void OnUserJoined(ChannelUser user)
        {
            AddUser(user);

            if (notifyOnUserListChange)
            {
                AddMessage(new ChatMessage(user.IRCUser.Name + " has joined " + UIName + "."));
            }
        }

        public void OnUserListReceived(List<ChannelUser> userList)
        {
            foreach (var user in userList)
            {
                var existingUser = users.Find(u => u.IRCUser.Name == user.IRCUser.Name);
                if (existingUser == null)
                    users.Add(user);
                else
                    existingUser.IsAdmin = user.IsAdmin;
            }

            users = users.OrderBy(u => u.IRCUser.Name).OrderBy(u => !u.IsAdmin).ToList();
            UserListReceived?.Invoke(this, EventArgs.Empty);
        }

        public void OnUserKicked(string userName)
        {
            int index = users.FindIndex(u => u.IRCUser.Name == userName);

            if (index == -1)
                return;

            ChannelUser user = users[index];

            if (user.IRCUser.Name == ProgramConstants.PLAYERNAME)
            {
                users.Clear();
            }
            else
            {
                users.RemoveAt(index);
            }

            AddMessage(new ChatMessage(userName + " has been kicked from " + UIName + "."));

            UserKicked?.Invoke(this, new UserNameIndexEventArgs(index, userName));
        }

        public void OnUserLeft(string userName)
        {
            int index = users.FindIndex(u => u.IRCUser.Name == userName);

            if (index == -1)
                return;

            if (notifyOnUserListChange)
            {
                AddMessage(new ChatMessage(userName + " has left from " + UIName + "."));
            }

            users.RemoveAt(index);
            UserLeft?.Invoke(this, new UserNameIndexEventArgs(index, userName));
        }

        public void OnUserQuitIRC(string userName)
        {
            int index = users.FindIndex(u => u.IRCUser.Name == userName);

            if (index == -1)
                return;

            if (notifyOnUserListChange)
            {
                AddMessage(new ChatMessage(userName + " has quit from CnCNet."));
            }

            users.RemoveAt(index);
            UserQuitIRC?.Invoke(this, new UserNameIndexEventArgs(index, userName));
        }

        public void UpdateGameIndexForUser(string userName)
        {
            int index = users.FindIndex(u => u.IRCUser.Name == userName);

            if (index > -1)
            {
                UserGameIndexUpdated?.Invoke(this, new ChannelUserEventArgs(index, users[index]));
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

        public void SendCTCPMessage(string message, QueuedMessageType qmType, int priority)
        {
            char CTCPChar1 = (char)58;
            char CTCPChar2 = (char)01;

            connection.QueueMessage(qmType, priority, 
                "NOTICE " + ChannelName + " " + CTCPChar1 + CTCPChar2 + message + CTCPChar2);
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
            if (string.IsNullOrEmpty(Password))
                connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, "JOIN " + ChannelName);
            else
                connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, "JOIN " + ChannelName + " " + Password);
        }

        public void RequestUserInfo()
        {
            connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, "WHO " + ChannelName);
        }

        public void Leave()
        {
            connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 9, "PART " + ChannelName);
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
        public ChannelUserEventArgs(int index, ChannelUser user)
        {
            UserIndex = index;
            User = user;
        }

        public int UserIndex { get; private set; }

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
