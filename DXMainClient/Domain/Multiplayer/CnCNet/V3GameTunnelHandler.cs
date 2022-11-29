using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal sealed class V3GameTunnelHandler
{
    private readonly Dictionary<uint, V3TunneledPlayerConnection> playerConnections = new();

    private V3TunnelConnection tunnelConnection;

    public event EventHandler Connected;

    public event EventHandler ConnectionFailed;

    public bool IsConnected { get; private set; }

    public static int GetFreePort(IEnumerable<int> playerPorts)
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

    public void SetUp(CnCNetTunnel tunnel, uint localId)
    {
        tunnelConnection = new V3TunnelConnection(tunnel, this, localId);
        tunnelConnection.Connected += TunnelConnection_Connected;
        tunnelConnection.ConnectionFailed += TunnelConnection_ConnectionFailed;
        tunnelConnection.ConnectionCut += TunnelConnection_ConnectionCut;
    }

    public List<int> CreatePlayerConnections(List<uint> playerIds)
    {
        int[] ports = new int[playerIds.Count];

        for (int i = 0; i < playerIds.Count; i++)
        {
            var playerConnection = new V3TunneledPlayerConnection(playerIds[i], this);

            playerConnection.CreateSocket();

            ports[i] = playerConnection.PortNumber;

            playerConnections.Add(playerIds[i], playerConnection);
        }

        return ports.ToList();
    }

    public void StartPlayerConnections(int gamePort)
    {
        foreach (KeyValuePair<uint, V3TunneledPlayerConnection> playerConnection in playerConnections)
        {
            playerConnection.Value.StartAsync(gamePort).HandleTask();
        }
    }

    public void ConnectToTunnel()
    {
        if (tunnelConnection == null)
            throw new InvalidOperationException("GameTunnelHandler: Call SetUp before calling ConnectToTunnel.");

        tunnelConnection.ConnectAsync().HandleTask();
    }

    public void Clear()
    {
        ClearPlayerConnections();

        if (tunnelConnection == null)
            return;

        tunnelConnection.CloseConnection();

        tunnelConnection.Connected -= TunnelConnection_Connected;
        tunnelConnection.ConnectionFailed -= TunnelConnection_ConnectionFailed;
        tunnelConnection.ConnectionCut -= TunnelConnection_ConnectionCut;

        tunnelConnection = null;
    }

    public async Task PlayerConnection_PacketReceivedAsync(V3TunneledPlayerConnection sender, ReadOnlyMemory<byte> data)
    {
        if (tunnelConnection != null)
            await tunnelConnection.SendDataAsync(data, sender.PlayerId);
    }

    public async Task TunnelConnection_MessageReceivedAsync(ReadOnlyMemory<byte> data, uint senderId)
    {
        V3TunneledPlayerConnection connection = GetPlayerConnection(senderId);

        if (connection is not null)
            await connection.SendPacketAsync(data);
    }

    private V3TunneledPlayerConnection GetPlayerConnection(uint senderId)
    {
        if (playerConnections.TryGetValue(senderId, out V3TunneledPlayerConnection connection))
            return connection;

        return null;
    }

    private void ClearPlayerConnections()
    {
        foreach (KeyValuePair<uint, V3TunneledPlayerConnection> connection in playerConnections)
        {
            connection.Value.Stop();
        }

        playerConnections.Clear();
    }

    private void TunnelConnection_Connected(object sender, EventArgs e)
    {
        IsConnected = true;

        Connected?.Invoke(this, EventArgs.Empty);
    }

    private void TunnelConnection_ConnectionFailed(object sender, EventArgs e)
    {
        IsConnected = false;

        ConnectionFailed?.Invoke(this, EventArgs.Empty);
        Clear();
    }

    private void TunnelConnection_ConnectionCut(object sender, EventArgs e)
    {
        Clear();
    }
}