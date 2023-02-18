using System;
using ClientCore;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Online
{
    internal static class CnCNetGameCheck
    {
        private const int REFRESH_INTERVAL = 15000; // 15 seconds

        public static async ValueTask RunServiceAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(REFRESH_INTERVAL, cancellationToken).ConfigureAwait(false);

                    CheatEngineWatchEvent();
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private static void CheatEngineWatchEvent()
        {
            Process[] processlist = Process.GetProcesses();

            foreach (Process process in processlist)
            {
                try
                {
                    if (process.ProcessName.Contains("cheatengine") ||
                        process.MainWindowTitle.ToLower().Contains("cheat engine") ||
                        process.MainWindowHandle.ToString().ToLower().Contains("cheat engine"))
                    {
                        KillGameInstance();
                    }
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex);
                }

                process.Dispose();
            }
        }

        private static void KillGameInstance()
        {
            string gameExecutableName = ClientConfiguration.Instance.GetOperatingSystemVersion() == OSVersion.UNIX ?
                ClientConfiguration.Instance.UnixGameExecutableName :
                ClientConfiguration.Instance.GetGameExecutableName();

            foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(gameExecutableName)))
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex);
                }

                process.Dispose();
            }
        }
    }
}