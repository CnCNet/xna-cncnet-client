#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Represents a single ping measurement sent to a tunnel or peer,
/// including send and receive times, and the computed round-trip time.
/// </summary>
public class PingResult
{
    public int ID { get; set; }
    public long SentTimeTicks { get; set; }
    public long? ReceivedTimeTicks { get; set; }

    public double? RoundTripTime => ReceivedTimeTicks.HasValue ? (ReceivedTimeTicks.Value - SentTimeTicks) * 1000.0 / Stopwatch.Frequency : null;

    /// <summary>
    /// A task completion source that can be awaited
    /// until the ping succeeds or times out.
    /// </summary>
    public TaskCompletionSource<bool> CompletionSource { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Tracks connection and ping statistics for a single tunnel
/// during peer-to-peer negotiation.
/// </summary>
public class TunnelTestResult
{
    // How long the non-decider will keep sending Connected packets to a single tunnel
    // while waiting for a Ping Request from the decider. After this, the tunnel is skipped.
    // Note that the existing players in the lobby will begin negotiating when
    // Channel_UserAdded is called, while the joining player will begin negotiation
    // when ApplyPlayerOptions is sent by the host. The timeout should be long enough for
    // the joining player to receive that IRC message + attempt connections to each tunnel.
    // Shares its value with V3PlayerNegotiator's connected-phase timeout (the same
    // handshake, seen from each side) - configurable via NetworkDefinitions.ini
    // ([V3TunnelNegotiation] ConnectedPhaseTimeoutMs).
    /// <summary>
    /// How long the non-decider keeps offering this tunnel before giving up. Set by the negotiator
    /// to match the decider's budget, so neither side waits on a handshake the other has stopped
    /// attempting.
    /// </summary>
    public int ConnectedTimeoutMs { get; set; } = ClientConfiguration.Instance.V3ConnectedPhaseTimeoutMs;

    private readonly object _pingLock = new();
    private readonly List<PingResult> _pingResults = [];

    /// <summary>
    /// Records a new ping attempt and returns it.
    /// </summary>
    public PingResult AddPing(int id, long sentTimeTicks)
    {
        var ping = new PingResult { ID = id, SentTimeTicks = sentTimeTicks };
        lock (_pingLock)
            _pingResults.Add(ping);
        return ping;
    }

    /// <summary>
    /// Marks the ping with the given id as received. Returns true if it was an
    /// outstanding ping that got completed.
    /// </summary>
    public bool CompletePing(int id, long receivedTimeTicks)
    {
        lock (_pingLock)
        {
            var ping = _pingResults.FirstOrDefault(p => p.ID == id);
            if (ping == null || ping.ReceivedTimeTicks.HasValue)
                return false;

            ping.ReceivedTimeTicks = receivedTimeTicks;
            ping.CompletionSource.TrySetResult(true);
            return true;
        }
    }

    /// <summary>
    /// Returns the count of successful and total pings.
    /// </summary>
    public (int successful, int total) GetPingCounts()
    {
        lock (_pingLock)
            return (_pingResults.Count(p => p.RoundTripTime.HasValue), _pingResults.Count);
    }

    public bool ConnectedReceived { get; set; }

    /// <summary>
    /// A completion source that resolves when a "Connected" packet is received.
    /// </summary>
    public TaskCompletionSource<bool> ConnectedTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// A completion source that resolves when all ping attempts are completed.
    /// </summary>
    public TaskCompletionSource<bool> PingsCompletedTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public double? AverageRtt
    {
        get
        {
            lock (_pingLock)
            {
                double sum = 0;
                int count = 0;
                foreach (var p in _pingResults)
                {
                    if (p.RoundTripTime.HasValue)
                    {
                        sum += p.RoundTripTime.Value;
                        count++;
                    }
                }

                return count > 0 ? sum / count : null;
            }
        }
    }

    public double PacketLoss
    {
        get
        {
            lock (_pingLock)
            {
                if (_pingResults.Count == 0)
                    return 100;

                return _pingResults.Count(p => !p.RoundTripTime.HasValue) * 100.0 / _pingResults.Count;
            }
        }
    }

    public DateTime? FirstConnectedSentTime { get; set; }
    public bool ConnectedTimedOut => FirstConnectedSentTime.HasValue &&
        (DateTime.UtcNow - FirstConnectedSentTime.Value).TotalMilliseconds > ConnectedTimeoutMs;

    /// <summary>
    /// Whether the non-decider gave up offering this tunnel because its connect budget ran out.
    /// Latched, unlike <see cref="ConnectedTimedOut"/>, which is recomputed from the clock and so
    /// reads true for every tunnel once the negotiation outlives the budget.
    /// </summary>
    public bool ConnectedAbandoned { get; set; }

    public bool PingRequestReceived { get; set; }
}

/// <summary>
/// Outcome of a call to <see cref="V3PlayerInfo.StartNegotiation"/>.
/// </summary>
public enum NegotiationStartResult
{
    Started,
    AlreadyInProgress,
    Failed
}

/// <summary>
/// A lobby's player for V3 tunnel-based negotiation and communication.
/// </summary>
public class V3PlayerInfo(uint id, string name, int playerIndex, ushort playerGameID)
{
    // Configurable via NetworkDefinitions.ini ([V3TunnelNegotiation] PacketLossWeight).
    private static int PACKET_LOSS_WEIGHT => ClientConfiguration.Instance.V3PacketLossWeight;
    private V3PlayerNegotiator? _negotiator;

    public uint Id { get; set; } = id;
    public string Name { get; set; } = name;
    public int PlayerIndex { get; set; } = playerIndex;
    public ushort PlayerGameId { get; set; } = playerGameID;
    public bool HasNegotiated { get; set; }
    public bool IsNegotiating { get; set; }
    public CnCNetTunnel? Tunnel { get; set; }

    /// <summary>
    /// Packet loss (percentage) measured for the chosen tunnel during negotiation.
    /// Set on both peers: the decider measures it, the non-decider receives it in the
    /// TunnelChoice packet, so both can display the same stats without extra IRC traffic.
    /// </summary>
    public double? NegotiatedPacketLoss { get; set; }

    /// <summary>
    /// Whether the remote player has P2P enabled. Learned from the TunnelAck/TunnelChoice
    /// payload at the end of relay round 1, so the upgrade round is only attempted when
    /// both sides have opted in.
    /// </summary>
    public bool P2PEnabled { get; set; }
    public V3PlayerNegotiator? Negotiator => _negotiator;
    public ConcurrentDictionary<CnCNetTunnel, TunnelTestResult> TunnelResults { get; } = new();

    /// <summary>
    /// Creates a fresh set of <see cref="TunnelTestResult"/> entries for all available tunnels.
    /// </summary>
    public void InitializeTunnelResults(List<CnCNetTunnel> tunnels, int connectedTimeoutMs)
    {
        TunnelResults.Clear();
        foreach (var tunnel in tunnels)
            TunnelResults[tunnel] = new TunnelTestResult { ConnectedTimeoutMs = connectedTimeoutMs };
    }

    /// <summary>
    /// Retrieves the <see cref="TunnelTestResult"/> for the specified tunnel, or null if not found.
    /// </summary>
    public TunnelTestResult? GetTunnelResult(CnCNetTunnel tunnel) => TunnelResults.TryGetValue(tunnel, out var result) ? result : null;

    /// <summary>
    /// Registers a tunnel at runtime (e.g. a P2P tunnel discovered post-relay-negotiation)
    /// and returns its fresh <see cref="TunnelTestResult"/>.
    /// </summary>
    public TunnelTestResult AddTunnelResult(CnCNetTunnel tunnel)
    {
        var result = new TunnelTestResult();
        TunnelResults[tunnel] = result;
        return result;
    }

    /// <summary>
    /// Selects the best available tunnel based on RTT and packet loss
    /// </summary>
    public CnCNetTunnel? SelectBestTunnel()
    {
        var bestTunnel = TunnelResults
            .Where(kvp => kvp.Value.AverageRtt.HasValue)
            .OrderBy(kvp => kvp.Value.AverageRtt!.Value + kvp.Value.PacketLoss * PACKET_LOSS_WEIGHT) //20% packet loss = 200ms penalty
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        if (bestTunnel != null)
            Tunnel = bestTunnel;

        return bestTunnel;
    }

    public void SetNegotiator(V3PlayerNegotiator negotiator)
    {
        StopNegotiation();
        _negotiator = negotiator;
    }

    public void StopNegotiation()
    {
        V3PlayerNegotiator? negotiator = Interlocked.Exchange(ref _negotiator, null);
        negotiator?.Dispose();
    }

    /// <summary>
    /// Stops and disposes <paramref name="negotiator"/> only if it is still the active
    /// negotiator. Used by <see cref="NegotiationWorkerAsync"/>'s own cleanup so a worker
    /// whose negotiator was already replaced (e.g. by <see cref="SetNegotiator"/> during a
    /// renegotiation restart) can't reach back and dispose the replacement out from under it.
    /// </summary>
    private bool StopNegotiationIfCurrent(V3PlayerNegotiator negotiator)
    {
        if (Interlocked.CompareExchange(ref _negotiator, null, negotiator) != negotiator)
            return false;

        negotiator.Dispose();
        return true;
    }

    public void ResetNegotiator()
    {
        Tunnel = null;
        NegotiatedPacketLoss = null;
        P2PEnabled = false;
        IsNegotiating = false;
        HasNegotiated = false;
    }

    public NegotiationStartResult StartNegotiation(
        V3PlayerInfo localPlayer,
        TunnelHandler tunnelHandler,
        List<CnCNetTunnel> availableTunnels,
        bool p2pEnabled = false)
    {
        if (this == localPlayer)
            throw new InvalidOperationException("Cannot start negotiation with yourself.");

        Logger.Log($"V3PlayerInfo: Starting negotiation with {Name} (ID: {Id})");

        if (Negotiator != null)
            return NegotiationStartResult.AlreadyInProgress;

        if (availableTunnels.Count == 0)
        {
            Logger.Log($"V3PlayerInfo: No available V3 tunnels for negotiation with {Name} (ID: {Id})");
            HasNegotiated = true;
            IsNegotiating = false;
            return NegotiationStartResult.Failed;
        }

        HasNegotiated = false;
        IsNegotiating = true;

        var negotiator = new V3PlayerNegotiator(localPlayer, this, availableTunnels, tunnelHandler, p2pEnabled);
        SetNegotiator(negotiator);

        _ = NegotiationWorkerAsync(negotiator);

        return NegotiationStartResult.Started;
    }

    /// <summary>
    /// Background worker that runs the negotiation.
    /// </summary>
    private async Task NegotiationWorkerAsync(V3PlayerNegotiator negotiator)
    {
        try
        {
            bool success = await negotiator.NegotiateAsync().ConfigureAwait(false);
            if (!success)
            {
                Logger.Log($"V3PlayerInfo: Negotiation failed for player {Name} (ID: {Id})");
                StopNegotiationIfCurrent(negotiator);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"V3PlayerInfo: Negotiation error with player {Name} (ID: {Id}): {ex.Message}");
            StopNegotiationIfCurrent(negotiator);
        }

        Logger.Log($"V3PlayerInfo: Negotiation finished for {Name} (ID: {Id})");
    }
}
