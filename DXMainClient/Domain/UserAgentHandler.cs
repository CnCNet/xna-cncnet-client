using ClientCore;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DTAClient.Domain
{
    class UserAgentHandler
    {
        [DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
        private static extern int UrlMkSetSessionOption(
            int dwOption, string pBuffer, int dwBufferLength, int dwReserved);

        const int URLMON_OPTION_USERAGENT = 0x10000001;

        public static void ChangeUserAgent()
        {
            List<string> userAgent = new List<string>();
            string ua = "DTA Client/" + Application.ProductVersion + "/Game " + ClientConfiguration.Instance.LocalGame + Environment.OSVersion.VersionString;

            UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, ua, ua.Length, 0);
        }
    }
}
