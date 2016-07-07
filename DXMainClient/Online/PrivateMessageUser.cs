using System.Collections.Generic;

namespace DTAClient.Online
{
    class PrivateMessageUser
    {
        public PrivateMessageUser(IRCUser user)
        {
            IrcUser = user;
        }

        public IRCUser IrcUser { get; private set; }

        public List<ChatMessage> Messages = new List<ChatMessage>();
    }
}
