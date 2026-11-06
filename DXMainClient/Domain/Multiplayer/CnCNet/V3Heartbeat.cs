#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Monotonic milliseconds for link timing. Unaffected by wall-clock changes.
/// </summary>
internal static class V3Clock
{
    public static long NowMs => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
}

/// <summary>
/// A heartbeat sent on one path of a pair. Besides proving the path works, it carries what the
/// sender knows about the path, so each side can measure both directions without trusting the
/// other's clock, and the pair's shared state, so the two sides converge on the same path without
/// a separate control protocol.
/// </summary>
/// <remarks>
/// <para>Layout (little-endian), after the usual sender/receiver IDs, magic and type byte:
/// format, flags, time, echo time, hold, heard-you-ago, sent total, received total, primary key,
/// epoch, trial key, list version, session, echoed sent total, then optional sections of
/// <c>[type u8][length u16][data]</c>.</para>
///
/// <para>Round trip: each heartbeat echoes the latest peer timestamp heard on this path and how
/// long ago it arrived, so the sender subtracts the hold time from the elapsed time.</para>
///
/// <para>Loss: each side counts everything it sends and receives on the path (heartbeats, filler
/// and game data). A heartbeat carries the sender's own sent total, and echoes the receiver's
/// sent total from the last heartbeat it got together with how many of the receiver's packets it
/// had received at that same moment. Both pairs of figures are taken at one instant, so packets
/// still in flight never read as lost.</para>
/// </remarks>
public sealed class V3Heartbeat
{
    public const byte FORMAT_VERSION = 1;

    public const byte FLAG_IN_GAME = 0x01;

    /// <summary>The sender is closing this pair (leaving the lobby or the client). Failover must not chase it.</summary>
    public const byte FLAG_GOODBYE = 0x02;

    /// <summary>The sender has P2P enabled and will accept direct paths.</summary>
    public const byte FLAG_P2P = 0x04;

    /// <summary>The sender has received the peer's P2P candidates.</summary>
    public const byte FLAG_HAVE_P2P_CANDIDATES = 0x08;

    /// <summary>The sender is tearing this connection down to start a new one at once. The receiver should start over too.</summary>
    public const byte FLAG_RESTART = 0x10;

    /// <summary>
    /// Sent on the path the sender is using. Direct paths have no shared name (each side sees the
    /// other's address), so this is how the non-decider finds the exact direct path the decider
    /// chose when a player has several network adapters.
    /// </summary>
    public const byte FLAG_ON_PRIMARY = 0x20;

    public const byte SECTION_FALLBACK_LIST = 1;
    public const byte SECTION_PING_TABLE = 2;
    public const byte SECTION_P2P_CANDIDATES = 3;
    public const byte SECTION_REQUESTS = 4;

    /// <summary>Request bit: please send your ping table.</summary>
    public const byte REQUEST_PING_TABLE = 0x01;

    private const int FIXED_SIZE = 42;

    public byte Flags { get; set; }

    /// <summary>The sender's clock (ms), echoed back for the round trip.</summary>
    public uint TimeMs { get; set; }

    /// <summary>The latest receiver timestamp the sender heard on this path, or 0.</summary>
    public uint EchoTimeMs { get; set; }

    /// <summary>How long (ms) the sender held <see cref="EchoTimeMs"/> before sending.</summary>
    public ushort HoldMs { get; set; }

    /// <summary>How long ago (ms) the sender last heard anything from the receiver on this path. <see cref="ushort.MaxValue"/> = never or long ago.</summary>
    public ushort HeardYouAgoMs { get; set; } = ushort.MaxValue;

    /// <summary>Packets the sender has sent on this path, in total.</summary>
    public uint SentTotal { get; set; }

    /// <summary>
    /// Packets the sender had received from the receiver on this path when it got the heartbeat
    /// carrying <see cref="EchoSentTotal"/>.
    /// </summary>
    public uint ReceivedTotal { get; set; }

    /// <summary>The <see cref="SentTotal"/> of the latest heartbeat the sender received from the receiver on this path.</summary>
    public uint EchoSentTotal { get; set; }

    /// <summary>
    /// The decider's chosen path, or for the non-decider the path it is following. See
    /// <see cref="TunnelTable.DIRECT_PATH_KEY"/> and <see cref="TunnelTable.NO_PATH_KEY"/>.
    /// </summary>
    public uint PrimaryKey { get; set; }

    /// <summary>Incremented by the decider each time it changes <see cref="PrimaryKey"/>; the non-decider follows the highest.</summary>
    public ushort Epoch { get; set; }

    /// <summary>A path the decider wants measured alongside the primary, or 0.</summary>
    public uint TrialKey { get; set; }

    /// <summary>
    /// The decider: the version of the fallback list it is publishing. The non-decider: the newest
    /// version it holds, which acknowledges it.
    /// </summary>
    public ushort ListVersion { get; set; }

    public FallbackListSection? FallbackList { get; set; }

    public List<TunnelTableEntry>? PingTable { get; set; }

    public List<IPEndPoint>? P2PCandidates { get; set; }

    public byte Requests { get; set; }

    /// <summary>
    /// The nonce of the table exchange that started this connection. Heartbeats from an earlier
    /// attempt with the same player, still in flight, carry a different one and are ignored.
    /// </summary>
    public uint Session { get; set; }

    public sealed class FallbackListSection
    {
        public FallbackListSection(ushort version, List<uint> keys)
        {
            Version = version;
            Keys = keys;
        }

        public ushort Version { get; }

        public List<uint> Keys { get; }
    }

    public byte[] Encode()
    {
        var sections = new List<(byte Type, byte[] Data)>();

        if (FallbackList != null)
        {
            var data = new byte[2 + (FallbackList.Keys.Count * 4)];
            BinaryPrimitives.WriteUInt16LittleEndian(data, FallbackList.Version);
            for (int i = 0; i < FallbackList.Keys.Count; i++)
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(2 + (i * 4)), FallbackList.Keys[i]);
            sections.Add((SECTION_FALLBACK_LIST, data));
        }

        if (PingTable != null)
            sections.Add((SECTION_PING_TABLE, TunnelTable.EncodeEntries(PingTable)));

        if (P2PCandidates != null)
        {
            var data = new List<byte>();
            foreach (var ep in P2PCandidates)
            {
                byte[] address = ep.Address.GetAddressBytes();
                if (address.Length != 4)
                    continue;

                data.AddRange(address);
                data.Add((byte)(ep.Port >> 8));
                data.Add((byte)ep.Port);
            }
            sections.Add((SECTION_P2P_CANDIDATES, data.ToArray()));
        }

        if (Requests != 0)
            sections.Add((SECTION_REQUESTS, [Requests]));

        int size = FIXED_SIZE;
        foreach (var (_, data) in sections)
            size += 3 + data.Length;

        var buffer = new byte[size];
        var span = buffer.AsSpan();
        span[0] = FORMAT_VERSION;
        span[1] = Flags;
        BinaryPrimitives.WriteUInt32LittleEndian(span[2..], TimeMs);
        BinaryPrimitives.WriteUInt32LittleEndian(span[6..], EchoTimeMs);
        BinaryPrimitives.WriteUInt16LittleEndian(span[10..], HoldMs);
        BinaryPrimitives.WriteUInt16LittleEndian(span[12..], HeardYouAgoMs);
        BinaryPrimitives.WriteUInt32LittleEndian(span[14..], SentTotal);
        BinaryPrimitives.WriteUInt32LittleEndian(span[18..], ReceivedTotal);
        BinaryPrimitives.WriteUInt32LittleEndian(span[22..], PrimaryKey);
        BinaryPrimitives.WriteUInt16LittleEndian(span[26..], Epoch);
        BinaryPrimitives.WriteUInt32LittleEndian(span[28..], TrialKey);
        BinaryPrimitives.WriteUInt16LittleEndian(span[32..], ListVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(span[34..], Session);
        BinaryPrimitives.WriteUInt32LittleEndian(span[38..], EchoSentTotal);

        int offset = FIXED_SIZE;
        foreach (var (type, data) in sections)
        {
            span[offset] = type;
            BinaryPrimitives.WriteUInt16LittleEndian(span[(offset + 1)..], (ushort)data.Length);
            data.CopyTo(span[(offset + 3)..]);
            offset += 3 + data.Length;
        }

        return buffer;
    }

    public static bool TryDecode(ReadOnlySpan<byte> span, out V3Heartbeat heartbeat)
    {
        heartbeat = new V3Heartbeat();

        if (span.Length < FIXED_SIZE || span[0] != FORMAT_VERSION)
            return false;

        heartbeat.Flags = span[1];
        heartbeat.TimeMs = BinaryPrimitives.ReadUInt32LittleEndian(span[2..]);
        heartbeat.EchoTimeMs = BinaryPrimitives.ReadUInt32LittleEndian(span[6..]);
        heartbeat.HoldMs = BinaryPrimitives.ReadUInt16LittleEndian(span[10..]);
        heartbeat.HeardYouAgoMs = BinaryPrimitives.ReadUInt16LittleEndian(span[12..]);
        heartbeat.SentTotal = BinaryPrimitives.ReadUInt32LittleEndian(span[14..]);
        heartbeat.ReceivedTotal = BinaryPrimitives.ReadUInt32LittleEndian(span[18..]);
        heartbeat.PrimaryKey = BinaryPrimitives.ReadUInt32LittleEndian(span[22..]);
        heartbeat.Epoch = BinaryPrimitives.ReadUInt16LittleEndian(span[26..]);
        heartbeat.TrialKey = BinaryPrimitives.ReadUInt32LittleEndian(span[28..]);
        heartbeat.ListVersion = BinaryPrimitives.ReadUInt16LittleEndian(span[32..]);
        heartbeat.Session = BinaryPrimitives.ReadUInt32LittleEndian(span[34..]);
        heartbeat.EchoSentTotal = BinaryPrimitives.ReadUInt32LittleEndian(span[38..]);

        int offset = FIXED_SIZE;
        while (offset + 3 <= span.Length)
        {
            byte type = span[offset];
            int length = BinaryPrimitives.ReadUInt16LittleEndian(span[(offset + 1)..]);
            if (offset + 3 + length > span.Length)
                return false;

            var data = span.Slice(offset + 3, length);
            switch (type)
            {
                case SECTION_FALLBACK_LIST when length >= 2:
                    var keys = new List<uint>((length - 2) / 4);
                    for (int i = 2; i + 4 <= length; i += 4)
                        keys.Add(BinaryPrimitives.ReadUInt32LittleEndian(data[i..]));
                    heartbeat.FallbackList = new FallbackListSection(BinaryPrimitives.ReadUInt16LittleEndian(data), keys);
                    break;

                case SECTION_PING_TABLE:
                    heartbeat.PingTable = [.. TunnelTable.DecodeEntries(data).Values];
                    break;

                case SECTION_P2P_CANDIDATES:
                    var endpoints = new List<IPEndPoint>();
                    for (int i = 0; i + 6 <= length; i += 6)
                        endpoints.Add(new IPEndPoint(new IPAddress(data.Slice(i, 4).ToArray()), (data[i + 4] << 8) | data[i + 5]));
                    heartbeat.P2PCandidates = endpoints;
                    break;

                case SECTION_REQUESTS when length >= 1:
                    heartbeat.Requests = data[0];
                    break;
            }

            offset += 3 + length;
        }

        return true;
    }
}
