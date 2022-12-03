using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using ClientCore;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal static class UPnPHandler
{
    private const string InternetGatewayDeviceDeviceType = "upnp:rootdevice";
    private const int UPnPMultiCastPort = 1900;
    private const int ReceiveTimeout = 2000;
    private const int SendCount = 3;

    private static IReadOnlyDictionary<AddressType, IPAddress> SsdpMultiCastAddresses => new Dictionary<AddressType, IPAddress>
    {
        [AddressType.IpV4SiteLocal] = IPAddress.Parse("239.255.255.250"),
        [AddressType.IpV6LinkLocal] = IPAddress.Parse("[FF02::C]"),
        [AddressType.IpV6SiteLocal] = IPAddress.Parse("[FF05::C]")
    }.AsReadOnly();

    public static async ValueTask<(InternetGatewayDevice InternetGatewayDevice, List<ushort> P2pPorts, List<ushort> P2pIpV6PortIds, IPAddress ipV6Address, IPAddress ipV4Address)> SetupPortsAsync(InternetGatewayDevice internetGatewayDevice, IEnumerable<ushort> p2pReservedPorts)
    {
        var p2pPorts = new List<ushort>();
        var p2pIpV6PortIds = new List<ushort>();
        IPAddress routerPublicIpV4Address = null;
        bool? routerNatEnabled = null;
        bool natDetected = false;

        if (internetGatewayDevice is null)
        {
            var internetGatewayDevices = (await GetInternetGatewayDevicesAsync()).ToList();

            internetGatewayDevice = GetInternetGatewayDevice(internetGatewayDevices, 2);
            internetGatewayDevice ??= GetInternetGatewayDevice(internetGatewayDevices, 1);
        }

        if (internetGatewayDevice is not null)
        {
            routerNatEnabled = await internetGatewayDevice.GetNatRsipStatusAsync();
            routerPublicIpV4Address = await internetGatewayDevice.GetExternalIpV4AddressAsync();
        }

        var publicIpAddresses = NetworkHelper.GetPublicIpAddresses().ToList();
        IPAddress publicIpV4Address = publicIpAddresses.FirstOrDefault(q => q.AddressFamily is AddressFamily.InterNetwork);

        if ((routerNatEnabled ?? false) || (publicIpV4Address is not null && routerPublicIpV4Address is not null && !publicIpV4Address.Equals(routerPublicIpV4Address)))
            natDetected = true;

        publicIpV4Address ??= routerPublicIpV4Address;

        var privateIpV4Addresses = NetworkHelper.GetPrivateIpAddresses().Where(q => q.AddressFamily is AddressFamily.InterNetwork).ToList();
        IPAddress privateIpV4Address = null;

        try
        {
            privateIpV4Address = privateIpV4Addresses.FirstOrDefault();

            if (natDetected && privateIpV4Address is not null)
            {
                foreach (int p2PReservedPort in p2pReservedPorts)
                {
                    p2pPorts.Add(await internetGatewayDevice.OpenIpV4PortAsync(privateIpV4Address, (ushort)p2PReservedPort));
                }

                p2pReservedPorts = p2pPorts;
            }
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, $"Could not open P2P IPV4 ports for {privateIpV4Address} -> {publicIpV4Address}.");
        }

        IPAddress publicIpV6Address = null;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var publicIpV6Addresses = NetworkHelper.GetWindowsPublicIpAddresses().Where(q => q.IpAddress.AddressFamily is AddressFamily.InterNetworkV6).ToList();

                (IPAddress IpAddress, PrefixOrigin PrefixOrigin, SuffixOrigin SuffixOrigin) foundPublicIpV6Address = publicIpV6Addresses
                    .FirstOrDefault(q => q.PrefixOrigin is PrefixOrigin.RouterAdvertisement && q.SuffixOrigin is SuffixOrigin.LinkLayerAddress);

                if (foundPublicIpV6Address.IpAddress is null)
                {
                    foundPublicIpV6Address = publicIpV6Addresses
                        .FirstOrDefault(q => q.PrefixOrigin is PrefixOrigin.Dhcp && q.SuffixOrigin is SuffixOrigin.OriginDhcp);
                }

                publicIpV6Address = foundPublicIpV6Address.IpAddress;
            }
            else
            {
                publicIpV6Address = NetworkHelper.GetPublicIpAddresses()
                    .FirstOrDefault(q => q.AddressFamily is AddressFamily.InterNetworkV6);
            }

            if (publicIpV6Address is not null && internetGatewayDevice is not null)
            {
                (bool firewallEnabled, bool inboundPinholeAllowed) = await internetGatewayDevice.GetIpV6FirewallStatusAsync();

                if (firewallEnabled && inboundPinholeAllowed)
                {
                    foreach (int p2pReservedPort in p2pReservedPorts)
                    {
                        p2pIpV6PortIds.Add(await internetGatewayDevice.OpenIpV6PortAsync(publicIpV6Address, (ushort)p2pReservedPort));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, $"Could not open P2P IPV6 ports for {publicIpV6Address}.");
        }

        return (internetGatewayDevice, p2pPorts, p2pIpV6PortIds, publicIpV6Address, publicIpV4Address);
    }

    private static async ValueTask<IEnumerable<InternetGatewayDevice>> GetInternetGatewayDevicesAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<string> rawDeviceResponses = await GetRawDeviceResponses(cancellationToken);
        IEnumerable<Dictionary<string, string>> formattedDeviceResponses = GetFormattedDeviceResponses(rawDeviceResponses);
        IEnumerable<IGrouping<string, InternetGatewayDeviceResponse>> groupedInternetGatewayDeviceResponses = GetGroupedInternetGatewayDeviceResponses(formattedDeviceResponses);

        return await ClientCore.Extensions.TaskExtensions.WhenAllSafe(groupedInternetGatewayDeviceResponses.Select(q => GetInternetGatewayDeviceAsync(q, cancellationToken)));
    }

    private static InternetGatewayDevice GetInternetGatewayDevice(List<InternetGatewayDevice> internetGatewayDevices, ushort uPnPVersion)
        => internetGatewayDevices.SingleOrDefault(q => $"{InternetGatewayDevice.UPnPInternetGatewayDevice}:{uPnPVersion}".Equals(q.UPnPDescription.Device.DeviceType, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<IGrouping<string, InternetGatewayDeviceResponse>> GetGroupedInternetGatewayDeviceResponses(IEnumerable<Dictionary<string, string>> formattedDeviceResponses)
    {
        return formattedDeviceResponses
            .Select(q => new InternetGatewayDeviceResponse(new(q["LOCATION"]), q["SERVER"], q["CACHE-CONTROL"], q["EXT"], q["ST"], q["USN"]))
            .GroupBy(q => q.Usn);
    }

    private static Uri GetPreferredLocation(IReadOnlyCollection<Uri> locations)
    {
        return locations.FirstOrDefault(q => q.HostNameType is UriHostNameType.IPv6) ?? locations.First(q => q.HostNameType is UriHostNameType.IPv4);
    }

    private static IEnumerable<Dictionary<string, string>> GetFormattedDeviceResponses(IEnumerable<string> responses)
    {
        return responses.Select(q => q.Split(Environment.NewLine)).Select(q => q.Where(r => r.Contains(':', StringComparison.OrdinalIgnoreCase)).ToDictionary(
            s => s[..s.IndexOf(':', StringComparison.OrdinalIgnoreCase)],
            s =>
            {
                string value = s[s.IndexOf(':', StringComparison.OrdinalIgnoreCase)..];

                if (value.EndsWith(":", StringComparison.OrdinalIgnoreCase))
                    return value.Replace(":", null, StringComparison.OrdinalIgnoreCase);

                return value.Replace(": ", null, StringComparison.OrdinalIgnoreCase);
            },
            StringComparer.OrdinalIgnoreCase));
    }

    private static async Task<IEnumerable<string>> SearchDevicesAsync(IPAddress localAddress, CancellationToken cancellationToken)
    {
        var responses = new List<string>();
        AddressType addressType = GetAddressType(localAddress);

        if (addressType is AddressType.Unknown)
            return responses;

        using var socket = new Socket(localAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        socket.ExclusiveAddressUse = true;

        socket.Bind(new IPEndPoint(localAddress, 0));

        var multiCastIpEndPoint = new IPEndPoint(SsdpMultiCastAddresses[addressType], UPnPMultiCastPort);
        string request = FormattableString.Invariant($"M-SEARCH * HTTP/1.1\r\nHOST: {multiCastIpEndPoint}\r\nST: {InternetGatewayDeviceDeviceType}\r\nMAN: \"ssdp:discover\"\r\nMX: 3\r\n\r\n");
        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(request));

        for (int i = 0; i < SendCount; i++)
        {
            _ = await socket.SendToAsync(buffer, SocketFlags.None, multiCastIpEndPoint);
        }

        await ReceiveAsync(socket, new(new byte[4096]), responses, ReceiveTimeout, cancellationToken);

        return responses;
    }

    private static AddressType GetAddressType(IPAddress localAddress)
    {
        if (localAddress.AddressFamily == AddressFamily.InterNetwork)
            return AddressType.IpV4SiteLocal;

        if (localAddress.IsIPv6LinkLocal)
            return AddressType.IpV6LinkLocal;

        if (localAddress.IsIPv6SiteLocal)
            return AddressType.IpV6SiteLocal;

        return AddressType.Unknown;
    }

    private static async ValueTask ReceiveAsync(Socket socket, ArraySegment<byte> buffer, ICollection<string> responses, int receiveTimeout, CancellationToken cancellationToken)
    {
        using var timeoutCancellationTokenSource = new CancellationTokenSource(receiveTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

        while (!linkedCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                int bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None, linkedCancellationTokenSource.Token);

                if (bytesReceived > 0)
                    responses.Add(Encoding.UTF8.GetString(buffer.Take(bytesReceived).ToArray()));
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private static async ValueTask<UPnPDescription> GetUPnPDescription(Uri uri, CancellationToken cancellationToken)
    {
        using var client = new HttpClient(
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                AutomaticDecompression = DecompressionMethods.All
            }, true)
        {
            Timeout = TimeSpan.FromMilliseconds(ReceiveTimeout),
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };

        await using Stream uPnPDescription = await client.GetStreamAsync(uri, cancellationToken);
        using var xmlTextReader = new XmlTextReader(uPnPDescription);

        return (UPnPDescription)new DataContractSerializer(typeof(UPnPDescription)).ReadObject(xmlTextReader);
    }

    private static async ValueTask<IEnumerable<string>> GetRawDeviceResponses(CancellationToken cancellationToken)
    {
        IEnumerable<IPAddress> localAddresses = NetworkHelper.GetLocalAddresses();
        IEnumerable<string>[] localAddressesDeviceResponses = await ClientCore.Extensions.TaskExtensions.WhenAllSafe(localAddresses.Select(q => SearchDevicesAsync(q, cancellationToken)));

        return localAddressesDeviceResponses.Where(q => q.Any()).SelectMany(q => q).Distinct();
    }

    private static async Task<InternetGatewayDevice> GetInternetGatewayDeviceAsync(IGrouping<string, InternetGatewayDeviceResponse> internetGatewayDeviceResponses, CancellationToken cancellationToken)
    {
        Uri[] locations = internetGatewayDeviceResponses.Select(r => r.Location).ToArray();
        Uri location = GetPreferredLocation(locations);
        UPnPDescription uPnPDescription = default;

        try
        {
            uPnPDescription = await GetUPnPDescription(location, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (location.HostNameType is UriHostNameType.IPv6 && locations.Any(q => q.HostNameType is UriHostNameType.IPv4))
            {
                try
                {
                    location = locations.SingleOrDefault(q => q.HostNameType is UriHostNameType.IPv4);

                    uPnPDescription = await GetUPnPDescription(location, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        return new(
            internetGatewayDeviceResponses.Select(r => r.Location).Distinct(),
            internetGatewayDeviceResponses.Select(r => r.Server).Distinct().Single(),
            internetGatewayDeviceResponses.Select(r => r.CacheControl).Distinct().Single(),
            internetGatewayDeviceResponses.Select(r => r.Ext).Distinct().Single(),
            internetGatewayDeviceResponses.Select(r => r.SearchTarget).Distinct().Single(),
            internetGatewayDeviceResponses.Key,
            uPnPDescription,
            location);
    }
}