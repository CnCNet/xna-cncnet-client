#nullable enable
using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer;

public interface IMapPreviewExtractor
{
    /// <summary>
    /// Extracts map preview image as a bitmap.
    /// </summary>
    /// <param name="mapFilePath">Path to the map file.</param>
    /// <returns>Bitmap of map preview image, or null if preview could not be extracted.</returns>
    Image? ExtractMapPreview(string mapFilePath);
}