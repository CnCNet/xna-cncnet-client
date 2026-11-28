using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MigrationTool;

/// <summary>
/// A client version. <see cref="Latest"/> sorts after every numbered version.
/// </summary>
/// <remarks>
/// Every migration patch of this build is registered here, so <see cref="All"/> is the single
/// list of the client versions that can be migrated to.
/// </remarks>
internal readonly record struct ClientVersion(int Major, int Minor, int Patch) : IComparable<ClientVersion>
{
    public static ClientVersion Latest { get; } = new(int.MaxValue, int.MaxValue, int.MaxValue);

    public static ClientVersion V2_11_0 { get; } = new(2, 11, 0);

    public static ClientVersion V2_11_1 { get; } = new(2, 11, 1);

    public static ClientVersion V2_11_2 { get; } = new(2, 11, 2);

    public static ClientVersion V2_11_6 { get; } = new(2, 11, 6);

    public static ClientVersion V2_11_7 { get; } = new(2, 11, 7);

    public static ClientVersion V2_12_0 { get; } = new(2, 12, 0);

    public static ClientVersion V2_12_6 { get; } = new(2, 12, 6);

    public static ClientVersion V2_13_0 { get; } = new(2, 13, 0);

    public static ClientVersion V3_0_0 { get; } = new(3, 0, 0);

    /// <summary>
    /// All patches of this build, sorted by version.
    /// </summary>
    public static IReadOnlyList<ClientVersion> All { get; } =
        new[]
        {
            V2_11_0,
            V2_11_1,
            V2_11_2,
            V2_11_6,
            V2_11_7,
            V2_12_0,
            V2_12_6,
            V2_13_0,
            V3_0_0,
            Latest
        }
        .OrderBy(version => version)
        .ToArray();

    /// <summary>
    /// Name that identifies the patch class and the embedded pictures of the version,
    /// such as <c>v2_11_6</c>.
    /// </summary>
    public string Name => this == Latest ? nameof(Latest) : $"v{Major}_{Minor}_{Patch}";

    /// <summary>
    /// Name of the version as shown to the user, such as <c>v2.11.6</c>.
    /// </summary>
    public string DisplayName => this == Latest ? nameof(Latest) : $"v{Major}.{Minor}.{Patch}";

    /// <summary>
    /// Parses a version such as <c>2.12.0</c>, <c>2_12_0</c>, <c>v2.12.0</c> or <c>Latest</c>.
    /// </summary>
    public static bool TryParse(string value, out ClientVersion version)
    {
        string trimmedValue = value.Trim();
        trimmedValue = trimmedValue.TrimStart('v', 'V');

        if (trimmedValue.Equals(nameof(Latest), StringComparison.OrdinalIgnoreCase))
        {
            version = Latest;
            return true;
        }

        string[] parts = trimmedValue.Replace('_', '.').Split('.');

        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int major)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int minor)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int patch))
        {
            version = default;
            return false;
        }

        version = new ClientVersion(major, minor, patch);
        return true;
    }

    public int CompareTo(ClientVersion other)
    {
        int comparison = Major.CompareTo(other.Major);

        if (comparison != 0)
            return comparison;

        comparison = Minor.CompareTo(other.Minor);

        return comparison != 0 ? comparison : Patch.CompareTo(other.Patch);
    }

    public static bool operator >(ClientVersion left, ClientVersion right) => left.CompareTo(right) > 0;

    public static bool operator <(ClientVersion left, ClientVersion right) => left.CompareTo(right) < 0;

    public static bool operator >=(ClientVersion left, ClientVersion right) => left.CompareTo(right) >= 0;

    public static bool operator <=(ClientVersion left, ClientVersion right) => left.CompareTo(right) <= 0;

    public override string ToString() => DisplayName;
}
