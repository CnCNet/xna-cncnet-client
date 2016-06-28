using System;

namespace DTAClient.Online.EventArguments
{
    public class UserListEventArgs : EventArgs
    {
        public UserListEventArgs(string channelName, string[] userNames)
        {
            ChannelName = channelName;
            UserNames = userNames;
        }

        public string ChannelName { get; private set; }
        public string[] UserNames { get; private set; }
    }
}
