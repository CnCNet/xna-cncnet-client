#nullable enable
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Represents a direct peer-to-peer connection as a synthetic V3 tunnel.
/// Treated identically to relay tunnels during negotiation and game bridging;
/// the communicator routes packets directly to <see cref="PeerEndpoint"/> instead
/// of a relay server.
/// </summary>
public class P2PTunnel : CnCNetTunnel
{
    private const string PeerEndpointHashSalt = "cncnet-p2p-tunnel";

    public IPEndPoint PeerEndpoint { get; }
    public string PeerName { get; }

    public override bool IsDirect => true;

    public P2PTunnel(IPEndPoint peerEndpoint, string peerName)
        : base(peerEndpoint.Address.ToString(), peerEndpoint.Port, $"Direct ({peerName} @ {GetPeerEndpointHash(peerEndpoint)})", version: 3)
    {
        PeerEndpoint = peerEndpoint;
        PeerName = peerName;
    }

    private static string GetPeerEndpointHash(IPEndPoint peerEndpoint)
    {
        byte[] hash;
        using (var sha1 = SHA1.Create())
            hash = sha1.ComputeHash(Encoding.UTF8.GetBytes($"{PeerEndpointHashSalt}|{peerEndpoint}"));

        return Utilities.BytesToHexString(hash, 0, 3);
    }
}
