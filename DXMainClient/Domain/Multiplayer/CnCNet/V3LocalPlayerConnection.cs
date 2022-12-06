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

    private Socket localGameSocket;
    private EndPoint remotePlayerEndPoint;
    private CancellationToken cancellationToken;
    private uint playerId;

    /// <summary>
    /// Creates a local game socket and returns the port.
    /// </summary>
    /// <param name="playerId">The id of the player for which to create the local game socket.</param>
    /// <returns>The port of the created socket.</returns>
    public ushort Setup(uint playerId, CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        this.playerId = playerId;
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
    public event EventHandler<GameDataReceivedEventArgs> RaiseGameDataReceivedEvent;

    /// <summary>
    /// Starts listening for local game player data and forwards it to the tunnel.
    /// </summary>
    public async ValueTask StartConnectionAsync()
    {
        remotePlayerEndPoint = new IPEndPoint(IPAddress.Loopback, 0);

        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(128);
        Memory<byte> buffer = memoryOwner.Memory[..128];
        int receiveTimeout = GameStartReceiveTimeout;

#if DEBUG
        Logger.Log($"Start listening for local game {remotePlayerEndPoint} on {localGameSocket.LocalEndPoint} for player {playerId}.");
#else
        Logger.Log($"Start listening for local game for player {playerId}.");
#endif

        while (!cancellationToken.IsCancellationRequested)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource(receiveTimeout);
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);
            Memory<byte> data;

            try
            {
                SocketReceiveFromResult socketReceiveFromResult = await localGameSocket.ReceiveFromAsync(buffer, SocketFlags.None, remotePlayerEndPoint, linkedCancellationTokenSource.Token);

                remotePlayerEndPoint = socketReceiveFromResult.RemoteEndPoint;
                data = buffer[..socketReceiveFromResult.ReceivedBytes];

#if DEBUG
                Logger.Log($"Received data from local game {socketReceiveFromResult.RemoteEndPoint} on {localGameSocket.LocalEndPoint} for player {playerId}.");
#endif
            }
            catch (SocketException ex)
            {
#if DEBUG
                ProgramConstants.LogException(ex, $"Socket exception in {remotePlayerEndPoint} receive loop for player {playerId}.");
#else
                ProgramConstants.LogException(ex, $"Socket exception in receive loop for player {playerId}.");
#endif
                OnRaiseConnectionCutEvent(EventArgs.Empty);

                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (OperationCanceledException)
            {
#if DEBUG
                Logger.Log($"Local game connection {localGameSocket.LocalEndPoint} timed out for player {playerId} when receiving data.");
#else
                Logger.Log($"Local game connection timed out for player {playerId} when receiving data.");
#endif
                OnRaiseConnectionCutEvent(EventArgs.Empty);

                return;
            }

            receiveTimeout = ReceiveTimeout;

            OnRaiseGameDataReceivedEvent(new(playerId, data));
        }
    }

    /// <summary>
    /// Sends tunnel data to the local game.
    /// </summary>
    /// <param name="data">The data to send to the game.</param>
    public async ValueTask SendDataAsync(ReadOnlyMemory<byte> data)
    {
#if DEBUG
        Logger.Log($"Sending data from {localGameSocket.LocalEndPoint} to local game {remotePlayerEndPoint} for player {playerId}.");

#endif
        if (remotePlayerEndPoint is null)
            return;

        using var timeoutCancellationTokenSource = new CancellationTokenSource(SendTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

        try
        {
            await localGameSocket.SendToAsync(data, SocketFlags.None, remotePlayerEndPoint, linkedCancellationTokenSource.Token);
        }
        catch (SocketException ex)
        {
#if DEBUG
            ProgramConstants.LogException(ex, $"Socket exception sending data to {remotePlayerEndPoint} for player {playerId}.");
#else
            ProgramConstants.LogException(ex, $"Socket exception sending data for player {playerId}.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
#if DEBUG
            Logger.Log($"Local game connection {localGameSocket.LocalEndPoint} timed out for player {playerId} when sending data.");
#else
            Logger.Log($"Local game connection timed out for player {playerId} when sending data.");
#endif
            OnRaiseConnectionCutEvent(EventArgs.Empty);
        }
    }

    public void Dispose()
    {
#if DEBUG
        Logger.Log($"Connection to local game {remotePlayerEndPoint} closed for player {playerId}.");
#else
        Logger.Log($"Connection to local game closed for player {playerId}.");
#endif
        localGameSocket.Close();
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