using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;

public class QmSpawnResponse : QmResponseMessage
{
    [JsonProperty("spawn")]
    public QmSpawn Spawn { get; set; }
}