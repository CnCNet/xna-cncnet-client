using System;
using ClientCore.I18N;

namespace ClientCore.Extensions
{
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

        /// <summary>
        /// Looks up a localized string for the specified label.
        /// </summary>
        /// <param name="defaultValue">The default string value as a fallback.</param>
        /// <param name="label">The unique label name.</param>
        /// <returns>The translated string value.</returns>
        public static string L10N(this String defaultValue, string label)
            => Locale.Instance.Localize(label, defaultValue);
    }
}
