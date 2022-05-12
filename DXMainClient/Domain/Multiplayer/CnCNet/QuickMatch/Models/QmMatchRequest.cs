using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public class QmMatchRequest : QmRequest
    {
        [JsonProperty("lan_ip")]
        public string LanIP { get; set; }

        [JsonProperty("lan_port")]
        public string LanPort { get; set; }

        [JsonProperty("ipv6_address")]
        public string IPv6Address { get; set; }

        [JsonProperty("ipv6_port")]
        public string IPv6Port { get; set; }

        [JsonProperty("ip_address")]
        public string IPAddress { get; set; }

        [JsonProperty("ip_port")]
        public string IPPort { get; set; }

        [JsonProperty("side")]
        public int Side { get; set; }

        [JsonProperty("map_bitfield")]
        public string MapBitfield { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; } = "2.0";

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("map_sides")]
        public string[] MapSides { get; set; }

        [JsonProperty("ai_dat")]
        public string CheatSeen { get; set; }

        [JsonProperty("exe_hash")]
        public string ExeHash { get; set; }

        [JsonProperty("ddraw")]
        public string DDrawHash { get; set; }

        [JsonProperty("session")]
        public string Session { get; set; }

        public QmMatchRequest()
        {
            Type = QmRequestTypes.MatchMeUp;
        }
    }
}