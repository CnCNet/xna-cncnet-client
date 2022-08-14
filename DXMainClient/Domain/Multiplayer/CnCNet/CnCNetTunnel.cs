using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A CnCNet tunnel server.
    /// </summary>
    internal sealed class CnCNetTunnel
    {
        private const int PING_PACKET_SEND_SIZE = 50;
        private const int PING_PACKET_RECEIVE_SIZE = 12;
        private const int PING_TIMEOUT = 1000;

        /// <summary>
        /// Parses a formatted string that contains the tunnel server's 
        /// information into a CnCNetTunnel instance.
        /// </summary>
        /// <param name="str">The string that contains the tunnel server's information.</param>
        /// <returns>A CnCNetTunnel instance parsed from the given string.</returns>
        public static CnCNetTunnel Parse(string str)
        {
            // For the format, check http://cncnet.org/master-list

            try
            {
                var tunnel = new CnCNetTunnel();
                string[] parts = str.Split(';');

                string address = parts[0];
                string[] detailedAddress = address.Split(':');
                int version = int.Parse(parts[10]);

                tunnel.Address = detailedAddress[0];
                tunnel.Port = int.Parse(detailedAddress[1]);
                tunnel.Country = parts[1];
                tunnel.CountryCode = parts[2];
                tunnel.Name = parts[3] + " V" + version;
                tunnel.RequiresPassword = parts[4] != "0";
                tunnel.Clients = int.Parse(parts[5], CultureInfo.InvariantCulture);
                tunnel.MaxClients = int.Parse(parts[6], CultureInfo.InvariantCulture);
                int status = int.Parse(parts[7], CultureInfo.InvariantCulture);
                tunnel.Official = status == 2;
                if (!tunnel.Official)
                    tunnel.Recommended = status == 1;

                tunnel.Latitude = double.Parse(parts[8], CultureInfo.InvariantCulture);
                tunnel.Longitude = double.Parse(parts[9], CultureInfo.InvariantCulture);
                tunnel.Version = version;
                tunnel.Distance = double.Parse(parts[11], CultureInfo.InvariantCulture);

                return tunnel;
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is OverflowException || ex is IndexOutOfRangeException)
                {
                    Logger.Log("Parsing tunnel information failed: " + ex.Message + Environment.NewLine + "Parsed string: " + str);
                    return null;
                }

                throw;
            }
        }

        private string _ipAddress;
        public string Address
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                if (IPAddress.TryParse(_ipAddress, out IPAddress address))
                    IPAddress = address;
            }
        }

        public IPAddress IPAddress { get; private set; }

        public int Port { get; private set; }
        public string Country { get; private set; }
        public string CountryCode { get; private set; }
        public string Name { get; private set; }
        public bool RequiresPassword { get; private set; }
        public int Clients { get; private set; }
        public int MaxClients { get; private set; }
        public bool Official { get; private set; }
        public bool Recommended { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public int Version { get; private set; }
        public double Distance { get; private set; }
        public int PingInMs { get; set; } = -1;

        public event EventHandler Pinged;

        /// <summary>
        /// Gets a list of player ports to use from a specific tunnel server.
        /// </summary>
        /// <returns>A list of player ports to use.</returns>
        public List<int> GetPlayerPortInfo(int playerCount)
        {
            if (Version != Constants.TUNNEL_VERSION_2)
                throw new InvalidOperationException("GetPlayerPortInfo only works with version 2 tunnels.");

            try
            {
                Logger.Log($"Contacting tunnel at {Address}:{Port}");

                string addressString = $"http://{Address}:{Port}/request?clients={playerCount}";
                Logger.Log($"Downloading from {addressString}");

                using (var client = new ExtendedWebClient(Constants.TUNNEL_CONNECTION_TIMEOUT))
                {
                    string data = client.DownloadString(addressString);

                    data = data.Replace("[", string.Empty);
                    data = data.Replace("]", string.Empty);

                    string[] portIDs = data.Split(',');
                    List<int> playerPorts = new List<int>();

                    foreach (string _port in portIDs)
                    {
                        playerPorts.Add(Convert.ToInt32(_port));
                        Logger.Log($"Added port {_port}");
                    }

                    return playerPorts;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to connect to the specified tunnel server. Returned error message: " + ex.Message);
            }

            return new List<int>();
        }

        public void UpdatePing()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.SendTimeout = PING_TIMEOUT;
            socket.ReceiveTimeout = PING_TIMEOUT;

            try
            {
                byte[] buffer = new byte[PING_PACKET_SEND_SIZE];
                EndPoint ep = new IPEndPoint(IPAddress, Port);
                long ticks = DateTime.Now.Ticks;
                socket.SendTo(buffer, ep);

                buffer = new byte[PING_PACKET_RECEIVE_SIZE];
                socket.ReceiveFrom(buffer, ref ep);

                ticks = DateTime.Now.Ticks - ticks;
                PingInMs = new TimeSpan(ticks).Milliseconds;
            }
            catch (SocketException ex)
            {
                Logger.Log($"Failed to ping tunnel {Name} ({Address}:{Port}). Message: {ex.Message}");

                PingInMs = -1;
            }

            Pinged?.Invoke(this, EventArgs.Empty);
        }
    }
}