using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.Multiplayer.LAN
{
    public class NetworkMessageEventArgs : EventArgs
    {
        public NetworkMessageEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
