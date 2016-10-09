using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online
{
    /// <summary>
    /// An user on an IRC channel.
    /// </summary>
    public class ChannelUser
    {
        public ChannelUser(IRCUser ircUser)
        {
            IRCUser = ircUser;
        }

        public IRCUser IRCUser { get; private set; }

        public bool IsAdmin { get; set; }
    }
}
