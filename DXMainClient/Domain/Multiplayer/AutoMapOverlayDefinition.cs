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
    }
}
