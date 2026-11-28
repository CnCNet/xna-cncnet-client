#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace ClientCore.Display;

/// <summary>
/// A screen resolution with width and height in pixels.
/// </summary>
public sealed record ScreenResolution : IComparable<ScreenResolution>
{

    /// <summary>
    /// The width of the resolution in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// The height of the resolution in pixels.
    /// </summary>
    public int Height { get; }

    public ScreenResolution(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public ScreenResolution(string resolution)
    {
        if (!TryParseWidthHeight(resolution, out int width, out int height))
            throw new FormatException($"Invalid screen resolution \"{resolution}\".");

        Width = width;
        Height = height;
    }

    /// <summary>
    /// Tries to parse a resolution string, such as <c>1280x720</c>.
    /// </summary>
    /// <param name="value">The resolution string to parse.</param>
    /// <param name="resolution">The parsed resolution when successful.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> was parsed successfully.</returns>
    public static bool TryParse(string? value, [NotNullWhen(true)] out ScreenResolution? resolution)
    {
        resolution = null;

        if (!TryParseWidthHeight(value, out int width, out int height))
            return false;

        resolution = new ScreenResolution(width, height);

        return true;
    }

    private static bool TryParseWidthHeight(string? value, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (value == null || string.IsNullOrWhiteSpace(value))
            return false;

        string[] parts = value.Trim().Split('x');

        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out width)
            && int.TryParse(parts[1], out height);
    }

    public static implicit operator ScreenResolution(string resolution) => new(resolution);

    public sealed override string ToString() => Width + "x" + Height;

    public static implicit operator string(ScreenResolution resolution) => resolution.ToString();

    public void Deconstruct(out int width, out int height)
    {
        width = this.Width;
        height = this.Height;
    }

    public static implicit operator ScreenResolution((int Width, int Height) resolutionTuple) => new(resolutionTuple.Width, resolutionTuple.Height);

    public static implicit operator (int Width, int Height)(ScreenResolution resolution) => new(resolution.Width, resolution.Height);

    public bool Fits(ScreenResolution child) => this.Width >= child.Width && this.Height >= child.Height;

    public int CompareTo(ScreenResolution? other)
    {
        if (other is null)
            return 1;
        return (this.Width, this.Height).CompareTo((other.Width, other.Height));
    }
}