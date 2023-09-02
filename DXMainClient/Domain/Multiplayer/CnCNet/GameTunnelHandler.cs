﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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

            tunnelConnection.ConnectAsync().HandleTask();
        }

        public Tuple<int[], int> CreatePlayerConnections(List<uint> playerIds)
        {
            int[] ports = new int[playerIds.Count];
            playerConnections = new Dictionary<uint, TunneledPlayerConnection>();

            for (int i = 0; i < playerIds.Count; i++)
            {
                var playerConnection = new TunneledPlayerConnection(playerIds[i], this);
                playerConnection.CreateSocket();
                ports[i] = playerConnection.PortNumber;
                playerConnections.Add(playerIds[i], playerConnection);
            }

            int gamePort = GetFreePort(ports);

            foreach (KeyValuePair<uint, TunneledPlayerConnection> playerConnection in playerConnections)
            {
                playerConnection.Value.StartAsync(gamePort).HandleTask();
            }

            return new Tuple<int[], int>(ports, gamePort);
        }

        private static int GetFreePort(int[] playerPorts)
        {
            IPEndPoint[] endPoints = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
            int[] usedPorts = endPoints.Select(q => q.Port).ToArray().Concat(playerPorts).ToArray();
            int selectedPort = 0;

            while (selectedPort == 0 || usedPorts.Contains(selectedPort))
            {
                selectedPort = new Random().Next(1, 65535);
            }

            return selectedPort;
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

        public async Task PlayerConnection_PacketReceivedAsync(TunneledPlayerConnection sender, ReadOnlyMemory<byte> data)
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

        public async Task TunnelConnection_MessageReceivedAsync(ReadOnlyMemory<byte> data, uint senderId)
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