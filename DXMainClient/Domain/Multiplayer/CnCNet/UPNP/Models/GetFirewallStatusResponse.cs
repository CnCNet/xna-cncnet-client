using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "GetFirewallStatusResponse", WrapperNamespace = "urn:dslforum-org:service:WANIPv6FirewallControl:1")]
internal readonly record struct GetFirewallStatusResponse(
    [property: MessageBodyMember(Name = "FirewallEnabled")] bool FirewallEnabled,
    [property: MessageBodyMember(Name = "InboundPinholeAllowed")] bool InboundPinholeAllowed);