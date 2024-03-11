using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;

public class IntCommandHandler : CommandHandlerBase
{
    public IntCommandHandler(string commandName, Action<string, int> handler) : base(commandName)
    {
        this.handler = handler;
    }

    private readonly Action<string, int> handler;

    public override bool Handle(string sender, string message)
    {
        if (message.Length < CommandName.Length + 1)
        {
            return false;
        }

        if (message.StartsWith(CommandName))
        {
            bool success = int.TryParse(message[(CommandName.Length + 1)..], out int value);

            if (success)
            {
                handler(sender, value);
                return true;
            }
        }

        return false;
    }
}