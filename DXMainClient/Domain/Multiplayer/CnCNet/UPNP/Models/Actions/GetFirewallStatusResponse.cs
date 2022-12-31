using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = $"{UPnPConstants.GetFirewallStatus}Response", WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpv6FirewallControl}:1")]
internal readonly record struct GetFirewallStatusResponse(
    [property: MessageBodyMember(Name = "FirewallEnabled")] bool FirewallEnabled,
    [property: MessageBodyMember(Name = "InboundPinholeAllowed")] bool InboundPinholeAllowed);