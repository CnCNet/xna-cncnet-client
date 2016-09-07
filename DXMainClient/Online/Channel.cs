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

        public event EventHandler<UserEventArgs> UserAdded;
        public event EventHandler<UserNameEventArgs> UserLeft;
        public event EventHandler<UserNameEventArgs> UserKicked;
        public event EventHandler<UserNameEventArgs> UserQuitIRC;
        public event EventHandler<UserEventArgs> UserGameIndexUpdated;
        public event EventHandler UserListReceived;
        public event EventHandler UserListCleared;

        public event EventHandler<IRCMessageEventArgs> MessageAdded;
        public event EventHandler<ChannelModeEventArgs> ChannelModesChanged;
        public event EventHandler<ChannelCTCPEventArgs> CTCPReceived;

        public Channel(string uiName, string channelName, bool persistent, string password, Connection connection)
        {
            UIName = uiName;
            ChannelName = channelName;
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
                    AddMessage(new ChatMessage(null, Color.White, DateTime.Now, "Topic for " + UIName + " is: " + _topic));
            }
        }

        List<ChatMessage> messages = new List<ChatMessage>();
        public List<ChatMessage> Messages
        {
            get { return messages; }
        }

        List<IRCUser> users = new List<IRCUser>();
        public List<IRCUser> Users
        {
            get { return users; }
        }

        #endregion

        bool notifyOnUserListChange = true;

        private void Instance_SettingsSaved(object sender, EventArgs e)
        {
            notifyOnUserListChange = UserINISettings.Instance.NotifyOnUserListChange;
        }

        public void AddUser(IRCUser user)
        {
            users.Add(user);
            users = users.OrderBy(u => u.Name).OrderBy(u => !u.IsAdmin).ToList();
            UserAdded?.Invoke(this, new UserEventArgs(-1, user));
        }

        public void OnUserJoined(IRCUser user)
        {
            AddUser(user);

            if (notifyOnUserListChange)
            {
                AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                    user.Name + " has joined " + UIName + "."));
            }
        }

        public void OnUserListReceived(string[] userList)
        {
            foreach (string userName in userList)
            {
                IRCUser user = new IRCUser();
                bool isAdmin = false;
                string name = userName;

                if (userName.StartsWith("@"))
                {
                    isAdmin = true;
                    name = userName.Substring(1);
                }
                else if (userName.StartsWith("+"))
                    name = userName.Substring(1);

                if (users.Find(u => u.Name == userName) != null)
                    continue;

                user.IsAdmin = isAdmin;
                user.Name = name;

                users.Add(user);
            }

            users = users.OrderBy(u => u.Name).OrderBy(u => !u.IsAdmin).ToList();
            UserListReceived?.Invoke(this, EventArgs.Empty);
        }

        public void OnUserKicked(string userName)
        {
            int index = users.FindIndex(u => u.Name == userName);

            if (index > -1)
            {
                IRCUser user = users[index];

                if (user.Name == ProgramConstants.PLAYERNAME)
                {
                    users.Clear();
                }
                else
                {
                    users.RemoveAt(index);
                }

                UserKicked?.Invoke(this, new UserNameEventArgs(index, userName));
                AddMessage(new ChatMessage(null, Color.White, DateTime.Now, 
                    userName + " has been kicked from " + UIName + "."));
            }
        }

        public void OnUserLeft(string userName)
        {
            int index = users.FindIndex(u => u.Name == userName);

            if (index > -1)
            {
                users.RemoveAt(index);
                UserLeft?.Invoke(this, new UserNameEventArgs(index, userName));

                if (notifyOnUserListChange)
                {
                    AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                        userName + " has left from " + UIName + "."));
                }
            }
        }

        public void OnUserQuitIRC(string userName)
        {
            int index = users.FindIndex(u => u.Name == userName);

            if (index > -1)
            {
                users.RemoveAt(index);
                UserQuitIRC?.Invoke(this, new UserNameEventArgs(index, userName));

                if (notifyOnUserListChange)
                {
                    AddMessage(new ChatMessage(null, Color.White, DateTime.Now,
                        userName + " has quit from CnCNet."));
                }
            }
        }

        public void ApplyGameIndexForUser(string userName, int gameIndex)
        {
            int index = users.FindIndex(u => u.Name == userName);

            if (index > -1)
            {
                users[index].GameID = gameIndex;
                UserGameIndexUpdated?.Invoke(this, new UserEventArgs(index, users[index]));
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
            connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, priority, "KICK " + ChannelName + " " + userName);
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

    public class UserEventArgs : EventArgs
    {
        public UserEventArgs(int index, IRCUser user)
        {
            UserIndex = index;
            User = user;
        }

        public int UserIndex { get; private set; }

        public IRCUser User { get; private set; }
    }

    public class UserNameEventArgs : EventArgs
    {
        public UserNameEventArgs(int index, string userName)
        {
            UserIndex = index;
            UserName = userName;
        }

        public int UserIndex { get; private set; }
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
}
