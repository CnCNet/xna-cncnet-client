using System;
using System.Diagnostics;
using System.ComponentModel;
using Rampastring.Tools;
using System.IO;

namespace ClientCore
{
    /// <summary>
    /// Lib class for linking (or copying) files.
    /// </summary>
    public static class ClientUtils
    {
        public static OSVersion GetOperatingSystemVersion()
        {
            Version osVersion = Environment.OSVersion.Version;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (osVersion.Major < 5)
                    return OSVersion.UNKNOWN;

                if (osVersion.Major == 5)
                    return OSVersion.WINXP;

                if (osVersion.Minor > 1)
                    return OSVersion.WIN810;
                else if (osVersion.Minor == 0)
                    return OSVersion.WINVISTA;

                return OSVersion.WIN7;
            }

            int p = (int)Environment.OSVersion.Platform;

            // http://mono.wikia.com/wiki/Detecting_the_execution_platform
            if (p == 4 || p == 6 || p == 128)
                return OSVersion.UNIX;

            return OSVersion.UNKNOWN;
        }

        public static bool BindFile(string sourcePath, string destPath) {
            OSVersion osVersion = GetOperatingSystemVersion();

            try
            {
                if (File.Exists(destPath))
                    File.Delete(destPath);
            }
            catch
            {
                Logger.Log("Can't remove file");
                return false;
            }

            try
            {
                if (osVersion == OSVersion.WINVISTA || osVersion == OSVersion.WIN7 || osVersion == OSVersion.WIN810)
                {
                    if (!WinLinker.CreateSymLink(sourcePath, destPath))
                        throw new IOException("Symlinking failed, copying file instead");
                }
                if (osVersion == OSVersion.WINXP)
                {
                    if (!WinLinker.CreateHardLink(sourcePath, destPath))
                        throw new IOException("Hardlinking failed, copying file instead");
                }
                else throw new NotSupportedException("Unsupported OS, copying file instead");
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);
                try
                {
                    File.Copy(sourcePath, destPath, false);
                }
                catch
                {
                    Logger.Log("Can't copy file");
                    return false;
                }
            }

            if (File.Exists(destPath)) return true;
            return false;
        }
    }
}