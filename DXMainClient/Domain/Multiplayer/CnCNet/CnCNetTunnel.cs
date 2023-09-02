﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A CnCNet tunnel server.
    /// </summary>
    internal sealed class CnCNetTunnel
    {
        private const int PING_PACKET_SEND_SIZE = 50;
        private const int PING_TIMEOUT = 1000;
        private const int HASH_LENGTH = 10;

        private string ipAddress;
        private string hash;

        /// <summary>
        /// Parses a formatted string that contains the tunnel server's
        /// information into a CnCNetTunnel instance.
        /// </summary>
        /// <param name="str">The string that contains the tunnel server's information.</param>
        /// <returns>A CnCNetTunnel instance parsed from the given string.</returns>
        public static CnCNetTunnel Parse(string str)
        {
            // For the format, check https://cncnet.org/master-list
            try
            {
                var tunnel = new CnCNetTunnel();
                string[] parts = str.Split(';');
                string addressAndPort = parts[0];
                string secondaryAddress = parts.Length > 12 ? parts[12] : null;
                int version = int.Parse(parts[10], CultureInfo.InvariantCulture);
                string primaryAddress = addressAndPort[..addressAndPort.LastIndexOf(':')];
                var primaryIpAddress = IPAddress.Parse(primaryAddress);
                IPAddress secondaryIpAddress = string.IsNullOrWhiteSpace(secondaryAddress) ? null : IPAddress.Parse(secondaryAddress);

                if (Socket.OSSupportsIPv6 && primaryIpAddress.AddressFamily is AddressFamily.InterNetworkV6)
                {
                    tunnel.Address = primaryIpAddress.ToString();
                }
                else if (Socket.OSSupportsIPv6 && secondaryIpAddress?.AddressFamily is AddressFamily.InterNetworkV6)
                {
                    tunnel.Address = secondaryIpAddress.ToString();
                }
                else if (Socket.OSSupportsIPv4 && primaryIpAddress.AddressFamily is AddressFamily.InterNetwork)
                {
                    tunnel.Address = primaryIpAddress.ToString();
                }
                else if (Socket.OSSupportsIPv4 && secondaryIpAddress?.AddressFamily is AddressFamily.InterNetwork)
                {
                    tunnel.Address = secondaryIpAddress.ToString();
                }
                else
                {
                    throw new($"No supported IP address found ({nameof(Socket.OSSupportsIPv6)}={Socket.OSSupportsIPv6}," +
                              $" {nameof(Socket.OSSupportsIPv4)}={Socket.OSSupportsIPv4}) for {str}.");
                }

                tunnel.IPAddresses = new List<IPAddress> { primaryIpAddress };

                if (secondaryIpAddress is not null)
                    tunnel.IPAddresses.Add(secondaryIpAddress);

                tunnel.Port = int.Parse(addressAndPort[(addressAndPort.LastIndexOf(':') + 1)..], CultureInfo.InvariantCulture);
                tunnel.Country = parts[1];
                tunnel.CountryCode = parts[2];
                tunnel.Name = parts[3];
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
            catch (Exception ex) when (ex is FormatException or OverflowException or IndexOutOfRangeException)
            {
                ProgramConstants.LogException(ex, "Parsing tunnel information failed. Parsed string: " + str);
                return null;
            }
        }

        public string Address
        {
            get => ipAddress;
            private set
            {
                ipAddress = value;

                if (IPAddress.TryParse(ipAddress, out IPAddress address))
                    IPAddress = address;
            }
        }

        public IPAddress IPAddress { get; private set; }

        public List<IPAddress> IPAddresses { get; private set; }

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

        public int PingInMs { get; private set; } = -1;

        public string Hash
        {
            get
            {
                return hash ??= Utilities.CalculateSHA1ForString(FormattableString.Invariant($"{Version}{CountryCode}{Name}{Official}{Recommended}"))[..HASH_LENGTH];
            }
        }

        /// <summary>
        /// Gets a list of player ports to use from a specific tunnel server.
        /// </summary>
        /// <returns>A list of player ports to use.</returns>
        public async ValueTask<List<int>> GetPlayerPortInfoAsync(int playerCount)
        {
            if (Version != Constants.TUNNEL_VERSION_2)
                throw new InvalidOperationException($"{nameof(GetPlayerPortInfoAsync)} only works with version {Constants.TUNNEL_VERSION_2} tunnels.");

            try
            {
                string addressString = $"{Uri.UriSchemeHttp}://{Address}:{Port}/request?clients={playerCount}";

                Logger.Log($"Contacting tunnel at {addressString}");

                string data = await Constants.CnCNetHttpClient.GetStringAsync(addressString);

                data = data.Replace("[", string.Empty);
                data = data.Replace("]", string.Empty);

                string[] portIDs = data.Split(',');
                var playerPorts = new List<int>();

                foreach (string port in portIDs)
                {
                    playerPorts.Add(Convert.ToInt32(port, CultureInfo.InvariantCulture));
                    Logger.Log($"Added port {port}");
                }

                return playerPorts;
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Unable to connect to the specified tunnel server.");
            }

            return new List<int>();
        }

        public async ValueTask UpdatePingAsync()
        {
            using var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            socket.SendTimeout = PING_TIMEOUT;
            socket.ReceiveTimeout = PING_TIMEOUT;

            try
            {
                EndPoint ep = new IPEndPoint(IPAddress, Port);
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(PING_PACKET_SEND_SIZE);
                Memory<byte> buffer = memoryOwner.Memory[..PING_PACKET_SEND_SIZE];
                long ticks = DateTime.Now.Ticks;

                await socket.SendToAsync(buffer, SocketFlags.None, ep);
                await socket.ReceiveFromAsync(buffer, SocketFlags.None, ep);

                ticks = DateTime.Now.Ticks - ticks;
                PingInMs = new TimeSpan(ticks).Milliseconds;
            }
            catch (SocketException ex)
            {
                ProgramConstants.LogException(ex, $"Failed to ping tunnel {Name} ({Address}:{Port}).");

                PingInMs = -1;
            }
        }
    }
}