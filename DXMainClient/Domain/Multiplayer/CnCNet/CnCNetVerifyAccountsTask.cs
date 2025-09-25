using System;
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A class for calling the CnCNet API to verify accounts periodically
    /// </summary>
    public static class CnCNetVerifyAccountsTask
    {
        public static event Action<object> VerifyCall;

        private static int REFRESH_INTERVAL = 60000; // 1 minute

        public static void InitializeService(CancellationTokenSource cts)
        {
            VerifyCall?.Invoke(null);
            ThreadPool.QueueUserWorkItem(new WaitCallback(RunTask), cts);
        }

        private static void RunTask(object tokenObj)
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
                    VerifyCall?.Invoke(null);
                }
            }
        }
    }
}
