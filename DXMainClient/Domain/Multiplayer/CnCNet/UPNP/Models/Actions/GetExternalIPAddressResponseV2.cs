using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = $"{UPnPConstants.GetExternalIPAddress}Response", WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpConnection}:2")]
internal readonly record struct GetExternalIPAddressResponseV2(
    [property: MessageBodyMember(Name = "NewExternalIPAddress")] string ExternalIPAddress);