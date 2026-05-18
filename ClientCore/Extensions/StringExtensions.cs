using System;
using System.Text;
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

    /// <summary>
    /// Returns a substring of this string starting at <paramref name="start"/> and containing at most
    /// <paramref name="maxLength"/> UTF-16 code units, ensuring the result does not end with an orphaned high surrogate.
    /// </summary>
    /// <param name="str">The string to slice.</param>
    /// <param name="start">The zero-based start index of the substring.</param>
    /// <param name="maxLength">Maximum number of UTF-16 code units to include in the result. Must be non-negative.</param>
    /// <returns>The safe substring.</returns>
    public static string SubstringSurrogateAware(this string str, int start, int maxLength)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (start < 0 || start > str.Length)
            throw new ArgumentOutOfRangeException(nameof(start), $"{nameof(start)} must be within the bounds of the string.");
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), $"{nameof(maxLength)} must be non-negative.");

        int available = str.Length - start;
        int length = maxLength < available ? maxLength : available;
        if (length > 0 && char.IsHighSurrogate(str[start + length - 1]))
            length--;

        return str.Substring(start, length);
    }

    /// <summary>
    /// Truncates this string to at most <paramref name="maxUtf8ByteLength"/> bytes in UTF-8.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <param name="maxUtf8ByteLength">Maximum UTF-8 byte length allowed for the returned string.</param>
    /// <returns>The original string if no truncation is needed; otherwise a UTF-8 byte-limited string.</returns>
    public static string TruncateToUtf8ByteLength(this string str, int maxUtf8ByteLength)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (maxUtf8ByteLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxUtf8ByteLength), $"{nameof(maxUtf8ByteLength)} must be non-negative.");
        if (str.Length == 0 || maxUtf8ByteLength == 0)
            return string.Empty;

        if (Encoding.UTF8.GetByteCount(str) <= maxUtf8ByteLength)
            return str;

        // Encoder.Convert fits as many source chars as possible into the byte budget
        // without splitting a multi-byte UTF-8 sequence or a surrogate pair.
        Encoder encoder = Encoding.UTF8.GetEncoder();
        char[] chars = str.ToCharArray();
        byte[] buffer = new byte[maxUtf8ByteLength];
        encoder.Convert(chars, 0, chars.Length, buffer, 0, buffer.Length,
            flush: true, out int charsUsed, out _, out _);

        return str.Substring(0, charsUsed);
    }
}
