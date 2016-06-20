using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using DTAClient.Online;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A list box for CnCNet chat. Supports opening links with a double-click.
    /// </summary>
    public class ChatListBox : DXListBox
    {
        public ChatListBox(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void OnDoubleLeftClick()
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

            base.OnDoubleLeftClick();
        }

        public void AddMessage(IRCMessage message)
        {
            if (message.Sender == null)
                AddItem(string.Format("[{0}] {1}",
                    message.DateTime.ToShortTimeString(),
                    Renderer.GetSafeString(message.Message, FontIndex)),
                    message.Color, true);
            else
            {
                AddItem(string.Format("[{0}] {1}: {2}",
                    message.DateTime.ToShortTimeString(), message.Sender,
                    Renderer.GetSafeString(message.Message, FontIndex)),
                    message.Color, true);
            }

            if (LastIndex == Items.Count - 2)
            {
                ScrollToBottom();
            }
        }
    }
}
