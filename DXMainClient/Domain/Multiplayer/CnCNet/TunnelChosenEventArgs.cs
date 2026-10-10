#nullable enable
using System;

namespace DTAClient.Domain.Multiplayer.CnCNet;

public class TunnelChosenEventArgs : EventArgs
{
    public uint PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public CnCNetTunnel? ChosenTunnel { get; set; }
    public bool IsLocalDecision { get; set; }
    public string? FailureReason { get; set; }
    public int NegotiationPing { get; set; }

    /// <summary>
    /// True when this (successful) result is the relay fallback after a P2P upgrade round
    /// broke down, so the lobby can tell the player the direct connection didn't stick.
    /// </summary>
    public bool IsRelayFallback { get; set; }
}
