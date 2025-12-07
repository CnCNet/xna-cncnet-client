using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization;

namespace DTAClient.Domain.Multiplayer
{
    public class CustomMapCache
    {
        [JsonInclude]
        [JsonPropertyName("version")]
        public required int Version { get; set; }

        [JsonInclude]
        [JsonPropertyName("maps")]
        public required ConcurrentDictionary<string, Item> Items { get; set; }

        public class Item
        {
            [JsonInclude]
            public required Map Map { get; init; }

            [JsonInclude]
            public required long FileSize { get; init; }

            [JsonInclude]
            public required DateTime LastWriteTimeUtc { get; init; }

            public Item() : base() { }

            [SetsRequiredMembers]
            public Item(Map map)
            {
                Map = map;

                FileInfo fileInfo = new(Map.CompleteFilePath);
                if (fileInfo.Exists)
                {
                    FileSize = fileInfo.Length;
                    LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                }
            }

            public void RefreshIfOutdated()
            {
                FileInfo fileInfo = new(Map.CompleteFilePath);
                bool recalculateSHA = fileInfo.Exists && (fileInfo.Length != FileSize || fileInfo.LastWriteTimeUtc != LastWriteTimeUtc);
                Map.AfterDeserialize(recalculateSHA);
            }
        }
    }
}
