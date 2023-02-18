using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "GetExternalIPAddressResponse", WrapperNamespace = "urn:schemas-upnp-org:service:WANIPConnection:2")]
internal readonly record struct GetExternalIPAddressResponseV2(
    [property: MessageBodyMember(Name = "NewExternalIPAddress")] string ExternalIPAddress);