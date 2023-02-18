using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "GetFirewallStatus", WrapperNamespace = "urn:dslforum-org:service:WANIPv6FirewallControl:1")]
internal readonly record struct GetFirewallStatusRequest;