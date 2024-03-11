using System.Collections.Generic;
using System.IO;
using System.Reflection;

using ClientCore.CnCNet5;
using ClientCore.Extensions;

using DTAClient.Online;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

using SixLabors.ImageSharp;

using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// A list box for listing the players in the CnCNet lobby.
/// </summary>
public class PlayerListBox : XNAListBox
{
    private const int MARGIN = 2;

    public List<ChannelUser> Users;

    private readonly Texture2D adminGameIcon;
    private readonly Texture2D unknownGameIcon;
    private readonly Texture2D badgeGameIcon;
    private readonly Texture2D friendIcon;
    private readonly Texture2D ignoreIcon;

    private readonly GameCollection gameCollection;

    public PlayerListBox(WindowManager windowManager, GameCollection gameCollection) : base(windowManager)
    {
        this.gameCollection = gameCollection;

        Users = [];

        Assembly assembly = Assembly.GetAssembly(typeof(GameCollection));
        using Stream cncnetIconStream = assembly.GetManifestResourceStream("ClientCore.Resources.cncneticon.png");
        using Stream unknownIconStream = assembly.GetManifestResourceStream("ClientCore.Resources.unknownicon.png");

        adminGameIcon = AssetLoader.TextureFromImage(Image.Load(cncnetIconStream));
        unknownGameIcon = AssetLoader.TextureFromImage(Image.Load(unknownIconStream));
        friendIcon = AssetLoader.LoadTexture("friendicon.png");
        ignoreIcon = AssetLoader.LoadTexture("ignoreicon.png");
        badgeGameIcon = AssetLoader.LoadTexture("Badges/badge.png");
    }

    public void AddUser(ChannelUser user)
    {
        XNAListBoxItem item = new();
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
            ChannelUser user = (ChannelUser)lbItem.Tag;

            if (height > Height)
            {
                break;
            }

            int x = TextBorderDistance;

            if (i == SelectedIndex)
            {
                int drawnWidth = DrawSelectionUnderScrollbar || !ScrollBar.IsDrawn() || !EnableScrollbar ? Width - 2 : Width - 2 - ScrollBar.Width;
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

            // Badge Icon - coming soon
            /*
            Renderer.DrawTexture(badgeGameIcon,
                new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                badgeGameIcon.Width, badgeGameIcon.Height), Color.White);

            x += badgeGameIcon.Width + margin;
            */

            // Player Name
            string name = user.IsAdmin ? user.IRCUser.Name + " " + "(Admin)".L10N("Client:Main:AdminSuffix") : user.IRCUser.Name;
            x += lbItem.TextXPadding;

            DrawStringWithShadow(name, FontIndex,
                new Vector2(x, height),
                user.IsAdmin ? Color.Red : lbItem.TextColor);

            height += LineHeight;
        }

        if (DrawBorders)
        {
            DrawPanelBorders();
        }

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

            item.Texture = user.IRCUser.GameID < 0 || user.IRCUser.GameID >= gameCollection.GameList.Count
                ? unknownGameIcon
                : gameCollection.GameList[user.IRCUser.GameID].Texture;
        }
    }
}