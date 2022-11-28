using System;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmLadderStatsTime
{
    [JsonProperty("date")]
    public DateTime Date { get; set; }

    [JsonProperty("timezone_type")]
    public int TimezoneType { get; set; }

    [JsonProperty("timezone")]
    public string Timezone { get; set; }
}