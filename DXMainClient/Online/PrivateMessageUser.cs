using System.Collections.Generic;

namespace DTAClient.Online;

internal class PrivateMessageUser
{
    public PrivateMessageUser(IRCUser user)
    {
        IrcUser = user;
    }

    public IRCUser IrcUser { get; private set; }

    public List<ChatMessage> Messages = [];
}