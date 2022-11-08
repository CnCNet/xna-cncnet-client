using System.Collections.Generic;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public class QmMatchRequest : QmRequest
    {
        [JsonProperty("lan_ip")]
        public string LanIP { get; set; }

        [JsonProperty("lan_port")]
        public int LanPort { get; set; }

        [JsonProperty("ipv6_address")]
        public string IPv6Address { get; set; }

        [JsonProperty("ipv6_port")]
        public int IPv6Port { get; set; }

        [JsonProperty("ip_address")]
        public string IPAddress { get; set; }

        [JsonProperty("ip_port")]
        public int IPPort { get; set; }

        [JsonProperty("side")]
        public int Side { get; set; }

        [JsonProperty("map_bitfield")]
        public string MapBitfield { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("map_sides")]
        public IEnumerable<int> MapSides { get; set; }

        [JsonProperty("ai_dat")]
        public bool CheatSeen { get; set; }

        [JsonProperty("exe_hash")]
        public string ExeHash { get; set; }

        [JsonProperty("ddraw")]
        public string DDrawHash { get; set; }

        [JsonProperty("session")]
        public string Session { get; set; }

        public QmMatchRequest()
        {
            Type = QmRequestTypes.MatchMeUp;
            MapBitfield = int.MaxValue.ToString();
            Platform = "win32";
            Session = string.Empty;
            DDrawHash = "8a00ba609f7d030c67339e1f555199bdb4054b67";
        }
    }
}