using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "GetNatRsipStatusRequest", WrapperNamespace = "urn:schemas-upnp-org:service:WANIPConnection:1")]
public readonly record struct GetNatRsipStatusRequestV1;