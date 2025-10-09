using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using DTAClient.DXGUI.Multiplayer.GameLobby;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Manages negotiation status and ping data for all player pairs in a game lobby.
/// </summary>
public class NegotiationDataManager
{
    // reportingPlayer -> targetPlayer -> status
    // This tracks what each player reports about their negotiation with each other player
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, NegotiationStatus>> _negotiationStatuses = new();

    // reportingPlayer -> targetPlayer -> ping (in milliseconds)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _playerPingMatrix = new();

    /// <summary>
    /// Updates the negotiation status reported by one player about another.
    /// </summary>
    public void UpdateStatus(string reportingPlayer, string targetPlayer, NegotiationStatus status)
    {
        var reporterStatuses = _negotiationStatuses.GetOrAdd(reportingPlayer,
            _ => new ConcurrentDictionary<string, NegotiationStatus>());
        reporterStatuses[targetPlayer] = status;
    }

    /// <summary>
    /// Updates the ping reported by one player to another.
    /// </summary>
    public void UpdatePing(string reportingPlayer, string targetPlayer, int ping)
    {
        var reporterPings = _playerPingMatrix.GetOrAdd(reportingPlayer,
            _ => new ConcurrentDictionary<string, int>());
        reporterPings[targetPlayer] = ping;
    }

    /// <summary>
    /// Gets the negotiation status between two players by checking both directions.
    /// Returns the first status found, checking player1->player2 then player2->player1.
    /// </summary>
    public NegotiationStatus GetNegotiationStatus(string player1, string player2)
    {
        // Players don't negotiate with themselves
        if (player1 == player2)
            return NegotiationStatus.NotStarted;

        // Either player could be the reporter
        if (_negotiationStatuses.TryGetValue(player1, out var player1Statuses) &&
            player1Statuses.TryGetValue(player2, out var status))
            return status;

        return _negotiationStatuses.TryGetValue(player2, out var player2Statuses) &&
            player2Statuses.TryGetValue(player1, out status)
            ? status
            : NegotiationStatus.NotStarted;
    }

    /// <summary>
    /// Gets the ping between two players by checking both directions.
    /// Returns the first ping found, checking player1->player2 then player2->player1.
    /// </summary>
    public int? GetPing(string player1, string player2)
    {
        // Players don't have ping to themselves
        if (player1 == player2)
            return null;

        if (_playerPingMatrix.TryGetValue(player1, out var player1Pings) &&
            player1Pings.TryGetValue(player2, out var ping))
            return ping;

        if (_playerPingMatrix.TryGetValue(player2, out var player2Pings) &&
            player2Pings.TryGetValue(player1, out ping))
            return ping;

        return null;
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
    /// Checks if all negotiations have finished (either succeeded or failed).
    /// </summary>
    public bool AreAllNegotiationsComplete(IReadOnlyList<string> playerNames)
    {
        foreach (var (player1, player2) in GetPlayerPairs(playerNames))
        {
            var status = GetNegotiationStatus(player1, player2);
            if (status == NegotiationStatus.NotStarted || status == NegotiationStatus.InProgress)
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