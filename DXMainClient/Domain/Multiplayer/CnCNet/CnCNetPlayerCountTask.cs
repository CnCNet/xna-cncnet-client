using ClientCore;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections.Generic;

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

        public static Dictionary<string, int> AllOnlineCounts { get; private set; }

        public static void InitializeService(CancellationTokenSource cts)
        {
            cncnetLiveStatusIdentifier = ClientConfiguration.Instance.CnCNetLiveStatusIdentifier;
            AllOnlineCounts = GetCnCNetPlayerCount();

            if (AllOnlineCounts.TryGetValue(ClientConfiguration.Instance.CnCNetLiveStatusIdentifier, out int value))
                PlayerCount = value;
            else
                PlayerCount = -1;

            CnCNetGameCountUpdated?.Invoke(null, new PlayerCountEventArgs(AllOnlineCounts));
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

        private static Dictionary<string, int> GetCnCNetPlayerCount()
        {
            var counts = new Dictionary<string, int>();
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

                foreach (string value in values)
                {
                    string[] kv = value.Split(new char[] { ':' });

                    if (kv.Length > 1 && int.TryParse(kv[1], out int c))
                        counts[kv[0]] = c;
                }

                return counts;
            }
            catch
            {
                return counts;
            }
        }
    }

    internal class PlayerCountEventArgs : EventArgs
    {
        public PlayerCountEventArgs(Dictionary<string, int> allOnline)
        {
            AllOnlineCounts = allOnline;

            if (AllOnlineCounts.TryGetValue(ClientConfiguration.Instance.CnCNetLiveStatusIdentifier, out int value))
                PlayerCount = value;
            else
                PlayerCount = -1;
        }

        public int PlayerCount { get; set; }
        public Dictionary<string, int> AllOnlineCounts { get; private set; }
    }
}
