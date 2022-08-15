using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    internal sealed class GameTunnelHandler
    {
        public event EventHandler Connected;
        public event EventHandler ConnectionFailed;

        private V3TunnelConnection tunnelConnection;
        private Dictionary<uint, TunneledPlayerConnection> playerConnections = new();

        private readonly SemaphoreSlim locker = new(1, 1);

        public void SetUp(CnCNetTunnel tunnel, uint ourSenderId)
        {
            tunnelConnection = new V3TunnelConnection(tunnel, this, ourSenderId);
            tunnelConnection.Connected += TunnelConnection_Connected;
            tunnelConnection.ConnectionFailed += TunnelConnection_ConnectionFailed;
            tunnelConnection.ConnectionCut += TunnelConnection_ConnectionCut;
        }

        public void ConnectToTunnel()
        {
            if (tunnelConnection == null)
                throw new InvalidOperationException("GameTunnelHandler: Call SetUp before calling ConnectToTunnel.");

            tunnelConnection.ConnectAsync();
        }

        public int[] CreatePlayerConnections(List<uint> playerIds)
        {
            int[] ports = new int[playerIds.Count];
            playerConnections = new Dictionary<uint, TunneledPlayerConnection>();

            for (int i = 0; i < playerIds.Count; i++)
            {
                var playerConnection = new TunneledPlayerConnection(playerIds[i], this);
                playerConnection.CreateSocket();
                ports[i] = playerConnection.PortNumber;
                playerConnections.Add(playerIds[i], playerConnection);
                playerConnection.StartAsync();
            }

            return ports;
        }

        public void Clear()
        {
            locker.Wait();

            try
            {
                foreach (var connection in playerConnections)
                {
                    connection.Value.Stop();
                }

                playerConnections.Clear();

                if (tunnelConnection == null)
                    return;

                tunnelConnection.CloseConnection();
                tunnelConnection.Connected -= TunnelConnection_Connected;
                tunnelConnection.ConnectionFailed -= TunnelConnection_ConnectionFailed;
                tunnelConnection.ConnectionCut -= TunnelConnection_ConnectionCut;
                tunnelConnection = null;
            }
            finally
            {
                locker.Release();
            }
        }

#if NETFRAMEWORK
        public async Task PlayerConnection_PacketReceivedAsync(TunneledPlayerConnection sender, byte[] data)
#else
        public async Task PlayerConnection_PacketReceivedAsync(TunneledPlayerConnection sender, ReadOnlyMemory<byte> data)
#endif
        {
            await locker.WaitAsync();

            try
            {
                if (tunnelConnection != null)
                    await tunnelConnection.SendDataAsync(data, sender.PlayerID);
            }
            finally
            {
                locker.Release();
            }
        }

#if NETFRAMEWORK
        public async Task TunnelConnection_MessageReceivedAsync(byte[] data, uint senderId)
#else
        public async Task TunnelConnection_MessageReceivedAsync(ReadOnlyMemory<byte> data, uint senderId)
#endif
        {
            await locker.WaitAsync();

            try
            {
                if (playerConnections.TryGetValue(senderId, out TunneledPlayerConnection connection))
                    await connection.SendPacketAsync(data);
            }
            finally
            {
                locker.Release();
            }
        }

        private void TunnelConnection_Connected(object sender, EventArgs e)
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        private void TunnelConnection_ConnectionFailed(object sender, EventArgs e)
        {
            ConnectionFailed?.Invoke(this, EventArgs.Empty);
            Clear();
        }

        private void TunnelConnection_ConnectionCut(object sender, EventArgs e)
        {
            Clear();
        }
    }
}