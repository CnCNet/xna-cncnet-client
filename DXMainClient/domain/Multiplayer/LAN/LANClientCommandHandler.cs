namespace DTAClient.Domain.Multiplayer.LAN
{
    public abstract class LANClientCommandHandler
    {
        public LANClientCommandHandler(string commandName)
        {
            CommandName = commandName;
        }

        public string CommandName { get; private set; }

        public abstract bool Handle(string message);
    }
}
