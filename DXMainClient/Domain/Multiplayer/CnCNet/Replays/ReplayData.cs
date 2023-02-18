using System;
using System.Text.Json.Serialization;

namespace DTAClient.Domain.Multiplayer.CnCNet.Replays;

internal readonly record struct ReplayData(
    [property: JsonPropertyName("t")] TimeSpan TimestampOffset,
    [property: JsonPropertyName("p")] uint PlayerId,
    [property: JsonPropertyName("g")][property: JsonConverter(typeof(GameDataJsonConverter))] ReadOnlyMemory<byte> GameData,
    [property: JsonPropertyName("v")] byte Version = 1);