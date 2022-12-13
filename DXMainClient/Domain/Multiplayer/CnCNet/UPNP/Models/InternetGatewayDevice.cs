using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;
using System.ServiceModel.Channels;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal sealed record InternetGatewayDevice(IEnumerable<Uri> Locations, string Server, string CacheControl, string Ext, string SearchTarget, string UniqueServiceName, UPnPDescription UPnPDescription, Uri PreferredLocation)
{
    private const int ReceiveTimeout = 10000;
    private const string UPnPWanConnectionDevice = "urn:schemas-upnp-org:device:WANConnectionDevice";
    private const string UPnPWanDevice = "urn:schemas-upnp-org:device:WANDevice";
    private const string UPnPService = "urn:schemas-upnp-org:service";
    private const string UPnPWanIpConnection = "WANIPConnection";
    private const uint IpLeaseTimeInSeconds = 4 * 60 * 60;
    private const ushort IanaUdpProtocolNumber = 17;
    private const string PortMappingDescription = "CnCNet";

    private static readonly HttpClient HttpClient = new(
        new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            SslOptions = new()
            {
                RemoteCertificateValidationCallback = (_, _, _, sslPolicyErrors) => (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == 0,
            }
        }, true)
    {
        Timeout = TimeSpan.FromMilliseconds(ReceiveTimeout),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
    };

    public const string UPnPInternetGatewayDevice = "urn:schemas-upnp-org:device:InternetGatewayDevice";

    public async ValueTask<ushort> OpenIpV4PortAsync(IPAddress ipAddress, ushort port, CancellationToken cancellationToken)
    {
        Logger.Log($"Opening IPV4 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        int uPnPVersion = GetDeviceUPnPVersion();
        (ServiceListItem service, string serviceUri, string serviceType) = GetSoapActionParameters($"{UPnPWanIpConnection}:{uPnPVersion}", AddressFamily.InterNetwork);

        switch (uPnPVersion)
        {
            case 2:
                string addAnyPortMappingAction = $"\"{service.ServiceType}#AddAnyPortMapping\"";
                var addAnyPortMappingRequest = new AddAnyPortMappingRequest(string.Empty, port, "UDP", port, ipAddress.ToString(), 1, PortMappingDescription, IpLeaseTimeInSeconds);
                AddAnyPortMappingResponse addAnyPortMappingResponse = await ExecuteSoapAction<AddAnyPortMappingRequest, AddAnyPortMappingResponse>(
                    serviceUri, addAnyPortMappingAction, serviceType, addAnyPortMappingRequest, cancellationToken);

                port = addAnyPortMappingResponse.ReservedPort;

                break;
            case 1:
                string addPortMappingAction = $"\"{service.ServiceType}#AddPortMapping\"";
                var addPortMappingRequest = new AddPortMappingRequest(string.Empty, port, "UDP", port, ipAddress.ToString(), 1, PortMappingDescription, IpLeaseTimeInSeconds);

                await ExecuteSoapAction<AddPortMappingRequest, AddPortMappingResponse>(
                    serviceUri, addPortMappingAction, serviceType, addPortMappingRequest, cancellationToken);

                break;
            default:
                throw new ArgumentException($"UPNP version {uPnPVersion} is not supported.");
        }

        Logger.Log($"Opened IPV4 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        return port;
    }

    public async ValueTask CloseIpV4PortAsync(ushort port, CancellationToken cancellationToken = default)
    {
        Logger.Log($"Deleting IPV4 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        int uPnPVersion = GetDeviceUPnPVersion();
        (ServiceListItem service, string serviceUri, string serviceType) = GetSoapActionParameters($"{UPnPWanIpConnection}:{uPnPVersion}");
        string serviceAction = $"\"{service.ServiceType}#DeletePortMapping\"";

        switch (uPnPVersion)
        {
            case 2:
                var deletePortMappingRequestV2 = new DeletePortMappingRequestV2(string.Empty, port, "UDP");

                await ExecuteSoapAction<DeletePortMappingRequestV2, DeletePortMappingResponseV2>(
                    serviceUri, serviceAction, serviceType, deletePortMappingRequestV2, cancellationToken);

                break;
            case 1:
                var deletePortMappingRequestV1 = new DeletePortMappingRequestV1(string.Empty, port, "UDP");

                await ExecuteSoapAction<DeletePortMappingRequestV1, DeletePortMappingResponseV1>(
                    serviceUri, serviceAction, serviceType, deletePortMappingRequestV1, cancellationToken);

                break;
            default:
                throw new ArgumentException($"UPNP version {uPnPVersion} is not supported.");
        }

        Logger.Log($"Deleted IPV4 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");
    }

    public async ValueTask<IPAddress> GetExternalIpV4AddressAsync(CancellationToken cancellationToken)
    {
        Logger.Log($"Requesting external IP address from UPnP device {UPnPDescription.Device.FriendlyName}.");

        int uPnPVersion = GetDeviceUPnPVersion();
        (ServiceListItem service, string serviceUri, string serviceType) = GetSoapActionParameters($"{UPnPWanIpConnection}:{uPnPVersion}");
        string serviceAction = $"\"{service.ServiceType}#GetExternalIPAddress\"";
        IPAddress ipAddress;

        switch (uPnPVersion)
        {
            case 2:
                GetExternalIPAddressResponseV2 getExternalIpAddressResponseV2 = await ExecuteSoapAction<GetExternalIPAddressRequestV2, GetExternalIPAddressResponseV2>(
                    serviceUri, serviceAction, serviceType, default, cancellationToken);

                ipAddress = string.IsNullOrWhiteSpace(getExternalIpAddressResponseV2.ExternalIPAddress) ? null : IPAddress.Parse(getExternalIpAddressResponseV2.ExternalIPAddress);

                break;
            case 1:
                GetExternalIPAddressResponseV1 getExternalIpAddressResponseV1 = await ExecuteSoapAction<GetExternalIPAddressRequestV1, GetExternalIPAddressResponseV1>(
                    serviceUri, serviceAction, serviceType, default, cancellationToken);

                ipAddress = string.IsNullOrWhiteSpace(getExternalIpAddressResponseV1.ExternalIPAddress) ? null : IPAddress.Parse(getExternalIpAddressResponseV1.ExternalIPAddress);
                break;
            default:
                throw new ArgumentException($"UPNP version {uPnPVersion} is not supported.");
        }

        Logger.Log($"Received external IP address {ipAddress} from UPnP device {UPnPDescription.Device.FriendlyName}.");

        return ipAddress;
    }

    public async ValueTask<bool> GetNatRsipStatusAsync(CancellationToken cancellationToken)
    {
        Logger.Log($"Checking NAT status on UPnP device {UPnPDescription.Device.FriendlyName}.");

        int uPnPVersion = GetDeviceUPnPVersion();
        (ServiceListItem service, string serviceUri, string serviceType) = GetSoapActionParameters($"{UPnPWanIpConnection}:{uPnPVersion}");
        string serviceAction = $"\"{service.ServiceType}#GetNatRsipStatus\"";
        bool natEnabled;

        switch (uPnPVersion)
        {
            case 2:
                GetNatRsipStatusResponseV2 getNatRsipStatusResponseV2 = await ExecuteSoapAction<GetNatRsipStatusRequestV2, GetNatRsipStatusResponseV2>(
                    serviceUri, serviceAction, serviceType, default, cancellationToken);

                natEnabled = getNatRsipStatusResponseV2.NatEnabled;

                break;
            case 1:
                GetNatRsipStatusResponseV1 getNatRsipStatusResponseV1 = await ExecuteSoapAction<GetNatRsipStatusRequestV1, GetNatRsipStatusResponseV1>(
                    serviceUri, serviceAction, serviceType, default, cancellationToken);

                natEnabled = getNatRsipStatusResponseV1.NatEnabled;
                break;
            default:
                throw new ArgumentException($"UPNP version {uPnPVersion} is not supported.");
        }

        Logger.Log($"Received NAT status {natEnabled} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        return natEnabled;
    }

    public async ValueTask<(bool FirewallEnabled, bool InboundPinholeAllowed)> GetIpV6FirewallStatusAsync(CancellationToken cancellationToken)
    {
        Logger.Log($"Checking IPV6 firewall status on UPnP device {UPnPDescription.Device.FriendlyName}.");

        (ServiceListItem service, string serviceUri, string serviceType) = GetSoapActionParameters("WANIPv6FirewallControl:1");
        string serviceAction = $"\"{service.ServiceType}#GetFirewallStatus\"";
        GetFirewallStatusResponse response = await ExecuteSoapAction<GetFirewallStatusRequest, GetFirewallStatusResponse>(
            serviceUri, serviceAction, serviceType, default, cancellationToken);

        Logger.Log($"Received IPV6 firewall status {response.FirewallEnabled} and port mapping allowed {response.InboundPinholeAllowed} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        return (response.FirewallEnabled, response.InboundPinholeAllowed);
    }

    public async ValueTask<ushort> OpenIpV6PortAsync(IPAddress ipAddress, ushort port, CancellationToken cancellationToken)
    {
        Logger.Log($"Opening IPV6 UDP port {port} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        (ServiceListItem service, string serviceUri, string serviceType) = GetSoapActionParameters("WANIPv6FirewallControl:1");
        string serviceAction = $"\"{service.ServiceType}#AddPinhole\"";
        var request = new AddPinholeRequest(string.Empty, port, ipAddress.ToString(), port, IanaUdpProtocolNumber, IpLeaseTimeInSeconds);
        AddPinholeResponse response = await ExecuteSoapAction<AddPinholeRequest, AddPinholeResponse>(
            serviceUri, serviceAction, serviceType, request, cancellationToken);

        Logger.Log($"Opened IPV6 UDP port {port} with ID {response.UniqueId} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        return response.UniqueId;
    }

    public async ValueTask CloseIpV6PortAsync(ushort uniqueId, CancellationToken cancellationToken = default)
    {
        Logger.Log($"Opening IPV6 UDP port with ID {uniqueId} on UPnP device {UPnPDescription.Device.FriendlyName}.");

        (ServiceListItem service, string serviceUri, string serviceType) = GetSoapActionParameters("WANIPv6FirewallControl:1");
        string serviceAction = $"\"{service.ServiceType}#DeletePinhole\"";
        var request = new DeletePinholeRequest(uniqueId);
        await ExecuteSoapAction<DeletePinholeRequest, DeletePinholeResponse>(
             serviceUri, serviceAction, serviceType, request, cancellationToken);

        Logger.Log($"Opened IPV6 UDP port with ID {uniqueId} on UPnP device {UPnPDescription.Device.FriendlyName}.");
    }

    private static async ValueTask<TResponse> ExecuteSoapAction<TRequest, TResponse>(string serviceUri, string soapAction, string defaultNamespace, TRequest request, CancellationToken cancellationToken)
    {
        HttpClient.DefaultRequestHeaders.Remove("SOAPAction");
        HttpClient.DefaultRequestHeaders.Add("SOAPAction", soapAction);

        var xmlSerializerFormatAttribute = new XmlSerializerFormatAttribute
        {
            Style = OperationFormatStyle.Rpc,
            Use = OperationFormatUse.Encoded
        };
        var requestTypedMessageConverter = TypedMessageConverter.Create(typeof(TRequest), soapAction, defaultNamespace, xmlSerializerFormatAttribute);
        using var requestMessage = requestTypedMessageConverter.ToMessage(request);
        await using var requestStream = new MemoryStream();
        await using var writer = XmlWriter.Create(
            requestStream,
            new()
            {
                OmitXmlDeclaration = true,
                Async = true,
                Encoding = new UTF8Encoding(false)
            });
        requestMessage.WriteMessage(writer);
        await writer.FlushAsync();

        requestStream.Position = 0L;

        using var content = new StreamContent(requestStream);

        content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml");

        using HttpResponseMessage httpResponseMessage = await HttpClient.PostAsync(serviceUri, content, cancellationToken);
        await using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken);

        try
        {
            httpResponseMessage.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            using var reader = new StreamReader(stream);
            string error = await reader.ReadToEndAsync(CancellationToken.None);

            ProgramConstants.LogException(ex, $"UPNP error {ex.StatusCode}:{error}.");

            throw;
        }

        using var envelopeReader = XmlDictionaryReader.CreateTextReader(stream, new());
        using var responseMessage = Message.CreateMessage(envelopeReader, int.MaxValue, MessageVersion.Soap11WSAddressingAugust2004);
        var responseTypedMessageConverter = TypedMessageConverter.Create(typeof(TResponse), null, defaultNamespace, xmlSerializerFormatAttribute);

        return (TResponse)responseTypedMessageConverter.FromMessage(responseMessage);
    }

    private (ServiceListItem WanIpConnectionService, string ServiceUri, string ServiceType) GetSoapActionParameters(string wanConnectionDeviceService, AddressFamily? addressFamily = null)
    {
        Uri location = PreferredLocation;

        if (addressFamily is AddressFamily.InterNetwork && Locations.Any(q => q.HostNameType is UriHostNameType.IPv4))
            location = Locations.FirstOrDefault(q => q.HostNameType is UriHostNameType.IPv4);

        int uPnPVersion = GetDeviceUPnPVersion();
        Device wanDevice = UPnPDescription.Device.DeviceList.Single(q => q.DeviceType.Equals($"{UPnPWanDevice}:{uPnPVersion}", StringComparison.OrdinalIgnoreCase));
        Device wanConnectionDevice = wanDevice.DeviceList.Single(q => q.DeviceType.Equals($"{UPnPWanConnectionDevice}:{uPnPVersion}", StringComparison.OrdinalIgnoreCase));
        string serviceType = $"{UPnPService}:{wanConnectionDeviceService}";
        ServiceListItem wanIpConnectionService = wanConnectionDevice.ServiceList.Single(q => q.ServiceType.Equals(serviceType, StringComparison.OrdinalIgnoreCase));
        string serviceUri = FormattableString.Invariant($"{location.Scheme}://{location.Authority}{wanIpConnectionService.ControlUrl}");

        return new(wanIpConnectionService, serviceUri, serviceType);
    }

    private int GetDeviceUPnPVersion()
    {
        return $"{UPnPInternetGatewayDevice}:2".Equals(UPnPDescription.Device.DeviceType, StringComparison.OrdinalIgnoreCase) ? 2
            : ($"{UPnPInternetGatewayDevice}:1".Equals(UPnPDescription.Device.DeviceType, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
    }
}