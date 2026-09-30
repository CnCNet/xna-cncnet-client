#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

/// <summary>Runs a configured executable. It has no map registry, UI state or cache policy.</summary>
internal sealed class ExternalMapPreviewExtractor : IExternalMapPreviewExtractor
{
    public Task ExtractAsync(MapPreviewRenderOptions options, string mapPath, string outputPath,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
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
        process.WaitForExit(); // Drain redirected output on a worker, never the UI thread.
        cancellationToken.ThrowIfCancellationRequested();
        if (process.ExitCode != 0)
            throw new IOException("Map renderer exit code " + process.ExitCode + ": " + messages);
    }, cancellationToken);

    // The process may exit between the check and the kill, so failures here are expected.
    private static void Kill(Process process)
    {
        try
        {
            if (process.HasExited) return;
#if NETFRAMEWORK
            process.Kill(); // .NET Framework cannot kill the process tree; child processes of the renderer survive.
#else
            process.Kill(entireProcessTree: true);
#endif
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception e) { Logger.Log("Map renderer termination: " + e.Message); }
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
