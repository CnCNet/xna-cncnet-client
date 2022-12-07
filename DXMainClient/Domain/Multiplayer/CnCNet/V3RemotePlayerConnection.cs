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
    private const int SendTimeout = 10000;
    private const int GameStartReceiveTimeout = 60000;
    private const int ReceiveTimeout = 60000;
    private const int MinimumPacketSize = 8;
    private const int MaximumPacketSize = 1024;

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

    /// <summary>
    /// Occurs when the connection to the remote host succeeded.
    /// </summary>
    public event EventHandler RaiseConnectedEvent;

    /// <summary>
    /// Occurs when the connection to the remote host could not be made.
    /// </summary>
    public event EventHandler RaiseConnectionFailedEvent;

    /// <summary>
    /// Occurs when the connection to the remote host was lost.
    /// </summary>
    public event EventHandler RaiseConnectionCutEvent;

    /// <summary>
    /// Occurs when game data from the remote host was received.
    /// </summary>
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

        tunnelSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        tunnelSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, localPort));

        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(MaximumPacketSize);
        Memory<byte> buffer = memoryOwner.Memory[..MaximumPacketSize];

        if (!BitConverter.TryWriteBytes(buffer.Span[..4], gameLocalPlayerId))
            throw new GameDataException();

        using var timeoutCancellationTokenSource = new CancellationTokenSource(SendTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

        try
        {
            await tunnelSocket.SendToAsync(buffer, SocketFlags.None, remoteEndPoint, linkedCancellationTokenSource.Token);
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (OperationCanceledException)
        {
#if DEBUG
            Logger.Log($"Failed to establish connection (time out) from port {localPort} to {remoteEndPoint}.");
#else
            Logger.Log($"Failed to establish connection (time out) using {localPort}.");
#endif
            OnRaiseConnectionFailedEvent(EventArgs.Empty);

            return;
        }

#if DEBUG
        Logger.Log($"Connection from {tunnelSocket.LocalEndPoint} to {remoteEndPoint} established.");
#else
        Logger.Log($"Connection using {localPort} established.");
#endif
        OnRaiseConnectedEvent(EventArgs.Empty);
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
            throw new GameDataException();

        if (!BitConverter.TryWriteBytes(packet.Span[4..8], receiverId))
            throw new GameDataException();

        data.CopyTo(packet[8..]);

        using var timeoutCancellationTokenSource = new CancellationTokenSource(SendTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

        try
        {
            await tunnelSocket.SendToAsync(packet, SocketFlags.None, remoteEndPoint, linkedCancellationTokenSource.Token);
        }
        catch (SocketException ex)
        {
#if DEBUG
            ProgramConstants.LogException(ex, $"Socket exception sending data to {remoteEndPoint}.");
#else
            ProgramConstants.LogException(ex, $"Socket exception sending data from port {localPort}.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
#if DEBUG
            Logger.Log($"Remote host connection {remoteEndPoint} timed out when sending data.");
#else
            Logger.Log($"Remote host connection from port {localPort} timed out when sending data.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
    }

    public void Dispose()
    {
#if DEBUG
        Logger.Log($"Connection to remote host {remoteEndPoint} closed.");
#else
        Logger.Log($"Connection to remote host on port {localPort} closed.");
#endif
        tunnelSocket?.Close();
    }

    private async ValueTask ReceiveLoopAsync()
    {
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(MaximumPacketSize);
        int receiveTimeout = GameStartReceiveTimeout;

#if DEBUG
        Logger.Log($"Start listening for {remoteEndPoint} on {tunnelSocket.LocalEndPoint}.");
#else
        Logger.Log($"Start listening on {localPort}.");
#endif

        while (!cancellationToken.IsCancellationRequested)
        {
            Memory<byte> buffer = memoryOwner.Memory[..MaximumPacketSize];
            SocketReceiveFromResult socketReceiveFromResult;
            using var timeoutCancellationTokenSource = new CancellationTokenSource(receiveTimeout);
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

            try
            {
                socketReceiveFromResult = await tunnelSocket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint, linkedCancellationTokenSource.Token);
            }
            catch (SocketException ex)
            {
#if DEBUG
                ProgramConstants.LogException(ex, $"Socket exception in {remoteEndPoint} receive loop.");
#else
                ProgramConstants.LogException(ex, $"Socket exception on port {localPort} receive loop.");
#endif
                OnRaiseConnectionCutEvent(EventArgs.Empty);

                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (OperationCanceledException)
            {
#if DEBUG
                Logger.Log($"Remote host connection {remoteEndPoint} timed out when receiving data.");
#else
                Logger.Log($"Remote host connection on port {localPort} timed out when receiving data.");
#endif
                OnRaiseConnectionCutEvent(EventArgs.Empty);

                return;
            }

            receiveTimeout = ReceiveTimeout;

            if (socketReceiveFromResult.ReceivedBytes < MinimumPacketSize)
            {
#if DEBUG
                Logger.Log($"Invalid data packet from {socketReceiveFromResult.RemoteEndPoint}");
#else
                Logger.Log($"Invalid data packet on {localPort}");
#endif
                continue;
            }

            Memory<byte> data = buffer[8..socketReceiveFromResult.ReceivedBytes];
            uint senderId = BitConverter.ToUInt32(buffer[..4].Span);
            uint receiverId = BitConverter.ToUInt32(buffer[4..8].Span);

#if DEBUG
            Logger.Log($"Received {senderId} -> {receiverId} from {socketReceiveFromResult.RemoteEndPoint} on {tunnelSocket.LocalEndPoint}.");

#endif
            if (receiverId != gameLocalPlayerId)
            {
#if DEBUG
                Logger.Log($"Invalid target (received: {receiverId}, expected: {gameLocalPlayerId}) from {socketReceiveFromResult.RemoteEndPoint}.");
#else
                Logger.Log($"Invalid target (received: {receiverId}, expected: {gameLocalPlayerId}) on port {localPort}.");
#endif

                continue;
            }

            OnRaiseGameDataReceivedEvent(new(senderId, data));
        }
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