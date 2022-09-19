using System.Diagnostics;

namespace ClientCore
{
    public static class ProcessLauncher
    {
        public static void StartShellProcess(string commandLine)
        {
            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = commandLine,
                UseShellExecute = true
            });
        }
    }
}