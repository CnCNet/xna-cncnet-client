using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ClientCore.LAN
{
    /// <summary>
    /// A player in a LAN game.
    /// </summary>
    public class LANPlayer
    {
        public IPAddress Address { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public bool IsInGame { get; set; }
        public DateTime LastStatusTime { get; set; }
        public string GameIdentifier { get; set; }
        public int Side { get; set; }
        public int Color { get; set; }
        public int Team { get; set; }
        public int Start { get; set; }
        public volatile TcpClient Client;
        public volatile NetworkStream Stream;
        public string Hash { get; set; }
        public string Version { get; set; }

        private static readonly object locker = new object();

        private volatile bool verified = false;
        public bool Verified
        {
            get { lock (locker) { return verified; } }
            set { lock (locker) { verified = true; } }
        }
        public bool Ready { get; set; }
        public string UnhandledMessagePart { get; set; }

        public void SendMessage(string message)
        {
            Encoding encoder = Encoding.GetEncoding(1252);
            byte[] buffer = encoder.GetBytes(message + "^");

            try
            {
                Stream.Write(buffer, 0, buffer.Length);
                Stream.Flush();
            }
            catch
            {
            }
        }
    }
}
