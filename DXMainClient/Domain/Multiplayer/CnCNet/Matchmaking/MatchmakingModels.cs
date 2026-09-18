#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DTAClient.Domain.Multiplayer.CnCNet.Matchmaking
{
    public class QmMatchRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("casual")]
        public bool Casual { get; set; } = true;

        [JsonPropertyName("side")]
        public int Side { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.0";

        [JsonPropertyName("client_version")]
        public string? ClientVersion { get; set; }

        [JsonPropertyName("lan_ip")]
        public string? LanIp { get; set; }

        [JsonPropertyName("lan_port")]
        public int? LanPort { get; set; }

        [JsonPropertyName("ip_address")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("ip_port")]
        public int? IpPort { get; set; }
    }

    public class QmMatchResponse
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("checkback")]
        public int CheckBack { get; set; } = 5;

        [JsonPropertyName("gameID")]
        public long? GameID { get; set; }

        [JsonPropertyName("spawn")]
        public Dictionary<string, Dictionary<string, object>>? Spawn { get; set; }

        [JsonPropertyName("spawnmap")]
        public Dictionary<string, Dictionary<string, object>>? SpawnMap { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
