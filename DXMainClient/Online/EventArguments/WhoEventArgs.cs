using System;

namespace DTAClient.Online.EventArguments
{
    public class WhoEventArgs : EventArgs
    {
        public WhoEventArgs(string ident, string userName, string extraInfo)
        {
            Ident = ident;
            UserName = userName;
            ExtraInfo = extraInfo;
        }
        
        public string Ident { get; private set; }

        public string UserName { get; private set; }
        public string ExtraInfo { get; private set; }
    }
}
