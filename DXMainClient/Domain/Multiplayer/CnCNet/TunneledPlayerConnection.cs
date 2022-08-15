using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Captures packets sent by an UDP client (the game) to a specific address
    /// and allows forwarding messages back to it.
    /// </summary>
    internal sealed class TunneledPlayerConnection
    {
        private const int Timeout = 60000;

        private GameTunnelHandler gameTunnelHandler;

        public TunneledPlayerConnection(uint playerId, GameTunnelHandler gameTunnelHandler)
        {
            PlayerID = playerId;
            this.gameTunnelHandler = gameTunnelHandler;
        }

        public int PortNumber { get; private set; }
        public uint PlayerID { get; }

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

        private Socket socket;
        private EndPoint endPoint;

        private readonly SemaphoreSlim locker = new(1, 1);

        public void Stop()
        {
            Aborted = true;
        }

        /// <summary>
        /// Creates a socket and sets the connection's port number.
        /// </summary>
        public void CreateSocket()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            socket.Bind(endPoint);

            PortNumber = ((IPEndPoint)socket.LocalEndPoint).Port;
            Logger.Log($"Tunnel_V3 Created local game connection for clientId {PlayerID} {socket.LocalEndPoint} ({PortNumber}).");
        }

        public async Task StartAsync()
        {
            try
            {
#if NETFRAMEWORK
                byte[] buffer1 = new byte[1024];
                var buffer = new ArraySegment<byte>(buffer1);
#else
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);
                Memory<byte> buffer = memoryOwner.Memory[..1024];
#endif

                socket.ReceiveTimeout = Timeout;

                try
                {
                    while (true)
                    {
                        if (Aborted)
                        {
                            Logger.Log($"Tunnel_V3 abort listening for game data for {PlayerID} on {socket.LocalEndPoint} from {socket.RemoteEndPoint}.");
                            break;
                        }

#if DEBUG
                        Logger.Log($"Tunnel_V3 listening for game data for {PlayerID} on {socket.LocalEndPoint} from {socket.RemoteEndPoint}.");
#endif

                        SocketReceiveFromResult socketReceiveFromResult = await socket.ReceiveFromAsync(buffer, SocketFlags.None, endPoint);

#if DEBUG
                        Logger.Log($"Tunnel_V3 received game data for {PlayerID} on {socket.LocalEndPoint} from {socket.RemoteEndPoint}.");
#endif

#if NETFRAMEWORK
                        byte[] data = new byte[socketReceiveFromResult.ReceivedBytes];
                        Array.Copy(buffer1, data, socketReceiveFromResult.ReceivedBytes);
                        Array.Clear(buffer1, 0, socketReceiveFromResult.ReceivedBytes);
#else

                        Memory<byte> data = buffer[..socketReceiveFromResult.ReceivedBytes];
#endif

                        await gameTunnelHandler.PlayerConnection_PacketReceivedAsync(this, data);
                    }
                }
                catch (SocketException)
                {
                    // Timeout
                }

                await locker.WaitAsync();

                try
                {
                    aborted = true;
                    socket.Close();
                }
                finally
                {
                    locker.Release();
                }
            }
            catch (Exception ex)
            {
                PreStartup.LogException(ex);
            }
        }

#if NETFRAMEWORK
        public async Task SendPacketAsync(byte[] packet)
        {
            var buffer = new ArraySegment<byte>(packet);

#else
        public async Task SendPacketAsync(ReadOnlyMemory<byte> packet)
        {
#endif
            await locker.WaitAsync();

            try
            {
                if (!aborted)
                {
#if DEBUG
                    Logger.Log($"Tunnel_V3 sending game data for {PlayerID} from {socket.LocalEndPoint} to {socket.RemoteEndPoint}.");
#endif
#if NETFRAMEWORK
                    await socket.SendToAsync(buffer, SocketFlags.None, endPoint);
#else
                    await socket.SendToAsync(packet, SocketFlags.None, endPoint);
#endif
                }
                else
                {
                    Logger.Log($"Tunnel_V3 abort sending game data for {PlayerID} from {socket.LocalEndPoint} to {socket.RemoteEndPoint}.");
                }
            }
            finally
            {
                locker.Release();
            }
        }
    }
}