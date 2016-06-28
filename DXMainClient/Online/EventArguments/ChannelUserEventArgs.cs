using System;

namespace DTAClient.Online.EventArguments
{
    public class ChannelUserEventArgs : EventArgs
    {
        public ChannelUserEventArgs(string channelName, string userName)
        {
            ChannelName = channelName;
            UserName = userName;
        }

        public string ChannelName { get; private set; }
        public string UserName { get; private set; }
    }
}
