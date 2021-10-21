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
        private readonly CnCNetManager connectionManager;
        private readonly CnCNetUserData cncnetUserData;
        private readonly PrivateMessagingWindow pmWindow;
        private PlayerContextMenuData contextMenuData;

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

            Name = "PlayerContextMenu";
            ClientRectangle = new Rectangle(0, 0, 150, 2);
            Enabled = false;
            Visible = false;
        }

        public override void Initialize()
        {
            AddItem("Private Message", () => pmWindow.InitPM(GetIrcUser().Name));
            AddItem("Add Friend", () => cncnetUserData.ToggleFriend(GetIrcUser().Name));
            AddItem("Ignore User", () => cncnetUserData.ToggleIgnoreUser(GetIrcUser().Ident));
            AddItem("Invite", Invite);
            AddItem("Join", () => JoinEvent?.Invoke(this, new JoinUserEventArgs(GetIrcUser())));
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

            Items[0].Visible = isOnline;
            Items[2].Visible = !isAdmin && !string.IsNullOrEmpty(ircUser.Ident);
            Items[3].Visible = isOnline && !string.IsNullOrEmpty(contextMenuData.inviteChannelName);
            Items[4].Visible = !contextMenuData.PreventJoinGame && isOnline;

            Items[1].Text = cncnetUserData.IsFriend(ircUser.Name) ? "Remove Friend" : "Add Friend";
            Items[2].Text = cncnetUserData.IsIgnored(ircUser.Ident) ? "Unblock" : "Block";
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
