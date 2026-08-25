#nullable enable
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Implemented by a lobby that wants V3 dynamic-tunnel negotiation orchestrated by
/// <see cref="V3TunnelNegotiationManager"/>. The manager owns the shared negotiation
/// state and protocol; the host provides the bits that differ between lobbies
/// (player list, channel, message transport) and reacts to UI-affecting callbacks.
/// </summary>
public interface IV3NegotiationHost
{
    /// <summary>The lobby's current player list.</summary>
    List<PlayerInfo> Players { get; }

    /// <summary>The IRC channel name, used to derive deterministic player IDs.</summary>
    string ChannelName { get; }

    TunnelMode TunnelMode { get; }

    /// <summary>Whether the local player hosts the game.</summary>
    bool IsHost { get; }

    /// <summary>
    /// Sends a full-state negotiation report to the channel as a coalescing GAME_NEGOTIATION_MESSAGE
    /// (replaces any previously queued report, so rapid state changes collapse to one wire message).
    /// </summary>
    void SendNegotiationReport(string message);

    /// <summary>
    /// Sends a CTCP system message to the game channel with the given queue priority.
    /// </summary>
    void SendChannelCTCP(string message, int priority);

    /// <summary>Adds a notice to the lobby chat.</summary>
    void AddNotice(string message, Color color);

    /// <summary>
    /// Raised whenever the overall negotiation state may have changed, so the lobby can
    /// refresh launch/load buttons and any status panels.
    /// </summary>
    void OnNegotiationStateChanged();

    /// <summary>
    /// Raised when a locally driven negotiation updates a peer's status. <paramref name="ping"/>
    /// is negative when unknown (e.g. failure or in-progress).
    /// </summary>
    void OnLocalNegotiationStatus(PlayerInfo player, NegotiationStatus status, int ping);

    /// <summary>
    /// Raised when a remote NEGRPT message updates the status of a player relevant to the
    /// local view. <paramref name="ping"/> is negative when unknown.
    /// </summary>
    void OnRemoteNegotiationStatus(PlayerInfo player, NegotiationStatus status, int ping);

    /// <summary>
    /// Raised when negotiations are reset/restarted, so the lobby can clear any one-shot
    /// state (e.g. a "negotiations complete" notice guard).
    /// </summary>
    void OnNegotiationsRestarted();

    /// <summary>
    /// Raised when a keepalive round trip refreshes the measured ping for a negotiated
    /// pair involving the local player, so the lobby can keep its ping display live.
    /// </summary>
    void OnPairPingUpdated(PlayerInfo player, int ping);
}
