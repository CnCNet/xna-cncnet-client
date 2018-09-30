using System;
using System.Collections.Generic;

namespace DTAClient.Online
{
    /// <summary>
    /// A user on an IRC server.
    /// </summary>
    public class IRCUser : ICloneable
    {
        public IRCUser() { }

        public IRCUser(string name)
        {
            Name = name;
        }

        public IRCUser(string name, string ident, string host)
        {
            Name = name;
            Ident = ident;
            Hostname = host;
        }

        public string Name { get; set; }
        public string Ident { get; set; }
        public string Hostname { get; set; }
        public int GameID { get; set; } = -1;

        public List<string> Channels = new List<string>();

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool IsFriend { get; set; }
        public bool IsIgnored { get; set; }
    }
}
