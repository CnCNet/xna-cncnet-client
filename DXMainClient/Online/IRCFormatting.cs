#nullable enable
using System.Text;

namespace DTAClient.Online
{
    /// <summary>
    /// Handles mIRC-style formatting control codes in IRC messages.
    /// </summary>
    public static class IRCFormatting
    {
        private const char Bold = '\x02';
        private const char Color = '\x03';
        private const char HexColor = '\x04';
        private const char Reset = '\x0F';
        private const char Monospace = '\x11';
        private const char Reverse = '\x16';
        private const char Italics = '\x1D';
        private const char Strikethrough = '\x1E';
        private const char Underline = '\x1F';

        /// <summary>
        /// Removes all formatting control codes (bold, italics, underline, strikethrough,
        /// monospace, reverse, reset and colors) from an IRC message.
        /// </summary>
        /// <param name="message">The raw IRC message.</param>
        /// <param name="foregroundColorIndex">The first foreground color index specified in the message,
        /// or -1 if the message doesn't specify one.</param>
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
                        i++;
                        int colorIndex = ReadColorCodes(message, ref i, ReadDecimalColor);
                        if (foregroundColorIndex == -1)
                            foregroundColorIndex = colorIndex;

                        break;
                    case HexColor:
                        i++;
                        ReadColorCodes(message, ref i, ReadHexColor);
                        break;
                    case Bold:
                    case Reset:
                    case Monospace:
                    case Reverse:
                    case Italics:
                    case Strikethrough:
                    case Underline:
                        i++;
                        break;
                    default:
                        sb.Append(c);
                        i++;
                        break;
                }
            }

            return sb.ToString();
        }

        private delegate bool ColorReader(string message, ref int index, out int value);

        /// <summary>
        /// Reads an optional foreground color and an optional ",background" color.
        /// Returns the foreground color, or -1 if none was specified.
        /// </summary>
        private static int ReadColorCodes(string message, ref int index, ColorReader readColor)
        {
            if (!readColor(message, ref index, out int foreground))
                return -1;

            // The comma is only part of the color code if a background color follows it
            if (index < message.Length && message[index] == ',')
            {
                int afterComma = index + 1;
                if (readColor(message, ref afterComma, out _))
                    index = afterComma;
            }

            return foreground;
        }

        private static bool ReadDecimalColor(string message, ref int index, out int value)
        {
            value = 0;
            int start = index;

            while (index < message.Length && index - start < 2 && IsDigit(message[index]))
            {
                value = (value * 10) + (message[index] - '0');
                index++;
            }

            return index > start;
        }

        private static bool ReadHexColor(string message, ref int index, out int value)
        {
            value = -1;

            if (index + 6 > message.Length)
                return false;

            for (int i = index; i < index + 6; i++)
            {
                if (!IsHexDigit(message[i]))
                    return false;
            }

            index += 6;

            // Hex colors don't map to the IRC color palette
            return true;
        }

        private static bool IsDigit(char c) => c >= '0' && c <= '9';

        private static bool IsHexDigit(char c) => IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }
}
