using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using System.Diagnostics;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using System;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A list box for CnCNet chat. Supports opening links with a double-click,
    /// and easy adding of IRC messages to the list box.
    /// </summary>
    public class ChatListBox : XNAListBox
    {
        public ChatListBox(WindowManager windowManager) : base(windowManager)
        {
            DoubleLeftClick += ChatListBox_DoubleLeftClick;
        }

        private void ChatListBox_DoubleLeftClick(object sender, EventArgs e)
        {
            if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            string itemText = Items[SelectedIndex].Text;

            int index = itemText.IndexOf("http://");
            if (index == -1)
                index = itemText.IndexOf("ftp://");
            if (index == -1)
                index = itemText.IndexOf("https://");

            if (index == -1)
                return; // No link found

            string link = itemText.Substring(index);
            link = link.Split(' ')[0]; // Nuke any words coming after the link

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
            if (message.SenderName == null)
                AddItem(Renderer.GetSafeString(string.Format("[{0}] {1}",
                    message.DateTime.ToShortTimeString(),
                    message.Message), FontIndex),
                    message.Color, true);
            else
            {
                AddItem(Renderer.GetSafeString(string.Format("[{0}] {1}: {2}",
                    message.DateTime.ToShortTimeString(), message.SenderName, message.Message), FontIndex),
                    message.Color, true);
            }

            if (LastIndex >= Items.Count - 2)
            {
                ScrollToBottom();
            }
        }
    }
}
