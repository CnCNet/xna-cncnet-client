#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Discovers this machine's P2P candidate endpoints: the STUN-reflexive external endpoint
/// (for peers on other networks) and the local LAN endpoints (for peers behind the same NAT).
/// Queries go through the <see cref="V3TunnelCommunicator"/>'s socket so the discovered
/// external port matches the port used for game data.
/// </summary>
public class P2PEndpointDiscovery
{
    private readonly V3TunnelCommunicator _communicator;

    private IPEndPoint? _cachedEndpoint;
    private Task<IPEndPoint?>? _discoveryTask;
    private readonly object _discoveryLock = new();

    public P2PEndpointDiscovery(V3TunnelCommunicator communicator)
    {
        _communicator = communicator;
    }

    /// <summary>
    /// Returns the cached STUN-discovered external endpoint for this session, or discovers it
    /// by querying official tunnel servers (and any configured STUN hosts) as STUN endpoints.
    /// Returns null if the NAT is symmetric or no STUN servers respond.
    /// </summary>
    /// <param name="tunnels">The current tunnel list, used to pick STUN hosts.</param>
    public Task<IPEndPoint?> GetOrDiscoverAsync(IReadOnlyList<CnCNetTunnel> tunnels)
    {
        if (_cachedEndpoint != null)
            return Task.FromResult<IPEndPoint?>(_cachedEndpoint);

        // Single-flight: several player negotiations can run at once (3+ player games), so
        // share one in-flight discovery rather than racing STUN queries to the same servers
        // on the shared communicator socket (which would clobber each other's pending query).
        lock (_discoveryLock)
        {
            if (_cachedEndpoint != null)
                return Task.FromResult<IPEndPoint?>(_cachedEndpoint);

            return _discoveryTask ??= DiscoverAsync(tunnels);
        }
    }

    private async Task<IPEndPoint?> DiscoverAsync(IReadOnlyList<CnCNetTunnel> tunnels)
    {
        try
        {
            var stunHosts = tunnels
                .Where(t => t.Official || t.Recommended)
                .Select(t => t.Address)
                .Distinct()
                .Take(8)
                .ToList();

            // Prepend any configured STUN hosts
            string configuredHosts = ClientConfiguration.Instance.P2PStunServers;
            if (!string.IsNullOrWhiteSpace(configuredHosts))
            {
                var configured = configuredHosts.Split(';', StringSplitOptions.RemoveEmptyEntries);
                stunHosts.InsertRange(0, configured);
            }

            var ep = await StunHelper.DiscoverExternalEndpointAsync(_communicator, stunHosts).ConfigureAwait(false);
            _cachedEndpoint = ep;
            return ep;
        }
        finally
        {
            // Release the in-flight slot: a success is now served from _cachedEndpoint,
            // and a failure (null) can be retried (serially) by a later negotiation.
            lock (_discoveryLock)
                _discoveryTask = null;
        }
    }

    /// <summary>
    /// Returns this machine's local (LAN) endpoints — every non-loopback IPv4 unicast address
    /// paired with the communicator's UDP port. These are offered as additional P2P candidates
    /// so peers behind the same NAT (e.g. on the same LAN) can connect directly without relying
    /// on NAT hairpinning of the reflexive address.
    /// </summary>
    public List<IPEndPoint> GetLocalEndpoints()
    {
        var result = new List<IPEndPoint>();
        int port = _communicator.LocalPort;
        if (port == 0)
            return result;

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((ni.OperationalStatus != OperationalStatus.Up && ni.OperationalStatus != OperationalStatus.Unknown) ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork ||
                        IPAddress.IsLoopback(addr.Address))
                        continue;

                    result.Add(new IPEndPoint(addr.Address, port));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"P2PEndpointDiscovery: Failed to enumerate local endpoints: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Clears the cached STUN result so the next P2P negotiation re-queries.
    /// Call when P2P is enabled in options or after a network change.
    /// </summary>
    public void ClearCache() => _cachedEndpoint = null;
}
