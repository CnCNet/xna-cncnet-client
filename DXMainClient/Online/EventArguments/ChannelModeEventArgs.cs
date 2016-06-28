using System;

namespace DTAClient.Online.EventArguments
{
    public class ChannelModeEventArgs : EventArgs
    {
        public ChannelModeEventArgs(string userName, string modeString)
        {
            UserName = userName;
            ModeString = modeString;
        }

        public string UserName { get; set; }
        public string ModeString { get; set; }
    }
}
