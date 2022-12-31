using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = "AddAnyPortMappingResponse", WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpConnection}:2")]
internal readonly record struct AddAnyPortMappingResponse(
    [property: MessageBodyMember(Name = "NewReservedPort")] ushort ReservedPort);