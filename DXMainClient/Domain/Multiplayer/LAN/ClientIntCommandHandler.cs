using System;

namespace DTAClient.Domain.Multiplayer.LAN;

public class ClientIntCommandHandler : LANClientCommandHandler
{
    public ClientIntCommandHandler(string commandName, Action<int> action) : base(commandName)
    {
        this.action = action;
    }

    private readonly Action<int> action;

    public override bool Handle(string message)
    {
        if (!message.StartsWith(CommandName))
        {
            return false;
        }

        if (message.Length < CommandName.Length + 2)
        {
            return false;
        }

        bool success = int.TryParse(message[(CommandName.Length + 1)..], out int value);

        if (!success)
        {
            return false;
        }

        action(value);
        return true;
    }
}