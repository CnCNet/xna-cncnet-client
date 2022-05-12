using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public class QmRequestSpawnResponse : QmRequestResponse
    {
        [JsonProperty("spawn")]
        public QmRequestSpawnResponseSpawn Spawn { get; set; }
    }
}