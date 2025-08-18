using ClientCore;
using System.Diagnostics;
using System.Threading;

namespace DTAClient.Online
{
    public sealed class CnCNetGameCheck
    {
        private static readonly CnCNetGameCheck _instance = new CnCNetGameCheck();

        private static readonly int REFRESH_INTERVAL = 15000; // 15 seconds

        private CnCNetGameCheck() { }

        public static CnCNetGameCheck Instance => _instance;

        public void InitializeService(CancellationTokenSource cts)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(RunService), cts);
        }

        private void RunService(object tokenObj)
        {
            var waitHandle = ((CancellationTokenSource)tokenObj).Token.WaitHandle;

            while (true)
            {
                if (waitHandle.WaitOne(REFRESH_INTERVAL))
                {
                    // Cancellation signaled
                    return;
                }
                else
                {
                    CheatEngineWatchEvent();
                }
            }
        }

        private void CheatEngineWatchEvent()
        {
            if (!ProgramConstants.IsInGame) 
                return;

            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                try {
                    if (process.ProcessName.Contains("cheatengine") ||
                        process.MainWindowTitle.ToLower().Contains("cheat engine")
                        )
                    {
                        KillGameInstance();
                    }
                }
                catch { }

                process.Dispose();
            }
        }

        private void KillGameInstance()
        {
            try
            {
                string gameExecutableName = ClientConfiguration.Instance.GetOperatingSystemVersion() == OSVersion.UNIX ?
                    ClientConfiguration.Instance.UnixGameExecutableName :
                    ClientConfiguration.Instance.GetGameExecutableName();

                gameExecutableName = gameExecutableName.Replace(".exe", "");

                Process[] processlist = Process.GetProcesses();
                foreach (Process process in processlist)
                {
                    try {
                        if (process.ProcessName.Contains(gameExecutableName))
                        {
                            process.Kill();
                        }
                    }
                    catch { }

                    process.Dispose();
                }
            }
            catch
            {
            }
        }
    }
}
