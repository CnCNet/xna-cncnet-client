using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "GetNatRsipStatusResponse", WrapperNamespace = "urn:schemas-upnp-org:service:WANIPConnection:1")]
public readonly record struct GetNatRsipStatusResponseV1(
    [property: MessageBodyMember(Name = "NewRSIPAvailable")] bool RsipAvailable,
    [property: MessageBodyMember(Name = "NewNATEnabled")] bool NatEnabled);