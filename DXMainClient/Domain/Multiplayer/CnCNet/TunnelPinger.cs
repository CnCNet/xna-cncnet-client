#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Measures relay tunnels with the V3 UDP ping: a 50-byte packet with zero sender and receiver
/// IDs, sent to the tunnel's own port, which the server answers with the first 12 bytes.
/// </summary>
/// <remarks>
/// <para>This is the only reachability test worth trusting. Many V3 tunnels drop ICMP while
/// relaying fine, others answer ICMP with a dead tunnel port, and ICMP latency can be off by tens
/// of milliseconds. A tunnel both peers can UDP-ping relays between them; one either peer cannot
/// does not.</para>
///
/// <para>Pings carry no ID, so they open no session and take no client slot. Servers answer at most
/// 20 per IP per minute, a budget shared by everyone behind the same public address, so each
/// tunnel is rationed to <see cref="ClientConfiguration.V3MaxPingsPerTunnelPerMinute"/>.</para>
///
/// <para>Pings go out on the communicator's socket, so they measure the path the relayed traffic
/// actually takes, through the same NAT mapping.</para>
/// </remarks>
public sealed class TunnelPinger
{
    private static int MAX_PINGS_PER_MINUTE => ClientConfiguration.Instance.V3MaxPingsPerTunnelPerMinute;
    private static int PING_TIMEOUT_MS => ClientConfiguration.Instance.V3UdpPingTimeoutMs;

    private readonly V3TunnelCommunicator _communicator;
    private readonly ConcurrentDictionary<uint, PendingPing> _pending = new();
    private readonly ConcurrentDictionary<CnCNetTunnel, Queue<long>> _sendHistory = new();
    private int _nextTag;

    private sealed class PendingPing
    {
        public PendingPing(CnCNetTunnel tunnel, long sentTicks)
        {
            Tunnel = tunnel;
            SentTicks = sentTicks;
        }

        public CnCNetTunnel Tunnel { get; }
        public long SentTicks { get; }
        public TaskCompletionSource<PingValue> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public TunnelPinger(V3TunnelCommunicator communicator)
    {
        _communicator = communicator;
        _communicator.UdpPingReplyReceived = OnReply;
    }

    /// <summary>
    /// Pings a tunnel once. Completes with the round trip, with <see cref="PingValue.Unknown"/> if
    /// no reply arrived in time, or with null if the ping was not sent because the tunnel's
    /// budget is spent or the communicator is not running — which the caller must not read as
    /// the tunnel being down.
    /// </summary>
    public async Task<PingValue?> PingAsync(CnCNetTunnel tunnel, CancellationToken cancellationToken = default)
    {
        if (!tunnel.SupportsUdpPing || !_communicator.IsInitialized || !TryTakeBudget(tunnel))
            return null;

        uint tag = (uint)Interlocked.Increment(ref _nextTag);
        var pending = new PendingPing(tunnel, Stopwatch.GetTimestamp());
        _pending[tag] = pending;

        try
        {
            if (!_communicator.SendUdpPing(tunnel, tag))
                return null;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(PING_TIMEOUT_MS);
            return await pending.Completion.Task.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PingValue.Unknown;
        }
        finally
        {
            _pending.TryRemove(tag, out _);
        }
    }

    /// <summary>
    /// Pings several tunnels at once, spacing the sends so a long list is not one burst. Tunnels
    /// whose ping was not sent are absent from the result.
    /// </summary>
    public async Task<Dictionary<CnCNetTunnel, PingValue>> PingAllAsync(
        IReadOnlyList<CnCNetTunnel> tunnels, CancellationToken cancellationToken = default)
    {
        var tasks = new List<(CnCNetTunnel Tunnel, Task<PingValue?> Task)>(tunnels.Count);

        foreach (var tunnel in tunnels)
        {
            tasks.Add((tunnel, PingAsync(tunnel, cancellationToken)));
            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
        }

        var results = new Dictionary<CnCNetTunnel, PingValue>();
        foreach (var (tunnel, task) in tasks)
        {
            PingValue? ping = await task.ConfigureAwait(false);
            if (ping.HasValue)
                results[tunnel] = ping.Value;
        }

        return results;
    }

    /// <summary>
    /// Whether this client's own connection works: true if any of the given tunnels answers.
    /// Used before blaming a tunnel for a silent peer, since a local outage silences every path
    /// at once and no failover can fix it.
    /// </summary>
    public async Task<bool?> IsAnyReachableAsync(IReadOnlyList<CnCNetTunnel> tunnels, CancellationToken cancellationToken = default)
    {
        var results = await PingAllAsync(tunnels, cancellationToken).ConfigureAwait(false);
        if (results.Count == 0)
            return null;

        return results.Values.Any(p => p.IsValid());
    }

    private bool TryTakeBudget(CnCNetTunnel tunnel)
    {
        var history = _sendHistory.GetOrAdd(tunnel, _ => new Queue<long>());
        long now = Stopwatch.GetTimestamp();
        long windowStart = now - Stopwatch.Frequency * 60;

        lock (history)
        {
            while (history.Count > 0 && history.Peek() < windowStart)
                history.Dequeue();

            if (history.Count >= MAX_PINGS_PER_MINUTE)
                return false;

            history.Enqueue(now);
            return true;
        }
    }

    // Receive thread.
    private void OnReply(uint tag, CnCNetTunnel tunnel, long receivedTicks)
    {
        if (!_pending.TryGetValue(tag, out var pending) || pending.Tunnel != tunnel)
            return;

        double ms = (receivedTicks - pending.SentTicks) * 1000.0 / Stopwatch.Frequency;
        pending.Completion.TrySetResult(PingValue.FromMs(Math.Max(0, (int)Math.Round(ms))));
    }
}
