using System;

namespace DTAClient.Online.EventArguments
{
    public class UserAwayEventArgs : EventArgs
    {
        public UserAwayEventArgs(string user, string awayReason)
        {
            UserName = user;
            AwayReason = awayReason;
        }

        public string UserName { get; private set; }

        public string AwayReason { get; private set; }
    }
}
