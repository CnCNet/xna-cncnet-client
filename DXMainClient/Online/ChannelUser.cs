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

        public static int ChannelUserComparison(ChannelUser u1, ChannelUser u2)
        {
            if (u1.IsAdmin)
            {
                if (u2.IsAdmin)
                    return u1.IRCUser.Name.CompareTo(u2.IRCUser.Name);
                else
                    return -1;
            }

            if (u2.IsAdmin)
                return 1;

            return u1.IRCUser.Name.CompareTo(u2.IRCUser.Name);
        }
    }
}
