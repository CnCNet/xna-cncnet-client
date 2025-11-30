using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Rampastring.Tools;
using ClientCore.PlatformShim;

namespace ClientCore.Extensions;

public class FileExtensions
{

    /// <summary>
    /// Establishes a hard link between an existing file and a new file. This function is only supported on the NTFS file system, and only for files, not directories.
    /// <br/>
    /// https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createhardlinkw
    /// </summary>
    /// <param name="lpFileName">The name of the new file.</param>
    /// <param name="lpExistingFileName">The name of the existing file.</param>
    /// <param name="lpSecurityAttributes">Reserved; must be NULL.</param>
    /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero (0).</returns>
    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateHardLinkW")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows")]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    /// <summary>
    /// The link function makes a new link to the existing file named by oldname, under the new name newname.
    /// <br/>
    /// https://www.gnu.org/software/libc/manual/html_node/Hard-Links.html
    /// <param name="oldname"></param>
    /// <param name="newname"></param>
    /// <returns>This function returns a value of 0 if it is successful and -1 on failure.</returns>
    [DllImport("libc", EntryPoint = "link", SetLastError = true)]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("osx")]
    private static extern int link([MarshalAs(UnmanagedType.LPUTF8Str)] string oldname, [MarshalAs(UnmanagedType.LPUTF8Str)] string newname);

    /// <summary>
    /// Creates hard link to the source file or copy that file, if got an error.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="fallback"></param>
    /// <exception cref="IOException"></exception>
    /// <exception cref="PlatformNotSupportedException"></exception>
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
                throw new IOException(string.Format("Unable to create hard link at {0} with the following error code: {1}"
                    .L10N("Client:DTAConfig:CreateHardLinkFailed"), destination, Marshal.GetLastWin32Error()));
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }

    /// <summary>
    /// Predicts text file encoding by its content.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="minimalConfidence"></param>
    /// <returns></returns>
    public static Encoding GetDetectedEncoding(string filename, float minimalConfidence = 0.5f)
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
