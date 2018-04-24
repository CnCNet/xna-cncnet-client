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
        private const string FRIEND_LIST_PATH = "Client\\friend_list";

        public PrivateMessagingWindow(WindowManager windowManager,
            CnCNetManager connectionManager, GameCollection gameCollection) : base(windowManager)
        {
            this.gameCollection = gameCollection;
            this.connectionManager = connectionManager;

            connectionManager.UserAdded += ConnectionManager_UserAdded;
            connectionManager.UserRemoved += ConnectionManager_UserRemoved;
            connectionManager.UserGameIndexUpdated += ConnectionManager_UserGameIndexUpdated;

            WindowManager.GameClosing += WindowManager_GameClosing;
        }

        private void ConnectionManager_UserGameIndexUpdated(object sender, UserEventArgs e)
        {
            var userItem = lbUserList.Items.Find(item => item.Text == e.User.Name);

            if (userItem != null)
            {
                userItem.Texture = GetUserTexture(e.User);
            }
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
                    lbItem.TextColor = UISettings.DisabledButtonColor;
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
                joinMessage = new ChatMessage(null, Color.White, DateTime.Now,
                    e.User.Name + " is now online.");
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
                    lbItem.TextColor = UISettings.AltColor;
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
                var item = new XNAListBoxItem(ircUser.Name, UISettings.AltColor);
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

        PlayerContextMenu playerContextMenu;

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

        public override void Initialize()
        {
            Name = "PrivateMessagingWindow";
            ClientRectangle = new Rectangle(0, 0, 600, 600);
            BackgroundTexture = AssetLoader.LoadTextureUncached("privatemessagebg.png");

            unknownGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.unknownicon);
            adminGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.cncneticon);

            personalMessageColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.SentPMColor);
            otherUserMessageColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.ReceivedPMColor);

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
            lbUserList.RightClick += LbUserList_RightClick;
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

            playerContextMenu = new PlayerContextMenu(WindowManager);
            playerContextMenu.Name = "playerContextMenu";
            playerContextMenu.ClientRectangle = new Rectangle(0, 0, 150, 2);
            playerContextMenu.Enabled = false;
            playerContextMenu.Visible = false;
            playerContextMenu.AddItem("Add Friend");
            playerContextMenu.OptionSelected += PlayerContextMenu_OptionSelected;

            notificationBox = new PrivateMessageNotificationBox(WindowManager);
            notificationBox.Enabled = false;
            notificationBox.Visible = false;

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

            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;
        }

        private void LbUserList_RightClick(object sender, EventArgs e)
        {
            lbUserList.SelectedIndex = lbUserList.HoveredIndex;

            if (lbUserList.SelectedIndex < 0 ||
                lbUserList.SelectedIndex >= lbUserList.Items.Count)
            {
                return;
            }

            playerContextMenu.Items[0].Text = IsFriend(lbUserList.SelectedItem.Text) ? "Remove Friend" : "Add Friend";

            playerContextMenu.Show();
        }

        private void PlayerContextMenu_OptionSelected(object sender, ContextMenuOptionEventArgs e)
        {
            var lbItem = lbUserList.SelectedItem;

            if (lbItem == null)
            {
                return;
            }

            ToggleFriend(lbItem.Text);

            // lazy solution, but friends are removed rarely so it shouldn't bother players too much
            if (tabControl.SelectedTab == FRIEND_LIST_VIEW_INDEX)
                TabControl_SelectedIndexChanged(this, EventArgs.Empty); 
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
            IRCUser iu = connectionManager.UserList.Find(u => u.Name == e.Sender);

            // We don't accept PMs from people who we don't share any channels with
            if (iu == null)
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
                foreach (string friendName in friendList)
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
                UISettings.AltColor : UISettings.DisabledButtonColor;
            lbItem.Tag = isOnline;
            lbItem.Texture = isOnline ? GetUserTexture(user) : null;

            lbUserList.AddItem(lbItem);
        }

        private Texture2D GetUserTexture(IRCUser user)
        {
            if (user.GameID < 0 || user.GameID >= gameCollection.GameList.Count)
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
                WindowManager.SelectedControl = tbMessageInput;
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

            if (lbUserList.SelectedIndex > -1)
            {
                WindowManager.SelectedControl = tbMessageInput;

                lbUserList.TopIndex = lbUserList.SelectedIndex > -1 ? lbUserList.SelectedIndex : 0;
            }

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
        /// Adds or removes an user from the friend list depending on whether
        /// they already are on the friend list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public void ToggleFriend(string name)
        {
            if (IsFriend(name))
                RemoveFriend(name);
            else
                AddFriend(name);
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

        public void SwitchOff()
        {
            Disable();
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
