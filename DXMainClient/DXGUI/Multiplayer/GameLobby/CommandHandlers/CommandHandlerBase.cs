namespace DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers
{
    public abstract class CommandHandlerBase
    {
        public CommandHandlerBase(string commandName)
        {
            CommandName = commandName;
        }

        public string CommandName { get; private set; }

        public abstract bool Handle(string sender, string message);
    }
}
