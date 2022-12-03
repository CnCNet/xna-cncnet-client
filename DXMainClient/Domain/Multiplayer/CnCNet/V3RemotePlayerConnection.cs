using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Manages a player connection between a remote host and this application.
/// </summary>
internal sealed class V3RemotePlayerConnection : IDisposable
{
    private uint gameLocalPlayerId;
    private CancellationToken cancellationToken;
    private Socket tunnelSocket;
    private IPEndPoint remoteEndPoint;
    private ushort localPort;

    public void SetUp(IPEndPoint remoteEndPoint, ushort localPort, uint gameLocalPlayerId, CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        this.gameLocalPlayerId = gameLocalPlayerId;
        this.remoteEndPoint = remoteEndPoint;
        this.localPort = localPort;
    }

    public event EventHandler RaiseConnectedEvent;

    public event EventHandler RaiseConnectionFailedEvent;

    public event EventHandler RaiseConnectionCutEvent;

    public event EventHandler<GameDataReceivedEventArgs> RaiseGameDataReceivedEvent;

    /// <summary>
    /// Starts listening for remote player data and forwards it to the local game.
    /// </summary>
    public async ValueTask StartConnectionAsync()
    {
#if DEBUG
        Logger.Log($"Attempting to establish a connection from port {localPort} to {remoteEndPoint}).");
#else
        Logger.Log($"Attempting to establish a connection using {localPort}).");
#endif

        tunnelSocket = new Socket(SocketType.Dgram, ProtocolType.Udp)
        {
            SendTimeout = Constants.TUNNEL_CONNECTION_TIMEOUT,
            ReceiveTimeout = Constants.TUNNEL_CONNECTION_TIMEOUT
        };

        tunnelSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, localPort));

        try
        {
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(50);
            Memory<byte> buffer = memoryOwner.Memory[..50];

            if (!BitConverter.TryWriteBytes(buffer.Span[..4], gameLocalPlayerId))
                throw new Exception();

            await tunnelSocket.SendToAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken);
#if DEBUG
            Logger.Log($"Connection from {tunnelSocket.LocalEndPoint} to {remoteEndPoint} established.");
#else
            Logger.Log($"Connection using {localPort} established.");
#endif
            OnRaiseConnectedEvent(EventArgs.Empty);
        }
        catch (SocketException ex)
        {
#if DEBUG
            ProgramConstants.LogException(ex, $"Failed to establish connection from port {localPort} to {remoteEndPoint}.");
#else
            ProgramConstants.LogException(ex, $"Failed to establish connection using {localPort}.");
#endif
            OnRaiseConnectionFailedEvent(EventArgs.Empty);
            return;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        tunnelSocket.ReceiveTimeout = Constants.TUNNEL_RECEIVE_TIMEOUT;

        await ReceiveLoopAsync();
    }

    /// <summary>
    /// Sends local game player data to the remote host.
    /// </summary>
    /// <param name="data">The data to send to the game.</param>
    /// <param name="receiverId">The id of the player that receives the data.</param>
    public async ValueTask SendDataAsync(ReadOnlyMemory<byte> data, uint receiverId)
    {
#if DEBUG
        Logger.Log($"Sending data {gameLocalPlayerId} -> {receiverId} from {tunnelSocket.LocalEndPoint} to {remoteEndPoint}.");

#endif
        const int idsSize = sizeof(uint) * 2;
        int bufferSize = data.Length + idsSize;
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
        Memory<byte> packet = memoryOwner.Memory[..bufferSize];

        if (!BitConverter.TryWriteBytes(packet.Span[..4], gameLocalPlayerId))
            throw new Exception();

        if (!BitConverter.TryWriteBytes(packet.Span[4..8], receiverId))
            throw new Exception();

        data.CopyTo(packet[8..]);

        try
        {
            await tunnelSocket.SendToAsync(packet, SocketFlags.None, remoteEndPoint, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async ValueTask ReceiveLoopAsync()
    {
        try
        {
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);

#if DEBUG
            Logger.Log($"Start listening for {remoteEndPoint} on {tunnelSocket.LocalEndPoint}.");
#else
            Logger.Log($"Start listening on {localPort}.");
#endif

            while (!cancellationToken.IsCancellationRequested)
            {
                Memory<byte> buffer = memoryOwner.Memory[..1024];
                SocketReceiveFromResult socketReceiveFromResult = await tunnelSocket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken);

                if (socketReceiveFromResult.ReceivedBytes < 8)
                {
#if DEBUG
                    Logger.Log($"Invalid data packet from {remoteEndPoint}");
#else
                    Logger.Log($"Invalid data packet on {localPort}");
#endif
                    continue;
                }

                Memory<byte> data = buffer[8..socketReceiveFromResult.ReceivedBytes];
                uint senderId = BitConverter.ToUInt32(buffer[..4].Span);
                uint receiverId = BitConverter.ToUInt32(buffer[4..8].Span);

#if DEBUG
                Logger.Log($"Received {senderId} -> {receiverId} from {remoteEndPoint} on {tunnelSocket.LocalEndPoint}.");

#endif
                if (receiverId != gameLocalPlayerId)
                {
#if DEBUG
                    Logger.Log($"Invalid target (received: {receiverId}, expected: {gameLocalPlayerId}) from {remoteEndPoint}.");
#else
                    Logger.Log($"Invalid target (received: {receiverId}, expected: {gameLocalPlayerId}) on port {localPort}.");
#endif
                    continue;
                }

                OnRaiseGameDataReceivedEvent(new(senderId, data));
            }
        }
        catch (SocketException ex)
        {
#if DEBUG
            ProgramConstants.LogException(ex, $"Socket exception in {remoteEndPoint} receive loop.");
#else
            ProgramConstants.LogException(ex, $"Socket exception on port {localPort} receive loop.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
#if DEBUG
        Logger.Log($"Connection to remote host {remoteEndPoint} closed.");
#else
        Logger.Log($"Connection to remote host on port {localPort} closed.");
#endif
        tunnelSocket?.Dispose();
    }

    private void OnRaiseConnectedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseConnectedEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void OnRaiseConnectionFailedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseConnectionFailedEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void OnRaiseConnectionCutEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseConnectionCutEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void OnRaiseGameDataReceivedEvent(GameDataReceivedEventArgs e)
    {
        EventHandler<GameDataReceivedEventArgs> raiseEvent = RaiseGameDataReceivedEvent;

        raiseEvent?.Invoke(this, e);
    }
}