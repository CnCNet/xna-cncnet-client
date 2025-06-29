using System;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using DTAClient.Online;

using Microsoft.Xna.Framework;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

using static System.Windows.Forms.LinkLabel;

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

            // Get the clicked links
            string[] links = Items[SelectedIndex].Text?.GetLinks();
            
            if (links == null) 
                return;

            if (links.Length == 0 || links.Length > 1)
                return;

            string link = links[0];

            // Determine if the links is trusted
            bool isTrusted = false;
            try
            {
                string domain = new Uri(link).Host;
                var trustedDomains = ClientConfiguration.Instance.TrustedDomains.Concat(ClientConfiguration.Instance.AlwaysTrustedDomains);
                isTrusted = trustedDomains.Contains(domain, StringComparer.InvariantCultureIgnoreCase)
                    || trustedDomains.Any(trustedDomain => domain.EndsWith("." + trustedDomain, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                isTrusted = false;
                Logger.Log($"Error in parsing the URL \"{link}\": {ex.ToString()}");
            }

            if (isTrusted)
            {
                ProcessLink(link);
                return;
            }

            // Show the warning if the links is not trusted
            var msgBox = new XNAMessageBox(WindowManager,
                "Open Link Confirmation".L10N("Client:Main:OpenLinkConfirmationTitle"),
                """
                    You're about to open a link shared in chat.

                    Please note that this link hasn't been verified,
                    and CnCNet is not responsible for its content.

                    Would you like to open the following link in your browser?
                    """.L10N("Client:Main:OpenLinkConfirmationText")
                + Environment.NewLine + Environment.NewLine + link,
                XNAMessageBoxButtons.YesNo);
            msgBox.YesClickedAction = (msgBox) => ProcessLink(link);
            msgBox.Show();
        }

        private void ProcessLink(string link)
        {
            if (link != null)
                ProcessLauncher.StartShellProcess(link);
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
