using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Handles connections to version 3 CnCNet tunnel servers.
/// </summary>
internal sealed class V3TunnelConnection
{
    private readonly CnCNetTunnel tunnel;
    private readonly V3GameTunnelHandler gameTunnelHandler;
    private readonly uint localId;

    private Socket tunnelSocket;
    private EndPoint tunnelEndPoint;
    private bool aborted;

    public V3TunnelConnection(CnCNetTunnel tunnel, V3GameTunnelHandler gameTunnelHandler, uint localId)
    {
        this.tunnel = tunnel;
        this.gameTunnelHandler = gameTunnelHandler;
        this.localId = localId;
    }

    public event EventHandler Connected;

    public event EventHandler ConnectionFailed;

    public event EventHandler ConnectionCut;

    public async Task ConnectAsync()
    {
        Logger.Log("Attempting to establish connection to V3 tunnel server " +
            $"{tunnel.Name} ({tunnel.Address}:{tunnel.Port})");

        tunnelEndPoint = new IPEndPoint(tunnel.IPAddress, tunnel.Port);
        tunnelSocket = new Socket(SocketType.Dgram, ProtocolType.Udp)
        {
            SendTimeout = Constants.TUNNEL_CONNECTION_TIMEOUT,
            ReceiveTimeout = Constants.TUNNEL_CONNECTION_TIMEOUT
        };

        try
        {
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(50);
            Memory<byte> buffer = memoryOwner.Memory[..50];

            if (!BitConverter.TryWriteBytes(buffer.Span[..4], localId))
                throw new Exception();

            await tunnelSocket.SendToAsync(buffer, SocketFlags.None, tunnelEndPoint);
            Logger.Log($"Connection to V3 tunnel server {tunnel.Name} ({tunnel.Address}:{tunnel.Port}) established.");
            Connected?.Invoke(this, EventArgs.Empty);
        }
        catch (SocketException ex)
        {
            ProgramConstants.LogException(ex, $"Failed to establish connection to V3 tunnel server {tunnel.Name} ({tunnel.Address}:{tunnel.Port}).");
            tunnelSocket.Close();
            ConnectionFailed?.Invoke(this, EventArgs.Empty);
            return;
        }

        tunnelSocket.ReceiveTimeout = Constants.TUNNEL_RECEIVE_TIMEOUT;

        await ReceiveLoopAsync();
    }

    public async Task SendDataAsync(ReadOnlyMemory<byte> data, uint receiverId)
    {
#if DEBUG
        Logger.Log($"Sending data {localId} -> {receiverId} from ({((IPEndPoint)tunnelSocket.LocalEndPoint).Address}:{((IPEndPoint)tunnelSocket.LocalEndPoint).Port}) to V3 tunnel server {tunnel.Name} ({tunnel.Address}:{tunnel.Port})");

#endif
        const int idsSize = sizeof(uint) * 2;
        int bufferSize = data.Length + idsSize;
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
        Memory<byte> packet = memoryOwner.Memory[..bufferSize];

        if (!BitConverter.TryWriteBytes(packet.Span[..4], localId))
            throw new Exception();

        if (!BitConverter.TryWriteBytes(packet.Span[4..8], receiverId))
            throw new Exception();

        data.CopyTo(packet[8..]);

        if (!aborted)
            await tunnelSocket.SendToAsync(packet, SocketFlags.None, tunnelEndPoint);
    }

    public void CloseConnection()
    {
        Logger.Log("Closing connection to the tunnel server.");

        aborted = true;
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);

#if DEBUG
            Logger.Log($"Start listening for V3 tunnel server {tunnel.Name} ({tunnel.Address}:{tunnel.Port}) on ({((IPEndPoint)tunnelSocket.LocalEndPoint).Address}:{((IPEndPoint)tunnelSocket.LocalEndPoint).Port})");

#endif
            while (true)
            {
                if (aborted)
                {
                    DoClose();
                    Logger.Log("Exiting receive loop.");
                    return;
                }

                Memory<byte> buffer = memoryOwner.Memory[..1024];
                SocketReceiveFromResult socketReceiveFromResult = await tunnelSocket.ReceiveFromAsync(buffer, SocketFlags.None, tunnelEndPoint);

                if (socketReceiveFromResult.ReceivedBytes < 8)
                {
                    Logger.Log("Invalid data packet from tunnel server");
                    continue;
                }

                Memory<byte> data = buffer[8..socketReceiveFromResult.ReceivedBytes];
                uint senderId = BitConverter.ToUInt32(buffer[..4].Span);
                uint receiverId = BitConverter.ToUInt32(buffer[4..8].Span);

#if DEBUG
                Logger.Log($"Received {senderId} -> {receiverId} from V3 tunnel server {tunnel.Name} ({tunnel.Address}:{tunnel.Port}) on ({((IPEndPoint)tunnelSocket.LocalEndPoint).Address}:{((IPEndPoint)tunnelSocket.LocalEndPoint).Port})");

#endif
                if (receiverId != localId)
                {
                    Logger.Log($"Invalid target (received: {receiverId}, expected: {localId}) from V3 tunnel server {tunnel.Name} ({tunnel.Address}:{tunnel.Port})");
                    continue;
                }

                await gameTunnelHandler.TunnelConnection_MessageReceivedAsync(data, senderId);
            }
        }
        catch (SocketException ex)
        {
            ProgramConstants.LogException(ex, "Socket exception in V3 tunnel receive loop.");
            DoClose();
            ConnectionCut?.Invoke(this, EventArgs.Empty);
        }
    }

    private void DoClose()
    {
        aborted = true;

        tunnelSocket?.Close();

        tunnelSocket = null;

        Logger.Log("Connection to tunnel server closed.");
    }
}