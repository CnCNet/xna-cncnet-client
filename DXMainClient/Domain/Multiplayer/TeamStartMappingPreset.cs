using System.Collections.Generic;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer
{
    public class TeamStartMappingPreset
    {
        public const string UserDefinedPrefx = "[U]";
        
        [JsonProperty("n")]
        public string Name { get; set; }

        [JsonProperty("m")]
        public List<TeamStartMapping> TeamStartMappings { get; set; }

        public bool IsCustom { get; set; }

        public bool IsUserDefined { get; set; }
        
        public bool IsDefaultForMap { get; set; }

        public TeamStartMappingPreset()
        {
            TeamStartMappings = new List<TeamStartMapping>();
        }
    }
}
