using System;
using System.Collections.Generic;

namespace DTAClient.Online
{
    public class IRCUser : ICloneable
    {
        public IRCUser() { }

        public IRCUser(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        int _gameId = -1;

        public int GameID
        {
            get { return _gameId; }
            set { _gameId = value; }
        }

        public List<string> Channels = new List<string>();

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
