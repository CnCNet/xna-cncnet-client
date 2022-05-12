using System;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;

/// <summary>
///  The response from the Ladder api for a match request can come back in a few different response types:
/// <see cref="QmResponseTypes"/>
/// </summary>
public class QmRequestResponseConverter : JsonConverter<QmRequestResponse>
{
    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, QmRequestResponse value, JsonSerializer serializer) => throw new NotImplementedException();

    public override QmRequestResponse ReadJson(JsonReader reader, Type objectType, QmRequestResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JObject.Load(reader);
        string responseType = token[QmRequestResponse.TypeKey]?.ToString();

        if (responseType == null)
            return null;

        Type subType = QmRequestResponse.GetSubType(responseType);

        existingValue ??= Activator.CreateInstance(subType) as QmRequestResponse;

        if (existingValue == null)
            return null;

        using JsonReader subReader = token.CreateReader();
        serializer.Populate(subReader, existingValue);

        return existingValue;
    }
}