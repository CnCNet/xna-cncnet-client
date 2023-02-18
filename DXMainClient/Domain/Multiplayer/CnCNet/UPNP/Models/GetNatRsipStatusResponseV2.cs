using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "GetNatRsipStatusResponse", WrapperNamespace = "urn:schemas-upnp-org:service:WANIPConnection:2")]
public readonly record struct GetNatRsipStatusResponseV2(
    [property: MessageBodyMember(Name = "NewRSIPAvailable")] bool RsipAvailable,
    [property: MessageBodyMember(Name = "NewNATEnabled")] bool NatEnabled);