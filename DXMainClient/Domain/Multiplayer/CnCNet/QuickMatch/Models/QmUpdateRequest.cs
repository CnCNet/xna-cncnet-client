using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public abstract class QmUpdateRequest : QmRequest
{
    [JsonProperty("status")]
    public string Status { get; protected set; }

    [JsonProperty("seed")]
    public int Seed { get; set; }

    protected QmUpdateRequest(int seed)
    {
        Type = "update";
        Seed = seed;
    }
}