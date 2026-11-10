#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// Defines a building type that is automatically detected in map files
    /// and rendered as an overlay icon on the map preview.
    /// Loaded from the [AutoMapOverlays] section of ClientDefinitions.ini.
    /// Format: BuildingID,ImageFile,OwnerFilter,CellOffsetX,CellOffsetY[,Toggleable]
    /// </summary>
    public sealed class AutoMapOverlayDefinition
    {
        /// <summary>Building type ID as it appears in [Structures], e.g. "CAOILD".</summary>
        public string BuildingId { get; }

        /// <summary>Texture file name to render on the preview, e.g. "oilderrick.png".</summary>
        public string TextureName { get; }

        /// <summary>
        /// If non-empty, only buildings whose owner matches this string (case-insensitive) are detected.
        /// Leave empty to detect regardless of owner.
        /// </summary>
        public string OwnerFilter { get; }

        /// <summary>Offset added to the building's cell X coordinate when placing the overlay icon.</summary>
        public int CellOffsetX { get; }

        /// <summary>Offset added to the building's cell Y coordinate when placing the overlay icon.</summary>
        public int CellOffsetY { get; }

        /// <summary>Whether the overlay icon can be toggled on/off by the user.</summary>
        public bool Toggleable { get; }

        public AutoMapOverlayDefinition(string buildingId, string textureName, string ownerFilter,
            int cellOffsetX, int cellOffsetY, bool toggleable)
        {
            BuildingId = buildingId;
            TextureName = textureName;
            OwnerFilter = ownerFilter;
            CellOffsetX = cellOffsetX;
            CellOffsetY = cellOffsetY;
            Toggleable = toggleable;
        }

        /// <summary>
        /// Parses the entries of the [AutoMapOverlays] section, skipping and logging invalid ones.
        /// </summary>
        public static IReadOnlyList<AutoMapOverlayDefinition> ParseSection(IniSection? section)
        {
            if (section == null)
                return Array.Empty<AutoMapOverlayDefinition>();

            var definitions = new List<AutoMapOverlayDefinition>();
            foreach (var kvp in section.Keys)
            {
                string[] parts = kvp.Value.Split(',');
                if (parts.Length < 5)
                {
                    Logger.Log($"AutoMapOverlays: Invalid entry '{kvp.Value}', expected at least 5 comma-separated fields.");
                    continue;
                }

                if (!int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int offsetX) ||
                    !int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int offsetY))
                {
                    Logger.Log($"AutoMapOverlays: Invalid cell offsets in entry '{kvp.Value}'.");
                    continue;
                }

                bool toggleable = parts.Length > 5 && Conversions.BooleanFromString(parts[5].Trim(), false);
                definitions.Add(new AutoMapOverlayDefinition(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(),
                    offsetX, offsetY, toggleable));
            }

            return definitions;
        }
    }
}
