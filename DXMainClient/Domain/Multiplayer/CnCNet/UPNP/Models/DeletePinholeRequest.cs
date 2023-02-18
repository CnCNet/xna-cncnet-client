using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "DeletePinhole", WrapperNamespace = "urn:dslforum-org:service:WANIPv6FirewallControl:1")]
internal readonly record struct DeletePinholeRequest(
    [property: MessageBodyMember(Name = "UniqueID")] ushort UniqueId);