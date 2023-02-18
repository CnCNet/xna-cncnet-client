using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "GetExternalIPAddressResponse", WrapperNamespace = "urn:schemas-upnp-org:service:WANIPConnection:1")]
internal readonly record struct GetExternalIPAddressResponseV1(
    [property: MessageBodyMember(Name = "NewExternalIPAddress")] string ExternalIPAddress);