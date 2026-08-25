#nullable enable
using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer.CnCNet;

public enum TunnelMode
{
    V3Static = 0,
    V3Dynamic = 1,
    V2Legacy = 2
}

internal static class TunnelModeExtensions
{
    /// <summary>
    /// Derives the tunnel mode from the host-selected tunnel: no tunnel means dynamic
    /// negotiation, otherwise the tunnel's protocol version decides.
    /// </summary>
    public static TunnelMode FromTunnel(CnCNetTunnel? tunnel) => tunnel == null
        ? TunnelMode.V3Dynamic
        : tunnel.Version == 2 ? TunnelMode.V2Legacy : TunnelMode.V3Static;

    /// <summary>
    /// A human-readable description of the tunnel mode, used in lobby notices.
    /// </summary>
    public static string GetDescription(this TunnelMode mode) => mode switch
    {
        TunnelMode.V3Dynamic => "dynamic tunnels (V3)".L10N("Client:Main:TunnelModeDynamicV3"),
        TunnelMode.V2Legacy => "legacy tunnels (V2)".L10N("Client:Main:TunnelModeLegacyV2"),
        _ => "static tunnels (V3)".L10N("Client:Main:TunnelModeStaticV3")
    };
}
