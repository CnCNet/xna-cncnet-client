using System;

namespace DTAClient.Online
{
    /// <summary>
    /// A struct containing information on an IRC server.
    /// </summary>
    public struct Server
    {
        public Server(string host, string name, int[] ports)
        {
            Host = host;
            Name = name;
            Ports = ports;
        }

        public string Host;
        public string Name;
        public int[] Ports;

        public string Serialize() => FormattableString.Invariant($"{Host}|{Name}|{string.Join(",", Ports)}");

        public static Server Deserialize(string serialized)
        {
            string[] parts = serialized.Split('|');
            string host = parts[0];
            string name = parts[1];
            string[] portStrings = parts[2].Split(',');
            int[] ports = new int[portStrings.Length];

            for (int i = 0; i < portStrings.Length; i++)
                ports[i] = int.Parse(portStrings[i]);

            return new Server(host, name, ports);
        }
    }
}
