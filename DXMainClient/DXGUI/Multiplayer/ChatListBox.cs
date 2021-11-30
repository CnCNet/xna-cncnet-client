using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using System.Diagnostics;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using System;
using ClientCore.Extensions;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A list box for CnCNet chat. Supports opening links with a double-click,
    /// and easy adding of IRC messages to the list box.
    /// </summary>
    public class ChatListBox : XNAListBox, IMessageView
    {
        public ChatListBox(WindowManager windowManager) : base(windowManager)
        {
            DoubleLeftClick += ChatListBox_DoubleLeftClick;
        }

        private void ChatListBox_DoubleLeftClick(object sender, EventArgs e)
        {
            if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            var link = Items[SelectedIndex].Text?.GetLink();
            if (link == null)
                return;

            Process.Start(link);
        }

        public void AddMessage(string message)
        {
            AddMessage(new ChatMessage(message));
        }

        public void AddMessage(string sender, string message, Color color)
        {
            AddMessage(new ChatMessage(sender, color, DateTime.Now, message));
        }

        public void AddMessage(ChatMessage message)
        {
            var listBoxItem = new XNAListBoxItem
            {
                TextColor = message.Color,
                Selectable = true,
                Tag = message
            };
            
            if (message.SenderName == null)
            {
                listBoxItem.Text = Renderer.GetSafeString(string.Format("[{0}] {1}",
                    message.DateTime.ToShortTimeString(),
                    message.Message), FontIndex);
            }
            else
            {
                listBoxItem.Text = Renderer.GetSafeString(string.Format("[{0}] {1}: {2}",
                    message.DateTime.ToShortTimeString(), message.SenderName, message.Message), FontIndex);
            }
            
            AddItem(listBoxItem);

            if (LastIndex >= Items.Count - 2)
            {
                ScrollToBottom();
            }
        }
    }
}
