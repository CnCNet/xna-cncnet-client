using System;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmLadderStats
{
    [JsonProperty("recentMatchedPlayers")]
    public int RecentMatchedPlayerCount { get; set; }

    [JsonProperty("queuedPlayers")]
    public int QueuedPlayerCount { get; set; }

    [JsonProperty("past24hMatches")]
    public int Past24HourMatchCount { get; set; }

    [JsonProperty("recentMatches")]
    public int RecentMatchCount { get; set; }

    [JsonProperty("activeMatches")]
    public int ActiveMatchCount { get; set; }

    [JsonProperty("time")]
    public QmLadderStatsTime Time { get; set; }
}