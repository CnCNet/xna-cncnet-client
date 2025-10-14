using System;
using DTAClient.Online;
public class ChannelUser
{
    public ChannelUser(IRCUser ircUser)
    {
        IRCUser = ircUser;
    }

    public IRCUser IRCUser { get; private set; }

    public bool IsAdmin { get; set; }

    public bool IsFriend { get; set; }

    // New property
    public bool HasMedal => IRCUser.HasMedal;

    public static int ChannelUserComparison(ChannelUser u1, ChannelUser u2)
    {
        if (u1.IsAdmin != u2.IsAdmin)
            return u1.IsAdmin ? -1 : 1;

        if (u1.IsFriend != u2.IsFriend)
            return u1.IsFriend ? -1 : 1;

        return string.Compare(u1.IRCUser.Name, u2.IRCUser.Name, StringComparison.InvariantCultureIgnoreCase);
    }
}