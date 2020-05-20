namespace DTAClient.Online
{
    /// <summary>
    /// A struct containing information on an IRC server.
    /// </summary>
    public struct Server
    {
        public Server(string host, string name, int[] ports, bool useSsl = false)
        {
            Host = host;
            Name = name;
            Ports = ports;
            UseSsl = useSsl;
        }

        public string Host;
        public string Name;
        public int[] Ports;
        public bool UseSsl { get; }
    }
}
