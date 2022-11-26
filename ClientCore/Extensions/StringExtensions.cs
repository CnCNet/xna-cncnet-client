using System;

namespace ClientCore.Extensions
{
    public static class StringExtensions
    {
        public static string GetLink(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            int index = text.IndexOf($"{Uri.UriSchemeHttp}://", StringComparison.Ordinal);
            if (index == -1)
                index = text.IndexOf($"{Uri.UriSchemeFtp}://", StringComparison.Ordinal);
            if (index == -1)
                index = text.IndexOf($"{Uri.UriSchemeHttps}://", StringComparison.Ordinal);

            if (index == -1)
                return null; // No link found

            string link = text.Substring(index);
            return link.Split(' ')[0]; // Nuke any words coming after the link
        }
    }
}
