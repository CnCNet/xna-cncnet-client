using System;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    [JsonConverter(typeof(QmRequestResponseConverter))]
    public abstract class QmRequestResponse
    {
        public const string TypeKey = "type";

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonIgnore]
        public bool IsSuccessful => this is not QmRequestErrorResponse;

        public static Type GetSubType(string type)
        {
            return type switch
            {
                QmResponseTypes.Wait => typeof(QmRequestWaitResponse),
                QmResponseTypes.Spawn => typeof(QmRequestSpawnResponse),
                QmResponseTypes.Error => typeof(QmRequestErrorResponse),
                QmResponseTypes.Fatal => typeof(QmRequestFatalResponse),
                QmResponseTypes.Update => typeof(QmRequestUpdateResponse),
                QmResponseTypes.Quit => typeof(QmRequestQuitResponse),
                _ => null
            };
        }
    }
}