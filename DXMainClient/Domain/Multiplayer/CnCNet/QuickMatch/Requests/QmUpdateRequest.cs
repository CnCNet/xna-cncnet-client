using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;

public abstract class QmUpdateRequest : QmRequest
{
    [JsonProperty("status")]
    public string Status { get; protected set; }

    [JsonProperty("seed")]
    public int Seed { get; set; }

    protected QmUpdateRequest(int seed)
    {
        Type = QmRequestTypes.Update;
        Seed = seed;
    }
}