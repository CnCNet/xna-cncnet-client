using Rampastring.Tools;
using System;
#if !NETFRAMEWORK
using System.Buffers;
#endif
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Handles connections to version 3 CnCNet tunnel servers.
    /// </summary>
    internal sealed class V3TunnelConnection
    {
        public V3TunnelConnection(CnCNetTunnel tunnel, GameTunnelHandler gameTunnelHandler, uint senderId)
        {
            this.tunnel = tunnel;
            this.gameTunnelHandler = gameTunnelHandler;
            SenderId = senderId;
        }

        public event EventHandler Connected;
        public event EventHandler ConnectionFailed;
        public event EventHandler ConnectionCut;

        public uint SenderId { get; }

        private bool aborted;
        public bool Aborted
        {
            get
            {
                locker.Wait();

                try
                {
                    return aborted;
                }
                finally
                {
                    locker.Release();
                }
            }
            private set
            {
                locker.Wait();

                try
                {
                    aborted = value;
                }
                finally
                {
                    locker.Release();
                }
            }
        }

        private readonly CnCNetTunnel tunnel;
        private readonly GameTunnelHandler gameTunnelHandler;
        private Socket tunnelSocket;
        private EndPoint tunnelEndPoint;

        private readonly SemaphoreSlim locker = new(1, 1);

        public async Task ConnectAsync()
        {
            try
            {
                Logger.Log($"Attempting to establish connection to V3 tunnel server " +
                    $"{tunnel.Name} ({tunnel.Address}:{tunnel.Port})");

                tunnelEndPoint = new IPEndPoint(tunnel.IPAddress, tunnel.Port);
                tunnelSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                tunnelSocket.SendTimeout = Constants.TUNNEL_CONNECTION_TIMEOUT;
                tunnelSocket.ReceiveTimeout = Constants.TUNNEL_CONNECTION_TIMEOUT;

                try
                {
#if NETFRAMEWORK
                    byte[] buffer1 = new byte[50];
                    WriteSenderIdToBuffer(buffer1);
                    var buffer = new ArraySegment<byte>(buffer1);
#else
                    using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(50);
                    Memory<byte> buffer = memoryOwner.Memory[..50];
                    if (!BitConverter.TryWriteBytes(buffer.Span[..4], SenderId)) throw new Exception();
#endif

                    await tunnelSocket.SendToAsync(buffer, SocketFlags.None, tunnelEndPoint);

                    Logger.Log($"Connection to tunnel server established.");
                    Connected?.Invoke(this, EventArgs.Empty);
                }
                catch (SocketException ex)
                {
                    PreStartup.LogException(ex, "Failed to establish connection to tunnel server.");
                    tunnelSocket.Close();
                    ConnectionFailed?.Invoke(this, EventArgs.Empty);
                    return;
                }

                tunnelSocket.ReceiveTimeout = Constants.TUNNEL_RECEIVE_TIMEOUT;

                await ReceiveLoopAsync();
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }
#if NETFRAMEWORK

        private void WriteSenderIdToBuffer(byte[] buffer) =>
            Array.Copy(BitConverter.GetBytes(SenderId), buffer, sizeof(uint));
#endif

        private async Task ReceiveLoopAsync()
        {
            try
            {
#if !NETFRAMEWORK
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);
#endif

                while (true)
                {
                    if (Aborted)
                    {
                        DoClose();
                        Logger.Log("Exiting receive loop.");
                        return;
                    }

#if NETFRAMEWORK
                    byte[] buffer1 = new byte[1024];
                    var buffer = new ArraySegment<byte>(buffer1);
#else
                    Memory<byte> buffer = memoryOwner.Memory[..1024];
#endif

                    SocketReceiveFromResult socketReceiveFromResult = await tunnelSocket.ReceiveFromAsync(buffer, SocketFlags.None, tunnelEndPoint);

                    if (socketReceiveFromResult.ReceivedBytes < 8)
                    {
                        Logger.Log("Invalid data packet from tunnel server");
                        continue;
                    }

#if NETFRAMEWORK
                    byte[] data = new byte[socketReceiveFromResult.ReceivedBytes - 8];
                    Array.Copy(buffer1, 8, data, 0, data.Length);
                    uint senderId = BitConverter.ToUInt32(buffer1, 0);
#else
                    Memory<byte> data = buffer[8..socketReceiveFromResult.ReceivedBytes];
                    uint senderId = BitConverter.ToUInt32(buffer[..4].Span);
#endif

                    await gameTunnelHandler.TunnelConnection_MessageReceivedAsync(data, senderId);
                }
            }
            catch (SocketException ex)
            {
                PreStartup.LogException(ex, "Socket exception in V3 tunnel receive loop.");
                DoClose();
                ConnectionCut?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CloseConnection()
        {
            Logger.Log("Closing connection to the tunnel server.");
            Aborted = true;
        }

        private void DoClose()
        {
            Aborted = true;

            if (tunnelSocket != null)
            {
                tunnelSocket.Close();
                tunnelSocket = null;
            }

            Logger.Log("Connection to tunnel server closed.");
        }

#if NETFRAMEWORK
        public async Task SendDataAsync(byte[] data, uint receiverId)
        {
            byte[] buffer = new byte[data.Length + 8]; // 8 = sizeof(uint) * 2
            WriteSenderIdToBuffer(buffer);
            Array.Copy(BitConverter.GetBytes(receiverId), 0, buffer, 4, sizeof(uint));
            Array.Copy(data, 0, buffer, 8, data.Length);
            var packet = new ArraySegment<byte>(buffer);
#else
        public async Task SendDataAsync(ReadOnlyMemory<byte> data, uint receiverId)
        {
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(data.Length + 8);
            Memory<byte> packet = memoryOwner.Memory[..(data.Length + 8)];
            if (!BitConverter.TryWriteBytes(packet.Span[..4], SenderId)) throw new Exception();
            if (!BitConverter.TryWriteBytes(packet.Span[4..8], receiverId)) throw new Exception();
            data.CopyTo(packet[8..]);
#endif

            await locker.WaitAsync();

            try
            {
                if (!aborted)
                    await tunnelSocket.SendToAsync(packet, SocketFlags.None, tunnelEndPoint);
            }
            finally
            {
                locker.Release();
            }
        }
    }
}