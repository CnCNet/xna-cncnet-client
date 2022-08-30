using System;
using System.Collections.Generic;
using DTAClient.Online;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Localization;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
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

            AddColumn("Player".L10N("UI:Main:RecentPlayerPlayer"));
            AddColumn("Game".L10N("UI:Main:RecentPlayerGame"));
            AddColumn("Date/Time".L10N("UI:Main:RecentPlayerDateTime"));
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

            var textColor = isOnline ? UISettings.ActiveSettings.AltColor : UISettings.ActiveSettings.DisabledItemColor;
            AddItem(new List<XNAListBoxItem>()
            {
                new XNAListBoxItem(recentPlayer.PlayerName, textColor)
                {
                    Tag = iu
                },
                new XNAListBoxItem(recentPlayer.GameName, textColor),
                new XNAListBoxItem(recentPlayer.GameTime.ToLocalTime().ToString("ddd, MMM d, yyyy @ h:mm tt"), textColor)
            });
        }

        private XNAPanel CreateColumnHeader(string headerText)
        {
            XNALabel xnaLabel = new XNALabel(WindowManager);
            xnaLabel.FontIndex = HeaderFontIndex;
            xnaLabel.X = 3;
            xnaLabel.Y = 2;
            xnaLabel.Text = headerText;
            XNAPanel header = new XNAPanel(WindowManager);
            header.Height = xnaLabel.Height + 3;
            var width = Width / 3;
            if (DrawListBoxBorders)
                header.Width = width + 1;
            else
                header.Width = width;
            header.AddChild(xnaLabel);

            return header;
        }

        private void AddColumn(string headerText)
        {
            var header = CreateColumnHeader(headerText);
            var xnaListBox = new XNAListBox(WindowManager);
            xnaListBox.RightClick += ListBox_RightClick;
            AddColumn(header, xnaListBox);
        }

        private void ListBox_RightClick(object sender, EventArgs e)
        {
            if (HoveredIndex < 0 || HoveredIndex >= ItemCount)
                return;
            
            SelectedIndex = HoveredIndex;

            var selectedItem = GetItem(0, SelectedIndex);
            PlayerRightClick?.Invoke(this, new RecentPlayerTableRightClickEventArgs((IRCUser)selectedItem.Tag));
        }
    }
}
