#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// One way of reaching a peer: a relay tunnel or a direct P2P route, with what the pair has
/// measured about it.
/// </summary>
/// <remarks>
/// The counters and <see cref="LastHeardMs"/> are written by the game bridge without the link's
/// lock, so they are 64-bit interlocked (the XNA build is 32-bit). Everything else belongs to the
/// owning <see cref="V3PairLink"/> and is only touched under its lock.
/// </remarks>
public sealed class V3LinkPath
{
    /// <summary>How far back loss is measured over.</summary>
    private const long LOSS_WINDOW_MS = 10_000;

    /// <summary>Fewest packets in the window for a loss figure to mean anything.</summary>
    private const int MIN_LOSS_SAMPLE_PACKETS = 8;

    private long _lastHeardMs;
    private long _sentTotal;
    private long _receivedTotal;

    private readonly Queue<LossSample> _lossSamples = new();

    private readonly struct LossSample
    {
        public LossSample(long atMs, uint mySent, uint peerReceived, uint peerSent, uint myReceived)
        {
            AtMs = atMs;
            MySent = mySent;
            PeerReceived = peerReceived;
            PeerSent = peerSent;
            MyReceived = myReceived;
        }

        public long AtMs { get; }
        public uint MySent { get; }
        public uint PeerReceived { get; }
        public uint PeerSent { get; }
        public uint MyReceived { get; }
    }

    internal V3LinkPath(CnCNetTunnel tunnel, long nowMs)
    {
        Tunnel = tunnel;
        CreatedMs = nowMs;
    }

    public CnCNetTunnel Tunnel { get; }

    public bool IsDirect => Tunnel.IsDirect;

    /// <summary>When the path was (re)activated.</summary>
    internal long CreatedMs { get; set; }

    /// <summary>When anything last arrived from the peer on this path, or 0 if nothing has.</summary>
    internal long LastHeardMs => Interlocked.Read(ref _lastHeardMs);

    public long SentTotal => Interlocked.Read(ref _sentTotal);

    public long ReceivedTotal => Interlocked.Read(ref _receivedTotal);

    /// <summary>Counts a packet sent on this path. Safe from any thread.</summary>
    public void CountSent() => Interlocked.Increment(ref _sentTotal);

    /// <summary>Counts a packet received from the peer on this path. Safe from any thread.</summary>
    public void CountReceived(long nowMs)
    {
        Interlocked.Increment(ref _receivedTotal);
        Interlocked.Exchange(ref _lastHeardMs, nowMs);
    }

    /// <summary>Whether heartbeats are being sent on the path.</summary>
    internal bool Active { get; set; } = true;

    /// <summary>For an inactive path, until when traffic arriving on it is still accepted.</summary>
    internal long RetireUntilMs { get; set; }

    internal long LastSentHeartbeatMs { get; set; } = long.MinValue / 2;

    /// <summary>How long ago the peer last heard us on this path, as of <see cref="PeerReportAtMs"/>; null before any heartbeat.</summary>
    internal int? PeerHeardMeAgoMs { get; set; }

    internal long PeerReportAtMs { get; set; }

    internal uint LastPeerTimeMs { get; set; }

    internal long LastPeerTimeAtMs { get; set; }

    /// <summary>Smoothed round trip (ms) through this path, or null before any measurement.</summary>
    internal double? RttMs { get; private set; }

    internal int RttSamples { get; private set; }

    /// <summary>When the path was first confirmed in both directions, or 0.</summary>
    internal long ConfirmedSinceMs { get; private set; }

    /// <summary>
    /// Marks the path confirmed. Loss measured before this covers packets the tunnel dropped
    /// while the peer was not yet registered on it, so it is discarded.
    /// </summary>
    internal void MarkConfirmed(long nowMs)
    {
        ConfirmedSinceMs = nowMs;
        _lossSamples.Clear();
    }

    internal void ResetConfirmation() => ConfirmedSinceMs = 0;

    internal void AddRttSample(int rttMs)
    {
        RttMs = RttMs == null ? rttMs : (RttMs.Value * 0.75) + (rttMs * 0.25);
        RttSamples++;
    }

    /// <summary>The peer's sent total from its latest heartbeat, and our received total at that moment; echoed back to it.</summary>
    internal uint LastPeerSentTotal { get; private set; }

    internal uint ReceivedAtLastPeerHeartbeat { get; private set; }

    /// <summary>
    /// Records a heartbeat's counts. <paramref name="echoedMySent"/> and
    /// <paramref name="peerReceivedOfMine"/> describe our direction at one instant on the peer's
    /// side; <paramref name="peerSent"/> and our received total do the same for theirs on ours.
    /// </summary>
    internal void AddLossSample(long nowMs, uint peerSent, uint echoedMySent, uint peerReceivedOfMine)
    {
        uint myReceived = (uint)ReceivedTotal;
        LastPeerSentTotal = peerSent;
        ReceivedAtLastPeerHeartbeat = myReceived;

        _lossSamples.Enqueue(new LossSample(nowMs, echoedMySent, peerReceivedOfMine, peerSent, myReceived));

        while (_lossSamples.Count > 2 && _lossSamples.Peek().AtMs < nowMs - (LOSS_WINDOW_MS * 2))
            _lossSamples.Dequeue();
    }

    /// <summary>
    /// Round-trip packet loss (percent) over the last 10 seconds: the chance a packet or its reply
    /// is lost, combining both directions. Null until enough has been sent to judge.
    /// </summary>
    /// <remarks>
    /// Each side reports cumulative totals, so the window needs no agreement about which packets
    /// it covers. Each sample pairs a sent total with the received total at the moment that sent
    /// total was known to the receiver, so packets in flight are not counted as lost.
    /// </remarks>
    internal double? GetLossPercent(long nowMs)
    {
        LossSample? first = null;
        LossSample last = default;
        int count = 0;

        foreach (var sample in _lossSamples)
        {
            if (sample.AtMs < nowMs - LOSS_WINDOW_MS)
                continue;

            first ??= sample;
            last = sample;
            count++;
        }

        if (first == null || count < 2)
            return null;

        double? outbound = Fraction(last.PeerReceived - first.Value.PeerReceived, last.MySent - first.Value.MySent);
        double? inbound = Fraction(last.MyReceived - first.Value.MyReceived, last.PeerSent - first.Value.PeerSent);

        if (outbound == null && inbound == null)
            return null;

        double delivered = (outbound ?? 1) * (inbound ?? 1);
        return Math.Max(0, Math.Min(100, (1 - delivered) * 100));
    }

    private static double? Fraction(uint received, uint sent)
    {
        if (sent < MIN_LOSS_SAMPLE_PACKETS || sent > int.MaxValue)
            return null;

        return Math.Min(1.0, received / (double)sent);
    }

    /// <summary>How long ago (ms) the peer last heard us on this path, aged to <paramref name="nowMs"/>.</summary>
    internal long GetPeerSilenceMs(long nowMs)
        => PeerHeardMeAgoMs.HasValue ? PeerHeardMeAgoMs.Value + (nowMs - PeerReportAtMs) : nowMs - CreatedMs;

    /// <summary>
    /// How long the path has been quiet in either direction: since we last heard the peer on it,
    /// or since the peer last heard us, whichever is longer. Using both is what makes a one-way
    /// failure visible to the side that can still hear.
    /// </summary>
    internal long GetSilenceMs(long nowMs)
    {
        long own = nowMs - Math.Max(LastHeardMs, CreatedMs);
        return Math.Max(own, GetPeerSilenceMs(nowMs));
    }

    /// <summary>Whether traffic has crossed the path in both directions within <paramref name="windowMs"/>.</summary>
    internal bool IsConfirmed(long nowMs, long windowMs)
    {
        long heard = LastHeardMs;
        return heard != 0 && nowMs - heard < windowMs && PeerHeardMeAgoMs.HasValue && GetPeerSilenceMs(nowMs) < windowMs;
    }

    /// <summary>The path's score for comparisons: round trip plus a penalty per percent of loss.</summary>
    internal double GetScore(long nowMs, int lossWeight)
        => RttMs.HasValue ? RttMs.Value + ((GetLossPercent(nowMs) ?? 0) * lossWeight) : double.MaxValue;

    public override string ToString() => Tunnel.Name;
}
