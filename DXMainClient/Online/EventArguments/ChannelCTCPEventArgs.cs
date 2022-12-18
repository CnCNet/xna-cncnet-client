using System;

namespace DTAClient.Online.EventArguments
{
    public class ChannelCTCPEventArgs : EventArgs
    {
        public ChannelCTCPEventArgs(string userName, string message, ChannelUser channelUser)
        {
            UserName = userName;
            Message = message;
            ChannelUser = channelUser;
        }

        public string UserName { get; }
        public string Message { get; }
        
        public ChannelUser ChannelUser { get; }
    }
}
