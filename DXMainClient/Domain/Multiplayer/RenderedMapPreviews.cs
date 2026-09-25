using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientGUI;
using Rampastring.Tools;
using Microsoft.Xna.Framework;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>Optional external renderer; all graphics-device work stays on the UI thread.</summary>
    internal static class RenderedMapPreviews
    {
        private static readonly SemaphoreSlim Queue = new(1, 1);
        private static readonly object Sync = new();
        private static readonly HashSet<string> Pending = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Map> Maps = new(StringComparer.OrdinalIgnoreCase);
        private static bool inGame;
        private static Process rendererProcess;
        private static string Root => ProgramConstants.GamePath;
        internal static string CacheDirectory => Path.Combine(Root, "Client", "MapPreviewCache");
        public static event Action<Map, bool, string> Completed;
        public static event Action<Map, string> Progress;
        public static bool Configured => !string.IsNullOrWhiteSpace(ClientConfiguration.Instance.MapRendererPath)
            && !string.IsNullOrWhiteSpace(ClientConfiguration.Instance.MapRendererArguments);
        public static bool Enabled => Configured && UserINISettings.Instance.RenderMapPreviews.Value;
        public static bool Selected => Enabled && UserINISettings.Instance.ShowGeneratedMapPreviews.Value;

        public sealed class Record
        {
            public string Fingerprint { get; set; }
            public long ImageLength { get; set; }
            public long ImageTicks { get; set; }
            // Optional renderer-provided projection: crop X/Y/W/H, scale, padding X/Y,
            // PNG width/height, followed by waypoint X/Y/height triplets.
            public double[] Transform { get; set; }
        }

        static RenderedMapPreviews()
        {
            GameProcessLogic.GameProcessStarting += () => { lock (Sync) { inGame = true; CancelRendering(); } };
            GameProcessLogic.GameProcessExited += () => { lock (Sync) inGame = false; };
            UserINISettings.Instance.SettingsSaved += (sender, args) => { if (!Selected) lock (Sync) CancelRendering(); };
        }

        private static void CancelRendering()
        {
            try { if (rendererProcess != null && !rendererProcess.HasExited) rendererProcess.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception e) { Logger.Log("Map renderer cancellation: " + e.Message); }
        }

        private static bool ValidHash(string hash) => hash != null && hash.Length == 40
            && hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        private static string ImagePath(Map map) => ValidHash(map?.SHA1)
            ? Path.Combine(CacheDirectory, map.SHA1.ToLowerInvariant() + ".png") : null;
        private static bool StillPresent(Map map) => Maps.Values.Any(m => m.SHA1 == map.SHA1 && File.Exists(m.CompleteFilePath));

        public static void Register(IEnumerable<Map> maps)
        {
            lock (Sync) {
                Maps.Clear();
                foreach (var map in maps) Maps[map.CompleteFilePath] = map;
            }
            _ = Task.Run(PruneAsync);
        }

        internal static async Task PruneAsync()
        {
            await Queue.WaitAsync().ConfigureAwait(false);
            try {
                lock (Sync) {
                    if (!Directory.Exists(CacheDirectory)) return;
                    var hashes = new HashSet<string>(Maps.Values.Where(m => File.Exists(m.CompleteFilePath))
                        .Select(m => m.SHA1), StringComparer.OrdinalIgnoreCase);
                    foreach (var file in Directory.EnumerateFiles(CacheDirectory)) {
                        var name = Path.GetFileNameWithoutExtension(file);
                        if (ValidHash(name) && !hashes.Contains(name)
                            && (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
                            File.Delete(file);
                    }
                }
            } catch (Exception e) { Logger.Log("Map preview cache pruning failed: " + e.Message); }
            finally { Queue.Release(); }
        }

        public static void Remove(Map map)
        {
            if (map == null) return;
            lock (Sync) Maps.Remove(map.CompleteFilePath);
            _ = Task.Run(PruneAsync);
        }

        private static Record ReadRecord(string image)
        {
            try {
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

        public static string CachedImage(Map map)
        {
            if (!Selected) return null;
            var path = ImagePath(map);
            return path != null && File.Exists(path) && ReadRecord(path) != null ? path : null;
        }

        private static bool ValidImage(string path)
        {
            try {
                var info = SixLabors.ImageSharp.Image.Identify(path);
                return info != null && info.Width > 0 && info.Height > 0 && info.Width <= 8192 && info.Height <= 8192
                    && SixLabors.ImageSharp.Image.DetectFormat(path)?.Name == "PNG";
            } catch (Exception e) { Logger.Log("Invalid rendered preview: " + e.Message); return false; }
        }

        private static string Fingerprint(string executable, string arguments, int width, int height)
        {
            var file = new FileInfo(executable);
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(executable + "\n" + file.Length + "\n" + file.LastWriteTimeUtc.Ticks
                + "\n" + arguments + "\n" + width + "," + height + "\n" + ClientConfiguration.Instance.MapRendererVersion);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }

        // Quote one argument, including trailing backslashes, without invoking a shell.
        internal static string Quote(string value)
        {
            var result = new StringBuilder("\""); int slashes = 0;
            foreach (char c in value) {
                if (c == '\\') { ++slashes; continue; }
                if (c == '"') { result.Append('\\', slashes * 2 + 1); result.Append(c); }
                else { result.Append('\\', slashes); result.Append(c); }
                slashes = 0;
            }
            result.Append('\\', slashes * 2); return result.Append('"').ToString();
        }

        public static Task<bool> Request(Map map, bool force = false)
        {
            if (map == null || !Selected || !ValidHash(map.SHA1)) return Task.FromResult(false);
            lock (Sync) {
                if (inGame || !Pending.Add(map.SHA1)) return Task.FromResult(false);
                Maps[map.CompleteFilePath] = map;
            }
            return Task.Run(async () => {
                await Queue.WaitAsync().ConfigureAwait(false);
                string temporary = null, error = null; bool changed = false;
                try {
                    lock (Sync) if (inGame || !StillPresent(map) || !Selected) return false;
                    var config = ClientConfiguration.Instance;
                    var executable = Path.GetFullPath(Path.Combine(Root, config.MapRendererPath));
                    if (!File.Exists(executable)) throw new IOException("Configured map renderer does not exist: " + executable);
                    int width = config.MapRendererWidth, height = config.MapRendererHeight;
                    var fingerprint = Fingerprint(executable, config.MapRendererArguments, width, height);
                    string output = ImagePath(map);
                    if (!force && File.Exists(output) && ReadRecord(output)?.Fingerprint == fingerprint && ValidImage(output)) return false;
                    Directory.CreateDirectory(CacheDirectory);
                    temporary = Path.Combine(CacheDirectory, map.SHA1.ToLowerInvariant() + "." + Guid.NewGuid().ToString("N") + ".png");
                    string arguments = config.MapRendererArguments.Replace("{game}", Quote(Root))
                        .Replace("{map}", Quote(map.CompleteFilePath)).Replace("{output}", Quote(temporary))
                        .Replace("{width}", width.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        .Replace("{height}", height.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    Progress?.Invoke(map, "Generating hi-res map preview…");
                    var messages = new StringBuilder();
                    using (var process = new Process { StartInfo = new ProcessStartInfo {
                        FileName = executable, Arguments = arguments, WorkingDirectory = Root,
                        UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true, RedirectStandardError = true
                    } }) {
                        DataReceivedEventHandler capture = (sender, args) => {
                            if (args.Data == null) return;
                            lock (messages) if (messages.Length < 16384) messages.AppendLine(args.Data);
                        };
                        process.OutputDataReceived += capture; process.ErrorDataReceived += capture;
                        lock (Sync) {
                            if (inGame || !StillPresent(map) || !Selected) return false;
                            process.Start(); rendererProcess = process;
                        }
                        try {
                            process.BeginOutputReadLine(); process.BeginErrorReadLine();
                            if (!process.WaitForExit(config.MapRendererTimeoutSeconds * 1000)) {
                                process.Kill(); process.WaitForExit(); throw new IOException("Map renderer timed out.");
                            }
                            process.WaitForExit(); // Drain redirected output on this background task.
                            lock (Sync) if (inGame || !Selected || !StillPresent(map)) return false;
                            if (process.ExitCode != 0) throw new IOException("Map renderer exit code " + process.ExitCode + ": " + messages);
                        } finally { lock (Sync) if (rendererProcess == process) rendererProcess = null; }
                    }
                    if (!ValidImage(temporary)) throw new IOException("Renderer did not produce a valid preview PNG.");
                    double[] transform = null;
                    var sidecar = temporary + ".transform.json";
                    if (File.Exists(sidecar)) {
                        transform = JsonSerializer.Deserialize<double[]>(File.ReadAllText(sidecar));
                        if (transform == null || transform.Length < 9 || (transform.Length - 9) % 3 != 0
                            || transform.Any(v => double.IsNaN(v) || double.IsInfinity(v)) || transform[7] <= 0 || transform[8] <= 0)
                            throw new IOException("Invalid preview projection metadata.");
                    }
                    if (Utilities.CalculateSHA1ForFile(map.CompleteFilePath) != map.SHA1)
                        throw new IOException("Map changed during rendering; select the updated map to retry.");
                    lock (Sync) {
                        if (inGame || !StillPresent(map) || !Selected) return false;
                        // Only managed cache files are replaced. Original previews are never modified.
                        if (File.Exists(output)) File.Replace(temporary, output, null); else File.Move(temporary, output);
                        string metadata = Path.ChangeExtension(output, ".json"), staging = metadata + ".tmp";
                        File.WriteAllText(staging, JsonSerializer.Serialize(new Record { Fingerprint = fingerprint, Transform = transform, ImageLength = new FileInfo(output).Length, ImageTicks = File.GetLastWriteTimeUtc(output).Ticks }));
                        if (File.Exists(metadata)) File.Replace(staging, metadata, null); else File.Move(staging, metadata);
                        changed = true;
                    }
                    return true;
                } catch (Exception e) { error = e.Message; Logger.Log("Map preview generation failed for " + map.CompleteFilePath + ": " + e); return false; }
                finally {
                    if (temporary != null) foreach (var suffix in new[] { "", ".transform.json", ".assets.log" })
                        try { File.Delete(temporary + suffix); } catch (IOException) { }
                    lock (Sync) Pending.Remove(map.SHA1);
                    Queue.Release();
                    Progress?.Invoke(map, string.Empty);
                    Completed?.Invoke(map, changed, error);
                }
            });
        }

        public static bool TryCoordinate(Map map, int x, int y, int level, Point size, out Point result)
        {
            result = default;
            if (!map.GeneratedPreviewActive) return false;
            var path = CachedImage(map);
            var t = path == null ? null : ReadRecord(path)?.Transform;
            if (t == null) return false;
            if (level == 0) for (int i = 9; i + 2 < t.Length; i += 3)
                if ((int)t[i] == x && (int)t[i + 1] == y) { level = (int)t[i + 2]; break; }
            double px = (x - y + map.RenderedMapWidth - 1) * 30.0;
            double py = (x + y - map.RenderedMapWidth - 1 - level) * 15.0;
            result = new Point((int)Math.Round(((px - t[0]) * t[4] + t[5]) * size.X / t[7]),
                (int)Math.Round(((py - t[1]) * t[4] + t[6]) * size.Y / t[8]));
            return true;
        }
    }
}
