using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

internal static class NetworkHelper
{
    private const string PingHost = "cncnet.org";
    private const int PingTimeout = 1000;
    private const int MinimumUdpPort = 1024;

    private static readonly IReadOnlyCollection<AddressFamily> SupportedAddressFamilies = new[]
    {
        AddressFamily.InterNetwork,
        AddressFamily.InterNetworkV6
    }.AsReadOnly();

    [SupportedOSPlatform("windows")]
    public static IEnumerable<(IPAddress IpAddress, PrefixOrigin PrefixOrigin, SuffixOrigin SuffixOrigin)> GetWindowsPublicIpAddresses()
        => GetUniCastIpAddresses()
        .Where(q => !IsPrivateIpAddress(q.Address))
        .Select(q => (q.Address, q.PrefixOrigin, q.SuffixOrigin));

    public static IEnumerable<IPAddress> GetLocalAddresses()
        => GetUniCastIpAddresses()
        .Select(q => q.Address);

    public static IEnumerable<IPAddress> GetPublicIpAddresses()
        => GetLocalAddresses()
        .Where(q => !IsPrivateIpAddress(q));

    public static IEnumerable<IPAddress> GetPrivateIpAddresses()
        => GetLocalAddresses()
        .Where(IsPrivateIpAddress);

    public static IEnumerable<UnicastIPAddressInformation> GetUniCastIpAddresses()
        => NetworkInterface.GetAllNetworkInterfaces()
        .Where(q => q.OperationalStatus is OperationalStatus.Up)
        .Select(q => q.GetIPProperties())
        .Where(q => q.GatewayAddresses.Any())
        .SelectMany(q => q.UnicastAddresses)
        .Where(q => SupportedAddressFamilies.Contains(q.Address.AddressFamily));

    public static IPAddress GetIpV4BroadcastAddress(UnicastIPAddressInformation unicastIpAddressInformation)
    {
        uint ipAddress = BitConverter.ToUInt32(unicastIpAddressInformation.Address.GetAddressBytes(), 0);
        uint ipMaskV4 = BitConverter.ToUInt32(unicastIpAddressInformation.IPv4Mask.GetAddressBytes(), 0);
        uint broadCastIpAddress = ipAddress | ~ipMaskV4;

        return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
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
        if ((ipAddress.AddressFamily is AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
            || (ipAddress.AddressFamily is AddressFamily.InterNetwork && !Socket.OSSupportsIPv4))
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

    public static async ValueTask<IPEndPoint> PerformStunAsync(IPAddress stunServerIpAddress, ushort localPort, CancellationToken cancellationToken)
    {
        const short stunId = 26262;
        const int stunPort1 = 3478;
        const int stunPort2 = 8054;
        const int stunSize = 48;
        int[] stunPorts = { stunPort1, stunPort2 };
        using var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        short stunIdNetworkOrder = IPAddress.HostToNetworkOrder(stunId);
        using IMemoryOwner<byte> receiveMemoryOwner = MemoryPool<byte>.Shared.Rent(stunSize);
        Memory<byte> buffer = receiveMemoryOwner.Memory[..stunSize];

        if (!BitConverter.TryWriteBytes(buffer.Span, stunIdNetworkOrder))
            throw new();

        IPEndPoint stunServerIpEndPoint = null;
        int addressBytes = stunServerIpAddress.GetAddressBytes().Length;
        const int portBytes = sizeof(ushort);

        socket.Bind(new IPEndPoint(IPAddress.IPv6Any, localPort));

        foreach (int stunPort in stunPorts)
        {
            try
            {
                using var timeoutCancellationTokenSource = new CancellationTokenSource(PingTimeout);
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

                stunServerIpEndPoint = new IPEndPoint(stunServerIpAddress, stunPort);

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

                return new IPEndPoint(publicIpAddress, publicPort);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                ProgramConstants.LogException(ex, $"STUN server {stunServerIpEndPoint} failed.");
            }
        }

        return null;
    }

    public static async Task KeepStunAliveAsync(IPAddress stunServerIpAddress, List<ushort> localPorts, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (ushort localPort in localPorts)
                {
                    await PerformStunAsync(stunServerIpAddress, localPort, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log($"{stunServerIpAddress.AddressFamily} STUN keep alive stopped.");
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, "STUN keep alive failed.");
        }
    }

    /// <summary>
    /// Returns a free UDP port number above 1023.
    /// </summary>
    /// <param name="excludedPorts">List of UDP port numbers which are additionally excluded.</param>
    /// <param name="numberOfPorts">The number of free ports to return.</param>
    /// <returns>A free UDP port number on the current system.</returns>
    public static IEnumerable<ushort> GetFreeUdpPorts(IEnumerable<ushort> excludedPorts, ushort numberOfPorts)
    {
        IPEndPoint[] endPoints = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
        var activePorts = endPoints.Select(q => (ushort)q.Port).ToArray().Concat(excludedPorts).ToList();
        ushort foundPortCount = 0;

        while (foundPortCount != numberOfPorts)
        {
            ushort foundPort = (ushort)new Random().Next(MinimumUdpPort, IPEndPoint.MaxPort);

            if (!activePorts.Contains(foundPort))
            {
                activePorts.Add(foundPort);

                foundPortCount++;

                yield return foundPort;
            }
        }
    }

    private static bool IsPrivateIpAddress(IPAddress ipAddress)
        => ipAddress.AddressFamily switch
        {
            AddressFamily.InterNetworkV6 => ipAddress.IsIPv6SiteLocal
                || ipAddress.IsIPv6UniqueLocal
                || ipAddress.IsIPv6LinkLocal,
            AddressFamily.InterNetwork => IsInRange("10.0.0.0", "10.255.255.255", ipAddress)
                || IsInRange("172.16.0.0", "172.31.255.255", ipAddress)
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
}