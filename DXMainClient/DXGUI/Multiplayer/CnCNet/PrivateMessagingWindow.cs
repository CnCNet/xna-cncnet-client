using ClientGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using DTAClient.Online;
using Microsoft.Xna.Framework.Graphics;
using DTAClient.Properties;
using System.IO;
using ClientCore;
using Rampastring.Tools;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework.Audio;
using ClientCore.CnCNet5;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    internal class PrivateMessagingWindow : XNAWindow, ISwitchable
    {
        const int ALL_PLAYERS_VIEW_INDEX = 2;
        const int FRIEND_LIST_VIEW_INDEX = 1;
        const string FRIEND_LIST_PATH = "Client\\friend_list";

        public PrivateMessagingWindow(WindowManager windowManager,
            CnCNetManager connectionManager, GameCollection gameCollection) : base(windowManager)
        {
            this.gameCollection = gameCollection;
            this.connectionManager = connectionManager;

            cncnetChannel = connectionManager.GetChannel("#cncnet");
            if (cncnetChannel == null)
            {
                cncnetChannel = connectionManager.CreateChannel("General CnCNet Chat", "#cncnet", true, null);
                connectionManager.AddChannel(cncnetChannel);
            }

            cncnetChannel.UserListCleared += CncnetChannel_UserListCleared;
            cncnetChannel.UserListReceived += CncnetChannel_UserListReceived;
            cncnetChannel.UserAdded += CncnetChannel_UserAdded;
            cncnetChannel.UserLeft += CncnetChannel_UserLeft;
            cncnetChannel.UserQuitIRC += CncnetChannel_UserQuitIRC;
            cncnetChannel.UserKicked += CncnetChannel_UserKicked;

            WindowManager.GameClosing += WindowManager_GameClosing;
        }

        private void WindowManager_GameClosing(object sender, EventArgs e)
        {
            SaveFriendList();
        }

        XNALabel lblPrivateMessaging;

        XNAClientTabControl tabControl;

        XNALabel lblPlayers;
        XNAListBox lbUserList;

        XNALabel lblMessages;
        ChatListBox lbMessages;

        XNATextBox tbMessageInput;

        Channel cncnetChannel;

        CnCNetManager connectionManager;

        GameCollection gameCollection;

        Texture2D unknownGameIcon;
        Texture2D adminGameIcon;

        Color personalMessageColor;
        Color otherUserMessageColor;

        string lastReceivedPMSender;
        string lastConversationPartner;

        /// <summary>
        /// Holds the users that the local user has had conversations with
        /// during this client session.
        /// </summary>
        List<PrivateMessageUser> privateMessageUsers = new List<PrivateMessageUser>();

        List<string> friendList;

        PrivateMessageNotificationBox notificationBox;

        ToggleableSound sndPrivateMessageSound;
        ToggleableSound sndMessageSound;

        /// <summary>
        /// Because the user cannot view PMs during a game, we store the latest
        /// PM received during a game in this variable and display it when the
        /// user has returned from the game.
        /// </summary>
        PrivateMessage pmReceivedDuringGame;

        private void CncnetChannel_UserKicked(object sender, UserNameEventArgs e)
        {
            RemoveUser(e.UserName, "was kicked from CnCNet.");
        }

        private void CncnetChannel_UserQuitIRC(object sender, UserNameEventArgs e)
        {
            RemoveUser(e.UserName, "quit CnCNet.");
        }

        private void CncnetChannel_UserLeft(object sender, UserNameEventArgs e)
        {
            RemoveUser(e.UserName, "left CnCNet.");
        }

        private void RemoveUser(string userName, string reason)
        {
            var pmUser = privateMessageUsers.Find(pmsgUser => pmsgUser.IrcUser.Name == userName);
            ChatMessage leaveMessage = null;

            if (pmUser != null)
            {
                leaveMessage = new ChatMessage(null, Color.White, DateTime.Now,
                    userName + " " + reason);
                pmUser.Messages.Add(leaveMessage);
            }

            if (tabControl.SelectedTab == ALL_PLAYERS_VIEW_INDEX)
            {
                RefreshCnCNetChannelUsers();
            }
            else
            {
                XNAListBoxItem lbItem = lbUserList.Items.Find(i => i.Text == userName);

                if (lbItem != null)
                {
                    lbItem.TextColor = UISettings.DisabledButtonColor;
                    lbItem.Texture = null;
                    lbItem.Tag = false;

                    if (lbItem == lbUserList.SelectedItem && leaveMessage != null)
                    {
                        tbMessageInput.Enabled = false;
                        tbMessageInput.IsSelected = false;
                        lbMessages.AddMessage(leaveMessage);
                    }
                }
            }
        }

        private void CncnetChannel_UserAdded(object sender, UserEventArgs e)
        {
            var pmUser = privateMessageUsers.Find(pmsgUser => pmsgUser.IrcUser.Name == e.User.Name);

            ChatMessage joinMessage = null;

            if (pmUser != null)
            {
                joinMessage = new ChatMessage(null, Color.White, DateTime.Now,
                    e.User.Name + " has logged in to CnCNet.");
                pmUser.Messages.Add(joinMessage);
            }

            if (tabControl.SelectedTab == ALL_PLAYERS_VIEW_INDEX)
            {
                RefreshCnCNetChannelUsers();
            }
            else // if (tabControl.SelectedTab == 0 or 1)
            {
                XNAListBoxItem lbItem = lbUserList.Items.Find(i => i.Text == e.User.Name);

                if (lbItem != null)
                {
                    lbItem.TextColor = UISettings.AltColor;
                    lbItem.Tag = true;
                    lbItem.Texture = GetUserTexture(e.User);

                    if (lbItem == lbUserList.SelectedItem)
                    {
                        tbMessageInput.Enabled = true;
                        lbMessages.AddMessage(joinMessage);
                    }
                }
            }
        }

        private void RefreshCnCNetChannelUsers()
        {
            string selectedUserName = null;
            if (lbUserList.SelectedIndex > -1 && lbUserList.SelectedIndex < lbUserList.Items.Count)
            {
                selectedUserName = lbUserList.SelectedItem.Text;
            }

            lbUserList.Clear();
            int textEndPosition = tbMessageInput.TextEndPosition;
            int textStartPosition = tbMessageInput.TextStartPosition;
            int textInputPosition = tbMessageInput.InputPosition;
            string wipMessage = tbMessageInput.Text;

            foreach (IRCUser user in cncnetChannel.Users)
            {
                AddPlayerToList(user);
            }

            lbUserList.SelectedIndex = lbUserList.Items.FindIndex(i => i.Text == selectedUserName);
            if (lbUserList.SelectedIndex > -1)
            {
                // Restore textbox state - user list index change cleared it
                // TODO This is a bit hacky, should do it in a more object-oriented way
                // without hacking around with the textbox's own variables
                tbMessageInput.Text = wipMessage;
                tbMessageInput.TextEndPosition = textEndPosition;
                tbMessageInput.TextStartPosition = textStartPosition;
                tbMessageInput.InputPosition = textInputPosition;
            }
        }

        private void CncnetChannel_UserListReceived(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == ALL_PLAYERS_VIEW_INDEX)
            {
                foreach (IRCUser user in cncnetChannel.Users)
                {
                    AddPlayerToList(user);
                }
            }
            else // if (tabControl.SelectedTab == 0 or 1)
            {
                foreach (IRCUser user in cncnetChannel.Users)
                {
                    XNAListBoxItem lbItem = lbUserList.Items.Find(i => i.Text == user.Name);

                    if (lbItem != null)
                    {
                        lbItem.TextColor = UISettings.AltColor;
                        lbItem.Tag = true;
                        lbItem.Texture = GetUserTexture(user);

                        if (lbItem == lbUserList.SelectedItem)
                        {
                            tbMessageInput.Enabled = true;
                        }
                    }
                }
            }
        }

        private void CncnetChannel_UserListCleared(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == ALL_PLAYERS_VIEW_INDEX)
            {
                lbUserList.Clear();
                lbMessages.Clear();
            }
            else
            {
                foreach (XNAListBoxItem lbItem in lbUserList.Items)
                {
                    lbItem.TextColor = UISettings.DisabledButtonColor;
                    lbItem.Texture = null;
                    lbItem.Tag = false;
                }
            }
        }

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 600, 600);
            BackgroundTexture = AssetLoader.LoadTextureUncached("privatemessagebg.png");

            unknownGameIcon = AssetLoader.TextureFromImage(Resources.unknownicon);
            adminGameIcon = AssetLoader.TextureFromImage(Resources.cncneticon);

            personalMessageColor = AssetLoader.GetColorFromString(DomainController.Instance().SentPMColor);
            otherUserMessageColor = AssetLoader.GetColorFromString(DomainController.Instance().GetReceivedPMColor());

            lblPrivateMessaging = new XNALabel(WindowManager);
            lblPrivateMessaging.Name = "lblPrivateMessaging";
            lblPrivateMessaging.FontIndex = 1;
            lblPrivateMessaging.Text = "PRIVATE MESSAGING";

            AddChild(lblPrivateMessaging);
            lblPrivateMessaging.CenterOnParent();
            lblPrivateMessaging.ClientRectangle = new Rectangle(
                lblPrivateMessaging.ClientRectangle.X, 12,
                lblPrivateMessaging.ClientRectangle.Width,
                lblPrivateMessaging.ClientRectangle.Height);

            tabControl = new XNAClientTabControl(WindowManager);
            tabControl.Name = "tabControl";
            tabControl.ClientRectangle = new Rectangle(60, 50, 0, 0);
            tabControl.SoundOnClick = AssetLoader.LoadSound("button.wav");
            tabControl.FontIndex = 1;
            tabControl.AddTab("Messages", 160);
            tabControl.AddTab("Friend List", 160);
            tabControl.AddTab("All Players", 160);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            lblPlayers = new XNALabel(WindowManager);
            lblPlayers.Name = "lblPlayers";
            lblPlayers.ClientRectangle = new Rectangle(12, tabControl.ClientRectangle.Bottom + 24, 0, 0);
            lblPlayers.FontIndex = 1;
            lblPlayers.Text = "PLAYERS:";

            lbUserList = new XNAListBox(WindowManager);
            lbUserList.Name = "lbUserList";
            lbUserList.ClientRectangle = new Rectangle(lblPlayers.ClientRectangle.X, 
                lblPlayers.ClientRectangle.Bottom + 6,
                150, ClientRectangle.Height - lblPlayers.ClientRectangle.Bottom - 18);
            lbUserList.SelectedIndexChanged += LbUserList_SelectedIndexChanged;
            lbUserList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbUserList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblMessages = new XNALabel(WindowManager);
            lblMessages.Name = "lblMessages";
            lblMessages.ClientRectangle = new Rectangle(lbUserList.ClientRectangle.Right + 12,
                lblPlayers.ClientRectangle.Y, 0, 0);
            lblMessages.FontIndex = 1;
            lblMessages.Text = "MESSAGES:";

            lbMessages = new ChatListBox(WindowManager);
            lbMessages.Name = "lbMessages";
            lbMessages.ClientRectangle = new Rectangle(lblMessages.ClientRectangle.X,
                lbUserList.ClientRectangle.Y,
                ClientRectangle.Width - lblMessages.ClientRectangle.X - 12,
                lbUserList.ClientRectangle.Height - 25);
            lbMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbMessages.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            tbMessageInput = new XNATextBox(WindowManager);
            tbMessageInput.Name = "tbMessageInput";
            tbMessageInput.ClientRectangle = new Rectangle(lbMessages.ClientRectangle.X,
                lbMessages.ClientRectangle.Bottom + 6, lbMessages.ClientRectangle.Width, 19);
            tbMessageInput.EnterPressed += TbMessageInput_EnterPressed;
            tbMessageInput.MaximumTextLength = 200;
            tbMessageInput.Enabled = false;

            notificationBox = new PrivateMessageNotificationBox(WindowManager);
            notificationBox.Enabled = false;
            notificationBox.Visible = false;

            AddChild(tabControl);
            AddChild(lblPlayers);
            AddChild(lbUserList);
            AddChild(lblMessages);
            AddChild(lbMessages);
            AddChild(tbMessageInput);
            WindowManager.AddAndInitializeControl(notificationBox);

            base.Initialize();

            CenterOnParent();

            try
            {
                friendList = File.ReadAllLines(ProgramConstants.GamePath + FRIEND_LIST_PATH).ToList();
            }
            catch
            {
                Logger.Log("Loading friend list failed!");
                friendList = new List<string>();
            }

            tabControl.SelectedTab = 0;

            connectionManager.PrivateMessageReceived += ConnectionManager_PrivateMessageReceived;

            SoundEffect seMessageSound = AssetLoader.LoadSound("message.wav");

            SoundEffect sePrivateMessageSound = AssetLoader.LoadSound("pm.wav");

            if (sePrivateMessageSound != null)
            {
                sndPrivateMessageSound = new ToggleableSound(sePrivateMessageSound.CreateInstance());
                sndPrivateMessageSound.Enabled = true;
            }

            if (seMessageSound != null)
            {
                sndMessageSound = new ToggleableSound(seMessageSound.CreateInstance());
                sndMessageSound.Enabled = UserINISettings.Instance.MessageSound;
            }

            SharedUILogic.GameProcessExited += SharedUILogic_GameProcessExited;
        }

        private void SharedUILogic_GameProcessExited()
        {
            WindowManager.AddCallback(new Action(HandleGameProcessExited), null);
        }

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
            if (pmUser == null)
            {
                IRCUser iu = cncnetChannel.Users.Find(u => u.Name == e.Sender);

                if (iu == null)
                {
                    iu = new IRCUser(e.Sender);
                }

                pmUser = new PrivateMessageUser(iu);
                privateMessageUsers.Add(pmUser);

                if (tabControl.SelectedTab == 0)
                {
                    string selecterUserName = string.Empty;

                    if (lbUserList.SelectedItem != null)
                        selecterUserName = lbUserList.SelectedItem.Text;

                    lbUserList.Clear();
                    privateMessageUsers.ForEach(pmsgUser => AddPlayerToList(pmsgUser.IrcUser));

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
            if (ProgramConstants.IsInGame)
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
                IRCUser iu = cncnetChannel.Users.Find(u => u.Name == userName);

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
                tbMessageInput.IsSelected = false;
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
                privateMessageUsers.ForEach(pmsgUser => AddPlayerToList(pmsgUser.IrcUser));
            }
            else if (tabControl.SelectedTab == 1)
            {
                foreach (string friendName in friendList)
                {
                    IRCUser iu = cncnetChannel.Users.Find(u => u.Name == friendName);

                    if (iu == null)
                    {
                        iu = new IRCUser(friendName);
                    }

                    AddPlayerToList(iu);
                }
            }
            else if (tabControl.SelectedTab == ALL_PLAYERS_VIEW_INDEX)
            {
                lbUserList.Clear();

                foreach (IRCUser user in cncnetChannel.Users)
                {
                    AddPlayerToList(user);
                }
            }
        }

        private void AddPlayerToList(IRCUser user)
        {
            XNAListBoxItem lbItem = new XNAListBoxItem();
            lbItem.Text = user.Name;

            bool isOnline = cncnetChannel.Users.Contains(user);

            lbItem.TextColor = isOnline ?
                UISettings.AltColor : UISettings.DisabledButtonColor;
            lbItem.Tag = isOnline;
            lbItem.Texture = isOnline ? GetUserTexture(user) : null;

            lbUserList.AddItem(lbItem);
        }

        private Texture2D GetUserTexture(IRCUser user)
        {
            if (user.IsAdmin)
            {
                return adminGameIcon;
            }
            else if (user.GameID < 0)
            {
                return unknownGameIcon;
            }
            else
                return gameCollection.GameList[user.GameID].Texture;
        }

        public void SaveFriendList()
        {
            Logger.Log("Saving friend list.");

            try
            {
                File.Delete(ProgramConstants.GamePath + FRIEND_LIST_PATH);
                File.WriteAllLines(ProgramConstants.GamePath + FRIEND_LIST_PATH,
                    friendList.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Log("Saving friend list failed! Error message: " + ex.Message);
            }
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
                return;
            }

            if (friendList.Contains(name))
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
            lbUserList.TopIndex = lbUserList.SelectedIndex > -1 ? lbUserList.SelectedIndex : 0;

            if (lbUserList.LastIndex - lbUserList.TopIndex < lbUserList.NumberOfLinesOnList - 1)
                lbUserList.ScrollToBottom();
        }

        /// <summary>
        /// Checks if a specified user belongs to the friend list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public bool IsFriend(string name)
        {
            return friendList.Contains(name);
        }

        /// <summary>
        /// Adds an user into the friend list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public void AddFriend(string name)
        {
            friendList.Add(name);
        }

        /// <summary>
        /// Removes an user from the friend list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public void RemoveFriend(string name)
        {
            friendList.Remove(name);
        }

        /// <summary>
        /// Adds a specified user to the chat ignore list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public void Ignore(string name)
        {
            // TODO implement
        }

        public void SwitchOn()
        {
            tabControl.SelectedTab = 0;
            notificationBox.Hide();

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
                Visible = true;
                Enabled = true;

                if (!string.IsNullOrEmpty(lastConversationPartner))
                {
                    int index = lbUserList.Items.FindIndex(i => i.Text == lastConversationPartner);

                    if (index > -1)
                        lbUserList.SelectedIndex = index;
                }
            }
        }

        public void SwitchOff()
        {
            Visible = false;
            Enabled = false;
        }   

        public string GetSwitchName()
        {
            return "Private Messaging";
        }

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
