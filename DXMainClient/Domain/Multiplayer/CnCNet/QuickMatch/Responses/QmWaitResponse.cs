using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;

public class QmWaitResponse : QmResponseMessage
{
    [JsonProperty("checkback")]
    public int CheckBack { get; set; }

    [JsonProperty("no_sooner_than")]
    public int NoSoonerThan { get; set; }
}