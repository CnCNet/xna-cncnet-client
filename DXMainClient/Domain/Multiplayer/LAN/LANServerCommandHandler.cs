namespace DTAClient.Domain.Multiplayer.LAN
{
    internal abstract class LANServerCommandHandler
    {
        public LANServerCommandHandler(string commandName)
        {
            CommandName = commandName;
        }

        public string CommandName { get; private set; }

        public abstract bool Handle(LANPlayerInfo pInfo, string message);
    }
}