using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A class for updating of the CnCNet game/player count.
    /// </summary>
    public static class CnCNetPlayerCountTask
    {
        private const int REFRESH_INTERVAL = 60000;
        private const int REFRESH_TIMEOUT = 10000;

        internal static event EventHandler<PlayerCountEventArgs> CnCNetGameCountUpdated;

        private static string cncnetLiveStatusIdentifier;

        public static void InitializeService(CancellationTokenSource cts)
        {
            cncnetLiveStatusIdentifier = ClientConfiguration.Instance.CnCNetLiveStatusIdentifier;

            RunServiceAsync(cts.Token).HandleTask();
        }

        private static async ValueTask RunServiceAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var timeoutCancellationTokenSource = new CancellationTokenSource(REFRESH_TIMEOUT);
                    using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

                    CnCNetGameCountUpdated?.Invoke(null, new PlayerCountEventArgs(await GetCnCNetPlayerCountAsync(linkedCancellationTokenSource.Token).ConfigureAwait(false)));
                    await Task.Delay(REFRESH_INTERVAL, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private static async ValueTask<int> GetCnCNetPlayerCountAsync(CancellationToken cancellationToken)
        {
            try
            {
                string info = await Constants.CnCNetHttpClient.GetStringAsync($"{Uri.UriSchemeHttps}://api.cncnet.org/status", cancellationToken).ConfigureAwait(false);

                info = info.Replace("{", string.Empty);
                info = info.Replace("}", string.Empty);
                info = info.Replace("\"", string.Empty);
                string[] values = info.Split(new[] { ',' });

                int numGames = -1;

                foreach (string value in values)
                {
                    if (value.Contains(cncnetLiveStatusIdentifier))
                    {
                        numGames = Convert.ToInt32(value[(cncnetLiveStatusIdentifier.Length + 1)..], CultureInfo.InvariantCulture);
                        return numGames;
                    }
                }

                return numGames;
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex);
                return -1;
            }
        }
    }

    internal sealed class PlayerCountEventArgs : EventArgs
    {
        public PlayerCountEventArgs(int playerCount)
        {
            PlayerCount = playerCount;
        }

        public int PlayerCount { get; set; }
    }
}