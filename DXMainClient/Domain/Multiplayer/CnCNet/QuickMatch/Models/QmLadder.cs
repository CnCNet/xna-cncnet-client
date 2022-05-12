using System.Collections.Generic;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public class QmLadder
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("abbreviation")]
        public string Abbreviation { get; set; }

        [JsonProperty("game")]
        public string Game { get; set; }

        [JsonProperty("clans_allowed")]
        private int clansAllowed { get; set; }

        [JsonIgnore]
        public bool ClansAllowed => clansAllowed == 1;

        [JsonProperty("game_object_schema_id")]
        public int GameObjectSchemaId { get; set; }

        [JsonProperty("map_pool_id")]
        public int MapPoolId { get; set; }

        [JsonProperty("private")]
        private int _private { get; set; }

        [JsonIgnore]
        public bool IsPrivate => _private == 1;

        [JsonProperty("sides")]
        public IEnumerable<QmSide> Sides { get; set; }

        [JsonProperty("vetoes")]
        public int VetoesRemaining { get; set; }

        [JsonProperty("allowed_sides")]
        public IEnumerable<int> AllowedSideLocalIds { get; set; }

        [JsonProperty("current")]
        public string Current { get; set; }

        [JsonProperty("qm_ladder_rules")]
        public QmLadderRules LadderRules { get; set; }
    }
}
