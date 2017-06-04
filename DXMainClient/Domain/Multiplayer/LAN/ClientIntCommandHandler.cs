using System;

namespace DTAClient.Domain.Multiplayer.LAN
{
    public class ClientIntCommandHandler : LANClientCommandHandler
    {
        public ClientIntCommandHandler(string commandName, Action<int> action) : base(commandName)
        {
            this.action = action;
        }

        private Action<int> action;

        public override bool Handle(string message)
        {
            if (!message.StartsWith(CommandName))
                return false;

            if (message.Length < CommandName.Length + 2)
                return false;

            int value;
            bool success = int.TryParse(message.Substring(CommandName.Length + 1), out value);

            if (!success)
                return false;

            action(value);
            return true;
        }
    }
}
