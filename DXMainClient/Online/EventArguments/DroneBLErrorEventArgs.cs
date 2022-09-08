using System;

namespace DTAClient.Online.EventArguments
{
    public class DroneBLErrorEventArgs : EventArgs
    {
        public string Message { get; }

        public DroneBLErrorEventArgs(string message)
        {
            Message = message;
        }
    }
}
