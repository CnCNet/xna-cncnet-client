using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public abstract class QmRequest
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; } = QmService.QmVersion;
}