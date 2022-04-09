using System.Collections.Generic;
using DTAClient.Online;

namespace DTAClient.DXGUI.Multiplayer
{
    public class PlayerListBoxOptions
    {
        public List<IRCUser> Users { get; set; }
        public bool HighlightOnline { get; set; }
        public bool HideFriendIcon { get; set; }

        public PlayerListBoxOptions() : this(null)
        {
        }

        public PlayerListBoxOptions(
            List<IRCUser> users
        )
        {
            Users = users;
        }
    }
}
