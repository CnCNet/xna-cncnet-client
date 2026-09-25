#nullable enable
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

/// <summary>Runs a configured executable. It has no map registry, UI state or cache policy.</summary>
internal sealed class ExternalMapPreviewExtractor : IExternalMapPreviewExtractor
{
    public Task ExtractAsync(MapPreviewRenderOptions options, string mapPath, string outputPath,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        string arguments = options.Arguments.Replace("{game}", Quote(options.Root))
            .Replace("{map}", Quote(mapPath)).Replace("{output}", Quote(outputPath))
            .Replace("{width}", options.Width.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{height}", options.Height.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception e) { Logger.Log("Map renderer cancellation: " + e.Message); }
        });
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(options.TimeoutSeconds * 1000))
        {
            process.Kill();
            process.WaitForExit();
            throw new IOException("Map renderer timed out.");
        }
        process.WaitForExit(); // Drain redirected output on a worker, never the UI thread.
        cancellationToken.ThrowIfCancellationRequested();
        if (process.ExitCode != 0)
            throw new IOException("Map renderer exit code " + process.ExitCode + ": " + messages);
    }, cancellationToken);

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