using System;

namespace DTAClient.Domain.Multiplayer.LAN;

/// <summary>
/// A command handler that has no parameters.
/// </summary>
internal class ClientNoParamCommandHandler : LANClientCommandHandler
{
    public ClientNoParamCommandHandler(string commandName, Action commandHandler) : base(commandName)
    {
        this.commandHandler = commandHandler;
    }

    private readonly Action commandHandler;

    public override bool Handle(string message)
    {
        if (message != CommandName)
        {
            return false;
        }

        commandHandler();
        return true;
    }
}