using System;

namespace DTAClient.Online.EventArguments
{
    public class PrivateCTCPEventArgs : EventArgs
    {
        public PrivateCTCPEventArgs(string sender, string message)
        {
            Sender = sender;
            Message = message;
        }

        public string Sender { get; private set; }

        public string Message { get; private set; }
    }
}
