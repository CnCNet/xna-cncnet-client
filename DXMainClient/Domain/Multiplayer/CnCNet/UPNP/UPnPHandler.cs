using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

internal static class UPnPHandler
{
    private const int ReceiveTimeoutInSeconds = 2;

    public static readonly HttpClient HttpClient = new(
        new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            ConnectCallback = async (context, token) =>
            {
                Socket socket = null;

                try
                {
                    socket = new(SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true
                    };

                    if (IPAddress.Parse(context.DnsEndPoint.Host).AddressFamily is AddressFamily.InterNetworkV6)
                    {
                        socket.Bind(
                            new IPEndPoint(NetworkHelper.GetLocalPublicIpV6Address()
                            ?? NetworkHelper.GetPrivateIpAddresses().First(q => q.AddressFamily is AddressFamily.InterNetworkV6),
                            0));
                    }

                    await socket.ConnectAsync(context.DnsEndPoint, token).ConfigureAwait(false);

                    return new NetworkStream(socket, true);
                }
                catch
                {
                    socket?.Dispose();

                    throw;
                }
            },
            SslOptions = new()
            {
                RemoteCertificateValidationCallback = (_, _, _, sslPolicyErrors) => (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == 0,
                CertificateChainPolicy = new()
                {
                    DisableCertificateDownloads = true
                }
            }
        },
        true)
    {
        Timeout = TimeSpan.FromSeconds(ReceiveTimeoutInSeconds),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
    };

    public static async ValueTask<(
        InternetGatewayDevice InternetGatewayDevice,
        List<(ushort InternalPort, ushort ExternalPort)> IpV6P2PPorts,
        List<(ushort InternalPort, ushort ExternalPort)> IpV4P2PPorts,
        List<ushort> P2PIpV6PortIds,
        IPAddress IpV6Address,
        IPAddress IpV4Address)> SetupPortsAsync(
        InternetGatewayDevice internetGatewayDevice,
        List<ushort> p2pReservedPorts,
        List<IPAddress> stunServerIpAddresses,
        CancellationToken cancellationToken)
    {
        Logger.Log("P2P: Starting Setup.");

        internetGatewayDevice ??= await GetInternetGatewayDeviceAsync(cancellationToken).ConfigureAwait(false);

        Task<(IPAddress IpAddress, List<(ushort InternalPort, ushort ExternalPort)> Ports)> ipV4Task =
            SetupIpV4PortsAsync(internetGatewayDevice, p2pReservedPorts, stunServerIpAddresses, cancellationToken);
        Task<(IPAddress IpAddress, List<(ushort InternalPort, ushort ExternalPort)> Ports, List<ushort> PortIds)> ipV6Task =
            SetupIpV6PortsAsync(internetGatewayDevice, p2pReservedPorts, stunServerIpAddresses, cancellationToken);

        await ClientCore.Extensions.TaskExtensions.WhenAllSafe(new Task[] { ipV4Task, ipV6Task }).ConfigureAwait(false);

        (IPAddress publicIpV4Address, List<(ushort InternalPort, ushort ExternalPort)> ipV4P2PPorts) = await ipV4Task.ConfigureAwait(false);
        (IPAddress publicIpV6Address, List<(ushort InternalPort, ushort ExternalPort)> ipV6P2PPorts, List<ushort> ipV6P2PPortIds) = await ipV6Task.ConfigureAwait(false);

        return (internetGatewayDevice, ipV6P2PPorts, ipV4P2PPorts, ipV6P2PPortIds, publicIpV6Address, publicIpV4Address);
    }

    private static async Task<InternetGatewayDevice> GetInternetGatewayDeviceAsync(CancellationToken cancellationToken)
    {
        var internetGatewayDevices = (await GetInternetGatewayDevices(cancellationToken).ConfigureAwait(false)).ToList();

        foreach (InternetGatewayDevice internetGatewayDevice in internetGatewayDevices)
        {
            Logger.Log($"P2P: Found gateway device v{internetGatewayDevice.UPnPDescription.Device.DeviceType.Split(':').LastOrDefault()} "
                + $"{internetGatewayDevice.UPnPDescription.Device.FriendlyName} ({internetGatewayDevice.Server}).");
        }

        InternetGatewayDevice selectedInternetGatewayDevice = GetInternetGatewayDeviceByVersion(internetGatewayDevices, 2);

        selectedInternetGatewayDevice ??= GetInternetGatewayDeviceByVersion(internetGatewayDevices, 1);

        if (selectedInternetGatewayDevice is not null)
        {
            Logger.Log($"P2P: Selected gateway device v{selectedInternetGatewayDevice.UPnPDescription.Device.DeviceType.Split(':').LastOrDefault()} "
                + $"{selectedInternetGatewayDevice.UPnPDescription.Device.FriendlyName} ({selectedInternetGatewayDevice.Server}).");
        }
        else
        {
            Logger.Log("P2P: No gateway devices detected.");
        }

        return selectedInternetGatewayDevice;
    }

    private static async Task<(IPAddress IpAddress, List<(ushort InternalPort, ushort ExternalPort)> Ports, List<ushort> PortIds)> SetupIpV6PortsAsync(
        InternetGatewayDevice internetGatewayDevice, List<ushort> p2pReservedPorts, List<IPAddress> stunServerIpAddresses, CancellationToken cancellationToken)
    {
        (IPAddress stunPublicIpV6Address, List<(ushort InternalPort, ushort ExternalPort)> ipV6StunPortMapping) = await NetworkHelper.PerformStunAsync(
            stunServerIpAddresses, p2pReservedPorts, AddressFamily.InterNetworkV6, cancellationToken).ConfigureAwait(false);
        IPAddress localPublicIpV6Address = NetworkHelper.GetLocalPublicIpV6Address();
        var ipV6P2PPorts = new List<(ushort InternalPort, ushort ExternalPort)>();
        var ipV6P2PPortIds = new List<ushort>();
        IPAddress publicIpV6Address = null;

        if (stunPublicIpV6Address is not null || localPublicIpV6Address is not null)
        {
            Logger.Log("P2P: Public IPV6 detected.");

            if (internetGatewayDevice is not null)
            {
                try
                {
                    (bool? firewallEnabled, bool? inboundPinholeAllowed) = await internetGatewayDevice.GetIpV6FirewallStatusAsync(
                        cancellationToken).ConfigureAwait(false);

                    if (firewallEnabled is not false && inboundPinholeAllowed is not false)
                    {
                        Logger.Log("P2P: Configuring IPV6 firewall.");

                        ipV6P2PPortIds = (await ClientCore.Extensions.TaskExtensions.WhenAllSafe(p2pReservedPorts.Select(
                            q => internetGatewayDevice.OpenIpV6PortAsync(localPublicIpV6Address, q, cancellationToken))).ConfigureAwait(false)).ToList();
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    ProgramConstants.LogException(ex, $"P2P: Could not open P2P IPV6 router ports for {localPublicIpV6Address}.");
#else
                    ProgramConstants.LogException(ex, $"P2P: Could not open P2P IPV6 router ports.");
#endif
                }
            }

            if (stunPublicIpV6Address is not null && localPublicIpV6Address is not null && !stunPublicIpV6Address.Equals(localPublicIpV6Address))
            {
                publicIpV6Address = stunPublicIpV6Address;
                ipV6P2PPorts = ipV6StunPortMapping.Any() ? ipV6StunPortMapping : p2pReservedPorts.Select(q => (q, q)).ToList();
            }
            else
            {
                publicIpV6Address = stunPublicIpV6Address ?? localPublicIpV6Address;
                ipV6P2PPorts = p2pReservedPorts.Select(q => (q, q)).ToList();
            }
        }

        return (publicIpV6Address, ipV6P2PPorts, ipV6P2PPortIds);
    }

    private static async Task<(IPAddress IpAddress, List<(ushort InternalPort, ushort ExternalPort)> Ports)> SetupIpV4PortsAsync(
        InternetGatewayDevice internetGatewayDevice, List<ushort> p2pReservedPorts, List<IPAddress> stunServerIpAddresses, CancellationToken cancellationToken)
    {
        bool? routerNatEnabled = null;
        IPAddress routerPublicIpV4Address = null;

        if (internetGatewayDevice is not null)
        {
            Task<bool?> natRsipStatusTask = internetGatewayDevice.GetNatRsipStatusAsync(cancellationToken);
            Task<IPAddress> externalIpv4AddressTask = internetGatewayDevice.GetExternalIpV4AddressAsync(cancellationToken);

            await ClientCore.Extensions.TaskExtensions.WhenAllSafe(new Task[] { natRsipStatusTask, externalIpv4AddressTask }).ConfigureAwait(false);

            routerNatEnabled = await natRsipStatusTask.ConfigureAwait(false);
            routerPublicIpV4Address = await externalIpv4AddressTask.ConfigureAwait(false);
        }

        (IPAddress stunPublicIpV4Address, List<(ushort InternalPort, ushort ExternalPort)> ipV4StunPortMapping) = await NetworkHelper.PerformStunAsync(
            stunServerIpAddresses, p2pReservedPorts, AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
        IPAddress tracePublicIpV4Address = null;

        if (routerPublicIpV4Address is null && stunPublicIpV4Address is null)
        {
            Logger.Log("P2P: Using IPV4 trace detection.");

            tracePublicIpV4Address = await NetworkHelper.TracePublicIpV4Address(cancellationToken).ConfigureAwait(false);
        }

        IPAddress localPublicIpV4Address = null;

        if (routerPublicIpV4Address is null && stunPublicIpV4Address is null && tracePublicIpV4Address is null)
        {
            Logger.Log("P2P: Using IPV4 local public address.");

            var localPublicIpAddresses = NetworkHelper.GetPublicIpAddresses().ToList();

            localPublicIpV4Address = localPublicIpAddresses.FirstOrDefault(q => q.AddressFamily is AddressFamily.InterNetwork);
        }

        IPAddress publicIpV4Address = stunPublicIpV4Address ?? routerPublicIpV4Address ?? tracePublicIpV4Address ?? localPublicIpV4Address;
        var ipV4P2PPorts = new List<(ushort InternalPort, ushort ExternalPort)>();

        if (publicIpV4Address is not null)
        {
            Logger.Log("P2P: Public IPV4 detected.");

            var privateIpV4Addresses = NetworkHelper.GetPrivateIpAddresses().Where(q => q.AddressFamily is AddressFamily.InterNetwork).ToList();
            IPAddress privateIpV4Address = privateIpV4Addresses.FirstOrDefault();

            if (internetGatewayDevice is not null && privateIpV4Address is not null && routerNatEnabled is not false)
            {
                Logger.Log("P2P: Using IPV4 port mapping.");

                try
                {
                    ipV4P2PPorts = (await ClientCore.Extensions.TaskExtensions.WhenAllSafe(p2pReservedPorts.Select(
                        q => internetGatewayDevice.OpenIpV4PortAsync(privateIpV4Address, q, cancellationToken))).ConfigureAwait(false)).Select(q => (q, q)).ToList();
                    p2pReservedPorts = ipV4P2PPorts.Select(q => q.InternalPort).ToList();
                }
                catch (Exception ex)
                {
#if DEBUG
                    ProgramConstants.LogException(ex, $"P2P: Could not open P2P IPV4 router ports for {privateIpV4Address} -> {publicIpV4Address}.");
#else
                    ProgramConstants.LogException(ex, $"P2P: Could not open P2P IPV4 router ports.");
#endif
                    ipV4P2PPorts = ipV4StunPortMapping.Any() ? ipV4StunPortMapping : p2pReservedPorts.Select(q => (q, q)).ToList();
                }
            }
            else
            {
                ipV4P2PPorts = ipV4StunPortMapping.Any() ? ipV4StunPortMapping : p2pReservedPorts.Select(q => (q, q)).ToList();
            }
        }

        return (publicIpV4Address, ipV4P2PPorts);
    }

    private static async ValueTask<IEnumerable<InternetGatewayDevice>> GetInternetGatewayDevices(CancellationToken cancellationToken)
    {
        IEnumerable<InternetGatewayDevice> devices = await GetDevicesAsync(cancellationToken).ConfigureAwait(false);

        return devices.Where(q => q?.UPnPDescription.Device.DeviceType?.StartsWith($"{UPnPConstants.UPnPInternetGatewayDevice}:", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static InternetGatewayDevice GetInternetGatewayDeviceByVersion(List<InternetGatewayDevice> internetGatewayDevices, ushort uPnPVersion)
        => internetGatewayDevices.FirstOrDefault(q => $"{UPnPConstants.UPnPInternetGatewayDevice}:{uPnPVersion}".Equals(q.UPnPDescription.Device.DeviceType, StringComparison.OrdinalIgnoreCase));

    private static async ValueTask<IEnumerable<InternetGatewayDevice>> GetDevicesAsync(CancellationToken cancellationToken)
    {
        IEnumerable<(IPAddress LocalIpAddress, IEnumerable<string> Responses)> rawDeviceResponses = await DetectDevicesAsync(cancellationToken).ConfigureAwait(false);
        IEnumerable<(IPAddress LocalIpAddress, IEnumerable<Dictionary<string, string>> Responses)> formattedDeviceResponses =
            rawDeviceResponses.Select(q => (q.LocalIpAddress, GetFormattedDeviceResponses(q.Responses)));
        IEnumerable<IGrouping<string, InternetGatewayDeviceResponse>> groupedInternetGatewayDeviceResponses =
            GetGroupedDeviceResponses(formattedDeviceResponses);

        return await ClientCore.Extensions.TaskExtensions.WhenAllSafe(
            groupedInternetGatewayDeviceResponses.Select(q => ParseDeviceAsync(q, cancellationToken))).ConfigureAwait(false);
    }

    private static IEnumerable<IGrouping<string, InternetGatewayDeviceResponse>> GetGroupedDeviceResponses(
        IEnumerable<(IPAddress LocalIpAddress, IEnumerable<Dictionary<string, string>> Responses)> formattedDeviceResponses)
        => formattedDeviceResponses
            .SelectMany(q => q.Responses.Where(r => Guid.TryParse(r["LOCATION"], out _)).Select(r => new InternetGatewayDeviceResponse(new(r["LOCATION"]), r["SERVER"], r["USN"], q.LocalIpAddress)))
            .GroupBy(q => q.Usn);

    private static Uri GetPreferredLocation(IReadOnlyCollection<Uri> locations)
    {
        return locations.FirstOrDefault(q => q.HostNameType is UriHostNameType.IPv6 && !NetworkHelper.IsPrivateIpAddress(IPAddress.Parse(q.IdnHost)))
            ?? locations.FirstOrDefault(q => q.HostNameType is UriHostNameType.IPv6 && NetworkHelper.IsPrivateIpAddress(IPAddress.Parse(q.IdnHost)))
            ?? locations.First(q => q.HostNameType is UriHostNameType.IPv4);
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

    private static async Task<(IPAddress LocalIpAddress, IEnumerable<string> Responses)> SearchDevicesAsync(IPAddress localAddress, IPAddress multicastAddress, CancellationToken cancellationToken)
    {
        var responses = new List<string>();
        using var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        var localEndPoint = new IPEndPoint(localAddress, 0);
        var multiCastIpEndPoint = new IPEndPoint(multicastAddress, UPnPConstants.UPnPMultiCastPort);

        try
        {
            socket.Bind(localEndPoint);

            string request = FormattableString.Invariant($"M-SEARCH * HTTP/1.1\r\nHOST: {NetworkHelper.FormatUri(multiCastIpEndPoint).Authority}\r\nST: {UPnPConstants.UPnPRootDevice}\r\nMAN: \"ssdp:discover\"\r\nMX: {ReceiveTimeoutInSeconds}\r\n\r\n");
            const int charSize = sizeof(char);
            int bufferSize = request.Length * charSize;
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
            Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
            int numberOfBytes = Encoding.UTF8.GetBytes(request.AsSpan(), buffer.Span);

            buffer = buffer[..numberOfBytes];

            await socket.SendToAsync(buffer, SocketFlags.None, multiCastIpEndPoint, cancellationToken).ConfigureAwait(false);
            await ReceiveAsync(socket, responses, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, $"P2P: Could not detect UPnP devices on {localEndPoint} / {multiCastIpEndPoint}.");
        }

        return new(localAddress, responses);
    }

    private static async ValueTask ReceiveAsync(Socket socket, ICollection<string> responses, CancellationToken cancellationToken)
    {
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);
        using var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(ReceiveTimeoutInSeconds));
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

        while (!linkedCancellationTokenSource.IsCancellationRequested)
        {
            Memory<byte> buffer = memoryOwner.Memory[..4096];

            try
            {
                int bytesReceived = await socket.ReceiveAsync(buffer, SocketFlags.None, linkedCancellationTokenSource.Token).ConfigureAwait(false);

                responses.Add(Encoding.UTF8.GetString(buffer.Span[..bytesReceived]));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    private static async ValueTask<UPnPDescription> GetDescriptionAsync(Uri uri, CancellationToken cancellationToken)
    {
        Stream uPnPDescription = await HttpClient.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);

        await using (uPnPDescription.ConfigureAwait(false))
        {
            using var xmlTextReader = new XmlTextReader(uPnPDescription);

            return (UPnPDescription)new DataContractSerializer(typeof(UPnPDescription)).ReadObject(xmlTextReader);
        }
    }

    private static async ValueTask<IEnumerable<(IPAddress LocalIpAddress, IEnumerable<string> Responses)>> DetectDevicesAsync(CancellationToken cancellationToken)
    {
        IEnumerable<IPAddress> unicastAddresses = NetworkHelper.GetLocalAddresses();
        IEnumerable<IPAddress> multicastAddresses = NetworkHelper.GetMulticastAddresses();
        (IPAddress LocalIpAddress, IEnumerable<string> Responses)[] localAddressesDeviceResponses = await ClientCore.Extensions.TaskExtensions.WhenAllSafe(
            multicastAddresses.SelectMany(q => unicastAddresses.Where(r => r.AddressFamily == q.AddressFamily).Select(r => SearchDevicesAsync(r, q, cancellationToken)))).ConfigureAwait(false);

        return localAddressesDeviceResponses.Where(q => q.Responses.Any(r => r.Any())).Select(q => (q.LocalIpAddress, q.Responses)).Distinct();
    }

    private static async Task<InternetGatewayDevice> ParseDeviceAsync(
        IGrouping<string, InternetGatewayDeviceResponse> internetGatewayDeviceResponses, CancellationToken cancellationToken)
    {
        Uri[] locations = null;

        try
        {
            locations = internetGatewayDeviceResponses.Select(q => (q.LocalIpAddress, q.Location)).Distinct().Select(ParseLocation).ToArray();

            Uri preferredLocation = GetPreferredLocation(locations);
            UPnPDescription uPnPDescription = default;

            try
            {
                uPnPDescription = await GetDescriptionAsync(preferredLocation, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (preferredLocation.HostNameType is UriHostNameType.IPv6 && locations.Any(q => q.HostNameType is UriHostNameType.IPv4))
                {
                    try
                    {
                        preferredLocation = locations.First(q => q.HostNameType is UriHostNameType.IPv4);
                        uPnPDescription = await GetDescriptionAsync(preferredLocation, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                    }
                }
            }

            return new(
                  locations,
                  internetGatewayDeviceResponses.Select(r => r.Server).Distinct().Single(),
                  uPnPDescription,
                  preferredLocation);
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, $"P2P: Could not get UPnP description from {locations?.Select(q => q.ToString()).DefaultIfEmpty().Aggregate((q, r) => $"{q} / {r}")}.");

            return null;
        }
    }

    private static Uri ParseLocation((IPAddress LocalIpAddress, Uri Location) location)
    {
        if (location.Location.HostNameType is not UriHostNameType.IPv6 || !IPAddress.TryParse(location.Location.IdnHost, out IPAddress ipAddress) || !NetworkHelper.IsPrivateIpAddress(ipAddress))
            return location.Location;

        return NetworkHelper.FormatUri(new(IPAddress.Parse(FormattableString.Invariant($"{location.Location.IdnHost}%{location.LocalIpAddress.ScopeId}")), location.Location.Port), location.Location.Scheme, location.Location.PathAndQuery);
    }
}