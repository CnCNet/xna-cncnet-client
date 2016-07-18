using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.Multiplayer.LAN
{
    /// <summary>
    /// A command handler that has no parameters.
    /// </summary>
    class ODNoArgCommandHandler : OneDirectionalCommandHandler
    {
        public ODNoArgCommandHandler(string commandName, Action commandHandler) : base(commandName)
        {
            this.commandHandler = commandHandler;
        }

        Action commandHandler;

        public override bool Handle(string message)
        {
            if (message != CommandName)
                return false;

            commandHandler();
            return true;
        }
    }
}
