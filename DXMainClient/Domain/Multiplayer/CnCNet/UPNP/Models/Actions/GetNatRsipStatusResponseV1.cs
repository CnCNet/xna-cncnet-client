using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = $"{UPnPConstants.GetNatRsipStatus}Response", WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpConnection}:1")]
public readonly record struct GetNatRsipStatusResponseV1(
    [property: MessageBodyMember(Name = "NewRSIPAvailable")] bool RsipAvailable,
    [property: MessageBodyMember(Name = "NewNATEnabled")] bool NatEnabled);