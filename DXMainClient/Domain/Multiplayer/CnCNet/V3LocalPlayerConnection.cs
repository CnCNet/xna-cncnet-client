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
/// Manages a player connection between the local game and this application.
/// </summary>
internal sealed class V3LocalPlayerConnection : IDisposable
{
    private const uint IOC_IN = 0x80000000;
    private const uint IOC_VENDOR = 0x18000000;
    private const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
    private const int SendTimeout = 10000;
    private const int GameStartReceiveTimeout = 60000;
    private const int ReceiveTimeout = 10000;
    private const int PlayerIdSize = sizeof(uint);
    private const int PlayerIdsSize = PlayerIdSize * 2;
    private const int MaximumPacketSize = 1024;

    private Socket localGameSocket;
    private EndPoint remotePlayerEndPoint;
    private CancellationToken cancellationToken;

    public uint PlayerId { get; private set; }

    /// <summary>
    /// Creates a local game socket and returns the port.
    /// </summary>
    /// <param name="playerId">The id of the player for which to create the local game socket.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop the connection.</param>
    /// <returns>The port of the created socket.</returns>
    public ushort Setup(uint playerId, CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        PlayerId = playerId;
        localGameSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

        // Disable ICMP port not reachable exceptions, happens when the game is still loading and has not yet opened the socket.
        if (OperatingSystem.IsWindows())
            localGameSocket.IOControl(unchecked((int)SIO_UDP_CONNRESET), new byte[] { 0 }, null);

        localGameSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        return (ushort)((IPEndPoint)localGameSocket.LocalEndPoint).Port;
    }

    /// <summary>
    /// Occurs when the connection to the local game was lost.
    /// </summary>
    public event EventHandler RaiseConnectionCutEvent;

    /// <summary>
    /// Occurs when game data from the local game was received.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs> RaiseDataReceivedEvent;

    /// <summary>
    /// Starts listening for local game player data and forwards it to the tunnel.
    /// </summary>
    public async ValueTask StartConnectionAsync()
    {
        remotePlayerEndPoint = new IPEndPoint(IPAddress.Loopback, 0);

        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(MaximumPacketSize);
        int receiveTimeout = GameStartReceiveTimeout;

#if DEBUG
        Logger.Log($"Start listening for local game {remotePlayerEndPoint} on {localGameSocket.LocalEndPoint} for player {PlayerId}.");
#else
        Logger.Log($"Start listening for local game for player {PlayerId}.");
#endif

        while (!cancellationToken.IsCancellationRequested)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource(receiveTimeout);
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);
            Memory<byte> buffer = memoryOwner.Memory[..MaximumPacketSize];

            try
            {
                SocketReceiveFromResult socketReceiveFromResult = await localGameSocket.ReceiveFromAsync(
                    buffer[PlayerIdsSize..], SocketFlags.None, remotePlayerEndPoint, linkedCancellationTokenSource.Token).ConfigureAwait(false);

                remotePlayerEndPoint = socketReceiveFromResult.RemoteEndPoint;
                buffer = buffer[..(PlayerIdsSize + socketReceiveFromResult.ReceivedBytes)];

#if DEBUG
                Logger.Log($"Received data from local game {socketReceiveFromResult.RemoteEndPoint} on {localGameSocket.LocalEndPoint} for player {PlayerId}.");
#endif
            }
            catch (SocketException ex)
            {
#if DEBUG
                ProgramConstants.LogException(ex, $"Socket exception in {remotePlayerEndPoint} receive loop for player {PlayerId}.");
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (OperationCanceledException)
            {
#if DEBUG
                Logger.Log($"Local game connection {localGameSocket.LocalEndPoint} timed out for player {PlayerId} when receiving data.");
#else
                Logger.Log($"Local game connection timed out for player {PlayerId} when receiving data.");
#endif
                OnRaiseConnectionCutEvent(EventArgs.Empty);

                return;
            }

            receiveTimeout = ReceiveTimeout;

            OnRaiseDataReceivedEvent(new(PlayerId, buffer));
        }
    }

    /// <summary>
    /// Sends tunnel data to the local game.
    /// </summary>
    /// <param name="data">The data to send to the game.</param>
    public async ValueTask SendDataAsync(ReadOnlyMemory<byte> data)
    {
        if (remotePlayerEndPoint is null || data.Length < PlayerIdsSize)
            return;

        using var timeoutCancellationTokenSource = new CancellationTokenSource(SendTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

        try
        {
#if DEBUG
            Logger.Log($"Sending data from {localGameSocket.LocalEndPoint} to local game {remotePlayerEndPoint} for player {PlayerId}.");

#endif
            await localGameSocket.SendToAsync(data, SocketFlags.None, remotePlayerEndPoint, linkedCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
#if DEBUG
            ProgramConstants.LogException(ex, $"Socket exception sending data to {remotePlayerEndPoint} for player {PlayerId}.");
#else
            ProgramConstants.LogException(ex, $"Socket exception sending data for player {PlayerId}.");
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
            Logger.Log($"Local game connection {localGameSocket.LocalEndPoint} timed out for player {PlayerId} when sending data.");
#else
            Logger.Log($"Local game connection timed out for player {PlayerId} when sending data.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
    }

    public void Dispose()
    {
#if DEBUG
        Logger.Log($"Connection to local game {remotePlayerEndPoint} closed for player {PlayerId}.");
#else
        Logger.Log($"Connection to local game closed for player {PlayerId}.");
#endif
        localGameSocket.Close();
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