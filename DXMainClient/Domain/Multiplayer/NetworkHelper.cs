using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

internal static class NetworkHelper
{
    private const string PingHost = "cncnet.org";
    private const int PingTimeout = 1000;

    private static readonly IReadOnlyCollection<AddressFamily> SupportedAddressFamilies = new[]
    {
        AddressFamily.InterNetwork,
        AddressFamily.InterNetworkV6
    }.AsReadOnly();

    public static bool HasIPv6Internet()
        => Socket.OSSupportsIPv6 && GetLocalPublicIpV6Address() is not null;

    public static bool HasIPv4Internet()
        => Socket.OSSupportsIPv4 && GetLocalAddresses().Any(q => q.AddressFamily is AddressFamily.InterNetwork);

    public static IEnumerable<IPAddress> GetLocalAddresses()
        => GetUniCastIpAddresses()
        .Select(q => q.Address);

    public static IEnumerable<IPAddress> GetPublicIpAddresses()
        => GetLocalAddresses()
        .Where(q => !IsPrivateIpAddress(q));

    public static IEnumerable<IPAddress> GetPrivateIpAddresses()
        => GetLocalAddresses()
        .Where(IsPrivateIpAddress);

    [SupportedOSPlatform("windows")]
    public static IEnumerable<UnicastIPAddressInformation> GetWindowsLanUniCastIpAddresses()
        => GetLanUniCastIpAddresses()
        .Where(q => q.SuffixOrigin is not SuffixOrigin.WellKnown);

    public static IEnumerable<UnicastIPAddressInformation> GetLanUniCastIpAddresses()
        => GetIpInterfaces()
        .SelectMany(q => q.UnicastAddresses)
        .Where(q => SupportedAddressFamilies.Contains(q.Address.AddressFamily));

    public static IEnumerable<IPAddress> GetMulticastAddresses()
        => GetIpInterfaces()
        .SelectMany(q => q.MulticastAddresses.Select(r => r.Address))
        .Where(q => SupportedAddressFamilies.Contains(q.AddressFamily));

    public static Uri FormatUri(string scheme, Uri uri, ushort port, string path)
    {
        string[] pathAndQuery = path.Split('?');
        var uriBuilder = new UriBuilder(uri)
        {
            Scheme = scheme,
            Host = uri.IdnHost,
            Port = port,
            Path = pathAndQuery.First(),
            Query = pathAndQuery.Skip(1).SingleOrDefault()
        };

        return uriBuilder.Uri;
    }

    public static Uri FormatUri(IPEndPoint ipEndPoint, string scheme = null, string path = null)
    {
        var uriBuilder = new UriBuilder(scheme ?? Uri.UriSchemeHttps, ipEndPoint.Address.ToString(), ipEndPoint.Port, path);

        return uriBuilder.Uri;
    }

    private static IEnumerable<UnicastIPAddressInformation> GetUniCastIpAddresses()
        => GetIpInterfaces()
        .SelectMany(q => q.UnicastAddresses)
        .Where(q => SupportedAddressFamilies.Contains(q.Address.AddressFamily));

    private static IEnumerable<IPInterfaceProperties> GetIpInterfaces()
        => NetworkInterface.GetAllNetworkInterfaces()
        .Where(q => q.OperationalStatus is OperationalStatus.Up && q.NetworkInterfaceType is not NetworkInterfaceType.Loopback)
        .Select(q => q.GetIPProperties())
        .Where(q => q.GatewayAddresses.Any());

    [SupportedOSPlatform("windows")]
    private static IEnumerable<(IPAddress IpAddress, PrefixOrigin PrefixOrigin, SuffixOrigin SuffixOrigin)> GetWindowsPublicIpAddresses()
        => GetUniCastIpAddresses()
        .Where(q => !IsPrivateIpAddress(q.Address))
        .Select(q => (q.Address, q.PrefixOrigin, q.SuffixOrigin));

    public static IPAddress GetIpV4BroadcastAddress(UnicastIPAddressInformation unicastIpAddressInformation)
    {
        uint ipAddress = BitConverter.ToUInt32(unicastIpAddressInformation.Address.GetAddressBytes(), 0);
        uint ipMaskV4 = BitConverter.ToUInt32(unicastIpAddressInformation.IPv4Mask.GetAddressBytes(), 0);
        uint broadCastIpAddress = ipAddress | ~ipMaskV4;

        return new(BitConverter.GetBytes(broadCastIpAddress));
    }

    public static IPAddress GetLocalPublicIpV6Address()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetPublicIpAddresses().FirstOrDefault(q => q.AddressFamily is AddressFamily.InterNetworkV6);

        var localIpV6Addresses = GetWindowsPublicIpAddresses()
            .Where(q => q.IpAddress.AddressFamily is AddressFamily.InterNetworkV6).ToList();

        (IPAddress IpAddress, PrefixOrigin PrefixOrigin, SuffixOrigin SuffixOrigin) foundLocalPublicIpV6Address = localIpV6Addresses
            .FirstOrDefault(q => q.PrefixOrigin is PrefixOrigin.RouterAdvertisement && q.SuffixOrigin is SuffixOrigin.LinkLayerAddress);

        if (foundLocalPublicIpV6Address.IpAddress is null)
        {
            foundLocalPublicIpV6Address = localIpV6Addresses.FirstOrDefault(
                q => q.PrefixOrigin is PrefixOrigin.Dhcp && q.SuffixOrigin is SuffixOrigin.OriginDhcp);
        }

        return foundLocalPublicIpV6Address.IpAddress;
    }

    public static async ValueTask<IPAddress> TracePublicIpV4Address(CancellationToken cancellationToken)
    {
        try
        {
            IPAddress[] ipAddresses = await Dns.GetHostAddressesAsync(PingHost, cancellationToken).ConfigureAwait(false);
            using var ping = new Ping();

            foreach (IPAddress ipAddress in ipAddresses.Where(q => q.AddressFamily is AddressFamily.InterNetwork))
            {
                PingReply pingReply = await ping.SendPingAsync(ipAddress, PingTimeout).ConfigureAwait(false);

                if (pingReply.Status is not IPStatus.Success)
                    continue;

                IPAddress pingIpAddress = null;
                int ttl = 1;

                while (!ipAddress.Equals(pingIpAddress))
                {
                    pingReply = await ping.SendPingAsync(ipAddress, PingTimeout, Array.Empty<byte>(), new(ttl++, false)).ConfigureAwait(false);
                    pingIpAddress = pingReply.Address;

                    if (ipAddress.Equals(pingIpAddress))
                        break;

                    if (!IsPrivateIpAddress(pingReply.Address))
                        return pingReply.Address;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ProgramConstants.LogException(ex, "IP trace detection failed.");
        }

        return null;
    }

    public static async ValueTask<long?> PingAsync(IPAddress ipAddress)
    {
        if ((ipAddress.AddressFamily is AddressFamily.InterNetworkV6 && !HasIPv6Internet())
            || (ipAddress.AddressFamily is AddressFamily.InterNetwork && !HasIPv4Internet()))
        {
            return null;
        }

        using var ping = new Ping();

        try
        {
            PingReply pingResult = await ping.SendPingAsync(ipAddress, PingTimeout).ConfigureAwait(false);

            if (pingResult.Status is IPStatus.Success)
                return pingResult.RoundtripTime;
        }
        catch (PingException ex)
        {
            ProgramConstants.LogException(ex, "Ping failed.");
        }

        return null;
    }

    public static async ValueTask<(IPAddress IPAddress, List<(ushort InternalPort, ushort ExternalPort)> PortMapping)> PerformStunAsync(
        List<IPAddress> stunServerIpAddresses, List<ushort> p2pReservedPorts, AddressFamily addressFamily, CancellationToken cancellationToken)
    {
        Logger.Log($"P2P: Using STUN to detect {addressFamily} address.");

        var stunPortMapping = new List<(ushort InternalPort, ushort ExternalPort)>();
        var matchingStunServerIpAddresses = stunServerIpAddresses.Where(q => q.AddressFamily == addressFamily).ToList();

        if (!matchingStunServerIpAddresses.Any())
        {
            Logger.Log($"P2P: No {addressFamily} STUN servers found.");

            return (null, stunPortMapping);
        }

        IPAddress stunPublicAddress = null;
        IPAddress stunServerIpAddress = null;

        foreach (IPAddress matchingStunServerIpAddress in matchingStunServerIpAddresses.TakeWhile(_ => stunPublicAddress is null))
        {
            stunServerIpAddress = matchingStunServerIpAddress;

            foreach (ushort p2pReservedPort in p2pReservedPorts)
            {
                IPEndPoint stunPublicIpEndPoint = await PerformStunAsync(
                    stunServerIpAddress, p2pReservedPort, addressFamily, cancellationToken).ConfigureAwait(false);

                if (stunPublicIpEndPoint is null)
                    break;

                stunPublicAddress = stunPublicIpEndPoint.Address;

                if (p2pReservedPort != stunPublicIpEndPoint.Port)
                    stunPortMapping.Add(new(p2pReservedPort, (ushort)stunPublicIpEndPoint.Port));
            }
        }

        if (stunPublicAddress is not null)
            Logger.Log($"P2P: {addressFamily} STUN detection succeeded using server {stunServerIpAddress}.");
        else
            Logger.Log($"P2P: {addressFamily} STUN detection failed.");

        if (stunPortMapping.Any())
        {
            Logger.Log($"P2P: {addressFamily} STUN detection detected mapped ports, running STUN keep alive.");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            KeepStunAliveAsync(
                stunServerIpAddress,
                stunPortMapping.Select(q => q.InternalPort).ToList(), cancellationToken).HandleTask();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        return (stunPublicAddress, stunPortMapping);
    }

    /// <summary>
    /// Returns the specified amount of free UDP port numbers.
    /// </summary>
    /// <param name="excludedPorts">List of UDP port numbers which are additionally excluded.</param>
    /// <param name="numberOfPorts">The number of free ports to return.</param>
    /// <returns>A free UDP port number on the current system.</returns>
    public static IEnumerable<ushort> GetFreeUdpPorts(IEnumerable<ushort> excludedPorts, ushort numberOfPorts)
    {
        IPEndPoint[] endPoints = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
        var activeV4AndV6Ports = endPoints.Select(q => (ushort)q.Port).ToArray().Concat(excludedPorts).Distinct().ToList();
        ushort foundPortCount = 0;

        while (foundPortCount != numberOfPorts)
        {
            using var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            ushort foundPort = (ushort)((IPEndPoint)socket.LocalEndPoint).Port;

            if (!activeV4AndV6Ports.Contains(foundPort))
            {
                activeV4AndV6Ports.Add(foundPort);

                foundPortCount++;

                yield return foundPort;
            }
        }
    }

    public static bool IsPrivateIpAddress(IPAddress ipAddress)
        => ipAddress.AddressFamily switch
        {
            AddressFamily.InterNetworkV6 => ipAddress.IsIPv6SiteLocal
                || ipAddress.IsIPv6UniqueLocal
                || ipAddress.IsIPv6LinkLocal,
            AddressFamily.InterNetwork => IsInRange("10.0.0.0", "10.255.255.255", ipAddress)
                || IsInRange("172.16.0.0", "172.31.255.255", ipAddress)
                || IsInRange("192.168.0.0", "192.168.255.255", ipAddress)
                || IsInRange("169.254.0.0", "169.254.255.255", ipAddress)
                || IsInRange("127.0.0.0", "127.255.255.255", ipAddress)
                || IsInRange("0.0.0.0", "0.255.255.255", ipAddress),
            _ => throw new ArgumentOutOfRangeException(nameof(ipAddress.AddressFamily), ipAddress.AddressFamily, null),
        };

    private static bool IsInRange(string startIpAddress, string endIpAddress, IPAddress address)
    {
        uint ipStart = BitConverter.ToUInt32(IPAddress.Parse(startIpAddress).GetAddressBytes().Reverse().ToArray(), 0);
        uint ipEnd = BitConverter.ToUInt32(IPAddress.Parse(endIpAddress).GetAddressBytes().Reverse().ToArray(), 0);
        uint ip = BitConverter.ToUInt32(address.GetAddressBytes().Reverse().ToArray(), 0);

        return ip >= ipStart && ip <= ipEnd;
    }

    private static async ValueTask<IPEndPoint> PerformStunAsync(IPAddress stunServerIpAddress, ushort localPort, AddressFamily addressFamily, CancellationToken cancellationToken)
    {
        const short stunId = 26262;
        const int stunPort1 = 3478;
        const int stunPort2 = 8054;
        const int stunSize = 48;
        int[] stunPorts = { stunPort1, stunPort2 };
        using var socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
        short stunIdNetworkOrder = IPAddress.HostToNetworkOrder(stunId);
        using IMemoryOwner<byte> receiveMemoryOwner = MemoryPool<byte>.Shared.Rent(stunSize);
        Memory<byte> buffer = receiveMemoryOwner.Memory[..stunSize];

        if (!BitConverter.TryWriteBytes(buffer.Span, stunIdNetworkOrder))
            throw new();

        IPEndPoint stunServerIpEndPoint = null;
        int addressBytes = stunServerIpAddress.GetAddressBytes().Length;
        const int portBytes = sizeof(ushort);

        socket.Bind(new IPEndPoint(addressFamily is AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, localPort));

        foreach (int stunPort in stunPorts)
        {
            try
            {
                using var timeoutCancellationTokenSource = new CancellationTokenSource(PingTimeout);
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

                stunServerIpEndPoint = new(stunServerIpAddress, stunPort);

                await socket.SendToAsync(buffer, stunServerIpEndPoint, linkedCancellationTokenSource.Token).ConfigureAwait(false);

                SocketReceiveFromResult socketReceiveFromResult = await socket.ReceiveFromAsync(
                    buffer, SocketFlags.None, stunServerIpEndPoint, linkedCancellationTokenSource.Token).ConfigureAwait(false);

                buffer = buffer[..socketReceiveFromResult.ReceivedBytes];

                // de-obfuscate
                for (int i = 0; i < addressBytes + portBytes; i++)
                    buffer.Span[i] ^= 0x20;

                ReadOnlyMemory<byte> publicIpAddressBytes = buffer[..addressBytes];
                var publicIpAddress = new IPAddress(publicIpAddressBytes.Span);
                ReadOnlyMemory<byte> publicPortBytes = buffer[addressBytes..(addressBytes + portBytes)];
                short publicPortNetworkOrder = BitConverter.ToInt16(publicPortBytes.Span);
                short publicPortHostOrder = IPAddress.NetworkToHostOrder(publicPortNetworkOrder);
                ushort publicPort = (ushort)publicPortHostOrder;

                return new(publicIpAddress, publicPort);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                Logger.Log($"P2P: STUN server {stunServerIpEndPoint} unreachable.");
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, $"P2P: STUN server {stunServerIpEndPoint} unreachable.");
            }
        }

        return null;
    }

    private static async Task KeepStunAliveAsync(IPAddress stunServerIpAddress, List<ushort> localPorts, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (ushort localPort in localPorts)
                {
                    await PerformStunAsync(stunServerIpAddress, localPort, stunServerIpAddress.AddressFamily, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log($"P2P: {stunServerIpAddress.AddressFamily} STUN keep alive stopped.");
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, "P2P: STUN keep alive failed.");
        }
    }
}