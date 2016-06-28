using System;
using System.IO;
using System.Net;
using System.Threading;
using Rampastring.Tools;

namespace DTAClient.domain.CnCNet
{
    /// <summary>
    /// A class for automatic updating of the CnCNet game/player count.
    /// </summary>
    public static class CnCNetInfoController
    {
        internal static event EventHandler<PlayerCountEventArgs> CnCNetGameCountUpdated;

        static bool ServiceDisabled = false;

        public static void InitializeService()
        {
            Logger.Log("Initializing CnCNet live status parsing.");
            ServiceDisabled = false;
            Thread thread = new Thread(RunService);
            thread.Start();
        }

        private static void RunService()
        {
            int ticks = 0;

            while (!ServiceDisabled)
            {
                if (ticks == 10 && CnCNetGameCountUpdated != null)
                {
                    CnCNetGameCountUpdated(null, new PlayerCountEventArgs(GetCnCNetPlayerCount()));
                    ticks = 0;
                }

                Thread.Sleep(1000);
                ticks++;
            }
        }

        public static void DisableService()
        {
            ServiceDisabled = true;
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
                    if (value.Contains(MainClientConstants.CNCNET_LIVE_STATUS_ID))
                    {
                        numGames = Convert.ToInt32(value.Substring(MainClientConstants.CNCNET_LIVE_STATUS_ID.Length + 1));
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
