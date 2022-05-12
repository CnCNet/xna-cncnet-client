using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public class QmUserAccount
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("ladder_id")]
        public int LadderId { get; set; }

        [JsonProperty("card_id")]
        public int CardId { get; set; }

        [JsonProperty("ladder")]
        public QmLadder Ladder { get; set; }
    }
}
