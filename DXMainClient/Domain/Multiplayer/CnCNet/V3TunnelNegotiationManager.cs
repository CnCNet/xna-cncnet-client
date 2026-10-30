#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using ClientCore;
using ClientCore.Extensions;

using Microsoft.Xna.Framework;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Orchestrates V3 dynamic-tunnel connections for a lobby: keeps one <see cref="V3PairLink"/>
/// per remote player, reflects their state in the lobby, exchanges status over IRC and prepares
/// the data needed to start the game bridge. Shared by CnCNetGameLobby and
/// CnCNetGameLoadingLobby, which live on separate inheritance trees.
/// </summary>
/// <remarks>
/// The links connect, monitor, fail over and retry on their own; this class only starts and stops
/// them and turns their events into lobby state. Link events arrive on the links' threads and are
/// marshalled to the game thread before touching anything here.
/// </remarks>
public class V3TunnelNegotiationManager
{
    private readonly IV3NegotiationHost host;
    private readonly TunnelHandler tunnelHandler;
    private readonly WindowManager windowManager;
    private readonly List<V3PlayerInfo> _v3PlayerInfos = new();
    private readonly NegotiationDataManager _negotiationData = new();
    private readonly Dictionary<string, int> _lastReportedPings = new();

    // Links of players who left while a game route was active: the bridge may still be
    // forwarding to them (they may only have lost IRC), so they keep running until it stops.
    private readonly List<V3PairLink> _deferredLinkDisposals = new();

    private volatile ConcurrentDictionary<uint, List<uint>>? _probeReports;

    public V3TunnelNegotiationManager(IV3NegotiationHost host, TunnelHandler tunnelHandler, WindowManager windowManager)
    {
        this.host = host;
        this.tunnelHandler = tunnelHandler;
        this.windowManager = windowManager;

        // Both the game lobby's and the loading lobby's managers subscribe; the one not in use
        // has no check running, so its handler ignores the reports.
        tunnelHandler.ProbeReportReceived += OnProbeReportReceived;
        tunnelHandler.GameBridgeStopped += FlushDeferredLinkDisposals;
    }

    public IReadOnlyList<V3PlayerInfo> PlayerInfos => _v3PlayerInfos;

    public NegotiationDataManager NegotiationData => _negotiationData;

    /// <summary>
    /// True while any local connection with a remote player is still being set up. Used to
    /// prevent overlapping renegotiation rounds.
    /// </summary>
    public bool HasActiveNegotiations => _v3PlayerInfos.Any(p => p.IsNegotiating);

    public V3PlayerInfo? FindPlayer(string name) => _v3PlayerInfos.FirstOrDefault(p => p.Name == name);

    /// <summary>
    /// Derives a deterministic player ID that all clients can compute without communicating.
    /// </summary>
    public uint GeneratePlayerID(string playerName)
    {
        using var sha1 = SHA1.Create();
        byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes($"{playerName}:{host.ChannelName}"));
        return BinaryPrimitives.ReadUInt32LittleEndian(hash);
    }

    /// <summary>
    /// Synchronises the V3 player list with the lobby's player list, creating entries for
    /// new players and tearing down connections to players who have left.
    /// </summary>
    public void RegenerateV3PlayerInfos()
    {
        var playersToRemove = _v3PlayerInfos.Where(v3p => host.Players.All(p => p.Name != v3p.Name)).ToList();
        foreach (var v3p in playersToRemove)
        {
            StopLink(v3p, LinkCloseNotice.Goodbye);
            _v3PlayerInfos.Remove(v3p);
        }

        for (int i = 0; i < host.Players.Count; i++)
        {
            var player = host.Players[i];
            var v3Player = FindPlayer(player.Name);
            if (v3Player == null)
            {
                _v3PlayerInfos.Add(new V3PlayerInfo(
                    GeneratePlayerID(player.Name),
                    player.Name,
                    i,
                    0)); // PlayerGameId is assigned at game start
            }
            else
            {
                v3Player.PlayerIndex = i;
            }
        }
    }

    /// <summary>
    /// Starts connecting to a single player (by name), if dynamic tunnels are in use and the
    /// player isn't the local user.
    /// </summary>
    public void StartNegotiationForPlayerName(string playerName)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic || playerName == ProgramConstants.PLAYERNAME)
            return;

        var v3Player = FindPlayer(playerName);
        if (v3Player != null)
            StartLink(v3Player);
    }

    /// <summary>
    /// Starts connecting to every remote player without a connection yet. Used when joining a
    /// lobby that already has players.
    /// </summary>
    public void StartPendingNegotiations()
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic || host.Players.Count <= 1)
            return;

        foreach (var v3Player in _v3PlayerInfos.Where(p => p.Name != ProgramConstants.PLAYERNAME && p.Link == null).ToList())
            StartLink(v3Player);
    }

    private void StartLink(V3PlayerInfo player)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic || player.Name == ProgramConstants.PLAYERNAME || player.Link != null)
            return;

        var localV3Player = FindPlayer(ProgramConstants.PLAYERNAME);
        if (localV3Player == null)
            return;

        var pInfo = host.Players.Find(p => p.Name == player.Name);

        if (tunnelHandler.GetV3RelayPool().Count == 0)
        {
            host.AddNotice("Cannot negotiate tunnel: no V3 tunnels are available. Wait for the tunnel list to refresh or switch to a different tunnel mode.".L10N("Client:Main:NegotiationNoTunnels"), Color.Yellow);

            // Report this pair as explicitly Failed rather than leaving it silently absent —
            // BroadcastNegotiationInfo skips NotStarted entries, so without this the pair would
            // never appear over the wire and would look permanently stuck to everyone else.
            _negotiationData.UpdateStatus(ProgramConstants.PLAYERNAME, player.Name, NegotiationStatus.Failed);
            BroadcastNegotiationInfo();

            if (pInfo != null)
                host.OnLocalNegotiationStatus(pInfo, NegotiationStatus.Failed, -1);

            host.OnNegotiationStateChanged();
            return;
        }

        // The decider chooses the pair's path. Use the ID ordering, but fall back to name
        // ordering if the IDs collide so exactly one side still decides.
        bool isDecider = localV3Player.Id != player.Id
            ? localV3Player.Id < player.Id
            : string.CompareOrdinal(localV3Player.Name, player.Name) < 0;

        if (localV3Player.Id == player.Id)
            Logger.Log($"V3TunnelNegotiationManager: WARNING - player ID collision between {localV3Player.Name} and {player.Name}. Falling back to name ordering to pick the decider.");

        var link = new V3PairLink(localV3Player, player, tunnelHandler, isDecider,
            UserINISettings.Instance.EnableP2P, message => windowManager.AddCallback(new Action<string>(host.SendTunnelTable), message));

        link.StatusChanged += (l, change) => windowManager.AddCallback(new Action<V3PairLink, LinkStatusChange>(HandleLinkStatus), l, change);
        link.PathChanged += (l, from, to, reason) => windowManager.AddCallback(new Action<V3PairLink, CnCNetTunnel?, CnCNetTunnel, string>(HandleLinkPathChanged), l, from, to, reason);
        link.PingUpdated += (l, ping) => windowManager.AddCallback(new Action<V3PairLink, int>(HandleLinkPing), l, ping);

        player.Link = link;

        _negotiationData.UpdateStatus(ProgramConstants.PLAYERNAME, player.Name, NegotiationStatus.InProgress);
        BroadcastNegotiationInfo();

        if (pInfo != null)
            host.OnLocalNegotiationStatus(pInfo, NegotiationStatus.InProgress, -1);

        host.OnNegotiationStateChanged();

        tunnelHandler.LinkScheduler.Register(link);
        link.Start();
    }

    /// <summary>
    /// Stops a player's link, unless the game bridge may still be routing to them, in which case
    /// the link keeps running until the bridge stops.
    /// </summary>
    private void StopLink(V3PlayerInfo player, LinkCloseNotice notice, bool allowDefer = true)
    {
        var link = player.Link;
        if (link == null)
            return;

        if (allowDefer && IsLocalGameRouteActive())
        {
            if (!_deferredLinkDisposals.Contains(link))
                _deferredLinkDisposals.Add(link);

            return;
        }

        player.Link = null;
        DisposeLink(link, notice);
    }

    private void DisposeLink(V3PairLink link, LinkCloseNotice notice)
    {
        link.Dispose(notice);
        tunnelHandler.LinkScheduler.Unregister(link);
        tunnelHandler.CleanupP2PPair(link.Local.Id, link.Remote.Id);
    }

    private void FlushDeferredLinkDisposals()
    {
        foreach (var link in _deferredLinkDisposals)
        {
            // A player who came back is still using it.
            if (_v3PlayerInfos.Any(p => p.Link == link))
                continue;

            if (link.Remote.Link == link)
                link.Remote.Link = null;

            DisposeLink(link, LinkCloseNotice.Goodbye);
        }

        _deferredLinkDisposals.Clear();
    }

    private void HandleLinkStatus(V3PairLink link, LinkStatusChange change)
    {
        var v3Player = link.Remote;
        if (v3Player.Link != link || !_v3PlayerInfos.Contains(v3Player))
            return;

        string localName = ProgramConstants.PLAYERNAME;
        var playerInfo = host.Players.FirstOrDefault(p => p.Name == v3Player.Name);
        var previous = _negotiationData.GetNegotiationStatus(localName, v3Player.Name);

        switch (change.Status)
        {
            case NegotiationStatus.Succeeded:
                if (change.Restored)
                    host.AddNotice(string.Format("Connection with {0} restored.".L10N("Client:Main:ConnectionRestored"), v3Player.Name), Color.LightGreen);
                else if (previous == NegotiationStatus.Failed)
                    host.AddNotice(string.Format("Connection with {0} established.".L10N("Client:Main:LinkEstablishedAfterRetry"), v3Player.Name), Color.LightGreen);

                if (link.PingMs >= 0)
                    _negotiationData.UpdatePing(localName, v3Player.Name, link.PingMs);

                _negotiationData.UpdateStatus(localName, v3Player.Name, NegotiationStatus.Succeeded);

                if (playerInfo != null)
                    host.OnLocalNegotiationStatus(playerInfo, NegotiationStatus.Succeeded, link.PingMs);

                break;

            case NegotiationStatus.Failed:
                if (previous != NegotiationStatus.Failed)
                {
                    string reason = string.IsNullOrEmpty(change.Reason) ? string.Empty : $" ({change.Reason})";
                    host.AddNotice(string.Format("Connection with {0} failed{1}. Retrying automatically.".L10N("Client:Main:LinkFailedRetrying"), v3Player.Name, reason), Color.Orange);
                }

                if (change.SuggestRenegotiate)
                    host.AddNotice("Use the Renegotiate All button (or type /renegotiate) to retry. If failures persist, consider changing tunnel mode or having the affected players rejoin.".L10N("Client:Main:RenegotiateHint"), Color.Yellow);

                _negotiationData.UpdateStatus(localName, v3Player.Name, NegotiationStatus.Failed);

                if (playerInfo != null)
                    host.OnLocalNegotiationStatus(playerInfo, NegotiationStatus.Failed, -1);

                break;

            default:
                _negotiationData.UpdateStatus(localName, v3Player.Name, change.Status);

                if (playerInfo != null)
                    host.OnLocalNegotiationStatus(playerInfo, change.Status, -1);

                break;
        }

        BroadcastNegotiationInfo();
        host.OnNegotiationStateChanged();
    }

    private void HandleLinkPathChanged(V3PairLink link, CnCNetTunnel? from, CnCNetTunnel to, string reason)
    {
        var v3Player = link.Remote;
        if (v3Player.Link != link || !_v3PlayerInfos.Contains(v3Player))
            return;

        // Only moves between paths are news; the first connection is reported by its status.
        // In game nobody can read the lobby, so the log has it.
        if (from != null && !ProgramConstants.IsInGame)
        {
            host.AddNotice(string.Format("Connection with {0} moved from {1} to {2} ({3}).".L10N("Client:Main:LinkPathChanged"),
                v3Player.Name, from.Name, to.Name, reason), Color.Gray);
        }

        var playerInfo = host.Players.FirstOrDefault(p => p.Name == v3Player.Name);
        if (playerInfo != null)
            host.OnPairPingUpdated(playerInfo, link.PingMs);

        host.OnNegotiationStateChanged();
    }

    private void HandleLinkPing(V3PairLink link, int rttMs)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic)
            return;

        var v3Player = link.Remote;
        if (v3Player.Link != link || !_v3PlayerInfos.Contains(v3Player))
            return;

        var pInfo = host.Players.Find(p => p.Name == v3Player.Name);
        if (pInfo == null)
            return;

        string localName = ProgramConstants.PLAYERNAME;
        if (_negotiationData.GetReportedStatus(localName, v3Player.Name) != NegotiationStatus.Succeeded)
            return;

        _negotiationData.UpdatePing(localName, v3Player.Name, rttMs);
        host.OnPairPingUpdated(pInfo, rttMs);

        // Quiet broadcast: only push the refreshed ping over IRC when it has moved into a
        // different quality tier by a clear margin from what other players last saw. They only
        // see it in the status panel, so exact values aren't worth the channel traffic; our
        // own pairs still update live locally.
        if (!_lastReportedPings.TryGetValue(v3Player.Name, out int lastReportedMs) ||
            PingQualityRules.IsMaterialChange(lastReportedMs, rttMs))
        {
            BroadcastNegotiationInfo();
        }

        host.OnNegotiationStateChanged();
    }

    /// <summary>
    /// Handles a <see cref="TunnelTableExchange.IRC_COMMAND"/> message: a tunnel table or ranked
    /// list sent over IRC because the matchmaking servers could not carry it.
    /// </summary>
    public void HandleTunnelTableMessage(string sender, string data)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic)
            return;

        if (!TunnelTableExchange.TryParseIrc(data, out uint fromId, out uint toId, out char kind, out uint nonce, out string payload))
            return;

        var localV3Player = FindPlayer(ProgramConstants.PLAYERNAME);
        var senderV3Player = FindPlayer(sender);

        // Messages go to the whole channel; only the addressed player acts on one, and only if
        // it really comes from the player it claims to.
        if (localV3Player == null || senderV3Player == null || toId != localV3Player.Id || fromId != senderV3Player.Id)
            return;

        senderV3Player.Link?.HandleIrcMessage(kind, nonce, payload);
    }

    /// <summary>
    /// Parses an incoming NEGRPT payload (the data portion after the command name has been
    /// stripped by the CTCP dispatcher). Each <c>|</c>-delimited entry is
    /// <c>{hex_player_id}:{status_int}[:{ping_ms}]</c>. Entries for unknown IDs are silently
    /// skipped so new players joining mid-parse don't crash the loop.
    /// </summary>
    public void HandleNegotiationReportMessage(string sender, string data)
    {
        foreach (var entry in data.Split('|'))
        {
            string[] parts = entry.Split(':');
            if (parts.Length < 2)
                continue;

            if (!uint.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out uint id))
                continue;

            var v3Player = _v3PlayerInfos.FirstOrDefault(p => p.Id == id);
            if (v3Player == null)
                continue;

            if (!int.TryParse(parts[1], out int statusInt) || !Enum.IsDefined(typeof(NegotiationStatus), statusInt))
                continue;

            int ping = parts.Length >= 3 && int.TryParse(parts[2], out int p) ? p : -1;
            HandleNegotiationEntry(sender, v3Player.Name, (NegotiationStatus)statusInt, ping);
        }

        host.OnNegotiationStateChanged();
    }

    private void HandleNegotiationEntry(string sender, string targetPlayer, NegotiationStatus status, int ping)
    {
        // Announce a pair flipping to Failed exactly once. The check uses the merged pair
        // status (not just this direction), so the second report about an already-failed
        // pair stays quiet — and it covers every pair, because a failure between two other
        // players matters to the host/everyone just as much as one involving the local player.
        var pairStatusBefore = _negotiationData.GetNegotiationStatus(sender, targetPlayer);
        _negotiationData.UpdateStatus(sender, targetPlayer, status);

        if (status == NegotiationStatus.Failed && pairStatusBefore != NegotiationStatus.Failed)
        {
            if (targetPlayer == ProgramConstants.PLAYERNAME)
                host.AddNotice(string.Format("{0} reported a failed tunnel negotiation with you.".L10N("Client:Main:NegotiationFailedWithYou"), sender), Color.Red);
            else
                host.AddNotice(string.Format("Tunnel negotiation between {0} and {1} failed.".L10N("Client:Main:NegotiationFailedBetween"), sender, targetPlayer), Color.Red);
        }

        if (ping >= 0)
        {
            _negotiationData.UpdatePing(sender, targetPlayer, ping);

            if (sender == ProgramConstants.PLAYERNAME)
            {
                PlayerInfo? pInfo = host.Players.Find(p => p.Name == targetPlayer);
                if (pInfo != null)
                    host.OnRemoteNegotiationStatus(pInfo, status, ping);
            }
            else if (targetPlayer == ProgramConstants.PLAYERNAME)
            {
                PlayerInfo? pInfo = host.Players.Find(p => p.Name == sender);
                if (pInfo != null)
                    host.OnRemoteNegotiationStatus(pInfo, status, ping);
            }
        }
        else if (targetPlayer == ProgramConstants.PLAYERNAME)
        {
            PlayerInfo? pInfo = host.Players.Find(p => p.Name == sender);
            if (pInfo != null)
                host.OnRemoteNegotiationStatus(pInfo, status, -1);
        }
    }

    /// <summary>
    /// Builds and queues a full negotiation state report (NEGRPT) covering all known
    /// local-player→peer statuses. Uses a replace-capable queue slot so multiple rapid
    /// state changes within a single send-sleep window collapse into one wire message.
    /// NotStarted entries are omitted (receivers assume NotStarted for any absent peer ID).
    /// </summary>
    private void BroadcastNegotiationInfo()
    {
        string localName = ProgramConstants.PLAYERNAME;
        var sb = new StringBuilder();

        _lastReportedPings.Clear();

        foreach (var peer in _v3PlayerInfos)
        {
            if (peer.Name == localName)
                continue;

            // Report our own directional view; GetNegotiationStatus merges both directions
            // and would echo the peer's report back at them, deadlocking confirmation.
            var peerStatus = _negotiationData.GetReportedStatus(localName, peer.Name);
            if (peerStatus == NegotiationStatus.NotStarted)
                continue;

            if (sb.Length > 0)
                sb.Append('|');

            sb.Append(peer.Id.ToString("x8")).Append(':').Append((int)peerStatus);

            if (peerStatus == NegotiationStatus.Succeeded)
            {
                var peerPing = _negotiationData.GetPing(localName, peer.Name);
                if (peerPing.HasValue && peerPing.Value.IsValid())
                {
                    sb.Append(':').Append(peerPing.Value.Milliseconds);
                    _lastReportedPings[peer.Name] = peerPing.Value.Milliseconds;
                }
            }
        }

        if (sb.Length == 0)
            return;

        host.SendNegotiationReport($"{TunnelNegotiationCommands.NegotiationReport} {sb}");
    }

    public bool AreAllNegotiationsSuccessful()
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic || host.Players.Count <= 1)
            return true;

        return _negotiationData.AreAllNegotiationsSuccessful(host.Players.Select(p => p.Name).ToList());
    }

    /// <summary>
    /// True while a pre-launch connectivity check is running, so the lobby can refuse a
    /// second launch attempt instead of starting overlapping checks.
    /// </summary>
    public bool LaunchConnectivityCheckInProgress { get; private set; }

    /// <summary>
    /// How long (ms) the pre-launch check waits for every player's report.
    /// </summary>
    private const int LAUNCH_CHECK_TIMEOUT_MS = 3000;

    /// <summary>
    /// Runs the pre-launch connectivity check: confirms our own pairs are live, then asks every
    /// player over its current path which of their pairs are live. Every pair is monitored all
    /// the time, so this takes one round trip rather than a round of probing. Invokes
    /// <paramref name="onVerified"/> on the game thread once every pair has verified and
    /// negotiations are still all successful; otherwise notices the problem and aborts.
    /// </summary>
    public void BeginLaunchConnectivityCheck(Action onVerified)
    {
        LaunchConnectivityCheckInProgress = true;
        host.AddNotice("Verifying player connections...".L10N("Client:Main:VerifyingConnections"), Color.White);

        var remotes = _v3PlayerInfos.Where(p => p.Name != ProgramConstants.PLAYERNAME).ToList();
        var reports = new ConcurrentDictionary<uint, List<uint>>();
        _probeReports = reports;

        Task.Run(async () =>
        {
            var result = new LaunchProbeResult();

            try
            {
                foreach (var remote in remotes)
                {
                    if (remote.Link == null || !remote.Link.IsLive(TunnelHandler.LAUNCH_CHECK_MAX_SILENCE_MS))
                        result.LocalUnresponsive.Add(remote.Id);
                }

                var asked = remotes.Where(r => !result.LocalUnresponsive.Contains(r.Id)).ToList();
                long start = V3Clock.NowMs;
                long nextSend = start;

                while (V3Clock.NowMs - start < LAUNCH_CHECK_TIMEOUT_MS && asked.Any(r => !reports.ContainsKey(r.Id)))
                {
                    if (V3Clock.NowMs >= nextSend)
                    {
                        foreach (var remote in asked.Where(r => !reports.ContainsKey(r.Id)))
                            remote.Link?.SendProbeRequest();

                        nextSend += 500;
                    }

                    await Task.Delay(50).ConfigureAwait(false);
                }

                foreach (var remote in asked)
                {
                    if (!reports.TryGetValue(remote.Id, out var unreachable))
                    {
                        result.MissingReports.Add(remote.Id);
                        continue;
                    }

                    foreach (uint failedId in unreachable)
                        result.RemoteFailures.Add((remote.Id, failedId));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Launch connectivity check failed: {ex.Message}");
                result = new LaunchProbeResult { LocalUnresponsive = remotes.Select(r => r.Id).ToList() };
            }
            finally
            {
                _probeReports = null;
            }

            windowManager.AddCallback(new Action<LaunchProbeResult, Action>(FinishLaunchConnectivityCheck), result, onVerified);
        });
    }

    // Receive thread. First report per peer wins; resent duplicates are ignored.
    private void OnProbeReportReceived(uint reporterId, List<uint> unreachableIds)
        => _probeReports?.TryAdd(reporterId, unreachableIds);

    private void FinishLaunchConnectivityCheck(LaunchProbeResult result, Action onVerified)
    {
        LaunchConnectivityCheckInProgress = false;

        if (!host.IsHost || host.TunnelMode != TunnelMode.V3Dynamic || host.Players.Count <= 1)
            return;

        bool failed = false;

        foreach (uint id in result.LocalUnresponsive)
        {
            host.AddNotice(string.Format("No response from {0} — they may have disconnected.".L10N("Client:Main:LaunchCheckNoResponse"), NameForId(id)), Color.Red);
            failed = true;
        }

        foreach (uint id in result.MissingReports)
        {
            // Silence from a player who left the lobby while the check ran adds nothing.
            string? name = _v3PlayerInfos.FirstOrDefault(p => p.Id == id)?.Name;
            if (name == null || host.Players.All(p => p.Name != name) || result.LocalUnresponsive.Contains(id))
                continue;

            host.AddNotice(string.Format("No connectivity report from {0} — their connection check did not complete.".L10N("Client:Main:LaunchCheckNoReport"), name), Color.Red);
            failed = true;
        }

        foreach (var (reporterId, failedId) in result.RemoteFailures)
        {
            // Skip pairs no longer relevant because one side left during the check.
            string? reporterName = _v3PlayerInfos.FirstOrDefault(p => p.Id == reporterId)?.Name;
            string? failedName = _v3PlayerInfos.FirstOrDefault(p => p.Id == failedId)?.Name;
            if (reporterName == null || failedName == null ||
                host.Players.All(p => p.Name != reporterName) || host.Players.All(p => p.Name != failedName))
                continue;

            host.AddNotice(string.Format("{0} got no response from {1}.".L10N("Client:Main:LaunchCheckPeerFailure"), reporterName, failedName), Color.Red);
            failed = true;
        }

        if (failed)
        {
            host.AddNotice("Launch aborted.".L10N("Client:Main:LaunchCheckAborted"), Color.Red);
            return;
        }

        // The lobby can change while the check runs (join/leave, renegotiation).
        if (!AreAllNegotiationsSuccessful())
        {
            host.AddNotice("Player connections changed during the check; launch cancelled.".L10N("Client:Main:LaunchCheckStateChanged"), Color.Yellow);
            return;
        }

        onVerified();
    }

    private string NameForId(uint id) => _v3PlayerInfos.FirstOrDefault(p => p.Id == id)?.Name ?? id.ToString("x8");

    /// <summary>
    /// Points every V3 player at the given tunnel. Only relevant in static mode, where all
    /// players share the host-selected tunnel; no-op in other modes.
    /// </summary>
    public void ApplyStaticTunnel(CnCNetTunnel tunnel)
    {
        if (host.TunnelMode != TunnelMode.V3Static)
            return;

        foreach (var v3Player in _v3PlayerInfos)
            v3Player.Tunnel = tunnel;
    }

    /// <summary>Tears down and restarts connections with every remote player.</summary>
    public void RestartAllNegotiations()
    {
        var allRemote = _v3PlayerInfos.Where(p => p.Name != ProgramConstants.PLAYERNAME).ToList();
        RestartNegotiations(allRemote);
    }

    /// <summary>
    /// Handles negotiation-side state transitions when the lobby's tunnel mode changes.
    /// Call this after updating the lobby's tunnel mode field so that
    /// <see cref="StartPendingNegotiations"/> sees the new mode via <see cref="IV3NegotiationHost.TunnelMode"/>.
    /// </summary>
    public void ApplyModeTransition(TunnelMode oldMode, TunnelMode newMode)
    {
        if (oldMode == TunnelMode.V3Dynamic && newMode != TunnelMode.V3Dynamic)
        {
            StopAllNegotiations();
            ClearNegotiationData();
        }
        else if (newMode == TunnelMode.V3Dynamic && oldMode != TunnelMode.V3Dynamic)
        {
            ResetAllNegotiators();
            StartPendingNegotiations();
        }
    }

    /// <summary>
    /// Tears down and restarts connections with the given players.
    /// </summary>
    public void RestartNegotiations(IEnumerable<V3PlayerInfo> affectedPlayers)
    {
        // The game bridge routes live traffic through the current paths, so no pair may be torn
        // down while a game is running — neither when the local game is running nor for peers
        // who are still in one.
        if (IsLocalGameRouteActive())
        {
            Logger.Log("V3TunnelNegotiationManager: Ignored a negotiation restart because the local game is running.");
            return;
        }

        var playersToRestart = new List<V3PlayerInfo>();
        var skippedPlayers = new List<string>();

        foreach (var v3Player in affectedPlayers.ToList())
        {
            if (host.Players.Find(p => p.Name == v3Player.Name)?.IsInGame == true)
                skippedPlayers.Add(v3Player.Name);
            else
                playersToRestart.Add(v3Player);
        }

        if (skippedPlayers.Count > 0)
            host.AddNotice(string.Format("Players currently in game were not renegotiated: {0}. Their existing connections were kept.".L10N("Client:Main:RenegotiateSkippedInGame"), string.Join(", ", skippedPlayers)), Color.Yellow);

        if (playersToRestart.Count == 0)
            return;

        host.OnNegotiationsRestarted();

        // Clear only the pairs being renegotiated: every pair among the restart participants
        // (the restarted players plus the local player). Wiping whole players would also drop
        // their pairs with non-participants — e.g. an in-game player — whose next one-sided
        // report would then leave those pairs looking stuck in progress.
        var participantNames = playersToRestart.Select(p => p.Name).ToList();
        if (!participantNames.Contains(ProgramConstants.PLAYERNAME))
            participantNames.Add(ProgramConstants.PLAYERNAME);

        foreach (var (player1, player2) in _negotiationData.GetPlayerPairs(participantNames))
            _negotiationData.ClearPair(player1, player2);

        foreach (var v3Player in playersToRestart)
        {
            // Disposed before the replacement starts: both register a handler for the same pair.
            // The restart notice brings the peer's side round too, even if our request to it was lost.
            StopLink(v3Player, LinkCloseNotice.Restart, allowDefer: false);

            if (v3Player.Name != ProgramConstants.PLAYERNAME)
                StartLink(v3Player);
        }

        host.OnNegotiationStateChanged();
    }

    /// <summary>
    /// Removes a single player's V3 state (the lobby still owns its own player list).
    /// </summary>
    public void RemovePlayer(string playerName)
    {
        var v3Player = FindPlayer(playerName);
        if (v3Player != null)
        {
            StopLink(v3Player, LinkCloseNotice.Goodbye);
            _v3PlayerInfos.Remove(v3Player);
        }

        _negotiationData.ClearPlayer(playerName);
    }

    /// <summary>
    /// Stops every link without clearing the player list or negotiation data. While a game
    /// route is active the links keep running until the bridge stops.
    /// </summary>
    public void StopAllNegotiations(bool keepGameRoutes = false)
    {
        foreach (var v3Player in _v3PlayerInfos)
            StopLink(v3Player, LinkCloseNotice.Goodbye, allowDefer: keepGameRoutes || IsLocalGameRouteActive());
    }

    /// <summary>Stops every link and forgets their tunnels (used when switching to dynamic mode).</summary>
    public void ResetAllNegotiators()
    {
        foreach (var v3Player in _v3PlayerInfos)
        {
            StopLink(v3Player, LinkCloseNotice.Goodbye, allowDefer: false);
            v3Player.Tunnel = null;
        }
    }

    /// <summary>Discards all negotiation status/ping data.</summary>
    public void ClearNegotiationData() => _negotiationData.ClearAll();

    /// <summary>
    /// Stops all links and clears every piece of negotiation state. Used on teardown.
    /// </summary>
    public void ClearAll()
    {
        bool gameRouteActive = IsLocalGameRouteActive();

        StopAllNegotiations(keepGameRoutes: gameRouteActive);
        _negotiationData.ClearAll();
        _v3PlayerInfos.Clear();

        // Re-query STUN in the next lobby: the NAT mapping behind the cached external endpoint
        // may expire and get remapped in between.
        tunnelHandler.ClearP2PEndpointCache();

        if (!gameRouteActive)
            tunnelHandler.ReleaseLocalGamePort();
    }

    /// <summary>
    /// Parses one STARTV3 player entry (3 semicolon-delimited fields: id;name;ip:port) from
    /// <paramref name="parts"/> starting at <paramref name="offset"/>, derives the game id
    /// from <paramref name="playerPosition"/>, and updates both the <see cref="PlayerInfo"/>
    /// and <see cref="V3PlayerInfo"/> for that player.
    /// </summary>
    /// <returns>False if any field is malformed or the player name is not found.</returns>
    public bool ApplyV3StartEntry(string[] parts, int offset, int playerPosition)
    {
        if (!uint.TryParse(parts[offset], out uint id))
            return false;

        string pName = parts[offset + 1];
        string[] ipAndPort = parts[offset + 2].Split(':');

        if (ipAndPort.Length != 2 || !int.TryParse(ipAndPort[1], out int tunnelPort))
            return false;

        PlayerInfo? pInfo = host.Players.Find(p => p.Name == pName);
        if (pInfo == null)
            return false;

        int gamePort = 48000 - playerPosition;
        pInfo.Port = gamePort;

        var v3PlayerInfo = FindPlayer(pName);
        if (v3PlayerInfo == null)
        {
            Logger.Log($"ApplyV3StartEntry: Missing V3 player info for {pName}.");
            return false;
        }

        if (host.TunnelMode != TunnelMode.V3Dynamic)
        {
            v3PlayerInfo.Tunnel = tunnelHandler.Tunnels.Find(t => t.Address == ipAndPort[0] && t.Port == tunnelPort);
            if (v3PlayerInfo.Tunnel == null)
                return false;
        }
        v3PlayerInfo.PlayerIndex = playerPosition;
        v3PlayerInfo.PlayerGameId = (ushort)gamePort;
        v3PlayerInfo.Id = id;

        return true;
    }

    /// <summary>
    /// Assigns final game ids/tunnels to every player and builds the "id;name;address;..."
    /// payload used in the STARTV3 message. The player order here defines each player's
    /// in-game id; all clients must iterate in this same order (the handler keys the id off
    /// message position via <see cref="ApplyV3StartEntry"/>).
    /// </summary>
    public string GenerateV3StartPayload()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < host.Players.Count; i++)
        {
            var player = host.Players[i];
            uint id = GeneratePlayerID(player.Name);
            int port = 48000 - i;
            player.Port = port;

            string address = IPAddress.Any + ":0";
            var v3PlayerInfo = FindPlayer(player.Name);
            if (v3PlayerInfo != null)
            {
                v3PlayerInfo.Id = id;
                v3PlayerInfo.PlayerIndex = i;
                v3PlayerInfo.PlayerGameId = (ushort)port;

                if (host.TunnelMode == TunnelMode.V3Static)
                    v3PlayerInfo.Tunnel = tunnelHandler.CurrentTunnel;

                // In dynamic mode each client uses its own per-peer path, so this address is
                // informational only; it is only consumed by clients in V3 static mode.
                address = v3PlayerInfo.Tunnel == null
                    ? IPAddress.Any + ":0"
                    : v3PlayerInfo.Tunnel.Address + ":" + v3PlayerInfo.Tunnel.Port;
            }
            else
            {
                Logger.Log($"GenerateV3StartPayload: Missing V3 player info for {player.Name}, using fallback tunnel address.");
            }

            sb.Append(id).Append(';')
              .Append(player.Name).Append(';')
              .Append(address).Append(';');
        }

        return sb.ToString().TrimEnd(';');
    }

    /// <summary>
    /// Reserves this client's local game-relay UDP socket.
    /// </summary>
    public bool TryReserveGamePort()
    {
        try
        {
            tunnelHandler.ReserveLocalGamePort();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"V3TunnelNegotiationManager: Failed to reserve a local game port: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Starts the in-game tunnel bridge for the local player. Returns false if the local
    /// player's V3 info could not be found or the local game port could not be reserved.
    /// </summary>
    public bool StartGameBridge()
    {
        var localV3Player = FindPlayer(ProgramConstants.PLAYERNAME);
        if (localV3Player == null)
        {
            Logger.Log("V3TunnelNegotiationManager: Could not find local V3 player info.");
            return false;
        }

        if (!TryReserveGamePort())
            return false;

        tunnelHandler.StartGameBridge(localV3Player.Id, _v3PlayerInfos);
        return true;
    }

    private bool IsLocalGameRouteActive()
        => tunnelHandler.GameTunnelBridge?.IsRunning == true || ProgramConstants.IsInGame;
}

/// <summary>
/// Result of the pre-launch connectivity check
/// (<see cref="V3TunnelNegotiationManager.BeginLaunchConnectivityCheck"/>).
/// </summary>
public sealed class LaunchProbeResult
{
    /// <summary>Peers whose connection with us is not live.</summary>
    public List<uint> LocalUnresponsive = new();

    /// <summary>Peers that never sent a report back.</summary>
    public List<uint> MissingReports = new();

    /// <summary>Peer-reported failures: reporter → the peer it could not reach.</summary>
    public List<(uint ReporterId, uint FailedId)> RemoteFailures = new();
}
