#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// The first step of connecting a pair: the non-decider sends its tunnel ping table and the
/// decider answers with the pair's ranked list, which both sides then work through.
/// </summary>
/// <remarks>
/// <para>The tables travel over the matchmaking servers, which relay between registered clients
/// without the pair needing a tunnel in common yet. Only servers that answer this client's UDP
/// pings are used. If none answers, or nothing has come back after
/// <see cref="ClientConfiguration.V3MatchmakingIrcFallbackDelayMs"/>, the table also goes over
/// IRC. That is slower, but it reaches any player who can be in the lobby at all, including one
/// who cannot reach the matchmaking servers' hosts.</para>
///
/// <para>Only the decider needs the peer's table, since the non-decider adopts the list that comes
/// back. One side ranking matters because the two clients refresh the master list on their own
/// schedules, and two slightly different pools would give two different lists.</para>
/// </remarks>
public sealed class TunnelTableExchange : IDisposable
{
    /// <summary>CTCP command carrying a compact table or ranked list over IRC.</summary>
    public const string IRC_COMMAND = "TNLTBL";

    private static bool MATCHMAKING_ENABLED => ClientConfiguration.Instance.V3MatchmakingEnabled;
    private static double CAPACITY_THRESHOLD => ClientConfiguration.Instance.V3MatchmakingCapacityThreshold;
    private static int EXCHANGE_TIMEOUT_MS => ClientConfiguration.Instance.V3MatchmakingExchangeTimeoutMs;
    private static int RETRY_INTERVAL_MS => ClientConfiguration.Instance.V3MatchmakingRetryIntervalMs;
    private static int IRC_FALLBACK_DELAY_MS => ClientConfiguration.Instance.V3MatchmakingIrcFallbackDelayMs;
    private static int HANDSHAKE_FAILURE_THRESHOLD => ClientConfiguration.Instance.V3MatchmakingHandshakeFailureThreshold;
    private static int RANKED_LIST_SIZE => ClientConfiguration.Instance.V3RankedListSize;
    private static int BRING_UP_BATCH_SIZE => ClientConfiguration.Instance.V3BringUpBatchSize;

    /// <summary>Interval (ms) between repeats of an IRC message that has not been answered.</summary>
    private const int IRC_RESEND_INTERVAL_MS = 6000;

    private readonly V3PlayerInfo _localPlayer;
    private readonly V3PlayerInfo _remotePlayer;
    private readonly IReadOnlyList<CnCNetTunnel> _pool;
    private readonly TunnelHandler _tunnelHandler;
    private readonly Action<string> _sendIrc;
    private readonly bool _isDecider;
    private readonly uint _pairSeed;

    private readonly TunnelTableAssembler _peerTable = new();
    private readonly TaskCompletionSource<bool> _peerTableReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<List<uint>> _rankedReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Identifies this exchange. The non-decider stamps it on its table and the decider echoes
    /// it in the ranked list, so a list answering a torn-down attempt is recognised and ignored.
    /// </summary>
    private readonly uint _nonce = (uint)Random.Shared.Next(int.MinValue, int.MaxValue);

    // Decider: the published answer, kept so a peer still sending its table can be answered
    // again. Written on the exchange task and read on the receive thread.
    private volatile byte[]? _publishedRanked;
    private volatile string? _publishedRankedCompact;
    private uint _publishedNonce;

    // Decider: whether the peer's table came over IRC, so the answer goes back the same way.
    private volatile bool _peerUsedIrc;
    private long _lastIrcAnswerTicks;

    /// <summary>
    /// The nonce both sides know once the exchange has succeeded: the non-decider's own, which
    /// the decider echoed. It identifies the resulting connection in its heartbeats.
    /// </summary>
    public uint SessionNonce => _isDecider ? _publishedNonce : _nonce;

    public TunnelTableExchange(V3PlayerInfo localPlayer, V3PlayerInfo remotePlayer,
        IReadOnlyList<CnCNetTunnel> pool, TunnelHandler tunnelHandler, Action<string> sendIrc,
        bool isDecider, uint pairSeed)
    {
        _localPlayer = localPlayer;
        _remotePlayer = remotePlayer;
        _pool = pool;
        _tunnelHandler = tunnelHandler;
        _sendIrc = sendIrc;
        _isDecider = isDecider;
        _pairSeed = pairSeed;
    }

    /// <summary>
    /// Offers a received UDP packet to the exchange. Returns true if it was exchange traffic, which
    /// the caller must then not handle itself.
    /// </summary>
    public bool TryHandlePacket(TunnelPacketType packetType, ReadOnlyMemory<byte> payload, CnCNetTunnel tunnel)
    {
        switch (packetType)
        {
            case TunnelPacketType.TunnelList:
                if (!_isDecider)
                    return true;

                if (_peerTable.AddChunk(payload.Span))
                    _peerTableReceived.TrySetResult(true);

                // The peer stops sending once our list reaches it, so a table arriving after we
                // answered means the answer was lost. Only answer again for the same exchange.
                byte[]? published = _publishedRanked;
                if (published != null && _peerTable.Nonce == _publishedNonce)
                    _tunnelHandler.SendPacket(tunnel, _localPlayer.Id, _remotePlayer.Id, TunnelPacketType.TunnelSet, published);

                return true;

            case TunnelPacketType.TunnelSet:
                if (!_isDecider && TunnelTable.TryDecodeRankedList(payload.Span, out var keys, out uint nonce))
                    AcceptRanked(keys, nonce);

                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Handles a <see cref="IRC_COMMAND"/> message from the peer. <paramref name="kind"/> is
    /// <c>L</c> for a table and <c>R</c> for a ranked list.
    /// </summary>
    public void HandleIrcMessage(char kind, uint nonce, string data)
    {
        if (kind == 'L' && _isDecider)
        {
            var entries = TunnelTable.DecodeTableCompact(data, _pool);
            if (entries == null)
                return;

            _peerUsedIrc = true;
            _peerTable.SetWhole(entries, nonce);
            _peerTableReceived.TrySetResult(true);

            string? published = _publishedRankedCompact;
            if (published != null && nonce == _publishedNonce)
                SendIrcAnswer(published, nonce, force: false);
        }
        else if (kind == 'R' && !_isDecider)
        {
            var keys = TunnelTable.DecodeRankedCompact(data, _pool);
            if (keys != null && keys.Count > 0)
                AcceptRanked(keys, nonce);
        }
    }

    private void AcceptRanked(List<uint> keys, uint nonce)
    {
        if (nonce == _nonce)
            _rankedReceived.TrySetResult(keys);
        else
            Logger.Log($"TunnelTableExchange: Ignoring a ranked list from {_remotePlayer.Name} for a different exchange");
    }

    /// <summary>
    /// Runs the exchange. Returns the pair's ranked list, or null if the peer never answered or
    /// the two tables have no tunnel in common.
    /// </summary>
    public async Task<List<CnCNetTunnel>?> RunAsync(CancellationToken cancellationToken)
    {
        var matchmakingTunnels = MATCHMAKING_ENABLED ? GetReachableMatchmakingTunnels() : [];

        // Matchmaking servers only relay between registered clients.
        if (matchmakingTunnels.Count > 0)
            _tunnelHandler.SendRegistrationToTunnels(_localPlayer.Id, matchmakingTunnels);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(EXCHANGE_TIMEOUT_MS);

        try
        {
            return _isDecider
                ? await RunAsDeciderAsync(matchmakingTunnels, cts.Token).ConfigureAwait(false)
                : await RunAsNonDeciderAsync(matchmakingTunnels, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.Log($"TunnelTableExchange: No answer from {_remotePlayer.Name} within {EXCHANGE_TIMEOUT_MS}ms");
            return null;
        }
    }

    /// <summary>
    /// The matchmaking servers this client can reach, judged by UDP ping. If none has been
    /// measured yet (the first refresh is still running), all of them are tried.
    /// </summary>
    private List<CnCNetTunnel> GetReachableMatchmakingTunnels()
    {
        var all = _tunnelHandler.MatchmakingTunnels;
        var reachable = all.Where(t => t.Ping.IsValid()).ToList();

        if (reachable.Count > 0)
            return reachable;

        return all.All(t => t.ConsecutiveUnknownPings == 0) ? all : [];
    }

    private async Task<List<CnCNetTunnel>?> RunAsDeciderAsync(List<CnCNetTunnel> matchmakingTunnels, CancellationToken token)
    {
        // Registration is a single datagram, so repeat it while waiting.
        while (!_peerTableReceived.Task.IsCompleted)
        {
            await Task.WhenAny(_peerTableReceived.Task, Task.Delay(2000, token)).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            if (!_peerTableReceived.Task.IsCompleted && matchmakingTunnels.Count > 0)
                _tunnelHandler.SendRegistrationToTunnels(_localPlayer.Id, matchmakingTunnels);
        }

        var (peerEntries, peerNonce) = _peerTable.GetEntries();

        bool viaIrc = _peerUsedIrc;
        var ranked = TunnelTable.Rank(_pool, peerEntries, new TunnelRankOptions
        {
            Count = RANKED_LIST_SIZE,
            CapacityThreshold = CAPACITY_THRESHOLD,
            HandshakeFailureThreshold = HANDSHAKE_FAILURE_THRESHOLD,
            PairSeed = _pairSeed,
            GroupSize = BRING_UP_BATCH_SIZE
        });

        // The compact encoding names tunnels by a 16-bit key, so a tunnel whose key collides
        // with another in our pool cannot be named unambiguously over IRC.
        if (viaIrc)
            ranked = ranked.Where(t => TunnelTable.HasUniqueShortKey(t, _pool)).ToList();

        if (ranked.Count == 0)
        {
            Logger.Log($"TunnelTableExchange: {_remotePlayer.Name} sent {peerEntries.Count} tunnel(s), but none is reachable by both of us");
            return null;
        }

        Logger.Log($"TunnelTableExchange: Ranked {ranked.Count} tunnel(s) with {_remotePlayer.Name} from their {peerEntries.Count} " +
            $"(via {(viaIrc ? "IRC" : "matchmaking")}): {string.Join(", ", ranked.Select(t => t.Name))}");

        _publishedNonce = peerNonce;
        _publishedRanked = TunnelTable.EncodeRankedList(ranked, peerNonce);
        _publishedRankedCompact = TunnelTable.EncodeRankedCompact(ranked);

        foreach (var matchmakingTunnel in matchmakingTunnels)
            _tunnelHandler.SendPacket(matchmakingTunnel, _localPlayer.Id, _remotePlayer.Id, TunnelPacketType.TunnelSet, _publishedRanked);

        if (viaIrc)
            SendIrcAnswer(_publishedRankedCompact, peerNonce, force: true);

        return ranked;
    }

    private void SendIrcAnswer(string compact, uint nonce, bool force)
    {
        long now = V3Clock.NowMs;
        if (!force && now - Interlocked.Read(ref _lastIrcAnswerTicks) < IRC_RESEND_INTERVAL_MS)
            return;

        Interlocked.Exchange(ref _lastIrcAnswerTicks, now);
        _sendIrc(FormatIrc('R', nonce, compact));
    }

    private async Task<List<CnCNetTunnel>?> RunAsNonDeciderAsync(List<CnCNetTunnel> matchmakingTunnels, CancellationToken token)
    {
        var table = TunnelTable.BuildLocalTable(_pool, HANDSHAKE_FAILURE_THRESHOLD, CAPACITY_THRESHOLD);
        var chunks = TunnelTable.EncodeTableChunks(table, _nonce);
        string compact = TunnelTable.EncodeTableCompact(table);

        long started = V3Clock.NowMs;
        long nextIrc = matchmakingTunnels.Count == 0 ? started : started + IRC_FALLBACK_DELAY_MS;

        // Repeat until the ranked list comes back. The two peers start off separate IRC events
        // and can be seconds apart, so this doubles as the wait for the decider to show up.
        while (!_rankedReceived.Task.IsCompleted)
        {
            foreach (var matchmakingTunnel in matchmakingTunnels)
            {
                foreach (var chunk in chunks)
                    _tunnelHandler.SendPacket(matchmakingTunnel, _localPlayer.Id, _remotePlayer.Id, TunnelPacketType.TunnelList, chunk);
            }

            if (V3Clock.NowMs >= nextIrc)
            {
                Logger.Log($"TunnelTableExchange: Sending our tunnel table to {_remotePlayer.Name} over IRC");
                _sendIrc(FormatIrc('L', _nonce, compact));
                nextIrc = V3Clock.NowMs + IRC_RESEND_INTERVAL_MS;
            }

            await Task.WhenAny(_rankedReceived.Task, Task.Delay(RETRY_INTERVAL_MS, token)).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
        }

        var keys = await _rankedReceived.Task.ConfigureAwait(false);
        var byKey = new Dictionary<uint, CnCNetTunnel>();
        foreach (var tunnel in _pool)
            byKey[TunnelTable.GetKey(tunnel)] = tunnel;

        // Unknown keys are skipped: a master list refresh can have dropped a tunnel the decider
        // still had.
        var ranked = keys
            .Where(byKey.ContainsKey)
            .Select(k => byKey[k])
            .Distinct()
            .ToList();

        if (ranked.Count == 0)
        {
            Logger.Log($"TunnelTableExchange: None of the {keys.Count} tunnel(s) {_remotePlayer.Name} ranked are in our tunnel list");
            return null;
        }

        Logger.Log($"TunnelTableExchange: Adopted {_remotePlayer.Name}'s ranked list of {ranked.Count} tunnel(s): {string.Join(", ", ranked.Select(t => t.Name))}");
        return ranked;
    }

    private string FormatIrc(char kind, uint nonce, string data)
        => $"{IRC_COMMAND} {_localPlayer.Id:x8} {_remotePlayer.Id:x8} {kind} {nonce:x8} {data}";

    /// <summary>
    /// Parses a <see cref="IRC_COMMAND"/> payload (the part after the command name).
    /// </summary>
    public static bool TryParseIrc(string payload, out uint fromId, out uint toId, out char kind, out uint nonce, out string data)
    {
        fromId = toId = nonce = 0;
        kind = '\0';
        data = string.Empty;

        string[] parts = payload.Split(' ');
        if (parts.Length != 5 || parts[2].Length != 1)
            return false;

        kind = parts[2][0];
        data = parts[4];

        return uint.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out fromId) &&
               uint.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out toId) &&
               uint.TryParse(parts[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out nonce);
    }

    /// <summary>Releases whichever wait is still pending.</summary>
    public void Dispose()
    {
        _peerTableReceived.TrySetCanceled();
        _rankedReceived.TrySetCanceled();
    }
}
