using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers
{
    class StringCommandHandler : CommandHandlerBase
    {
        public StringCommandHandler(string commandName, Action<string, string> commandHandler) : base(commandName)
        {
            this.commandHandler = commandHandler;
        }

        private Action<string, string> commandHandler;

        public override bool Handle(string sender, string message)
        {
            if (message.Length < CommandName.Length + 1)
                return false;

            if (message.StartsWith(CommandName))
            {
                string parameters = message.Substring(CommandName.Length + 1);

                commandHandler.Invoke(sender, parameters);
                //commandHandler(sender, message.Substring(CommandName.Length + 1));
                return true;
            }

            return false;
        }
    }
}
