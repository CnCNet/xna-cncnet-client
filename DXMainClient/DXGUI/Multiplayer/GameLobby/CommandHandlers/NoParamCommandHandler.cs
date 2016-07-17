using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers
{
    /// <summary>
    /// A command handler that handles a command that has no parameter aside from the sender.
    /// </summary>
    public class NoParamCommandHandler : CommandHandlerBase
    {
        public NoParamCommandHandler(string commandName, Action<string> commandHandler) : base(commandName)
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
