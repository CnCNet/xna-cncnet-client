using System;

namespace DTAClient.Online.EventArguments
{
    public class ConnectionLostEventArgs : EventArgs
    {
        public ConnectionLostEventArgs(string reason)
        {
            Reason = reason;
        }
        public string Reason { get; private set; }
    }
}
