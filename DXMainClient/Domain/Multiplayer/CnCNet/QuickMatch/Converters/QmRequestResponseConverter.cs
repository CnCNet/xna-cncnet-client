using System;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Converters;

/// <summary>
///  The response from the Ladder api for a match request can come back in a few different response types:
/// <see cref="QmResponseTypes"/>
/// </summary>
public class QmRequestResponseConverter : JsonConverter<QmResponseMessage>
{
    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, QmResponseMessage value, JsonSerializer serializer) => throw new NotImplementedException();

    public override QmResponseMessage ReadJson(JsonReader reader, Type objectType, QmResponseMessage existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JObject.Load(reader);
        string responseType = token[QmResponseMessage.TypeKey]?.ToString();

        if (responseType == null)
            return null;

        Type subType = QmResponseMessage.GetSubType(responseType);

        existingValue ??= Activator.CreateInstance(subType) as QmResponseMessage;

        if (existingValue == null)
            return null;

        using JsonReader subReader = token.CreateReader();
        serializer.Populate(subReader, existingValue);

        return existingValue;
    }
}