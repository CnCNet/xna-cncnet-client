using System.Text;

namespace ClientCore.PlatformShim;

public static class EncodingExt
{
    static EncodingExt()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        ANSI = Encoding.GetEncoding(0);
    }

    /// <summary>
    /// Gets the legacy ANSI encoding (not Windows-1252 and also not any specific encoding).
    /// ANSI doesn't mean a specific codepage, it means the default non-Unicode codepage which can be changed from Control Panel.
    /// </summary>
    public static Encoding ANSI { get; }
}