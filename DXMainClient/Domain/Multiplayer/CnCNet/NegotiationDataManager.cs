#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Manages negotiation status and ping data for all player pairs in a game lobby.
/// </summary>
public class NegotiationDataManager
{
    /// <summary>
    /// This tracks what each player reports about their negotiation with each other player
    /// reportingPlayer -> targetPlayer -> status
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, NegotiationStatus>> _negotiationStatuses = new();

    /// <summary>
    /// This tracks what each player reports about their negotiation with each other player
    /// reportingPlayer -> targetPlayer -> ping
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PingValue>> _playerPingMatrix = new();

    /// <summary>
    /// Updates the negotiation status reported by one player about another.
    /// </summary>
    /// <returns>The previously reported status in this direction, or null if none existed.</returns>
    public NegotiationStatus? UpdateStatus(string reportingPlayer, string targetPlayer, NegotiationStatus status)
    {
        var reporterStatuses = _negotiationStatuses.GetOrAdd(reportingPlayer,
            _ => new ConcurrentDictionary<string, NegotiationStatus>());
        NegotiationStatus? previous = reporterStatuses.TryGetValue(targetPlayer, out var prev) ? prev : null;
        reporterStatuses[targetPlayer] = status;
        return previous;
    }

    /// <summary>
    /// Updates the ping reported by one player to another.
    /// </summary>
    public void UpdatePing(string reportingPlayer, string targetPlayer, int ping)
    {
        var reporterPings = _playerPingMatrix.GetOrAdd(reportingPlayer,
            _ => new ConcurrentDictionary<string, PingValue>());
        reporterPings[targetPlayer] = ping >= 0 ? PingValue.FromMs(ping) : PingValue.Unknown;
    }

    /// <summary>
    /// Gets the status one player has reported about their negotiation with another,
    /// without consulting the reverse direction. Used when broadcasting the local
    /// player's own view over the wire.
    /// </summary>
    public NegotiationStatus GetReportedStatus(string reportingPlayer, string targetPlayer)
        => _negotiationStatuses.TryGetValue(reportingPlayer, out var statuses) &&
           statuses.TryGetValue(targetPlayer, out var status)
            ? status
            : NegotiationStatus.NotStarted;

    /// <summary>
    /// Gets the negotiation status between two players by combining both directions.
    /// A pair is only as healthy as its worst report: a Failed or InProgress report from
    /// either side must not be masked by the other side's (possibly stale) Succeeded, and
    /// the pair only counts as Succeeded once both sides have confirmed it.
    /// </summary>
    public NegotiationStatus GetNegotiationStatus(string player1, string player2)
    {
        if (player1 == player2)
            throw new ArgumentException("Cannot get negotiation status between a player and themselves", nameof(player2));

        NegotiationStatus? status1 = GetDirectionalStatus(player1, player2);
        NegotiationStatus? status2 = GetDirectionalStatus(player2, player1);

        if (status1 == NegotiationStatus.Failed || status2 == NegotiationStatus.Failed)
            return NegotiationStatus.Failed;

        if (status1 == NegotiationStatus.InProgress || status2 == NegotiationStatus.InProgress)
            return NegotiationStatus.InProgress;

        if (status1 == NegotiationStatus.Succeeded && status2 == NegotiationStatus.Succeeded)
            return NegotiationStatus.Succeeded;

        // One side reports success but the other side's confirmation hasn't arrived yet.
        if (status1 == NegotiationStatus.Succeeded || status2 == NegotiationStatus.Succeeded)
            return NegotiationStatus.InProgress;

        return NegotiationStatus.NotStarted;
    }

    /// <summary>
    /// Returns the stored reporter→target status, treating an explicit NotStarted like no report.
    /// </summary>
    private NegotiationStatus? GetDirectionalStatus(string reportingPlayer, string targetPlayer)
    {
        if (_negotiationStatuses.TryGetValue(reportingPlayer, out var statuses) &&
            statuses.TryGetValue(targetPlayer, out var status) &&
            status != NegotiationStatus.NotStarted)
            return status;

        return null;
    }

    /// <summary>
    /// Gets the ping between two players by checking both directions.
    /// Returns the first ping found, checking player1->player2 then player2->player1.
    /// Returns null if no ping data exists for this player pair.
    /// </summary>
    public PingValue? GetPing(string player1, string player2)
    {
        if (player1 == player2)
            throw new ArgumentException("Cannot get ping between a player and themselves", nameof(player2));

        if (_playerPingMatrix.TryGetValue(player1, out var player1Pings) &&
            player1Pings.TryGetValue(player2, out var ping) && ping.IsValid())
            return ping;

        if (_playerPingMatrix.TryGetValue(player2, out var player2Pings) &&
            player2Pings.TryGetValue(player1, out ping) && ping.IsValid())
            return ping;

        return null;
    }

    /// <summary>
    /// Removes the negotiation data between two players only (both directions, status and
    /// ping), leaving each player's pairs with everyone else intact. Use this when a single
    /// pair is renegotiated; <see cref="ClearPlayer"/> is for a player leaving entirely.
    /// </summary>
    public void ClearPair(string player1, string player2)
    {
        if (_negotiationStatuses.TryGetValue(player1, out var statuses1))
            statuses1.TryRemove(player2, out _);
        if (_negotiationStatuses.TryGetValue(player2, out var statuses2))
            statuses2.TryRemove(player1, out _);

        if (_playerPingMatrix.TryGetValue(player1, out var pings1))
            pings1.TryRemove(player2, out _);
        if (_playerPingMatrix.TryGetValue(player2, out var pings2))
            pings2.TryRemove(player1, out _);
    }

    /// <summary>
    /// Removes all negotiation data for a specific player.
    /// This includes data they reported and data others reported about them.
    /// </summary>
    public void ClearPlayer(string playerName)
    {
        _negotiationStatuses.TryRemove(playerName, out _);
        _playerPingMatrix.TryRemove(playerName, out _);

        // Remove this player from all other players' reports
        foreach (var status in _negotiationStatuses.Values)
            status.TryRemove(playerName, out _);

        foreach (var pings in _playerPingMatrix.Values)
            pings.TryRemove(playerName, out _);
    }

    /// <summary>
    /// Clears all negotiation and ping data.
    /// </summary>
    public void ClearAll()
    {
        _negotiationStatuses.Clear();
        _playerPingMatrix.Clear();
    }

    /// <summary>
    /// Generates all unique player pairs from a list of player names.
    /// Avoids duplicates (only returns (A,B), not (B,A)).
    /// </summary>
    public IEnumerable<(string player1, string player2)> GetPlayerPairs(IReadOnlyList<string> playerNames)
    {
        for (int i = 0; i < playerNames.Count; i++)
        {
            for (int j = i + 1; j < playerNames.Count; j++)
            {
                yield return (playerNames[i], playerNames[j]);
            }
        }
    }

    /// <summary>
    /// Checks if all negotiations have been completed successfully.
    /// </summary>
    public bool AreAllNegotiationsSuccessful(IReadOnlyList<string> playerNames)
    {
        foreach (var (player1, player2) in GetPlayerPairs(playerNames))
        {
            var status = GetNegotiationStatus(player1, player2);
            if (status != NegotiationStatus.Succeeded)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Gets a list of all incomplete negotiations (NotStarted or InProgress).
    /// </summary>
    public List<(string player1, string player2, NegotiationStatus status)> GetIncompleteNegotiations(IReadOnlyList<string> playerNames)
    {
        var incomplete = new List<(string, string, NegotiationStatus)>();

        foreach (var (player1, player2) in GetPlayerPairs(playerNames))
        {
            var status = GetNegotiationStatus(player1, player2);
            if (status == NegotiationStatus.NotStarted || status == NegotiationStatus.InProgress)
                incomplete.Add((player1, player2, status));
        }

        return incomplete;
    }

    /// <summary>
    /// Gets a list of all failed negotiation pairs.
    /// </summary>
    public List<(string player1, string player2)> GetFailedPairs(IReadOnlyList<string> playerNames)
    {
        var failedPairs = new List<(string, string)>();

        foreach (var (player1, player2) in GetPlayerPairs(playerNames))
        {
            var status = GetNegotiationStatus(player1, player2);
            if (status == NegotiationStatus.Failed)
                failedPairs.Add((player1, player2));
        }

        return failedPairs;
    }

    /// <summary>
    /// Gets counts of incomplete and failed negotiations.
    /// </summary>
    public (int incomplete, int failed) GetNegotiationStatusCounts(IReadOnlyList<string> playerNames)
    {
        int incomplete = 0, failed = 0;

        foreach (var (player1, player2) in GetPlayerPairs(playerNames))
        {
            var status = GetNegotiationStatus(player1, player2);
            if (status == NegotiationStatus.NotStarted || status == NegotiationStatus.InProgress)
                incomplete++;
            else if (status == NegotiationStatus.Failed)
                failed++;
        }

        return (incomplete, failed);
    }

    /// <summary>
    /// Gets a summary of the current negotiation status across all player pairs.
    /// </summary>
    public string GetStatusSummary(IReadOnlyList<string> playerNames)
    {
        if (playerNames.Count < 2)
            return "No negotiations needed";

        int total = 0;
        int succeeded = 0;
        int failed = 0;
        int inProgress = 0;

        foreach (var (player1, player2) in GetPlayerPairs(playerNames))
        {
            total++;
            var status = GetNegotiationStatus(player1, player2);
            switch (status)
            {
                case NegotiationStatus.Succeeded:
                    succeeded++;
                    break;
                case NegotiationStatus.Failed:
                    failed++;
                    break;
                case NegotiationStatus.InProgress:
                    inProgress++;
                    break;
            }
        }

        if (total == 0)
            return "No negotiations needed";

        if (inProgress > 0)
            return $"Negotiations: {succeeded}/{total} complete ({inProgress} in progress)";
        else if (failed > 0)
            return $"Negotiations: {succeeded}/{total} succeeded ({failed} failed)";
        else if (succeeded == total)
            return "All negotiations complete!";
        else
            return $"Negotiations: {succeeded}/{total} complete";
    }
}
