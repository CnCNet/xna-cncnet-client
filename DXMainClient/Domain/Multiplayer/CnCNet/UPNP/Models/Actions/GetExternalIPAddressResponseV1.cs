using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = $"{UPnPConstants.GetExternalIPAddress}Response", WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpConnection}:1")]
internal readonly record struct GetExternalIPAddressResponseV1(
    [property: MessageBodyMember(Name = "NewExternalIPAddress")] string ExternalIPAddress);