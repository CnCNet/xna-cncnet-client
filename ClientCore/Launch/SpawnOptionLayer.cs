#nullable enable

using System.Collections.Generic;
using System.Globalization;

using Rampastring.Tools;

namespace ClientCore.Launch;

/// <summary>A game setting defined by the mod. The client passes it to the game without reading it.</summary>
public sealed record SpawnOption(string Key, string Value);

/// <summary>
/// One named source of mod-defined settings, such as the lobby's controls or a map's
/// forced options. Layers are applied in order, so a later layer overrides an earlier one.
/// </summary>
public sealed record SpawnOptionLayer(string Name, IReadOnlyList<SpawnOption> Options)
{
    /// <summary>Returns the value the last layer set for a key, or null when no layer sets it.</summary>
    public static string? GetMergedValue(IEnumerable<SpawnOptionLayer> layers, string key)
    {
        string? value = null;

        foreach (SpawnOptionLayer layer in layers)
        {
            foreach (SpawnOption option in layer.Options)
            {
                if (option.Key == key)
                    value = option.Value;
            }
        }

        return value;
    }

    /// <summary>Converts a boolean to the "True"/"False" form INI files use.</summary>
    public static string FormatBoolean(bool value) => Conversions.BooleanToString(value, BooleanStringStyle.TRUEFALSE);

    /// <summary>Converts an integer to the form INI files use.</summary>
    public static string FormatInt(int value) => value.ToString(CultureInfo.InvariantCulture);
}
