using System;

namespace DTAClient.Online.EventArguments
{
    public class UserJoinedEventArgs : EventArgs
    {
        public UserJoinedEventArgs(string channel, string host, string ident, string user)
        {
            ChannelName = channel;
            HostName = host;
            Ident = ident;
            UserName = user;
        }
        public string ChannelName { get; private set; }
        public string UserName { get; private set; }
        public string HostName { get; private set; }
        public string Ident { get; private set; }
    }
}
