using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;

public class QmRequest
{
    [JsonIgnore]
    public string Url { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; } = QmService.QmVersion;
}