#nullable enable
using System;

using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer.CnCNet;

public readonly record struct PingValue
{
    private readonly int _value;

    private PingValue(int value) => _value = value;

    /// <summary>
    /// Represents an unknown or invalid ping measurement.
    /// </summary>
    public static PingValue Unknown => new(-1);

    /// <summary>
    /// Creates a PingValue from a millisecond measurement.
    /// </summary>
    /// <param name="milliseconds">The ping in milliseconds. Must be non-negative.</param>
    /// <returns>A valid PingValue.</returns>
    /// <exception cref="ArgumentException">Thrown when milliseconds is negative.</exception>
    public static PingValue FromMs(int milliseconds)
    {
        return milliseconds < 0
            ? throw new ArgumentException("Ping cannot be negative. Use PingValue.Unknown for unknown pings.", nameof(milliseconds))
            : new(milliseconds);
    }

    /// <summary>
    /// Returns true if this ping value is valid (not unknown).
    /// </summary>
    public bool IsValid() => _value >= 0;

    /// <summary>
    /// Returns true if this ping value is unknown or invalid.
    /// </summary>
    public bool IsUnknown() => _value < 0;

    /// <summary>
    /// Gets the ping in milliseconds.
    /// Returns -1 for unknown pings.
    /// Prefer using IsValid() to check validity before accessing this value.
    /// </summary>
    public int Milliseconds => _value;

    /// <summary>
    /// Gets the ping value or a default if unknown.
    /// </summary>
    /// <param name="defaultValue">The value to return if ping is unknown. Defaults to -1 (unknown).</param>
    /// <returns>The ping in milliseconds if valid, otherwise the default value.</returns>
    public int GetValueOrDefault(int defaultValue = -1) => IsValid() ? _value : defaultValue;

    /// <summary>
    /// Returns a localized string representation of this ping value.
    /// Format: "50 ms" for valid pings, or localized "Unknown" for invalid pings.
    /// </summary>
    public override string ToString() => IsValid()
        ? _value.ToString() + " " + "ms".L10N("Client:Main:MillisecondsShort")
        : "Unknown".L10N("Client:Main:UnknownPing");
}
