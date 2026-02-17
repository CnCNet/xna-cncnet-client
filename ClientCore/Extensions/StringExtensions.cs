using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

using ClientCore.I18N;

namespace ClientCore.Extensions;

public static class StringExtensions
{
    private static Regex extractLinksRE = new Regex(@"((http[s]?)|(ftp))\S+");

    public static string[] GetLinks(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var matches = extractLinksRE.Matches(text);

        if (matches.Count == 0)
            return null; // No link found

        string[] links = new string[matches.Count];
        for (int i = 0; i < links.Length; i++)
            links[i] = matches[i].Value.Trim();
            
        return links;
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
        if (raw.Contains(ESCAPED_INI_NEWLINE_PATTERN, StringComparison.InvariantCulture))
            throw new ArgumentException($"The string contains an illegal character sequence! ({ESCAPED_INI_NEWLINE_PATTERN})");

        if (raw.Contains(ESCAPED_SEMICOLON, StringComparison.InvariantCulture))
            throw new ArgumentException($"The string contains an illegal character sequence! ({ESCAPED_SEMICOLON})");

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
    /// <param name="notify">Whether to add this key and value to the list of missing key-values.</param>
    /// <returns>The translated string value.</returns>
    /// <remarks>
    /// This method is referenced by <c>TranslationNotifierGenerator</c> in order to check if the const
    /// values that are not initialized on client start automatically are missing (via notification
    /// mechanism implemented down the call chain). Do not change the signature or move the method out
    /// of the namespace it's currently defined in. If you do - you have to also edit the generator
    /// source code to match.
    /// </remarks>
    public static string L10N(this string defaultValue, string key, bool notify = true)
        => string.IsNullOrEmpty(defaultValue)
            ? defaultValue
            : Translation.Instance.LookUp(key, defaultValue, notify);

    /// <summary>
    /// Replace special characters with spaces in the filename to avoid conflicts with WIN32API.
    /// </summary>
    /// <param name="defaultValue">The default string value.</param>
    /// <returns>File name without special characters or reserved combinations.</returns>
    /// <remarks>
    /// Reference: <a href="https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file">Naming Files, Paths, and Namespaces</a>.
    /// </remarks>
    public static string ToWin32FileName(this string filename)
    {
        foreach (char ch in "/\\:*?<>|")
            filename = filename.Replace(ch, '_');

        // If the user is somehow using "con" or any other filename that is
        // reserved by WIN32API, it would be better to rename it.

        HashSet<string> reservedFileNames = new HashSet<string>(new List<string>(){
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³"
        }, StringComparer.InvariantCultureIgnoreCase);

        if (reservedFileNames.Contains(filename))
            filename += "_";

        return filename;
    }
  
    public static T ToEnum<T>(this string value) where T : Enum 
        => (T)Enum.Parse(typeof(T), value, true);

    public static string[] SplitWithCleanup(this string value, char[] separators = null)
        => value
            .Split(separators ?? [','])
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
}
