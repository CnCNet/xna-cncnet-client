using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTAClient.Domain.Multiplayer.CnCNet.Replays;

internal sealed class GameDataJsonConverter : JsonConverter<ReadOnlyMemory<byte>>
{
    public override ReadOnlyMemory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetBytesFromBase64());

    public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte> value, JsonSerializerOptions options)
        => writer.WriteBase64StringValue(value.Span);
}