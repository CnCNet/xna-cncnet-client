using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer
{
    public class CustomMapCache
    {
        [JsonProperty("version")]
        public int Version { get; set; }
        
        [JsonProperty("maps")]
        public ConcurrentDictionary<string, Map> Maps { get; set; }
    }
}
