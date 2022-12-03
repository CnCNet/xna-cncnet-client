using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "AddAnyPortMapping", WrapperNamespace = "urn:schemas-upnp-org:service:WANIPConnection:2")]
internal readonly record struct AddAnyPortMappingRequest(
    [property: MessageBodyMember(Name = "NewRemoteHost")] string RemoteHost, // “x.x.x.x” or empty string
    [property: MessageBodyMember(Name = "NewExternalPort")] ushort ExternalPort,
    [property: MessageBodyMember(Name = "NewProtocol")] string Protocol, // TCP or UDP
    [property: MessageBodyMember(Name = "NewInternalPort")] ushort InternalPort,
    [property: MessageBodyMember(Name = "NewInternalClient")] string InternalClient, // “x.x.x.x” or empty string
    [property: MessageBodyMember(Name = "NewEnabled")] byte Enabled, // bool
    [property: MessageBodyMember(Name = "NewPortMappingDescription")] string PortMappingDescription,
    [property: MessageBodyMember(Name = "NewLeaseDuration")] uint LeaseDuration); // seconds