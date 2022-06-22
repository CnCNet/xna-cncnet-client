using System;
using System.Text;

namespace ClientCore.PlatformShim;

public static class EncodingExt
{
    static EncodingExt()
    {
#if NETCOREAPP3_0_OR_GREATER
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        ANSI = Encoding.GetEncoding(0);
    }

    public static Encoding ANSI { get; }
}