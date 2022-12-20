using System;
using ClientCore.I18N;

namespace ClientCore.Extensions;

public static class StringExtensions
{
    public static string GetLink(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        int index = text.IndexOf("http://", StringComparison.Ordinal);
        if (index == -1)
            index = text.IndexOf("ftp://", StringComparison.Ordinal);
        if (index == -1)
            index = text.IndexOf("https://", StringComparison.Ordinal);

        if (index == -1)
            return null; // No link found

        string link = text.Substring(index);
        return link.Split(' ')[0]; // Nuke any words coming after the link
    }

    private const string ESCAPED_INI_NEWLINE_PATTERN = $"\\{ProgramConstants.INI_NEWLINE_PATTERN}";
    private const string ESCAPED_SEMICOLON = "\\semicolon";

    /// <summary>
    /// Converts a regular string to an INI representation of it.
    /// </summary>
    /// <param name="raw">Input string.</param>
    /// <returns>INI-safe string.</returns>
    public static string ToIniString(this string raw)
    {
        return raw
            .Replace(ProgramConstants.INI_NEWLINE_PATTERN, ESCAPED_INI_NEWLINE_PATTERN)
            .Replace(";", ESCAPED_SEMICOLON)
            .Replace(Environment.NewLine, "\n")
            .Replace("\n", ProgramConstants.INI_NEWLINE_PATTERN);
    }

    /// <summary>
    /// Converts an INI-safe string to a normal string.
    /// </summary>
    /// <param name="iniString">Input INI string.</param>
    /// <returns>Regular string.</returns>
    public static string FromIniString(this string iniString)
    {
        return iniString
            .Replace(ESCAPED_INI_NEWLINE_PATTERN, ProgramConstants.INI_NEWLINE_PATTERN)
            .Replace(ESCAPED_SEMICOLON, ";")
            .Replace(ProgramConstants.INI_NEWLINE_PATTERN, Environment.NewLine);
    }

    /// <summary>
    /// Looks up a translated string for the specified key.
    /// </summary>
    /// <param name="defaultValue">The default string value as a fallback.</param>
    /// <param name="key">The unique key name.</param>
    /// <returns>The translated string value.</returns>
    public static string L10N(this string defaultValue, string key)
        => Translation.Instance.LookUp(key, defaultValue);
}
