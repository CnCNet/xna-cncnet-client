using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
#if DEBUG
using Rampastring.Tools;
#endif

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Captures packets sent by an UDP client (the game) to a specific address
/// and allows forwarding messages back to it.
/// </summary>
internal sealed class V3TunneledPlayerConnection
{
    private const int Timeout = 60000;
    private const uint IOC_IN = 0x80000000;
    private const uint IOC_VENDOR = 0x18000000;
    private const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

    private readonly V3GameTunnelHandler gameTunnelHandler;

    private Socket socket;
    private EndPoint endPoint;
    private EndPoint remoteEndPoint;
    private bool aborted;

    public V3TunneledPlayerConnection(uint playerId, V3GameTunnelHandler gameTunnelHandler)
    {
        PlayerId = playerId;
        this.gameTunnelHandler = gameTunnelHandler;
    }

    public int PortNumber { get; private set; }

    public uint PlayerId { get; }

    public void Stop()
    {
        aborted = true;
    }

    /// <summary>
    /// Creates a socket and sets the connection's port number.
    /// </summary>
    public void CreateSocket()
    {
        socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        endPoint = new IPEndPoint(IPAddress.Loopback, 0);

        // Disable ICMP port not reachable exceptions, happens when the game is still loading and has not yet opened the socket.
        if (OperatingSystem.IsWindows())
            socket.IOControl(unchecked((int)SIO_UDP_CONNRESET), new byte[] { 0 }, null);

        socket.Bind(endPoint);

        PortNumber = ((IPEndPoint)socket.LocalEndPoint).Port;
    }

    public async Task StartAsync(int gamePort)
    {
        remoteEndPoint = new IPEndPoint(IPAddress.Loopback, gamePort);

        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(128);
        Memory<byte> buffer = memoryOwner.Memory[..128];

        socket.ReceiveTimeout = Timeout;

#if DEBUG
        Logger.Log($"Start listening for local game {((IPEndPoint)remoteEndPoint).Address}:{((IPEndPoint)remoteEndPoint).Port} on ({((IPEndPoint)socket.LocalEndPoint).Address}:{((IPEndPoint)socket.LocalEndPoint).Port})");

#endif
        try
        {
            while (true)
            {
                if (aborted)
                    break;

                SocketReceiveFromResult socketReceiveFromResult = await socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint);
                Memory<byte> data = buffer[..socketReceiveFromResult.ReceivedBytes];

#if DEBUG
                Logger.Log($"Received data from local game {((IPEndPoint)socketReceiveFromResult.RemoteEndPoint).Address}:{((IPEndPoint)socketReceiveFromResult.RemoteEndPoint).Port} on ({((IPEndPoint)socket.LocalEndPoint).Address}:{((IPEndPoint)socket.LocalEndPoint).Port})");

#endif

                await gameTunnelHandler.PlayerConnection_PacketReceivedAsync(this, data);
            }
        }
        catch (SocketException)
        {
        }

        aborted = true;

        socket.Close();
    }

    public async Task SendPacketAsync(ReadOnlyMemory<byte> packet)
    {
        if (aborted)
            return;

#if DEBUG
        Logger.Log($"Sending data from ({((IPEndPoint)socket.LocalEndPoint).Address}:{((IPEndPoint)socket.LocalEndPoint).Port}) to local game {((IPEndPoint)remoteEndPoint).Address}:{((IPEndPoint)remoteEndPoint).Port}");

#endif
        await socket.SendToAsync(packet, SocketFlags.None, remoteEndPoint);
    }
}