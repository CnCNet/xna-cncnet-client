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
    private readonly Dictionary<uint, V3LocalPlayerConnection> localGameConnections = new();
    private readonly CancellationTokenSource connectionErrorCancellationTokenSource = new();

    private V3RemotePlayerConnection remoteHostConnection;
    private EventHandler<DataReceivedEventArgs> remoteHostConnectionDataReceivedFunc;
    private EventHandler<DataReceivedEventArgs> localGameConnectionDataReceivedFunc;

    /// <summary>
    /// Occurs when the connection to the remote host succeeded.
    /// </summary>
    public event EventHandler RaiseRemoteHostConnectedEvent;

    /// <summary>
    /// Occurs when the connection to the remote host could not be made.
    /// </summary>
    public event EventHandler RaiseRemoteHostConnectionFailedEvent;

    public bool ConnectSucceeded { get; private set; }

    public void SetUp(IPEndPoint remoteIpEndPoint, ushort localPort, uint gameLocalPlayerId, CancellationToken cancellationToken)
    {
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(connectionErrorCancellationTokenSource.Token, cancellationToken);

        remoteHostConnection = new V3RemotePlayerConnection();
        remoteHostConnectionDataReceivedFunc = (_, e) => RemoteHostConnection_DataReceivedAsync(e).HandleTask();
        localGameConnectionDataReceivedFunc = (_, e) => LocalGameConnection_DataReceivedAsync(e).HandleTask();

        remoteHostConnection.RaiseConnectedEvent += RemoteHostConnection_Connected;
        remoteHostConnection.RaiseConnectionFailedEvent += RemoteHostConnection_ConnectionFailed;
        remoteHostConnection.RaiseConnectionCutEvent += RemoteHostConnection_ConnectionCut;
        remoteHostConnection.RaiseDataReceivedEvent += remoteHostConnectionDataReceivedFunc;

        remoteHostConnection.SetUp(remoteIpEndPoint, localPort, gameLocalPlayerId, cancellationToken);
    }

    public IEnumerable<ushort> CreatePlayerConnections(List<uint> playerIds)
    {
        foreach (uint playerId in playerIds)
        {
            var localPlayerConnection = new V3LocalPlayerConnection();

            localPlayerConnection.RaiseConnectionCutEvent += LocalGameConnection_ConnectionCut;
            localPlayerConnection.RaiseDataReceivedEvent += localGameConnectionDataReceivedFunc;

            localGameConnections.Add(playerId, localPlayerConnection);

            yield return localPlayerConnection.Setup(playerId, connectionErrorCancellationTokenSource.Token);
        }
    }

    public void StartPlayerConnections()
    {
        foreach (KeyValuePair<uint, V3LocalPlayerConnection> playerConnection in localGameConnections)
            playerConnection.Value.StartConnectionAsync().HandleTask();
    }

    public void ConnectToTunnel()
    {
        if (remoteHostConnection == null)
            throw new InvalidOperationException($"Call SetUp before calling {nameof(ConnectToTunnel)}.");

        remoteHostConnection.StartConnectionAsync().HandleTask();
    }

    public void Dispose()
    {
        if (!connectionErrorCancellationTokenSource.IsCancellationRequested)
            connectionErrorCancellationTokenSource.Cancel();

        connectionErrorCancellationTokenSource.Dispose();

        foreach (KeyValuePair<uint, V3LocalPlayerConnection> localGamePlayerConnection in localGameConnections)
        {
            localGamePlayerConnection.Value.RaiseConnectionCutEvent -= LocalGameConnection_ConnectionCut;
            localGamePlayerConnection.Value.RaiseDataReceivedEvent -= localGameConnectionDataReceivedFunc;

            localGamePlayerConnection.Value.Dispose();
        }

        localGameConnections.Clear();

        if (remoteHostConnection == null)
            return;

        remoteHostConnection.RaiseConnectedEvent -= RemoteHostConnection_Connected;
        remoteHostConnection.RaiseConnectionFailedEvent -= RemoteHostConnection_ConnectionFailed;
        remoteHostConnection.RaiseConnectionCutEvent -= RemoteHostConnection_ConnectionCut;
        remoteHostConnection.RaiseDataReceivedEvent -= remoteHostConnectionDataReceivedFunc;

        remoteHostConnection.Dispose();
    }

    private void LocalGameConnection_ConnectionCut(object sender, EventArgs e)
    {
        var localGamePlayerConnection = sender as V3LocalPlayerConnection;

        localGameConnections.Remove(localGameConnections.Single(q => q.Value == localGamePlayerConnection).Key);

        localGamePlayerConnection.RaiseConnectionCutEvent -= LocalGameConnection_ConnectionCut;
        localGamePlayerConnection.RaiseDataReceivedEvent -= localGameConnectionDataReceivedFunc;
        localGamePlayerConnection.Dispose();
    }

    /// <summary>
    /// Forwards local game data to the remote host.
    /// </summary>
    private ValueTask LocalGameConnection_DataReceivedAsync(DataReceivedEventArgs e)
        => remoteHostConnection?.SendDataAsync(e.GameData, e.PlayerId) ?? ValueTask.CompletedTask;

    /// <summary>
    /// Forwards remote player data to the local game.
    /// </summary>
    private ValueTask RemoteHostConnection_DataReceivedAsync(DataReceivedEventArgs e)
        => GetLocalPlayerConnection(e.PlayerId)?.SendDataAsync(e.GameData) ?? ValueTask.CompletedTask;

    private V3LocalPlayerConnection GetLocalPlayerConnection(uint senderId)
        => localGameConnections.TryGetValue(senderId, out V3LocalPlayerConnection connection) ? connection : null;

    private void RemoteHostConnection_Connected(object sender, EventArgs e)
    {
        ConnectSucceeded = true;

        OnRaiseRemoteHostConnectedEvent(EventArgs.Empty);
    }

    private void RemoteHostConnection_ConnectionFailed(object sender, EventArgs e)
        => OnRaiseRemoteHostConnectionFailedEvent(EventArgs.Empty);

    private void OnRaiseRemoteHostConnectedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseRemoteHostConnectedEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void OnRaiseRemoteHostConnectionFailedEvent(EventArgs e)
    {
        EventHandler raiseEvent = RaiseRemoteHostConnectionFailedEvent;

        raiseEvent?.Invoke(this, e);
    }

    private void RemoteHostConnection_ConnectionCut(object sender, EventArgs e)
        => Dispose();
}