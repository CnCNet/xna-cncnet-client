using System;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Converters;

/// <summary>
///  The response from the Ladder api for a match request can come back in a few different response types:
/// <see cref="QmResponseTypes"/>
/// </summary>
public class QmRequestResponseConverter : JsonConverter<QmResponse>
{
    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, QmResponse value, JsonSerializer serializer) => throw new NotImplementedException();

    public override QmResponse ReadJson(JsonReader reader, Type objectType, QmResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JObject.Load(reader);
        string responseType = token[QmResponse.TypeKey]?.ToString();

        if (responseType == null)
            return null;

        Type subType = QmResponse.GetSubType(responseType);

        existingValue ??= Activator.CreateInstance(subType) as QmResponse;

        if (existingValue == null)
            return null;

        using JsonReader subReader = token.CreateReader();
        serializer.Populate(subReader, existingValue);

        return existingValue;
    }
}