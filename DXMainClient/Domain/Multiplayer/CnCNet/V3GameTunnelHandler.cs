using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Manages connections between one or more <see cref="V3LocalPlayerConnection"/>s representing local game players and a <see cref="V3RemotePlayerConnection"/> representing a remote host.
/// </summary>
internal sealed class V3GameTunnelHandler : IDisposable
{
    private readonly Dictionary<uint, V3LocalPlayerConnection> playerConnections = new();

    private CancellationToken cancellationToken;
    private V3RemotePlayerConnection remoteHostConnection;
    private EventHandler<GameDataReceivedEventArgs> gameDataReceivedFunc;

    public event EventHandler RaiseConnectedEvent;

    public event EventHandler RaiseConnectionFailedEvent;

    public bool IsConnected { get; private set; }

    public void SetUp(IPEndPoint remoteIpEndPoint, ushort localPort, uint gameLocalPlayerId, CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        remoteHostConnection = new V3RemotePlayerConnection();
        gameDataReceivedFunc = (_, e) => RemoteHostConnection_MessageReceivedAsync(e).HandleTask();

        remoteHostConnection.RaiseConnectedEvent += RemoteHostConnection_Connected;
        remoteHostConnection.RaiseConnectionFailedEvent += RemoteHostConnection_ConnectionFailed;
        remoteHostConnection.RaiseConnectionCutEvent += RemoteHostConnection_ConnectionCut;
        remoteHostConnection.RaiseGameDataReceivedEvent += gameDataReceivedFunc;

        remoteHostConnection.SetUp(remoteIpEndPoint, localPort, gameLocalPlayerId, cancellationToken);
    }

    public List<ushort> CreatePlayerConnections(List<uint> playerIds)
    {
        ushort[] ports = new ushort[playerIds.Count];

        for (int i = 0; i < playerIds.Count; i++)
        {
            var playerConnection = new V3LocalPlayerConnection();

            playerConnection.RaiseGameDataReceivedEvent += (_, e) => PlayerConnection_PacketReceivedAsync(e).HandleTask();
            playerConnection.Setup(playerIds[i], cancellationToken);

            ports[i] = playerConnection.PortNumber;

            playerConnections.Add(playerIds[i], playerConnection);
        }

        return ports.ToList();
    }

    public void StartPlayerConnections(int gamePort)
    {
        foreach (KeyValuePair<uint, V3LocalPlayerConnection> playerConnection in playerConnections)
        {
            playerConnection.Value.StartConnectionAsync(gamePort).HandleTask();
        }
    }

    public void ConnectToTunnel()
    {
        if (remoteHostConnection == null)
            throw new InvalidOperationException($"Call SetUp before calling {nameof(ConnectToTunnel)}.");

        remoteHostConnection.StartConnectionAsync().HandleTask();
    }

    /// <summary>
    /// Forwards local game data to the remote host.
    /// </summary>
    private ValueTask PlayerConnection_PacketReceivedAsync(GameDataReceivedEventArgs e)
        => remoteHostConnection?.SendDataAsync(e.GameData, e.PlayerId) ?? ValueTask.CompletedTask;

    /// <summary>
    /// Forwards remote player data to the local game.
    /// </summary>
    private ValueTask RemoteHostConnection_MessageReceivedAsync(GameDataReceivedEventArgs e)
    {
        V3LocalPlayerConnection localPlayerConnection = GetLocalPlayerConnection(e.PlayerId);

        return localPlayerConnection?.SendDataAsync(e.GameData) ?? ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        foreach (KeyValuePair<uint, V3LocalPlayerConnection> remotePlayerGameConnection in playerConnections)
        {
            remotePlayerGameConnection.Value.Dispose();
        }

        playerConnections.Clear();

        if (remoteHostConnection == null)
            return;

        remoteHostConnection.RaiseConnectedEvent -= RemoteHostConnection_Connected;
        remoteHostConnection.RaiseConnectionFailedEvent -= RemoteHostConnection_ConnectionFailed;
        remoteHostConnection.RaiseConnectionCutEvent -= RemoteHostConnection_ConnectionCut;
        remoteHostConnection.RaiseGameDataReceivedEvent -= gameDataReceivedFunc;

        remoteHostConnection.Dispose();
    }

    private V3LocalPlayerConnection GetLocalPlayerConnection(uint senderId)
        => playerConnections.TryGetValue(senderId, out V3LocalPlayerConnection connection) ? connection : null;

    private void RemoteHostConnection_Connected(object sender, EventArgs e)
    {
        IsConnected = true;

        OnRaiseConnectedEvent(EventArgs.Empty);
    }

    private void RemoteHostConnection_ConnectionFailed(object sender, EventArgs e)
    {
        IsConnected = false;

        OnRaiseConnectionFailedEvent(EventArgs.Empty);
    }

    private void OnRaiseConnectedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseConnectedEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void OnRaiseConnectionFailedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseConnectionFailedEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void RemoteHostConnection_ConnectionCut(object sender, EventArgs e)
    {
        Dispose();
    }
}