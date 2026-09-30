#nullable enable
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// An HTTP client wrapper that enforces a per-request timeout covering
    /// the entire operation, including both connection establishment and
    /// response body read.
    /// </summary>
    internal sealed class TimedHttpClient
    {
        private static readonly HttpClient sharedHttpClient = new HttpClient()
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private readonly int timeoutMilliseconds;

        /// <param name="timeoutMilliseconds">
        /// The timeout in milliseconds. The entire HTTP operation must complete within this time.
        /// </param>
        public TimedHttpClient(int timeoutMilliseconds)
        {
            this.timeoutMilliseconds = timeoutMilliseconds;
        }

        /// <summary>
        /// Downloads the resource at the specified URL as a string.
        /// Guaranteed to return or throw within the configured timeout.
        /// </summary>
        public async Task<string> GetStringAsync(string url)
        {
            using var cts = new CancellationTokenSource(timeoutMilliseconds);

            // GetAsync with the default HttpCompletionOption.ResponseContentRead downloads
            // the complete response body before completing, so cts.Token covers the full operation.
            using var response = await sharedHttpClient.GetAsync(url, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronous wrapper for <see cref="GetStringAsync"/>.
        /// </summary>
        public string GetString(string url)
            => GetStringAsync(url).GetAwaiter().GetResult();

        /// <summary>
        /// Downloads the resource at the specified URL as a byte array.
        /// Guaranteed to return or throw within the configured timeout.
        /// </summary>
        public async Task<byte[]> GetBytesAsync(string url)
        {
            using var cts = new CancellationTokenSource(timeoutMilliseconds);

            using var response = await sharedHttpClient.GetAsync(url, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronous wrapper for <see cref="GetBytesAsync"/>.
        /// </summary>
        public byte[] GetBytes(string url)
            => GetBytesAsync(url).GetAwaiter().GetResult();

        /// <summary>
        /// Downloads the resource at the specified URL and saves it to a file.
        /// Guaranteed to complete or throw within the configured timeout.
        /// </summary>
        public async Task DownloadFileAsync(string url, string filePath)
        {
            using var cts = new CancellationTokenSource(timeoutMilliseconds);

            // Use ResponseHeadersRead for streaming to avoid buffering the entire file in memory.
            using var response = await sharedHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

            // CopyToAsync respects the cancellation token between iterations,
            // ensuring the timeout is enforced throughout the body read.
            await contentStream.CopyToAsync(fileStream, 81920, cts.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronous wrapper for <see cref="DownloadFileAsync"/>.
        /// </summary>
        public void DownloadFile(string url, string filePath)
            => DownloadFileAsync(url, filePath).GetAwaiter().GetResult();

        /// <summary>
        /// Posts the specified content to the URL and returns the response body as a byte array.
        /// Guaranteed to return or throw within the configured timeout.
        /// </summary>
        public async Task<byte[]> PostAsync(string url, HttpContent content)
        {
            using var cts = new CancellationTokenSource(timeoutMilliseconds);

            using var response = await sharedHttpClient.PostAsync(url, content, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronous wrapper for <see cref="PostAsync"/>.
        /// </summary>
        public byte[] Post(string url, HttpContent content)
            => PostAsync(url, content).GetAwaiter().GetResult();
    }
}
