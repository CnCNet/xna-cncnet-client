using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "AddPinholeResponse", WrapperNamespace = "urn:dslforum-org:service:WANIPv6FirewallControl:1")]
internal readonly record struct AddPinholeResponse(
    [property: MessageBodyMember(Name = "UniqueID")] ushort UniqueId);