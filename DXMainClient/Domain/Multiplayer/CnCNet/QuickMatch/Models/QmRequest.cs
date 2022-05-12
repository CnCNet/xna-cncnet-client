using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public abstract class QmRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonIgnore]
        public string Ladder { get; set; }

        [JsonIgnore]
        public string PlayerName { get; set; }
    }
}