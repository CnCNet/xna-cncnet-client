using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online.EventArguments
{
    public class PrivateMessageEventArgs : EventArgs
    {
        public PrivateMessageEventArgs(string sender, string message)
        {
            Sender = sender;
            Message = message;
        }

        public string Sender { get; private set; }

        public string Message { get; private set; }
    }
}
