using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "GetExternalIPAddress", WrapperNamespace = "urn:schemas-upnp-org:service:WANIPConnection:2")]
internal readonly record struct GetExternalIPAddressRequestV2;