#nullable enable
using System.Text;

namespace DTAClient.Online;

/// <summary>
/// Handles mIRC-style formatting control codes in IRC messages.
/// </summary>
/// <remarks>
/// IRC clients format text by inserting invisible control characters into the message,
/// e.g. "\x02bold\x02" or "\x0304red text". The client's fonts have no glyphs for these
/// characters, so if they aren't removed they're rendered as unknown characters.
/// See https://modern.ircdocs.horse/formatting.html for the full specification.
/// </remarks>
public static class IRCFormatting
{
    // Control characters that toggle a style on or off. They're never followed by any parameters.
    private const char Bold = '\x02';
    private const char Reset = '\x0F';
    private const char Monospace = '\x11';
    private const char Reverse = '\x16';
    private const char Italics = '\x1D';
    private const char Strikethrough = '\x1E';
    private const char Underline = '\x1F';

    /// <summary>
    /// Starts a color code that uses the IRC color palette: "\x03" followed by an optional
    /// foreground color number and an optional ",background" color number, e.g. "\x0304,12".
    /// </summary>
    private const char Color = '\x03';

    /// <summary>
    /// Starts a color code that uses RGB colors: "\x04" followed by an optional
    /// foreground color and an optional ",background" color, e.g. "\x04FF0000,0000FF".
    /// </summary>
    private const char HexColor = '\x04';

    /// <summary>
    /// The maximum number of digits in an IRC palette color number.
    /// For example, in "\x0312" the color number is "12", which is 2 digits long.
    /// </summary>
    private const int MaxColorNumberLength = 2;

    /// <summary>
    /// The number of hex digits in an RGB color (RRGGBB).
    /// For example, in "\x04FF0000" the RGB color is "FF0000", which is 6 digits long.
    /// </summary>
    private const int HexColorLength = 6;

    /// <summary>
    /// Removes all formatting control codes (bold, italics, underline, strikethrough,
    /// monospace, reverse, reset and colors) from an IRC message.
    /// </summary>
    /// <param name="message">The raw IRC message.</param>
    /// <param name="foregroundColorIndex">The first IRC palette foreground color number
    /// in the message, or -1 if the message doesn't contain one.
    /// The client draws each chat message in a single color, so this is used as the message's color.</param>
    /// <returns>The message without any formatting control codes.</returns>
    public static string StripFormatting(string message, out int foregroundColorIndex)
    {
        foregroundColorIndex = -1;

        if (string.IsNullOrEmpty(message))
            return message;

        var sb = new StringBuilder(message.Length);
        int i = 0;

        while (i < message.Length)
        {
            char c = message[i];

            switch (c)
            {
                case Color:
                    // Skip the \x03, then its color numbers (if any)
                    i++;
                    if (TryReadColorNumber(message, ref i, out int colorNumber))
                    {
                        // Only the first color in the message is used
                        if (foregroundColorIndex == -1)
                            foregroundColorIndex = colorNumber;

                        SkipBackgroundColor(message, ref i, isHexColor: false);
                    }

                    break;
                case HexColor:
                    // Skip the \x04, then its RGB colors (if any).
                    // RGB colors can't be mapped to the IRC color palette, so they don't affect the message color.
                    i++;
                    if (TrySkipHexColor(message, ref i))
                        SkipBackgroundColor(message, ref i, isHexColor: true);

                    break;
                case Bold:
                case Reset:
                case Monospace:
                case Reverse:
                case Italics:
                case Strikethrough:
                case Underline:
                    // Style toggles are a single character, so just skip them
                    i++;
                    break;
                default:
                    // Regular text, keep it
                    sb.Append(c);
                    i++;
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Skips an optional ",background" color that follows a foreground color.
    /// </summary>
    /// <remarks>
    /// The comma only belongs to the color code if a valid color follows it.
    /// Otherwise it's regular text, e.g. in "\x0312,hello" the comma is part of the message.
    /// </remarks>
    private static void SkipBackgroundColor(string message, ref int index, bool isHexColor)
    {
        if (index >= message.Length || message[index] != ',')
            return;

        int afterComma = index + 1;
        bool hasBackgroundColor = isHexColor
            ? TrySkipHexColor(message, ref afterComma)
            : TryReadColorNumber(message, ref afterComma, out _);

        if (hasBackgroundColor)
            index = afterComma;
    }

    /// <summary>
    /// Reads an IRC palette color number of 1 or 2 digits, e.g. "4" or "04".
    /// On success, moves <paramref name="index"/> past the digits.
    /// </summary>
    /// <returns>True if a color number was found, otherwise false.</returns>
    private static bool TryReadColorNumber(string message, ref int index, out int colorNumber)
    {
        colorNumber = 0;
        int start = index;

        while (index < message.Length && index - start < MaxColorNumberLength && IsDigit(message[index]))
        {
            colorNumber = (colorNumber * 10) + (message[index] - '0');
            index++;
        }

        return index > start;
    }

    /// <summary>
    /// Checks for an RGB color of exactly 6 hex digits (RRGGBB), e.g. "FF0000".
    /// On success, moves <paramref name="index"/> past the digits.
    /// </summary>
    /// <returns>True if an RGB color was found, otherwise false.</returns>
    private static bool TrySkipHexColor(string message, ref int index)
    {
        if (index + HexColorLength > message.Length)
            return false;

        for (int i = index; i < index + HexColorLength; i++)
        {
            if (!IsHexDigit(message[i]))
                return false;
        }

        index += HexColorLength;
        return true;
    }

    // char.IsDigit also accepts non-ASCII digits, and char.IsAsciiDigit isn't available on .NET Framework
    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static bool IsHexDigit(char c) => IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}
