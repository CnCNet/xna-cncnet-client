#nullable enable

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// A lobby's player for V3 tunnel-based negotiation and communication.
/// </summary>
public class V3PlayerInfo(uint id, string name, int playerIndex, ushort playerGameID)
{
    private CnCNetTunnel? _assignedTunnel;

    public uint Id { get; set; } = id;
    public string Name { get; set; } = name;
    public int PlayerIndex { get; set; } = playerIndex;
    public ushort PlayerGameId { get; set; } = playerGameID;

    /// <summary>
    /// The connection to this player in dynamic mode. Null for the local player, in other modes,
    /// and before a connection has been started.
    /// </summary>
    public V3PairLink? Link { get; internal set; }

    /// <summary>
    /// The tunnel this player's traffic goes through: in dynamic mode the pair's current path,
    /// which can change at any time; otherwise the tunnel assigned by the host.
    /// </summary>
    public CnCNetTunnel? Tunnel
    {
        get => Link?.PrimaryTunnel ?? _assignedTunnel;
        set => _assignedTunnel = value;
    }

    /// <summary>Measured packet loss (percent) to this player over the last 10 seconds, or null.</summary>
    public double? PacketLoss => Link?.PacketLossPercent;

    /// <summary>Whether a connection to this player is being set up and has not succeeded yet.</summary>
    public bool IsNegotiating => Link != null && Link.Status == NegotiationStatus.InProgress;
}
