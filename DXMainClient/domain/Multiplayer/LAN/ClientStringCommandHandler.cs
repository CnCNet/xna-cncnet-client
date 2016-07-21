using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.Multiplayer.LAN
{
    public class ClientStringCommandHandler : LANClientCommandHandler
    {
        public ClientStringCommandHandler(string commandName, Action<string> action) : base(commandName)
        {
            this.action = action;
        }

        Action<string> action;

        public override bool Handle(string message)
        {
            if (!message.StartsWith(CommandName))
                return false;

            action(message.Substring(CommandName.Length + 1));
            return true;
        }
    }
}
