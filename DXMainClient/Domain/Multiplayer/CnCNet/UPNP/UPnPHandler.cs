using System;
using System.Buffers;
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
using ClientCore.Extensions;
using Rampastring.Tools;

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

    public static async ValueTask<(InternetGatewayDevice InternetGatewayDevice, List<(ushort InternalPort, ushort ExternalPort)> IpV6P2PPorts, List<(ushort InternalPort, ushort ExternalPort)> IpV4P2PPorts, List<ushort> P2PIpV6PortIds, IPAddress ipV6Address, IPAddress ipV4Address)> SetupPortsAsync(
        InternetGatewayDevice internetGatewayDevice, List<ushort> p2pReservedPorts, List<IPAddress> stunServerIpAddresses, CancellationToken cancellationToken)
    {
        Logger.Log("Starting P2P Setup.");

        if (internetGatewayDevice is null)
        {
            var internetGatewayDevices = (await GetInternetGatewayDevicesAsync(cancellationToken)).ToList();

            internetGatewayDevice = GetInternetGatewayDevice(internetGatewayDevices, 2);
            internetGatewayDevice ??= GetInternetGatewayDevice(internetGatewayDevices, 1);
        }

        IPAddress detectedPublicIpV4Address = null;
        bool routerNatEnabled = false;

        if (internetGatewayDevice is not null)
        {
            Logger.Log("Found NAT device.");

            routerNatEnabled = await internetGatewayDevice.GetNatRsipStatusAsync(cancellationToken);
            detectedPublicIpV4Address = await internetGatewayDevice.GetExternalIpV4AddressAsync(cancellationToken);
        }

        var ipV4StunPortMapping = new List<(ushort InternalPort, ushort ExternalPort)>();

        if (stunServerIpAddresses.Any(q => q.AddressFamily is AddressFamily.InterNetwork))
        {
            IPAddress stunServerIpAddress = stunServerIpAddresses.Single(q => q.AddressFamily is AddressFamily.InterNetwork);

            if (detectedPublicIpV4Address == null)
            {
                Logger.Log("Using IPV4 STUN.");

                foreach (ushort p2pReservedPort in p2pReservedPorts)
                {
                    IPEndPoint publicIpV4Endpoint = await NetworkHelper.PerformStunAsync(stunServerIpAddress, p2pReservedPort, cancellationToken);

                    if (publicIpV4Endpoint is null)
                    {
                        Logger.Log("IPV4 STUN failed.");
                        break;
                    }

                    detectedPublicIpV4Address ??= publicIpV4Endpoint.Address;

                    if (p2pReservedPort != publicIpV4Endpoint.Port)
                        ipV4StunPortMapping.Add(new(p2pReservedPort, (ushort)publicIpV4Endpoint.Port));
                }
            }

            if (ipV4StunPortMapping.Any())
            {
                NetworkHelper.KeepStunAliveAsync(
                    stunServerIpAddress,
                    ipV4StunPortMapping.Select(q => q.InternalPort).ToList(), cancellationToken).HandleTask();
            }
        }
        else
        {
            Logger.Log($"STUN server {stunServerIpAddresses.First()} has no IPV4 address.");
        }

        if (detectedPublicIpV4Address == null)
        {
            Logger.Log("Using IPV4 trace detection.");

            detectedPublicIpV4Address = await NetworkHelper.TracePublicIpV4Address(cancellationToken);
        }

        var publicIpAddresses = NetworkHelper.GetPublicIpAddresses().ToList();
        IPAddress publicIpV4Address = publicIpAddresses.FirstOrDefault(q => q.AddressFamily is AddressFamily.InterNetwork);
        bool natDetected = routerNatEnabled || (publicIpV4Address is not null && detectedPublicIpV4Address is not null && !publicIpV4Address.Equals(detectedPublicIpV4Address));

        publicIpV4Address ??= detectedPublicIpV4Address;

        if (publicIpV4Address is not null)
            Logger.Log("Public IPV4 detected.");

        var privateIpV4Addresses = NetworkHelper.GetPrivateIpAddresses().Where(q => q.AddressFamily is AddressFamily.InterNetwork).ToList();
        IPAddress privateIpV4Address = privateIpV4Addresses.FirstOrDefault();
        var ipV4P2PPorts = new List<(ushort InternalPort, ushort ExternalPort)>();

        if (natDetected && routerNatEnabled && privateIpV4Address is not null && publicIpV4Address is not null)
        {
            Logger.Log("Using IPV4 port mapping.");

            try
            {
                foreach (int p2PReservedPort in p2pReservedPorts)
                {
                    ushort openedPort = await internetGatewayDevice.OpenIpV4PortAsync(privateIpV4Address, (ushort)p2PReservedPort, cancellationToken);

                    ipV4P2PPorts.Add((openedPort, openedPort));
                }

                p2pReservedPorts = ipV4P2PPorts.Select(q => q.InternalPort).ToList();
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, $"Could not open P2P IPV4 router ports for {privateIpV4Address} -> {publicIpV4Address}.");
            }
        }
        else if (ipV4StunPortMapping.Any())
        {
            ipV4P2PPorts = ipV4StunPortMapping;
        }
        else
        {
            ipV4P2PPorts = p2pReservedPorts.Select(q => (q, q)).ToList();
        }

        IPAddress detectedPublicIpV6Address = null;
        var ipV6StunPortMapping = new List<(ushort InternalPort, ushort ExternalPort)>();

        if (stunServerIpAddresses.Any(q => q.AddressFamily is AddressFamily.InterNetworkV6))
        {
            Logger.Log("Using IPV6 STUN.");

            IPAddress stunServerIpAddress = stunServerIpAddresses.Single(q => q.AddressFamily is AddressFamily.InterNetworkV6);

            foreach (ushort p2pReservedPort in p2pReservedPorts)
            {
                IPEndPoint publicIpV6Endpoint = await NetworkHelper.PerformStunAsync(stunServerIpAddress, p2pReservedPort, cancellationToken);

                if (publicIpV6Endpoint is null)
                {
                    Logger.Log("IPV6 STUN failed.");
                    break;
                }

                detectedPublicIpV6Address ??= publicIpV6Endpoint.Address;

                if (p2pReservedPort != publicIpV6Endpoint.Port)
                    ipV6StunPortMapping.Add(new(p2pReservedPort, (ushort)publicIpV6Endpoint.Port));
            }

            if (ipV6StunPortMapping.Any())
            {
                NetworkHelper.KeepStunAliveAsync(
                    stunServerIpAddress,
                    ipV6StunPortMapping.Select(q => q.InternalPort).ToList(), cancellationToken).HandleTask();
            }
        }
        else
        {
            Logger.Log($"STUN server {stunServerIpAddresses.First()} has no IPV6 address.");
        }

        IPAddress publicIpV6Address;

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

        var ipV6P2PPorts = new List<(ushort InternalPort, ushort ExternalPort)>();
        var p2pIpV6PortIds = new List<ushort>();

        if (detectedPublicIpV6Address is not null || publicIpV6Address is not null)
        {
            Logger.Log("Public IPV6 detected.");

            if (internetGatewayDevice is not null)
            {
                try
                {
                    (bool firewallEnabled, bool inboundPinholeAllowed) = await internetGatewayDevice.GetIpV6FirewallStatusAsync(cancellationToken);

                    if (firewallEnabled && inboundPinholeAllowed)
                    {
                        Logger.Log("Configuring IPV6 firewall.");

                        foreach (ushort p2pReservedPort in p2pReservedPorts)
                        {
                            p2pIpV6PortIds.Add(await internetGatewayDevice.OpenIpV6PortAsync(publicIpV6Address, p2pReservedPort, cancellationToken));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex, $"Could not open P2P IPV6 router ports for {publicIpV6Address}.");
                }
            }

            if (detectedPublicIpV6Address is not null && publicIpV6Address is not null && !detectedPublicIpV6Address.Equals(publicIpV6Address))
            {
                publicIpV6Address = detectedPublicIpV6Address;

                ipV6P2PPorts = ipV6StunPortMapping;
            }
            else
            {
                ipV6P2PPorts = p2pReservedPorts.Select(q => (q, q)).ToList();
            }
        }

        return (internetGatewayDevice, ipV6P2PPorts, ipV4P2PPorts, p2pIpV6PortIds, publicIpV6Address, publicIpV4Address);
    }

    private static async ValueTask<IEnumerable<InternetGatewayDevice>> GetInternetGatewayDevicesAsync(CancellationToken cancellationToken)
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

        var socket = new Socket(localAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        try
        {
            socket.ExclusiveAddressUse = true;

            socket.Bind(new IPEndPoint(localAddress, 0));

            var multiCastIpEndPoint = new IPEndPoint(SsdpMultiCastAddresses[addressType], UPnPMultiCastPort);
            string request = FormattableString.Invariant($"M-SEARCH * HTTP/1.1\r\nHOST: {multiCastIpEndPoint}\r\nST: {InternetGatewayDeviceDeviceType}\r\nMAN: \"ssdp:discover\"\r\nMX: 3\r\n\r\n");
            const int charSize = sizeof(char);
            int bufferSize = request.Length * charSize;
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
            Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
            int bytes = Encoding.UTF8.GetBytes(request.AsSpan(), buffer.Span);

            buffer = buffer[..bytes];

            for (int i = 0; i < SendCount; i++)
            {
                await socket.SendToAsync(buffer, SocketFlags.None, multiCastIpEndPoint, cancellationToken);
                await Task.Delay(100, cancellationToken);
            }

            await ReceiveAsync(socket, responses, cancellationToken);
        }
        finally
        {
            socket.Close();
        }

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

    private static async ValueTask ReceiveAsync(Socket socket, ICollection<string> responses, CancellationToken cancellationToken)
    {
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);
        using var timeoutCancellationTokenSource = new CancellationTokenSource(ReceiveTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

        while (!linkedCancellationTokenSource.IsCancellationRequested)
        {
            Memory<byte> buffer = memoryOwner.Memory[..4096];

            try
            {
                int bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None, linkedCancellationTokenSource.Token);

                responses.Add(Encoding.UTF8.GetString(buffer.Span[..bytesReceived]));
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
                    location = locations.First(q => q.HostNameType is UriHostNameType.IPv4);

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