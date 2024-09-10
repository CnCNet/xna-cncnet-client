using System.Diagnostics;

namespace ClientCore
{
    public static class ProcessLauncher
    {
        public static void StartShellProcess(string commandLine, string arguments = null)
        {
            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = commandLine,
                Arguments = arguments,
                UseShellExecute = true
            });
        }
    }
}
