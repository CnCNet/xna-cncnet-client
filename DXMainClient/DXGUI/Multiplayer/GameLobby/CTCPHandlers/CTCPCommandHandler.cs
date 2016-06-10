using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Rampastring.Tools.Delegates;

namespace DTAClient.DXGUI.Multiplayer.GameLobby.CTCPHandlers
{
    public abstract class CTCPCommandHandler
    {
        public CTCPCommandHandler(string commandName)
        {
            CommandName = commandName;
        }

        public string CommandName { get; private set; }

        public abstract bool Handle(string sender, string message);
    }
}
