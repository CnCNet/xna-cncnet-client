using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientCore;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    class PlayerContextMenu : XNAContextMenu
    {
        private const string PRIVATE_MESSAGE = "Private Message";
        private const string ADD_FRIEND = "Add Friend";
        private const string REMOVE_FRIEND = "Remove Friend";
        private const string BLOCK = "Block";
        private const string UNBLOCK = "Unblock";
        private const string INVITE = "Invite";
        private const string JOIN = "Join";
        
        private readonly CnCNetManager connectionManager;
        private readonly CnCNetUserData cncnetUserData;
        private readonly PrivateMessagingWindow pmWindow;
        private PlayerContextMenuData contextMenuData;
        private XNAContextMenuItem privateMessageItem;
        private XNAContextMenuItem toggleFriendItem;
        private XNAContextMenuItem toggleIgnoreItem;
        private XNAContextMenuItem invitePlayerItem;
        private XNAContextMenuItem joinPlayerItem;

        public EventHandler<JoinUserEventArgs> JoinEvent;

        public PlayerContextMenu(
            WindowManager windowManager,
            CnCNetManager connectionManager,
            CnCNetUserData cncnetUserData,
            PrivateMessagingWindow pmWindow
        ) : base(windowManager)
        {
            this.connectionManager = connectionManager;
            this.cncnetUserData = cncnetUserData;
            this.pmWindow = pmWindow;

            Name = nameof(PlayerContextMenu);
            ClientRectangle = new Rectangle(0, 0, 150, 2);
            Enabled = false;
            Visible = false;
        }

        public override void Initialize()
        {
            privateMessageItem = new XNAContextMenuItem()
            {
                Text = PRIVATE_MESSAGE,
                SelectAction = () => pmWindow.InitPM(GetIrcUser().Name)
            };
            toggleFriendItem = new XNAContextMenuItem()
            {
                Text = ADD_FRIEND,
                SelectAction = () => cncnetUserData.ToggleFriend(GetIrcUser().Name)
            };
            toggleIgnoreItem = new XNAContextMenuItem()
            {
                Text = BLOCK,
                SelectAction = () => GetIrcUserIdent(cncnetUserData.ToggleIgnoreUser)
            };
            invitePlayerItem = new XNAContextMenuItem()
            {
                Text = INVITE,
                SelectAction = Invite
            };
            joinPlayerItem = new XNAContextMenuItem()
            {
                Text = JOIN,
                SelectAction = () => JoinEvent?.Invoke(this, new JoinUserEventArgs(GetIrcUser()))
            };
            AddItem(privateMessageItem);
            AddItem(toggleFriendItem);
            AddItem(toggleIgnoreItem);
            AddItem(invitePlayerItem);
            AddItem(joinPlayerItem);
        }

        private void Invite()
        {
            // note it's assumed that if the channel name is specified, the game name must be also
            if (string.IsNullOrEmpty(contextMenuData.inviteChannelName) || ProgramConstants.IsInGame)
            {
                return;
            }

            string messageBody = ProgramConstants.GAME_INVITE_CTCP_COMMAND + " " + contextMenuData.inviteChannelName + ";" + contextMenuData.inviteGameName;

            if (!string.IsNullOrEmpty(contextMenuData.inviteChannelPassword))
            {
                messageBody += ";" + contextMenuData.inviteChannelPassword;
            }

            connectionManager.SendCustomMessage(new QueuedMessage(
                "PRIVMSG " + GetIrcUser().Name + " :\u0001" + messageBody + "\u0001", QueuedMessageType.CHAT_MESSAGE, 0
            ));
        }

        private void UpdateButtons(IRCUser ircUser, ChannelUser channelUser = null)
        {
            var isOnline = connectionManager.UserList.Any(u => u.Name == ircUser.Name);
            var isAdmin = channelUser?.IsAdmin ?? false;

            privateMessageItem.Visible = isOnline;
            toggleIgnoreItem.Selectable = !isAdmin;
            invitePlayerItem.Visible = isOnline && !string.IsNullOrEmpty(contextMenuData.inviteChannelName);
            joinPlayerItem.Visible = !contextMenuData.PreventJoinGame && isOnline;

            toggleFriendItem.Text = cncnetUserData.IsFriend(ircUser.Name) ? REMOVE_FRIEND : ADD_FRIEND;
            toggleIgnoreItem.Text = cncnetUserData.IsIgnored(ircUser.Ident) ? UNBLOCK : BLOCK;
        }

        private void GetIrcUserIdent(Action<string> callback)
        {
            var ircUser = GetIrcUser();

            if (!string.IsNullOrEmpty(ircUser.Ident))
            {
                callback.Invoke(ircUser.Ident);
                return;
            }

            void WhoIsReply(object sender, WhoEventArgs whoEventargs)
            {
                ircUser.Ident = whoEventargs.Ident;
                callback.Invoke(whoEventargs.Ident);
                connectionManager.WhoReplyReceived -= WhoIsReply;
            }

            connectionManager.WhoReplyReceived += WhoIsReply;
            connectionManager.SendWhoIsMessage(ircUser.Name);
        }

        private IRCUser GetIrcUser()
        {
            if (contextMenuData.IrcUser != null)
                return contextMenuData.IrcUser;
            
            if (contextMenuData.ChannelUser?.IRCUser != null)
                return contextMenuData.ChannelUser.IRCUser;

            if (!string.IsNullOrEmpty(contextMenuData.PlayerName))
                return connectionManager.UserList.Find(u => u.Name == contextMenuData.PlayerName);

            return null;
        }

        public void Show(string playerName, Point cursorPoint)
        {
            Show(new PlayerContextMenuData
            {
                PlayerName = playerName
            }, cursorPoint);
        }

        public void Show(IRCUser ircUser, Point cursorPoint)
        {
            Show(new PlayerContextMenuData
            {
                IrcUser = ircUser
            }, cursorPoint);
        }

        public void Show(ChannelUser channelUser, Point cursorPoint)
        {
            Show(new PlayerContextMenuData
            {
                ChannelUser = channelUser
            }, cursorPoint);
        }

        public void Show(PlayerContextMenuData data, Point cursorPoint)
        {
            contextMenuData = data;
            var ircUser = GetIrcUser();
            if (ircUser == null)
                return;

            UpdateButtons(ircUser, contextMenuData.ChannelUser);
            Open(cursorPoint);
        }
    }
}
