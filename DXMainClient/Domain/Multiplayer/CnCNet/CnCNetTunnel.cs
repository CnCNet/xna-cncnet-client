using Rampastring.Tools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A CnCNet tunnel server.
    /// </summary>
    /// <remarks>
    /// Equality is by <see cref="Address"/> and <see cref="Port"/>, not by reference. The same
    /// endpoint can be represented by more than one instance — a relay list refresh, or a
    /// renegotiation rebuilding its <see cref="P2PTunnel"/> candidates — and every consumer
    /// (routing maps, per-tunnel test results, keep-alive trackers) must treat those as one
    /// path. Both properties are immutable after construction, so instances are safe as
    /// dictionary keys.
    /// </remarks>
    public class CnCNetTunnel : IEquatable<CnCNetTunnel>
    {
        private const int REQUEST_TIMEOUT = 10000; // In milliseconds
        private const int PING_TIMEOUT = 1000;

        public CnCNetTunnel() { }

        protected CnCNetTunnel(string address, int port, string name, int version)
        {
            Address = address;
            Port = port;
            Name = name;
            Version = version;
            Official = false;
            Recommended = true;
        }

        /// <summary>
        /// Parses a formatted string that contains the tunnel server's 
        /// information into a CnCNetTunnel instance.
        /// </summary>
        /// <param name="str">The string that contains the tunnel server's information.</param>
        /// <returns>A CnCNetTunnel instance parsed from the given string.</returns>
        public static CnCNetTunnel Parse(string str)
        {
            // For the format, check http://cncnet.org/master-list

            try
            {
                var tunnel = new CnCNetTunnel();
                string[] parts = str.Split(';');

                string address = parts[0];
                string[] detailedAddress = address.Split(new char[] { ':' });

                tunnel.Address = detailedAddress[0];
                tunnel.Port = int.Parse(detailedAddress[1]);
                tunnel.Country = parts[1];
                tunnel.CountryCode = parts[2];
                tunnel.Name = parts[3];
                tunnel.RequiresPassword = parts[4] != "0";
                tunnel.Clients = int.Parse(parts[5]);
                tunnel.MaxClients = int.Parse(parts[6]);
                int status = int.Parse(parts[7]);
                tunnel.Official = status == 2;
                if (!tunnel.Official)
                    tunnel.Recommended = status == 1;

                CultureInfo cultureInfo = CultureInfo.InvariantCulture;

                tunnel.Latitude = double.Parse(parts[8], cultureInfo);
                tunnel.Longitude = double.Parse(parts[9], cultureInfo);
                tunnel.Version = int.Parse(parts[10]);
                tunnel.Distance = double.Parse(parts[11], cultureInfo);

                return tunnel;
            }
            catch (Exception ex)
            {
                if (ex is FormatException || ex is OverflowException || ex is IndexOutOfRangeException)
                {
                    Logger.Log("Parsing tunnel information failed: " + ex.ToString() + Environment.NewLine + "Parsed string: " + str);
                    return null;
                }

                throw;
            }
        }

        public string Address { get; private set; }
        public int Port { get; private set; }
        public string Country { get; private set; }
        public string CountryCode { get; private set; }
        public string Name { get; private set; }
        public bool RequiresPassword { get; private set; }
        public int Clients { get; private set; }
        public int MaxClients { get; private set; }
        public bool Official { get; private set; }
        public bool Recommended { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        /// <summary>
        /// The tunnel's entry in the master list: 2 and 3 are wire protocols,
        /// <see cref="MATCHMAKING_VERSION"/> is a role that still speaks protocol 3.
        /// </summary>
        public int Version { get; private set; }

        public double Distance { get; private set; }
        public PingValue Ping { get; set; } = PingValue.Unknown;

        /// <summary>
        /// How many probes in a row have come back unanswered. Drives how long the last good
        /// measurement is retained by <see cref="ApplyPingResult"/>.
        /// </summary>
        internal int ConsecutiveUnknownPings { get; private set; }

        /// <summary>
        /// Records the outcome of a ping probe, keeping the previous measurement when a probe is
        /// merely lost rather than discarding it on the first miss.
        /// </summary>
        /// <param name="measuredPing">The probe's result, or <see cref="PingValue.Unknown"/> if it went unanswered.</param>
        /// <param name="maxRetainedFailures">
        /// How many consecutive unanswered probes the last good measurement survives. Zero
        /// discards it immediately, restoring the previous behaviour.
        /// </param>
        /// <remarks>
        /// One probe is sent per master list refresh, so on a lossy link a single lost datagram
        /// used to leave a perfectly good tunnel with no measurement until the next refresh a
        /// minute later. That is more damaging than it sounds: a tunnel nobody could measure is
        /// ranked below every tunnel that was measured, so the best server drops out of the ranked
        /// list entirely and is never even tried. Retaining is deliberately bounded — a tunnel that
        /// has genuinely gone away stops reporting a stale ping after a few rounds.
        /// </remarks>
        internal void ApplyPingResult(PingValue measuredPing, int maxRetainedFailures)
        {
            if (measuredPing.IsValid())
            {
                ConsecutiveUnknownPings = 0;
                Ping = measuredPing;
                return;
            }

            ConsecutiveUnknownPings++;

            // Nothing worth keeping, or kept for long enough that it can no longer be trusted.
            if (Ping.IsUnknown() || ConsecutiveUnknownPings > maxRetainedFailures)
                Ping = PingValue.Unknown;
        }

        // One negotiator runs per remote player, on pool threads, all sharing this instance out of
        // TunnelHandler.Tunnels - so these two are written concurrently.
        private int _consecutiveHandshakeFailures;
        private int _hasCompletedHandshake;

        /// <summary>
        /// How many negotiations in a row have tested this tunnel without a single packet from the
        /// peer arriving through it. Maintained by the negotiator; see <see cref="IsHandshakeSuspect"/>.
        /// </summary>
        internal int ConsecutiveHandshakeFailures => Volatile.Read(ref _consecutiveHandshakeFailures);

        /// <summary>
        /// Whether a negotiation has ever relayed a packet through this tunnel, so a tunnel that
        /// has stopped working can be told from one that never worked.
        /// </summary>
        internal bool HasCompletedHandshake => Volatile.Read(ref _hasCompletedHandshake) != 0;

        internal void RecordHandshakeSuccess()
        {
            Volatile.Write(ref _hasCompletedHandshake, 1);
            Volatile.Write(ref _consecutiveHandshakeFailures, 0);
        }

        internal void RecordHandshakeFailure() => Interlocked.Increment(ref _consecutiveHandshakeFailures);

        /// <summary>
        /// Whether this tunnel has failed to relay often enough to be ranked below tunnels that
        /// have not. A <paramref name="failureThreshold"/> of zero or less disables the check.
        /// </summary>
        /// <remarks>
        /// A server can answer pings while refusing registrations, for example when it is full or
        /// in maintenance, in which case it looks ideal to a latency ranking and then wastes a
        /// bring-up slot. This is the only signal that tells the two apart.
        /// </remarks>
        public bool IsHandshakeSuspect(int failureThreshold)
            => failureThreshold > 0 && ConsecutiveHandshakeFailures >= failureThreshold;

        private long _lastSessionUseTicks;

        /// <summary>
        /// Records that this client just sent the tunnel something under its own V3 ID, which
        /// creates or refreshes a session there. Pings do not count: they carry no ID.
        /// </summary>
        internal void MarkSessionUse() => Volatile.Write(ref _lastSessionUseTicks, Stopwatch.GetTimestamp());

        /// <summary>
        /// Whether this client has held a session on the tunnel within the last
        /// <paramref name="seconds"/>.
        /// </summary>
        /// <remarks>
        /// A tunnel pins each session to the endpoint that opened it and rejects the ID from any
        /// other endpoint until the session has been idle for its timeout (60 seconds on current
        /// servers). After a NAT rebind every such tunnel is unusable for that long, while a tunnel
        /// the client has not touched recently works at once, so failover prefers the latter.
        /// </remarks>
        internal bool WasSessionUsedWithin(double seconds)
        {
            long ticks = Volatile.Read(ref _lastSessionUseTicks);
            return ticks != 0 && (Stopwatch.GetTimestamp() - ticks) < seconds * Stopwatch.Frequency;
        }

        /// <summary>
        /// Whether the tunnel answers the V3 UDP ping on its own port: a 50-byte packet with zero
        /// sender and receiver IDs, echoed as its first 12 bytes. It needs no registration and
        /// takes no client slot, and unlike ICMP it proves the tunnel port itself is reachable.
        /// </summary>
        public bool SupportsUdpPing => !IsDirect && (Version == 3 || Version == MATCHMAKING_VERSION);

        /// <summary>
        /// Whether this is a direct peer-to-peer path rather than a relay tunnel server.
        /// Relay tunnels are always false; <see cref="P2PTunnel"/> overrides this to true.
        /// Used to exclude synthetic P2P entries from relay-only operations (registration,
        /// endpoint mapping, ping refresh).
        /// </summary>
        public virtual bool IsDirect => false;

        /// <summary>
        /// The master list version that identifies a matchmaking server.
        /// </summary>
        /// <remarks>
        /// A discovery label rather than a wire protocol version — matchmaking servers speak the
        /// same V3 protocol. Announcing them as 4 keeps clients that accept only 2 and 3 from
        /// picking one to host a game on, which it would refuse to carry.
        /// </remarks>
        public const int MATCHMAKING_VERSION = 4;

        /// <summary>
        /// Whether this tunnel is a matchmaking server. These carry no game traffic and are
        /// excluded from the candidate pool.
        /// </summary>
        public bool IsMatchmaking => Version == MATCHMAKING_VERSION;

        /// <summary>
        /// Whether the tunnel had room as of the last master list refresh, given a 0-1
        /// <paramref name="occupancyThreshold"/> of its advertised capacity. Keeps negotiation from
        /// steering a pair onto a server about to reject them, which the client cannot otherwise
        /// detect: a full server simply drops the registration packet.
        /// </summary>
        /// <remarks>
        /// A <see cref="MaxClients"/> of zero counts as available. That is what synthetic tunnels
        /// and incomplete master list entries look like, and excluding them would be worse.
        /// </remarks>
        public bool HasSpareCapacity(double occupancyThreshold) => MaxClients <= 0 || Clients < MaxClients * occupancyThreshold;

        /// <summary>
        /// Updates this tunnel's metadata from another tunnel instance, preserving Address, Port, and existing Ping.
        /// </summary>
        internal void UpdateFrom(CnCNetTunnel updatedTunnel)
        {
            Country = updatedTunnel.Country;
            CountryCode = updatedTunnel.CountryCode;
            Name = updatedTunnel.Name;
            Clients = updatedTunnel.Clients;
            MaxClients = updatedTunnel.MaxClients;
            Official = updatedTunnel.Official;
            Recommended = updatedTunnel.Recommended;
            Version = updatedTunnel.Version;

            RequiresPassword = updatedTunnel.RequiresPassword;
            Latitude = updatedTunnel.Latitude;
            Longitude = updatedTunnel.Longitude;
            Distance = updatedTunnel.Distance;
        }

        /// <summary>
        /// Gets a list of player ports to use from a specific V2 tunnel server.
        /// </summary>
        /// <returns>A list of player ports to use.</returns>
        public List<int> GetPlayerPortInfo(int playerCount)
        {
            try
            {
                Logger.Log($"Contacting tunnel at {Address}:{Port}");

                // Do not use https here as not supported by tunnels
                string addressString = $"http://{Address}:{Port}/request?clients={playerCount}";
                Logger.Log($"Downloading from {addressString}");

                string data = new TimedHttpClient(REQUEST_TIMEOUT).GetString(addressString);

                data = data.Replace("[", String.Empty);
                data = data.Replace("]", String.Empty);

                string[] portIDs = data.Split(',');
                List<int> playerPorts = new List<int>();

                foreach (string _port in portIDs)
                {
                    playerPorts.Add(Convert.ToInt32(_port));
                    Logger.Log($"Added port {_port}");
                }

                return playerPorts;
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to connect to the specified tunnel server. Returned error message: " + ex.ToString());
            }

            return new List<int>();
        }

        /// <summary>
        /// Sends a single ICMP probe and returns its result without storing it. The caller decides
        /// what the tunnel's reported ping becomes; see <see cref="ApplyPingResult"/>. Only used
        /// for tunnels without <see cref="SupportsUdpPing"/>: many V3 tunnels drop ICMP while
        /// relaying fine, and others answer ICMP with a dead tunnel port.
        /// </summary>
        /// <remarks>
        /// Asynchronous so a refresh's dozens of probes do not each hold a thread-pool thread for
        /// up to a second, which starves everything else on the pool, the UDP pings included.
        /// </remarks>
        public async Task<PingValue> MeasurePingAsync()
        {
            using (Ping p = new Ping())
            {
                try
                {
                    PingReply reply = await p.SendPingAsync(IPAddress.Parse(Address), PING_TIMEOUT).ConfigureAwait(false);
                    return reply.Status == IPStatus.Success
                        ? PingValue.FromMs(Convert.ToInt32(reply.RoundtripTime))
                        : PingValue.Unknown;
                }
                catch (PingException ex)
                {
                    Logger.Log($"Caught an exception when pinging {Name} tunnel server: {ex.ToString()}");
                    return PingValue.Unknown;
                }
                // Not only PingException: Wine Mono throws a bare SocketException when raw ICMP sockets are not permitted.
                // Leave the result unknown instead of failing the ping task.
                catch (Exception ex)
                {
                    Logger.Log($"Caught an exception when pinging {Name} tunnel server: {ex.ToString()}");
                    return PingValue.Unknown;
                }
            }
        }

        public bool Equals(CnCNetTunnel other)
        {
            if (other is null)
                return false;

            return Address == other.Address && Port == other.Port;
        }

        public override bool Equals(object obj) => Equals(obj as CnCNetTunnel);

        // Implemented with a ValueTuple rather than an anonymous type: SendPacket hashes the
        // tunnel to look up its endpoint for every outbound game packet, and an anonymous
        // type would allocate on that path.
        public override int GetHashCode() => (Address, Port).GetHashCode();

        public static bool operator ==(CnCNetTunnel left, CnCNetTunnel right)
        {
            if (left is null && right is null)
                return true;
            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(CnCNetTunnel left, CnCNetTunnel right)
        {
            return !(left == right);
        }
    }
}
