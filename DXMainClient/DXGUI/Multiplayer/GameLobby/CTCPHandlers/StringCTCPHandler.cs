using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Rampastring.Tools.Delegates;

namespace DTAClient.DXGUI.Multiplayer.GameLobby.CTCPHandlers
{
    class StringCTCPHandler : CTCPCommandHandler
    {
        public StringCTCPHandler(string commandName, Action<string, string> commandHandler) : base(commandName)
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
                commandHandler(sender, message.Substring(CommandName.Length + 1));
                return true;
            }

            return false;
        }
    }
}
