using System;
using System.Collections.Generic;
using System.Linq;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;

public class QmRequestSpawnResponseSpawnConverter : JsonConverter<QmRequestSpawnResponseSpawn>
{
    private const int MaxOtherSpawns = 7;

    public override void WriteJson(JsonWriter writer, QmRequestSpawnResponseSpawn value, JsonSerializer serializer)
    {
        var obj = new JObject();

        List<QmRequestSpawnResponseSpawnOther> others = value?.Others?.ToList() ?? new List<QmRequestSpawnResponseSpawnOther>();
        for (int i = 0; i < others.Count; i++)
            obj.Add($"Other{i + 1}", JObject.FromObject(others[i]));

        obj.WriteTo(writer);
    }

    public override QmRequestSpawnResponseSpawn ReadJson(JsonReader reader, Type objectType, QmRequestSpawnResponseSpawn existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JObject.Load(reader);

        existingValue ??= new QmRequestSpawnResponseSpawn();

        // populate base properties that require no specific conversions
        using JsonReader subReader = token.CreateReader();
        serializer.Populate(subReader, existingValue);

        var others = new List<QmRequestSpawnResponseSpawnOther>();
        for (int i = 1; i <= MaxOtherSpawns; i++)
        {
            JToken otherN = token[$"Other{i}"];
            if (otherN == null)
                break;

            others.Add(JsonConvert.DeserializeObject<QmRequestSpawnResponseSpawnOther>(otherN.ToString()));
        }

        if (others.Any())
            existingValue.Others = others;

        return existingValue;
    }
}