using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore.CnCNet5;
using ClientCore.Properties;
using DTAClient.Online;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A list box for listing players.
    /// </summary>
    public class PlayerListBox : XNAListBox
    {
        private const int MARGIN = 2;

        private readonly Texture2D adminGameIcon;
        private readonly Texture2D unknownGameIcon;
        private readonly Texture2D badgeGameIcon;
        private readonly Texture2D friendIcon;
        private readonly Texture2D ignoreIcon;

        private readonly CnCNetUserData cncnetUserData;
        private readonly CnCNetManager cncnetManager;
        private readonly GameCollection gameCollection;

        public PlayerListBoxOptions _options { get; set; }

        public PlayerListBox(
            WindowManager windowManager,
            CnCNetUserData cncnetUserData,
            CnCNetManager cncnetManager,
            GameCollection gameCollection
        ) : base(windowManager)
        {
            this.cncnetUserData = cncnetUserData;
            this.cncnetManager = cncnetManager;
            this.gameCollection = gameCollection;

            adminGameIcon = AssetLoader.TextureFromImage(Resources.cncneticon);
            unknownGameIcon = AssetLoader.TextureFromImage(Resources.unknownicon);
            friendIcon = AssetLoader.LoadTexture("friendicon.png");
            ignoreIcon = AssetLoader.LoadTexture("ignoreicon.png");
            badgeGameIcon = AssetLoader.LoadTexture("Badges/badge.png");
        }

        public override void Initialize()
        {
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            LineHeight = 16;
            
            base.Initialize();
        }

        public IRCUser GetSelectedUser() => SelectedItem?.Tag as IRCUser;

        public void UpdatePlayers(PlayerListBoxOptions options = null)
        {
            _options = options ?? _options;
            if (_options?.Users == null)
                return;

            string selectedUserName = SelectedItem?.Text ?? string.Empty;

            Clear();

            _options.Users.ForEach(user =>
            {
                user.IsFriend = cncnetUserData.IsFriend(user.Name);
                user.IsIgnored = cncnetUserData.IsIgnored(user.Ident);
            });

            var sortedUsers = _options.Users
                .OrderByDescending(u => cncnetManager.IsOnline(u))
                .ThenByDescending(u => cncnetManager.IsAdmin(u))
                .ThenByDescending(u => u.IsFriend)
                .ThenBy(u => u.Name);

            foreach (IRCUser user in sortedUsers)
                AddUser(user);

            if (selectedUserName != string.Empty)
            {
                SelectedIndex = Items.FindIndex(
                    i => i.Text == selectedUserName);
            }
        }


        private void AddUser(IRCUser user)
        {
            XNAListBoxItem item = new XNAListBoxItem();
            UpdateItemInfo(user, item);
            AddItem(item);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            int height = 2 - (ViewTop % LineHeight);

            for (int i = TopIndex; i < Items.Count; i++)
            {
                XNAListBoxItem lbItem = Items[i];
                var user = (IRCUser)lbItem.Tag;
                bool isAdmin = cncnetManager.IsAdmin(user);

                if (height > Height)
                    break;

                int x = TextBorderDistance;

                if (i == SelectedIndex)
                {
                    int drawnWidth;

                    if (DrawSelectionUnderScrollbar || !ScrollBar.IsDrawn() || !EnableScrollbar)
                    {
                        drawnWidth = Width - 2;
                    }
                    else
                    {
                        drawnWidth = Width - 2 - ScrollBar.Width;
                    }

                    FillRectangle(new Rectangle(1, height,
                            drawnWidth, lbItem.TextLines.Count * LineHeight),
                        FocusColor);
                }

                if (isAdmin || lbItem.Texture != null)
                    DrawTexture(isAdmin ? adminGameIcon : lbItem.Texture, new Rectangle(x, height,
                        adminGameIcon.Width, adminGameIcon.Height), Color.White);

                x += adminGameIcon.Width + MARGIN;

                // Friend Icon
                if (user.IsFriend && !(_options?.HideFriendIcon ?? false))
                {
                    DrawTexture(friendIcon,
                        new Rectangle(x, height,
                            friendIcon.Width, friendIcon.Height), Color.White);

                    x += friendIcon.Width + MARGIN;
                }
                // Ignore Icon
                else if (user.IsIgnored && !isAdmin)
                {
                    DrawTexture(ignoreIcon,
                        new Rectangle(x, height,
                            ignoreIcon.Width, ignoreIcon.Height), Color.White);

                    x += ignoreIcon.Width + MARGIN;
                }

                // Badge Icon - coming soon
                /*
                Renderer.DrawTexture(badgeGameIcon,
                    new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                    badgeGameIcon.Width, badgeGameIcon.Height), Color.White);

                x += badgeGameIcon.Width + margin;
                */

                x += lbItem.TextXPadding;

                DrawStringWithShadow(lbItem.Text, FontIndex,
                    new Vector2(x, height),
                    isAdmin ? Color.Red : lbItem.TextColor);

                height += LineHeight;
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }

        private void UpdateItemInfo(IRCUser user, XNAListBoxItem item)
        {
            item.Tag = user;
            item.Text = user.Name;

            if (cncnetManager.IsAdmin(user))
            {
                item.Text = user.Name + " " + "(Admin)".L10N("UI:Main:AdminSuffix");
                item.TextColor = Color.Red;
                item.Texture = adminGameIcon;
                return;
            }

            item.TextColor = (_options?.HighlightOnline ?? false) && !cncnetManager.IsOnline(user) ? UISettings.ActiveSettings.DisabledItemColor : UISettings.ActiveSettings.AltColor;

            item.Texture = user.GameID < 0 || user.GameID >= gameCollection.GameList.Count ? unknownGameIcon : gameCollection.GameList[user.GameID].Texture;
        }
    }
}
