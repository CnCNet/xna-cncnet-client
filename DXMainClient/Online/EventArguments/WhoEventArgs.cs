using System;

namespace DTAClient.Online.EventArguments
{
    public class WhoEventArgs : EventArgs
    {
        public WhoEventArgs(string hostName, string ident, string userName,
                            string extraInfo)
        {
            UserName = userName;
            ExtraInfo = extraInfo;
            HostName = hostName;
            Ident = ident;
        }

        public string UserName { get; private set; }
        public string ExtraInfo { get; private set; }
        public string HostName { get; private set; }
        public string Ident { get; private set; }
    }
}
