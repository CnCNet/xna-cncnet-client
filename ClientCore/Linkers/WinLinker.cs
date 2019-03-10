using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ClientCore
{
    public static class WinLinker
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );

        [DllImport("kernel32.dll")]
        private static extern bool CreateSymbolicLink(
            string lpSymlinkFileName,
            string lpTargetFileName,
            int dwFlags
        );

        public static bool CreateHardLink(string source, string dest)
        {
            return CreateHardLink(dest, source, IntPtr.Zero);
        }
        public static bool CreateSymLink(string source, string dest)
        {
            return CreateSymbolicLink(dest, source, 0);
        }
    }
}