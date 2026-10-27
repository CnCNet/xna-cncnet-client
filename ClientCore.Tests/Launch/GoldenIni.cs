using System;
using System.IO;
using System.Text;

using Rampastring.Tools;

using Xunit;

namespace ClientCore.Tests.Launch;

/// <summary>Compares generated INI files against expected files under Launch/Golden.</summary>
internal static class GoldenIni
{
    public static string Render(IniFile iniFile)
    {
        using var stream = new MemoryStream();
        iniFile.WriteIniStream(stream, new UTF8Encoding(false));

        return Normalize(Encoding.UTF8.GetString(stream.ToArray()));
    }

    /// <summary>Asserts that the INI file matches the golden file, including section and key order.</summary>
    public static void AssertMatches(string goldenName, IniFile actual)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Launch", "Golden", goldenName + ".ini");
        string expected = Normalize(File.ReadAllText(path));

        Assert.Equal(expected, Render(actual));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd() + "\n";
}
