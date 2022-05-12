using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public class QmRequestWaitResponse : QmRequestResponse
    {
        [JsonProperty("checkback")]
        public int CheckBack { get; set; }

        [JsonProperty("no_sooner_than")]
        public int NoSoonerThan { get; set; }
    }
}