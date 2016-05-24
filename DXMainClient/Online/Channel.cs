using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online
{
    public class Channel
    {
        const int MESSAGE_LIMIT = 1024;

        public EventHandler<UserEventArgs> UserAdded;
        public EventHandler<UserEventArgs> UserLeft;
        public EventHandler<UserEventArgs> UserKicked;
        public EventHandler<UserEventArgs> UserQuitIRC;
        public EventHandler<UserEventArgs> UserGameIndexUpdated;

        public EventHandler<IRCMessageEventArgs> MessageAdded;
        public EventHandler<ChannelModeEventArgs> ChannelModesChanged;

        public Channel(string uiName, string channelName, bool persistent, string password, Connection connection)
        {
            UIName = uiName;
            ChannelName = channelName;
            Persistent = persistent;
            Password = password;
            this.connection = connection;
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
                    AddMessage(new IRCMessage(null, Color.White, DateTime.Now, "Topic for " + UIName + " is: " + _topic));
            }
        }

        List<IRCMessage> messages = new List<IRCMessage>();
        public IList<IRCMessage> Messages
        {
            get { return messages.AsReadOnly(); }
        }

        List<IRCUser> users = new List<IRCUser>();
        public IList<IRCUser> Users
        {
            get { return users.AsReadOnly(); }
        }

        #endregion

        public void AddUser(IRCUser user)
        {
            users.Add(user);
            users.OrderBy(u => u.Name).OrderBy(u => !u.IsAdmin).ToList();
            UserAdded(this, new UserEventArgs(-1, user.Name));
        }

        public void OnUserKicked(string userName)
        {
            int index = users.FindIndex(u => u.Name == userName);

            if (index > -1)
            {
                users.RemoveAt(index);
                UserKicked?.Invoke(this, new UserEventArgs(index, userName));
                AddMessage(new IRCMessage(null, Color.White, DateTime.Now, 
                    userName + " has been kicked from " + UIName + "."));
            }
        }

        public void OnUserLeft(string userName)
        {
            int index = users.FindIndex(u => u.Name == userName);

            if (index > -1)
            {
                users.RemoveAt(index);
                UserLeft?.Invoke(this, new UserEventArgs(index, userName));
                AddMessage(new IRCMessage(null, Color.White, DateTime.Now, 
                    userName + " has left from " + UIName + "."));
            }
        }

        public void OnUserQuitIRC(string userName)
        {
            int index = users.FindIndex(u => u.Name == userName);

            if (index > -1)
            {
                users.RemoveAt(index);
                UserQuitIRC?.Invoke(this, new UserEventArgs(index, userName));
                AddMessage(new IRCMessage(null, Color.White, DateTime.Now,
                    userName + " has quit from CnCNet."));
            }
        }

        public void ApplyGameIndexForUser(string userName, int gameIndex)
        {
            int index = users.FindIndex(u => u.Name == userName);

            if (index > -1)
            {
                users[index].GameID = gameIndex;
                UserGameIndexUpdated?.Invoke(this, new UserEventArgs(index, users[index].Name));
            }
        }

        public void OnChannelModesChanged(string sender, string modes)
        {
            ChannelModesChanged?.Invoke(this, new ChannelModeEventArgs(sender, modes));
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

                user.IsAdmin = isAdmin;
                user.Name = name;

                AddUser(user);
            }
        }

        public void AddMessage(IRCMessage message)
        {
            if (messages.Count == MESSAGE_LIMIT)
                messages.RemoveAt(0);

            messages.Add(message);

            MessageAdded?.Invoke(this, new IRCMessageEventArgs(message));
        }

        public void Join()
        {
            if (string.IsNullOrEmpty(Password))
                connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 10, "JOIN " + ChannelName);
            else
                connection.QueueMessage(QueuedMessageType.SYSTEM_MESSAGE, 10, "JOIN " + ChannelName + " " + Password);
        }
    }

    public class UserEventArgs : EventArgs
    {
        public UserEventArgs(int index, string userName)
        {
            UserIndex = index;
            UserName = userName;
        }

        public int UserIndex { get; private set; }

        public string UserName { get; private set; }
    }

    public class IRCMessageEventArgs : EventArgs
    {
        public IRCMessageEventArgs(IRCMessage ircMessage)
        {
            Message = ircMessage;
        }

        public IRCMessage Message { get; private set; }
    }
}
