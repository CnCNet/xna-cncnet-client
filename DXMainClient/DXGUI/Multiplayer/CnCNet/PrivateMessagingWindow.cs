using ClientCore;
using ClientCore.CnCNet5;
using ClientGUI;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    internal class PrivateMessagingWindow : XNAWindow, ISwitchable
    {
        private const int ALL_PLAYERS_VIEW_INDEX = 2;
        private const int FRIEND_LIST_VIEW_INDEX = 1;

        private CnCNetUserData cncnetUserData;

        public PrivateMessagingWindow(WindowManager windowManager,
            CnCNetManager connectionManager, GameCollection gameCollection, CnCNetUserData cncnetUserData) : base(windowManager)
        {
            this.gameCollection = gameCollection;
            this.connectionManager = connectionManager;
            this.cncnetUserData = cncnetUserData;
        }

        private XNALabel lblPrivateMessaging;

        private XNAClientTabControl tabControl;

        private XNALabel lblPlayers;
        private XNAListBox lbUserList;

        private XNALabel lblMessages;
        private ChatListBox lbMessages;

        private XNATextBox tbMessageInput;

        private XNAContextMenu playerContextMenu;

        private CnCNetManager connectionManager;

        private GameCollection gameCollection;

        private Texture2D unknownGameIcon;
        private Texture2D adminGameIcon;

        private Color personalMessageColor;
        private Color otherUserMessageColor;

        private string lastReceivedPMSender;
        private string lastConversationPartner;

        /// <summary>
        /// Holds the users that the local user has had conversations with
        /// during this client session.
        /// </summary>
        private List<PrivateMessageUser> privateMessageUsers = new List<PrivateMessageUser>();

        private PrivateMessageNotificationBox notificationBox;

        private EnhancedSoundEffect sndPrivateMessageSound;
        private EnhancedSoundEffect sndMessageSound;

        /// <summary>
        /// Because the user cannot view PMs during a game, we store the latest
        /// PM received during a game in this variable and display it when the
        /// user has returned from the game.
        /// </summary>
        private PrivateMessage pmReceivedDuringGame;

        // These are used by the "invite to game" feature in the
        // context menu and are kept up-to-date by the lobby
        private string inviteChannelName;
        private string inviteGameName;
        private string inviteChannelPassword;

        public override void Initialize()
        {
            Name = nameof(PrivateMessagingWindow);
            ClientRectangle = new Rectangle(0, 0, 600, 600);
            BackgroundTexture = AssetLoader.LoadTextureUncached("privatemessagebg.png");

            unknownGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.unknownicon);
            adminGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.cncneticon);

            personalMessageColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.SentPMColor);
            otherUserMessageColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ReceivedPMColor);

            lblPrivateMessaging = new XNALabel(WindowManager);
            lblPrivateMessaging.Name = nameof(lblPrivateMessaging);
            lblPrivateMessaging.FontIndex = 1;
            lblPrivateMessaging.Text = "PRIVATE MESSAGING";

            AddChild(lblPrivateMessaging);
            lblPrivateMessaging.CenterOnParent();
            lblPrivateMessaging.ClientRectangle = new Rectangle(
                lblPrivateMessaging.X, 12,
                lblPrivateMessaging.Width,
                lblPrivateMessaging.Height);

            tabControl = new XNAClientTabControl(WindowManager);
            tabControl.Name = nameof(tabControl);
            tabControl.ClientRectangle = new Rectangle(60, 50, 0, 0);
            tabControl.ClickSound = new EnhancedSoundEffect("button.wav");
            tabControl.FontIndex = 1;
            tabControl.AddTab("Messages", 160);
            tabControl.AddTab("Friend List", 160);
            tabControl.AddTab("All Players", 160);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            lblPlayers = new XNALabel(WindowManager);
            lblPlayers.Name = nameof(lblPlayers);
            lblPlayers.ClientRectangle = new Rectangle(12, tabControl.Bottom + 24, 0, 0);
            lblPlayers.FontIndex = 1;
            lblPlayers.Text = "PLAYERS:";

            lbUserList = new XNAListBox(WindowManager);
            lbUserList.Name = nameof(lbUserList);
            lbUserList.ClientRectangle = new Rectangle(lblPlayers.X, 
                lblPlayers.Bottom + 6,
                150, Height - lblPlayers.Bottom - 18);
            lbUserList.RightClick += LbUserList_RightClick;
            lbUserList.SelectedIndexChanged += LbUserList_SelectedIndexChanged;
            lbUserList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbUserList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblMessages = new XNALabel(WindowManager);
            lblMessages.Name = nameof(lblMessages);
            lblMessages.ClientRectangle = new Rectangle(lbUserList.Right + 12,
                lblPlayers.Y, 0, 0);
            lblMessages.FontIndex = 1;
            lblMessages.Text = "MESSAGES:";

            lbMessages = new ChatListBox(WindowManager);
            lbMessages.Name = nameof(lbMessages);
            lbMessages.ClientRectangle = new Rectangle(lblMessages.X,
                lbUserList.Y,
                Width - lblMessages.X - 12,
                lbUserList.Height - 25);
            lbMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            tbMessageInput = new XNATextBox(WindowManager);
            tbMessageInput.Name = nameof(tbMessageInput);
            tbMessageInput.ClientRectangle = new Rectangle(lbMessages.X,
                lbMessages.Bottom + 6, lbMessages.Width, 19);
            tbMessageInput.EnterPressed += TbMessageInput_EnterPressed;
            tbMessageInput.MaximumTextLength = 200;
            tbMessageInput.Enabled = false;

            playerContextMenu = new XNAContextMenu(WindowManager);
            playerContextMenu.Name = nameof(playerContextMenu);
            playerContextMenu.ClientRectangle = new Rectangle(0, 0, 150, 2);
            playerContextMenu.Disable();
            playerContextMenu.AddItem("Add Friend", PlayerContextMenu_ToggleFriend);
            playerContextMenu.AddItem("Toggle Block", PlayerContextMenu_ToggleIgnore, null, () => (bool)lbUserList.SelectedItem.Tag, null);
            playerContextMenu.AddItem("Invite", PlayerContextMenu_Invite, null, () => !string.IsNullOrEmpty(inviteChannelName));

            notificationBox = new PrivateMessageNotificationBox(WindowManager);
            notificationBox.Enabled = false;
            notificationBox.Visible = false;
            notificationBox.LeftClick += NotificationBox_LeftClick;

            AddChild(tabControl);
            AddChild(lblPlayers);
            AddChild(lbUserList);
            AddChild(lblMessages);
            AddChild(lbMessages);
            AddChild(tbMessageInput);
            AddChild(playerContextMenu);
            WindowManager.AddAndInitializeControl(notificationBox);

            base.Initialize();

            CenterOnParent();

            tabControl.SelectedTab = 0;

            connectionManager.PrivateMessageReceived += ConnectionManager_PrivateMessageReceived;
            connectionManager.UserAdded += ConnectionManager_UserAdded;
            connectionManager.UserRemoved += ConnectionManager_UserRemoved;
            connectionManager.UserGameIndexUpdated += ConnectionManager_UserGameIndexUpdated;

            sndMessageSound = new EnhancedSoundEffect("message.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundMessageCooldown);

            sndPrivateMessageSound = new EnhancedSoundEffect("pm.wav", 0.0, 0.0, ClientConfiguration.Instance.SoundPrivateMessageCooldown);

            sndMessageSound.Enabled = UserINISettings.Instance.MessageSound;

            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;
        }

        private void ConnectionManager_UserGameIndexUpdated(object sender, UserEventArgs e)
        {
            var userItem = lbUserList.Items.Find(item => item.Text == e.User.Name);

            if (userItem != null)
                userItem.Texture = GetUserTexture(e.User);
        }

        private void ConnectionManager_UserRemoved(object sender, UserNameIndexEventArgs e)
        {
            var pmUser = privateMessageUsers.Find(pmsgUser => pmsgUser.IrcUser.Name == e.UserName);
            ChatMessage leaveMessage = null;

            if (pmUser != null)
            {
                leaveMessage = new ChatMessage(Color.White,
                    e.UserName + " is now offline.");
                pmUser.Messages.Add(leaveMessage);
            }

            if (tabControl.SelectedTab == ALL_PLAYERS_VIEW_INDEX)
            {
                if (e.UserIndex >= lbUserList.Items.Count || e.UserIndex < 0)
                    return;

                if (e.UserIndex == lbUserList.SelectedIndex)
                {
                    lbUserList.SelectedIndex = -1;
                }
                else if (e.UserIndex < lbUserList.SelectedIndex)
                {
                    lbUserList.SelectedIndexChanged -= LbUserList_SelectedIndexChanged;
                    lbUserList.SelectedIndex--;
                    lbUserList.SelectedIndexChanged += LbUserList_SelectedIndexChanged;
                }

                lbUserList.Items.RemoveAt(e.UserIndex);
            }
            else
            {
                XNAListBoxItem lbItem = lbUserList.Items.Find(i => i.Text == e.UserName);

                if (lbItem != null)
                {
                    lbItem.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                    lbItem.Texture = null;
                    lbItem.Tag = false;

                    if (lbItem == lbUserList.SelectedItem && leaveMessage != null)
                    {
                        tbMessageInput.Enabled = false;
                        lbMessages.AddMessage(leaveMessage);
                    }
                }
            }
        }

        private void ConnectionManager_UserAdded(object sender, UserEventArgs e)
        {
            var pmUser = privateMessageUsers.Find(pmsgUser => pmsgUser.IrcUser.Name == e.User.Name);

            ChatMessage joinMessage = null;

            if (pmUser != null)
            {
                joinMessage = new ChatMessage(e.User.Name + " is now online.");
                pmUser.Messages.Add(joinMessage);
            }

            if (tabControl.SelectedTab == ALL_PLAYERS_VIEW_INDEX)
            {
                RefreshAllUsers();
            }
            else // if (tabControl.SelectedTab == 0 or 1)
            {
                XNAListBoxItem lbItem = lbUserList.Items.Find(i => i.Text == e.User.Name);

                if (lbItem != null)
                {
                    lbItem.Tag = true;
                    lbItem.Texture = GetUserTexture(e.User);

                    if (lbItem == lbUserList.SelectedItem)
                    {
                        tbMessageInput.Enabled = true;

                        if (joinMessage != null)
                            lbMessages.AddMessage(joinMessage);
                    }
                }
            }
        }

        private void RefreshAllUsers()
        {
            lbUserList.SelectedIndexChanged -= LbUserList_SelectedIndexChanged;

            string selectedUserName = string.Empty;

            var selectedItem = lbUserList.SelectedItem;
            if (selectedItem != null)
                selectedUserName = selectedItem.Text;

            lbUserList.Clear();

            foreach (var ircUser in connectionManager.UserList)
            {
                var item = new XNAListBoxItem(ircUser.Name);
                item.Tag = true;
                item.Texture = GetUserTexture(ircUser);
                lbUserList.AddItem(item);
            }

            lbUserList.SelectedIndex = lbUserList.Items.FindIndex(item => item.Text == selectedUserName);

            if (lbUserList.SelectedIndex == -1)
            {
                // If we previously had an user selected and they now went offline,
                // clear the messages and message input
                tbMessageInput.Text = string.Empty;
                tbMessageInput.Enabled = false;
                lbMessages.Clear();
                lbMessages.SelectedIndex = -1;
                lbMessages.TopIndex = 0;
            }

            lbUserList.SelectedIndexChanged += LbUserList_SelectedIndexChanged;
        }

        public void SetInviteChannelInfo(string channelName, string gameName, string channelPassword)
        {
            inviteChannelName = channelName;
            inviteGameName = gameName;
            inviteChannelPassword = channelPassword;
        }

        public void ClearInviteChannelInfo() => SetInviteChannelInfo(string.Empty, string.Empty, string.Empty);

        private void NotificationBox_LeftClick(object sender, EventArgs e) => SwitchOn();

        private void LbUserList_RightClick(object sender, EventArgs e)
        {
            lbUserList.SelectedIndex = lbUserList.HoveredIndex;

            if (lbUserList.SelectedIndex < 0 ||
                lbUserList.SelectedIndex >= lbUserList.Items.Count)
            {
                return;
            }

            playerContextMenu.Items[0].Text = cncnetUserData.IsFriend(lbUserList.SelectedItem.Text) ? "Remove Friend" : "Add Friend";
            
            if ((bool)lbUserList.SelectedItem.Tag)
            {
                IRCUser iu = connectionManager.UserList.Find(u => u.Name == lbUserList.SelectedItem.Text);
                playerContextMenu.Items[1].Text = cncnetUserData.IsIgnored(iu.Ident) ? "Unblock" : "Block";
            }

            playerContextMenu.Open(GetCursorPoint());
        }

        private void PlayerContextMenu_ToggleFriend()
        {
            var lbItem = lbUserList.SelectedItem;

            if (lbItem == null)
                return;

            cncnetUserData.ToggleFriend(lbItem.Text);

            // lazy solution, but friends are removed rarely so it shouldn't bother players too much
            if (tabControl.SelectedTab == FRIEND_LIST_VIEW_INDEX)
                TabControl_SelectedIndexChanged(this, EventArgs.Empty); 
        }

        private void PlayerContextMenu_Invite()
        {
            var lbItem = lbUserList.SelectedItem;

            if (lbItem == null)
            {
                return;
            }

            // note it's assumed that if the channel name is specified, the game name must be also
            if (string.IsNullOrEmpty(inviteChannelName) || ProgramConstants.IsInGame)
            {
                return;
            }

            string messageBody = ProgramConstants.GAME_INVITE_CTCP_COMMAND + " " + inviteChannelName + ";" + inviteGameName;

            if (!string.IsNullOrEmpty(inviteChannelPassword))
            {
                messageBody += ";" + inviteChannelPassword;
            }

            connectionManager.SendCustomMessage(new QueuedMessage("PRIVMSG " + lbItem.Text + " :\u0001" +
                messageBody + "\u0001",
                QueuedMessageType.CHAT_MESSAGE, 0));
        }

        private void PlayerContextMenu_ToggleIgnore()
        {
            var lbItem = lbUserList.SelectedItem;

            if (lbItem == null || !(bool)lbUserList.SelectedItem.Tag)
                return;

            IRCUser iu = connectionManager.UserList.Find(u => u.Name == lbUserList.SelectedItem.Text);

            cncnetUserData.ToggleIgnoreUser(iu.Ident);
        }

        private void SharedUILogic_GameProcessExited() =>
            WindowManager.AddCallback(new Action(HandleGameProcessExited), null);

        private void HandleGameProcessExited()
        {
            if (pmReceivedDuringGame != null)
            {
                ShowNotification(pmReceivedDuringGame.User, pmReceivedDuringGame.Message);
                pmReceivedDuringGame = null;
            }
        }

        private void ConnectionManager_PrivateMessageReceived(object sender, PrivateMessageEventArgs e)
        {
            PrivateMessageUser pmUser = privateMessageUsers.Find(u => u.IrcUser.Name == e.Sender);
            IRCUser iu = connectionManager.UserList.Find(u => u.Name == e.Sender);

            // We don't accept PMs from people who we don't share any channels with
            if (iu == null)
            {
                return;
            }

            // Messages from users we've blocked are not wanted
            if (cncnetUserData.IsIgnored(iu.Ident))
            {
                return;
            }

            if (pmUser == null)
            {
                pmUser = new PrivateMessageUser(iu);
                privateMessageUsers.Add(pmUser);

                if (tabControl.SelectedTab == 0)
                {
                    string selecterUserName = string.Empty;

                    if (lbUserList.SelectedItem != null)
                        selecterUserName = lbUserList.SelectedItem.Text;

                    lbUserList.Clear();
                    privateMessageUsers.ForEach(pmsgUser => AddPlayerToList(pmsgUser.IrcUser,
                        connectionManager.UserList.Find(u => u.Name == pmsgUser.IrcUser.Name) != null));

                    lbUserList.SelectedIndex = lbUserList.Items.FindIndex(i => i.Text == selecterUserName);
                }
            }

            ChatMessage message = new ChatMessage(e.Sender, otherUserMessageColor, DateTime.Now, e.Message);

            pmUser.Messages.Add(message);

            lastReceivedPMSender = e.Sender;
            lastConversationPartner = e.Sender;

            if (!Visible)
            {
                HandleNotification(pmUser.IrcUser, e.Message);

                if (lbUserList.SelectedItem == null || lbUserList.SelectedItem.Text != e.Sender)
                    return;
            }
            else if (lbUserList.SelectedItem == null || lbUserList.SelectedItem.Text != e.Sender)
            {
                HandleNotification(pmUser.IrcUser, e.Message);
                return;
            }

            lbMessages.AddMessage(message);
            if (sndMessageSound != null)
                sndMessageSound.Play();
        }

        /// <summary>
        /// Displays a PM message if the user is not in-game, and queues
        /// it to be displayed after the game if the user is in-game.
        /// </summary>
        /// <param name="ircUser">The sender of the private message.</param>
        /// <param name="message">The contents of the private message.</param>
        private void HandleNotification(IRCUser ircUser, string message)
        {
            if (!ProgramConstants.IsInGame)
            {
                ShowNotification(ircUser, message);
            }
            else
                pmReceivedDuringGame = new PrivateMessage(ircUser, message);
        }

        private void ShowNotification(IRCUser ircUser, string message)
        {
            notificationBox.Show(GetUserTexture(ircUser), ircUser.Name, message);
            if (sndPrivateMessageSound != null)
                sndPrivateMessageSound.Play();
        }

        private void TbMessageInput_EnterPressed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbMessageInput.Text))
                return;

            if (lbUserList.SelectedItem == null)
                return;

            string userName = lbUserList.SelectedItem.Text;

            connectionManager.SendCustomMessage(new QueuedMessage("PRIVMSG " + userName + " :" + tbMessageInput.Text,
                QueuedMessageType.CHAT_MESSAGE, 0));

            PrivateMessageUser pmUser = privateMessageUsers.Find(u => u.IrcUser.Name == userName);
            if (pmUser == null)
            {
                IRCUser iu = connectionManager.UserList.Find(u => u.Name == userName);

                if (iu == null)
                {
                    Logger.Log("Null IRCUser in private messaging?");
                    return;
                }

                pmUser = new PrivateMessageUser(iu);
                privateMessageUsers.Add(pmUser);
            }

            ChatMessage sentMessage = new ChatMessage(ProgramConstants.PLAYERNAME,
                personalMessageColor, DateTime.Now, tbMessageInput.Text);

            pmUser.Messages.Add(sentMessage);

            lbMessages.AddMessage(sentMessage);
            if (sndMessageSound != null)
                sndMessageSound.Play();

            lastConversationPartner = userName;

            if (tabControl.SelectedTab != 0)
            {
                tabControl.SelectedTab = 0;
                lbUserList.SelectedIndex = lbUserList.Items.FindIndex(i => i.Text == userName);
            }

            tbMessageInput.Text = string.Empty;
        }

        private void LbUserList_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbMessages.Clear();
            lbMessages.SelectedIndex = -1;
            lbMessages.TopIndex = 0;
            tbMessageInput.Text = string.Empty;

            if (lbUserList.SelectedItem == null)
            {
                tbMessageInput.Enabled = false;
                return;
            }

            tbMessageInput.Enabled = (bool)lbUserList.SelectedItem.Tag == true;

            var pmUser = privateMessageUsers.Find(u => 
                u.IrcUser.Name == lbUserList.SelectedItem.Text);

            if (pmUser == null)
            {
                return;
            }

            foreach (ChatMessage message in pmUser.Messages)
            {
                lbMessages.AddMessage(message);
            }

            lbMessages.ScrollToBottom();
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbMessages.Clear();
            lbMessages.SelectedIndex = -1;
            lbMessages.TopIndex = 0;
            lbUserList.Clear();
            lbUserList.SelectedIndex = -1;
            lbUserList.TopIndex = 0;
            tbMessageInput.Text = string.Empty;

            if (tabControl.SelectedTab == 0)
            {
                privateMessageUsers.ForEach(pmsgUser => AddPlayerToList(pmsgUser.IrcUser, 
                    connectionManager.UserList.Find(u => u.Name == pmsgUser.IrcUser.Name) != null));
            }
            else if (tabControl.SelectedTab == FRIEND_LIST_VIEW_INDEX)
            {
                foreach (string friendName in cncnetUserData.FriendList)
                {
                    IRCUser iu = connectionManager.UserList.Find(u => u.Name == friendName);
                    bool isOnline = true;

                    if (iu == null)
                    {
                        iu = new IRCUser(friendName);
                        isOnline = false;
                    }

                    AddPlayerToList(iu, isOnline);
                }
            }
            else if (tabControl.SelectedTab == ALL_PLAYERS_VIEW_INDEX)
            {
                foreach (var user in connectionManager.UserList)
                {
                    AddPlayerToList(user, true);
                }
            }
        }

        private void AddPlayerToList(IRCUser user, bool isOnline)
        {
            XNAListBoxItem lbItem = new XNAListBoxItem();
            lbItem.Text = user.Name;

            lbItem.TextColor = isOnline ?
                UISettings.ActiveSettings.AltColor : UISettings.ActiveSettings.DisabledItemColor;
            lbItem.Tag = isOnline;
            lbItem.Texture = isOnline ? GetUserTexture(user) : null;

            lbUserList.AddItem(lbItem);
        }

        private Texture2D GetUserTexture(IRCUser user)
        {
            if (user.GameID < 0 || user.GameID >= gameCollection.GameList.Count)
                return unknownGameIcon;
            else
                return gameCollection.GameList[user.GameID].Texture;
        }

        /// <summary>
        /// Prepares a recipient for sending a private message.
        /// </summary>
        /// <param name="name"></param>
        public void InitPM(string name)
        {
            Visible = true;
            Enabled = true;

            // Check if we've already talked with the user during this session 
            // and if so, open the old conversation
            int pmUserIndex = privateMessageUsers.FindIndex(
                pmUser => pmUser.IrcUser.Name == name);

            if (pmUserIndex > -1)
            {
                tabControl.SelectedTab = 0;
                lbUserList.SelectedIndex = lbUserList.Items.FindIndex(i => i.Text == name);
                WindowManager.SelectedControl = tbMessageInput;
                return;
            }

            if (cncnetUserData.IsFriend(name))
            {
                // If we haven't talked with the user, check if they are a friend and if so,
                // let's enter the friend list and talk to them there
                tabControl.SelectedTab = FRIEND_LIST_VIEW_INDEX;
            }
            else
            {
                // If the user isn't a friend, switch to the "all players" view and
                // open the conversation there
                tabControl.SelectedTab = ALL_PLAYERS_VIEW_INDEX;
            }

            lbUserList.SelectedIndex = lbUserList.Items.FindIndex(i => i.Text == name);

            if (lbUserList.SelectedIndex > -1)
            {
                WindowManager.SelectedControl = tbMessageInput;

                lbUserList.TopIndex = lbUserList.SelectedIndex > -1 ? lbUserList.SelectedIndex : 0;
            }

            if (lbUserList.LastIndex - lbUserList.TopIndex < lbUserList.NumberOfLinesOnList - 1)
                lbUserList.ScrollToBottom();
        }

        public void SwitchOn()
        {
            tabControl.SelectedTab = 0;
            notificationBox.Hide();

            WindowManager.SelectedControl = null;

            if (Visible)
            {
                if (!string.IsNullOrEmpty(lastReceivedPMSender))
                {
                    int index = lbUserList.Items.FindIndex(i => i.Text == lastReceivedPMSender);

                    if (index > -1)
                        lbUserList.SelectedIndex = index;
                }
            }
            else
            {
                Enable();

                if (!string.IsNullOrEmpty(lastConversationPartner))
                {
                    int index = lbUserList.Items.FindIndex(i => i.Text == lastConversationPartner);

                    if (index > -1)
                    {
                        lbUserList.SelectedIndex = index;
                        WindowManager.SelectedControl = tbMessageInput;
                    }
                }
            }
        }

        public void SwitchOff() => Disable();

        public string GetSwitchName() => "Private Messaging";

        /// <summary>
        /// A class for storing a private message in memory.
        /// </summary>
        class PrivateMessage
        {
            public PrivateMessage(IRCUser user, string message)
            {
                User = user;
                Message = message;
            }

            public IRCUser User;
            public string Message;
        }
    }
}
