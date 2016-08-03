using System;

namespace DTAClient.Domain.Multiplayer.LAN
{
    public class ServerNoParamCommandHandler : LANServerCommandHandler
    {
        public ServerNoParamCommandHandler(string commandName,
            Action<LANPlayerInfo> handler) : base(commandName)
        {
            this.handler = handler;
        }

        Action<LANPlayerInfo> handler;

        public override bool Handle(LANPlayerInfo pInfo, string message)
        {
            if (message == CommandName)
            {
                handler(pInfo);
                return true;
            }

            return false;
        }
    }
}
