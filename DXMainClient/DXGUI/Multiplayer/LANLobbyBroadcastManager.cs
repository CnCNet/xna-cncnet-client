#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Rampastring.Tools;

using NetworkInterface = System.Net.NetworkInformation.NetworkInterface;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// Thread-safe manager for LAN lobby broadcasting and network communication.
    /// Encapsulates socket management, broadcast interface discovery, message sending,
    /// and network listening to ensure thread-safe operations.
    /// </summary>
    internal class LANLobbyBroadcastManager : IDisposable
    {
        private readonly object socketLock = new();
        private readonly ConcurrentDictionary<string, PlayerNetworkInterface> broadcastInterfaces = new();
        private readonly Encoding encoding;
        private readonly int lobbyPort;

        private Socket? socket;
        private Thread? listener;
        private int disposed = 0;

        /// <summary>
        /// Event raised when a network message is received.
        /// The callback is invoked on the main thread (via AddCallback).
        /// </summary>
        public event EventHandler<NetworkMessageReceivedEventArgs>? MessageReceived;

        /// <summary>
        /// Delegate for adding callbacks to the main thread.
        /// This is typically provided by the WindowManager.
        /// </summary>
        private readonly Action<Action<string, IPEndPoint>, string, IPEndPoint>? addCallback;

        /// <summary>
        /// Record for storing network interface information.
        /// </summary>
        /// <param name="LocalIP">The local IP address of this interface.</param>
        /// <param name="Broadcast">The broadcast endpoint for this interface.</param>
        private record PlayerNetworkInterface(IPAddress LocalIP, IPEndPoint Broadcast);

        /// <summary>
        /// Initializes a new instance of the LANLobbyBroadcastManager class.
        /// </summary>
        /// <param name="lobbyPort">The UDP port to bind for LAN lobby communication.</param>
        /// <param name="encoding">The text encoding to use for messages (typically UTF-8).</param>
        public LANLobbyBroadcastManager(int lobbyPort, Encoding encoding)
        {
            this.lobbyPort = lobbyPort;
            this.encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        }

        /// <summary>
        /// Gets whether the socket is successfully initialized and bound.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                lock (socketLock)
                {
                    return socket != null && socket.IsBound;
                }
            }
        }

        /// <summary>
        /// Initializes the socket, binds it to the lobby port, and starts listening for messages.
        /// </summary>
        public void Initialize()
        {
            lock (socketLock)
            {
                // Clean up any existing socket
                if (socket != null)
                {
                    try
                    {
                        socket.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed
                    }
                    socket = null;
                }

                // Clear broadcast interfaces
                broadcastInterfaces.Clear();

                Logger.Log("Creating LAN socket.");

                try
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                    {
                        EnableBroadcast = true
                    };
                    socket.Bind(new IPEndPoint(IPAddress.Any, lobbyPort));
                    AddBroadcastInterfaces();
                }
                catch (SocketException ex)
                {
                    Logger.Log("Creating LAN socket failed! Message: " + ex.ToString());
                    throw;
                }

                Logger.Log("Starting LAN broadcast message listener.");
                listener = new Thread(new ThreadStart(Listen));
                listener.Start();
            }
        }

        /// <summary>
        /// Discovers and adds all available network interfaces for broadcasting.
        /// This method scans all network interfaces and identifies those with valid IPv4 addresses.
        /// </summary>
        private void AddBroadcastInterfaces()
        {
            Logger.Log("Discovering broadcast interfaces.");

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface iface in interfaces)
            {
                Logger.Log($"Examining interface: {iface.Name}, Type: {iface.NetworkInterfaceType}, Status: {iface.OperationalStatus}");

                IPInterfaceProperties prop = iface.GetIPProperties();
                UnicastIPAddressInformation? info = prop.UnicastAddresses.FirstOrDefault(info =>
                    info.Address.AddressFamily == AddressFamily.InterNetwork);

                if (info == null || info.IPv4Mask == null)
                    continue;

                // Note: even if an interface is down, we may still want to broadcast on it in case it's up later. Therefore, there is no check for OperationalStatus.

                IPAddress localIPAddress = info.Address;
                byte[] ipBytes = localIPAddress.GetAddressBytes();
                byte[] maskBytes = info.IPv4Mask.GetAddressBytes();
                byte[] broadcastBytes = new byte[ipBytes.Length];
                for (int i = 0; i < ipBytes.Length; i++)
                {
                    broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                }
                IPAddress broadcastIP = new IPAddress(broadcastBytes);

                string key = localIPAddress.ToString();
                var netIf = new PlayerNetworkInterface(localIPAddress, new IPEndPoint(broadcastIP, lobbyPort));
                broadcastInterfaces[key] = netIf;
            }

            if (broadcastInterfaces.IsEmpty)
            {
                Logger.Log("Warning: No broadcast interfaces found! LAN lobby broadcasting will not function. " +
                    "Please ensure that your network adapters are enabled and have valid IPv4 addresses.");
                return;
            }
        }

        /// <summary>
        /// Sends a message to all broadcast interfaces.
        /// If an interface fails, it will be removed from the broadcast list.
        /// If all interfaces are removed, the method attempts to refresh the interface list.
        /// </summary>
        /// <param name="message">The message to broadcast.</param>
        /// <returns>True if the message was sent successfully, false if the socket is not initialized.</returns>
        public bool SendMessage(string message)
        {
            lock (socketLock)
            {
                if (socket == null || !socket.IsBound)
                    return false;

                byte[] buffer = encoding.GetBytes(message);

                // If there is a socket error when sending to an interface, remove that interface.
                // This is rare, so keep `failedInterfaces` null by default to avoid allocating a list on every SendMessage.
                List<PlayerNetworkInterface>? failedInterfaces = null;

                if (broadcastInterfaces.IsEmpty)
                {
                    Logger.Log("Warning: No broadcast interfaces available in SendMessage!");
                }

                foreach ((string key, PlayerNetworkInterface networkInterface) in broadcastInterfaces)
                {
                    try
                    {
                        _ = socket.SendTo(buffer, networkInterface.Broadcast);
                    }
                    catch (SocketException)
                    {
                        failedInterfaces ??= [];
                        failedInterfaces.Add(networkInterface);
                    }
                }

                if (failedInterfaces != null)
                {
                    foreach (var key in failedInterfaces.Select(iface => iface.LocalIP.ToString()))
                    {
                        _ = broadcastInterfaces.TryRemove(key, out _);
                    }
                }

                // If no broadcast interfaces remain, we cannot continue using the socket. Refresh the interfaces.
                if (broadcastInterfaces.IsEmpty)
                {
                    Logger.Log("No broadcast interfaces remain; refreshing interfaces.");
                    AddBroadcastInterfaces();
                }

                return true;
            }
        }

        /// <summary>
        /// Background thread that listens for incoming UDP messages.
        /// Received messages are dispatched to the MessageReceived event on the main thread.
        /// </summary>
        private void Listen()
        {
            try
            {
                while (true)
                {
                    Socket? currentSocket;
                    lock (socketLock)
                    {
                        currentSocket = socket;
                    }

                    if (currentSocket == null)
                        break;

                    EndPoint endPoint = new IPEndPoint(IPAddress.Any, lobbyPort);
                    byte[] buffer = new byte[4096];
                    int receivedBytes = currentSocket.ReceiveFrom(buffer, ref endPoint);

                    IPEndPoint ipEndPoint = (IPEndPoint)endPoint;
                    string data = encoding.GetString(buffer, 0, receivedBytes);

                    if (string.IsNullOrEmpty(data))
                        continue;

                    HandleNetworkMessage(data, ipEndPoint);
                }
            }
            catch (Exception ex)
            {
                if (ex is SocketException socketEx && socketEx.SocketErrorCode == SocketError.Interrupted)
                {
                    // Do nothing; this is the expected way for the listener thread to end.
                }
                else
                {
                    Logger.Log("LAN socket listener: exception: " + ex.ToString());
                }
            }
        }

        /// <summary>
        /// Handles a received network message by raising the MessageReceived event.
        /// </summary>
        private void HandleNetworkMessage(string data, IPEndPoint endPoint)
        {
            MessageReceived?.Invoke(this, new NetworkMessageReceivedEventArgs(data, endPoint));
        }

        /// <summary>
        /// Timeout in milliseconds for waiting for the listener thread to terminate during shutdown.
        /// </summary>
        private const int LISTENER_SHUTDOWN_TIMEOUT_MS = 1000;

        /// <summary>
        /// Closes the socket and stops the listening thread.
        /// </summary>
        public void Shutdown()
        {
            lock (socketLock)
            {
                if (socket != null && socket.IsBound)
                {
                    try
                    {
                        socket.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed
                    }
                }

                socket = null;
            }

            if (listener != null)
            {
                bool listenerTerminated = listener.Join(millisecondsTimeout: LISTENER_SHUTDOWN_TIMEOUT_MS);
                if (!listenerTerminated)
                    Logger.Log("Failed to shut down listener after timeout!");

                listener = null;
            }

            // Clear broadcast interfaces
            broadcastInterfaces.Clear();
        }

        /// <summary>
        /// Gets the count of active broadcast interfaces.
        /// </summary>
        public int BroadcastInterfaceCount => broadcastInterfaces.Count;

        /// <summary>
        /// Disposes the broadcast manager and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
            {
                Shutdown();
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Event arguments for network message received events.
    /// </summary>
    internal class NetworkMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The received message data.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// The endpoint from which the message was received.
        /// </summary>
        public IPEndPoint EndPoint { get; }

        public NetworkMessageReceivedEventArgs(string data, IPEndPoint endPoint)
        {
            Data = data;
            EndPoint = endPoint;
        }
    }
}
