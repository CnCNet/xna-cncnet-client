#nullable enable
using System;

namespace DTAClient.Domain.Multiplayer.CnCNet;

public class TunnelFailedEventArgs : EventArgs
{
    public CnCNetTunnel Tunnel { get; }
    public TunnelFailedEventArgs(CnCNetTunnel tunnel)
    {
        Tunnel = tunnel;
    }
}
