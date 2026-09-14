#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.Matchmaking
{
    public class MatchmakingApiService : IDisposable
    {
        private readonly HttpClient httpClient;
        private string apiBaseUrl;
        private readonly JsonSerializerOptions jsonOptions;
        private bool fallbackTested;

        public MatchmakingApiService(string apiBaseUrl)
        {
            this.apiBaseUrl = NormalizeApiBaseUrl(apiBaseUrl);
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            httpClient.DefaultRequestHeaders.ConnectionClose = true;

            jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private static string NormalizeApiBaseUrl(string url)
        {
            string clean = url.TrimEnd('/');

            if (!clean.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
            {
                if (clean.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                {
                    clean += "/v1";
                }
                else
                {
                    clean += "/api/v1";
                }
            }

            return clean;
        }

        public async Task<QmMatchResponse?> SendMatchRequestAsync(string ladder, string playerName, QmMatchRequest request)
        {
            string url = $"{apiBaseUrl}/qm/{Uri.EscapeDataString(ladder)}/{Uri.EscapeDataString(playerName)}";
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                string json = JsonSerializer.Serialize(request, jsonOptions);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                Logger.Log($"[MatchmakingApi] Sending '{request.Type}' for '{playerName}' to {url}");

                HttpResponseMessage response = await httpClient.PostAsync(url, content);
                stopwatch.Stop();

                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Log($"[MatchmakingApi] HTTP error {response.StatusCode} in {stopwatch.ElapsedMilliseconds}ms: {responseBody}");

                    return new QmMatchResponse
                    {
                        Type = "error",
                        Description = $"Server error (HTTP {(int)response.StatusCode}): {response.ReasonPhrase}"
                    };
                }

                QmMatchResponse? result = JsonSerializer.Deserialize<QmMatchResponse>(responseBody, jsonOptions);

                if (result == null)
                {
                    Logger.Log($"[MatchmakingApi] Empty JSON response from server in {stopwatch.ElapsedMilliseconds}ms: {responseBody}");

                    return new QmMatchResponse
                    {
                        Type = "error",
                        Description = "Empty response received from matchmaking server."
                    };
                }

                Logger.Log($"[MatchmakingApi] Response received in {stopwatch.ElapsedMilliseconds}ms: type='{result.Type}'");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.Log($"[MatchmakingApi] Request exception after {stopwatch.ElapsedMilliseconds}ms: {ex.GetType().Name}: {ex.Message}");

                if (!fallbackTested && (ex is HttpRequestException || ex is System.Net.Sockets.SocketException))
                {
                    string oldBase = apiBaseUrl;
                    await TryProbeVmGatewayAsync();
                    if (!apiBaseUrl.Equals(oldBase, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Log($"[MatchmakingApi] Retrying request with auto-detected base URL: {apiBaseUrl}");
                        return await SendMatchRequestAsync(ladder, playerName, request);
                    }
                }

                return new QmMatchResponse
                {
                    Type = "error",
                    Description = ex.Message
                };
            }
        }

        public async Task<Dictionary<string, int>?> GetQueueCountsAsync()
        {
            string url = $"{apiBaseUrl}/qm/queue-counts";

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<Dictionary<string, int>>(responseBody, jsonOptions);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MatchmakingApi] GetQueueCounts error: {ex.Message}");

                if (!fallbackTested && (ex is HttpRequestException || ex is System.Net.Sockets.SocketException))
                {
                    string oldBase = apiBaseUrl;
                    await TryProbeVmGatewayAsync();
                    if (!apiBaseUrl.Equals(oldBase, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Log($"[MatchmakingApi] Retrying GetQueueCounts with auto-detected base URL: {apiBaseUrl}");
                        return await GetQueueCountsAsync();
                    }
                }

                return null;
            }
        }

        private async Task TryProbeVmGatewayAsync()
        {
            if (fallbackTested)
                return;

            fallbackTested = true;

            string[] candidates = new[]
            {
                "http://10.0.2.2:3000/api/v1",       // VirtualBox NAT host gateway
                "http://192.168.0.57:3000/api/v1",   // Host LAN IP
                "http://localhost:3000/api/v1"       // Local host
            };

            foreach (string candidate in candidates)
            {
                if (candidate.Equals(apiBaseUrl, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(1500);
                    using var req = new HttpRequestMessage(HttpMethod.Get, $"{candidate}/qm/queue-counts");
                    var resp = await httpClient.SendAsync(req, cts.Token);
                    if (resp.IsSuccessStatusCode)
                    {
                        Logger.Log($"[MatchmakingApi] Auto-detected reachable server at {candidate} (switched from {apiBaseUrl})");
                        apiBaseUrl = candidate;
                        return;
                    }
                }
                catch
                {
                    // Continue to next candidate
                }
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
