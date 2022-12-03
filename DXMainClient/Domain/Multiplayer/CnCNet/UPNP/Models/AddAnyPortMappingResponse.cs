using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "AddAnyPortMappingResponse", WrapperNamespace = "urn:schemas-upnp-org:service:WANIPConnection:2")]
internal readonly record struct AddAnyPortMappingResponse(
    [property: MessageBodyMember(Name = "NewReservedPort")] ushort ReservedPort);