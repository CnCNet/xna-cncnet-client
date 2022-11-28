using System;
using System.Collections.Generic;
using System.Linq;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Converters;

public class QmRequestSpawnResponseSpawnConverter : JsonConverter<QmSpawn>
{
    private const int MaxOtherSpawns = 7;

    public override void WriteJson(JsonWriter writer, QmSpawn value, JsonSerializer serializer)
    {
        var obj = new JObject();

        List<QmSpawnOther> others = value?.Others?.ToList() ?? new List<QmSpawnOther>();
        for (int i = 0; i < others.Count; i++)
            obj.Add($"Other{i + 1}", JObject.FromObject(others[i]));

        obj.WriteTo(writer);
    }

    public override QmSpawn ReadJson(JsonReader reader, Type objectType, QmSpawn existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JObject.Load(reader);

        existingValue ??= new QmSpawn();

        // populate base properties that require no specific conversions
        using JsonReader subReader = token.CreateReader();
        serializer.Populate(subReader, existingValue);

        var others = new List<QmSpawnOther>();
        for (int i = 1; i <= MaxOtherSpawns; i++)
        {
            JToken otherN = token[$"Other{i}"];
            if (otherN == null)
                break;

            others.Add(JsonConvert.DeserializeObject<QmSpawnOther>(otherN.ToString()));
        }

        if (others.Any())
            existingValue.Others = others;

        return existingValue;
    }
}