#nullable enable
using System;
using System.Buffers.Binary;
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
/// Orchestrates V3 dynamic-tunnel negotiation for a lobby: maintains the per-player
/// negotiation state, drives <see cref="V3PlayerNegotiator"/> instances, exchanges status
/// over IRC and prepares the data needed to start the game bridge. Shared by
/// CnCNetGameLobby and CnCNetGameLoadingLobby, which live on separate inheritance trees.
/// </summary>
public class V3TunnelNegotiationManager
{
    private readonly IV3NegotiationHost host;
    private readonly TunnelHandler tunnelHandler;
    private readonly WindowManager windowManager;
    private readonly List<V3PlayerInfo> _v3PlayerInfos = new();
    private readonly NegotiationDataManager _negotiationData = new();

    // Pairs whose P2P routes had to be kept when the player left mid-game (the bridge
    // was still routing to them); cleaned up once the game bridge stops so departed
    // players' routing entries don't accumulate across a long session.
    private readonly List<(uint LocalId, uint RemoteId)> _deferredP2PCleanups = new();

    public V3TunnelNegotiationManager(IV3NegotiationHost host, TunnelHandler tunnelHandler, WindowManager windowManager)
    {
        this.host = host;
        this.tunnelHandler = tunnelHandler;
        this.windowManager = windowManager;

        // Fired on the game thread. Both the game lobby's and the loading lobby's managers
        // subscribe to these; the manager of whichever lobby is not currently in use has an
        // empty player list, so its lookups fail and the events fall through harmlessly.
        tunnelHandler.KeepAliveMonitor.PongReceived += OnKeepAlivePongReceived;
        tunnelHandler.KeepAliveMonitor.TimedOut += OnKeepAliveTimedOut;
        tunnelHandler.GameBridgeStopped += FlushDeferredP2PCleanups;
    }

    public IReadOnlyList<V3PlayerInfo> PlayerInfos => _v3PlayerInfos;

    public NegotiationDataManager NegotiationData => _negotiationData;

    /// <summary>
    /// True while any local negotiation with a remote player is still running. Used to
    /// prevent overlapping renegotiation rounds, whose stale packets and reports would
    /// land on the fresh round and flip pairs to Failed.
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
    /// The relay tunnels this client may use for game traffic — the pool the matchmaking phase
    /// advertises and narrows, not the set anything registers on, so it stays wide. Matchmaking
    /// servers announce themselves as version 4 and are excluded by the version check.
    /// </summary>
    private List<CnCNetTunnel> GetAvailableTunnelsForNegotiation()
    {
        return tunnelHandler.Tunnels
            .Where(t => t.Version == 3 &&
                (UserINISettings.Instance.PingUnofficialCnCNetTunnels || t.Official || t.Recommended))
            .ToList();
    }

    /// <summary>
    /// Synchronises the V3 player list with the lobby's player list, creating entries for
    /// new players and tearing down negotiations for players who have left.
    /// </summary>
    public void RegenerateV3PlayerInfos()
    {
        // Remove players who are no longer in the game; clean up their negotiations first.
        var playersToRemove = _v3PlayerInfos.Where(v3p => host.Players.All(p => p.Name != v3p.Name)).ToList();
        foreach (var v3p in playersToRemove)
        {
            DetachNegotiator(v3p);
            v3p.StopNegotiation();
            CleanupP2PForRemovedPlayer(v3p);
            _v3PlayerInfos.Remove(v3p);
        }

        if (playersToRemove.Count > 0)
            RefreshKeepAliveTargets();

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
    /// Starts negotiating with a single player (by name), if dynamic tunnels are in use and
    /// the player isn't the local user.
    /// </summary>
    public void StartNegotiationForPlayerName(string playerName)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic || playerName == ProgramConstants.PLAYERNAME)
            return;

        var v3Player = FindPlayer(playerName);
        if (v3Player != null)
            StartTunnelNegotiationForPlayer(v3Player);
    }

    /// <summary>
    /// Starts negotiations with every remote player that hasn't yet negotiated (or isn't
    /// already negotiating). Used when joining a lobby that already has players.
    /// </summary>
    public void StartPendingNegotiations()
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic || host.Players.Count <= 1)
            return;

        foreach (var v3Player in _v3PlayerInfos
            .Where(p => p.Name != ProgramConstants.PLAYERNAME && !p.HasNegotiated && !p.IsNegotiating)
            .ToList())
            StartTunnelNegotiationForPlayer(v3Player);
    }

    private void StartTunnelNegotiationForPlayer(V3PlayerInfo player)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic || player.Name == ProgramConstants.PLAYERNAME)
            return;

        var localV3Player = FindPlayer(ProgramConstants.PLAYERNAME);
        if (localV3Player == null)
            return;

        var pInfo = host.Players.Find(p => p.Name == player.Name);

        var availableTunnels = GetAvailableTunnelsForNegotiation();
        if (availableTunnels.Count == 0)
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

        _negotiationData.UpdateStatus(ProgramConstants.PLAYERNAME, player.Name, NegotiationStatus.InProgress);
        BroadcastNegotiationInfo();

        if (pInfo != null)
            host.OnLocalNegotiationStatus(pInfo, NegotiationStatus.InProgress, -1);

        // Disable the launch/load button until this negotiation succeeds.
        host.OnNegotiationStateChanged();

        try
        {
            var startResult = player.StartNegotiation(
                localV3Player,
                tunnelHandler,
                availableTunnels,
                p2pEnabled: UserINISettings.Instance.EnableP2P);

            switch (startResult)
            {
                case NegotiationStartResult.Started:
                    AttachNegotiator(player);
                    break;

                case NegotiationStartResult.AlreadyInProgress:
                    // A negotiation is already running for this player; leave its state untouched.
                    break;

                case NegotiationStartResult.Failed:
                    MarkNegotiationFailed(player.Name, pInfo);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error negotiating with player {player.Name}: {ex.Message}");
            MarkNegotiationFailed(player.Name, pInfo);
        }
    }

    private void MarkNegotiationFailed(string playerName, PlayerInfo? pInfo)
    {
        host.AddNotice(string.Format("Could not start tunnel negotiation with {0}.".L10N("Client:Main:NegotiationStartFailed"), playerName), Color.Red);

        _negotiationData.UpdateStatus(ProgramConstants.PLAYERNAME, playerName, NegotiationStatus.Failed);
        BroadcastNegotiationInfo();

        if (pInfo != null)
            host.OnLocalNegotiationStatus(pInfo, NegotiationStatus.Failed, -1);

        host.OnNegotiationStateChanged();
    }

    /// <summary>
    /// Negotiator events fire on thread-pool tasks, but everything downstream touches UI
    /// (chat notices, ping indicators, the status panel — whose resize recreates a render
    /// target and crashes the render thread if done concurrently). Marshal to the game
    /// thread first, matching how CnCNetManager marshals all IRC events.
    /// </summary>
    private void OnPlayerNegotiationResult(object? sender, TunnelChosenEventArgs e)
        => windowManager.AddCallback(new Action<object?, TunnelChosenEventArgs>(HandlePlayerNegotiationResult), sender, e);

    private void OnPlayerNegotiationComplete(object? sender, EventArgs e)
        => windowManager.AddCallback(new Action<object?, EventArgs>(HandlePlayerNegotiationComplete), sender, e);

    private void HandlePlayerNegotiationResult(object? sender, TunnelChosenEventArgs e)
    {
        var v3PlayerInfo = _v3PlayerInfos.FirstOrDefault(p => p.Id == e.PlayerId);
        if (v3PlayerInfo == null)
            return;

        v3PlayerInfo.HasNegotiated = true;
        v3PlayerInfo.IsNegotiating = false;

        var playerInfo = host.Players.FirstOrDefault(p => p.Name == e.PlayerName);

        if (e.ChosenTunnel != null)
        {
            // Success — this fires for the relay choice (round 1) and again if the P2P
            // upgrade round picks a direct path (round 2).
            v3PlayerInfo.Tunnel = e.ChosenTunnel;

            if (e.IsRelayFallback)
                host.AddNotice(string.Format("Direct connection with {0} could not be established; using relay server {1}.".L10N("Client:Main:P2PRelayFallback"), e.PlayerName, e.ChosenTunnel.Name), Color.Orange);

            // Only re-broadcast when the pair's ping/status actually changed, so a P2P
            // upgrade is propagated to everyone while a round-2 that simply re-confirms
            // the relay (same values) doesn't add redundant IRC traffic.
            var prevPing = _negotiationData.GetPing(ProgramConstants.PLAYERNAME, e.PlayerName);
            var prevStatus = _negotiationData.GetNegotiationStatus(ProgramConstants.PLAYERNAME, e.PlayerName);
            bool changed = prevStatus != NegotiationStatus.Succeeded
                || (prevPing?.Milliseconds ?? -1) != e.NegotiationPing;

            if (e.IsLocalDecision)
                _negotiationData.UpdatePing(ProgramConstants.PLAYERNAME, e.PlayerName, e.NegotiationPing);
            else
                _negotiationData.UpdatePing(e.PlayerName, ProgramConstants.PLAYERNAME, e.NegotiationPing);

            _negotiationData.UpdateStatus(ProgramConstants.PLAYERNAME, e.PlayerName, NegotiationStatus.Succeeded);

            if (playerInfo != null)
                host.OnLocalNegotiationStatus(playerInfo, NegotiationStatus.Succeeded, e.NegotiationPing);

            host.OnNegotiationStateChanged();

            if (changed)
                BroadcastNegotiationInfo();
        }
        else
        {
            // Failure — announce once per pair transition (the peer's own Failed report may
            // have already produced a notice for this pair).
            var pairStatusBefore = _negotiationData.GetNegotiationStatus(ProgramConstants.PLAYERNAME, e.PlayerName);
            if (pairStatusBefore != NegotiationStatus.Failed)
            {
                string reason = string.IsNullOrEmpty(e.FailureReason) ? string.Empty : $" ({e.FailureReason})";
                host.AddNotice(string.Format("Tunnel negotiation with {0} failed{1}.".L10N("Client:Main:NegotiationFailedWith"), e.PlayerName, reason), Color.Red);
            }

            _negotiationData.UpdateStatus(ProgramConstants.PLAYERNAME, e.PlayerName, NegotiationStatus.Failed);

            if (playerInfo != null)
                host.OnLocalNegotiationStatus(playerInfo, NegotiationStatus.Failed, -1);

            host.OnNegotiationStateChanged();

            BroadcastNegotiationInfo();
        }
    }

    private void HandlePlayerNegotiationComplete(object? sender, EventArgs e)
    {
        if (sender is not V3PlayerNegotiator negotiator)
            return;

        var player = negotiator.RemotePlayer;

        if (!player.HasNegotiated)
        {
            player.HasNegotiated = true;
            player.IsNegotiating = false;
            BroadcastNegotiationInfo();
        }

        negotiator.NegotiationResult -= OnPlayerNegotiationResult;
        negotiator.NegotiationComplete -= OnPlayerNegotiationComplete;

        if (ReferenceEquals(player.Negotiator, negotiator))
            player.StopNegotiation();

        // Drop the unused P2P candidate routes from this negotiation, but keep the
        // chosen path (if direct) alive for the game bridge.
        CleanupP2PForPlayer(player, keepChosenTunnel: true);

        RefreshKeepAliveTargets();

        host.OnNegotiationStateChanged();
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
        var sb = new System.Text.StringBuilder();

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
                    sb.Append(':').Append(peerPing.Value.Milliseconds);
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
    /// Runs the distributed pre-launch connectivity check (briefly, off the game thread):
    /// pings every remote player over their negotiated tunnel, and over the same UDP
    /// paths — asks each of them to probe their own peers and report back. Invokes
    /// <paramref name="onVerified"/> on the game thread once every pair has verified and
    /// negotiations are still all successful; otherwise notices the problem and aborts.
    /// </summary>
    public void BeginLaunchConnectivityCheck(Action onVerified)
    {
        LaunchConnectivityCheckInProgress = true;
        host.AddNotice("Verifying player connections...".L10N("Client:Main:VerifyingConnections"), Color.White);

        var remoteIdsSnapshot = _v3PlayerInfos
            .Where(p => p.Name != ProgramConstants.PLAYERNAME)
            .Select(p => p.Id)
            .ToList();

        Task.Run(async () =>
        {
            LaunchProbeResult result;

            try
            {
                result = await tunnelHandler.KeepAliveMonitor.ProbeAllPairsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Launch connectivity check failed: {ex.Message}");
                result = new LaunchProbeResult { LocalUnresponsive = remoteIdsSnapshot };
            }

            windowManager.AddCallback(new Action<LaunchProbeResult, Action>(FinishLaunchConnectivityCheck), result, onVerified);
        });
    }

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
            // Silence from a player who already failed the direct probe (or who left the
            // lobby while the check ran) adds nothing worth announcing.
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
    /// Returns remote players whose negotiated tunnel matches the given address/port.
    /// </summary>
    public List<V3PlayerInfo> FindRemotePlayersUsingTunnel(string address, int port)
        => _v3PlayerInfos
            .Where(p => p.Name != ProgramConstants.PLAYERNAME &&
                        p.Tunnel?.Address == address && p.Tunnel?.Port == port)
            .ToList();

    /// <summary>
    /// Reacts to a tunnel failure in dynamic mode by renegotiating with the players routed
    /// through it: notifies the lobby, broadcasts TunnelRenegotiate so remote clients restart
    /// the same pairs, and restarts the affected negotiations.
    /// Returns false in non-dynamic modes so the caller can run its own fallback.
    /// </summary>
    public bool TryHandleTunnelFailure(CnCNetTunnel failedTunnel)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic)
            return false;

        // Broadcasting TunnelRenegotiate now would make lobby-side peers restart their
        // pair with us while we can't reciprocate, stranding the pair. The keepalive
        // monitor will surface a genuinely dead path after we return to the lobby.
        if (IsLocalGameRouteActive())
            return true;

        var affectedPlayers = FindRemotePlayersUsingTunnel(failedTunnel.Address, failedTunnel.Port);

        if (affectedPlayers.Count > 0)
        {
            host.AddNotice(string.Format("Tunnel {0} failed. Starting renegotiation with affected players...".L10N("Client:Main:TunnelFailedRenegotiating"), failedTunnel.Name), Color.Orange);
            host.SendChannelCTCP($"{TunnelNegotiationCommands.TunnelRenegotiate} {failedTunnel.Address}:{failedTunnel.Port}", 10);
            RestartNegotiations(affectedPlayers);
        }

        return true;
    }

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

    /// <summary>Tears down and restarts negotiations with every remote player.</summary>
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

        RefreshKeepAliveTargets();
    }

    /// <summary>
    /// Tears down and restarts negotiations for the given players.
    /// </summary>
    public void RestartNegotiations(IEnumerable<V3PlayerInfo> affectedPlayers)
    {
        // The game bridge routes live traffic through the negotiated paths, so no pair
        // may be torn down while a game is running — neither when the local game is
        // running nor for peers who are still in one.
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

        // Clear only the pairs being renegotiated: every pair among the restart
        // participants (the restarted players plus the local player). Wiping whole
        // players (ClearPlayer) would also drop their pairs with non-participants —
        // e.g. an in-game player — whose next one-sided report would then leave those
        // pairs looking stuck in progress.
        var participantNames = playersToRestart.Select(p => p.Name).ToList();
        if (!participantNames.Contains(ProgramConstants.PLAYERNAME))
            participantNames.Add(ProgramConstants.PLAYERNAME);

        foreach (var (player1, player2) in _negotiationData.GetPlayerPairs(participantNames))
            _negotiationData.ClearPair(player1, player2);

        foreach (var v3Player in playersToRestart)
        {
            // Detach before disposing, as every other teardown path does. A negotiator cancelled
            // mid-round can still raise a result as it unwinds (the P2P round's relay fallback
            // does exactly this), and that stale event would otherwise land on the fresh round and
            // overwrite its state with the old round's tunnel.
            DetachNegotiator(v3Player);
            v3Player.StopNegotiation();
            CleanupP2PForPlayer(v3Player, keepChosenTunnel: false);
            v3Player.ResetNegotiator();

            if (v3Player.Name != ProgramConstants.PLAYERNAME)
                StartTunnelNegotiationForPlayer(v3Player);
        }

        RefreshKeepAliveTargets();
        host.OnNegotiationStateChanged();
    }

    /// <summary>
    /// Handles a remote player's request to renegotiate the tunnel shared with us.
    /// </summary>
    public void HandleRemoteTunnelRenegotiate(string sender, string tunnelAddressAndPort)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic || IsLocalGameRouteActive())
            return;

        string[] split = tunnelAddressAndPort.Split(':');
        if (split.Length != 2 || !int.TryParse(split[1], out int tunnelPort))
            return;

        string tunnelAddress = split[0];

        var remoteV3Player = FindPlayer(sender);
        if (remoteV3Player == null)
            return;

        if (remoteV3Player.Tunnel?.Address == tunnelAddress && remoteV3Player.Tunnel?.Port == tunnelPort)
        {
            host.AddNotice(string.Format("{0} needs to renegotiate tunnel. Starting renegotiation...".L10N("Client:Main:PeerRenegotiating"), sender), Color.Orange);
            RestartNegotiations(new[] { remoteV3Player });
        }
    }

    /// <summary>
    /// Surfaces a remote player's tunnel-failure report in the lobby chat.
    /// </summary>
    public void HandleRemoteTunnelFailed(string sender, string tunnelName)
    {
        if (host.IsHost)
            host.AddNotice(string.Format("{0} can no longer connect to tunnel: {1}. Change the tunnel or the game won't start.".L10N("Client:Main:PlayerTunnelFailedHost"), sender, tunnelName), Color.Orange);
        else
            host.AddNotice(string.Format("{0} can no longer connect to tunnel: {1}. The host needs to change the tunnel or the game won't start.".L10N("Client:Main:PlayerTunnelFailed"), sender, tunnelName), Color.Orange);
    }

    /// <summary>
    /// Removes a single player's V3 negotiation state (the lobby still owns its own player list).
    /// </summary>
    public void RemovePlayer(string playerName)
    {
        var v3Player = FindPlayer(playerName);
        if (v3Player != null)
        {
            if (host.TunnelMode == TunnelMode.V3Dynamic)
            {
                DetachNegotiator(v3Player);
                v3Player.StopNegotiation();
            }

            CleanupP2PForRemovedPlayer(v3Player);
            _v3PlayerInfos.Remove(v3Player);
        }

        _negotiationData.ClearPlayer(playerName);
        RefreshKeepAliveTargets();
    }

    /// <summary>
    /// Stops every active negotiation without clearing the player list or negotiation data.
    /// </summary>
    public void StopAllNegotiations(bool keepGameRoutes = false)
    {
        bool gameRouteActive = keepGameRoutes || IsLocalGameRouteActive();

        foreach (var v3Player in _v3PlayerInfos)
        {
            DetachNegotiator(v3Player);
            v3Player.StopNegotiation();
            CleanupP2PForPlayer(v3Player, keepChosenTunnel: gameRouteActive);
            DeferP2PCleanupIfNeeded(v3Player, gameRouteActive);
        }
    }

    /// <summary>Resets every player's negotiation state (used when switching to dynamic mode).</summary>
    public void ResetAllNegotiators()
    {
        foreach (var v3Player in _v3PlayerInfos)
            v3Player.ResetNegotiator();
    }

    /// <summary>Discards all negotiation status/ping data.</summary>
    public void ClearNegotiationData() => _negotiationData.ClearAll();

    /// <summary>
    /// Stops all negotiations and clears every piece of negotiation state. Used on teardown.
    /// </summary>
    public void ClearAll()
    {
        StopAllNegotiations(keepGameRoutes: IsLocalGameRouteActive());
        _negotiationData.ClearAll();
        _v3PlayerInfos.Clear();

        tunnelHandler.KeepAliveMonitor.ClearTargets();

        // Re-query STUN in the next lobby: without keepalives running, the NAT mapping
        // behind the cached external endpoint may expire and get remapped.
        tunnelHandler.ClearP2PEndpointCache();
    }

    /// <summary>
    /// Parses one STARTV3 player entry (3 semicolon-delimited fields: id;name;ip:port) from
    /// <paramref name="parts"/> starting at <paramref name="offset"/>, derives the game port
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
    /// Assigns final game IDs/ports/tunnels to every player and builds the
    /// "id;name;address;..." payload used in the STARTV3 message. Sets each player's Port.
    /// </summary>
    public string GenerateV3StartPayload()
    {
        var sb = new StringBuilder();

        // The player order here defines each player's in-game id (port). All clients must
        // iterate in this same order; the STARTV3 handler keys the id off message position.
        for (int i = 0; i < host.Players.Count; i++)
        {
            var player = host.Players[i];
            uint id = GeneratePlayerID(player.Name);
            int port = 48000 - i; // with V3 this is more like an ID for the game (first bytes of packet data)
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

                // In dynamic mode each client uses its own per-peer negotiated tunnel, so this
                // address is informational only; it is only consumed by clients in V3 static mode.
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
    /// Starts the in-game tunnel bridge for the local player. Returns false if the local
    /// player's V3 info could not be found.
    /// </summary>
    public bool StartGameBridge()
    {
        var localV3Player = FindPlayer(ProgramConstants.PLAYERNAME);
        if (localV3Player == null)
        {
            Logger.Log("V3TunnelNegotiationManager: Could not find local V3 player info.");
            return false;
        }

        tunnelHandler.StartGameBridge(localV3Player.Id, localV3Player.PlayerGameId, _v3PlayerInfos);
        return true;
    }

    /// <summary>
    /// Handles a keepalive round trip completing for a pair involving the local player:
    /// refreshes the live ping, and — if the pair had been declared lost — restores it.
    /// Runs on the game thread.
    /// </summary>
    private void OnKeepAlivePongReceived(uint remoteId, int rttMs)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic)
            return;

        var player = _v3PlayerInfos.FirstOrDefault(p => p.Id == remoteId);
        if (player == null || player.Name == ProgramConstants.PLAYERNAME || player.IsNegotiating)
            return;

        var pInfo = host.Players.Find(p => p.Name == player.Name);
        if (pInfo == null)
            return;

        string localName = ProgramConstants.PLAYERNAME;
        var reportedStatus = _negotiationData.GetReportedStatus(localName, player.Name);

        if (reportedStatus == NegotiationStatus.Failed)
        {
            // The peer answers again (e.g. cable replugged) — the negotiated path works,
            // so bring the pair back rather than requiring a full renegotiation.
            host.AddNotice(string.Format("Connection with {0} restored.".L10N("Client:Main:ConnectionRestored"), player.Name), Color.LightGreen);
            _negotiationData.UpdateStatus(localName, player.Name, NegotiationStatus.Succeeded);
            _negotiationData.UpdatePing(localName, player.Name, rttMs);
            BroadcastNegotiationInfo();
            host.OnLocalNegotiationStatus(pInfo, NegotiationStatus.Succeeded, rttMs);
            host.OnNegotiationStateChanged();
            return;
        }

        if (reportedStatus != NegotiationStatus.Succeeded)
            return;

        var previousPing = _negotiationData.GetPing(localName, player.Name);
        _negotiationData.UpdatePing(localName, player.Name, rttMs);
        host.OnPairPingUpdated(pInfo, rttMs);

        // Quiet broadcast: only push the refreshed ping over IRC when it changed
        // materially — otherwise every pong would trigger wire traffic.
        if (previousPing == null || !previousPing.Value.IsValid() ||
            PingQualityRules.IsMaterialChange(previousPing.Value.Milliseconds, rttMs))
        {
            BroadcastNegotiationInfo();
        }

        // The local UI (status panel, launch button) is cheap to refresh and early-outs
        // when the panel is closed, so keep it live on every pong rather than only on
        // material changes.
        host.OnNegotiationStateChanged();
    }

    /// <summary>
    /// Handles a peer missing several keepalive pings in a row: announce it and mark the
    /// pair failed, which blocks the host's launch through the existing gate and informs
    /// everyone else through the existing report broadcast. Runs on the game thread.
    /// </summary>
    private void OnKeepAliveTimedOut(uint remoteId)
    {
        if (host.TunnelMode != TunnelMode.V3Dynamic)
            return;

        var player = _v3PlayerInfos.FirstOrDefault(p => p.Id == remoteId);
        if (player == null || player.Name == ProgramConstants.PLAYERNAME || player.IsNegotiating)
            return;

        string localName = ProgramConstants.PLAYERNAME;
        if (_negotiationData.GetReportedStatus(localName, player.Name) != NegotiationStatus.Succeeded)
            return;

        host.AddNotice(string.Format("Lost connection with {0} — they are not responding to connection checks.".L10N("Client:Main:ConnectionLostKeepAlive"), player.Name), Color.Red);

        _negotiationData.UpdateStatus(localName, player.Name, NegotiationStatus.Failed);
        BroadcastNegotiationInfo();

        var pInfo = host.Players.Find(p => p.Name == player.Name);
        if (pInfo != null)
            host.OnLocalNegotiationStatus(pInfo, NegotiationStatus.Failed, -1);

        host.OnNegotiationStateChanged();
    }

    /// <summary>
    /// Rebuilds the tunnel handler's lobby keepalive targets from the currently negotiated
    /// tunnels, so NAT mappings and tunnel registrations stay alive between negotiation and
    /// game start. Cleared when not in dynamic mode or when nothing is negotiated.
    /// </summary>
    private void RefreshKeepAliveTargets()
    {
        var localV3Player = FindPlayer(ProgramConstants.PLAYERNAME);
        if (localV3Player == null || host.TunnelMode != TunnelMode.V3Dynamic)
        {
            tunnelHandler.KeepAliveMonitor.ClearTargets();
            return;
        }

        var targets = _v3PlayerInfos
            .Where(p => p.Name != ProgramConstants.PLAYERNAME && p.Tunnel != null)
            .Select(p => (p.Id, p.Tunnel!))
            .ToList();

        if (targets.Count == 0)
            tunnelHandler.KeepAliveMonitor.ClearTargets();
        else
            tunnelHandler.KeepAliveMonitor.SetTargets(localV3Player.Id, targets);
    }

    /// <summary>
    /// Removes a pair's P2P routing entries. When <paramref name="keepChosenTunnel"/> is set
    /// and the player's negotiated tunnel is a direct path, that endpoint survives so the
    /// game bridge can keep using it; everything else (candidate and auto-learned endpoints
    /// from the finished negotiation) is dropped so stale entries don't accumulate.
    /// </summary>
    private void CleanupP2PForPlayer(V3PlayerInfo player, bool keepChosenTunnel)
    {
        var localV3Player = FindPlayer(ProgramConstants.PLAYERNAME);
        if (localV3Player == null || player.Name == ProgramConstants.PLAYERNAME)
            return;

        IPEndPoint? keepEndpoint = keepChosenTunnel && player.Tunnel is P2PTunnel p2pTunnel
            ? p2pTunnel.PeerEndpoint
            : null;

        tunnelHandler.CleanupP2PPair(localV3Player.Id, player.Id, keepEndpoint);
    }

    /// <summary>
    /// Queues a pair's P2P routing entry for cleanup once <see cref="TunnelHandler.GameBridgeStopped"/>
    /// fires. Call this whenever a chosen P2P path was just kept alive past its normal cleanup
    /// point (<paramref name="gameRouteActive"/> was true) so it isn't kept forever —
    /// <see cref="FlushDeferredP2PCleanups"/> skips any pair whose player is still tracked locally,
    /// so entries added defensively for players who are still around are simply no-ops.
    /// </summary>
    private void DeferP2PCleanupIfNeeded(V3PlayerInfo player, bool gameRouteActive)
    {
        if (!gameRouteActive || player.Tunnel is not P2PTunnel)
            return;

        var localV3Player = FindPlayer(ProgramConstants.PLAYERNAME);
        if (localV3Player == null || player.Name == ProgramConstants.PLAYERNAME)
            return;

        if (!_deferredP2PCleanups.Contains((localV3Player.Id, player.Id)))
            _deferredP2PCleanups.Add((localV3Player.Id, player.Id));
    }

    /// <summary>
    /// P2P cleanup for a player leaving the lobby. While a game route is active their
    /// chosen path must survive (the bridge may still be forwarding to them — e.g. they
    /// only lost IRC, not the game connection), so the full cleanup is deferred until
    /// <see cref="TunnelHandler.GameBridgeStopped"/> fires.
    /// </summary>
    private void CleanupP2PForRemovedPlayer(V3PlayerInfo player)
    {
        bool gameRouteActive = IsLocalGameRouteActive();
        CleanupP2PForPlayer(player, keepChosenTunnel: gameRouteActive);
        DeferP2PCleanupIfNeeded(player, gameRouteActive);
    }

    /// <summary>
    /// Runs the P2P cleanups that were deferred because a game route was still active,
    /// skipping any pair whose player has since rejoined the lobby.
    /// </summary>
    private void FlushDeferredP2PCleanups()
    {
        foreach (var (localId, remoteId) in _deferredP2PCleanups)
        {
            if (_v3PlayerInfos.Any(p => p.Id == remoteId))
                continue;

            tunnelHandler.CleanupP2PPair(localId, remoteId);
        }

        _deferredP2PCleanups.Clear();
    }

    private bool IsLocalGameRouteActive()
        => tunnelHandler.GameTunnelBridge?.IsRunning == true || ProgramConstants.IsInGame;

    private void AttachNegotiator(V3PlayerInfo player)
    {
        if (player.Negotiator == null)
            return;

        player.Negotiator.NegotiationResult += OnPlayerNegotiationResult;
        player.Negotiator.NegotiationComplete += OnPlayerNegotiationComplete;
    }

    private void DetachNegotiator(V3PlayerInfo player)
    {
        if (player.Negotiator == null)
            return;

        player.Negotiator.NegotiationResult -= OnPlayerNegotiationResult;
        player.Negotiator.NegotiationComplete -= OnPlayerNegotiationComplete;
    }
}
