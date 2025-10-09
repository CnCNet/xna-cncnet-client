using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Represents a single ping measurement sent to a tunnel or peer,
    /// including send and receive times, and the computed round-trip time.
    /// </summary>
    public class PingResult
    {
        public int ID { get; set; }
        public long SentTimeTicks { get; set; }
        public long? ReceivedTimeTicks { get; set; }

        public double? RoundTripTime
        {
            get
            {
                return ReceivedTimeTicks.HasValue ? (double)(ReceivedTimeTicks.Value - SentTimeTicks) / Stopwatch.Frequency * 1000.0 : null;
            }
        }

        /// <summary>
        /// A task completion source that can be awaited
        /// until the ping succeeds or times out.
        /// </summary>
        public TaskCompletionSource<bool> CompletionSource { get; set; } = new TaskCompletionSource<bool>();
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
        private const int CONNECTED_TIMEOUT_MS = 15000;

        public List<PingResult> PingResults { get; } = [];
        public bool ConnectedReceived { get; set; }

        /// <summary>
        /// A completion source that resolves when a "Connected" packet is received.
        /// </summary>
        public TaskCompletionSource<bool> ConnectedTcs { get; } = new TaskCompletionSource<bool>();

        /// <summary>
        /// A completion source that resolves when all ping attempts are completed.
        /// </summary>
        public TaskCompletionSource<bool> PingsCompletedTcs { get; } = new TaskCompletionSource<bool>();

        public double AverageRtt => PingResults
            .Where(p => p.RoundTripTime.HasValue)
            .Select(p => p.RoundTripTime.Value)
            .DefaultIfEmpty(-1)
            .Average();

        public double PacketLoss => PingResults.Count == 0 ? 100 :
            PingResults.Count(p => !p.RoundTripTime.HasValue) * 100.0 / PingResults.Count;

        public DateTime? FirstConnectedSentTime { get; set; }
        public bool ConnectedTimedOut => FirstConnectedSentTime.HasValue &&
            (DateTime.UtcNow - FirstConnectedSentTime.Value).TotalMilliseconds > CONNECTED_TIMEOUT_MS;

        public bool PingRequestReceived { get; set; }
    }

    /// <summary>
    /// A lobby's player for V3 tunnel-based negotiation and communication.
    /// </summary>
    public class V3PlayerInfo(uint id, string name, int playerIndex, ushort playerGameID)
    {
        public uint Id { get; set; } = id;
        public string Name { get; set; } = name;
        public int PlayerIndex { get; set; } = playerIndex;
        public ushort PlayerGameId { get; set; } = playerGameID;
        public bool HasNegotiated { get; set; }
        public bool IsNegotiating { get; set; }
        public CnCNetTunnel Tunnel { get; set; }
        public V3PlayerNegotiator Negotiator { get; set; }
        public Dictionary<CnCNetTunnel, TunnelTestResult> TunnelResults { get; } = [];
        private const int PACKET_LOSS_WEIGHT = 10;

        /// <summary>
        /// Creates a fresh set of <see cref="TunnelTestResult"/> entries for all available tunnels.
        /// </summary>
        public void InitializeTunnelResults(List<CnCNetTunnel> tunnels)
        {
            TunnelResults.Clear();
            foreach (var tunnel in tunnels)
                TunnelResults[tunnel] = new TunnelTestResult();
        }

        /// <summary>
        /// Retrieves the <see cref="TunnelTestResult"/> for the specified tunnel, or null if not found.
        /// </summary>
        public TunnelTestResult GetTunnelResult(CnCNetTunnel tunnel) => TunnelResults.TryGetValue(tunnel, out var result) ? result : null;

        /// <summary>
        /// Selects the best available tunnel based on RTT and packet loss
        /// </summary>
        public CnCNetTunnel SelectBestTunnel()
        {
            var bestTunnel = TunnelResults
                .Where(kvp => kvp.Value.PingResults.Any(p => p.RoundTripTime.HasValue))
                .OrderBy(kvp => kvp.Value.AverageRtt + kvp.Value.PacketLoss * PACKET_LOSS_WEIGHT) //20% packet loss = 200ms penalty
                .Select(kvp => kvp.Key)
                .FirstOrDefault();

            if (bestTunnel != null)
                Tunnel = bestTunnel;

            return bestTunnel;
        }

        /// <summary>
        /// Returns the lowest average ping across all tested tunnels.
        /// </summary>
        public double GetBestPing()
        {
            var best = SelectBestTunnel();
            return best != null ? TunnelResults[best].AverageRtt : double.NaN;
        }

        public void SetNegotiator(V3PlayerNegotiator negotiator)
        {
            StopNegotiation();
            Negotiator = negotiator;
        }

        public void StopNegotiation()
        {
            if (Negotiator != null)
            {
                Negotiator.Dispose();
                Negotiator = null;
            }
        }

        public bool StartNegotiation(V3PlayerInfo localPlayer, TunnelHandler tunnelHandler, List<CnCNetTunnel> availableTunnels)
        {
            if (this == localPlayer)
                return true;

            HasNegotiated = false;
            IsNegotiating = true;

            Logger.Log($"V3PlayerInfo: Starting negotiation with {Name} (ID: {Id})");

            if (Negotiator != null)
                return true;

            if (availableTunnels.Count == 0)
            {
                Logger.Log($"V3PlayerInfo: No available V3 tunnels for negotiation with {Name} (ID: {Id})");
                HasNegotiated = true;
                IsNegotiating = false;
                return false;
            }

            var negotiator = new V3PlayerNegotiator(localPlayer, this, availableTunnels, tunnelHandler);
            SetNegotiator(negotiator);

            _ = NegotiationWorkerAsync(negotiator);

            return true;
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
                    await Task.Yield();
                    StopNegotiation();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"V3PlayerInfo: Negotiation error with player {Name} (ID: {Id}): {ex.Message}");
                StopNegotiation();
            }

            Logger.Log($"V3PlayerInfo: Negotiation finished for {Name} (ID: {Id})");
        }
    }
}