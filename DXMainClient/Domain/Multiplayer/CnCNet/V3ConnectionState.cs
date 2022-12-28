using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet.Replays;
using DTAClient.Domain.Multiplayer.CnCNet.UPNP;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal sealed class V3ConnectionState : IAsyncDisposable
{
    private const ushort MAX_REMOTE_PLAYERS = 7;
    private const int PINNED_DYNAMIC_TUNNELS = 10;

    private readonly TunnelHandler tunnelHandler;
    private readonly List<(string RemotePlayerName, CnCNetTunnel Tunnel, int CombinedPing)> playerTunnels = new();
    private readonly ReplayHandler replayHandler = new();

    private IPAddress publicIpV4Address;
    private IPAddress publicIpV6Address;
    private List<ushort> p2pIpV6PortIds = new();
    private InternetGatewayDevice internetGatewayDevice;
    private List<PlayerInfo> playerInfos;
    private List<uint> gamePlayerIds;
    private List<(ushort InternalPort, ushort ExternalPort)> ipV6P2PPorts = new();
    private List<(ushort InternalPort, ushort ExternalPort)> ipV4P2PPorts = new();

    public List<(int Ping, string Hash)> PinnedTunnels { get; private set; } = new();

    public string PinnedTunnelPingsMessage { get; private set; }

    public bool DynamicTunnelsEnabled { get; set; }

    public bool P2PEnabled { get; set; }

    public bool RecordingEnabled { get; set; }

    public CnCNetTunnel InitialTunnel { get; private set; }

    public CancellationTokenSource StunCancellationTokenSource { get; private set; }

    public List<(List<string> RemotePlayerNames, V3GameTunnelHandler Tunnel)> V3GameTunnelHandlers { get; } = new();

    public List<P2PPlayer> P2PPlayers { get; } = new();

    public V3ConnectionState(TunnelHandler tunnelHandler)
    {
        this.tunnelHandler = tunnelHandler;
    }

    public void Setup(CnCNetTunnel tunnel)
    {
        InitialTunnel = tunnel;
        tunnelHandler.CurrentTunnel = !DynamicTunnelsEnabled ? InitialTunnel : GetEligibleTunnels().MinBy(q => q.PingInMs);
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
        if (!ipV6P2PPorts.Any() && !ipV4P2PPorts.Any())
        {
            StunCancellationTokenSource?.Cancel();
            StunCancellationTokenSource?.Dispose();

            StunCancellationTokenSource = new();

            var p2pPorts = NetworkHelper.GetFreeUdpPorts(Array.Empty<ushort>(), MAX_REMOTE_PLAYERS).ToList();

            try
            {
                (internetGatewayDevice, ipV6P2PPorts, ipV4P2PPorts, p2pIpV6PortIds, publicIpV6Address, publicIpV4Address) = await UPnPHandler.SetupPortsAsync(
                    internetGatewayDevice, p2pPorts, GetEligibleTunnels().SelectMany(q => q.IPAddresses).ToList(), StunCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        return publicIpV4Address is not null || publicIpV6Address is not null;
    }

    public void RemoveV3Player(string playerName)
    {
        playerTunnels.Remove(playerTunnels.SingleOrDefault(q => q.RemotePlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)));
        P2PPlayers.Remove(P2PPlayers.SingleOrDefault(q => q.RemotePlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)));
    }

    public string GetP2PRequestCommand()
        => $" {publicIpV4Address}\t{(!ipV4P2PPorts.Any() ? null : ipV4P2PPorts.Select(q => q.ExternalPort.ToString(CultureInfo.InvariantCulture)).Aggregate((q, r) => $"{q}-{r}"))}" +
        $";{publicIpV6Address}\t{(!ipV6P2PPorts.Any() ? null : ipV6P2PPorts.Select(q => q.ExternalPort.ToString(CultureInfo.InvariantCulture)).Aggregate((q, r) => $"{q}-{r}"))}";

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

    public async ValueTask<bool> ToggleRecordingAsync()
    {
        RecordingEnabled = !RecordingEnabled;

        if (RecordingEnabled)
            return true;

        await replayHandler.DisposeAsync().ConfigureAwait(false);

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
            remotePlayerIpV4Ports = ipV4splitLines[1].Split('-', StringSplitOptions.RemoveEmptyEntries).Select(q => ushort.Parse(q, CultureInfo.InvariantCulture)).ToArray();
        }

        if (parsedIpV6Address is not null)
        {
            remotePlayerP2PEnabled = true;
            remotePlayerIpV6Ports = ipV6splitLines[1].Split('-', StringSplitOptions.RemoveEmptyEntries).Select(q => ushort.Parse(q, CultureInfo.InvariantCulture)).ToArray();
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
        int uniqueGameId,
        uint gameLocalPlayerId,
        string localPlayerName,
        List<PlayerInfo> playerInfos,
        Action remoteHostConnectedAction,
        Action remoteHostConnectionFailedAction,
        CancellationToken cancellationToken = default)
    {
        this.playerInfos = playerInfos;

        V3GameTunnelHandlers.Clear();

        if (RecordingEnabled)
            replayHandler.SetupRecording(uniqueGameId, gameLocalPlayerId);

        if (!DynamicTunnelsEnabled)
        {
            SetupGameTunnelHandler(
                gameLocalPlayerId,
                remoteHostConnectedAction,
                remoteHostConnectionFailedAction,
                playerInfos.Where(q => !q.Name.Equals(localPlayerName, StringComparison.OrdinalIgnoreCase)).Select(q => q.Name).ToList(),
                new(tunnelHandler.CurrentTunnel.IPAddress, tunnelHandler.CurrentTunnel.Port),
                0,
                cancellationToken);
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

                    if (combinedPing < playerTunnels.Single(q => q.RemotePlayerName.Equals(remotePlayerName, StringComparison.OrdinalIgnoreCase)).CombinedPing)
                    {
                        ushort[] localPorts;
                        ushort[] remotePorts;

                        if (selectedRemoteIpAddress.AddressFamily is AddressFamily.InterNetworkV6)
                        {
                            localPorts = ipV6P2PPorts.Select(q => q.InternalPort).ToArray();
                            remotePorts = remoteIpV6Ports;
                        }
                        else
                        {
                            localPorts = ipV4P2PPorts.Select(q => q.InternalPort).ToArray();
                            remotePorts = remoteIpV4Ports;
                        }

                        var allPlayerNames = playerInfos.Select(q => q.Name).OrderBy(q => q, StringComparer.OrdinalIgnoreCase).ToList();
                        var remotePlayerNames = allPlayerNames.Where(q => !q.Equals(localPlayerName, StringComparison.OrdinalIgnoreCase)).ToList();
                        var tunnelClientPlayerNames = allPlayerNames.Where(q => !q.Equals(remotePlayerName, StringComparison.OrdinalIgnoreCase)).ToList();
                        ushort localPort = localPorts[6 - remotePlayerNames.FindIndex(q => q.Equals(remotePlayerName, StringComparison.OrdinalIgnoreCase))];
                        ushort remotePort = remotePorts[6 - tunnelClientPlayerNames.FindIndex(q => q.Equals(localPlayerName, StringComparison.OrdinalIgnoreCase))];

                        SetupGameTunnelHandler(
                            gameLocalPlayerId,
                            remoteHostConnectedAction,
                            remoteHostConnectionFailedAction,
                            new() { remotePlayerName },
                            new(selectedRemoteIpAddress, remotePort),
                            localPort,
                            cancellationToken);
                        p2pPlayerTunnels.Add(remotePlayerName);
                    }
                }
            }

            foreach (IGrouping<CnCNetTunnel, (string Name, CnCNetTunnel Tunnel, int CombinedPing)> tunnelGrouping in playerTunnels.Where(q => !p2pPlayerTunnels.Contains(q.RemotePlayerName, StringComparer.OrdinalIgnoreCase)).GroupBy(q => q.Tunnel))
            {
                SetupGameTunnelHandler(
                    gameLocalPlayerId,
                    remoteHostConnectedAction,
                    remoteHostConnectionFailedAction,
                    tunnelGrouping.Select(q => q.Name).ToList(),
                    new(tunnelGrouping.Key.IPAddress, tunnelGrouping.Key.Port),
                    0,
                    cancellationToken);
            }
        }
    }

    private void SetupGameTunnelHandler(
        uint gameLocalPlayerId,
        Action remoteHostConnectedAction,
        Action remoteHostConnectionFailedAction,
        List<string> remotePlayerNames,
        IPEndPoint remoteIpEndpoint,
        ushort localPort,
        CancellationToken cancellationToken)
    {
        var gameTunnelHandler = new V3GameTunnelHandler();

        gameTunnelHandler.RaiseRemoteHostConnectedEvent += (_, _) => remoteHostConnectedAction();
        gameTunnelHandler.RaiseRemoteHostConnectionFailedEvent += (_, _) => remoteHostConnectionFailedAction();

        if (RecordingEnabled)
        {
            gameTunnelHandler.RaiseRemoteHostDataReceivedEvent += replayHandler.RemoteHostConnection_DataReceivedAsync;
            gameTunnelHandler.RaiseLocalGameDataReceivedEvent += replayHandler.LocalGameConnection_DataReceivedAsync;
        }

        gameTunnelHandler.SetUp(remoteIpEndpoint, localPort, gameLocalPlayerId, cancellationToken);
        gameTunnelHandler.ConnectToTunnel();
        V3GameTunnelHandlers.Add(new(remotePlayerNames, gameTunnelHandler));
    }

    public List<ushort> StartPlayerConnections(List<uint> gamePlayerIds)
    {
        this.gamePlayerIds = gamePlayerIds;

        List<ushort> usedPorts = new(ipV4P2PPorts.Select(q => q.InternalPort).Concat(ipV6P2PPorts.Select(q => q.InternalPort)).Distinct());

        foreach ((List<string> remotePlayerNames, V3GameTunnelHandler v3GameTunnelHandler) in V3GameTunnelHandlers)
        {
            var currentTunnelPlayers = playerInfos.Where(q => remotePlayerNames.Contains(q.Name)).ToList();
            IEnumerable<int> indexes = currentTunnelPlayers.Select(q => q.Index);
            var playerIds = indexes.Select(q => gamePlayerIds[q]).ToList();
            var createdLocalPlayerPorts = v3GameTunnelHandler.CreatePlayerConnections(playerIds).ToList();
            int i = 0;

            foreach (PlayerInfo currentTunnelPlayer in currentTunnelPlayers)
                currentTunnelPlayer.Port = createdLocalPlayerPorts.Skip(i++).Take(1).Single();

            usedPorts.AddRange(createdLocalPlayerPorts);
        }

        foreach (V3GameTunnelHandler v3GameTunnelHandler in V3GameTunnelHandlers.Select(q => q.Tunnel))
            v3GameTunnelHandler.StartPlayerConnections();

        return usedPorts;
    }

    public async ValueTask SaveReplayAsync()
    {
        if (!RecordingEnabled)
            return;

        await replayHandler.StopRecordingAsync(gamePlayerIds, playerInfos, V3GameTunnelHandlers.Select(q => q.Tunnel).ToList()).ConfigureAwait(false);
    }

    public async ValueTask ClearConnectionsAsync()
    {
        if (replayHandler is not null)
            await replayHandler.DisposeAsync().ConfigureAwait(false);

        foreach (V3GameTunnelHandler v3GameTunnelHandler in V3GameTunnelHandlers.Select(q => q.Tunnel))
            v3GameTunnelHandler.Dispose();

        V3GameTunnelHandlers.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        PinnedTunnelPingsMessage = null;
        StunCancellationTokenSource?.Cancel();
        await ClearConnectionsAsync().ConfigureAwait(false);
        playerTunnels.Clear();
        P2PPlayers.Clear();
        PinnedTunnels?.Clear();
        await CloseP2PPortsAsync().ConfigureAwait(false);
    }

    public IEnumerable<CnCNetTunnel> GetEligibleTunnels()
        => tunnelHandler.Tunnels.Where(q => !q.RequiresPassword && q.PingInMs > -1 && q.Clients < q.MaxClients - 8 && q.Version is Constants.TUNNEL_VERSION_3);

    public string HandleTunnelPingsMessage(string playerName, string tunnelPingsMessage)
    {
        string[] tunnelPingsLines = tunnelPingsMessage.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<(int Ping, string Hash)> tunnelPings = tunnelPingsLines.Select(q =>
        {
            string[] split = q.Split(';');

            return (int.Parse(split[0], CultureInfo.InvariantCulture), split[1]);
        });
        IEnumerable<(int CombinedPing, string Hash)> combinedTunnelResults = tunnelPings
            .Where(q => PinnedTunnels.Select(r => r.Hash).Contains(q.Hash))
            .Select(q => (CombinedPing: q.Ping + PinnedTunnels.SingleOrDefault(r => q.Hash.Equals(r.Hash, StringComparison.OrdinalIgnoreCase)).Ping, q.Hash));
        (int combinedPing, string hash) = combinedTunnelResults
            .OrderBy(q => q.CombinedPing)
            .ThenBy(q => q.Hash, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (hash is null)
            return null;

        CnCNetTunnel tunnel = tunnelHandler.Tunnels.Single(q => q.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));

        playerTunnels.Remove(playerTunnels.SingleOrDefault(q => q.RemotePlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase)));
        playerTunnels.Add(new(playerName, tunnel, combinedPing));

        return hash;
    }

    private async ValueTask CloseP2PPortsAsync()
    {
        try
        {
            if (internetGatewayDevice is not null)
            {
                foreach (ushort p2pPort in ipV4P2PPorts.Select(q => q.InternalPort))
                    await internetGatewayDevice.CloseIpV4PortAsync(p2pPort).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            ProgramConstants.LogException(ex, "Could not close P2P IPV4 ports.");
        }
        finally
        {
            ipV4P2PPorts.Clear();
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
            ipV6P2PPorts.Clear();
            p2pIpV6PortIds.Clear();
        }
    }
}