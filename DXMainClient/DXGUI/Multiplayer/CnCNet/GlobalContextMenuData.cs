using DTAClient.Online;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class GlobalContextMenuData
    {
        /// <summary>
        /// The ChannelUser to show the menu for.
        /// </summary>
        public ChannelUser ChannelUser { get; set; }
        
        /// <summary>
        /// The ChatMessage to show the menu for.
        /// </summary>
        public ChatMessage ChatMessage { get; set; }
        
        /// <summary>
        /// The IRCUser to show the menu for.
        /// </summary>
        public IRCUser IrcUser { get; set; }
        
        /// <summary>
        /// The player to show the menu for. This is used to determine the IRCUser internally.
        /// </summary>
        public string PlayerName { get; set; }
        
        /// <summary>
        /// The invite properties are used for the Invite option in the menu.
        /// </summary>
        public string inviteChannelName { get; set; }
        public string inviteGameName { get; set; }
        public string inviteChannelPassword { get; set; }
        
        /// <summary>
        /// Prevent the Join option from showing in the menu.
        /// </summary>
        public bool PreventJoinGame { get; set; }
    }
}
