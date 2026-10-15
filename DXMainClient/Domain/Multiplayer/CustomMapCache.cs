using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

using ClientCore;

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

            [JsonInclude]
            public string SupplementalFilesSignature { get; private set; }

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

                SupplementalFilesSignature = string.Join(
                    "|",
                    map.GetMapFilePaths()
                        .Skip(1)
                        .Select(path =>
                        {
                            var fileInfo = new FileInfo(path);
                            return Path.GetExtension(path).ToLowerInvariant() + ":" +
                                   fileInfo.Length + ":" +
                                   fileInfo.LastWriteTimeUtc.Ticks;
                        }));
            }

            public bool IsOutdated()
            {
                Item refreshedItem = new(Map);
                return refreshedItem.FileSize != FileSize
                       || refreshedItem.LastWriteTimeUtc != LastWriteTimeUtc
                       || refreshedItem.SupplementalFilesSignature != SupplementalFilesSignature;
            }
        }
    }
}
