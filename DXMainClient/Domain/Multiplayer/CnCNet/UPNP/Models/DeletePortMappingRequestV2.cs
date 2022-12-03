using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "DeletePortMapping", WrapperNamespace = "urn:schemas-upnp-org:service:WANIPConnection:2")]
internal readonly record struct DeletePortMappingRequestV2(
    [property: MessageBodyMember(Name = "NewRemoteHost")] string RemoteHost, // “x.x.x.x” or empty string
    [property: MessageBodyMember(Name = "NewExternalPort")] ushort ExternalPort,
    [property: MessageBodyMember(Name = "NewProtocol")] string Protocol); // TCP or UDP