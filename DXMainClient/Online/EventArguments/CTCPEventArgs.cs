using System;

namespace DTAClient.Online.EventArguments
{
    public class CTCPEventArgs : EventArgs
    {
        public CTCPEventArgs(string sender, string channelName, string ctcpMessage)
        {
            Sender = sender;
            ChannelName = channelName;
            CTCPMessage = ctcpMessage;
        }

        public string Sender { get; private set; }
        public string ChannelName { get; private set; }
        public string CTCPMessage { get; private set; }
    }
}
