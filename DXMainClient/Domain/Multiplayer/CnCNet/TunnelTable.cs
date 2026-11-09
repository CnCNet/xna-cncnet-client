#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// One tunnel as a player measured it: the V3 UDP ping from their side, and what else they know
/// about it.
/// </summary>
public readonly struct TunnelTableEntry
{
    public TunnelTableEntry(uint key, ushort pingMs, byte flags)
    {
        Key = key;
        PingMs = pingMs;
        Flags = flags;
    }

    public uint Key { get; }

    /// <summary>The player's latency to the tunnel in ms, or <see cref="TunnelTable.UNKNOWN_PING"/>.</summary>
    public ushort PingMs { get; }

    public byte Flags { get; }

    public bool HasPing => PingMs != TunnelTable.UNKNOWN_PING;

    /// <summary>The player has this tunnel down as one that does not relay for them.</summary>
    public bool IsSuspect => (Flags & TunnelTable.FLAG_SUSPECT) != 0;

    /// <summary>The player held a session on this tunnel within the last minute. See <see cref="CnCNetTunnel.WasSessionUsedWithin"/>.</summary>
    public bool IsRecentlyUsed => (Flags & TunnelTable.FLAG_RECENTLY_USED) != 0;

    /// <summary>The player's copy of the master list shows the tunnel nearly full.</summary>
    public bool IsNearCapacity => (Flags & TunnelTable.FLAG_NEAR_CAPACITY) != 0;
}

/// <summary>
/// Options for <see cref="TunnelTable.Rank"/>.
/// </summary>
public sealed class TunnelRankOptions
{
    public int Count { get; init; }

    public double CapacityThreshold { get; init; }

    public int HandshakeFailureThreshold { get; init; }

    /// <summary>
    /// Seeds the tie-break shuffle (XOR of the two player IDs). Without it, every pair leaving a
    /// failed tunnel would pile onto the same next-best one and fill it.
    /// </summary>
    public uint PairSeed { get; init; }

    /// <summary>Tunnels to leave out, such as the pair's current path.</summary>
    public IReadOnlyCollection<CnCNetTunnel>? Exclude { get; init; }

    /// <summary>
    /// Rank tunnels either side used in the last minute after the rest. Set for fallback lists:
    /// after a NAT rebind those tunnels keep rejecting the rebound side for up to a minute.
    /// </summary>
    public bool DeprioritiseRecentlyUsed { get; init; }

    /// <summary>
    /// An address the first tunnels should avoid sharing a network with, such as the current
    /// path's, so one outage does not take out the path and its first fallbacks together.
    /// </summary>
    public string? AvoidNetworkOf { get; init; }

    /// <summary>
    /// How many tunnels are brought up together. Each group of this size is spread over as many
    /// networks as the scores allow.
    /// </summary>
    public int GroupSize { get; init; } = 4;
}

/// <summary>
/// The wire formats and ranking for tunnel ping tables. Each side of a pair measures every relay
/// tunnel with the V3 UDP ping; the decider combines the two tables into the pair's ranked list.
/// </summary>
/// <remarks>
/// Tunnels are identified by a hash of <c>address:port</c> rather than by position in the master
/// list: the two clients refresh that list independently, so any positional encoding would
/// silently mismatch.
/// </remarks>
public static class TunnelTable
{
    /// <summary>
    /// Format version leading every binary payload. A client that does not recognise it ignores
    /// the packet rather than misreading a layout it was never written for.
    /// </summary>
    public const byte FORMAT_VERSION = 2;

    public const ushort UNKNOWN_PING = ushort.MaxValue;

    public const byte FLAG_SUSPECT = 0x01;
    public const byte FLAG_RECENTLY_USED = 0x02;
    public const byte FLAG_NEAR_CAPACITY = 0x04;

    /// <summary>Seconds within which a held session counts as recent. Current tunnel servers time sessions out after 60.</summary>
    public const double RECENT_SESSION_SECONDS = 65;

    /// <summary>Bytes per entry on UDP: 4 for the key, 2 for the ping, 1 for flags.</summary>
    internal const int ENTRY_SIZE = 7;

    /// <summary>Header on each table chunk: format version, 4-byte exchange nonce, chunk index, chunk count.</summary>
    internal const int CHUNK_HEADER_SIZE = 7;

    /// <summary>Header on a ranked list: format version plus the nonce of the table it answers.</summary>
    internal const int RANKED_HEADER_SIZE = 5;

    /// <summary>
    /// Entries per chunk, keeping a full chunk near 1000 bytes: matchmaking servers relay nothing
    /// above 1200 by default.
    /// </summary>
    private const int MAX_ENTRIES_PER_CHUNK = 140;

    /// <summary>A score added to a tunnel only one side could measure, so measured tunnels win ties.</summary>
    private const int ONE_SIDED_PENALTY_MS = 40;

    /// <summary>
    /// How far behind the best remaining tunnel (ms) a tunnel on another network may be and still
    /// be preferred within a group.
    /// </summary>
    private const int DIVERSITY_SLACK_MS = 40;

    /// <summary>Upper bound (ms) of the per-pair shuffle among near-equal tunnels.</summary>
    private const int SHUFFLE_SPREAD_MS = 8;

    /// <summary>
    /// A tunnel's identity on the wire: FNV-1a over <c>address:port</c>, hashed ordinally so it
    /// agrees with <see cref="CnCNetTunnel.Equals(CnCNetTunnel)"/>.
    /// </summary>
    public static uint GetKey(string address, int port)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        uint hash = offsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes($"{address}:{port}"))
        {
            hash ^= b;
            hash *= prime;
        }

        // 0 and 1 are reserved: no path, and the pair's direct path.
        return hash <= DIRECT_PATH_KEY ? hash + 2 : hash;
    }

    public static uint GetKey(CnCNetTunnel tunnel) => GetKey(tunnel.Address, tunnel.Port);

    /// <summary>Key reserved for "no path".</summary>
    public const uint NO_PATH_KEY = 0;

    /// <summary>
    /// Key reserved for the pair's direct path. The two sides see a direct path from opposite
    /// ends, so it has no shared address to hash.
    /// </summary>
    public const uint DIRECT_PATH_KEY = 1;

    /// <summary>The 16-bit key used by the compact IRC encoding.</summary>
    public static ushort GetShortKey(uint key) => (ushort)(key ^ (key >> 16));

    /// <summary>
    /// Builds this client's table from its tunnel list: every relay tunnel, measured or not, since
    /// the peer can still use a tunnel only one side has measured.
    /// </summary>
    public static List<TunnelTableEntry> BuildLocalTable(
        IEnumerable<CnCNetTunnel> pool, int handshakeFailureThreshold, double capacityThreshold)
    {
        var table = new List<TunnelTableEntry>();

        foreach (var tunnel in pool)
        {
            if (tunnel.IsMatchmaking || tunnel.IsDirect)
                continue;

            byte flags = 0;
            if (tunnel.IsHandshakeSuspect(handshakeFailureThreshold))
                flags |= FLAG_SUSPECT;
            if (tunnel.WasSessionUsedWithin(RECENT_SESSION_SECONDS))
                flags |= FLAG_RECENTLY_USED;
            if (!tunnel.HasSpareCapacity(capacityThreshold))
                flags |= FLAG_NEAR_CAPACITY;

            table.Add(new TunnelTableEntry(GetKey(tunnel), EncodePing(tunnel.Ping), flags));
        }

        return table;
    }

    private static ushort EncodePing(PingValue ping)
        => ping.IsValid() ? (ushort)Math.Min(ping.Milliseconds, UNKNOWN_PING - 1) : UNKNOWN_PING;

    #region Binary encoding (matchmaking servers and heartbeats)

    /// <summary>Writes entries back to back, without a header. Used inside heartbeat sections.</summary>
    public static byte[] EncodeEntries(IReadOnlyList<TunnelTableEntry> entries)
    {
        var buffer = new byte[entries.Count * ENTRY_SIZE];
        for (int i = 0; i < entries.Count; i++)
            WriteEntry(buffer.AsSpan(i * ENTRY_SIZE), entries[i]);

        return buffer;
    }

    public static Dictionary<uint, TunnelTableEntry> DecodeEntries(ReadOnlySpan<byte> span)
    {
        var entries = new Dictionary<uint, TunnelTableEntry>();
        for (int offset = 0; offset + ENTRY_SIZE <= span.Length; offset += ENTRY_SIZE)
        {
            var entry = ReadEntry(span[offset..]);
            entries[entry.Key] = entry;
        }

        return entries;
    }

    private static void WriteEntry(Span<byte> span, TunnelTableEntry entry)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span, entry.Key);
        BinaryPrimitives.WriteUInt16LittleEndian(span[4..], entry.PingMs);
        span[6] = entry.Flags;
    }

    private static TunnelTableEntry ReadEntry(ReadOnlySpan<byte> span)
        => new(BinaryPrimitives.ReadUInt32LittleEndian(span),
            BinaryPrimitives.ReadUInt16LittleEndian(span[4..]),
            span[6]);

    /// <summary>Splits a table into chunk payloads for the matchmaking servers.</summary>
    public static List<byte[]> EncodeTableChunks(IReadOnlyList<TunnelTableEntry> entries, uint nonce)
    {
        int chunkCount = Math.Min(byte.MaxValue, Math.Max(1, (entries.Count + MAX_ENTRIES_PER_CHUNK - 1) / MAX_ENTRIES_PER_CHUNK));
        var chunks = new List<byte[]>(chunkCount);

        for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            int offset = chunkIndex * MAX_ENTRIES_PER_CHUNK;
            int entryCount = Math.Max(0, Math.Min(MAX_ENTRIES_PER_CHUNK, entries.Count - offset));

            var payload = new byte[CHUNK_HEADER_SIZE + (entryCount * ENTRY_SIZE)];
            payload[0] = FORMAT_VERSION;
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(1), nonce);
            payload[5] = (byte)chunkIndex;
            payload[6] = (byte)chunkCount;

            for (int i = 0; i < entryCount; i++)
                WriteEntry(payload.AsSpan(CHUNK_HEADER_SIZE + (i * ENTRY_SIZE)), entries[offset + i]);

            chunks.Add(payload);
        }

        return chunks;
    }

    /// <summary>Encodes the pair's ranked list, behind the nonce of the table it answers.</summary>
    public static byte[] EncodeRankedList(IReadOnlyList<CnCNetTunnel> tunnels, uint nonce)
    {
        var payload = new byte[RANKED_HEADER_SIZE + (tunnels.Count * sizeof(uint))];
        payload[0] = FORMAT_VERSION;
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(1), nonce);

        for (int i = 0; i < tunnels.Count; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(RANKED_HEADER_SIZE + (i * sizeof(uint))), GetKey(tunnels[i]));

        return payload;
    }

    /// <summary>
    /// Reads a ranked list. Returns false for an empty or unrecognised one, which the caller must
    /// treat as "no list yet" rather than "an empty list".
    /// </summary>
    public static bool TryDecodeRankedList(ReadOnlySpan<byte> span, out List<uint> keys, out uint nonce)
    {
        keys = [];
        nonce = 0;

        if (span.Length < RANKED_HEADER_SIZE + sizeof(uint) || span[0] != FORMAT_VERSION)
            return false;

        nonce = BinaryPrimitives.ReadUInt32LittleEndian(span[1..]);
        for (int i = RANKED_HEADER_SIZE; i + sizeof(uint) <= span.Length; i += sizeof(uint))
            keys.Add(BinaryPrimitives.ReadUInt32LittleEndian(span[i..]));

        return keys.Count > 0;
    }

    #endregion

    #region Compact encoding (IRC)

    /// <summary>
    /// Most entries a compact table carries. An IRC line is limited to 512 bytes including the
    /// server's prefix and the channel name, which leaves room for about this many base64 entries.
    /// </summary>
    private const int MAX_COMPACT_ENTRIES = 85;

    /// <summary>
    /// Encodes a table for IRC: 2-byte short key plus 1 byte of ping in 2 ms units, base64. The
    /// best-measured entries are kept if the table is too long for one line.
    /// </summary>
    public static string EncodeTableCompact(IReadOnlyList<TunnelTableEntry> entries)
    {
        var chosen = entries
            .OrderBy(e => e.HasPing ? 0 : 1)
            .ThenBy(e => e.PingMs)
            .Take(MAX_COMPACT_ENTRIES)
            .ToList();

        var buffer = new byte[chosen.Count * 3];
        for (int i = 0; i < chosen.Count; i++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(i * 3), GetShortKey(chosen[i].Key));
            buffer[(i * 3) + 2] = chosen[i].HasPing ? (byte)Math.Min(254, (chosen[i].PingMs + 1) / 2) : (byte)255;
        }

        return Convert.ToBase64String(buffer);
    }

    /// <summary>
    /// Decodes a compact table against the local tunnel list. Entries whose short key matches no
    /// local tunnel, or more than one, are dropped.
    /// </summary>
    public static Dictionary<uint, TunnelTableEntry>? DecodeTableCompact(string text, IReadOnlyList<CnCNetTunnel> pool)
    {
        byte[] buffer;
        try
        {
            buffer = Convert.FromBase64String(text);
        }
        catch (FormatException)
        {
            return null;
        }

        var byShortKey = BuildShortKeyIndex(pool);
        var entries = new Dictionary<uint, TunnelTableEntry>();

        for (int offset = 0; offset + 3 <= buffer.Length; offset += 3)
        {
            ushort shortKey = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset));
            if (!byShortKey.TryGetValue(shortKey, out var tunnel) || tunnel == null)
                continue;

            byte ping = buffer[offset + 2];
            uint key = GetKey(tunnel);
            entries[key] = new TunnelTableEntry(key, ping == 255 ? UNKNOWN_PING : (ushort)(ping * 2), 0);
        }

        return entries;
    }

    public static string EncodeRankedCompact(IReadOnlyList<CnCNetTunnel> tunnels)
    {
        var buffer = new byte[tunnels.Count * 2];
        for (int i = 0; i < tunnels.Count; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(i * 2), GetShortKey(GetKey(tunnels[i])));

        return Convert.ToBase64String(buffer);
    }

    /// <summary>Decodes a compact ranked list into full keys, skipping ambiguous or unknown entries.</summary>
    public static List<uint>? DecodeRankedCompact(string text, IReadOnlyList<CnCNetTunnel> pool)
    {
        byte[] buffer;
        try
        {
            buffer = Convert.FromBase64String(text);
        }
        catch (FormatException)
        {
            return null;
        }

        var byShortKey = BuildShortKeyIndex(pool);
        var keys = new List<uint>();

        for (int offset = 0; offset + 2 <= buffer.Length; offset += 2)
        {
            if (byShortKey.TryGetValue(BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset)), out var tunnel) && tunnel != null)
                keys.Add(GetKey(tunnel));
        }

        return keys;
    }

    /// <summary>Maps short keys to tunnels; a short key shared by two tunnels maps to null.</summary>
    private static Dictionary<ushort, CnCNetTunnel?> BuildShortKeyIndex(IReadOnlyList<CnCNetTunnel> pool)
    {
        var index = new Dictionary<ushort, CnCNetTunnel?>();
        foreach (var tunnel in pool)
        {
            ushort shortKey = GetShortKey(GetKey(tunnel));
            index[shortKey] = index.ContainsKey(shortKey) ? null : tunnel;
        }

        return index;
    }

    /// <summary>Whether a tunnel's short key is unique in the pool, so the compact encoding can name it.</summary>
    public static bool HasUniqueShortKey(CnCNetTunnel tunnel, IReadOnlyList<CnCNetTunnel> pool)
    {
        ushort shortKey = GetShortKey(GetKey(tunnel));
        return pool.Count(t => GetShortKey(GetKey(t)) == shortKey) == 1;
    }

    #endregion

    #region Ranking

    /// <summary>
    /// The predicted round trip between the two players through a tunnel: the sum of both sides'
    /// pings, which the relay round trip closely follows. Null unless both sides measured it.
    /// </summary>
    public static int? PredictPairRtt(CnCNetTunnel tunnel, IReadOnlyDictionary<uint, TunnelTableEntry> peerTable)
    {
        if (!tunnel.Ping.IsValid() || !peerTable.TryGetValue(GetKey(tunnel), out var entry) || !entry.HasPing)
            return null;

        return tunnel.Ping.Milliseconds + entry.PingMs;
    }

    /// <summary>
    /// Ranks the tunnels both sides can use and returns the best <see cref="TunnelRankOptions.Count"/>.
    /// </summary>
    /// <remarks>
    /// Only tunnels in both tables are eligible. A tunnel neither side reached is left out. One
    /// only one side reached competes on double that side's ping plus a penalty, since the other
    /// side's ping may simply have been rate limited. Suspect tunnels, and with
    /// <see cref="TunnelRankOptions.DeprioritiseRecentlyUsed"/> recently used ones, sort after
    /// the rest. Tunnels the master list shows as nearly full are left out unless nothing else
    /// remains.
    /// </remarks>
    public static List<CnCNetTunnel> Rank(
        IReadOnlyList<CnCNetTunnel> pool,
        IReadOnlyDictionary<uint, TunnelTableEntry> peerTable,
        TunnelRankOptions options)
    {
        var ranked = Rank(pool, peerTable, options, applyCapacityFilter: true);
        return ranked.Count > 0 ? ranked : Rank(pool, peerTable, options, applyCapacityFilter: false);
    }

    private static List<CnCNetTunnel> Rank(
        IReadOnlyList<CnCNetTunnel> pool,
        IReadOnlyDictionary<uint, TunnelTableEntry> peerTable,
        TunnelRankOptions options,
        bool applyCapacityFilter)
    {
        var scored = new List<(CnCNetTunnel Tunnel, int Tier, int Score)>();

        foreach (var tunnel in pool)
        {
            if (tunnel.IsMatchmaking || tunnel.IsDirect || (options.Exclude?.Contains(tunnel) ?? false))
                continue;

            uint key = GetKey(tunnel);
            if (!peerTable.TryGetValue(key, out var peer))
                continue;

            if (applyCapacityFilter && (!tunnel.HasSpareCapacity(options.CapacityThreshold) || peer.IsNearCapacity))
                continue;

            bool localKnown = tunnel.Ping.IsValid();
            int score;
            if (localKnown && peer.HasPing)
                score = tunnel.Ping.Milliseconds + peer.PingMs;
            else if (localKnown)
                score = (tunnel.Ping.Milliseconds * 2) + ONE_SIDED_PENALTY_MS;
            else if (peer.HasPing)
                score = (peer.PingMs * 2) + ONE_SIDED_PENALTY_MS;
            else
                continue;

            int tier = 0;
            if (tunnel.IsHandshakeSuspect(options.HandshakeFailureThreshold) || peer.IsSuspect)
                tier += 2;
            if (options.DeprioritiseRecentlyUsed && (peer.IsRecentlyUsed || tunnel.WasSessionUsedWithin(RECENT_SESSION_SECONDS)))
                tier += 1;

            score += (int)(MixKey(key, options.PairSeed) % SHUFFLE_SPREAD_MS);
            scored.Add((tunnel, tier, score));
        }

        var ordered = scored
            .OrderBy(e => e.Tier)
            .ThenBy(e => e.Score)
            .ThenBy(e => GetKey(e.Tunnel))
            .ToList();

        return TakeDiverse(ordered, options);
    }

    /// <summary>
    /// Takes tunnels in score order, but within each bring-up group prefers a tunnel on a network
    /// not already in the group when it is close enough to the best remaining one. Ranking on
    /// latency alone tends to return one provider's servers, which a single outage takes out
    /// together.
    /// </summary>
    private static List<CnCNetTunnel> TakeDiverse(List<(CnCNetTunnel Tunnel, int Tier, int Score)> ordered, TunnelRankOptions options)
    {
        var remaining = new List<(CnCNetTunnel Tunnel, int Tier, int Score)>(ordered);
        var result = new List<CnCNetTunnel>(options.Count);
        var groupNetworks = new HashSet<string>(StringComparer.Ordinal);
        int groupSize = Math.Max(1, options.GroupSize);
        string? avoid = options.AvoidNetworkOf != null ? GetNetwork(options.AvoidNetworkOf) : null;

        while (result.Count < options.Count && remaining.Count > 0)
        {
            if (result.Count % groupSize == 0)
            {
                groupNetworks.Clear();
                if (result.Count == 0 && avoid != null)
                    groupNetworks.Add(avoid);
            }

            var best = remaining[0];
            int pick = 0;

            for (int i = 0; i < remaining.Count; i++)
            {
                var candidate = remaining[i];
                if (candidate.Tier != best.Tier || candidate.Score > best.Score + DIVERSITY_SLACK_MS)
                    break;

                if (!groupNetworks.Contains(GetNetwork(candidate.Tunnel.Address)))
                {
                    pick = i;
                    break;
                }
            }

            var chosen = remaining[pick];
            remaining.RemoveAt(pick);
            result.Add(chosen.Tunnel);
            groupNetworks.Add(GetNetwork(chosen.Tunnel.Address));
        }

        return result;
    }

    /// <summary>The first two octets of an IPv4 address, a rough stand-in for "same provider".</summary>
    private static string GetNetwork(string address)
    {
        int first = address.IndexOf('.');
        int second = first >= 0 ? address.IndexOf('.', first + 1) : -1;
        return second > 0 ? address[..second] : address;
    }

    /// <summary>Murmur3-style finalizer, so the shuffle spreads pairs evenly.</summary>
    public static uint MixKey(uint key, uint seed)
    {
        uint h = key ^ seed;
        h ^= h >> 16;
        h *= 0x85EBCA6B;
        h ^= h >> 13;
        h *= 0xC2B2AE35;
        h ^= h >> 16;
        return h;
    }

    #endregion
}

/// <summary>
/// Reassembles a peer's chunked table. Chunks are sent over several matchmaking servers and
/// repeated until answered, so they can arrive out of order, duplicated, or interleaved with a
/// resend. They arrive on the communicator's receive thread while the exchange task reads the
/// assembled table, so every member takes <c>_chunksLock</c>.
/// </summary>
public sealed class TunnelTableAssembler
{
    private readonly Dictionary<int, Dictionary<uint, TunnelTableEntry>> _chunks = [];
    private readonly object _chunksLock = new();
    private int _chunkCount = -1;
    private uint _nonce;
    private bool _hasNonce;

    private bool IsCompleteUnlocked => _chunkCount > 0 && _chunks.Count == _chunkCount;

    /// <summary>Adds a received chunk. Returns true if the table is complete as a result.</summary>
    public bool AddChunk(ReadOnlySpan<byte> span)
    {
        if (span.Length < TunnelTable.CHUNK_HEADER_SIZE || span[0] != TunnelTable.FORMAT_VERSION)
            return false;

        uint nonce = BinaryPrimitives.ReadUInt32LittleEndian(span[1..]);
        int chunkIndex = span[5];
        int chunkCount = span[6];

        if (chunkCount == 0 || chunkIndex >= chunkCount)
            return false;

        var entries = TunnelTable.DecodeEntries(span[TunnelTable.CHUNK_HEADER_SIZE..]);

        lock (_chunksLock)
        {
            // A chunk from a different exchange is a different table, so start over rather than
            // blending two generations of it.
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

    /// <summary>Replaces whatever has arrived with a whole table received another way (IRC).</summary>
    public void SetWhole(Dictionary<uint, TunnelTableEntry> entries, uint nonce)
    {
        lock (_chunksLock)
        {
            _chunks.Clear();
            _chunks[0] = entries;
            _chunkCount = 1;
            _nonce = nonce;
            _hasNonce = true;
        }
    }

    /// <summary>The exchange the held chunks belong to, or 0 if nothing has arrived.</summary>
    public uint Nonce
    {
        get
        {
            lock (_chunksLock)
                return _hasNonce ? _nonce : 0;
        }
    }

    /// <summary>The peer's table so far, with the nonce of the exchange it came from.</summary>
    public (Dictionary<uint, TunnelTableEntry> Entries, uint Nonce) GetEntries()
    {
        var merged = new Dictionary<uint, TunnelTableEntry>();

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
