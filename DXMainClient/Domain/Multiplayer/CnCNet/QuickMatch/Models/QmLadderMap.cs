using System.Collections.Generic;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmLadderMap
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("ladder_id")]
    public int LadderId { get; set; }

    [JsonProperty("map_id")]
    public int MapId { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("bit_idx")]
    public int BitIndex { get; set; }

    [JsonProperty("valid")]
    private int valid { get; set; }

    [JsonIgnore]
    public bool IsValid => valid == 1;

    [JsonProperty("spawn_order")]
    public string SpawnOrder { get; set; }

    [JsonProperty("team1_spawn_order")]
    public string Team1SpawnOrder { get; set; }

    [JsonProperty("team2_spawn_order")]
    public string Team2SpawnOrder { get; set; }

    [JsonProperty("allowed_sides")]
    public IEnumerable<int> AllowedSideIds { get; set; }

    [JsonProperty("admin_description")]
    public string AdminDescription { get; set; }

    [JsonProperty("map_pool_id")]
    public int? MapPoolId { get; set; }

    [JsonProperty("rejectable")]
    private int rejectable { get; set; }

    [JsonIgnore]
    public bool IsRejectable => rejectable == 1;

    [JsonProperty("default_reject")]
    private int defaultReject { get; set; }

    [JsonIgnore]
    public bool IsDefaultReject => defaultReject == 1;

    [JsonProperty("hash")]
    public string Hash { get; set; }

    [JsonProperty("map")]
    public QmMap Map { get; set; }
}