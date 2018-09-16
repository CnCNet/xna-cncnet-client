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

        public IRCUser(string name, string host)
        {
            Name = name;
            Hostname = host;
        }

        public string Name { get; set; }

        public string Hostname { get; set; }

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

        public bool IsFriend { get; set; }
        public bool IsIgnored { get; set; }
    }
}
