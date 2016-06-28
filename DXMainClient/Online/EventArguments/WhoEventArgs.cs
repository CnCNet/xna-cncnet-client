using System;

namespace DTAClient.Online.EventArguments
{
    public class WhoEventArgs : EventArgs
    {
        public WhoEventArgs(string userName, string extraInfo)
        {
            UserName = userName;
            ExtraInfo = extraInfo;
        }

        public string UserName { get; private set; }
        public string ExtraInfo { get; private set; }
    }
}
