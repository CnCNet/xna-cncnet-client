using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.Multiplayer.LAN
{
    public abstract class OneDirectionalCommandHandler
    {
        public OneDirectionalCommandHandler(string commandName)
        {
            CommandName = commandName;
        }

        public string CommandName { get; private set; }

        public abstract bool Handle(string message);
    }
}
