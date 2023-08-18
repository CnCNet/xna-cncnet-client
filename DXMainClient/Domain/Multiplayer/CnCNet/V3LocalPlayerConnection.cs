using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
#if DEBUG
using Rampastring.Tools;
#endif

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Manages a player connection between the local game and this application.
/// </summary>
internal sealed class V3LocalPlayerConnection : PlayerConnection
{
    private const uint IOC_IN = 0x80000000;
    private const uint IOC_VENDOR = 0x18000000;
    private const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

    private readonly IPEndPoint loopbackIpEndPoint = new(IPAddress.Loopback, 0);

    /// <summary>
    /// Creates a local game socket and returns the port.
    /// </summary>
    /// <param name="playerId">The id of the player for which to create the local game socket.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop the connection.</param>
    /// <returns>The port of the created socket.</returns>
    public ushort Setup(uint playerId, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        PlayerId = playerId;
        Socket = new(SocketType.Dgram, ProtocolType.Udp);
        RemoteEndPoint = loopbackIpEndPoint;

        // Disable ICMP port not reachable exceptions, happens when the game is still loading and has not yet opened the socket.
        if (OperatingSystem.IsWindows())
            Socket.IOControl(unchecked((int)SIO_UDP_CONNRESET), new byte[] { 0 }, null);

        Socket.Bind(loopbackIpEndPoint);

        return (ushort)((IPEndPoint)Socket.LocalEndPoint).Port;
    }

    /// <summary>
    /// Sends remote player data to the local game.
    /// </summary>
    /// <param name="data">The data to send to the game.</param>
    public async ValueTask SendDataToGameAsync(ReadOnlyMemory<byte> data)
    {
        if (RemoteEndPoint.Equals(loopbackIpEndPoint) || data.Length < PlayerIdsSize)
        {
#if DEBUG
            Logger.Log($"{GetType().Name}: Discarded remote data from {Socket.LocalEndPoint} to {RemoteEndPoint} for player {PlayerId}.");

#endif
            return;
        }

        await SendDataAsync(data).ConfigureAwait(false);
    }

    protected override ValueTask<SocketReceiveFromResult> DoReceiveDataAsync(Memory<byte> buffer, CancellationToken cancellation)
        => Socket.ReceiveFromAsync(buffer[PlayerIdsSize..], SocketFlags.None, RemoteEndPoint, cancellation);

    protected override DataReceivedEventArgs ProcessReceivedData(Memory<byte> buffer, SocketReceiveFromResult socketReceiveFromResult)
        => new(PlayerId, buffer[..(PlayerIdsSize + socketReceiveFromResult.ReceivedBytes)]);
}