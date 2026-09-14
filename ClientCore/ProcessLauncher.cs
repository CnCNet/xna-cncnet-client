using System;
using System.Diagnostics;

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
                Logger.Log($"Failed to start process '{commandLine} {arguments}': {ex.Message}");
            }
        }
    }
}
