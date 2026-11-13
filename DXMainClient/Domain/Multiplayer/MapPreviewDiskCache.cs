#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;

using Rampastring.Tools;

using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer;

/// <summary>Owns hash-based disk entries, metadata validation, staging and publication.
/// The extractor only produces an image; it never decides cache policy.</summary>
internal sealed class MapPreviewDiskCache
{
    internal string DirectoryPath { get; }

    // Renders and publication stage their files under here, so leftovers can be removed together.
    internal string StagingPath => Path.Combine(DirectoryPath, "staging");

    internal MapPreviewDiskCache(string directory) => DirectoryPath = directory;

    internal static bool ValidHash(string? hash) => hash != null && hash.Length == 40
        && hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));

    private string ImagePath(Map map) => Path.Combine(DirectoryPath, map.SHA1.ToLowerInvariant() + ".png");

    /// <returns>The path of the map's cached generated preview, or null if there is no valid entry.</returns>
    internal string? Read(Map map)
    {
        if (!ValidHash(map.SHA1)) return null;
        string path = ImagePath(map);
        return ReadRecord(path) == null ? null : path;
    }

    /// <summary>Deletes entries for maps that are no longer loaded, and staging files left behind
    /// by renders that never finished (e.g. the client was killed). Must not run during a render.</summary>
    internal void Prune(IEnumerable<string> liveHashes)
    {
        if (!Directory.Exists(DirectoryPath)) return;
        var hashes = new HashSet<string>(liveHashes, StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(DirectoryPath))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (ValidHash(name) && !hashes.Contains(name)
                && (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
                File.Delete(file);
        }
        if (Directory.Exists(StagingPath))
            Directory.Delete(StagingPath, true);
    }

    /// <summary>Renders the map with <paramref name="extractor"/> and publishes the result, unless a
    /// current entry exists and <paramref name="force"/> is not set. Blocks until the render ends.</summary>
    /// <param name="publishIfCurrent">Runs publication under the coordinator's registry lock so deletion
    /// or cancellation cannot race between checking the map and committing the cache entry.</param>
    /// <returns>Whether a new entry was published.</returns>
    internal bool Generate(Map map, MapPreviewRenderOptions options, IMapPreviewExtractor extractor, bool force,
        Action progress, Func<Action, bool> publishIfCurrent, CancellationToken cancellationToken)
    {
        if (!File.Exists(options.Executable))
            throw new IOException("Configured map renderer does not exist: " + options.Executable);
        string fingerprint = Fingerprint(options);
        string output = ImagePath(map);
        if (!force && ReadRecord(output)?.Fingerprint == fingerprint && ValidImage(output)) return false;

        progress();
        using Image image = extractor.ExtractMapPreview(map.CompleteFilePath)
            ?? throw new IOException("Map renderer did not produce a preview image.");
        cancellationToken.ThrowIfCancellationRequested();
        if (Utilities.CalculateSHA1ForFile(map.CompleteFilePath) != map.SHA1)
            throw new IOException("Map changed during rendering; select the updated map to retry.");

        Directory.CreateDirectory(StagingPath);
        string stagedImage = Path.Combine(StagingPath, Guid.NewGuid().ToString("N") + ".png");
        string stagedMetadata = Path.ChangeExtension(stagedImage, ".json");
        try
        {
            // Re-encoding means the cache always holds PNGs, whatever format the renderer wrote.
            image.SaveAsPng(stagedImage);
            return publishIfCurrent(() =>
            {
                if (File.Exists(output)) File.Replace(stagedImage, output, null); else File.Move(stagedImage, output);
                string metadata = Path.ChangeExtension(output, ".json");
                File.WriteAllText(stagedMetadata, JsonSerializer.Serialize(new Record
                {
                    Fingerprint = fingerprint,
                    ImageLength = new FileInfo(output).Length,
                    ImageTicks = File.GetLastWriteTimeUtc(output).Ticks
                }));
                if (File.Exists(metadata)) File.Replace(stagedMetadata, metadata, null); else File.Move(stagedMetadata, metadata);
            });
        }
        finally
        {
            try
            {
                File.Delete(stagedImage);
                File.Delete(stagedMetadata);
            }
            catch (IOException e) { Logger.Log("Map preview staging cleanup: " + e.Message); }
            catch (UnauthorizedAccessException e) { Logger.Log("Map preview staging cleanup: " + e.Message); }
        }
    }

    public sealed class Record
    {
        public string Fingerprint { get; set; } = string.Empty;
        public long ImageLength { get; set; }
        public long ImageTicks { get; set; }
    }

    private static Record? ReadRecord(string image)
    {
        try
        {
            var record = JsonSerializer.Deserialize<Record>(File.ReadAllText(Path.ChangeExtension(image, ".json")));
            var file = new FileInfo(image);
            if (record == null || !file.Exists || record.ImageLength != file.Length || record.ImageTicks != file.LastWriteTimeUtc.Ticks) return null;
            return record;
        }
        catch (IOException) { return null; }
        catch (JsonException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    private static bool ValidImage(string path)
    {
        try
        {
            var info = Image.Identify(path);
            return info != null && info.Width > 0 && info.Height > 0
                && Image.DetectFormat(path)?.Name == "PNG";
        }
        catch (Exception e) { Logger.Log("Invalid cached map preview: " + e.Message); return false; }
    }

    private static string Fingerprint(MapPreviewRenderOptions options)
    {
        var file = new FileInfo(options.Executable);
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(options.Executable + "\n" + file.Length + "\n" + file.LastWriteTimeUtc.Ticks
            + "\n" + options.Arguments + "\n" + options.Width + "," + options.Height + "\n" + options.Version);
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }
}
