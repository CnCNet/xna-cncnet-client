using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby.CTCPHandlers
{
    public class NoParamCTCPHandler : CTCPCommandHandler
    {
        public NoParamCTCPHandler(string commandName, Action<string> commandHandler) : base(commandName)
        {
            this.commandHandler = commandHandler;
        }

        Action<string> commandHandler;

        public override bool Handle(string sender, string message)
        {
            if (message == CommandName)
            {
                commandHandler(sender);
                return true;
            }

            return false;
        }
    }
}
