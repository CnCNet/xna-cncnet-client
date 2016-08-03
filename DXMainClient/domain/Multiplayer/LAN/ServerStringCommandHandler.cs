using System;

namespace DTAClient.Domain.Multiplayer.LAN
{
    public class ServerStringCommandHandler : LANServerCommandHandler
    {
        public ServerStringCommandHandler(string commandName,
            Action<LANPlayerInfo, string> handler)
            : base(commandName)
        {
            this.handler = handler;
        }

        Action<LANPlayerInfo, string> handler;

        public override bool Handle(LANPlayerInfo pInfo, string message)
        {
            if (!message.StartsWith(CommandName) ||
                message.Length <= CommandName.Length + 1)
                return false;

            handler(pInfo, message);
            return true;
        }
    }
}
