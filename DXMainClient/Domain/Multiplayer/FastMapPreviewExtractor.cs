#nullable enable
using System;
using System.IO;
using System.Text;

using ClientCore;

using Rampastring.Tools;

using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer;

public class FastMapPreviewExtractor : MapPreviewExtractor, IMapPreviewExtractor
{
    public static new readonly FastMapPreviewExtractor Instance = new FastMapPreviewExtractor();

    /// <summary>
    /// Extracts map preview image as a bitmap.
    /// </summary>
    /// <param name="mapFilePath">Path to the map file.</param>
    /// <remarks>This method is optimized for speed. It should have exact behavior as MapPreviewExtractor.ExtractMapPreview(). Any changes to this method must be carefully ported to MapPreviewExtractor.ExtractMapPreview() to ensure the two methods remain in sync, and vice versa.</remarks>
    /// <returns>Bitmap of map preview image, or null if preview could not be extracted.</returns>
    public override Image? ExtractMapPreview(string mapFilePath)
    {
        string baseFilename = mapFilePath.Replace(ProgramConstants.GamePath, "");

        const string hiddenPreviewSentinel = "yAsAIAXQ5PDQ5PDQ6JQATAEE6PDQ4PDI4JgBTAFEAkgAJyAATAG0AydEAEABpAJIA0wBVA";

        string? previewSize = null;
        var sb = new StringBuilder();
        bool inPreview = false;
        bool inPreviewPack = false;
        bool hasPreviewPack = false;

        // Note: we only care about ASCII characters here. Therefore, we use the default UTF-8 encoding reading the file, ignoring the MapCodeHelper.GetMapEncoding() call.
        // Microsoft says using UTF8Encoding is faster than using ASCIIEncoding.
        // Ref: https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding
        foreach (string rawLine in File.ReadLines(mapFilePath))
        {
            string line = rawLine;
            int semicolonIndex = line.IndexOf(';');
            if (semicolonIndex >= 0)
                line = line.Substring(0, semicolonIndex);

            line = line.Trim();
            if (line.Length == 0)
                continue;

            if (line[0] == '[')
            {
                int closeBracket = line.IndexOf(']');
                if (closeBracket < 0)
                    continue;
                string sectionName = line.Substring(1, closeBracket - 1);
                inPreview = sectionName == "Preview";
                inPreviewPack = sectionName == "PreviewPack";
                continue;
            }

            int equalsIndex = line.IndexOf('=');
            if (equalsIndex < 0)
                continue;

            if (inPreview)
            {
                if (line.Substring(0, equalsIndex).Trim() == "Size")
                    previewSize = line.Substring(equalsIndex + 1).Trim();
            }
            else if (inPreviewPack)
            {
                hasPreviewPack = true;
                string value = line.Substring(equalsIndex + 1).Trim();
                if (line.Substring(0, equalsIndex).Trim() == "1" && value == hiddenPreviewSentinel)
                {
                    Logger.Log("MapPreviewExtractor: " + baseFilename + " - Hidden preview detected, not extracting preview.");
                    return null;
                }
                sb.Append(value);
            }
        }

        if (!hasPreviewPack)
        {
            Logger.Log("MapPreviewExtractor: " + baseFilename + " - no [PreviewPack] exists, unable to extract preview.");
            return null;
        }

        string[] previewSizes = previewSize != null ? previewSize.Split(',') : Array.Empty<string>();
        int previewWidth = previewSizes.Length > 3 ? Conversions.IntFromString(previewSizes[2].Trim(), -1) : -1;
        int previewHeight = previewSizes.Length > 3 ? Conversions.IntFromString(previewSizes[3].Trim(), -1) : -1;

        if (previewWidth < 1 || previewHeight < 1)
        {
            Logger.Log("MapPreviewExtractor: " + baseFilename + " - [Preview] Size value is invalid, unable to extract preview.");
            return null;
        }

        byte[] dataSource;

        try
        {
            dataSource = Convert.FromBase64String(sb.ToString());
        }
        catch (Exception)
        {
            Logger.Log("MapPreviewExtractor: " + baseFilename + " - [PreviewPack] is malformed, unable to extract preview.");
            return null;
        }

        byte[] dataDest = DecompressPreviewData(dataSource, previewWidth * previewHeight * 3, out string errorMessage);

        if (errorMessage != null)
        {
            Logger.Log("MapPreviewExtractor: " + baseFilename + " - " + errorMessage);
            return null;
        }

        Image bitmap = CreatePreviewBitmapFromImageData(previewWidth, previewHeight, dataDest, out errorMessage);

        if (errorMessage != null)
        {
            Logger.Log("MapPreviewExtractor: " + baseFilename + " - " + errorMessage);
            return null;
        }

        return bitmap;
    }
}
