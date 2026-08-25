#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// The tunnels a pair will negotiate over, and how that set was arrived at.
/// </summary>
public readonly struct MatchmakingResult
{
    public MatchmakingResult(List<CnCNetTunnel> tunnels, bool agreed)
    {
        Tunnels = tunnels;
        Agreed = agreed;
    }

    /// <summary>The tunnels to negotiate over, in preference order.</summary>
    public List<CnCNetTunnel> Tunnels { get; }

    /// <summary>
    /// Whether the exchange produced this list. If so both peers hold the same tunnels and got
    /// here within about a second of each other, so the relay negotiation can use a tighter
    /// handshake budget.
    /// </summary>
    public bool Agreed { get; }
}

/// <summary>
/// The matchmaking phase of a negotiation: over the matchmaking servers, the non-decider
/// advertises every relay tunnel it knows and the decider answers with the shortlist the pair
/// will negotiate over. Runs to completion before either peer touches a relay tunnel.
/// </summary>
/// <remarks>
/// <para>Meeting on one server to trade lists costs two registrations; testing every tunnel
/// instead would put every player on every server at once, and a tunnel server holds only a few
/// hundred clients.</para>
///
/// <para>Only the decider needs the peer's list, since the non-decider adopts whatever shortlist
/// comes back. One side publishing it matters because the two clients refresh the master list on
/// their own schedules, and two slightly different pools would yield two different shortlists.</para>
///
/// <para>Every failure path falls back to a locally ranked shortlist, so a matchmaking outage
/// costs tunnel quality, not playability.</para>
/// </remarks>
public sealed class MatchmakingExchange : IDisposable
{
    // Configurable via NetworkDefinitions.ini (section [V3Matchmaking]); see ClientConfiguration
    // for the documented defaults.
    private static bool ENABLED => ClientConfiguration.Instance.V3MatchmakingEnabled;
    private static int CANDIDATE_COUNT => ClientConfiguration.Instance.V3MatchmakingCandidateCount;
    private static int DIVERSITY_SLOTS => ClientConfiguration.Instance.V3MatchmakingDiversitySlots;
    private static int FALLBACK_COUNT => ClientConfiguration.Instance.V3MatchmakingFallbackCandidateCount;
    private static int FALLBACK_DETERMINISTIC_SLOTS => ClientConfiguration.Instance.V3MatchmakingFallbackDeterministicSlots;
    private static double CAPACITY_THRESHOLD => ClientConfiguration.Instance.V3MatchmakingCapacityThreshold;
    private static int EXCHANGE_TIMEOUT_MS => ClientConfiguration.Instance.V3MatchmakingExchangeTimeoutMs;
    private static int RETRY_INTERVAL_MS => ClientConfiguration.Instance.V3MatchmakingRetryIntervalMs;
    private static int HANDSHAKE_FAILURE_THRESHOLD => ClientConfiguration.Instance.V3MatchmakingHandshakeFailureThreshold;

    private readonly V3PlayerInfo _localPlayer;
    private readonly V3PlayerInfo _remotePlayer;
    private readonly IReadOnlyList<CnCNetTunnel> _candidatePool;
    private readonly TunnelHandler _tunnelHandler;
    private readonly bool _isDecider;

    /// <summary>
    /// The peer's advertised tunnel list, reassembled as its chunks arrive. Decider only. Filled
    /// on the communicator's receive thread and read on the negotiation task.
    /// </summary>
    private readonly TunnelListAssembler _peerTunnelList = new();

    /// <summary>Completes once every chunk of the peer's list has arrived.</summary>
    private readonly TaskCompletionSource<bool> _peerTunnelListReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes with the decider's shortlist keys once they arrive.</summary>
    private readonly TaskCompletionSource<List<uint>> _shortlistReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Identifies this exchange on the wire. The non-decider stamps it on every advertised chunk
    /// and the decider echoes it in the shortlist, so a shortlist published by a torn-down round —
    /// which a lobby-wide renegotiation makes routine, since the peer's old negotiator can still be
    /// alive and answering when the new one starts advertising — is recognised and ignored rather
    /// than adopted as agreement. Random rather than sequential so it cannot repeat across rounds.
    /// </summary>
    private readonly uint _nonce = (uint)Random.Shared.Next(int.MinValue, int.MaxValue);

    /// <summary>
    /// The decider's encoded shortlist, kept so a peer that is still advertising can be answered
    /// again. Written on the negotiation task and read on the receive thread.
    /// </summary>
    private volatile byte[]? _sentShortlistPayload;

    /// <summary>
    /// The nonce <see cref="_sentShortlistPayload"/> answers. A later advertisement carrying a
    /// different nonce is a new exchange, so the stored answer no longer applies to it.
    /// </summary>
    private uint _sentShortlistNonce;

    /// <param name="candidatePool">Every relay tunnel available to this client.</param>
    /// <param name="isDecider">
    /// Which half of the exchange this client performs, so exactly one peer publishes the shortlist.
    /// </param>
    public MatchmakingExchange(V3PlayerInfo localPlayer, V3PlayerInfo remotePlayer,
        IReadOnlyList<CnCNetTunnel> candidatePool, TunnelHandler tunnelHandler, bool isDecider)
    {
        _localPlayer = localPlayer;
        _remotePlayer = remotePlayer;
        _candidatePool = candidatePool;
        _tunnelHandler = tunnelHandler;
        _isDecider = isDecider;
    }

    /// <summary>
    /// Offers a received packet to the exchange. Returns true if it was matchmaking traffic, which
    /// the caller must then not handle itself.
    /// </summary>
    public bool TryHandlePacket(TunnelPacketType packetType, ReadOnlyMemory<byte> payload, CnCNetTunnel tunnel)
    {
        switch (packetType)
        {
            case TunnelPacketType.TunnelList:
                if (!_isDecider)
                    return true;

                if (_peerTunnelList.AddChunk(payload))
                    _peerTunnelListReceived.TrySetResult(true);

                // The peer stops advertising once our shortlist reaches it, so a list arriving
                // after we published one means that shortlist was lost in flight. Only resend when
                // the list still belongs to the exchange that shortlist answered: if the peer has
                // moved on to a new exchange, answering it here would hand its fresh negotiation a
                // shortlist agreed with a round that no longer exists.
                byte[]? publishedShortlist = _sentShortlistPayload;
                if (publishedShortlist != null && _peerTunnelList.Nonce == _sentShortlistNonce)
                {
                    _tunnelHandler.SendPacket(tunnel, _localPlayer.Id, _remotePlayer.Id,
                        TunnelPacketType.TunnelSet, publishedShortlist);
                }

                return true;

            case TunnelPacketType.TunnelSet:
                // An unreadable payload leaves the wait running rather than failing the exchange;
                // the peer may yet send one this client can read. A shortlist answering a different
                // exchange is likewise ignored rather than adopted — it comes from a round that has
                // since been torn down, and adopting it would leave this client negotiating over
                // tunnels the peer's current round knows nothing about.
                if (!_isDecider && TunnelShortlist.TryDecodeShortlist(payload, out var keys, out uint nonce))
                {
                    if (nonce == _nonce)
                        _shortlistReceived.TrySetResult(keys);
                    else
                        Logger.Log($"MatchmakingExchange: Ignoring a shortlist from {_remotePlayer.Name} for a different exchange (likely from a round that has since restarted)");
                }

                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Runs the exchange and returns the tunnels to negotiate over. Never fails: if matchmaking
    /// cannot be reached or produces nothing usable, the tunnels are ranked locally instead and
    /// <see cref="MatchmakingResult.Agreed"/> reports that.
    /// </summary>
    public async Task<MatchmakingResult> RunAsync(CancellationToken cancellationToken)
    {
        var matchmakingTunnels = _tunnelHandler.MatchmakingTunnels;

        if (!ENABLED || matchmakingTunnels.Count == 0)
            return RankLocally("no matchmaking servers available");

        // Matchmaking servers only relay between registered clients.
        _tunnelHandler.SendRegistrationToTunnels(_localPlayer.Id, matchmakingTunnels);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(EXCHANGE_TIMEOUT_MS);

        try
        {
            return _isDecider
                ? await RunAsDeciderAsync(matchmakingTunnels, cts.Token, cancellationToken)
                : await RunAsNonDeciderAsync(matchmakingTunnels, cts.Token, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return RankLocally($"the exchange failed ({ex.Message})");
        }
    }

    /// <summary>
    /// The decider's half: wait for the peer's advertised list, rank the tunnels both sides can
    /// reach, and publish the result.
    /// </summary>
    private async Task<MatchmakingResult> RunAsDeciderAsync(
        List<CnCNetTunnel> matchmakingTunnels, CancellationToken exchangeToken, CancellationToken negotiationToken)
    {
        try
        {
            await _peerTunnelListReceived.Task.WaitAsync(exchangeToken);
        }
        catch (OperationCanceledException) when (!negotiationToken.IsCancellationRequested)
        {
            // Timed out. A partial list is still worth ranking, so fall through.
        }

        var (peerTunnels, peerNonce) = _peerTunnelList.GetEntries();
        if (peerTunnels.Count == 0)
            return RankLocally($"no tunnel list arrived within {EXCHANGE_TIMEOUT_MS}ms");

        var shortlist = TunnelShortlist.Select(
            _candidatePool, peerTunnels, CANDIDATE_COUNT, CAPACITY_THRESHOLD, DIVERSITY_SLOTS,
            HANDSHAKE_FAILURE_THRESHOLD);

        if (shortlist.Count == 0)
            return RankLocally($"they advertised {peerTunnels.Count} tunnel(s) but none are usable by both of us");

        Logger.Log($"MatchmakingExchange: Succeeded with {_remotePlayer.Name}: they advertised {peerTunnels.Count} tunnel(s), shortlisted {shortlist.Count} from both lists");

        // Published once here; TryHandlePacket resends it for as long as the peer keeps
        // advertising, so a lost shortlist is retried on demand rather than on a schedule. It
        // carries the nonce of the list it was ranked from, so the peer can tell it apart from a
        // shortlist left over from an earlier round.
        var payload = TunnelShortlist.EncodeShortlist(shortlist, peerNonce);
        _sentShortlistNonce = peerNonce;
        _sentShortlistPayload = payload;

        foreach (var matchmakingTunnel in matchmakingTunnels)
        {
            _tunnelHandler.SendPacket(matchmakingTunnel, _localPlayer.Id, _remotePlayer.Id,
                TunnelPacketType.TunnelSet, payload);
        }

        return new MatchmakingResult(shortlist, agreed: true);
    }

    /// <summary>
    /// The non-decider's half: advertise the local tunnel list until the decider's shortlist comes
    /// back, then adopt it.
    /// </summary>
    private async Task<MatchmakingResult> RunAsNonDeciderAsync(
        List<CnCNetTunnel> matchmakingTunnels, CancellationToken exchangeToken, CancellationToken negotiationToken)
    {
        // Near-capacity tunnels are advertised too. Applying the capacity rule is the decider's
        // job, since it is the side that can also fall back to ignoring it when the filtered
        // ranking comes up empty; filtering here would hide those tunnels from that fallback.
        var advertisedTunnels = _candidatePool
            .Where(t => !t.IsMatchmaking && !t.IsDirect)
            .ToList();

        var chunks = TunnelShortlist.EncodeTunnelList(advertisedTunnels, HANDSHAKE_FAILURE_THRESHOLD, _nonce);
        List<uint>? shortlistKeys = null;

        try
        {
            // Repeat until the shortlist comes back. The two peers start negotiating off separate
            // IRC events and can be seconds apart, so this doubles as the wait for the decider to
            // show up and register.
            while (!exchangeToken.IsCancellationRequested)
            {
                foreach (var matchmakingTunnel in matchmakingTunnels)
                {
                    foreach (var chunk in chunks)
                    {
                        _tunnelHandler.SendPacket(matchmakingTunnel, _localPlayer.Id, _remotePlayer.Id,
                            TunnelPacketType.TunnelList, chunk);
                    }
                }

                var completed = await Task.WhenAny(
                    _shortlistReceived.Task,
                    Task.Delay(RETRY_INTERVAL_MS, exchangeToken));

                if (completed == _shortlistReceived.Task)
                {
                    shortlistKeys = await _shortlistReceived.Task;
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (!negotiationToken.IsCancellationRequested)
        {
            // The exchange timed out; fall through to the local ranking.
        }

        if (shortlistKeys == null)
            return RankLocally($"no shortlist arrived within {EXCHANGE_TIMEOUT_MS}ms");

        var shortlist = ResolveShortlist(shortlistKeys);

        // Only reachable if a master list refresh landed mid-exchange and dropped the tunnels we
        // had advertised.
        if (shortlist.Count == 0)
            return RankLocally($"none of the {shortlistKeys.Count} tunnel(s) they chose are in our tunnel list");

        Logger.Log($"MatchmakingExchange: Succeeded with {_remotePlayer.Name}: adopted their shortlist of {shortlist.Count} tunnel(s) after advertising {advertisedTunnels.Count}");
        return new MatchmakingResult(shortlist, agreed: true);
    }

    /// <summary>
    /// Maps the decider's shortlist keys back onto local tunnel instances, preserving the
    /// decider's preference order and skipping keys this client cannot resolve.
    /// </summary>
    private List<CnCNetTunnel> ResolveShortlist(List<uint> shortlistKeys)
    {
        var tunnelsByKey = new Dictionary<uint, CnCNetTunnel>();
        foreach (var tunnel in _candidatePool)
            tunnelsByKey[TunnelShortlist.GetKey(tunnel)] = tunnel;

        var shortlist = new List<CnCNetTunnel>(shortlistKeys.Count);
        foreach (uint key in shortlistKeys)
        {
            if (tunnelsByKey.TryGetValue(key, out var tunnel) && !shortlist.Contains(tunnel))
                shortlist.Add(tunnel);
        }

        return shortlist;
    }

    /// <summary>
    /// The universal fallback: a shortlist built without the peer's help. Partly ranked on local
    /// latency, partly chosen by a hash both peers compute identically from the pair's IDs — so
    /// even a pair whose local rankings share no tunnels at all (opposite sides of the world) is
    /// guaranteed common candidates, and one mutually reachable tunnel is all negotiation needs.
    /// </summary>
    private MatchmakingResult RankLocally(string reason)
    {
        Logger.Log($"MatchmakingExchange: Ranking a shortlist locally for {_remotePlayer.Name}: {reason}");
        return new MatchmakingResult(
            TunnelShortlist.SelectLocalOnly(
                _candidatePool, FALLBACK_COUNT, CAPACITY_THRESHOLD, HANDSHAKE_FAILURE_THRESHOLD,
                pairSeed: _localPlayer.Id ^ _remotePlayer.Id, FALLBACK_DETERMINISTIC_SLOTS),
            agreed: false);
    }

    /// <summary>
    /// Releases whichever half of the exchange is still waiting. The negotiator must cancel its
    /// own token first: both waits tell a timeout from a teardown by testing that token in an
    /// exception filter.
    /// </summary>
    public void Dispose()
    {
        _peerTunnelListReceived.TrySetCanceled();
        _shortlistReceived.TrySetCanceled();
    }
}
