using System;
using System.Linq;
using ClientCore;
using ClientCore.Extensions;
using ClientGUI;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TextCopy;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class GlobalContextMenu : XNAContextMenu
    {
        private readonly string PRIVATE_MESSAGE = "Private Message".L10N("Client:Main:PrivateMessage");
        private readonly string ADD_FRIEND = "Add Friend".L10N("Client:Main:AddFriend");
        private readonly string REMOVE_FRIEND = "Remove Friend".L10N("Client:Main:RemoveFriend");
        private readonly string BLOCK = "Block".L10N("Client:Main:Block");
        private readonly string UNBLOCK = "Unblock".L10N("Client:Main:Unblock");
        private readonly string INVITE = "Invite".L10N("Client:Main:Invite");
        private readonly string JOIN = "Join".L10N("Client:Main:Join");
        private readonly string COPY_LINK = "Copy Link".L10N("Client:Main:CopyLink");
        private readonly string OPEN_LINK = "Open Link".L10N("Client:Main:OpenLink");
        private readonly int LINK_LENGTH = 30;
        private readonly Rectangle STD_SIZE = new Rectangle(0, 0, 150, 2);
        private readonly Rectangle LNK_SIZE = new Rectangle(0, 0, 300, 2);

        private readonly CnCNetUserData cncnetUserData;
        private readonly PrivateMessagingWindow pmWindow;
        private XNAContextMenuItem privateMessageItem;
        private XNAContextMenuItem toggleFriendItem;
        private XNAContextMenuItem toggleIgnoreItem;
        private XNAContextMenuItem invitePlayerItem;
        private XNAContextMenuItem joinPlayerItem;

        protected readonly CnCNetManager connectionManager;
        protected GlobalContextMenuData contextMenuData;

        public EventHandler<JoinUserEventArgs> JoinEvent;

        public GlobalContextMenu(
            WindowManager windowManager,
            CnCNetManager connectionManager,
            CnCNetUserData cncnetUserData,
            PrivateMessagingWindow pmWindow
        ) : base(windowManager)
        {
            this.connectionManager = connectionManager;
            this.cncnetUserData = cncnetUserData;
            this.pmWindow = pmWindow;

            Name = nameof(GlobalContextMenu);
            ClientRectangle = STD_SIZE;
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

        private void UpdateButtons()
        {
            UpdatePlayerBasedButtons();
            UpdateMessageBasedButtons();
        }

        private void UpdatePlayerBasedButtons()
        {
            var ircUser = GetIrcUser();
            var isOnline = ircUser != null && connectionManager.UserList.Any(u => u.Name == ircUser.Name);
            var isAdmin = contextMenuData.ChannelUser?.IsAdmin ?? false;

            toggleFriendItem.Visible = ircUser != null;
            privateMessageItem.Visible = ircUser != null && isOnline;
            toggleIgnoreItem.Visible = ircUser != null;
            invitePlayerItem.Visible = ircUser != null && isOnline && !string.IsNullOrEmpty(contextMenuData.inviteChannelName);
            joinPlayerItem.Visible = ircUser != null && !contextMenuData.PreventJoinGame && isOnline;

            toggleIgnoreItem.Selectable = !isAdmin;

            if (ircUser == null)
                return;

            toggleFriendItem.Text = cncnetUserData.IsFriend(ircUser.Name) ? REMOVE_FRIEND : ADD_FRIEND;
            toggleIgnoreItem.Text = cncnetUserData.IsIgnored(ircUser.Ident) ? UNBLOCK : BLOCK;
        }

        private void UpdateMessageBasedButtons()
        {
            RemoveLinks();

            var links = contextMenuData?.ChatMessage?.Message?.GetLinks();

            if (links == null)
            {
                ClientRectangle = STD_SIZE;
                return;
            }

            ClientRectangle = LNK_SIZE;

            foreach (string link in links)
            {
                string linkToDisplay = LINK_LENGTH > link.Length ? link.Substring(0, link.Length) : link.Substring(0, LINK_LENGTH) + "...";

                if (Items.Where(item => item.Text.Contains(linkToDisplay)).ToList().Count > 0)
                    continue;

                var copyLinkItem = new XNAContextMenuItem()
                {
                    Text = $"{COPY_LINK} {linkToDisplay}",
                    SelectAction = () => CopyLink(link)
                };

                var openLinkItem = new XNAContextMenuItem()
                {
                    Text = $"{OPEN_LINK} {linkToDisplay}",
                    SelectAction = () =>
                    {
                        bool isTrusted = false;
                        try
                        {
                            string domain = new Uri(link).Host;
                            var trustedDomains = ClientConfiguration.Instance.TrustedDomains.Concat(ClientConfiguration.Instance.AlwaysTrustedDomains);
                            isTrusted = trustedDomains.Contains(domain, StringComparer.InvariantCultureIgnoreCase)
                                || trustedDomains.Any(trustedDomain => domain.EndsWith("." + trustedDomain, StringComparison.InvariantCultureIgnoreCase));
                        }
                        catch (Exception ex)
                        {
                            isTrusted = false;
                            Logger.Log($"Error in parsing the URL \"{link}\": {ex.ToString()}");
                        }

                        if (isTrusted)
                        {
                            ProcessLauncher.StartShellProcess(link);
                            return;
                        }

                        // Show the warning if the links is not trusted
                        var msgBox = new XNAMessageBox(WindowManager,
                        "Open Link Confirmation".L10N("Client:Main:OpenLinkConfirmationTitle"),
                        """
                        You're about to open a link shared in chat.

                        Please note that this link hasn't been verified,
                        and CnCNet is not responsible for its content.

                        Would you like to open the following link in your browser?
                        """.L10N("Client:Main:OpenLinkConfirmationText")
                        + Environment.NewLine + Environment.NewLine + link,
                        XNAMessageBoxButtons.YesNo);
                        msgBox.YesClickedAction = (msgBox) => ProcessLauncher.StartShellProcess(link);
                        msgBox.Show();
                    }
                };

                AddItem(copyLinkItem);
                AddItem(openLinkItem);
            }
        }

        private void CopyLink(string link)
        {
            try
            {
                ClipboardService.SetText(link);
            }
            catch (Exception)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("Client:Main:Error"), "Unable to copy link".L10N("Client:Main:ClipboardCopyLinkFailed"));
            }
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

            if (!string.IsNullOrEmpty(contextMenuData.ChatMessage?.SenderName))
                return connectionManager.UserList.Find(u => u.Name == contextMenuData.ChatMessage.SenderName);

            return null;
        }

        public void RemoveLinks()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (!(Items[i].Text.Contains(COPY_LINK) || Items[i].Text.Contains(OPEN_LINK)))
                    continue;

                Items.RemoveAt(i);

                i--;
            }
        }

        public void Show(string playerName, Point cursorPoint)
        {
            Show(new GlobalContextMenuData
            {
                PlayerName = playerName
            }, cursorPoint);
        }

        public void Show(IRCUser ircUser, Point cursorPoint)
        {
            Show(new GlobalContextMenuData
            {
                IrcUser = ircUser
            }, cursorPoint);
        }

        public void Show(ChannelUser channelUser, Point cursorPoint)
        {
            Show(new GlobalContextMenuData
            {
                ChannelUser = channelUser
            }, cursorPoint);
        }

        public void Show(ChatMessage chatMessage, Point cursorPoint)
        {
            Show(new GlobalContextMenuData()
            {
                ChatMessage = chatMessage
            }, cursorPoint);
        }

        public void Show(GlobalContextMenuData data, Point cursorPoint)
        {
            Disable();
            contextMenuData = data;
            UpdateButtons();

            if (!Items.Any(i => i.Visible))
                return;

            Open(cursorPoint);
        }
    }
}
