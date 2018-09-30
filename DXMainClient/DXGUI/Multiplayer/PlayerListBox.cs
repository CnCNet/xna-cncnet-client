using ClientCore.CnCNet5;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.DXGUI.Multiplayer
{
    public class PlayerListBox: XNAListBox
    {
        public List<ChannelUser> Users;

        private Texture2D adminGameIcon;
        private Texture2D unknownGameIcon;
        private Texture2D badgeGameIcon;
        private Texture2D friendIcon;
        private Texture2D ignoreIcon;

        private XNAScrollBar scrollBar;

        private GameCollection gameCollection;

        public PlayerListBox(WindowManager windowManager, GameCollection gameCollection) : base(windowManager)
        {
            this.gameCollection = gameCollection;
            
            Users = new List<ChannelUser>();

            adminGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.cncneticon);
            unknownGameIcon = AssetLoader.TextureFromImage(ClientCore.Properties.Resources.unknownicon);
            friendIcon = AssetLoader.LoadTexture("friendicon.png");
            ignoreIcon = AssetLoader.LoadTexture("ignoreicon.png");
            badgeGameIcon = AssetLoader.LoadTexture("Badges\\badge.png");

            scrollBar = new XNAScrollBar(WindowManager);
        }

        public void AddUser(ChannelUser user)
        {
            AddUserToList(user);
        }

        /// <summary>
        /// Refreshes game information in the game list box.
        /// </summary>
        public void Refresh()
        {
            Items.Clear();
        }

        public override void Draw(GameTime gameTime)
        {
            Rectangle windowRectangle = WindowRectangle();
            
            DrawPanel();

            int height = 2;
            int margin = 2;

            for (int i = TopIndex; i < Items.Count; i++)
            {
                XNAListBoxItem lbItem = Items[i];
                var user = (ChannelUser)lbItem.Tag;

                if (height + lbItem.TextLines.Count * LineHeight > Height)
                    break;

                int x = TextBorderDistance;

                if (i == SelectedIndex)
                {
                    int drawnWidth;

                    if (DrawSelectionUnderScrollbar || !scrollBar.IsDrawn() || !EnableScrollbar)
                    {
                        drawnWidth = windowRectangle.Width - 2;
                    }
                    else
                    {
                        drawnWidth = windowRectangle.Width - 2 - scrollBar.Width;
                    }

                    Renderer.FillRectangle(
                        new Rectangle(windowRectangle.X + 1, windowRectangle.Y + height,
                        drawnWidth, lbItem.TextLines.Count * LineHeight),
                        GetColorWithAlpha(FocusColor));
                }

                Renderer.DrawTexture(lbItem.Texture, new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                        adminGameIcon.Width, adminGameIcon.Height), Color.White);

                x += adminGameIcon.Width + margin;

                // Friend Icon
                if (user.IRCUser.IsFriend)
                {
                    Renderer.DrawTexture(friendIcon,
                        new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                        friendIcon.Width, friendIcon.Height), Color.White);

                    x += friendIcon.Width + margin;
                }
                // Ignore Icon
                else if (user.IRCUser.IsIgnored)
                {
                    Renderer.DrawTexture(ignoreIcon,
                        new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                        ignoreIcon.Width, ignoreIcon.Height), Color.White);

                    x += ignoreIcon.Width + margin;
                }

                // Badge Icon - coming soon
                /*
                Renderer.DrawTexture(badgeGameIcon,
                    new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                    badgeGameIcon.Width, badgeGameIcon.Height), Color.White);

                x += badgeGameIcon.Width + margin;
                */

                // Player Name
                string name = user.IsAdmin ? user.IRCUser.Name + " (Admin)" : user.IRCUser.Name;
                x += lbItem.TextXPadding;

                Renderer.DrawStringWithShadow(name, FontIndex,
                    new Vector2(windowRectangle.X + x, windowRectangle.Y + height),
                    lbItem.TextColor);

                height += LineHeight;
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }

        private void AddUserToList(ChannelUser user)
        {
            XNAListBoxItem item = new XNAListBoxItem();

            item.Tag = user;

            if (user.IsAdmin)
            {
                item.Text = user.IRCUser.Name + " (Admin)";
                item.TextColor = Color.Red;
                item.Texture = adminGameIcon;
            }
            else
            {
                item.Text = user.IRCUser.Name;
                item.TextColor = UISettings.AltColor;

                if (user.IRCUser.GameID < 0 || user.IRCUser.GameID >= gameCollection.GameList.Count)
                    item.Texture = unknownGameIcon;
                else
                    item.Texture = gameCollection.GameList[user.IRCUser.GameID].Texture;
            }

            AddItem(item);
        }
    }
}
