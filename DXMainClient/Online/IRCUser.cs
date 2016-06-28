namespace DTAClient.Online
{
    public class IRCUser
    {
        public IRCUser() { }

        public IRCUser(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public bool IsAdmin { get; set; }

        int _gameId = -1;

        public int GameID
        {
            get { return _gameId; }
            set { _gameId = value; }
        }
    }
}
