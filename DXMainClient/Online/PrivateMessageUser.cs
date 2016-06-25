using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online
{
    class PrivateMessageUser
    {
        public PrivateMessageUser(IRCUser user)
        {
            IrcUser = user;
        }

        public IRCUser IrcUser { get; private set; }

        public List<IRCMessage> Messages = new List<IRCMessage>();
    }
}
