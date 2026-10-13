#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;

using Rampastring.XNAUI;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Sends keepalive ping round trips on the negotiated V3 paths while a lobby waits for
/// game start: keeps tunnel registrations and P2P NAT mappings from expiring, measures
/// live per-pair round-trip times, and detects unreachable peers far faster than IRC
/// does. Owned and ticked by <see cref="TunnelHandler"/>.
/// </summary>
public class V3KeepAliveMonitor
{
    /// <summary>
    /// How often each negotiated path gets a keepalive ping while sitting in a lobby.
    /// Keeps the tunnel server registration and — crucially — the P2P NAT mappings from
    /// expiring during long waits between negotiation and game start; typical NAT UDP
    /// timeouts are 30-180 seconds.
    /// </summary>
    /// Configurable via NetworkDefinitions.ini ([V3TunnelNegotiation] KeepAliveIntervalSeconds).
    private static double KEEPALIVE_INTERVAL_SECONDS => ClientConfiguration.Instance.V3KeepAliveIntervalSeconds;

    /// <summary>How often the keepalive state machine runs; also the retry cadence for unanswered pings.
    /// Configurable via NetworkDefinitions.ini ([V3TunnelNegotiation] KeepAliveTickSeconds).</summary>
    private static double KEEPALIVE_TICK_SECONDS => ClientConfiguration.Instance.V3KeepAliveTickSeconds;

    /// <summary>
    /// Unanswered pings in a row before a peer is declared unreachable (~15-20 seconds
    /// after their connection actually died — far faster than IRC notices it).
    /// Configurable via NetworkDefinitions.ini ([V3TunnelNegotiation] KeepAliveMaxMisses).
    /// </summary>
    private static int KEEPALIVE_MAX_MISSES => ClientConfiguration.Instance.V3KeepAliveMaxMisses;

    private readonly V3TunnelCommunicator _communicator;
    private readonly WindowManager _windowManager;

    private uint _localId;
    // Snapshot list, replaced atomically by the negotiation manager (possibly from a
    // negotiation task) and read on the game loop thread.
    private volatile List<(uint remoteId, CnCNetTunnel tunnel)>? _targets;
    private int _targetGeneration;
    private long _lastTickTicks;
    private long _lastRegistrationRefreshTicks;
    private readonly ConcurrentDictionary<uint, KeepAliveTracker> _trackers = new();

    public V3KeepAliveMonitor(V3TunnelCommunicator communicator, WindowManager windowManager)
    {
        _communicator = communicator;
        _windowManager = windowManager;
        _communicator.KeepAlivePongReceived = Communicator_KeepAlivePongReceived;
        _communicator.ProbeRequestReceived = Communicator_ProbeRequestReceived;
        _communicator.ProbeReportReceived = Communicator_ProbeReportReceived;
    }

    /// <summary>
    /// Fired on the game thread when a keepalive pong arrives: remote player's V3 ID and
    /// the measured round-trip time in milliseconds.
    /// </summary>
    public event Action<uint, int>? PongReceived;

    /// <summary>
    /// Fired on the game thread when a peer has missed <see cref="KEEPALIVE_MAX_MISSES"/>
    /// keepalive pings in a row and should be considered unreachable.
    /// </summary>
    public event Action<uint>? TimedOut;

    /// <summary>
    /// Per-peer keepalive bookkeeping. Pong timestamps are written on the receive thread
    /// and read on the game thread and probe tasks, so the 64-bit tick fields go through
    /// Interlocked (long reads/writes aren't atomic on the 32-bit XNA build).
    /// </summary>
    private sealed class KeepAliveTracker
    {
        private long lastPongTicks;
        private long lastPingTicks;

        public CnCNetTunnel Tunnel;
        public int ConsecutiveMisses;
        public bool TimedOutReported;

        public KeepAliveTracker(CnCNetTunnel tunnel, long nowTicks)
        {
            Tunnel = tunnel;
            lastPongTicks = nowTicks;
        }

        public long LastPongTicks => Interlocked.Read(ref lastPongTicks);
        public long LastPingTicks => Interlocked.Read(ref lastPingTicks);

        public void RecordPing(long ticks) => Interlocked.Exchange(ref lastPingTicks, ticks);
        public void RecordPong(long ticks) => Interlocked.Exchange(ref lastPongTicks, ticks);

        public void ResetBaseline(long nowTicks)
        {
            Interlocked.Exchange(ref lastPongTicks, nowTicks);
            Interlocked.Exchange(ref lastPingTicks, 0);
            ConsecutiveMisses = 0;
            TimedOutReported = false;
        }
    }

    /// <summary>
    /// Sets the negotiated paths to keep alive while in a lobby. Pass the local player's
    /// V3 ID and each remote player's ID with the tunnel negotiated for that pair.
    /// </summary>
    public void SetTargets(uint localId, List<(uint remoteId, CnCNetTunnel tunnel)> targets)
    {
        _localId = localId;
        _targets = targets;
        Interlocked.Increment(ref _targetGeneration);
        _lastProbeReply = null;
    }

    /// <summary>Stops lobby keepalives. Call on lobby teardown or when leaving dynamic mode.</summary>
    public void ClearTargets()
    {
        _targets = null;
        _trackers.Clear();
        Interlocked.Increment(ref _targetGeneration);
        _lastProbeReply = null;
        _probeReports = null;
    }

    /// <summary>
    /// Runs the keepalive state machine; call once per game-loop update — self-throttled to
    /// <see cref="KEEPALIVE_TICK_SECONDS"/>. Pass whether the in-game tunnel bridge is
    /// running, since in-game traffic proves liveness on its own.
    /// </summary>
    public void Update(bool gameBridgeRunning)
    {
        long now = Stopwatch.GetTimestamp();
        if ((now - _lastTickTicks) / (double)Stopwatch.Frequency < KEEPALIVE_TICK_SECONDS)
            return;

        _lastTickTicks = now;
        ProcessTick(gameBridgeRunning, now);
    }

    private void ProcessTick(bool gameBridgeRunning, long now)
    {
        var targets = _targets;
        if (targets == null || targets.Count == 0)
        {
            _trackers.Clear();
            return;
        }

        // Relay tunnels: refresh our registration (also refreshes our own NAT mapping,
        // which keeps the session's STUN-discovered external endpoint valid). This must
        // run in-game too: peers who returned to the lobby before us keep pinging us
        // through these relays, and an expired registration would drop their pings and
        // make them falsely declare us unreachable.
        if ((now - _lastRegistrationRefreshTicks) / (double)Stopwatch.Frequency >= KEEPALIVE_INTERVAL_SECONDS)
        {
            _lastRegistrationRefreshTicks = now;

            var relayTunnels = targets
                .Select(t => t.tunnel)
                .Where(t => t != null && !t.IsDirect)
                .Distinct()
                .ToList();

            if (relayTunnels.Count > 0)
                _communicator.SendRegistrationToTunnels(_localId, relayTunnels, quiet: true);
        }

        // In-game the bridge traffic proves liveness on its own; pause monitoring but
        // keep baselines fresh so returning to the lobby doesn't trigger instant
        // false timeouts from an hour-old "last pong".
        if (gameBridgeRunning)
        {
            foreach (var tracker in _trackers.Values)
                tracker.ResetBaseline(now);

            return;
        }

        var activeIds = new HashSet<uint>();

        foreach (var (remoteId, tunnel) in targets)
        {
            if (tunnel == null)
                continue;

            activeIds.Add(remoteId);

            var tracker = _trackers.GetOrAdd(remoteId, _ => new KeepAliveTracker(tunnel, now));
            if (tracker.Tunnel != tunnel)
            {
                // Pair renegotiated onto a different path; measure it from scratch. Compared by
                // value so a rebuilt instance for the same endpoint doesn't discard the baseline.
                tracker.Tunnel = tunnel;
                tracker.ResetBaseline(now);
            }

            double secondsSincePing = (now - tracker.LastPingTicks) / (double)Stopwatch.Frequency;
            bool awaitingPong = tracker.LastPingTicks > tracker.LastPongTicks;

            if (awaitingPong)
            {
                // Unanswered — retry at the faster tick cadence and count the miss.
                if (secondsSincePing >= KEEPALIVE_TICK_SECONDS)
                {
                    tracker.ConsecutiveMisses++;

                    if (tracker.ConsecutiveMisses >= KEEPALIVE_MAX_MISSES && !tracker.TimedOutReported)
                    {
                        tracker.TimedOutReported = true;
                        TimedOut?.Invoke(remoteId);
                    }

                    SendKeepAlivePing(remoteId, tunnel);
                }
            }
            else if (secondsSincePing >= KEEPALIVE_INTERVAL_SECONDS)
            {
                SendKeepAlivePing(remoteId, tunnel);
            }
        }

        foreach (uint id in _trackers.Keys)
        {
            if (!activeIds.Contains(id))
                _trackers.TryRemove(id, out _);
        }
    }

    private void SendKeepAlivePing(uint remoteId, CnCNetTunnel tunnel)
    {
        var tracker = _trackers.GetOrAdd(remoteId, _ => new KeepAliveTracker(tunnel, Stopwatch.GetTimestamp()));

        var payload = new byte[8];
        long now = Stopwatch.GetTimestamp();
        BinaryPrimitives.WriteInt64LittleEndian(payload, now);

        _communicator.SendPacket(tunnel, _localId, remoteId, TunnelPacketType.KeepAlivePing, payload);
        tracker.RecordPing(now);
    }

    // Receive thread: record the pong, then hop to the game thread for the state
    // machine bookkeeping and the public event.
    private void Communicator_KeepAlivePongReceived(uint remoteId, int rttMs)
    {
        if (_trackers.TryGetValue(remoteId, out var tracker))
            tracker.RecordPong(Stopwatch.GetTimestamp());

        _windowManager.AddCallback(new Action<uint, int>(DoPongReceived), remoteId, rttMs);
    }

    private void DoPongReceived(uint remoteId, int rttMs)
    {
        if (_trackers.TryGetValue(remoteId, out var tracker))
        {
            tracker.ConsecutiveMisses = 0;
            tracker.TimedOutReported = false;
        }

        PongReceived?.Invoke(remoteId, rttMs);
    }

    /// <summary>
    /// Actively pings every keepalive target and waits for fresh pongs — used as a
    /// launch-time connectivity check, since IRC can take minutes to notice a dead
    /// connection. Returns the V3 IDs of peers that did not answer within the timeout.
    /// Safe to call from any thread.
    /// </summary>
    public async Task<List<uint>> ProbeTargetsAsync(int timeoutMs = 3000, int resendIntervalMs = 1000)
    {
        var unresponsive = new List<uint>();
        var targets = _targets;
        if (targets == null || targets.Count == 0)
            return unresponsive;

        long probeStart = Stopwatch.GetTimestamp();
        var pending = targets
            .Where(t => t.tunnel != null)
            .GroupBy(t => t.remoteId)
            .Select(g => g.First())
            .ToList();
        var responded = new HashSet<uint>();

        var elapsed = Stopwatch.StartNew();
        long nextSendMs = 0;

        while (elapsed.ElapsedMilliseconds < timeoutMs && responded.Count < pending.Count)
        {
            if (elapsed.ElapsedMilliseconds >= nextSendMs)
            {
                foreach (var (remoteId, tunnel) in pending)
                {
                    if (!responded.Contains(remoteId))
                        SendKeepAlivePing(remoteId, tunnel);
                }

                nextSendMs += resendIntervalMs;
            }

            await Task.Delay(50).ConfigureAwait(false);

            foreach (var (remoteId, _) in pending)
            {
                if (!responded.Contains(remoteId) &&
                    _trackers.TryGetValue(remoteId, out var tracker) &&
                    tracker.LastPongTicks > probeStart)
                {
                    responded.Add(remoteId);
                }
            }
        }

        foreach (var (remoteId, _) in pending)
        {
            if (!responded.Contains(remoteId))
                unresponsive.Add(remoteId);
        }

        return unresponsive;
    }

    /// <summary>How long a completed probe result is answered from cache, covering the requester's UDP resends.
    /// Configurable via NetworkDefinitions.ini ([V3TunnelNegotiation] ProbeReplyCacheSeconds).</summary>
    private static double PROBE_REPLY_CACHE_SECONDS => ClientConfiguration.Instance.V3ProbeReplyCacheSeconds;

    private int _probeRunning;
    private volatile ProbeReply? _lastProbeReply;
    private volatile ConcurrentDictionary<uint, List<uint>>? _probeReports;

    private sealed class ProbeReply
    {
        public readonly byte[] Payload;
        public readonly long CompletedTicks;

        public ProbeReply(byte[] payload, long completedTicks)
        {
            Payload = payload;
            CompletedTicks = completedTicks;
        }
    }

    /// <summary>
    /// The distributed pre-launch connectivity check (host side): probes the local paths
    /// and, over the same UDP paths, asks every peer to probe theirs and report back —
    /// so every pair gets a fresh round trip, not just the host's own connections.
    /// Requests are resent until each peer's report arrives or the timeout elapses.
    /// Safe to call from any thread.
    /// </summary>
    public async Task<LaunchProbeResult> ProbeAllPairsAsync(int timeoutMs = 6000, int resendIntervalMs = 1000)
    {
        var result = new LaunchProbeResult();
        var targets = _targets;
        if (targets == null || targets.Count == 0)
            return result;

        var pending = targets
            .Where(t => t.tunnel != null)
            .GroupBy(t => t.remoteId)
            .Select(g => g.First())
            .ToList();

        var reports = new ConcurrentDictionary<uint, List<uint>>();
        _probeReports = reports;

        try
        {
            Task<List<uint>> localProbeTask = ProbeTargetsAsync();

            var elapsed = Stopwatch.StartNew();
            long nextSendMs = 0;

            while (elapsed.ElapsedMilliseconds < timeoutMs)
            {
                if (elapsed.ElapsedMilliseconds >= nextSendMs)
                {
                    foreach (var (remoteId, tunnel) in pending)
                    {
                        if (!reports.ContainsKey(remoteId))
                            _communicator.SendPacket(tunnel, _localId, remoteId, TunnelPacketType.ProbeRequest, Array.Empty<byte>());
                    }

                    nextSendMs += resendIntervalMs;
                }

                if (localProbeTask.IsCompleted && pending.All(t => reports.ContainsKey(t.remoteId)))
                    break;

                await Task.Delay(50).ConfigureAwait(false);
            }

            result.LocalUnresponsive = await localProbeTask.ConfigureAwait(false);

            foreach (var (remoteId, _) in pending)
            {
                if (!reports.TryGetValue(remoteId, out var failedIds))
                {
                    result.MissingReports.Add(remoteId);
                    continue;
                }

                foreach (uint failedId in failedIds)
                    result.RemoteFailures.Add((remoteId, failedId));
            }
        }
        finally
        {
            _probeReports = null;
        }

        return result;
    }

    // Receive thread. The probe takes seconds, so run it off-thread and cache the
    // result: the requester resends until a report arrives, and each resend should be
    // answered from cache instead of starting another probe.
    private void Communicator_ProbeRequestReceived(uint senderId, CnCNetTunnel tunnel)
    {
        var cached = _lastProbeReply;
        if (cached != null &&
            (Stopwatch.GetTimestamp() - cached.CompletedTicks) / (double)Stopwatch.Frequency < PROBE_REPLY_CACHE_SECONDS)
        {
            _communicator.SendPacket(tunnel, _localId, senderId, TunnelPacketType.ProbeReport, cached.Payload);
            return;
        }

        if (Interlocked.CompareExchange(ref _probeRunning, 1, 0) != 0)
            return; // probe already running; the requester's resend picks up the cached result

        int targetGeneration = Volatile.Read(ref _targetGeneration);

        Task.Run(async () =>
        {
            try
            {
                List<uint> unresponsive;
                try
                {
                    unresponsive = await ProbeTargetsAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    unresponsive = new List<uint>();
                }

                var payload = new byte[unresponsive.Count * 4];
                for (int i = 0; i < unresponsive.Count; i++)
                    BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(i * 4), unresponsive[i]);

                if (targetGeneration != Volatile.Read(ref _targetGeneration))
                    return;

                _lastProbeReply = new ProbeReply(payload, Stopwatch.GetTimestamp());
                _communicator.SendPacket(tunnel, _localId, senderId, TunnelPacketType.ProbeReport, payload);
            }
            finally
            {
                Interlocked.Exchange(ref _probeRunning, 0);
            }
        });
    }

    // Receive thread. First report per peer wins; resent duplicates are ignored.
    private void Communicator_ProbeReportReceived(uint senderId, List<uint> unresponsiveIds)
        => _probeReports?.TryAdd(senderId, unresponsiveIds);
}

/// <summary>
/// Result of the distributed pre-launch connectivity check
/// (<see cref="V3KeepAliveMonitor.ProbeAllPairsAsync"/>).
/// </summary>
public sealed class LaunchProbeResult
{
    /// <summary>Peers that did not answer the local probe.</summary>
    public List<uint> LocalUnresponsive = new();

    /// <summary>Peers that never sent a probe report back.</summary>
    public List<uint> MissingReports = new();

    /// <summary>Peer-reported failures: reporter → the peer it could not reach.</summary>
    public List<(uint ReporterId, uint FailedId)> RemoteFailures = new();
}
