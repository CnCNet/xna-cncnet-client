using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Rampastring.Tools;

namespace ClientCore
{
    public static class ProcessLauncher
    {
        public static void StartShellProcess(string commandLine, string arguments = null)
        {
            try
            {
                using var _ = Process.Start(new ProcessStartInfo
                {
                    FileName = commandLine,
                    Arguments = arguments,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(arguments))
                    Logger.Log($"Failed to start process '{commandLine}': {ex.Message}");
                else
                    Logger.Log($"Failed to start process '{commandLine} {arguments}': {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the folder containing a file in the system file manager and selects the file where supported.
        /// </summary>
        /// <param name="filePath">The full path to the file.</param>
        public static void OpenContainingFolder(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // https://stackoverflow.com/questions/13680415/how-to-open-explorer-with-a-specific-file-selected
                StartShellProcess("explorer.exe", $"/select,\"{filePath}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // https://stackoverflow.com/questions/39214539/opening-finder-from-terminal-with-file-selected
                StartShellProcess("open", $"-R \"{filePath}\"");
            }
            else
            {
                // Linux: no standard way to select a file, just open the folder
                StartShellProcess(Path.GetDirectoryName(filePath));
            }
        }
    }
}
