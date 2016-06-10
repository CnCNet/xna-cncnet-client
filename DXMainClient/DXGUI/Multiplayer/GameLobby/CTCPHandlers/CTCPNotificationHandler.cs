using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.DXGUI.Multiplayer.GameLobby.CTCPHandlers
{
    public class CTCPNotificationHandler : CTCPCommandHandler
    {
        public CTCPNotificationHandler(string commandName, Action<string, Action> action,
            Action innerAction) : base(commandName)
        {
            this.action = action;
            this.innerAction = innerAction;
        }

        Action<string, Action> action;
        Action innerAction;

        public override bool Handle(string sender, string message)
        {
            if (message == CommandName)
            {
                action(sender, innerAction);
                return true;
            }

            return false;
        }
    }
}
