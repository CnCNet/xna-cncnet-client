namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

internal static class UPnPConstants
{
    private const string UPnPNamespace = "urn:schemas-upnp-org";
    private const string UPnPDeviceNamespace = $"{UPnPNamespace}:device";
    public const string UPnPDevice10Namespace = $"{UPnPDeviceNamespace}-1-0";
    public const string UPnPServiceNamespace = $"{UPnPNamespace}:service";
    public const string UPnPWanConnectionDevice = $"{UPnPDeviceNamespace}:WANConnectionDevice";
    public const string UPnPWanDevice = $"{UPnPDeviceNamespace}:WANDevice";
    public const string UPnPInternetGatewayDevice = $"{UPnPDeviceNamespace}:InternetGatewayDevice";
    public const string WanIpConnection = "WANIPConnection";
    public const string WanIpv6FirewallControl = "WANIPv6FirewallControl";
    public const string UPnPRootDevice = "upnp:rootdevice";
    public const string AddAnyPortMapping = "AddAnyPortMapping";
    public const string AddPinhole = "AddPinhole";
    public const string AddPortMapping = "AddPortMapping";
    public const string DeletePinhole = "DeletePinhole";
    public const string DeletePortMapping = "DeletePortMapping";
    public const string GetExternalIPAddress = "GetExternalIPAddress";
    public const string GetFirewallStatus = "GetFirewallStatus";
    public const string GetNatRsipStatus = "GetNatRsipStatus";
    public const int UPnPMultiCastPort = 1900;
}