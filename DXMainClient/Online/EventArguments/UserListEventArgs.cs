using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online.EventArguments
{
    public class UserListEventArgs : EventArgs
    {
        public UserListEventArgs(string channelName, string[] userNames)
        {
            ChannelName = channelName;
            UserNames = userNames;
        }

        public string ChannelName { get; private set; }
        public string[] UserNames { get; private set; }
    }
}
