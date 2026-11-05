#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// What a map preview needs from a map file beyond its INI data: the terrain height at each
    /// start location, and the building icons found in the map.
    /// </summary>
    public sealed class MapTileData
    {
        /// <summary>Size of the map file this data was read from.</summary>
        [JsonInclude]
        public long FileSize { get; init; }

        /// <summary>Last write time of the map file this data was read from.</summary>
        [JsonInclude]
        public DateTime LastWriteTimeUtc { get; init; }

        /// <summary>
        /// Terrain height of each waypoint's cell, indexed like the map's waypoints.
        /// -1 keeps the level given in the waypoint string. Null if the map has no waypoints.
        /// </summary>
        [JsonInclude]
        public int[]? WaypointLevels { get; init; }

        /// <summary>
        /// Icons for the detected buildings, in cell coordinates with the height of the building's cell.
        /// </summary>
        [JsonInclude]
        public List<ExtraMapPreviewTexture>? BuildingOverlays { get; init; }

        [JsonConstructor]
        public MapTileData() { }

        public MapTileData(long fileSize, DateTime lastWriteTimeUtc,
            int[]? waypointLevels, List<ExtraMapPreviewTexture> buildingOverlays)
        {
            FileSize = fileSize;
            LastWriteTimeUtc = lastWriteTimeUtc;
            WaypointLevels = waypointLevels;
            BuildingOverlays = buildingOverlays;
        }

        /// <summary>Whether the map file has changed or gone since this data was read.</summary>
        public bool IsOutdated(string completeFilePath)
        {
            var fileInfo = new FileInfo(completeFilePath);
            return !fileInfo.Exists || fileInfo.Length != FileSize || fileInfo.LastWriteTimeUtc != LastWriteTimeUtc;
        }
    }
}
