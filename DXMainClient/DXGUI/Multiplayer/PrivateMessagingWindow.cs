using ClientGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using DTAClient.Online;
using DTAClient.domain.CnCNet;
using Microsoft.Xna.Framework.Graphics;
using DTAClient.Properties;

namespace DTAClient.DXGUI.Multiplayer
{
    class PrivateMessagingWindow : XNAWindow, ISwitchable
    {
        const int ALL_PLAYERS_VIEW_INDEX = 2;
        const int FRIEND_LIST_VIEW_INDEX = 1;

        public PrivateMessagingWindow(WindowManager windowManager,
            CnCNetManager connectionManager) : base(windowManager)
        {
            gameCollection = connectionManager.GetGameCollection();

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
        }

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
            IRCMessage leaveMessage = null;

            if (pmUser != null)
            {
                leaveMessage = new IRCMessage(null, Color.White, DateTime.Now,
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
                        lbMessages.AddMessage(leaveMessage);
                    }
                }
            }
        }

        private void CncnetChannel_UserAdded(object sender, UserEventArgs e)
        {
            var pmUser = privateMessageUsers.Find(pmsgUser => pmsgUser.IrcUser.Name == e.User.Name);

            if (pmUser != null)
            {
                pmUser.Messages.Add(new IRCMessage(null, Color.White, DateTime.Now,
                    e.User.Name + " has logged in to CnCNet."));
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
                    lbItem.Texture = GetUserTexture(true, e.User);

                    if (lbItem == lbUserList.SelectedItem)
                    {
                        tbMessageInput.Enabled = true;
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

            foreach (IRCUser user in cncnetChannel.Users)
            {
                AddPlayerToList(user);
            }

            lbUserList.SelectedIndex = lbUserList.Items.FindIndex(i => i.Text == selectedUserName);
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
                        lbItem.Texture = GetUserTexture(true, user);

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
        }

        XNALabel lblPrivateMessaging;

        XNATabControl tabControl;

        XNALabel lblPlayers;
        XNAListBox lbUserList;

        XNALabel lblMessages;
        ChatListBox lbMessages;

        XNATextBox tbMessageInput;

        Channel cncnetChannel;

        GameCollection gameCollection;

        Texture2D unknownGameIcon;
        Texture2D adminGameIcon;

        /// <summary>
        /// Holds the users that the local user has had conversations with
        /// during this client session.
        /// </summary>
        List<PrivateMessageUser> privateMessageUsers = new List<PrivateMessageUser>();

        List<string> friendList = new List<string>();

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 600, 600);
            BackgroundTexture = AssetLoader.LoadTextureUncached("privatemessagebg.png");

            unknownGameIcon = AssetLoader.TextureFromImage(Resources.unknownicon);
            adminGameIcon = AssetLoader.TextureFromImage(Resources.cncneticon);

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

            tabControl = new XNATabControl(WindowManager);
            tabControl.Name = "tabControl";
            tabControl.ClientRectangle = new Rectangle(60, 50, 0, 0);
            tabControl.SoundOnClick = AssetLoader.LoadSound("button.wav");
            tabControl.FontIndex = 1;
            tabControl.AddTab("Messages", AssetLoader.LoadTexture("160pxbtn.png"),
                AssetLoader.LoadTexture("160pxbtn_c.png"), true);
            tabControl.AddTab("Friend List", AssetLoader.LoadTexture("160pxbtn.png"),
                AssetLoader.LoadTexture("160pxbtn_c.png"), true);
            tabControl.AddTab("All Players", AssetLoader.LoadTexture("160pxbtn.png"),
                AssetLoader.LoadTexture("160pxbtn_c.png"), true);
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

            tbMessageInput = new XNATextBox(WindowManager);
            tbMessageInput.Name = "tbMessageInput";
            tbMessageInput.ClientRectangle = new Rectangle(lbMessages.ClientRectangle.X,
                lbMessages.ClientRectangle.Bottom + 6, lbMessages.ClientRectangle.Width, 19);

            AddChild(tabControl);
            AddChild(lblPlayers);
            AddChild(lbUserList);
            AddChild(lblMessages);
            AddChild(lbMessages);
            AddChild(tbMessageInput);

            base.Initialize();

            CenterOnParent();

            // Load friend list

        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbMessages.Clear();
            lbMessages.SelectedIndex = -1;
            lbMessages.TopIndex = 0;
            lbUserList.Clear();
            lbUserList.SelectedIndex = -1;
            lbUserList.TopIndex = 0;

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
            lbItem.Texture = GetUserTexture(isOnline, user);

            lbUserList.AddItem(lbItem);
        }

        private Texture2D GetUserTexture(bool isOnline, IRCUser user)
        {
            if (!isOnline)
                return null;

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

        public void SwitchOn()
        {
            Visible = true;
            Enabled = true;
            tabControl.SelectedTab = 0;
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
    }
}
