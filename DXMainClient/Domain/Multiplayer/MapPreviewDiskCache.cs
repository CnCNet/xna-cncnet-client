#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

/// <summary>Owns hash-based disk entries, metadata validation, staging and publication.
/// The injected extractor only produces staging files; it never decides cache policy.</summary>
internal sealed class MapPreviewDiskCache
{
    private readonly IExternalMapPreviewExtractor extractor;
    internal string DirectoryPath { get; }
    internal MapPreviewDiskCache(string directory, IExternalMapPreviewExtractor extractor)
    {
        DirectoryPath = directory;
        this.extractor = extractor;
    }
    internal static bool ValidHash(string? hash) => hash != null && hash.Length == 40
        && hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    private string ImagePath(Map map) => Path.Combine(DirectoryPath, map.SHA1.ToLowerInvariant() + ".png");
    internal MapPreviewSource? Read(Map map)
    {
        if (!ValidHash(map.SHA1)) return null;
        string path = ImagePath(map);
        var record = ReadRecord(path);
        return record == null ? null : new MapPreviewSource(map, path, true, record.Transform);
    }
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
    }

    // publishIfCurrent runs publication under the coordinator's registry lock so deletion
    // or cancellation cannot race between checking the map and committing the cache entry.
    internal async Task<bool> GenerateAsync(Map map, MapPreviewRenderOptions options, bool force,
        Action progress, Func<Action, bool> publishIfCurrent, CancellationToken cancellationToken)
    {
        if (!File.Exists(options.Executable))
            throw new IOException("Configured map renderer does not exist: " + options.Executable);
        string fingerprint = Fingerprint(options.Executable, options.Arguments, options.Width, options.Height, options.Version);
        string output = ImagePath(map);
        if (!force && ReadRecord(output)?.Fingerprint == fingerprint && ValidImage(output)) return false;
        Directory.CreateDirectory(DirectoryPath);
        string temporary = Path.Combine(DirectoryPath, map.SHA1.ToLowerInvariant() + "." + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            progress();
            await extractor.ExtractAsync(options, map.CompleteFilePath, temporary, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!ValidImage(temporary)) throw new IOException("Renderer did not produce a valid preview PNG.");
            double[]? transform = null;
            string sidecar = temporary + ".transform.json";
            if (File.Exists(sidecar))
            {
                transform = JsonSerializer.Deserialize<double[]>(File.ReadAllText(sidecar));
                if (transform == null || transform.Length < 9 || (transform.Length - 9) % 3 != 0
                    || transform.Any(v => double.IsNaN(v) || double.IsInfinity(v)) || transform[7] <= 0 || transform[8] <= 0)
                    throw new IOException("Invalid preview projection metadata.");
            }
            if (Utilities.CalculateSHA1ForFile(map.CompleteFilePath) != map.SHA1)
                throw new IOException("Map changed during rendering; select the updated map to retry.");
            return publishIfCurrent(() =>
            {
                if (File.Exists(output)) File.Replace(temporary, output, null); else File.Move(temporary, output);
                string metadata = Path.ChangeExtension(output, ".json"), staging = metadata + ".tmp";
                File.WriteAllText(staging, JsonSerializer.Serialize(new Record
                {
                    Fingerprint = fingerprint,
                    Transform = transform,
                    ImageLength = new FileInfo(output).Length,
                    ImageTicks = File.GetLastWriteTimeUtc(output).Ticks
                }));
                if (File.Exists(metadata)) File.Replace(staging, metadata, null); else File.Move(staging, metadata);
            });
        }
        finally
        {
            foreach (var suffix in new[] { "", ".transform.json", ".assets.log" })
                try { File.Delete(temporary + suffix); }
                catch (IOException e) { Logger.Log("Map preview staging cleanup: " + e.Message); }
                catch (UnauthorizedAccessException e) { Logger.Log("Map preview staging cleanup: " + e.Message); }
        }
    }

    public sealed class Record
    {
        public string Fingerprint { get; set; } = string.Empty;
        public long ImageLength { get; set; }
        public long ImageTicks { get; set; }
        // Optional renderer-provided projection: crop X/Y/W/H, scale, padding X/Y,
        // PNG width/height, followed by waypoint X/Y/height triplets.
        public double[]? Transform { get; set; }
    }

    private static Record? ReadRecord(string image)
    {
        try
        {
            var record = JsonSerializer.Deserialize<Record>(File.ReadAllText(Path.ChangeExtension(image, ".json")));
            var file = new FileInfo(image);
            if (record == null || !file.Exists || record.ImageLength != file.Length || record.ImageTicks != file.LastWriteTimeUtc.Ticks) return null;
            var t = record.Transform;
            if (t != null && (t.Length < 9 || (t.Length - 9) % 3 != 0 || t.Any(v => double.IsNaN(v) || double.IsInfinity(v)) || t[7] <= 0 || t[8] <= 0)) return null;
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
            var info = SixLabors.ImageSharp.Image.Identify(path);
            return info != null && info.Width > 0 && info.Height > 0 && info.Width <= 8192 && info.Height <= 8192
                && SixLabors.ImageSharp.Image.DetectFormat(path)?.Name == "PNG";
        }
        catch (Exception e) { Logger.Log("Invalid rendered preview: " + e.Message); return false; }
    }

    private static string Fingerprint(string executable, string arguments, int width, int height, string version)
    {
        var file = new FileInfo(executable);
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(executable + "\n" + file.Length + "\n" + file.LastWriteTimeUtc.Ticks
            + "\n" + arguments + "\n" + width + "," + height + "\n" + version);
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

}