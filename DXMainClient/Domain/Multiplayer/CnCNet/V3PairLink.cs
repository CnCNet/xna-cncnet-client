#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>What a link tells its peer as it is torn down.</summary>
public enum LinkCloseNotice
{
    /// <summary>Nothing; the peer finds out from the silence.</summary>
    None,

    /// <summary>The pair is going away (a player leaving); the peer must not fail over to chase it.</summary>
    Goodbye,

    /// <summary>A new connection is about to replace this one; the peer should start over too.</summary>
    Restart
}

/// <summary>
/// A change in a pair's connection status, for the lobby.
/// </summary>
public sealed class LinkStatusChange
{
    public NegotiationStatus Status { get; init; }

    /// <summary>The pair's round trip (ms), or -1 if unknown.</summary>
    public int PingMs { get; init; } = -1;

    public string? Reason { get; init; }

    /// <summary>The pair was connected before, lost the connection and has it back.</summary>
    public bool Restored { get; init; }

    /// <summary>Automatic retries have failed often enough that a manual renegotiation is worth suggesting.</summary>
    public bool SuggestRenegotiate { get; init; }
}

/// <summary>
/// The connection between the local player and one other player: the paths (relay tunnels or a
/// direct route) that can carry their traffic, which one does, and everything needed to keep it
/// working.
/// </summary>
/// <remarks>
/// <para><b>Connecting.</b> The two sides swap tunnel ping tables (<see cref="TunnelTableExchange"/>)
/// and the decider ranks them into the pair's list. Both bring up the head of the list in batches,
/// sending heartbeats on each path, and the decider picks the best path confirmed in both
/// directions and announces it. Nothing more is registered than those batches, and once a path is
/// chosen the others are left to expire. A pair that cannot connect retries on its own.</para>
///
/// <para><b>Staying connected.</b> Heartbeats run on the chosen path all the time, from this
/// class's own scheduler thread rather than the UI. Each heartbeat says how long ago its sender
/// last heard the other side, so whichever side stops hearing, both see the path going quiet. The
/// remainder of the ranked list, kept current with fresh ping tables swapped over the working
/// path, is the pair's cold fallback list: nothing is registered on it until it is needed.</para>
///
/// <para><b>Failover.</b> When the path goes quiet, both sides bring up the next fallbacks. The
/// non-decider uses the heads of both of the latest list versions it holds, so it overlaps the
/// decider even if the last update was lost in flight. Game traffic is copied onto the fallbacks
/// at once, so the game stalls for about the suspect time plus one trip through a fallback, not
/// the commit time. If the path stays quiet until the commit time, the decider promotes the best
/// fallback; the non-decider follows the decider's announcement, which every heartbeat on every
/// path carries.</para>
///
/// <para><b>Better routes.</b> The decider trials a fallback that predicts clearly better, or any
/// fallback when the current path is losing packets, by copying traffic onto it for a few seconds,
/// and moves only if the measurement agrees. A direct P2P path that beats the relay is adopted the
/// same way. Cooldowns and a flap limit keep a pair from bouncing.</para>
///
/// <para><b>Threading.</b> <see cref="Tick"/> runs on the scheduler thread and
/// <see cref="OnPacket"/> on the communicator's receive thread; both take the lock. The game bridge
/// only reads <see cref="SendPaths"/> and <see cref="FindAcceptingPath"/>, which are lock-free
/// snapshots. Events are raised outside the lock, on either thread.</para>
/// </remarks>
public sealed class V3PairLink
{
    private enum LinkState
    {
        Idle,
        Exchanging,
        BringingUp,
        Connected,
        WaitingRetry,
        Disposed
    }

    private static int HEARTBEAT_LOBBY_MS => ClientConfiguration.Instance.V3HeartbeatIntervalLobbyMs;
    private static int HEARTBEAT_GAME_MS => ClientConfiguration.Instance.V3HeartbeatIntervalGameMs;
    private static int SUSPECT_LOBBY_MS => ClientConfiguration.Instance.V3SuspectAfterLobbyMs;
    private static int COMMIT_LOBBY_MS => ClientConfiguration.Instance.V3CommitAfterLobbyMs;
    private static int SUSPECT_GAME_MS => ClientConfiguration.Instance.V3SuspectAfterGameMs;
    private static int COMMIT_GAME_MS => ClientConfiguration.Instance.V3CommitAfterGameMs;
    private static int BRING_UP_BATCH_SIZE => ClientConfiguration.Instance.V3BringUpBatchSize;
    private static int BRING_UP_BATCH_TIMEOUT_MS => ClientConfiguration.Instance.V3BringUpBatchTimeoutMs;
    private static int BRING_UP_SETTLE_MS => ClientConfiguration.Instance.V3BringUpSettleMs;
    private static int FAILOVER_BATCH_SIZE => ClientConfiguration.Instance.V3FailoverBatchSize;
    private static int FAILOVER_BATCH_TIMEOUT_MS => ClientConfiguration.Instance.V3FailoverBatchTimeoutMs;
    private static int FAILOVER_SETTLE_MS => ClientConfiguration.Instance.V3FailoverSettleMs;
    private static int RANKED_LIST_SIZE => ClientConfiguration.Instance.V3RankedListSize;
    private static int RETRIES_BEFORE_HINT => ClientConfiguration.Instance.V3RetriesBeforeHint;
    private static int DEGRADE_MARGIN_MS => ClientConfiguration.Instance.V3DegradeMarginMs;
    private static double DEGRADE_MARGIN_FRACTION => ClientConfiguration.Instance.V3DegradeMarginFraction;
    private static int DEGRADE_SUSTAIN_SECONDS => ClientConfiguration.Instance.V3DegradeSustainSeconds;
    private static double DEGRADE_LOSS_PERCENT => ClientConfiguration.Instance.V3DegradeLossPercent;
    private static double SEVERE_LOSS_PERCENT => Math.Max(8, DEGRADE_LOSS_PERCENT * 4);
    private static int TRIAL_DURATION_MS => ClientConfiguration.Instance.V3TrialDurationMs;
    private static int SWITCH_COOLDOWN_SECONDS => ClientConfiguration.Instance.V3SwitchCooldownSeconds;
    private static int DEGRADE_FREEZE_AFTER => ClientConfiguration.Instance.V3DegradeFreezeAfterSwitches;
    private static int DEGRADE_FREEZE_SECONDS => ClientConfiguration.Instance.V3DegradeFreezeSeconds;
    private static int PING_TABLE_INTERVAL_SECONDS => ClientConfiguration.Instance.V3PingTableIntervalSeconds;
    private static int LOAD_PROBE_INTERVAL_SECONDS => ClientConfiguration.Instance.V3LoadProbeIntervalSeconds;
    private static int PEER_LOST_SECONDS => ClientConfiguration.Instance.V3PeerLostSeconds;
    private static int PACKET_LOSS_WEIGHT => ClientConfiguration.Instance.V3PacketLossWeight;
    private static double CAPACITY_THRESHOLD => ClientConfiguration.Instance.V3MatchmakingCapacityThreshold;
    private static int HANDSHAKE_FAILURE_THRESHOLD => ClientConfiguration.Instance.V3MatchmakingHandshakeFailureThreshold;

    /// <summary>Cap on paths brought up while connecting, so a pair with no working tunnel does not register on the whole list.</summary>
    private const int MAX_BRING_UP_PATHS = 12;

    /// <summary>Cap on fallbacks brought up in one failover.</summary>
    private const int MAX_FAILOVER_PATHS = 6;

    /// <summary>How long loss trials pause after two in a row found the loss on the trial path too.</summary>
    private const long LOSS_TRIAL_HOLD_MS = 120_000;

    /// <summary>Times the primary may go quiet within <see cref="FLAPPING_WINDOW_MS"/> before an answering fallback replaces it.</summary>
    private const int FLAPPING_QUIETS = 3;

    private const long FLAPPING_WINDOW_MS = 30_000;

    /// <summary>How long (ms) traffic is still accepted on a path after the pair stops using it, covering packets in flight and a peer that has not switched yet.</summary>
    private const int RETIRE_ACCEPT_MS = 10_000;

    /// <summary>How long (ms) an unconfirmed direct path keeps being punched.</summary>
    private const int DIRECT_PUNCH_MS = 20_000;

    /// <summary>How long (ms) our P2P candidates are advertised after they are gathered.</summary>
    private const int P2P_ADVERTISE_MS = 20_000;

    /// <summary>How long (ms) a fallback that failed a trial is not trialled again.</summary>
    private const int TRIAL_FAILED_BACKOFF_MS = 300_000;

    /// <summary>Most ping table entries carried in one heartbeat, keeping it under 1024 bytes with every other section.</summary>
    private const int MAX_HEARTBEAT_TABLE_ENTRIES = 100;

    /// <summary>Lobby load probe: duration (ms), interval between filler packets (ms) and filler size (bytes).</summary>
    private const int LOAD_PROBE_DURATION_MS = 3000;
    private const int LOAD_PROBE_INTERVAL_MS = 33;
    private const int FILLER_SIZE = 160;

    private readonly object _lock = new();
    private readonly TunnelHandler _tunnelHandler;
    private readonly Action<string> _sendIrc;
    private readonly bool _p2pEnabled;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Action> _pendingEvents = [];
    private readonly Random _random = new();

    private LinkState _state = LinkState.Idle;
    private TunnelTableExchange? _exchange;
    private int _attempt;
    private uint _session;
    private List<CnCNetTunnel> _ranked = [];
    private int _bringUpCursor;
    private long _bringUpStartMs;
    private long _batchStartMs;
    private long _firstConfirmMs;

    private readonly Dictionary<CnCNetTunnel, V3LinkPath> _paths = [];
    private V3LinkPath? _primary;
    private V3LinkPath? _previousPrimary;
    private long _switchMs;
    private ushort _epoch;
    private bool _everConnected;

    private V3LinkPath? _trial;
    private bool _trialForLoss;
    private int _failedLossTrials;
    private long _lossTrialsHeldUntilMs;
    private readonly Queue<long> _quietStartsMs = new();
    private long _trialStartMs;
    private long _trialConfirmedMs;

    private bool _failover;
    private long _failoverStartMs;
    private long _failoverBatchStartMs;
    private long _failoverFirstConfirmMs;
    private readonly HashSet<CnCNetTunnel> _failoverCandidates = [];
    private readonly Dictionary<CnCNetTunnel, long> _deadUntilMs = [];
    private bool? _localOk;
    private bool _localCheckRunning;
    private long _localCheckMs;
    private bool _peerLostReported;

    // Decider: the list being published and the last one the peer acknowledged.
    private ushort _listVersion;
    private List<CnCNetTunnel> _list = [];
    private ushort _committedVersion;
    private List<CnCNetTunnel> _committedList = [];
    private long _lastListRecomputeMs;

    // Non-decider: the two newest list versions received.
    private readonly List<(ushort Version, List<CnCNetTunnel> List)> _receivedLists = [];

    private Dictionary<uint, TunnelTableEntry> _peerTable = [];
    private long _peerTableAtMs;
    private bool _peerTableChanged;
    private long _lastTableSentMs;
    private bool _sendTableNext;
    private long _lastTableRequestMs;

    private bool _peerGoodbye;
    private bool _peerInGame;
    private long _bothInGameSinceMs;
    private bool _inGame;
    private bool _peerP2P;
    private bool _peerHasOurCandidates;
    private bool _havePeerCandidates;

    private List<IPEndPoint>? _localCandidates;
    private bool _gathering;
    private long _p2pAdvertiseUntilMs;
    private long _lastP2PAdvertMs;

    private long _lastSwitchMs;
    private readonly Queue<long> _degradeSwitches = new();
    private long _degradeFrozenUntilMs;
    private int _degradeEvidence;
    private long _lastDegradeEvalMs;
    private readonly Dictionary<CnCNetTunnel, long> _trialFailedAtMs = [];

    private int _failures;
    private long _retryAtMs;

    private long _nextLoadProbeMs;
    private long _loadProbeUntilMs;
    private long _nextFillerMs;

    private NegotiationStatus _status = NegotiationStatus.NotStarted;
    private long _lastPingEventMs;
    private int _lastPingEventValue = -1;

    private volatile V3LinkPath[] _sendPaths = [];
    private volatile V3LinkPath[] _acceptPaths = [];
    private volatile CnCNetTunnel? _primaryTunnel;
    private volatile int _pingMs = -1;
    private volatile int _lossTenths = -1;

    public V3PairLink(V3PlayerInfo local, V3PlayerInfo remote, TunnelHandler tunnelHandler,
        bool isDecider, bool p2pEnabled, Action<string> sendIrc)
    {
        Local = local;
        Remote = remote;
        _tunnelHandler = tunnelHandler;
        IsDecider = isDecider;
        _p2pEnabled = p2pEnabled;
        _sendIrc = sendIrc;
        PairSeed = local.Id ^ remote.Id;
    }

    public V3PlayerInfo Local { get; }

    public V3PlayerInfo Remote { get; }

    /// <summary>The side that chooses the pair's path. See <see cref="V3TunnelNegotiationManager"/> for how it is picked.</summary>
    public bool IsDecider { get; }

    private uint PairSeed { get; }

    public event Action<V3PairLink, LinkStatusChange>? StatusChanged;

    /// <summary>The pair moved to another path: old tunnel (null when first connecting), new tunnel, reason.</summary>
    public event Action<V3PairLink, CnCNetTunnel?, CnCNetTunnel, string>? PathChanged;

    /// <summary>The measured round trip (ms) moved; raised at most once a second.</summary>
    public event Action<V3PairLink, int>? PingUpdated;

    public NegotiationStatus Status
    {
        get
        {
            lock (_lock)
                return _status;
        }
    }

    /// <summary>The tunnel the pair currently sends on, or null.</summary>
    public CnCNetTunnel? PrimaryTunnel => _primaryTunnel;

    /// <summary>The pair's measured round trip (ms), or -1.</summary>
    public int PingMs => _pingMs;

    /// <summary>The pair's measured packet loss (percent) over the last 10 seconds, or null.</summary>
    public double? PacketLossPercent => _lossTenths < 0 ? null : _lossTenths / 10.0;

    /// <summary>
    /// The paths game data to this peer goes out on: the primary, plus any path it is being
    /// copied onto during a failover or trial. Lock-free snapshot for the game bridge.
    /// </summary>
    public V3LinkPath[] SendPaths => _sendPaths;

    /// <summary>The path game data from this peer arrived on, if it is one the pair accepts. Lock-free.</summary>
    public V3LinkPath? FindAcceptingPath(CnCNetTunnel tunnel)
    {
        foreach (var path in _acceptPaths)
        {
            if (path.Tunnel == tunnel)
                return path;
        }

        return null;
    }

    /// <summary>Whether the pair has a path that has carried traffic both ways within <paramref name="maxSilenceMs"/>.</summary>
    public bool IsLive(long maxSilenceMs)
    {
        lock (_lock)
            return _state == LinkState.Connected && _primary != null && _primary.GetSilenceMs(V3Clock.NowMs) < maxSilenceMs;
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_state != LinkState.Idle)
                return;

            SetStatus(NegotiationStatus.InProgress, null);
            StartExchange(V3Clock.NowMs);
        }

        DispatchEvents();
    }

    /// <summary>
    /// Tears the pair down, first telling the peer why if <paramref name="notice"/> says so, so it
    /// neither treats our silence as a failure to recover from nor waits it out before starting
    /// over.
    /// </summary>
    public void Dispose(LinkCloseNotice notice)
    {
        lock (_lock)
        {
            if (_state == LinkState.Disposed)
                return;

            if (notice != LinkCloseNotice.None && _primary != null)
            {
                long now = V3Clock.NowMs;
                byte flag = notice == LinkCloseNotice.Goodbye ? V3Heartbeat.FLAG_GOODBYE : V3Heartbeat.FLAG_RESTART;
                for (int i = 0; i < 2; i++)
                    SendHeartbeat(_primary, now, inGame: false, extraFlags: flag);
            }

            _state = LinkState.Disposed;
            _exchange?.Dispose();
            _exchange = null;
            _sendPaths = [];
            _acceptPaths = [];
            _primaryTunnel = null;
        }

        _cts.Cancel();
        _tunnelHandler.UnregisterV3PacketHandler(Local.Id, Remote.Id, OnPacket);
    }

    public bool IsDisposed
    {
        get
        {
            lock (_lock)
                return _state == LinkState.Disposed;
        }
    }

    /// <summary>Sends a pre-launch probe request to the peer over the current path.</summary>
    public void SendProbeRequest()
    {
        CnCNetTunnel? tunnel = _primaryTunnel;
        if (tunnel != null)
            _tunnelHandler.SendPacket(tunnel, Local.Id, Remote.Id, TunnelPacketType.ProbeRequest, []);
    }

    #region Exchange and bring-up

    private List<CnCNetTunnel> GetPool() => _tunnelHandler.GetV3RelayPool();

    private void StartExchange(long now)
    {
        RetireAll(now);
        _primary = null;
        _primaryTunnel = null;
        _ranked = [];
        _state = LinkState.Exchanging;
        _attempt++;

        _exchange?.Dispose();
        var exchange = new TunnelTableExchange(Local, Remote, GetPool(), _tunnelHandler, _sendIrc, IsDecider, PairSeed);
        _exchange = exchange;
        int attempt = _attempt;

        _tunnelHandler.RegisterV3PacketHandler(Local.Id, Remote.Id, OnPacket);
        _ = RunExchangeAsync(exchange, attempt);
    }

    private async Task RunExchangeAsync(TunnelTableExchange exchange, int attempt)
    {
        List<CnCNetTunnel>? ranked = null;
        try
        {
            ranked = await exchange.RunAsync(_cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Logger.Log($"V3PairLink: Table exchange with {Remote.Name} failed: {ex.Message}");
        }

        lock (_lock)
        {
            if (_state != LinkState.Exchanging || attempt != _attempt)
                return;

            long now = V3Clock.NowMs;
            if (ranked == null || ranked.Count == 0)
            {
                Fail("no tunnel list could be agreed", now);
            }
            else
            {
                _session = exchange.SessionNonce;
                _ranked = ranked;
                _state = LinkState.BringingUp;
                _bringUpCursor = 0;
                _bringUpStartMs = now;
                AddBringUpBatch(now);
            }
        }

        DispatchEvents();
    }

    private bool AddBringUpBatch(long now)
    {
        var batch = _ranked.Skip(_bringUpCursor).Take(BRING_UP_BATCH_SIZE).ToList();
        if (batch.Count == 0)
            return false;

        _bringUpCursor += batch.Count;
        _batchStartMs = now;
        _firstConfirmMs = 0;

        foreach (var tunnel in batch)
            ActivatePath(tunnel, now);

        _tunnelHandler.SendRegistrationToTunnels(Local.Id, batch);
        Logger.Log($"V3PairLink: Bringing up {string.Join(", ", batch.Select(t => t.Name))} with {Remote.Name}");
        return true;
    }

    private void TickBringUp(long now)
    {
        var active = _paths.Values.Where(p => p.Active).ToList();
        var confirmed = active.Where(p => p.IsConfirmed(now, SUSPECT_LOBBY_MS)).ToList();

        if (confirmed.Count > 0 && _firstConfirmMs == 0)
            _firstConfirmMs = now;

        if (IsDecider && _firstConfirmMs != 0)
        {
            bool allMeasured = active.All(p => p.IsConfirmed(now, SUSPECT_LOBBY_MS) && p.RttSamples >= 3);
            if (now - _firstConfirmMs >= BRING_UP_SETTLE_MS || allMeasured)
            {
                var best = PickBest(confirmed, now);
                RecordHandshakeOutcomes(active, now);
                Promote(best, "connected", now);
                return;
            }
        }

        if (confirmed.Count == 0 && now - _batchStartMs >= BRING_UP_BATCH_TIMEOUT_MS)
        {
            if (active.Count >= MAX_BRING_UP_PATHS || !AddBringUpBatch(now))
            {
                Fail($"none of {_bringUpCursor} tunnel(s) carried traffic both ways", now);
                return;
            }
        }

        // The non-decider waits for the announcement, but not forever: the decider may be gone.
        long budget = (long)BRING_UP_BATCH_TIMEOUT_MS * ((Math.Min(_ranked.Count, MAX_BRING_UP_PATHS) / Math.Max(1, BRING_UP_BATCH_SIZE)) + 1) + 10_000;
        if (!IsDecider && now - _bringUpStartMs > budget)
            Fail("no path was announced by the other player", now);
    }

    /// <summary>
    /// Records which bring-up tunnels carried traffic, so a tunnel that answers pings but never
    /// relays (full, or in maintenance) stops being ranked ahead of ones that do. Nothing is
    /// recorded unless one worked, since otherwise the peer is the likely fault.
    /// </summary>
    private void RecordHandshakeOutcomes(List<V3LinkPath> paths, long now)
    {
        var relays = paths.Where(p => !p.IsDirect).ToList();
        if (!relays.Any(p => p.LastHeardMs != 0))
            return;

        foreach (var path in relays)
        {
            if (path.LastHeardMs != 0)
            {
                path.Tunnel.RecordHandshakeSuccess();
            }
            else
            {
                path.Tunnel.RecordHandshakeFailure();
                Logger.Log($"V3PairLink: {path.Tunnel.Name} carried nothing from {Remote.Name} ({path.Tunnel.ConsecutiveHandshakeFailures} in a row)");
            }
        }
    }

    private void Fail(string reason, long now)
    {
        _failures++;
        int[] delays = ParseRetryDelays();
        int delay = delays[Math.Min(_failures - 1, delays.Length - 1)];
        _retryAtMs = now + (delay * 1000L);

        RetireAll(now);
        _primary = null;
        _primaryTunnel = null;
        _state = LinkState.WaitingRetry;
        _failover = false;
        _trial = null;
        _exchange?.Dispose();
        _exchange = null;

        Logger.Log($"V3PairLink: Could not connect to {Remote.Name} ({reason}); retrying in {delay}s (attempt {_failures})");

        bool hint = _failures == RETRIES_BEFORE_HINT;
        if (_status != NegotiationStatus.Failed || hint)
            SetStatus(NegotiationStatus.Failed, reason, suggestRenegotiate: hint);
    }

    private static int[] ParseRetryDelays()
    {
        var delays = ClientConfiguration.Instance.V3RetryDelaysSeconds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? Math.Max(1, v) : 0)
            .Where(v => v > 0)
            .ToArray();

        return delays.Length > 0 ? delays : [5, 15, 60];
    }

    #endregion

    #region Tick

    /// <summary>Advances the pair's timers and sends whatever is due. Scheduler thread.</summary>
    public void Tick(long now, bool inGame)
    {
        lock (_lock)
        {
            _inGame = inGame;

            switch (_state)
            {
                case LinkState.Disposed:
                case LinkState.Idle:
                    return;

                case LinkState.WaitingRetry:
                    if (now >= _retryAtMs && !inGame)
                        StartExchange(now);
                    break;

                case LinkState.BringingUp:
                    TickBringUp(now);
                    break;

                case LinkState.Connected:
                    TickConnected(now, inGame);
                    break;
            }

            if (_state is LinkState.BringingUp or LinkState.Connected)
            {
                SendDueHeartbeats(now, inGame);
                SendDueFiller(now);
            }

            ExpireRetiredPaths(now);
            UpdateSnapshots(now);
        }

        DispatchEvents();
    }

    private void TickConnected(long now, bool inGame)
    {
        var primary = _primary!;

        // The tighter in-game thresholds assume both sides heartbeat at the in-game rate. Players
        // start the game at different moments, so until the peer says it is in game too, and for
        // a moment after, its last heartbeats are still lobby-paced and would read as silence.
        if (!(inGame && _peerInGame))
            _bothInGameSinceMs = 0;
        else if (_bothInGameSinceMs == 0)
            _bothInGameSinceMs = now;

        bool bothInGame = _bothInGameSinceMs != 0 && now - _bothInGameSinceMs >= 3000;
        int suspect = bothInGame ? SUSPECT_GAME_MS : SUSPECT_LOBBY_MS;
        int commit = bothInGame ? COMMIT_GAME_MS : COMMIT_LOBBY_MS;
        long silence = primary.GetSilenceMs(now);

        if (!_failover)
        {
            if (silence >= suspect && !_peerGoodbye)
                BeginFailover(now);
        }
        else if (primary.IsConfirmed(now, suspect))
        {
            if (!TryLeaveFlappingPrimary(now, suspect))
                EndFailover($"{primary.Tunnel.Name} recovered after {(now - _failoverStartMs) / 1000.0:F1}s", now);
        }
        else
        {
            TickFailover(now, silence, suspect, commit, inGame);
            if (_state != LinkState.Connected)
                return;
        }

        if (!_failover)
        {
            if (IsDecider)
            {
                TickTrial(now, suspect);
                TickDegradation(now);
            }

            TickP2P(now, suspect);
            TickLoadProbe(now, inGame);
        }

        TickPingTable(now);

        if (IsDecider)
            TickFallbackList(now);

        UpdateDisplayStats(now);
    }

    #endregion

    #region Failover

    private void BeginFailover(long now)
    {
        _failover = true;
        _failoverStartMs = now;
        _failoverFirstConfirmMs = 0;
        _failoverCandidates.Clear();
        _localOk = null;
        StartLocalCheck(now);

        _quietStartsMs.Enqueue(now);
        Logger.Log($"V3PairLink: {_primary!.Tunnel.Name} to {Remote.Name} has gone quiet; bringing up fallbacks");
        AddFailoverBatch(now);
    }

    private bool AddFailoverBatch(long now)
    {
        var batch = GetFailoverCandidates();
        if (batch.Count == 0)
            return false;

        foreach (var tunnel in batch)
        {
            ActivatePath(tunnel, now);
            _failoverCandidates.Add(tunnel);
        }

        _failoverBatchStartMs = now;
        _tunnelHandler.SendRegistrationToTunnels(Local.Id, batch.Where(t => !t.IsDirect).ToList());
        Logger.Log($"V3PairLink: Fallbacks for {Remote.Name}: {string.Join(", ", batch.Select(t => t.Name))}");
        return true;
    }

    /// <summary>
    /// The next fallbacks to bring up. The decider takes them from the list the peer has
    /// acknowledged; the non-decider from both of its two newest lists, so its batch overlaps the
    /// decider's whichever of the two the decider is using.
    /// </summary>
    private List<CnCNetTunnel> GetFailoverCandidates()
    {
        var lists = IsDecider
            ? [_committedList]
            : _receivedLists.OrderByDescending(l => l.Version).Select(l => l.List).ToList();

        if (lists.Count == 0 || lists.All(l => l.Count == 0))
            lists = [_ranked];

        long now = V3Clock.NowMs;
        bool Eligible(CnCNetTunnel t) =>
            t != _primary?.Tunnel &&
            !_failoverCandidates.Contains(t) &&
            !(_deadUntilMs.TryGetValue(t, out long until) && until > now);

        var batch = new List<CnCNetTunnel>();
        foreach (var list in lists)
        {
            foreach (var tunnel in list.Where(Eligible).Take(FAILOVER_BATCH_SIZE))
            {
                if (!batch.Contains(tunnel))
                    batch.Add(tunnel);
            }
        }

        return batch;
    }

    private void TickFailover(long now, long silence, int suspect, int commit, bool inGame)
    {
        var confirmed = _paths.Values
            .Where(p => p.Active && p != _primary && (_failoverCandidates.Contains(p.Tunnel) || p.IsDirect) && p.IsConfirmed(now, suspect))
            .ToList();

        if (confirmed.Count > 0 && _failoverFirstConfirmMs == 0)
        {
            _failoverFirstConfirmMs = now;
            Logger.Log($"V3PairLink: {confirmed[0].Tunnel.Name} answers for {Remote.Name}; copying traffic onto it");
        }

        if (IsDecider && silence >= commit && confirmed.Count > 0 && now - _failoverFirstConfirmMs >= FAILOVER_SETTLE_MS)
        {
            var dead = _primary!;
            _deadUntilMs[dead.Tunnel] = now + 60_000;
            Promote(PickBest(confirmed, now), $"{dead.Tunnel.Name} stopped responding", now);
            return;
        }

        // Our own connection is down: every tunnel would look dead, so trying more of them only
        // burns the list. Keep heartbeating what is already up and wait. A fallback that answers
        // proves otherwise, which is why this is checked only when nothing has.
        if (confirmed.Count == 0 && _localOk == false)
        {
            if (now - _localCheckMs > 2000)
                StartLocalCheck(now);

            return;
        }

        if (confirmed.Count == 0 && now - _failoverBatchStartMs >= FAILOVER_BATCH_TIMEOUT_MS)
        {
            if (_failoverCandidates.Count >= MAX_FAILOVER_PATHS || !AddFailoverBatch(now))
            {
                // Everything on the list has been tried. Start over after a pause, in case the
                // peer comes back.
                if (now - _failoverBatchStartMs >= 10_000)
                {
                    foreach (var tunnel in _failoverCandidates)
                        DeactivatePath(tunnel, now);

                    _failoverCandidates.Clear();
                    AddFailoverBatch(now);
                }
            }
        }

        if (silence >= PEER_LOST_SECONDS * 1000L && confirmed.Count == 0)
        {
            if (!_peerLostReported)
            {
                _peerLostReported = true;
                Logger.Log($"V3PairLink: Lost {Remote.Name}: nothing heard for {silence / 1000}s on any path");
                SetStatus(NegotiationStatus.Failed, "not responding on any tunnel");
            }

            // In the lobby, start over from scratch after a while: both sides' lists may be stale.
            if (!inGame && silence >= 60_000)
                Fail("lost the connection", now);
        }
    }

    /// <summary>
    /// A primary that keeps going quiet and coming back is too lossy to carry the game, yet never
    /// stays quiet long enough to fail over, and degradation does not run during a failover. After
    /// a few rounds of that the decider moves to a fallback that is answering instead of cancelling.
    /// </summary>
    private bool TryLeaveFlappingPrimary(long now, int suspect)
    {
        while (_quietStartsMs.Count > 0 && now - _quietStartsMs.Peek() > FLAPPING_WINDOW_MS)
            _quietStartsMs.Dequeue();

        if (!IsDecider || _quietStartsMs.Count < FLAPPING_QUIETS || now < _degradeFrozenUntilMs)
            return false;

        var answering = _paths.Values
            .Where(p => p.Active && p != _primary && (_failoverCandidates.Contains(p.Tunnel) || p.IsDirect) && p.IsConfirmed(now, suspect))
            .ToList();

        if (answering.Count == 0)
            return false;

        string from = _primary!.Tunnel.Name;
        _quietStartsMs.Clear();
        RecordDegradeSwitch(now);
        Promote(PickBest(answering, now), $"{from} keeps going quiet", now);
        return true;
    }

    private void EndFailover(string reason, long now)
    {
        Logger.Log($"V3PairLink: Failover for {Remote.Name} cancelled: {reason}");
        foreach (var tunnel in _failoverCandidates)
        {
            if (!tunnel.IsDirect)
                DeactivatePath(tunnel, now);
        }

        _failoverCandidates.Clear();
        _failover = false;

        if (_peerLostReported)
        {
            _peerLostReported = false;
            SetStatus(NegotiationStatus.Succeeded, null, restored: true);
        }
    }

    private void StartLocalCheck(long now)
    {
        if (_localCheckRunning)
            return;

        // Tunnels on hosts other than the one that went quiet, so its outage cannot pass for ours.
        // At least two hosts are needed for a verdict: one silent host proves nothing about us.
        string? failedHost = _primary?.IsDirect == false ? _primary.Tunnel.Address : null;
        var probes = GetPool()
            .Concat(_tunnelHandler.MatchmakingTunnels)
            .Where(t => t.Ping.IsValid() && t.Address != failedHost && !_paths.ContainsKey(t))
            .GroupBy(t => t.Address)
            .Select(g => g.OrderBy(t => t.Ping.Milliseconds).First())
            .OrderBy(t => t.Ping.Milliseconds)
            .Take(3)
            .ToList();

        if (probes.Count < 2)
        {
            _localOk = true;
            return;
        }

        _localCheckRunning = true;
        _localCheckMs = now;

        _ = Task.Run(async () =>
        {
            bool? reachable = null;
            try
            {
                reachable = await _tunnelHandler.Pinger.IsAnyReachableAsync(probes, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }

            lock (_lock)
            {
                _localCheckRunning = false;
                _localCheckMs = V3Clock.NowMs;

                // A ping that was not sent says nothing about our connection.
                _localOk = reachable ?? true;
                if (_localOk == false)
                    Logger.Log($"V3PairLink: No tunnel answers us either; waiting for our own connection rather than failing over from {Remote.Name}");
            }
        });
    }

    #endregion

    #region Trials and degradation (decider)

    private double Margin(double score) => Math.Max(DEGRADE_MARGIN_MS, score * DEGRADE_MARGIN_FRACTION);

    private void TickDegradation(long now)
    {
        if (_trial != null || now - _lastSwitchMs < SWITCH_COOLDOWN_SECONDS * 1000L ||
            now < _degradeFrozenUntilMs || now - _lastDegradeEvalMs < 5000)
        {
            return;
        }

        _lastDegradeEvalMs = now;

        var primary = _primary!;
        if (primary.RttMs == null)
            return;

        var candidate = _committedList.FirstOrDefault(t =>
            t != primary.Tunnel &&
            !(_trialFailedAtMs.TryGetValue(t, out long failed) && now - failed < TRIAL_FAILED_BACKOFF_MS) &&
            !(_deadUntilMs.TryGetValue(t, out long dead) && dead > now) &&
            TunnelTable.PredictPairRtt(t, _peerTable).HasValue);

        if (candidate == null)
        {
            _degradeEvidence = 0;
            return;
        }

        int predictedCandidate = TunnelTable.PredictPairRtt(candidate, _peerTable)!.Value;
        double measured = primary.RttMs.Value;
        double loss = primary.GetLossPercent(now) ?? 0;
        string? reason = null;

        bool severe = false;
        if (loss >= DEGRADE_LOSS_PERCENT)
        {
            // A trial the candidate cannot win only costs a registration, e.g. a direct path on Wi-Fi
            // against a distant relay.
            if (predictedCandidate >= primary.GetScore(now, PACKET_LOSS_WEIGHT) || now < _lossTrialsHeldUntilMs)
            {
                _degradeEvidence = 0;
                return;
            }

            reason = $"{loss:F1}% loss";

            // Heavy loss is hurting the game now; the trial's measurement is safeguard enough.
            severe = loss >= SEVERE_LOSS_PERCENT;
        }
        else if (!primary.IsDirect)
        {
            // Compared like with like: both on the ping sums, then the current path's measured
            // round trip against its own prediction, which catches a congested tunnel.
            int? predictedPrimary = TunnelTable.PredictPairRtt(primary.Tunnel, _peerTable);
            if (predictedPrimary.HasValue && predictedCandidate + Margin(predictedPrimary.Value) < predictedPrimary.Value)
                reason = $"{candidate.Name} predicts {predictedCandidate}ms against {predictedPrimary}ms";
            else if (predictedPrimary.HasValue && measured > predictedPrimary.Value + (2 * Margin(predictedPrimary.Value)) &&
                     predictedCandidate + Margin(measured) < measured)
                reason = $"{primary.Tunnel.Name} measures {measured:F0}ms against a predicted {predictedPrimary}ms";
        }
        else if (predictedCandidate + Margin(measured) < measured)
        {
            // A direct path has no ping sum to predict it, so its measured round trip has to do.
            reason = $"{primary.Tunnel.Name} measures {measured:F0}ms against {candidate.Name}'s predicted {predictedCandidate}ms";
        }

        _degradeEvidence = reason != null ? _degradeEvidence + 1 : 0;

        if (reason != null && (severe || _degradeEvidence * 5 >= DEGRADE_SUSTAIN_SECONDS))
        {
            _degradeEvidence = 0;
            Logger.Log($"V3PairLink: Trialling {candidate.Name} for {Remote.Name}: {reason}");
            StartTrial(candidate, now);
            _trialForLoss = loss >= DEGRADE_LOSS_PERCENT;
        }
    }

    private void StartTrial(CnCNetTunnel tunnel, long now)
    {
        _trial = ActivatePath(tunnel, now);
        _trialStartMs = now;
        _trialConfirmedMs = 0;

        if (!tunnel.IsDirect)
            _tunnelHandler.SendRegistrationToTunnels(Local.Id, [tunnel]);
    }

    private void TickTrial(long now, int suspect)
    {
        var trial = _trial;
        if (trial == null)
            return;

        if (!trial.IsConfirmed(now, suspect))
        {
            if (_trialConfirmedMs == 0 && now - _trialStartMs > TRIAL_DURATION_MS)
                AbortTrial("it did not answer", now);

            return;
        }

        if (_trialConfirmedMs == 0)
            _trialConfirmedMs = now;

        if (now - _trialConfirmedMs < TRIAL_DURATION_MS)
            return;

        double primaryScore = _primary!.GetScore(now, PACKET_LOSS_WEIGHT);
        double trialScore = trial.GetScore(now, PACKET_LOSS_WEIGHT);

        if (trialScore + Margin(primaryScore) < primaryScore)
        {
            _failedLossTrials = 0;
            RecordDegradeSwitch(now);
            Promote(trial, $"better route ({trialScore:F0} against {primaryScore:F0})", now);
        }
        else
        {
            AbortTrial($"it was not better ({trialScore:F0} against {primaryScore:F0})", now);

            // Loss that follows us onto other paths is ours or the peer's, not the path's, and
            // trialling every relay in turn would not help. A clean trial path that only missed
            // the margin says nothing of the kind.
            if (_trialForLoss && (trial.GetLossPercent(now) ?? 0) >= DEGRADE_LOSS_PERCENT && ++_failedLossTrials >= 2)
            {
                _failedLossTrials = 0;
                _lossTrialsHeldUntilMs = now + LOSS_TRIAL_HOLD_MS;
                Logger.Log($"V3PairLink: Loss with {Remote.Name} is on every path tried; no loss trials for {LOSS_TRIAL_HOLD_MS / 1000}s");
            }
        }
    }

    private void AbortTrial(string reason, long now)
    {
        var trial = _trial!;
        Logger.Log($"V3PairLink: Staying on {_primary!.Tunnel.Name} with {Remote.Name}: trial of {trial.Tunnel.Name} ended, {reason}");
        _trialFailedAtMs[trial.Tunnel] = now;
        _trial = null;

        if (!trial.IsDirect)
            DeactivatePath(trial.Tunnel, now);
    }

    private void RecordDegradeSwitch(long now)
    {
        _degradeSwitches.Enqueue(now);
        while (_degradeSwitches.Count > 0 && now - _degradeSwitches.Peek() > 300_000)
            _degradeSwitches.Dequeue();

        if (_degradeSwitches.Count >= DEGRADE_FREEZE_AFTER)
        {
            _degradeFrozenUntilMs = now + (DEGRADE_FREEZE_SECONDS * 1000L);
            _degradeSwitches.Clear();
            Logger.Log($"V3PairLink: {Remote.Name}'s path changed {DEGRADE_FREEZE_AFTER} times in five minutes; holding it for {DEGRADE_FREEZE_SECONDS}s");
        }
    }

    #endregion

    #region P2P

    private void TickP2P(long now, int suspect)
    {
        if (!_p2pEnabled || !_peerP2P)
            return;

        if (_localCandidates == null && !_gathering)
            GatherCandidates();

        // Stop punching direct candidates that never answered.
        foreach (var path in _paths.Values.Where(p => p.Active && p.IsDirect && p != _primary).ToList())
        {
            if (path.LastHeardMs == 0 && now - path.CreatedMs > DIRECT_PUNCH_MS)
                DeactivatePath(path.Tunnel, now);
        }

        if (!IsDecider || _trial != null || _primary!.IsDirect || (_lastSwitchMs != 0 && now - _lastSwitchMs < SWITCH_COOLDOWN_SECONDS * 1000L))
            return;

        // A warm path carries few packets, so its loss can go unmeasured; scored as lossless it
        // would win against a clean relay while still dropping half its traffic. Heavy loss is
        // what drives a pair off a direct path, so it has to clear before the pair goes back.
        var direct = _paths.Values
            .Where(p => p.Active && p.IsDirect && p.IsConfirmed(now, suspect) && p.RttSamples >= 4 &&
                p.GetLossPercent(now) < SEVERE_LOSS_PERCENT &&
                !(_deadUntilMs.TryGetValue(p.Tunnel, out long dead) && dead > now))
            .OrderBy(p => p.GetScore(now, PACKET_LOSS_WEIGHT))
            .FirstOrDefault();

        if (direct == null)
            return;

        double primaryScore = _primary.GetScore(now, PACKET_LOSS_WEIGHT);
        double directScore = direct.GetScore(now, PACKET_LOSS_WEIGHT);

        if (directScore + Math.Max(5, primaryScore * 0.1) < primaryScore)
            Promote(direct, $"direct connection ({directScore:F0} against {primaryScore:F0})", now);
    }

    private void GatherCandidates()
    {
        _gathering = true;

        _ = Task.Run(async () =>
        {
            var candidates = new List<IPEndPoint>();
            try
            {
                candidates.AddRange(_tunnelHandler.GetLocalP2PEndpoints());
                var reflexive = await _tunnelHandler.GetOrDiscoverP2PEndpointAsync().ConfigureAwait(false);
                if (reflexive != null)
                    candidates.Add(reflexive);
            }
            catch (Exception ex)
            {
                Logger.Log($"V3PairLink: Gathering P2P candidates failed: {ex.Message}");
            }

            candidates = candidates.GroupBy(e => e.ToString()).Select(g => g.First()).ToList();

            lock (_lock)
            {
                _gathering = false;
                _localCandidates = candidates;
                _p2pAdvertiseUntilMs = V3Clock.NowMs + P2P_ADVERTISE_MS;
            }

            if (candidates.Count > 0)
                Logger.Log($"V3PairLink: Offering {candidates.Count} P2P candidate(s) to {Remote.Name}: {string.Join(", ", candidates)}");
        });
    }

    private void AddDirectCandidates(List<IPEndPoint> endpoints, long now)
    {
        _havePeerCandidates = true;

        foreach (var endpoint in endpoints.Take(6))
        {
            var tunnel = new P2PTunnel(endpoint, Remote.Name);
            if (_paths.TryGetValue(tunnel, out var existing))
            {
                // Retired before it was heard (a restart); offered again, so punch it again.
                if (!existing.Active)
                    ActivatePath(existing.Tunnel, now);

                continue;
            }

            _tunnelHandler.AddP2PTunnel(tunnel, Local.Id, Remote.Id);
            ActivatePath(tunnel, now);
        }
    }

    #endregion

    #region Ping tables, fallback list, load probe, stats

    private void TickPingTable(long now)
    {
        if (now - _lastTableSentMs >= PING_TABLE_INTERVAL_SECONDS * 1000L)
            _sendTableNext = true;
    }

    private void TickFallbackList(long now)
    {
        if ((!_peerTableChanged && now - _lastListRecomputeMs < 30_000) || _peerTable.Count == 0)
            return;

        // One update in flight at a time, so the peer's two newest versions always include ours.
        if (_listVersion != _committedVersion)
            return;

        _lastListRecomputeMs = now;
        _peerTableChanged = false;

        var primary = _primary!;
        var list = TunnelTable.Rank(GetPool(), _peerTable, new TunnelRankOptions
        {
            Count = RANKED_LIST_SIZE,
            CapacityThreshold = CAPACITY_THRESHOLD,
            HandshakeFailureThreshold = HANDSHAKE_FAILURE_THRESHOLD,
            PairSeed = PairSeed,
            Exclude = primary.IsDirect ? null : [primary.Tunnel],
            DeprioritiseRecentlyUsed = true,
            AvoidNetworkOf = primary.IsDirect ? null : primary.Tunnel.Address,
            GroupSize = FAILOVER_BATCH_SIZE
        });

        if (list.Count == 0 || list.Take(6).SequenceEqual(_list.Take(6)))
            return;

        _list = list;
        _listVersion++;
        Logger.Log($"V3PairLink: Fallbacks for {Remote.Name} now {string.Join(", ", list.Take(4).Select(t => t.Name))}");
    }

    private void StoreReceivedList(V3Heartbeat.FallbackListSection section)
    {
        if (_receivedLists.Any(l => l.Version == section.Version))
            return;

        var pool = _tunnelHandler.Tunnels;
        var list = section.Keys
            .Select(k => pool.Find(t => TunnelTable.GetKey(t) == k))
            .Where(t => t != null)
            .Select(t => t!)
            .ToList();

        _receivedLists.Add((section.Version, list));
        if (_receivedLists.Count > 2)
            _receivedLists.RemoveAt(0);
    }

    private ushort NewestReceivedListVersion => _receivedLists.Count == 0 ? (ushort)0 : _receivedLists[^1].Version;

    private void TickLoadProbe(long now, bool inGame)
    {
        if (inGame || LOAD_PROBE_INTERVAL_SECONDS <= 0 || _trial != null)
            return;

        if (_nextLoadProbeMs == 0)
            _nextLoadProbeMs = now + 20_000 + _random.Next(10_000);

        if (now >= _nextLoadProbeMs)
        {
            _loadProbeUntilMs = now + LOAD_PROBE_DURATION_MS;
            _nextLoadProbeMs = now + (LOAD_PROBE_INTERVAL_SECONDS * 1000L) + _random.Next(10_000);
        }
    }

    private void SendDueFiller(long now)
    {
        if (now >= _loadProbeUntilMs || now < _nextFillerMs || _primary == null)
            return;

        _nextFillerMs = now + LOAD_PROBE_INTERVAL_MS;
        _tunnelHandler.SendPacket(_primary.Tunnel, Local.Id, Remote.Id, TunnelPacketType.Filler, new byte[FILLER_SIZE]);
        _primary.CountSent();
    }

    private void UpdateDisplayStats(long now)
    {
        var primary = _primary;
        if (primary?.RttMs == null)
            return;

        int ping = (int)Math.Round(primary.RttMs.Value);
        double? loss = primary.GetLossPercent(now);
        _pingMs = ping;
        _lossTenths = loss.HasValue ? (int)Math.Round(loss.Value * 10) : -1;

        if (ping != _lastPingEventValue && now - _lastPingEventMs >= 1000)
        {
            _lastPingEventMs = now;
            _lastPingEventValue = ping;
            _pendingEvents.Add(() => PingUpdated?.Invoke(this, ping));
        }
    }

    #endregion

    #region Paths

    private V3LinkPath ActivatePath(CnCNetTunnel tunnel, long now)
    {
        if (!_paths.TryGetValue(tunnel, out var path))
        {
            path = new V3LinkPath(tunnel, now);
            _paths[tunnel] = path;
        }
        else if (!path.Active)
        {
            path.Active = true;
            path.CreatedMs = now;
            path.ResetConfirmation();
        }

        return path;
    }

    private void DeactivatePath(CnCNetTunnel tunnel, long now)
    {
        if (_paths.TryGetValue(tunnel, out var path) && path != _primary && path.Active)
        {
            path.Active = false;
            path.RetireUntilMs = now + RETIRE_ACCEPT_MS;
        }
    }

    private void RetireAll(long now)
    {
        foreach (var path in _paths.Values)
        {
            if (path.Active)
            {
                path.Active = false;
                path.RetireUntilMs = now + RETIRE_ACCEPT_MS;
            }
        }
    }

    private void ExpireRetiredPaths(long now)
    {
        foreach (var tunnel in _paths.Where(kv => !kv.Value.Active && kv.Value != _primary && now > kv.Value.RetireUntilMs).Select(kv => kv.Key).ToList())
            _paths.Remove(tunnel);
    }

    private V3LinkPath PickBest(List<V3LinkPath> confirmed, long now)
        => confirmed.Where(p => p.RttMs.HasValue).OrderBy(p => p.GetScore(now, PACKET_LOSS_WEIGHT)).FirstOrDefault() ?? confirmed[0];

    /// <summary>Makes <paramref name="path"/> the pair's path. The decider announces it with a new epoch.</summary>
    private void Promote(V3LinkPath path, string reason, long now)
    {
        var old = _primary;

        if (IsDecider)
            _epoch++;

        _primary = path;
        _primaryTunnel = path.Tunnel;
        path.Active = true;
        _previousPrimary = old != path ? old : null;
        _switchMs = now;

        // The cooldown is between changes of path; the first connection is not one.
        if (old != null)
            _lastSwitchMs = now;

        // The old primary keeps being accepted for a while; everything else that was only being
        // tried goes quiet and is left to expire. A direct path that has answered, or is still
        // being punched, stays warm, since it costs no tunnel slot.
        foreach (var other in _paths.Values.ToList())
        {
            if (other == path || !other.Active)
                continue;

            if (other.IsDirect && (other.LastHeardMs != 0 || now - other.CreatedMs <= DIRECT_PUNCH_MS))
                continue;

            other.Active = false;
            other.RetireUntilMs = now + RETIRE_ACCEPT_MS;
        }

        _trial = null;
        _failover = false;
        _failoverCandidates.Clear();
        _degradeEvidence = 0;

        bool firstConnect = _state == LinkState.BringingUp;
        _state = LinkState.Connected;
        _failures = 0;

        if (firstConnect)
        {
            // Version 0 of the fallback list is the rest of the ranked list. Both sides derive it
            // from the same ranked list and announced path, so it needs no exchange.
            var initial = _ranked.Where(t => t != path.Tunnel).ToList();
            _list = _committedList = initial;
            _listVersion = _committedVersion = 0;
            _receivedLists.Clear();
            _receivedLists.Add((0, initial));
            _sendTableNext = true;
        }

        Logger.Log($"V3PairLink: {Remote.Name} now on {path.Tunnel.Name}{(old != null && old != path ? $" (was {old.Tunnel.Name})" : string.Empty)}: {reason}");

        var oldTunnel = old?.Tunnel;
        _pendingEvents.Add(() => PathChanged?.Invoke(this, oldTunnel, path.Tunnel, reason));

        if (path.RttMs.HasValue)
            _pingMs = (int)Math.Round(path.RttMs.Value);

        if (_status != NegotiationStatus.Succeeded)
        {
            bool restored = _everConnected;
            _peerLostReported = false;
            SetStatus(NegotiationStatus.Succeeded, null, restored: restored);
        }

        _everConnected = true;
        UpdateSnapshots(now);
    }

    private V3LinkPath? ResolveAnnounced(uint key, long now)
    {
        if (key == TunnelTable.NO_PATH_KEY)
            return null;

        if (key == TunnelTable.DIRECT_PATH_KEY)
        {
            return _paths.Values
                .Where(p => p.IsDirect && p.LastHeardMs != 0)
                .OrderByDescending(p => p.IsConfirmed(now, SUSPECT_LOBBY_MS))
                .ThenBy(p => p.GetScore(now, PACKET_LOSS_WEIGHT))
                .FirstOrDefault();
        }

        var existing = _paths.Values.FirstOrDefault(p => !p.IsDirect && TunnelTable.GetKey(p.Tunnel) == key);
        if (existing != null)
            return ActivatePath(existing.Tunnel, now);

        var tunnel = _tunnelHandler.Tunnels.Find(t => TunnelTable.GetKey(t) == key);
        if (tunnel == null)
            return null;

        _tunnelHandler.SendRegistrationToTunnels(Local.Id, [tunnel]);
        return ActivatePath(tunnel, now);
    }

    private static uint KeyOf(V3LinkPath? path)
        => path == null ? TunnelTable.NO_PATH_KEY : path.IsDirect ? TunnelTable.DIRECT_PATH_KEY : TunnelTable.GetKey(path.Tunnel);

    /// <summary>Publishes the lock-free snapshots the game bridge reads.</summary>
    private void UpdateSnapshots(long now)
    {
        var send = new List<V3LinkPath>(3);
        if (_primary != null && _state == LinkState.Connected)
        {
            send.Add(_primary);
            int suspect = SUSPECT_LOBBY_MS;

            if (_failover)
            {
                // Copy traffic onto fallbacks straight away rather than waiting for them to
                // confirm: a tunnel just drops packets for a peer not registered there yet, and our
                // own packets register us, so traffic flows the moment the peer arrives. Once
                // something confirms, only the confirmed ones are kept.
                var candidates = _paths.Values
                    .Where(p => p.Active && p != _primary && (_failoverCandidates.Contains(p.Tunnel) || p.IsDirect))
                    .ToList();

                var confirmed = candidates.Where(p => p.IsConfirmed(now, suspect)).ToList();
                send.AddRange(confirmed.Count > 0
                    ? confirmed.OrderBy(p => p.GetScore(now, PACKET_LOSS_WEIGHT)).Take(2)
                    : candidates.OrderByDescending(p => p.LastHeardMs).Take(3));
            }

            if (_trial != null && _trial != _primary && _trial.IsConfirmed(now, suspect))
                send.Add(_trial);

            // Just switched to a path we have not heard on yet: keep the old one going too until
            // the new one confirms, so nothing is lost while the peer catches up.
            if (_previousPrimary != null && now - _switchMs < RETIRE_ACCEPT_MS && !_primary.IsConfirmed(now, suspect) &&
                _previousPrimary.IsConfirmed(now, suspect))
            {
                send.Add(_previousPrimary);
            }
        }

        if (!SameSequence(send, _sendPaths))
            _sendPaths = [.. send];

        var accept = _paths.Values.Where(p => p.Active || now <= p.RetireUntilMs).ToList();
        if (!SameSequence(accept, _acceptPaths))
            _acceptPaths = [.. accept];
    }

    private static bool SameSequence(List<V3LinkPath> a, V3LinkPath[] b)
    {
        if (a.Count != b.Length)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    #endregion

    #region Heartbeats

    private void SendDueHeartbeats(long now, bool inGame)
    {
        int interval = inGame ? HEARTBEAT_GAME_MS : HEARTBEAT_LOBBY_MS;

        foreach (var path in _paths.Values)
        {
            if (!path.Active)
                continue;

            // A spare direct path beside a direct primary needs less, but still enough to stay
            // confirmed within the suspect window in case it is promoted. Beside a relay it is the
            // upgrade candidate, and needs the full rate for its loss to be measured.
            int pathInterval = _state == LinkState.Connected && path != _primary && path != _trial && !_failover && path.IsDirect &&
                path.LastHeardMs != 0 && _primary?.IsDirect == true
                ? interval * 2
                : interval;

            if (now - path.LastSentHeartbeatMs >= pathInterval)
                SendHeartbeat(path, now, inGame, extraFlags: 0);
        }
    }

    private void SendHeartbeat(V3LinkPath path, long now, bool inGame, byte extraFlags)
    {
        var heartbeat = new V3Heartbeat
        {
            Session = _session,
            Flags = (byte)((inGame ? V3Heartbeat.FLAG_IN_GAME : 0) |
                           (path == _primary ? V3Heartbeat.FLAG_ON_PRIMARY : 0) |
                           extraFlags |
                           (_p2pEnabled ? V3Heartbeat.FLAG_P2P : 0) |
                           (_havePeerCandidates ? V3Heartbeat.FLAG_HAVE_P2P_CANDIDATES : 0)),
            TimeMs = (uint)now,
            SentTotal = (uint)(path.SentTotal + 1),
            EchoSentTotal = path.LastPeerSentTotal,
            ReceivedTotal = path.ReceivedAtLastPeerHeartbeat,
            PrimaryKey = KeyOf(_primary),
            Epoch = _epoch,
            TrialKey = IsDecider ? KeyOf(_trial) : TunnelTable.NO_PATH_KEY,
            ListVersion = IsDecider ? _listVersion : NewestReceivedListVersion
        };

        if (path.LastPeerTimeAtMs != 0 && now - path.LastPeerTimeAtMs < ushort.MaxValue)
        {
            heartbeat.EchoTimeMs = path.LastPeerTimeMs;
            heartbeat.HoldMs = (ushort)(now - path.LastPeerTimeAtMs);
        }

        // The game bridge stamps LastHeardMs from its own thread, so it can be a moment later than
        // this tick's clock; a negative difference would wrap to the "never heard" marker.
        long heard = path.LastHeardMs;
        heartbeat.HeardYouAgoMs = heard == 0 ? ushort.MaxValue : (ushort)Math.Min(ushort.MaxValue - 1, Math.Max(0, now - heard));

        if (path == _primary && _state == LinkState.Connected)
        {
            if (IsDecider && _listVersion != _committedVersion)
                heartbeat.FallbackList = new V3Heartbeat.FallbackListSection(_listVersion, _list.Select(TunnelTable.GetKey).ToList());

            if (_sendTableNext)
            {
                _sendTableNext = false;
                _lastTableSentMs = now;
                // Measured tunnels first, and capped: the original tunnel server reads datagrams
                // into a 1024-byte buffer.
                heartbeat.PingTable = TunnelTable.BuildLocalTable(GetPool(), HANDSHAKE_FAILURE_THRESHOLD, CAPACITY_THRESHOLD)
                    .OrderBy(e => e.HasPing ? 0 : 1)
                    .ThenBy(e => e.PingMs)
                    .Take(MAX_HEARTBEAT_TABLE_ENTRIES)
                    .ToList();
            }

            if (now - _peerTableAtMs > PING_TABLE_INTERVAL_SECONDS * 2000L && now - _lastTableRequestMs > 10_000)
            {
                _lastTableRequestMs = now;
                heartbeat.Requests |= V3Heartbeat.REQUEST_PING_TABLE;
            }
        }

        if (_localCandidates is { Count: > 0 } && !_peerHasOurCandidates && now < _p2pAdvertiseUntilMs &&
            now - _lastP2PAdvertMs >= 1000 && path == _primary)
        {
            _lastP2PAdvertMs = now;
            heartbeat.P2PCandidates = _localCandidates;
        }

        path.LastSentHeartbeatMs = now;
        path.CountSent();
        _tunnelHandler.SendPacket(path.Tunnel, Local.Id, Remote.Id, TunnelPacketType.Heartbeat, heartbeat.Encode());
    }

    /// <summary>Receive thread: every negotiation-type packet from the peer.</summary>
    private void OnPacket(uint senderId, uint receiverId, TunnelPacketType packetType,
        ReadOnlyMemory<byte> payload, long receivedTime, CnCNetTunnel tunnel)
    {
        if (packetType is TunnelPacketType.TunnelList or TunnelPacketType.TunnelSet)
        {
            TunnelTableExchange? exchange;
            lock (_lock)
            {
                // The peer is trying again while we wait out our own retry delay, or has started
                // over while we still think we are connected: join in now. Never mid-game, which
                // needs the current path.
                if (packetType == TunnelPacketType.TunnelList && IsDecider && !_inGame &&
                    (_state == LinkState.WaitingRetry ||
                     (_state is LinkState.BringingUp or LinkState.Connected && IsNewAttempt(payload.Span))))
                {
                    StartExchange(V3Clock.NowMs);
                }

                exchange = _exchange;
            }

            exchange?.TryHandlePacket(packetType, payload, tunnel);
            DispatchEvents();
            return;
        }

        lock (_lock)
        {
            if (_state is not (LinkState.BringingUp or LinkState.Connected))
                return;

            long now = V3Clock.NowMs;

            if (!_paths.TryGetValue(tunnel, out var path))
            {
                // A direct path the communicator learned from an unexpected source address.
                if (tunnel is not P2PTunnel || !_p2pEnabled)
                    return;

                path = ActivatePath(tunnel, now);
                Logger.Log($"V3PairLink: Auto-learned direct path {tunnel.Name} to {Remote.Name}");
            }

            if (!path.Active && now > path.RetireUntilMs)
                return;

            if (packetType == TunnelPacketType.Heartbeat)
            {
                if (!V3Heartbeat.TryDecode(payload.Span, out var heartbeat))
                    return;

                // Left over from an earlier attempt with this player, still in flight.
                if (_session != 0 && heartbeat.Session != 0 && heartbeat.Session != _session)
                    return;

                path.CountReceived(now);
                ProcessHeartbeat(path, heartbeat, now);
            }
            else if (packetType == TunnelPacketType.Filler)
            {
                path.CountReceived(now);
            }
        }

        DispatchEvents();
    }

    /// <summary>Whether a table chunk belongs to an exchange other than the one this connection came from.</summary>
    private bool IsNewAttempt(ReadOnlySpan<byte> chunk)
        => chunk.Length >= 5 && chunk[0] == TunnelTable.FORMAT_VERSION &&
           System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(chunk[1..]) != _session;

    private void ProcessHeartbeat(V3LinkPath path, V3Heartbeat heartbeat, long now)
    {

        if (heartbeat.EchoTimeMs != 0)
        {
            int rtt = (int)((uint)now - heartbeat.EchoTimeMs) - heartbeat.HoldMs;
            if (rtt >= 0 && rtt < 10_000)
                path.AddRttSample(rtt);
        }

        path.LastPeerTimeMs = heartbeat.TimeMs;
        path.LastPeerTimeAtMs = now;
        path.PeerHeardMeAgoMs = heartbeat.HeardYouAgoMs == ushort.MaxValue ? 1_000_000 : heartbeat.HeardYouAgoMs;
        path.PeerReportAtMs = now;
        path.AddLossSample(now, heartbeat.SentTotal, heartbeat.EchoSentTotal, heartbeat.ReceivedTotal);

        if (path.ConfirmedSinceMs == 0 && path.IsConfirmed(now, SUSPECT_LOBBY_MS))
            path.MarkConfirmed(now);

        _peerGoodbye = (heartbeat.Flags & V3Heartbeat.FLAG_GOODBYE) != 0;
        _peerInGame = (heartbeat.Flags & V3Heartbeat.FLAG_IN_GAME) != 0;
        _peerP2P = (heartbeat.Flags & V3Heartbeat.FLAG_P2P) != 0;
        _peerHasOurCandidates = (heartbeat.Flags & V3Heartbeat.FLAG_HAVE_P2P_CANDIDATES) != 0;

        if (_peerGoodbye)
            Logger.Log($"V3PairLink: {Remote.Name} is closing the connection");

        if ((heartbeat.Flags & V3Heartbeat.FLAG_RESTART) != 0 && !_inGame)
        {
            Logger.Log($"V3PairLink: {Remote.Name} is starting the connection over; doing the same");
            StartExchange(now);
            return;
        }

        if (heartbeat.PingTable != null)
        {
            _peerTable = heartbeat.PingTable.ToDictionary(e => e.Key);
            _peerTableAtMs = now;
            _peerTableChanged = true;
        }

        if ((heartbeat.Requests & V3Heartbeat.REQUEST_PING_TABLE) != 0)
            _sendTableNext = true;

        if (heartbeat.P2PCandidates != null && _p2pEnabled)
            AddDirectCandidates(heartbeat.P2PCandidates, now);

        if (IsDecider)
        {
            if (heartbeat.ListVersion == _listVersion && _committedVersion != _listVersion)
            {
                _committedVersion = _listVersion;
                _committedList = _list;
            }

            return;
        }

        if (heartbeat.FallbackList != null)
            StoreReceivedList(heartbeat.FallbackList);

        // Follow the decider's choice. Epochs only grow within a session, with wraparound.
        if ((short)(heartbeat.Epoch - _epoch) > 0 && heartbeat.PrimaryKey != TunnelTable.NO_PATH_KEY)
        {
            // For a direct path, the one the decider's own path heartbeat arrived on is exactly the
            // one it chose; any other direct path may pair different adapters.
            bool onDecidersDirectPath = heartbeat.PrimaryKey == TunnelTable.DIRECT_PATH_KEY && path.IsDirect &&
                (heartbeat.Flags & V3Heartbeat.FLAG_ON_PRIMARY) != 0;
            var target = onDecidersDirectPath ? path : ResolveAnnounced(heartbeat.PrimaryKey, now);
            if (target != null)
            {
                _epoch = heartbeat.Epoch;
                if (target != _primary)
                {
                    string reason = _state == LinkState.BringingUp ? "connected" : $"chosen by {Remote.Name}";
                    if (_state == LinkState.BringingUp)
                        RecordHandshakeOutcomes(_paths.Values.Where(p => p.Active).ToList(), now);

                    Promote(target, reason, now);
                }
            }
        }

        // Measure whatever the decider is trialling, so both directions are judged.
        uint trialKey = heartbeat.TrialKey;
        if (_state == LinkState.Connected)
        {
            if (trialKey == TunnelTable.NO_PATH_KEY)
            {
                if (_trial != null && _trial != _primary)
                {
                    if (!_trial.IsDirect)
                        DeactivatePath(_trial.Tunnel, now);

                    _trial = null;
                }
            }
            else if (KeyOf(_trial) != trialKey && trialKey != KeyOf(_primary))
            {
                _trial = ResolveAnnounced(trialKey, now);
            }
        }
    }

    #endregion

    #region Events

    private void SetStatus(NegotiationStatus status, string? reason, bool restored = false, bool suggestRenegotiate = false)
    {
        _status = status;
        var change = new LinkStatusChange
        {
            Status = status,
            PingMs = _pingMs,
            Reason = reason,
            Restored = restored,
            SuggestRenegotiate = suggestRenegotiate
        };

        _pendingEvents.Add(() => StatusChanged?.Invoke(this, change));
    }

    private void DispatchEvents()
    {
        List<Action>? events = null;
        lock (_lock)
        {
            if (_pendingEvents.Count == 0)
                return;

            events = [.. _pendingEvents];
            _pendingEvents.Clear();
        }

        foreach (var raise in events)
        {
            try
            {
                raise();
            }
            catch (Exception ex)
            {
                Logger.Log($"V3PairLink: Event handler failed: {ex}");
            }
        }
    }

    #endregion

    /// <summary>Handles a <see cref="TunnelTableExchange.IRC_COMMAND"/> message from the peer.</summary>
    public void HandleIrcMessage(char kind, uint nonce, string data)
    {
        TunnelTableExchange? exchange;
        lock (_lock)
        {
            if (_state == LinkState.WaitingRetry && kind == 'L' && IsDecider)
                StartExchange(V3Clock.NowMs);

            exchange = _exchange;
        }

        exchange?.HandleIrcMessage(kind, nonce, data);
        DispatchEvents();
    }

    public override string ToString() => $"{Local.Name} <-> {Remote.Name}";
}
