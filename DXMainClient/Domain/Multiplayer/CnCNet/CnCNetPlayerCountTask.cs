using ClientCore;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A class for updating of the CnCNet game/player count.
    /// </summary>
    public static class CnCNetPlayerCountTask
    {
        public static int PlayerCount { get; private set; }

        private static int REFRESH_INTERVAL = 60000; // 1 minute

        internal static event EventHandler<PlayerCountEventArgs> CnCNetGameCountUpdated;

        private static string cncnetLiveStatusIdentifier;

        public static void InitializeService(CancellationTokenSource cts)
        {
            cncnetLiveStatusIdentifier = ClientConfiguration.Instance.CnCNetLiveStatusIdentifier;
            
            // This call is synchronous. Therefore, we use a short timeout to avoid blocking the main thread for too long.
            PlayerCount = GetCnCNetPlayerCount(timeoutMilliseconds: 1000);

            CnCNetGameCountUpdated?.Invoke(null, new PlayerCountEventArgs(PlayerCount));
            ThreadPool.QueueUserWorkItem(new WaitCallback(RunService), cts);
        }

        private static void RunService(object tokenObj)
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
                    CnCNetGameCountUpdated?.Invoke(null, new PlayerCountEventArgs(GetCnCNetPlayerCount(timeoutMilliseconds: 5000)));
                }
            }
        }

        private static int GetCnCNetPlayerCount(int timeoutMilliseconds = 5000)
        {
            try
            {
                // Don't fetch the player count if it is explicitly disabled
                // For example, the official CnCNet server might be unavailable/unstable in a country with Internet censorship,
                // which causes lags in the splash screen. In the worst case, say if packets are dropped, it waits until timeouts
                if (string.IsNullOrWhiteSpace(ClientConfiguration.Instance.CnCNetPlayerCountURL))
                    return -1;

                WebClient client = new ExtendedWebClient(timeout: timeoutMilliseconds);
                Stream data = client.OpenRead(ClientConfiguration.Instance.CnCNetPlayerCountURL);

                string info = string.Empty;

                using (StreamReader reader = new StreamReader(data))
                {
                    info = reader.ReadToEnd();
                }

                info = info.Replace("{", String.Empty);
                info = info.Replace("}", String.Empty);
                info = info.Replace("\"", String.Empty);
                string[] values = info.Split(new char[] { ',' });

                int numGames = -1;

                foreach (string value in values)
                {
                    if (value.Contains(cncnetLiveStatusIdentifier))
                    {
                        numGames = Convert.ToInt32(value.Substring(cncnetLiveStatusIdentifier.Length + 1));
                        return numGames;
                    }
                }

                return numGames;
            }
            catch
            {
                return -1;
            }
        }
    }

    internal class PlayerCountEventArgs : EventArgs
    {
        public PlayerCountEventArgs(int playerCount)
        {
            PlayerCount = playerCount;
        }

        public int PlayerCount { get; set; }
    }
}
