using ClientCore;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A class for updating of the CnCNet game/player count.
    /// </summary>
    public static class CnCNetPlayerCountTask
    {
        private static int REFRESH_INTERVAL = 60000; // 1 minute

        internal static event EventHandler<PlayerCountEventArgs> CnCNetGameCountUpdated;

        private static string cncnetLiveStatusIdentifier;

        public static void InitializeService(CancellationTokenSource cts)
        {
            cncnetLiveStatusIdentifier = ClientConfiguration.Instance.CnCNetLiveStatusIdentifier;

            RunServiceAsync(cts.Token);
        }

        private static async Task RunServiceAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                CnCNetGameCountUpdated?.Invoke(null, new PlayerCountEventArgs(await GetCnCNetPlayerCountAsync()));

                try
                {
                    await Task.Delay(REFRESH_INTERVAL, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static async Task<int> GetCnCNetPlayerCountAsync()
        {
            try
            {
                var httpClientHandler = new HttpClientHandler
                {
#if NETFRAMEWORK
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
#else
                    AutomaticDecompression = DecompressionMethods.All
#endif
                };
                using var client = new HttpClient(httpClientHandler, true)
                {
                    Timeout = TimeSpan.FromMilliseconds(Constants.TUNNEL_CONNECTION_TIMEOUT),
#if !NETFRAMEWORK
                    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
#endif
                };

                string info = await client.GetStringAsync("https://api.cncnet.org/status");

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
            catch (Exception ex)
            {
                PreStartup.LogException(ex);
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