using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.Online;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SixLabors.ImageSharp;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A list box for listing the players in the CnCNet lobby.
    /// </summary>
    public class PlayerListBox : XNAListBox
    {
        private const int MARGIN = 2;

        public List<ChannelUser> Users;

        private Texture2D adminGameIcon;
        private Texture2D unknownGameIcon;
        private Texture2D friendIcon;
        private Texture2D ignoreIcon;
        private Texture2D? voiceIcon;

        private GameCollection gameCollection;

        public PlayerListBox(WindowManager windowManager, GameCollection gameCollection) : base(windowManager)
        {
            this.gameCollection = gameCollection;

            Users = new List<ChannelUser>();

            var assembly = Assembly.GetAssembly(typeof(GameCollection));
            using Stream cncnetIconStream = assembly.GetManifestResourceStream("DTAClient.Icons.cncneticon.png");
            using Stream unknownIconStream = assembly.GetManifestResourceStream("DTAClient.Icons.unknownicon.png");

            adminGameIcon = AssetLoader.TextureFromImage(Image.Load(cncnetIconStream));
            unknownGameIcon = AssetLoader.TextureFromImage(Image.Load(unknownIconStream));
            friendIcon = AssetLoader.LoadTexture("friendicon.png");
            ignoreIcon = AssetLoader.LoadTexture("ignoreicon.png");

            const string voiceIconName = "voiceicon.png";
            if (AssetLoader.AssetExists(voiceIconName))
                voiceIcon = AssetLoader.LoadTexture(voiceIconName);
        }

        public void AddUser(ChannelUser user)
        {
            XNAListBoxItem item = new XNAListBoxItem();
            UpdateItemInfo(user, item);
            AddItem(item);
        }

        public void UpdateUserInfo(ChannelUser user)
        {
            XNAListBoxItem item = Items.Find(x => x.Tag == user);
            UpdateItemInfo(user, item);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            int height = 2 - (ViewTop % LineHeight);

            for (int i = TopIndex; i < Items.Count; i++)
            {
                XNAListBoxItem lbItem = Items[i];
                var user = (ChannelUser)lbItem.Tag;

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

                DrawTexture(user.IsAdmin ? adminGameIcon : lbItem.Texture, new Rectangle(x, height,
                        adminGameIcon.Width, adminGameIcon.Height), Color.White);

                x += adminGameIcon.Width + MARGIN;

                // Friend Icon
                if (user.IRCUser.IsFriend)
                {
                    DrawTexture(friendIcon,
                        new Rectangle(x, height,
                        friendIcon.Width, friendIcon.Height), Color.White);

                    x += friendIcon.Width + MARGIN;
                }
                // Ignore Icon
                else if (user.IRCUser.IsIgnored && !user.IsAdmin)
                {
                    DrawTexture(ignoreIcon,
                        new Rectangle(x, height,
                        ignoreIcon.Width, ignoreIcon.Height), Color.White);

                    x += ignoreIcon.Width + MARGIN;
                }

                // Voice Icon
                if (user.HasVoice && voiceIcon != null)
                {
                    DrawTexture(voiceIcon,
                        new Rectangle(x, height, voiceIcon.Width, voiceIcon.Height), Color.White);

                    x += voiceIcon.Width + MARGIN;
                }

                // Player Name
                string name = user.IsAdmin ? user.IRCUser.Name + " " + "(Admin)".L10N("Client:Main:AdminSuffix") : user.IRCUser.Name;
                x += lbItem.TextXPadding;

                DrawStringWithShadow(name, FontIndex,
                    new Vector2(x, height),
                    user.IsAdmin ? Color.Red : lbItem.TextColor);

                height += LineHeight;
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }

        private void UpdateItemInfo(ChannelUser user, XNAListBoxItem item)
        {
            item.Tag = user;

            if (user.IsAdmin)
            {
                item.Text = user.IRCUser.Name + " " + "(Admin)".L10N("Client:Main:AdminSuffix");
                item.TextColor = Color.Red;
                item.Texture = adminGameIcon;
            }
            else
            {
                item.Text = user.IRCUser.Name;

                if (user.IRCUser.GameID < 0 || user.IRCUser.GameID >= gameCollection.GameList.Count)
                    item.Texture = unknownGameIcon;
                else
                    item.Texture = gameCollection.GameList[user.IRCUser.GameID].Texture;
            }
        }
    }
}