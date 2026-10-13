#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// One tunnel as a peer advertised it: their latency to it, and the flags they attached.
/// </summary>
public readonly struct PeerTunnelEntry
{
    public PeerTunnelEntry(byte ping, byte flags)
    {
        Ping = ping;
        Flags = flags;
    }

    /// <summary>The peer's latency in ms, or <see cref="TunnelShortlist.UNKNOWN_PING"/>.</summary>
    public byte Ping { get; }

    public byte Flags { get; }

    /// <summary>Whether the peer has this tunnel down as one that does not relay for them.</summary>
    public bool IsHandshakeSuspect => (Flags & TunnelShortlist.ENTRY_FLAG_HANDSHAKE_SUSPECT) != 0;
}

/// <summary>
/// The wire format and ranking used by the matchmaking phase: peers advertise every relay tunnel
/// they know together with its measured latency, and the decider intersects the two lists to pick
/// the handful of tunnels the pair will actually negotiate over.
/// </summary>
/// <remarks>
/// Tunnels are identified on the wire by a 32-bit hash of <c>address:port</c> rather than by index
/// into the master list: the two clients refresh that list independently, so any positional
/// encoding would silently mismatch.
/// </remarks>
public static class TunnelShortlist
{
    /// <summary>
    /// Format version leading every matchmaking payload. Bump it for any change to the encodings
    /// below; a client that does not recognise the value ignores the packet and ranks locally
    /// instead of misreading a layout it was never written for.
    /// </summary>
    public const byte FORMAT_VERSION = 1;

    /// <summary>Ping byte reserved for "this side has no measurement".</summary>
    public const byte UNKNOWN_PING = 255;

    /// <summary>Highest latency (ms) representable on the wire; anything worse is clamped to it.</summary>
    public const int MAX_ENCODABLE_PING = 254;

    /// <summary>
    /// Entry flag: the advertiser has this tunnel down as one that does not relay. Kept separate
    /// from <see cref="UNKNOWN_PING"/> because "could not measure" and "does not work" are ranked
    /// differently.
    /// </summary>
    public const byte ENTRY_FLAG_HANDSHAKE_SUSPECT = 0x01;

    /// <summary>Bytes per advertised tunnel: 4 for the key, 1 for the ping, 1 for flags.</summary>
    internal const int ENTRY_SIZE = 6;

    /// <summary>
    /// Header on each list chunk: format version, 4-byte exchange nonce, chunk index, chunk count.
    /// </summary>
    internal const int CHUNK_HEADER_SIZE = 7;

    /// <summary>
    /// Header on a shortlist payload: format version plus the nonce of the list it answers.
    /// </summary>
    internal const int SHORTLIST_HEADER_SIZE = 5;

    /// <summary>
    /// Entries per chunk, sized so a full chunk is 742 bytes on the wire once the tunnel header,
    /// magic and packet type are added. The headroom under the MTU is deliberate: a matchmaking
    /// server silently drops anything above its own relay limit, which operators can set as low as
    /// 64 bytes and the client cannot read. An extra chunk costs one datagram, so it is the cheap
    /// side to err on. Recheck if <see cref="ENTRY_SIZE"/> or <see cref="CHUNK_HEADER_SIZE"/> grows.
    /// </summary>
    private const int MAX_ENTRIES_PER_CHUNK = 120;

    /// <summary>
    /// A tunnel's identity on the wire: FNV-1a over <c>address:port</c>. Both peers derive it from
    /// their own master list copy, so it must depend on nothing else.
    /// </summary>
    /// <remarks>
    /// Hashed ordinally, without case folding, so it agrees with
    /// <see cref="CnCNetTunnel.Equals(CnCNetTunnel)"/> — the same tunnel is looked up by key here
    /// and by instance in the negotiator's result dictionary. A 32-bit collision is possible in
    /// principle and costs one misresolved candidate, not a failed negotiation.
    /// </remarks>
    public static uint GetKey(string address, int port)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        string text = $"{address}:{port}";
        uint hash = offsetBasis;

        foreach (byte b in Encoding.UTF8.GetBytes(text))
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }

    public static uint GetKey(CnCNetTunnel tunnel) => GetKey(tunnel.Address, tunnel.Port);

    /// <summary>
    /// Encodes the local tunnel list into one or more chunk payloads. Tunnels with no ping
    /// measurement are advertised too: the peer needs to know which tunnels we can reach at all.
    /// </summary>
    /// <param name="handshakeFailureThreshold">
    /// Consecutive failures after which a tunnel is advertised as a suspect.
    /// </param>
    /// <param name="nonce">
    /// Identifies the exchange this list belongs to. The decider echoes it in its answer, which is
    /// how a shortlist from a torn-down round is told from one answering this list.
    /// </param>
    public static List<byte[]> EncodeTunnelList(
        IReadOnlyList<CnCNetTunnel> tunnels, int handshakeFailureThreshold, uint nonce)
    {
        int chunkCount = Math.Max(1, (tunnels.Count + MAX_ENTRIES_PER_CHUNK - 1) / MAX_ENTRIES_PER_CHUNK);

        // The chunk index and count are single bytes; drop the tail rather than wrap around.
        if (chunkCount > byte.MaxValue)
            chunkCount = byte.MaxValue;

        var chunks = new List<byte[]>(chunkCount);

        for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            int offset = chunkIndex * MAX_ENTRIES_PER_CHUNK;
            int entryCount = Math.Min(MAX_ENTRIES_PER_CHUNK, tunnels.Count - offset);
            if (entryCount < 0)
                entryCount = 0;

            var payload = new byte[CHUNK_HEADER_SIZE + (entryCount * ENTRY_SIZE)];
            payload[0] = FORMAT_VERSION;
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(1), nonce);
            payload[5] = (byte)chunkIndex;
            payload[6] = (byte)chunkCount;

            for (int i = 0; i < entryCount; i++)
            {
                var tunnel = tunnels[offset + i];
                int entryOffset = CHUNK_HEADER_SIZE + (i * ENTRY_SIZE);

                BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(entryOffset), GetKey(tunnel));
                payload[entryOffset + 4] = EncodePing(tunnel.Ping);
                payload[entryOffset + 5] = tunnel.IsHandshakeSuspect(handshakeFailureThreshold)
                    ? ENTRY_FLAG_HANDSHAKE_SUSPECT
                    : (byte)0;
            }

            chunks.Add(payload);
        }

        return chunks;
    }

    private static byte EncodePing(PingValue ping)
    {
        if (ping.IsUnknown())
            return UNKNOWN_PING;

        return (byte)Math.Min(ping.Milliseconds, MAX_ENCODABLE_PING);
    }

    /// <summary>
    /// Encodes an agreed shortlist as a sequence of tunnel keys in preference order, behind the
    /// nonce of the advertised list it answers.
    /// </summary>
    public static byte[] EncodeShortlist(IReadOnlyList<CnCNetTunnel> tunnels, uint nonce)
    {
        var payload = new byte[SHORTLIST_HEADER_SIZE + (tunnels.Count * sizeof(uint))];
        payload[0] = FORMAT_VERSION;
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(1), nonce);

        for (int i = 0; i < tunnels.Count; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(
                payload.AsSpan(SHORTLIST_HEADER_SIZE + (i * sizeof(uint))), GetKey(tunnels[i]));
        }

        return payload;
    }

    /// <summary>
    /// Reads a shortlist payload. Returns false for an empty or unrecognised one, which the caller
    /// must treat as "no shortlist yet" rather than "an empty shortlist".
    /// </summary>
    /// <param name="nonce">The advertised list this shortlist answers; only meaningful when true is returned.</param>
    public static bool TryDecodeShortlist(ReadOnlyMemory<byte> payload, out List<uint> keys, out uint nonce)
    {
        var span = payload.Span;
        keys = [];
        nonce = 0;

        if (span.Length < SHORTLIST_HEADER_SIZE + sizeof(uint) || span[0] != FORMAT_VERSION)
            return false;

        nonce = BinaryPrimitives.ReadUInt32LittleEndian(span[1..]);

        for (int i = SHORTLIST_HEADER_SIZE; i + sizeof(uint) <= span.Length; i += sizeof(uint))
            keys.Add(BinaryPrimitives.ReadUInt32LittleEndian(span[i..]));

        return keys.Count > 0;
    }

    /// <summary>
    /// Ranks the tunnels both peers can reach and returns the best <paramref name="count"/> of them.
    /// </summary>
    /// <param name="localTunnels">The local candidate pool, already filtered of matchmaking servers.</param>
    /// <param name="peerTunnels">The peer's advertised tunnels, keyed by tunnel key.</param>
    /// <param name="count">How many tunnels to shortlist.</param>
    /// <param name="capacityThreshold">Occupancy fraction at which a tunnel stops being eligible.</param>
    /// <remarks>
    /// Only tunnels the peer also advertised are eligible, since a key the peer cannot resolve is a
    /// candidate it would drop. Ranking is on the sum of the two sides' latencies, the closest
    /// predictor of relay round-trip time available before anything is measured through the tunnel.
    /// Where only one side measured, that figure is doubled and competes on the same scale, rather
    /// than the tunnel being ranked below everything measured: the shortlist has few slots, and a
    /// player who merely lost an ICMP probe (or whose network blocks ICMP) would otherwise lose
    /// their best tunnel to servers several times slower. Only a tunnel neither side could measure
    /// has nothing to rank on and sorts last.
    /// </remarks>
    /// <param name="diversitySlots">
    /// How many of the <paramref name="count"/> slots to reserve for tunnels outside the top
    /// pick's country. See <see cref="TakeWithDiversity"/>.
    /// </param>
    /// <param name="handshakeFailureThreshold">
    /// Consecutive failures after which a tunnel is ranked below everything that has not failed.
    /// See <see cref="CnCNetTunnel.IsHandshakeSuspect"/>.
    /// </param>
    public static List<CnCNetTunnel> Select(
        IReadOnlyList<CnCNetTunnel> localTunnels,
        IReadOnlyDictionary<uint, PeerTunnelEntry> peerTunnels,
        int count,
        double capacityThreshold,
        int diversitySlots,
        int handshakeFailureThreshold)
    {
        var shortlist = Select(localTunnels, peerTunnels, count, capacityThreshold, diversitySlots,
            handshakeFailureThreshold, applyCapacityFilter: true);

        // Capacity figures come from the master list and can be minutes stale, so if honouring
        // them leaves nothing at all, a possibly-full tunnel beats no tunnel.
        return shortlist.Count > 0
            ? shortlist
            : Select(localTunnels, peerTunnels, count, capacityThreshold, diversitySlots,
                handshakeFailureThreshold, applyCapacityFilter: false);
    }

    private static List<CnCNetTunnel> Select(
        IReadOnlyList<CnCNetTunnel> localTunnels,
        IReadOnlyDictionary<uint, PeerTunnelEntry> peerTunnels,
        int count,
        double capacityThreshold,
        int diversitySlots,
        int handshakeFailureThreshold,
        bool applyCapacityFilter)
    {
        var ranked = new List<(CnCNetTunnel Tunnel, int Suspect, int Unmeasured, int Score, int Estimated, uint Key)>();

        foreach (var tunnel in localTunnels)
        {
            if (tunnel.IsMatchmaking || tunnel.IsDirect)
                continue;

            if (applyCapacityFilter && !tunnel.HasSpareCapacity(capacityThreshold))
                continue;

            uint key = GetKey(tunnel);
            if (!peerTunnels.TryGetValue(key, out var peerEntry))
                continue;

            bool localKnown = tunnel.Ping.IsValid();
            bool peerKnown = peerEntry.Ping != UNKNOWN_PING;

            // A one-sided measurement still ranks on latency, against the tunnels both sides
            // measured. Only a tunnel neither side could measure has nothing to rank on and falls
            // to the bottom.
            (int unmeasured, int score, int estimated) = (localKnown, peerKnown) switch
            {
                (true, true) => (0, Math.Min(tunnel.Ping.Milliseconds, MAX_ENCODABLE_PING) + peerEntry.Ping, 0),

                // One side is blind. Doubling the side that did measure assumes the two legs are
                // comparable — crude, but far better than the alternative of ignoring the only
                // latency figure available, which drops a tunnel one player merely failed to probe
                // below every tunnel that answered, however much slower those are.
                (true, false) => (0, Math.Min(tunnel.Ping.Milliseconds, MAX_ENCODABLE_PING) * 2, 1),
                (false, true) => (0, peerEntry.Ping * 2, 1),

                _ => (1, 0, 1)
            };

            // Both peers have to reach the tunnel, so one side finding it dead condemns it.
            bool suspect = tunnel.IsHandshakeSuspect(handshakeFailureThreshold) || peerEntry.IsHandshakeSuspect;

            ranked.Add((tunnel, suspect ? 1 : 0, unmeasured, score, estimated, key));
        }

        // Suspects sort below everything else regardless of latency, but stay in the list so they
        // are still picked when there is nothing better and can prove themselves again.
        var ordered = ranked
            .OrderBy(e => e.Suspect)
            .ThenBy(e => e.Unmeasured)
            .ThenBy(e => e.Score)
            // Between a measured tunnel and an estimated one of equal score, prefer the one that
            // cannot be wrong about it.
            .ThenBy(e => e.Estimated)
            .ThenBy(e => e.Key) // Stable, snapshot-independent tiebreak.
            .Select(e => e.Tunnel)
            .ToList();

        return TakeWithDiversity(ordered, count, diversitySlots);
    }

    /// <summary>
    /// Takes the best <paramref name="count"/> tunnels from a ranked list, reserving
    /// <paramref name="diversitySlots"/> of them for tunnels outside the top pick's country.
    /// </summary>
    /// <remarks>
    /// Ranking on latency alone clusters the shortlist into one region, where a single bad link can
    /// take out every candidate at once. A distant tunnel will usually lose the ping comparison and
    /// never be chosen, so the reserved slots cost little. They fall back to the next best by score
    /// if too few tunnels elsewhere exist to fill them.
    /// </remarks>
    private static List<CnCNetTunnel> TakeWithDiversity(List<CnCNetTunnel> ranked, int count, int diversitySlots)
    {
        if (diversitySlots <= 0 || ranked.Count <= count)
            return ranked.Take(count).ToList();

        int scoredSlots = Math.Max(1, count - diversitySlots);
        var selected = ranked.Take(scoredSlots).ToList();

        string topCountry = selected[0].CountryCode ?? string.Empty;

        foreach (var tunnel in ranked.Skip(scoredSlots))
        {
            if (selected.Count >= count)
                break;

            if (!string.Equals(tunnel.CountryCode ?? string.Empty, topCountry, StringComparison.OrdinalIgnoreCase))
                selected.Add(tunnel);
        }

        // Not enough elsewhere to fill the reserved slots - top up by score.
        foreach (var tunnel in ranked.Skip(scoredSlots))
        {
            if (selected.Count >= count)
                break;

            if (!selected.Contains(tunnel))
                selected.Add(tunnel);
        }

        return selected;
    }

    /// <summary>
    /// Builds the shortlist each side uses when the matchmaking exchange produced nothing. The
    /// list is part locally ranked, part deterministic: the locally ranked slots depend on where
    /// this player sits, while the deterministic slots depend only on the pair's IDs and the
    /// master list — the peer computes the very same ones, so even two players whose local
    /// rankings share nothing (opposite sides of the world) are guaranteed common candidates.
    /// </summary>
    /// <param name="pairSeed">
    /// A value both peers derive identically without communicating (XOR of the two player IDs).
    /// Seeding per pair matters: a matchmaking outage makes every active pair fall back at once,
    /// and an unseeded ordering would send them all to the same few servers.
    /// </param>
    /// <param name="deterministicSlots">
    /// How many of the <paramref name="count"/> slots are filled by pair-seeded hash instead of
    /// local ranking. 0 restores a purely local ranking.
    /// </param>
    public static List<CnCNetTunnel> SelectLocalOnly(
        IReadOnlyList<CnCNetTunnel> localTunnels,
        int count,
        double capacityThreshold,
        int handshakeFailureThreshold,
        uint pairSeed,
        int deterministicSlots)
    {
        var shortlist = SelectLocalOnly(localTunnels, count, capacityThreshold,
            handshakeFailureThreshold, pairSeed, deterministicSlots, applyCapacityFilter: true);

        return shortlist.Count > 0
            ? shortlist
            : SelectLocalOnly(localTunnels, count, capacityThreshold,
                handshakeFailureThreshold, pairSeed, deterministicSlots, applyCapacityFilter: false);
    }

    private static List<CnCNetTunnel> SelectLocalOnly(
        IReadOnlyList<CnCNetTunnel> localTunnels,
        int count,
        double capacityThreshold,
        int handshakeFailureThreshold,
        uint pairSeed,
        int deterministicSlots,
        bool applyCapacityFilter)
    {
        var eligible = localTunnels
            .Where(t => !t.IsMatchmaking && !t.IsDirect &&
                (!applyCapacityFilter || t.HasSpareCapacity(capacityThreshold)))
            .ToList();

        var rankedLocally = eligible
            .OrderBy(t => t.IsHandshakeSuspect(handshakeFailureThreshold) ? 1 : 0)
            .ThenBy(t => t.Ping.IsValid() ? 0 : 1)
            .ThenBy(t => t.Ping.IsValid() ? t.Ping.Milliseconds : 0)
            // Unmeasured tunnels fall back to geographic distance, the only other distance signal
            // the master list carries.
            .ThenBy(t => t.Ping.IsValid() ? 0 : t.Distance)
            .ThenBy(t => GetKey(t))
            .ToList();

        int localSlots = Math.Max(0, count - Math.Max(0, deterministicSlots));
        var selected = rankedLocally.Take(localSlots).ToList();

        // The deterministic slots: official tunnels only, because those are in every client's
        // pool regardless of the PingUnofficialCnCNetTunnels setting, and ordered by nothing but
        // the pair-seeded hash — mixing in local knowledge (ping, distance, handshake suspicion)
        // would desynchronise the two sides' picks, which is the one property these slots exist
        // to provide.
        foreach (var tunnel in eligible
            .Where(t => t.Official)
            .OrderBy(t => MixKey(GetKey(t), pairSeed))
            .ThenBy(t => GetKey(t)))
        {
            if (selected.Count >= count)
                break;

            if (!selected.Contains(tunnel))
                selected.Add(tunnel);
        }

        // Too few officials to fill the deterministic slots - top up by local rank.
        foreach (var tunnel in rankedLocally)
        {
            if (selected.Count >= count)
                break;

            if (!selected.Contains(tunnel))
                selected.Add(tunnel);
        }

        return selected;
    }

    /// <summary>
    /// Mixes a tunnel key with a pair seed into a well-distributed ordering value
    /// (murmur3-style finalizer). A plain XOR would preserve too much key structure to spread
    /// pairs evenly across the tunnel list.
    /// </summary>
    private static uint MixKey(uint key, uint seed)
    {
        uint h = key ^ seed;
        h ^= h >> 16;
        h *= 0x85EBCA6B;
        h ^= h >> 13;
        h *= 0xC2B2AE35;
        h ^= h >> 16;
        return h;
    }
}

/// <summary>
/// Reassembles a peer's chunked tunnel list. Chunks are sent over several matchmaking servers and
/// repeated until answered, so they can arrive out of order, duplicated, or interleaved with a
/// resend. They arrive on the communicator's receive thread while the negotiation task reads the
/// assembled list, so every member takes <c>_chunksLock</c>.
/// </summary>
public sealed class TunnelListAssembler
{
    private readonly Dictionary<int, Dictionary<uint, PeerTunnelEntry>> _chunks = [];
    private readonly object _chunksLock = new();
    private int _chunkCount = -1;
    private uint _nonce;
    private bool _hasNonce;

    /// <summary>True once every chunk of the peer's list has arrived.</summary>
    public bool IsComplete
    {
        get
        {
            lock (_chunksLock)
                return IsCompleteUnlocked;
        }
    }

    private bool IsCompleteUnlocked => _chunkCount > 0 && _chunks.Count == _chunkCount;

    /// <summary>
    /// Adds a received chunk. Returns true if the list is complete as a result.
    /// </summary>
    public bool AddChunk(ReadOnlyMemory<byte> payload)
    {
        var span = payload.Span;
        if (span.Length < TunnelShortlist.CHUNK_HEADER_SIZE || span[0] != TunnelShortlist.FORMAT_VERSION)
            return IsComplete;

        uint nonce = BinaryPrimitives.ReadUInt32LittleEndian(span[1..]);
        int chunkIndex = span[5];
        int chunkCount = span[6];

        if (chunkCount == 0 || chunkIndex >= chunkCount)
            return IsComplete;

        var entries = new Dictionary<uint, PeerTunnelEntry>();
        for (int offset = TunnelShortlist.CHUNK_HEADER_SIZE;
             offset + TunnelShortlist.ENTRY_SIZE <= span.Length;
             offset += TunnelShortlist.ENTRY_SIZE)
        {
            uint key = BinaryPrimitives.ReadUInt32LittleEndian(span[offset..]);
            entries[key] = new PeerTunnelEntry(span[offset + 4], span[offset + 5]);
        }

        lock (_chunksLock)
        {
            // A chunk from a different exchange (or one that disagrees about the chunk count) is a
            // different list, so start over rather than blending two generations of it. The peer
            // restarting its negotiation is exactly this case: its new list supersedes the old.
            if (!_hasNonce || _nonce != nonce || _chunkCount != chunkCount)
            {
                _chunks.Clear();
                _chunkCount = chunkCount;
                _nonce = nonce;
                _hasNonce = true;
            }

            _chunks[chunkIndex] = entries;
            return IsCompleteUnlocked;
        }
    }

    /// <summary>
    /// The exchange the currently held chunks belong to, or 0 if nothing has arrived. Separate
    /// from <see cref="GetEntries"/> so the receive thread can check which exchange a resend
    /// request belongs to without merging the whole list.
    /// </summary>
    public uint Nonce
    {
        get
        {
            lock (_chunksLock)
                return _hasNonce ? _nonce : 0;
        }
    }

    /// <summary>
    /// The peer's advertised tunnels, keyed by tunnel key, together with the nonce of the exchange
    /// they came from — which the decider must echo so the peer can tell this answer from a stale
    /// one. Safe to read before the list is complete; a partial list still beats none if the
    /// exchange times out.
    /// </summary>
    public (Dictionary<uint, PeerTunnelEntry> Entries, uint Nonce) GetEntries()
    {
        var merged = new Dictionary<uint, PeerTunnelEntry>();

        lock (_chunksLock)
        {
            foreach (var chunk in _chunks.Values)
            {
                foreach (var entry in chunk)
                    merged[entry.Key] = entry.Value;
            }

            return (merged, _nonce);
        }
    }
}
