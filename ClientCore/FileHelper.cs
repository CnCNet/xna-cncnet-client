using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using ClientCore.Extensions;
using ClientCore.PlatformShim;

using Rampastring.Tools;

namespace ClientCore
{
    public class FileHelper
    {
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateHardLinkW")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SupportedOSPlatform("windows")]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        [DllImport("libc", EntryPoint = "link", SetLastError = true)]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("osx")]
        private static extern int link([MarshalAs(UnmanagedType.LPUTF8Str)] string oldname, [MarshalAs(UnmanagedType.LPUTF8Str)] string newname);

        public static void CreateHardLinkFromSource(string source, string destination, bool fallback = true)
        {
            if (fallback)
            {
                try
                {
                    CreateHardLinkFromSource(source, destination, fallback: false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to create hard link at {destination}. Fallback to copy. {ex.Message}");
                    File.Copy(source, destination, true);
                }

                return;
            }

            if (File.Exists(destination))
            {
                FileInfo destinationFile = new(destination);
                destinationFile.IsReadOnly = false;
                destinationFile.Delete();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!CreateHardLink(destination, source, IntPtr.Zero))
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (link(source, destination) != 0)
                    throw new IOException($"{"Client:DTAConfig:CreateHardLinkFailed".L10N()}: {destination}. Error: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static Encoding GetEncoding(string filename, float minimalConfidence = 0.5f)
        {
            Encoding encoding = EncodingExt.UTF8NoBOM;

            using (FileStream fs = File.OpenRead(filename))
            {
                Ude.CharsetDetector cdet = new Ude.CharsetDetector();
                cdet.Feed(fs);
                cdet.DataEnd();
                if (cdet.Charset != null && cdet.Confidence > minimalConfidence)
                {
                    Encoding detectedEncoding = Encoding.GetEncoding(cdet.Charset);

                    if (detectedEncoding is not UTF8Encoding and not ASCIIEncoding)
                        encoding = detectedEncoding;
                }
            }

            return encoding;
        }
    }
}