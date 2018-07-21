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
            PlayerCount = GetCnCNetPlayerCount();

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
                    CnCNetGameCountUpdated?.Invoke(null, new PlayerCountEventArgs(GetCnCNetPlayerCount()));
                }
            }
        }

        private static int GetCnCNetPlayerCount()
        {
            try
            {
                WebClient client = new WebClient();

                Stream data = client.OpenRead("http://api.cncnet.org/status");
                
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
