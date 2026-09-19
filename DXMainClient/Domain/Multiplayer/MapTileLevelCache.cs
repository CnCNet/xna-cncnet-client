using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// Persistent cache of per-map tile height data decoded from IsoMapPack5.
    /// Covers both official and custom maps, keyed by normalized BaseFilePath.
    /// Separate from CustomMapCache — stores only the height data needed for
    /// accurate preview overlay and starting-position placement.
    /// </summary>
    public class MapTileLevelCache
    {
        [JsonInclude]
        [JsonPropertyName("version")]
        public required int Version { get; set; }

        /// <summary>
        /// Short hash of the [AutoMapOverlays] config. If this changes all entries are discarded
        /// so the building detection is re-run with the new definitions.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("configHash")]
        public required string ConfigHash { get; set; }

        [JsonInclude]
        [JsonPropertyName("maps")]
        public required ConcurrentDictionary<string, Item> Items { get; set; }

        public sealed class Item
        {
            [JsonInclude]
            public long FileSize { get; init; }

            [JsonInclude]
            public DateTime LastWriteTimeUtc { get; init; }

            /// <summary>
            /// Tile height level for each waypoint, indexed to match the map's waypoints list.
            /// Null if the map is non-isometric or has no waypoints.
            /// </summary>
            [JsonInclude]
            public int[] WaypointLevels { get; init; }

            /// <summary>
            /// Auto-detected building overlays including the height level at each building's cell.
            /// Stored as cell coordinates (converted to preview pixels at render time).
            /// </summary>
            [JsonInclude]
            public List<ExtraMapPreviewTexture> BuildingOverlays { get; init; }

            [JsonConstructor]
            public Item() { }

            public Item(long fileSize, DateTime lastWriteTimeUtc,
                int[] waypointLevels, List<ExtraMapPreviewTexture> buildingOverlays)
            {
                FileSize = fileSize;
                LastWriteTimeUtc = lastWriteTimeUtc;
                WaypointLevels = waypointLevels;
                BuildingOverlays = buildingOverlays;
            }

            public bool IsOutdated(string completeFilePath)
            {
                var fi = new FileInfo(completeFilePath);
                return !fi.Exists || fi.Length != FileSize || fi.LastWriteTimeUtc != LastWriteTimeUtc;
            }
        }
    }
}
