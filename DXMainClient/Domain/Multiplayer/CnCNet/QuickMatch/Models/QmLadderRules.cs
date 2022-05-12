using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public class QmLadderRules
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("ladder_id")]
        public int LadderId { get; set; }

        [JsonProperty("player_count")]
        public int PlayerCount { get; set; }

        [JsonProperty("map_vetoes")]
        public int MapVetoes { get; set; }

        [JsonProperty("max_difference")]
        public int MaxDifference { get; set; }

        [JsonProperty("all_sides")]
        private string allSides { get; set; }

        [JsonIgnore]
        public IEnumerable<int> AllSides => allSides?.Split(',').Select(int.Parse) ?? new List<int>();

        [JsonProperty("allowed_sides")]
        private string allowedSides { get; set; }

        [JsonIgnore]
        public IEnumerable<int> AllowedSides => allSides?.Split(',').Select(int.Parse) ?? new List<int>();

        [JsonProperty("bail_time")]
        public int BailTime { get; set; }

        [JsonProperty("bail_fps")]
        public int BailFps { get; set; }

        [JsonProperty("tier2_rating")]
        public int Tier2Rating { get; set; }

        [JsonProperty("rating_per_second")]
        public double RatingPerSecond { get; set; }

        [JsonProperty("max_points_difference")]
        public int MaxPointsDifference { get; set; }

        [JsonProperty("points_per_second")]
        public int PointsPerSecond { get; set; }

        [JsonProperty("use_elo_points")]
        private int useEloPoints { get; set; }

        [JsonIgnore]
        public bool UseEloPoints => useEloPoints == 1;

        [JsonProperty("wol_k")]
        public int WolK { get; set; }

        [JsonProperty("show_map_preview")]
        private int showMapPreview { get; set; }

        [JsonIgnore]
        public bool ShowMapPreview => showMapPreview == 1;

        [JsonProperty("reduce_map_repeats")]
        private int reduceMapRepeats { get; set; }

        [JsonIgnore]
        public bool ReduceMapRepeats => reduceMapRepeats == 1;
    }
}
