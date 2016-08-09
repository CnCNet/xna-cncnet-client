namespace DTAClient.Domain.Multiplayer.LAN
{
    public abstract class LANServerCommandHandler
    {
        public LANServerCommandHandler(string commandName)
        {
            CommandName = commandName;
        }

        public string CommandName { get; private set; }

        public abstract bool Handle(LANPlayerInfo pInfo, string message);
    }
}
