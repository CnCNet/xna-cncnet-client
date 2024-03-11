using System;
using System.Collections.Generic;

using ClientCore.Extensions;

using DTAClient.Online;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

public class RecentPlayerTable : XNAMultiColumnListBox
{
    private readonly CnCNetManager connectionManager;

    public EventHandler<RecentPlayerTableRightClickEventArgs> PlayerRightClick;

    public RecentPlayerTable(WindowManager windowManager, CnCNetManager connectionManager) : base(windowManager)
    {
        this.connectionManager = connectionManager;
    }

    public override void Initialize()
    {
        AllowRightClickUnselect = false;

        base.Initialize();

        AddColumn("Player".L10N("Client:Main:RecentPlayerPlayer"));
        AddColumn("Game".L10N("Client:Main:RecentPlayerGame"));
        AddColumn("Date/Time".L10N("Client:Main:RecentPlayerDateTime"));
    }

    public void AddRecentPlayer(RecentPlayer recentPlayer)
    {
        IRCUser iu = connectionManager.UserList.Find(u => u.Name == recentPlayer.PlayerName);
        bool isOnline = true;

        if (iu == null)
        {
            iu = new IRCUser(recentPlayer.PlayerName);
            isOnline = false;
        }

        Microsoft.Xna.Framework.Color textColor = isOnline ? UISettings.ActiveSettings.AltColor : UISettings.ActiveSettings.DisabledItemColor;
        AddItem(new List<XNAListBoxItem>()
        {
            new(recentPlayer.PlayerName, textColor)
            {
                Tag = iu
            },
            new(recentPlayer.GameName, textColor),
            new(recentPlayer.GameTime.ToLocalTime().ToString("ddd, MMM d, yyyy @ h:mm tt"), textColor)
        });
    }

    private XNAPanel CreateColumnHeader(string headerText)
    {
        XNALabel xnaLabel = new(WindowManager)
        {
            FontIndex = HeaderFontIndex,
            X = 3,
            Y = 2,
            Text = headerText
        };
        XNAPanel header = new(WindowManager)
        {
            Height = xnaLabel.Height + 3
        };
        int width = Width / 3;
        header.Width = DrawListBoxBorders ? width + 1 : width;
        header.AddChild(xnaLabel);

        return header;
    }

    private void AddColumn(string headerText)
    {
        XNAPanel header = CreateColumnHeader(headerText);
        XNAListBox xnaListBox = new(WindowManager);
        xnaListBox.RightClick += ListBox_RightClick;
        AddColumn(header, xnaListBox);
    }

    private void ListBox_RightClick(object sender, EventArgs e)
    {
        if (HoveredIndex < 0 || HoveredIndex >= ItemCount)
        {
            return;
        }

        SelectedIndex = HoveredIndex;

        XNAListBoxItem selectedItem = GetItem(0, SelectedIndex);
        PlayerRightClick?.Invoke(this, new RecentPlayerTableRightClickEventArgs((IRCUser)selectedItem.Tag));
    }
}