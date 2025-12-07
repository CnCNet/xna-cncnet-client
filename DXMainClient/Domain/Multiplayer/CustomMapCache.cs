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

        public record Item
        {
            [JsonInclude]
            public required Map Map { get; init; }

            [JsonInclude]
            public long FileSize { get; private set; }

            [JsonInclude]
            public DateTime LastWriteTimeUtc { get; private set; }

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
                else
                {
                    FileSize = 0;
                    LastWriteTimeUtc = DateTime.MinValue;
                }
            }

            public void RefreshIfOutdated()
            {
                Item refreshedItem = new(Map);
                bool recalculateSHA = refreshedItem.FileSize != FileSize || refreshedItem.LastWriteTimeUtc != LastWriteTimeUtc;
                if (recalculateSHA)
                {
                    FileSize = refreshedItem.FileSize;
                    LastWriteTimeUtc = refreshedItem.LastWriteTimeUtc;

                    Map.AfterDeserialize(recalculateSHA);
                }
            }
        }
    }
}
