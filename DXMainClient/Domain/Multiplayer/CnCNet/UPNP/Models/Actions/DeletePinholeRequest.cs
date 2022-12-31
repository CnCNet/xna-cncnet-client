using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = UPnPConstants.DeletePinhole, WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpv6FirewallControl}:1")]
internal readonly record struct DeletePinholeRequest(
    [property: MessageBodyMember(Name = "UniqueID")] ushort UniqueId);