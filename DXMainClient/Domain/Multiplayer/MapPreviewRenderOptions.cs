#nullable enable
using System.IO;

namespace DTAClient.Domain.Multiplayer;

/// <summary>Configuration captured once for a render, independent of subsequent settings changes.</summary>
internal sealed class MapPreviewRenderOptions
{
    public string Root { get; }
    public string Executable { get; }
    public string Arguments { get; }
    public string Version { get; }
    public int Width { get; }
    public int Height { get; }
    public int TimeoutSeconds { get; }

    public MapPreviewRenderOptions(string root, string executable, string arguments,
        string version, int width, int height, int timeoutSeconds)
    {
        Root = root;
        Executable = Path.GetFullPath(Path.Combine(root, executable));
        Arguments = arguments;
        Version = version;
        Width = width;
        Height = height;
        TimeoutSeconds = timeoutSeconds;
    }
}