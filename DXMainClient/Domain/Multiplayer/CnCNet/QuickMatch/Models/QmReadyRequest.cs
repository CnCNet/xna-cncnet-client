using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public abstract class QmReadyRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("status")]
        public string Status { get; private set; }

        [JsonProperty("seed")]
        public int Seed { get; set; }

        public QmReadyRequest()
        {
            Type = "update";
            Status = "ready";
        }
    }
}