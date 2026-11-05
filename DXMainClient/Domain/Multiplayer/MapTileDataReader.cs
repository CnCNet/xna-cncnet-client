#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using Microsoft.Xna.Framework;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// Reads what a map preview needs from a map file: the terrain height at each start location,
    /// and the buildings to mark with an icon. Only the Tiberian Sun / Red Alert 2 map format is
    /// supported (heights from IsoMapPack5, buildings from [Structures]); another format needs its
    /// own read method.
    /// </summary>
    internal static class MapTileDataReader
    {
        private const string IsoMapPackSection = "[IsoMapPack5]";
        private const string StructuresSection = "[Structures]";

        /// <summary>
        /// Reads the map file and finds its start location heights and the buildings that match
        /// <paramref name="overlayDefinitions"/>.
        /// </summary>
        /// <returns>The data for the map, or null if the file could not be read.</returns>
        public static MapTileData? Read(Map map, IReadOnlyList<AutoMapOverlayDefinition> overlayDefinitions)
        {
            try
            {
                var (isoMapPack, structures) = ReadSections(map.CompleteFilePath);
                Dictionary<long, byte> tileLevels = IsoMapPackDecoder.Decode(isoMapPack);

                int[]? waypointLevels = GetWaypointLevels(map.waypoints, tileLevels);
                List<ExtraMapPreviewTexture> buildingOverlays = DetectBuildingOverlays(structures, tileLevels, overlayDefinitions);

                var fileInfo = new FileInfo(map.CompleteFilePath);
                return new MapTileData(fileInfo.Length, fileInfo.LastWriteTimeUtc, waypointLevels, buildingOverlays);
            }
            catch (Exception ex)
            {
                Logger.Log($"MapTileDataReader: Failed to read tile data for {map.BaseFilePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads a map file in one pass and returns the concatenated base64 IsoMapPack5 data and the
        /// values of the [Structures] entries (everything after the '=' on each line).
        /// </summary>
        private static (string isoMapPack, List<string> structures) ReadSections(string filePath)
        {
            var isoMapPack = new StringBuilder(32 * 1024);
            var structures = new List<string>(128);

            bool inIsoMapPack = false, inStructures = false;

            foreach (string line in File.ReadLines(filePath))
            {
                if (line.Length == 0 || line[0] == ';')
                    continue;

                if (line[0] == '[')
                {
                    string header = line.Trim();
                    inIsoMapPack = header.Equals(IsoMapPackSection, StringComparison.OrdinalIgnoreCase);
                    inStructures = header.Equals(StructuresSection, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inIsoMapPack && !inStructures)
                    continue;

                int eq = line.IndexOf('=');
                if (eq < 0)
                    continue;

                if (inIsoMapPack)
                    isoMapPack.Append(line, eq + 1, line.Length - eq - 1);
                else
                    structures.Add(line.Substring(eq + 1));
            }

            return (isoMapPack.ToString(), structures);
        }

        /// <summary>
        /// Looks up the terrain height of each waypoint's cell, indexed like <paramref name="waypoints"/>.
        /// A cell missing from IsoMapPack5 gets -1, which keeps the level given in the waypoint string.
        /// </summary>
        private static int[]? GetWaypointLevels(IReadOnlyList<string> waypoints, Dictionary<long, byte> tileLevels)
        {
            if (waypoints.Count == 0)
                return null;

            var levels = new int[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++)
            {
                var (x, y) = Map.ParseWaypointCellCoords(waypoints[i].Split(',')[0]);
                levels[i] = tileLevels.TryGetValue(IsoMapPackDecoder.TileKey(x, y), out byte level) ? level : -1;
            }

            return levels;
        }

        /// <summary>
        /// Finds the [Structures] entries that match an overlay definition and returns an icon for each,
        /// raised to the height of the building's cell.
        /// </summary>
        private static List<ExtraMapPreviewTexture> DetectBuildingOverlays(List<string> structures,
            Dictionary<long, byte> tileLevels, IReadOnlyList<AutoMapOverlayDefinition> definitions)
        {
            var result = new List<ExtraMapPreviewTexture>();
            if (structures.Count == 0 || definitions.Count == 0)
                return result;

            // A BuildingId ending with '*' matches any ID starting with that prefix.
            var exactDefinitions = new Dictionary<string, AutoMapOverlayDefinition>(StringComparer.OrdinalIgnoreCase);
            var wildcardDefinitions = new List<(string Prefix, AutoMapOverlayDefinition Definition)>();
            foreach (var definition in definitions)
            {
                if (definition.BuildingId.EndsWith("*", StringComparison.Ordinal))
                    wildcardDefinitions.Add((definition.BuildingId.TrimEnd('*'), definition));
                else if (!exactDefinitions.ContainsKey(definition.BuildingId))
                    exactDefinitions[definition.BuildingId] = definition;
            }

            foreach (string value in structures)
            {
                // Owner,BuildingTypeID,Health,X,Y,...
                string[] parts = value.Split(',');
                if (parts.Length < 5)
                    continue;

                string owner = parts[0].Trim();
                string buildingId = parts[1].Trim();

                if (!exactDefinitions.TryGetValue(buildingId, out AutoMapOverlayDefinition? definition))
                {
                    foreach (var (prefix, wildcardDefinition) in wildcardDefinitions)
                    {
                        if (buildingId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            definition = wildcardDefinition;
                            break;
                        }
                    }
                }

                if (definition == null)
                    continue;

                if (!string.IsNullOrEmpty(definition.OwnerFilter) &&
                    !string.Equals(owner, definition.OwnerFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ||
                    !int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                    continue;

                int cellX = x + definition.CellOffsetX;
                int cellY = y + definition.CellOffsetY;
                byte level = tileLevels.TryGetValue(IsoMapPackDecoder.TileKey(cellX, cellY), out byte cellLevel) ? cellLevel : (byte)0;

                result.Add(new ExtraMapPreviewTexture(definition.TextureName, new Point(cellX, cellY), level, definition.Toggleable));
            }

            return result;
        }
    }
}
