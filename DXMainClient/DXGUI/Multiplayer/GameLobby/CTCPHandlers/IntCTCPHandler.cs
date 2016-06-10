using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Rampastring.Tools.Delegates;

namespace DTAClient.DXGUI.Multiplayer.GameLobby.CTCPHandlers
{
    public class IntCTCPHandler : CTCPCommandHandler
    {
        public IntCTCPHandler(string commandName, Action<string, int> handler) : base(commandName)
        {
            this.handler = handler;
        }

        Action<string, int> handler;

        public override bool Handle(string sender, string message)
        {
            if (message.Length < CommandName.Length + 1)
                return false;

            if (message.StartsWith(CommandName))
            {
                int value;
                bool success = int.TryParse(message.Substring(CommandName.Length + 1), out value);

                if (success)
                {
                    handler(sender, value);
                    return true;
                }
            }

            return false;
        }
    }
}
