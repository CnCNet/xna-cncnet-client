using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "DeletePinholeResponse", WrapperNamespace = "urn:dslforum-org:service:WANIPv6FirewallControl:1")]
internal readonly record struct DeletePinholeResponse;