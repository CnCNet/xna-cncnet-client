using System;
using DTAClient.Online;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class RecentPlayerTableRightClickEventArgs : EventArgs
    {
        public IRCUser IrcUser { get; set; }

        public RecentPlayerTableRightClickEventArgs(IRCUser ircUser)
        {
            IrcUser = ircUser;
        }
    }
}
