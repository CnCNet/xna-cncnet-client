#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// Keeps each map's <see cref="MapTileData"/> on disk, so a map file is only read again when it changes.
    /// Covers official and custom maps, keyed by the map's base file path. Separate from the custom map
    /// cache, because official maps need this data too.
    /// </summary>
    internal sealed class MapTileDataCache
    {
        private const int CurrentVersion = 1;

        private static readonly string CacheFilePath = SafePath.CombineFilePath(
            ProgramConstants.ClientUserFilesPath, $"map_tile_data_v{CurrentVersion}");

        private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };

        private readonly IReadOnlyList<AutoMapOverlayDefinition> overlayDefinitions;
        private readonly string configHash;
        private readonly ConcurrentDictionary<string, MapTileData> entries;
        private readonly object saveLock = new();

        // Keys used during the initial map load; anything else belongs to a map that no longer exists.
        private ConcurrentDictionary<string, byte>? keysInUse = new();

        private MapTileDataCache(IReadOnlyList<AutoMapOverlayDefinition> overlayDefinitions, string configHash,
            ConcurrentDictionary<string, MapTileData> entries)
        {
            this.overlayDefinitions = overlayDefinitions;
            this.configHash = configHash;
            this.entries = entries;
        }

        /// <summary>
        /// Loads the cache from disk. Starts empty if there is none, or if it was written by another
        /// cache version or for different <paramref name="overlayDefinitions"/>.
        /// </summary>
        public static MapTileDataCache Load(IReadOnlyList<AutoMapOverlayDefinition> overlayDefinitions)
        {
            string configHash = ComputeConfigHash(overlayDefinitions);
            var entries = new ConcurrentDictionary<string, MapTileData>();

            try
            {
                if (File.Exists(CacheFilePath))
                {
                    var file = JsonSerializer.Deserialize<CacheFile>(File.ReadAllText(CacheFilePath), JsonOptions);
                    if (file != null && file.Version == CurrentVersion && file.ConfigHash == configHash)
                        entries = file.Maps;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"MapTileDataCache: Failed to load {CacheFilePath}: {ex.Message}");
            }

            return new MapTileDataCache(overlayDefinitions, configHash, entries);
        }

        /// <summary>
        /// Applies the map's start location heights and detected building icons, reading them
        /// from the map file when the cached entry is missing or stale.
        /// </summary>
        public void Apply(Map map)
        {
            if (!MainClientConstants.USE_ISOMETRIC_CELLS)
                return;

            string key = NormalizePath(map.BaseFilePath);
            keysInUse?.TryAdd(key, 0);

            if (!entries.TryGetValue(key, out MapTileData? data) || data.IsOutdated(map.CompleteFilePath))
            {
                data = MapTileDataReader.Read(map, overlayDefinitions);
                if (data == null)
                    return;

                entries[key] = data;
            }

            map.ApplyTileData(data);
        }

        /// <summary>Removes the entry of a deleted map.</summary>
        public void Remove(string baseFilePath) => entries.TryRemove(NormalizePath(baseFilePath), out _);

        /// <summary>
        /// Call once after the initial map load: removes entries for maps that were not loaded,
        /// so the cache does not grow with every deleted or renamed map.
        /// </summary>
        public void RemoveUnused()
        {
            var used = Interlocked.Exchange(ref keysInUse, null);
            if (used == null)
                return;

            foreach (string key in entries.Keys)
            {
                if (!used.ContainsKey(key))
                    entries.TryRemove(key, out _);
            }
        }

        public void Save()
        {
            if (!MainClientConstants.USE_ISOMETRIC_CELLS)
                return;

            try
            {
                lock (saveLock)
                {
                    var file = new CacheFile { Version = CurrentVersion, ConfigHash = configHash, Maps = entries };
                    File.WriteAllText(CacheFilePath, JsonSerializer.Serialize(file, JsonOptions));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"MapTileDataCache: Failed to save {CacheFilePath}: {ex.Message}");
            }
        }

        private static string NormalizePath(string baseFilePath)
            => baseFilePath
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

        /// <summary>
        /// A short hash of the overlay definitions, so the cache is rebuilt when they change.
        /// </summary>
        private static string ComputeConfigHash(IReadOnlyList<AutoMapOverlayDefinition> definitions)
        {
            if (definitions.Count == 0)
                return "empty";

            string text = string.Join("|", definitions.Select(d =>
                $"{d.BuildingId},{d.TextureName},{d.OwnerFilter},{d.CellOffsetX},{d.CellOffsetY},{d.Toggleable}"));

            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return string.Concat(hash.Take(8).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        }

        /// <summary>The JSON file layout.</summary>
        private sealed class CacheFile
        {
            [JsonInclude]
            [JsonPropertyName("version")]
            public required int Version { get; set; }

            [JsonInclude]
            [JsonPropertyName("configHash")]
            public required string ConfigHash { get; set; }

            [JsonInclude]
            [JsonPropertyName("maps")]
            public required ConcurrentDictionary<string, MapTileData> Maps { get; set; }
        }
    }
}
