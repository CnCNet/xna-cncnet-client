using System;

namespace DTAClient.Domain.Multiplayer.CnCNet;

public class TunnelChosenEventArgs : EventArgs
{
    public uint PlayerId { get; set; }
    public string PlayerName { get; set; }
    public CnCNetTunnel ChosenTunnel { get; set; }
    public bool IsLocalDecision { get; set; }
    public string FailureReason { get; set; }
}
