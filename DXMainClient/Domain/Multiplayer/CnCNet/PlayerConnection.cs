using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal abstract class PlayerConnection : IDisposable
{
    protected const int PlayerIdSize = sizeof(uint);
    protected const int PlayerIdsSize = PlayerIdSize * 2;
    protected const int SendTimeout = 10000;
    protected const int MaximumPacketSize = 1024;

    protected CancellationToken CancellationToken;
    protected Socket Socket;
    protected EndPoint RemoteEndPoint;

    public uint PlayerId { get; protected set; }

    protected virtual int GameStartReceiveTimeout => 60000;

    protected virtual int GameInProgressReceiveTimeout => 10000;

    /// <summary>
    /// Occurs when the connection was lost.
    /// </summary>
    public event EventHandler RaiseConnectionCutEvent;

    /// <summary>
    /// Occurs when game data was received.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs> RaiseDataReceivedEvent;

    public void Dispose()
    {
#if DEBUG
        Logger.Log($"{GetType().Name}: Connection to {RemoteEndPoint} closed for player {PlayerId}.");
#else
        Logger.Log($"{GetType().Name}: Connection closed for player {PlayerId}.");
#endif
        Socket?.Close();
    }

    /// <summary>
    /// Starts listening for game data and forwards it.
    /// </summary>
    public async ValueTask StartConnectionAsync()
    {
        await DoStartConnectionAsync().ConfigureAwait(false);
        await ReceiveLoopAsync().ConfigureAwait(false);
    }

    protected virtual ValueTask DoStartConnectionAsync()
        => ValueTask.CompletedTask;

    protected abstract ValueTask<SocketReceiveFromResult> DoReceiveDataAsync(Memory<byte> buffer, CancellationToken cancellation);

    protected abstract DataReceivedEventArgs ProcessReceivedData(Memory<byte> buffer, SocketReceiveFromResult socketReceiveFromResult);

    protected async ValueTask SendDataAsync(ReadOnlyMemory<byte> data)
    {
        using var timeoutCancellationTokenSource = new CancellationTokenSource(SendTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, CancellationToken);

        try
        {
#if DEBUG
#if NETWORKTRACE
            Logger.Log($"{GetType().Name}: Sending data from {Socket.LocalEndPoint} to {RemoteEndPoint} for player {PlayerId}: {BitConverter.ToString(data.Span.ToArray())}.");
#else
            Logger.Log($"{GetType().Name}: Sending data from {Socket.LocalEndPoint} to {RemoteEndPoint} for player {PlayerId}.");
#endif
#endif
            await Socket.SendToAsync(data, SocketFlags.None, RemoteEndPoint, linkedCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
#if DEBUG
            ProgramConstants.LogException(ex, $"Socket exception sending data to {RemoteEndPoint} for player {PlayerId}.");
#else
            ProgramConstants.LogException(ex, $"Socket exception sending data for player {PlayerId}.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
#if DEBUG
            Logger.Log($"{GetType().Name}: Connection from {Socket.LocalEndPoint} to {RemoteEndPoint} timed out for player {PlayerId} when sending data.");
#else
            Logger.Log($"{GetType().Name}: Connection timed out for player {PlayerId} when sending data.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
    }

    private async ValueTask ReceiveLoopAsync()
    {
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(MaximumPacketSize);
        int receiveTimeout = GameStartReceiveTimeout;

#if DEBUG
        Logger.Log($"{GetType().Name}: Start listening for {RemoteEndPoint} on {Socket.LocalEndPoint} for player {PlayerId}.");
#else
        Logger.Log($"{GetType().Name}: Start listening for player {PlayerId}.");
#endif

        while (!CancellationToken.IsCancellationRequested)
        {
            Memory<byte> buffer = memoryOwner.Memory[..MaximumPacketSize];
            SocketReceiveFromResult socketReceiveFromResult;
            using var timeoutCancellationTokenSource = new CancellationTokenSource(receiveTimeout);
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, CancellationToken);

            try
            {
                socketReceiveFromResult = await DoReceiveDataAsync(buffer, linkedCancellationTokenSource.Token).ConfigureAwait(false);
                RemoteEndPoint = socketReceiveFromResult.RemoteEndPoint;
            }
            catch (SocketException ex)
            {
#if DEBUG
                ProgramConstants.LogException(ex, $"Socket exception in {RemoteEndPoint} receive loop for player {PlayerId}.");
#else
                ProgramConstants.LogException(ex, $"Socket exception in receive loop for player {PlayerId}.");
#endif
                OnRaiseConnectionCutEvent(EventArgs.Empty);

                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (OperationCanceledException)
            {
#if DEBUG
                Logger.Log($"{GetType().Name}: Connection from {Socket.LocalEndPoint} to {RemoteEndPoint} timed out for player {PlayerId} when receiving data.");
#else
                Logger.Log($"{GetType().Name}: Connection timed out for player {PlayerId} when receiving data.");
#endif
                OnRaiseConnectionCutEvent(EventArgs.Empty);

                return;
            }

            receiveTimeout = GameInProgressReceiveTimeout;

#if DEBUG
#if NETWORKTRACE
            Logger.Log($"{GetType().Name}: Received data from {RemoteEndPoint} on {Socket.LocalEndPoint} for player {PlayerId}: {BitConverter.ToString(buffer.Span.ToArray())}.");
#else
            Logger.Log($"{GetType().Name}: Received data from {RemoteEndPoint} on {Socket.LocalEndPoint} for player {PlayerId}.");
#endif
#endif

            DataReceivedEventArgs dataReceivedEventArgs = ProcessReceivedData(buffer, socketReceiveFromResult);

            if (dataReceivedEventArgs is not null)
                OnRaiseDataReceivedEvent(dataReceivedEventArgs);
        }
    }

    private void OnRaiseConnectionCutEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseConnectionCutEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void OnRaiseDataReceivedEvent(DataReceivedEventArgs e)
    {
        EventHandler<DataReceivedEventArgs> raiseEvent = RaiseDataReceivedEvent;

        raiseEvent?.Invoke(this, e);
    }
}