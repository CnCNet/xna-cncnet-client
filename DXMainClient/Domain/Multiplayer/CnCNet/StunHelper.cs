#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Implements the lightweight STUN variant used by cncnet-server (PeerToPeerUtil).
///
/// Request: 48 bytes, STUN_ID (26262 / 0x66B6) big-endian in bytes 0-1, rest random.
/// Response: 40 bytes; bytes 0-5 are XOR'd with 0x20.
///   After XOR: bytes 0-3 = external IPv4, bytes 4-5 = external port (big-endian).
///   Bytes 6-7 = STUN_ID (not XOR'd, used for validation).
///
/// IMPORTANT: queries must go through the V3TunnelCommunicator's socket so the
/// NAT mapping (internal port → external port) matches the port used for game data.
/// </summary>
public static class StunHelper
{
    private const short STUN_ID = 26262;
    private const int REQUEST_SIZE = 48;
    private const int RESPONSE_SIZE = 40;

    public static byte[] CreateRequest()
    {
        var request = new byte[REQUEST_SIZE];
        Random.Shared.NextBytes(request);
        BinaryPrimitives.WriteInt16BigEndian(request, STUN_ID);
        return request;
    }

    public static IPEndPoint? ParseResponse(byte[] response, int length)
    {
        if (length < RESPONSE_SIZE)
            return null;

        // XOR de-obfuscate bytes 0-5 (IP + port)
        var buf = (byte[])response.Clone();
        for (int i = 0; i < 6; i++)
            buf[i] ^= 0x20;

        // Validate STUN_ID at bytes 6-7 (not XOR'd by server)
        if (BinaryPrimitives.ReadInt16BigEndian(buf.AsSpan(6)) != STUN_ID)
            return null;

        var ip = new IPAddress(buf.AsSpan(0, 4).ToArray());
        ushort port = BinaryPrimitives.ReadUInt16BigEndian(buf.AsSpan(4));

        if (port == 0 || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.Loopback))
            return null;

        return new IPEndPoint(ip, port);
    }

    /// <summary>
    /// Queries multiple STUN servers via the communicator's socket and returns the
    /// external endpoint if at least <paramref name="minAgreement"/> servers return
    /// the same IP:port (confirming endpoint-independent NAT).
    /// Returns null if results diverge (symmetric NAT) or fewer than
    /// <paramref name="minAgreement"/> servers responded.
    /// </summary>
    public static async Task<IPEndPoint?> DiscoverExternalEndpointAsync(
        V3TunnelCommunicator communicator,
        IEnumerable<string> stunHosts,
        int port = 3478,
        int perHostTimeoutMs = 2000,
        int minAgreement = 2)
    {
        var hosts = stunHosts.ToList();
        if (hosts.Count == 0)
        {
            Logger.Log("StunHelper: No STUN hosts to query");
            return null;
        }

        var tasks = hosts.Select(async host =>
        {
            try
            {
                if (!IPAddress.TryParse(host, out var addr))
                {
                    var resolved = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
                    addr = resolved.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (addr == null)
                        return null;
                }
                var server = new IPEndPoint(addr, port);
                byte[]? response = await communicator.QueryStunAsync(server, perHostTimeoutMs).ConfigureAwait(false);
                if (response == null)
                    return null;
                var result = ParseResponse(response, response.Length);
                if (result != null)
                    Logger.Log($"StunHelper: {host}:{port} → {result}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"StunHelper: Error querying {host}:{port}: {ex.Message}");
                return null;
            }
        }).ToList();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var validResults = results.Where(r => r != null).Select(r => r!).ToList();

        if (validResults.Count < minAgreement)
        {
            Logger.Log($"StunHelper: Only {validResults.Count}/{hosts.Count} servers responded (need {minAgreement})");
            return null;
        }

        // Check that at least minAgreement responses agree on the same endpoint
        var groups = validResults
            .GroupBy(ep => ep.ToString())
            .OrderByDescending(g => g.Count())
            .ToList();

        var best = groups.First();
        if (best.Count() < minAgreement)
        {
            Logger.Log($"StunHelper: Results diverge — likely symmetric NAT (best agreement: {best.Count()}/{validResults.Count})");
            return null;
        }

        var discovered = best.First();
        Logger.Log($"StunHelper: External endpoint confirmed by {best.Count()}/{validResults.Count} servers: {discovered}");
        return discovered;
    }
}
