#nullable enable
using DTAClient.Domain.Multiplayer.CnCNet;

using System;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

public class TunnelSelectedEventArgs : EventArgs
{
    public TunnelSelectedEventArgs(CnCNetTunnel? tunnel, TunnelMode mode)
    {
        Tunnel = tunnel;
        Mode = mode;
    }

    public CnCNetTunnel? Tunnel { get; }
    public TunnelMode Mode { get; }
}
