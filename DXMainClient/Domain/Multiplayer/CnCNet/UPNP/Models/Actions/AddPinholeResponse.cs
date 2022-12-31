using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = $"{UPnPConstants.AddPinhole}Response", WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpv6FirewallControl}:1")]
internal readonly record struct AddPinholeResponse(
    [property: MessageBodyMember(Name = "UniqueID")] ushort UniqueId);