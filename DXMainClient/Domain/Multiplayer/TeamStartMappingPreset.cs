using System.Collections.Generic;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer
{
    public class TeamStartMappingPreset
    {
        public const string CustomPresetName = "Custom";
        
        [JsonProperty("n")]
        public string Name { get; set; }

        [JsonProperty("m")]
        public List<TeamStartMapping> TeamStartMappings { get; set; }

        [JsonIgnore]
        public bool IsUserDefined { get; set; }

        [JsonIgnore]
        public bool IsCustom => Name == CustomPresetName;

        [JsonIgnore]
        public bool CanSave => IsCustom || IsUserDefined;

        [JsonIgnore]
        public bool CanDelete => IsUserDefined;

        public TeamStartMappingPreset()
        {
            TeamStartMappings = new List<TeamStartMapping>();
        }
    }
}
