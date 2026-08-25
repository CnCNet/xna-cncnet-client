#nullable enable
using System.Net;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Represents a direct peer-to-peer connection as a synthetic V3 tunnel.
/// Treated identically to relay tunnels during negotiation and game bridging;
/// the communicator routes packets directly to <see cref="PeerEndpoint"/> instead
/// of a relay server.
/// </summary>
public class P2PTunnel : CnCNetTunnel
{
    public IPEndPoint PeerEndpoint { get; }
    public string PeerName { get; }

    public override bool IsDirect => true;

    public P2PTunnel(IPEndPoint peerEndpoint, string peerName)
        : base(peerEndpoint.Address.ToString(), peerEndpoint.Port, $"Direct ({peerName} @ {peerEndpoint})", version: 3)
    {
        PeerEndpoint = peerEndpoint;
        PeerName = peerName;
    }
}
