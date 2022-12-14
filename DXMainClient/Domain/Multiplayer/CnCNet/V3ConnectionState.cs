using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet.UPNP;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal sealed class V3ConnectionState : IAsyncDisposable
{
    private const ushort MAX_REMOTE_PLAYERS = 7;
    private const int PINNED_DYNAMIC_TUNNELS = 10;

    private readonly TunnelHandler tunnelHandler;

    private IPAddress publicIpV4Address;
    private IPAddress publicIpV6Address;
    private List<ushort> p2pIpV6PortIds = new();
    private InternetGatewayDevice internetGatewayDevice;

    public List<(ushort InternalPort, ushort ExternalPort)> IpV6P2PPorts { get; private set; } = new();

    public List<(ushort InternalPort, ushort ExternalPort)> IpV4P2PPorts { get; private set; } = new();

    public List<(int Ping, string Hash)> PinnedTunnels { get; private set; } = new();

    public string PinnedTunnelPingsMessage { get; private set; }

    public bool DynamicTunnelsEnabled { get; set; }

    public bool P2PEnabled { get; set; }

    public CnCNetTunnel InitialTunnel { get; private set; }

    public CancellationTokenSource StunCancellationTokenSource { get; private set; }

    public List<(string RemotePlayerName, CnCNetTunnel Tunnel, int CombinedPing)> PlayerTunnels { get; } = new();

    public List<(List<string> RemotePlayerNames, V3GameTunnelHandler Tunnel)> V3GameTunnelHandlers { get; } = new();

    public List<P2PPlayer> P2PPlayers { get; } = new();

    public V3ConnectionState(TunnelHandler tunnelHandler)
    {
        this.tunnelHandler = tunnelHandler;
    }

    public void Setup(CnCNetTunnel tunnel)
    {
        InitialTunnel = tunnel;

        if (!DynamicTunnelsEnabled)
        {
            tunnelHandler.CurrentTunnel = InitialTunnel;
        }
        else
        {
            tunnelHandler.CurrentTunnel = GetEligibleTunnels()
                .MinBy(q => q.PingInMs);
        }
    }

    public void PinTunnels()
    {
        PinnedTunnels = GetEligibleTunnels()
            .OrderBy(q => q.PingInMs)
            .ThenBy(q => q.Hash, StringComparer.OrdinalIgnoreCase)
            .Take(PINNED_DYNAMIC_TUNNELS)
            .Select(q => (q.PingInMs, q.Hash))
            .ToList();

        IEnumerable<string> tunnelPings = PinnedTunnels
            .Select(q => FormattableString.Invariant($"{q.Ping};{q.Hash}\t"));

        PinnedTunnelPingsMessage = string.Concat(tunnelPings);
    }

    public async ValueTask<bool> HandlePlayerP2PRequestAsync()
    {
        if (!IpV6P2PPorts.Any() && !IpV4P2PPorts.Any())
        {
            var p2pPorts = NetworkHelper.GetFreeUdpPorts(Array.Empty<ushort>(), MAX_REMOTE_PLAYERS).ToList();

            StunCancellationTokenSource?.Cancel();
            StunCancellationTokenSource?.Dispose();

            StunCancellationTokenSource = new();

            (internetGatewayDevice, IpV6P2PPorts, IpV4P2PPorts, p2pIpV6PortIds, publicIpV6Address, publicIpV4Address) = await UPnPHandler.SetupPortsAsync(
                internetGatewayDevice, p2pPorts, tunnelHandler.CurrentTunnel?.IPAddresses ?? InitialTunnel.IPAddresses, StunCancellationTokenSource.Token).ConfigureAwait(false);
        }

        return publicIpV4Address is not null || publicIpV6Address is not null;
    }

    public void RemoveV3Player(string playerName)
    {
        PlayerTunnels.Remove(PlayerTunnels.SingleOrDefault(q => q.RemotePlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)));
        P2PPlayers.Remove(P2PPlayers.SingleOrDefault(q => q.RemotePlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)));
    }

    public string GetP2PRequestCommand()
        => $" {publicIpV4Address}\t{(!IpV4P2PPorts.Any() ? null : IpV4P2PPorts.Select(q => q.ExternalPort.ToString(CultureInfo.InvariantCulture)).Aggregate((q, r) => $"{q}-{r}"))}" +
        $";{publicIpV6Address}\t{(!IpV6P2PPorts.Any() ? null : IpV6P2PPorts.Select(q => q.ExternalPort.ToString(CultureInfo.InvariantCulture)).Aggregate((q, r) => $"{q}-{r}"))}";

    public string GetP2PPingCommand(string playerName)
        => $" {playerName}-{P2PPlayers.Single(q => q.RemotePlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)).LocalPingResults.Select(q => $"{q.RemoteIpAddress};{q.Ping}\t").Aggregate((q, r) => $"{q}{r}")}";

    public async ValueTask<bool> ToggleP2PAsync()
    {
        P2PEnabled = !P2PEnabled;

        if (P2PEnabled)
            return true;

        await CloseP2PPortsAsync().ConfigureAwait(false);

        internetGatewayDevice = null;
        publicIpV4Address = null;
        publicIpV6Address = null;

        return false;
    }

    public async ValueTask<bool> PingRemotePlayer(string playerName, string p2pRequestMessage)
    {
        List<(IPAddress RemoteIpAddress, long Ping)> localPingResults = new();
        string[] splitLines = p2pRequestMessage.Split(';');
        string[] ipV4splitLines = splitLines[0].Split('\t');
        string[] ipV6splitLines = splitLines[1].Split('\t');

        if (IPAddress.TryParse(ipV4splitLines[0], out IPAddress parsedIpV4Address))
        {
            long? pingResult = await NetworkHelper.PingAsync(parsedIpV4Address).ConfigureAwait(false);

            if (pingResult is not null)
                localPingResults.Add((parsedIpV4Address, pingResult.Value));
        }

        if (IPAddress.TryParse(ipV6splitLines[0], out IPAddress parsedIpV6Address))
        {
            long? pingResult = await NetworkHelper.PingAsync(parsedIpV6Address).ConfigureAwait(false);

            if (pingResult is not null)
                localPingResults.Add((parsedIpV6Address, pingResult.Value));
        }

        bool remotePlayerP2PEnabled = false;
        ushort[] remotePlayerIpV4Ports = Array.Empty<ushort>();
        ushort[] remotePlayerIpV6Ports = Array.Empty<ushort>();
        P2PPlayer remoteP2PPlayer;

        if (parsedIpV4Address is not null)
        {
            remotePlayerP2PEnabled = true;
            remotePlayerIpV4Ports = ipV4splitLines[1].Split('-').Select(q => ushort.Parse(q, CultureInfo.InvariantCulture)).ToArray();
        }

        if (parsedIpV6Address is not null)
        {
            remotePlayerP2PEnabled = true;
            remotePlayerIpV6Ports = ipV6splitLines[1].Split('-').Select(q => ushort.Parse(q, CultureInfo.InvariantCulture)).ToArray();
        }

        if (P2PPlayers.Any(q => q.RemotePlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
        {
            remoteP2PPlayer = P2PPlayers.Single(q => q.RemotePlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));

            P2PPlayers.Remove(remoteP2PPlayer);
        }
        else
        {
            remoteP2PPlayer = new(playerName, Array.Empty<ushort>(), Array.Empty<ushort>(), new(), new(), false);
        }

        P2PPlayers.Add(remoteP2PPlayer with { LocalPingResults = localPingResults, RemoteIpV6Ports = remotePlayerIpV6Ports, RemoteIpV4Ports = remotePlayerIpV4Ports, Enabled = remotePlayerP2PEnabled });

        return remotePlayerP2PEnabled;
    }

    public bool UpdateRemotePingResults(string senderName, string p2pPingsMessage, string localPlayerName)
    {
        if (!P2PEnabled)
            return false;

        string[] splitLines = p2pPingsMessage.Split('-');
        string pingPlayerName = splitLines[0];

        if (!localPlayerName.Equals(pingPlayerName, StringComparison.OrdinalIgnoreCase))
            return false;

        string[] pingResults = splitLines[1].Split('\t', StringSplitOptions.RemoveEmptyEntries);
        List<(IPAddress IpAddress, long Ping)> playerPings = new();

        foreach (string pingResult in pingResults)
        {
            string[] ipAddressPingResult = pingResult.Split(';');

            if (IPAddress.TryParse(ipAddressPingResult[0], out IPAddress ipV4Address))
                playerPings.Add((ipV4Address, long.Parse(ipAddressPingResult[1], CultureInfo.InvariantCulture)));
        }

        P2PPlayer p2pPlayer;

        if (P2PPlayers.Any(q => q.RemotePlayerName.Equals(senderName, StringComparison.OrdinalIgnoreCase)))
        {
            p2pPlayer = P2PPlayers.Single(q => q.RemotePlayerName.Equals(senderName, StringComparison.OrdinalIgnoreCase));

            P2PPlayers.Remove(p2pPlayer);
        }
        else
        {
            p2pPlayer = new(senderName, Array.Empty<ushort>(), Array.Empty<ushort>(), new(), new(), false);
        }

        P2PPlayers.Add(p2pPlayer with { RemotePingResults = playerPings });

        return !p2pPlayer.RemotePingResults.Any();
    }

    public void StartV3ConnectionListeners(
        uint gameLocalPlayerId,
        string localPlayerName,
        List<PlayerInfo> players,
        Action remoteHostConnectedAction,
        Action remoteHostConnectionFailedAction,
        CancellationToken cancellationToken)
    {
        V3GameTunnelHandlers.Clear();

        if (!DynamicTunnelsEnabled)
        {
            var gameTunnelHandler = new V3GameTunnelHandler();

            gameTunnelHandler.RaiseRemoteHostConnectedEvent += (_, _) => remoteHostConnectedAction();
            gameTunnelHandler.RaiseRemoteHostConnectionFailedEvent += (_, _) => remoteHostConnectionFailedAction();

            gameTunnelHandler.SetUp(new(tunnelHandler.CurrentTunnel.IPAddress, tunnelHandler.CurrentTunnel.Port), 0, gameLocalPlayerId, cancellationToken);
            gameTunnelHandler.ConnectToTunnel();
            V3GameTunnelHandlers.Add(new(players.Where(q => !q.Name.Equals(localPlayerName, StringComparison.OrdinalIgnoreCase)).Select(q => q.Name).ToList(), gameTunnelHandler));
        }
        else
        {
            List<string> p2pPlayerTunnels = new();

            if (P2PEnabled)
            {
                foreach (var (remotePlayerName, remoteIpV6Ports, remoteIpV4Ports, localPingResults, remotePingResults, _) in P2PPlayers.Where(q => q.RemotePingResults.Any() && q.Enabled))
                {
                    (IPAddress selectedRemoteIpAddress, long combinedPing) = localPingResults
                        .Where(q => q.RemoteIpAddress is not null && remotePingResults
                            .Where(r => r.RemoteIpAddress is not null)
                            .Select(r => r.RemoteIpAddress.AddressFamily)
                            .Contains(q.RemoteIpAddress.AddressFamily))
                        .Select(q => (q.RemoteIpAddress, q.Ping + remotePingResults.Single(r => r.RemoteIpAddress.AddressFamily == q.RemoteIpAddress.AddressFamily).Ping))
                        .MaxBy(q => q.RemoteIpAddress.AddressFamily);

                    if (combinedPing < PlayerTunnels.Single(q => q.RemotePlayerName.Equals(remotePlayerName, StringComparison.OrdinalIgnoreCase)).CombinedPing)
                    {
                        ushort[] localPorts;
                        ushort[] remotePorts;

                        if (selectedRemoteIpAddress.AddressFamily is AddressFamily.InterNetworkV6)
                        {
                            localPorts = IpV6P2PPorts.Select(q => q.InternalPort).ToArray();
                            remotePorts = remoteIpV6Ports;
                        }
                        else
                        {
                            localPorts = IpV4P2PPorts.Select(q => q.InternalPort).ToArray();
                            remotePorts = remoteIpV4Ports;
                        }

                        var allPlayerNames = players.Select(q => q.Name).OrderBy(q => q, StringComparer.OrdinalIgnoreCase).ToList();
                        var remotePlayerNames = allPlayerNames.Where(q => !q.Equals(localPlayerName, StringComparison.OrdinalIgnoreCase)).ToList();
                        var tunnelClientPlayerNames = allPlayerNames.Where(q => !q.Equals(remotePlayerName, StringComparison.OrdinalIgnoreCase)).ToList();
                        ushort localPort = localPorts[6 - remotePlayerNames.FindIndex(q => q.Equals(remotePlayerName, StringComparison.OrdinalIgnoreCase))];
                        ushort remotePort = remotePorts[6 - tunnelClientPlayerNames.FindIndex(q => q.Equals(localPlayerName, StringComparison.OrdinalIgnoreCase))];
                        var p2pLocalTunnelHandler = new V3GameTunnelHandler();

                        p2pLocalTunnelHandler.RaiseRemoteHostConnectedEvent += (_, _) => remoteHostConnectedAction();
                        p2pLocalTunnelHandler.RaiseRemoteHostConnectionFailedEvent += (_, _) => remoteHostConnectionFailedAction();

                        p2pLocalTunnelHandler.SetUp(new(selectedRemoteIpAddress, remotePort), localPort, gameLocalPlayerId, cancellationToken);
                        p2pLocalTunnelHandler.ConnectToTunnel();
                        V3GameTunnelHandlers.Add(new(new() { remotePlayerName }, p2pLocalTunnelHandler));
                        p2pPlayerTunnels.Add(remotePlayerName);
                    }
                }
            }

            foreach (IGrouping<CnCNetTunnel, (string Name, CnCNetTunnel Tunnel, int CombinedPing)> tunnelGrouping in PlayerTunnels.Where(q => !p2pPlayerTunnels.Contains(q.RemotePlayerName, StringComparer.OrdinalIgnoreCase)).GroupBy(q => q.Tunnel))
            {
                var gameTunnelHandler = new V3GameTunnelHandler();

                gameTunnelHandler.RaiseRemoteHostConnectedEvent += (_, _) => remoteHostConnectedAction();
                gameTunnelHandler.RaiseRemoteHostConnectionFailedEvent += (_, _) => remoteHostConnectionFailedAction();

                gameTunnelHandler.SetUp(new(tunnelGrouping.Key.IPAddress, tunnelGrouping.Key.Port), 0, gameLocalPlayerId, cancellationToken);
                gameTunnelHandler.ConnectToTunnel();
                V3GameTunnelHandlers.Add(new(tunnelGrouping.Select(q => q.Name).ToList(), gameTunnelHandler));
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        PinnedTunnelPingsMessage = null;
        StunCancellationTokenSource?.Cancel();
        V3GameTunnelHandlers.ForEach(q => q.Tunnel.Dispose());
        V3GameTunnelHandlers.Clear();
        PlayerTunnels.Clear();
        P2PPlayers.Clear();
        PinnedTunnels?.Clear();
        await CloseP2PPortsAsync().ConfigureAwait(false);
    }

    private IEnumerable<CnCNetTunnel> GetEligibleTunnels()
        => tunnelHandler.Tunnels.Where(q => !q.RequiresPassword && q.PingInMs > -1 && q.Clients < q.MaxClients - 8 && q.Version is Constants.TUNNEL_VERSION_3);

    private async ValueTask CloseP2PPortsAsync()
    {
        try
        {
            if (internetGatewayDevice is not null)
            {
                foreach (ushort p2pPort in IpV4P2PPorts.Select(q => q.InternalPort))
                    await internetGatewayDevice.CloseIpV4PortAsync(p2pPort).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, "Could not close P2P IPV4 ports.");
        }
        finally
        {
            IpV4P2PPorts.Clear();
        }

        try
        {
            if (internetGatewayDevice is not null)
            {
                foreach (ushort p2pIpV6PortId in p2pIpV6PortIds)
                    await internetGatewayDevice.CloseIpV6PortAsync(p2pIpV6PortId).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, "Could not close P2P IPV6 ports.");
        }
        finally
        {
            IpV6P2PPorts.Clear();
            p2pIpV6PortIds.Clear();
        }
    }
}