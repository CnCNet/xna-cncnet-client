using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Manages a player connection between the local game and this application.
/// </summary>
internal sealed class V3LocalPlayerConnection : IDisposable
{
    private const int Timeout = 60000;
    private const uint IOC_IN = 0x80000000;
    private const uint IOC_VENDOR = 0x18000000;
    private const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

    private Socket localGameSocket;
    private EndPoint remotePlayerEndPoint;
    private CancellationToken cancellationToken;
    private uint playerId;

    public ushort PortNumber { get; private set; }

    public void Setup(uint playerId, CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        this.playerId = playerId;
        localGameSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        // Disable ICMP port not reachable exceptions, happens when the game is still loading and has not yet opened the socket.
        if (OperatingSystem.IsWindows())
            localGameSocket.IOControl(unchecked((int)SIO_UDP_CONNRESET), new byte[] { 0 }, null);

        localGameSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        PortNumber = (ushort)((IPEndPoint)localGameSocket.LocalEndPoint).Port;
    }

    public event EventHandler<GameDataReceivedEventArgs> RaiseGameDataReceivedEvent;

    /// <summary>
    /// Starts listening for local game player data and forwards it to the tunnel.
    /// </summary>
    /// <param name="gamePort">The game UDP port to listen on.</param>
    public async ValueTask StartConnectionAsync(int gamePort)
    {
        remotePlayerEndPoint = new IPEndPoint(IPAddress.Loopback, gamePort);

        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(128);
        Memory<byte> buffer = memoryOwner.Memory[..128];

        localGameSocket.ReceiveTimeout = Timeout;

#if DEBUG
        Logger.Log($"Start listening for local game {remotePlayerEndPoint} on {localGameSocket.LocalEndPoint}.");
#else
        Logger.Log($"Start listening for local game player {playerId}.");
#endif
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                SocketReceiveFromResult socketReceiveFromResult = await localGameSocket.ReceiveFromAsync(buffer, SocketFlags.None, remotePlayerEndPoint, cancellationToken);
                Memory<byte> data = buffer[..socketReceiveFromResult.ReceivedBytes];

#if DEBUG
                Logger.Log($"Received data from local game {socketReceiveFromResult.RemoteEndPoint} on {localGameSocket.LocalEndPoint}.");
#endif
                OnRaiseGameDataReceivedEvent(new(playerId, data));
            }
        }
        catch (SocketException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Sends tunnel data to the local game.
    /// </summary>
    /// <param name="data">The data to send to the game.</param>
    public async ValueTask SendDataAsync(ReadOnlyMemory<byte> data)
    {
#if DEBUG
        Logger.Log($"Sending data from {localGameSocket.LocalEndPoint} to local game {remotePlayerEndPoint}.");

#endif
        try
        {
            await localGameSocket.SendToAsync(data, SocketFlags.None, remotePlayerEndPoint, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
#if DEBUG
        Logger.Log($"Connection to local game {localGameSocket.RemoteEndPoint} closed.");
#else
        Logger.Log($"Connection to local game for player {playerId} closed.");
#endif
        localGameSocket.Dispose();
    }

    private void OnRaiseGameDataReceivedEvent(GameDataReceivedEventArgs e)
    {
        EventHandler<GameDataReceivedEventArgs> raiseEvent = RaiseGameDataReceivedEvent;

        raiseEvent?.Invoke(this, e);
    }
}