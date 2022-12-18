using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DTAClient.Domain.Multiplayer.CnCNet.Replays;

internal readonly record struct Replay(
    [property: JsonPropertyName("i")] int Id,
    [property: JsonPropertyName("s")] string Settings,
    [property: JsonPropertyName("t")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("p")] uint RecordingPlayerId,
    [property: JsonPropertyName("m")] Dictionary<uint, string> PlayerMappings,
    [property: JsonPropertyName("d")] List<ReplayData> Data,
    [property: JsonPropertyName("v")] byte Version = 1);