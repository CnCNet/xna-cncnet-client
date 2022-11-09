using System;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Converters;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;

[JsonConverter(typeof(QmRequestResponseConverter))]
public abstract class QmResponse
{
    public const string TypeKey = "type";

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonIgnore]
    public bool IsSuccessful => this is not QmErrorResponse;

    [JsonIgnore]
    public QmRequest Request { get; set; }

    public static Type GetSubType(string type)
    {
        return type switch
        {
            QmResponseTypes.Wait => typeof(QmWaitResponse),
            QmResponseTypes.Spawn => typeof(QmSpawnResponse),
            QmResponseTypes.Error => typeof(QmErrorResponse),
            QmResponseTypes.Fatal => typeof(QmFatalResponse),
            QmResponseTypes.Update => typeof(QmUpdateResponse),
            QmResponseTypes.Quit => typeof(QmQuitResponse),
            _ => null
        };
    }
}