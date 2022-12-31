using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[MessageContract(WrapperName = UPnPConstants.GetFirewallStatus, WrapperNamespace = $"{UPnPConstants.UPnPServiceNamespace}:{UPnPConstants.WanIpv6FirewallControl}:1")]
internal readonly record struct GetFirewallStatusRequest;