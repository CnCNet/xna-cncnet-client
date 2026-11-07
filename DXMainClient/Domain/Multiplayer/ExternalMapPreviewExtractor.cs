#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Rampastring.Tools;

using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer;

/// <summary>
/// Renders a map preview by running the mod's configured renderer executable.
/// Blocks until the renderer exits, so it must not be called on the UI thread.
/// </summary>
internal sealed class ExternalMapPreviewExtractor : IMapPreviewExtractor
{
    private const int MaxImageDimension = 8192;

    private readonly MapPreviewRenderOptions options;
    private readonly string stagingDirectory;
    private readonly CancellationToken cancellationToken;

    /// <param name="stagingDirectory">Folder for per-render work folders, deleted after each render.</param>
    public ExternalMapPreviewExtractor(MapPreviewRenderOptions options, string stagingDirectory,
        CancellationToken cancellationToken)
    {
        this.options = options;
        this.stagingDirectory = stagingDirectory;
        this.cancellationToken = cancellationToken;
    }

    /// <returns>The rendered image, or null if the renderer exited without writing one.</returns>
    /// <exception cref="OperationCanceledException">The render was cancelled.</exception>
    /// <exception cref="IOException">The renderer failed, timed out or wrote an unusable image.</exception>
    public Image? ExtractMapPreview(string mapFilePath)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Each render works in its own folder, so everything the renderer writes is removed together.
        string workDirectory = Path.Combine(stagingDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDirectory);
        try
        {
            string outputPath = Path.Combine(workDirectory, "preview.png");
            RunRenderer(mapFilePath, outputPath);

            if (!File.Exists(outputPath))
                return null;

            IImageInfo info = Image.Identify(outputPath)
                ?? throw new IOException("Map renderer output is not a supported image.");
            if (info.Width <= 0 || info.Height <= 0 || info.Width > MaxImageDimension || info.Height > MaxImageDimension)
                throw new IOException($"Map renderer output has unsupported dimensions {info.Width}x{info.Height}.");

            return Image.Load(outputPath);
        }
        finally
        {
            try { Directory.Delete(workDirectory, true); }
            catch (IOException e) { Logger.Log("Map preview staging cleanup: " + e.Message); }
            catch (UnauthorizedAccessException e) { Logger.Log("Map preview staging cleanup: " + e.Message); }
        }
    }

    private void RunRenderer(string mapPath, string outputPath)
    {
        // Substitute in one pass so a path containing e.g. "{output}" is not expanded again.
        string arguments = Regex.Replace(options.Arguments, @"\{(game|map|output|width|height)\}", match => match.Groups[1].Value switch
        {
            "game" => Quote(options.Root),
            "map" => Quote(mapPath),
            "output" => Quote(outputPath),
            "width" => options.Width.ToString(CultureInfo.InvariantCulture),
            _ => options.Height.ToString(CultureInfo.InvariantCulture)
        });
        var messages = new StringBuilder();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = options.Executable,
                Arguments = arguments,
                WorkingDirectory = options.Root,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        DataReceivedEventHandler capture = (sender, args) =>
        {
            if (args.Data != null)
                lock (messages) if (messages.Length < 16384) messages.AppendLine(args.Data);
        };
        process.OutputDataReceived += capture;
        process.ErrorDataReceived += capture;
        process.Start();
        using var registration = cancellationToken.Register(() => Kill(process));
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(options.TimeoutSeconds * 1000))
        {
            Kill(process);
            process.WaitForExit();
            throw new IOException("Map renderer timed out.");
        }
        process.WaitForExit(); // Drains the redirected output.
        cancellationToken.ThrowIfCancellationRequested();
        if (process.ExitCode != 0)
            throw new IOException("Map renderer exit code " + process.ExitCode + ": " + messages);
    }

    // The process may exit between the check and the kill, so failures here are expected.
    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true); // Polyfill provides this on .NET Framework.
        }
        catch (InvalidOperationException) { }
        catch (Exception e) { Logger.Log("Map renderer termination: " + e.Message); }
    }

    // Quote one argument, including trailing backslashes, without invoking a shell.
    internal static string Quote(string value)
    {
        var result = new StringBuilder("\""); int slashes = 0;
        foreach (char c in value)
        {
            if (c == '\\') { ++slashes; continue; }
            if (c == '"') { result.Append('\\', slashes * 2 + 1); result.Append(c); }
            else { result.Append('\\', slashes); result.Append(c); }
            slashes = 0;
        }
        result.Append('\\', slashes * 2); return result.Append('"').ToString();
    }
}
