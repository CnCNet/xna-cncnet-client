using System;

namespace DTAClient.Online.EventArguments
{
    public class JoinUserEventArgs : EventArgs
    {
        public IRCUser IrcUser { get; }

        public JoinUserEventArgs(IRCUser ircUser)
        {
            IrcUser = ircUser;
        }
    }
}
