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

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// Thread-safe manager for LAN lobby broadcasting and network communication.
/// Encapsulates socket management, broadcast interface discovery, message sending,
/// and network listening to ensure thread-safe operations.
/// 
/// This class broadcasts messages to all available network interfaces, and provides a message ID based de-duplication mechanism.
/// By broadcasting to all interfaces, it ensures that messages reach all clients.
/// Otherwise, Sending UDP to 255.255.255.255 typically uses the network card with the lowest metric -- this does not fit the use case of players using a dedicated interface for gaming, such as VPNs or a secondary router without Internet access.
/// </summary>
internal class LANLobbyBroadcastManager : IDisposable
{
    private readonly object socketLock = new();
    private readonly ConcurrentDictionary<string, PlayerNetworkInterface> broadcastInterfaces = new();
    private readonly Encoding encoding;
    private readonly int lobbyPort;

    private Socket? socket;
    private Thread? listener;
    private Thread? interfaceRefresher;
    private volatile bool stopRefresher = false;
    private int disposed = 0;

    /// <summary>
    /// Event raised when a network message is received.
    /// The event is raised on the listener thread; subscribers are responsible
    /// for marshaling to the main/UI thread if required.
    /// </summary>
    public event EventHandler<LANLobbyBroadcastMessageReceivedEventArgs>? MessageReceived;

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
                
                // Discover initial broadcast interfaces
                var initialInterfaces = DiscoverBroadcastInterfaces(lobbyPort);
                foreach (var (key, netIf) in initialInterfaces)
                {
                    broadcastInterfaces[key] = netIf;
                }
            }
            catch (SocketException ex)
            {
                Logger.Log("Creating LAN socket failed! Message: " + ex.ToString());
                throw;
            }

            // Reset stop flag for the refresher thread
            stopRefresher = false;

            Logger.Log("Starting LAN broadcast message listener.");
            listener = new Thread(new ThreadStart(Listen))
            {
                IsBackground = true
            };
            listener.Start();

            Logger.Log("Starting network interface refresh thread.");
            interfaceRefresher = new Thread(new ThreadStart(RefreshInterfacesPeriodically))
            {
                IsBackground = true
            };
            interfaceRefresher.Start();
        }
    }

    /// <summary>
    /// Discovers all available network interfaces for broadcasting.
    /// This method scans only "up" network interfaces and identifies those with valid IPv4 addresses.
    /// </summary>
    /// <param name="port">The port to use for broadcast endpoints.</param>
    /// <returns>A dictionary of network interfaces keyed by their local IP address.</returns>
    private static Dictionary<string, PlayerNetworkInterface> DiscoverBroadcastInterfaces(int port)
    {
        Logger.Log("Discovering broadcast interfaces.");

        var discoveredInterfaces = new Dictionary<string, PlayerNetworkInterface>();
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface iface in interfaces)
        {
            // Only consider interfaces that are operational (up)
            if (iface.OperationalStatus != OperationalStatus.Up)
                continue;

            IPInterfaceProperties prop = iface.GetIPProperties();
            UnicastIPAddressInformation? info = prop.UnicastAddresses.FirstOrDefault(info =>
                info.Address.AddressFamily == AddressFamily.InterNetwork);

            if (info == null || info.IPv4Mask == null)
                continue;

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
            var netIf = new PlayerNetworkInterface(localIPAddress, new IPEndPoint(broadcastIP, port));
            discoveredInterfaces[key] = netIf;
        }

        if (discoveredInterfaces.Count == 0)
        {
            Logger.Log("Warning: No broadcast interfaces found! LAN lobby broadcasting will not function. " +
                "Please ensure that your network adapters are enabled and have valid IPv4 addresses.");
        }

        return discoveredInterfaces;
    }

    /// <summary>
    /// Sends a message to all broadcast interfaces.
    /// Failed interfaces are logged but not removed from the broadcast list.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <returns>True if the message was sent successfully to at least one interface, false if the socket is not initialized or all interfaces fail.</returns>
    public bool SendMessage(string message)
    {
        lock (socketLock)
        {
            if (socket == null || !socket.IsBound)
                return false;

            byte[] buffer = encoding.GetBytes(message);

            if (broadcastInterfaces.IsEmpty)
            {
                Logger.Log("Warning: No broadcast interfaces available in SendMessage!");
            }

            bool success = false;
            foreach ((string key, PlayerNetworkInterface networkInterface) in broadcastInterfaces)
            {
                try
                {
                    _ = socket.SendTo(buffer, networkInterface.Broadcast);
                    success = true;
                }
                catch (SocketException)
                {
                    // Do nothing
                }
            }

            return success;
        }
    }

    /// <summary>
    /// Background thread that listens for incoming UDP messages.
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
        MessageReceived?.Invoke(this, new LANLobbyBroadcastMessageReceivedEventArgs(data, endPoint));
    }

    /// <summary>
    /// Interval in milliseconds for refreshing network interfaces.
    /// </summary>
    private const int INTERFACE_REFRESH_INTERVAL_MS = 5000;

    /// <summary>
    /// Interval in milliseconds for checking the stop signal during sleep.
    /// </summary>
    private const int STOP_CHECK_INTERVAL_MS = 100;

    /// <summary>
    /// Background thread that periodically refreshes network interfaces.
    /// This ensures that the broadcast list stays up-to-date with network changes.
    /// </summary>
    private void RefreshInterfacesPeriodically()
    {
        try
        {
            while (!stopRefresher)
            {
                // Sleep for the refresh interval, but check periodically for stop signal
                int iterations = INTERFACE_REFRESH_INTERVAL_MS / STOP_CHECK_INTERVAL_MS;
                for (int i = 0; i < iterations && !stopRefresher; i++)
                    Thread.Sleep(STOP_CHECK_INTERVAL_MS);

                if (stopRefresher)
                    break;

                // Check if we're disposed
                if (Volatile.Read(ref disposed) != 0)
                    break;

                lock (socketLock)
                {
                    // Check stop flag again inside lock to avoid race condition
                    if (stopRefresher)
                        break;

                    // Check if socket is still valid
                    if (socket == null || !socket.IsBound)
                        break;
                }

                // Discover new interfaces outside the lock to minimize lock time
                var newInterfaces = DiscoverBroadcastInterfaces(lobbyPort);

                lock (socketLock)
                {
                    // Check again after discovery in case state changed
                    if (stopRefresher || socket == null || !socket.IsBound)
                        break;

                    broadcastInterfaces.Clear();
                    foreach (var (key, netIf) in newInterfaces)
                    {
                        broadcastInterfaces[key] = netIf;
                    }
                }
            }
        }
        catch (ThreadInterruptedException)
        {
            // Expected when shutting down
        }
        catch (Exception ex)
        {
            Logger.Log("Network interface refresh thread: exception: " + ex.ToString());
        }
    }

    /// <summary>
    /// Timeout in milliseconds for waiting for threads to terminate during shutdown.
    /// </summary>
    private const int THREAD_SHUTDOWN_TIMEOUT_MS = 1000;

    /// <summary>
    /// Closes the socket and stops the listening thread.
    /// </summary>
    public void Shutdown()
    {
        lock (socketLock)
        {
            // Signal the refresher thread to stop (inside lock for thread safety)
            stopRefresher = true;

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
            bool listenerTerminated = listener.Join(millisecondsTimeout: THREAD_SHUTDOWN_TIMEOUT_MS);
            if (!listenerTerminated)
                Logger.Log("Failed to shut down listener after timeout!");

            listener = null;
        }

        if (interfaceRefresher != null)
        {
            // Interrupt the thread to wake it from sleep
            interfaceRefresher.Interrupt();
            bool refresherTerminated = interfaceRefresher.Join(millisecondsTimeout: THREAD_SHUTDOWN_TIMEOUT_MS);
            if (!refresherTerminated)
                Logger.Log("Failed to shut down interface refresher after timeout!");

            interfaceRefresher = null;
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
            Shutdown();

        GC.SuppressFinalize(this);
    }
}
