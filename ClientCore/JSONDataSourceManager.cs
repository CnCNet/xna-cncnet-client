using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Rampastring.Tools;

namespace ClientCore;

/// <summary>
/// Manages shared web data sources (JSON or INI) for XNAClientWebLabel.
/// Data sources are listed in the [DataSources] section of an INI file.
/// </summary>
public class JSONDataSourceManager
{
    private readonly Dictionary<string, DataSource> _dataSources = new Dictionary<string, DataSource>();
    private readonly PauseGate _pauseGate = new PauseGate();

    public JSONDataSourceManager() { }

    /// <summary>
    /// Loads data sources from an INI file.
    /// </summary>
    /// <param name="iniFile">The INI file to read from</param>
    public void LoadFromINI(IniFile iniFile)
    {
        const string dataSourcesSection = "DataSources";
        var section = iniFile.GetSection(dataSourcesSection);

        if (section == null)
            return;

        int count = section.GetIntValue("Count", 0);

        for (int i = 1; i <= count; i++)
        {
            string dataSourceName = section.GetStringValue(i.ToString(), string.Empty);

            if (string.IsNullOrEmpty(dataSourceName))
                continue;

            if (_dataSources.ContainsKey(dataSourceName))
                continue;

            var dataSourceSection = iniFile.GetSection(dataSourceName);
            if (dataSourceSection == null)
            {
                Logger.Log($"JSONDataSourceManager: Data source '{dataSourceName}' listed but section not found");
                continue;
            }

            string url = dataSourceSection.GetStringValue("URL", string.Empty);
            if (string.IsNullOrEmpty(url))
            {
                Logger.Log($"JSONDataSourceManager: Data source '{dataSourceName}' has no URL");
                continue;
            }

            int refreshInterval = dataSourceSection.GetIntValue("RefreshIntervalSeconds", 300);
            int timeout = dataSourceSection.GetIntValue("TimeoutSeconds", 10);
            IDataSourceFormat format = dataSourceSection.GetStringValue("Format", "json").ToLowerInvariant() switch
            {
                "ini" => new IniDataSourceFormat(),
                _ => new JsonDataSourceFormat()
            };

            var dataSource = new DataSource(dataSourceName, url, refreshInterval, timeout, format, _pauseGate);
            _dataSources.Add(dataSourceName, dataSource);
        }
    }

    /// <summary>
    /// Subscribes to a data source and receives its data whenever it updates.
    /// </summary>
    /// <param name="dataSourceId">The ID of the data source</param>
    /// <param name="callback">Callback invoked when new data is available (data, isError)</param>
    /// <returns>True if subscription was successful, false if data source not found</returns>
    public bool Subscribe(string dataSourceId, Action<JsonNode, bool> callback)
    {
        if (string.IsNullOrEmpty(dataSourceId))
            return false;

        if (!_dataSources.TryGetValue(dataSourceId, out var dataSource))
            return false;

        dataSource.Subscribe(callback);
        return true;
    }

    /// <summary>
    /// Unsubscribes from a data source.
    /// </summary>
    public void Unsubscribe(string dataSourceId, Action<JsonNode, bool> callback)
    {
        if (string.IsNullOrEmpty(dataSourceId))
            return;

        if (_dataSources.TryGetValue(dataSourceId, out var dataSource))
            dataSource.Unsubscribe(callback);
    }

    /// <summary>
    /// Stops starting new fetches, e.g. while the game is running. Fetches already
    /// in progress complete normally.
    /// </summary>
    public void Pause() => _pauseGate.Pause();

    /// <summary>
    /// Resumes fetching. Sources whose refresh came due while paused fetch immediately.
    /// </summary>
    public void Resume() => _pauseGate.Resume();

    private class PauseGate
    {
        private readonly object _lock = new object();
        private TaskCompletionSource<bool> _resumed;

        public void Pause()
        {
            lock (_lock)
                _resumed ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void Resume()
        {
            TaskCompletionSource<bool> resumed;
            lock (_lock)
            {
                resumed = _resumed;
                _resumed = null;
            }

            resumed?.TrySetResult(true);
        }

        public async Task WaitWhilePausedAsync(CancellationToken token)
        {
            Task resumedTask;
            lock (_lock)
                resumedTask = _resumed?.Task;

            if (resumedTask == null)
                return;

            await Task.WhenAny(resumedTask, Task.Delay(Timeout.Infinite, token)).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
        }
    }

    private interface IDataSourceFormat
    {
        JsonNode Convert(string input);
    }

    private class JsonDataSourceFormat : IDataSourceFormat
    {
        public JsonNode Convert(string input) => JsonNode.Parse(input);
    }

    private class IniDataSourceFormat : IDataSourceFormat
    {
        public JsonNode Convert(string input)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var iniFile = new IniFile(stream, applyBaseIni: false);

            var result = new JsonObject();
            foreach (var sectionName in iniFile.GetSections())
            {
                var section = iniFile.GetSection(sectionName);
                var sectionObj = new JsonObject();
                foreach (var key in section.Keys)
                {
                    string value = section.GetStringValue(key.Key, string.Empty);
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                        sectionObj[key.Key] = JsonValue.Create(intValue);
                    else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
                        sectionObj[key.Key] = JsonValue.Create(doubleValue);
                    else
                        sectionObj[key.Key] = JsonValue.Create(value);
                }
                result[section.SectionName] = sectionObj;
            }
            return result;
        }
    }

    private class DataSource
    {
        // Per-request timeouts are applied with a CancellationToken in DownloadWithTimeout.
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        private readonly string _id;
        private readonly string _url;
        private readonly int _refreshIntervalSeconds;
        private readonly int _timeoutSeconds;
        private readonly IDataSourceFormat _format;
        private readonly PauseGate _pauseGate;
        private readonly List<Action<JsonNode, bool>> _subscribers = new List<Action<JsonNode, bool>>();
        private CancellationTokenSource _cts;
        private volatile JsonNode _lastData;

        public DataSource(string id, string url, int refreshIntervalSeconds, int timeoutSeconds, IDataSourceFormat format, PauseGate pauseGate)
        {
            _id = id;
            _url = url;
            _refreshIntervalSeconds = refreshIntervalSeconds;
            _timeoutSeconds = timeoutSeconds;
            _format = format;
            _pauseGate = pauseGate;
        }

        public void Subscribe(Action<JsonNode, bool> callback)
        {
            lock (_subscribers)
            {
                if (_subscribers.Contains(callback))
                    return;

                _subscribers.Add(callback);

                if (_cts == null)
                    StartFetching();
            }

            JsonNode cached = _lastData;
            if (cached != null)
                InvokeSubscriber(callback, cached, false);
        }

        public void Unsubscribe(Action<JsonNode, bool> callback)
        {
            lock (_subscribers)
            {
                _subscribers.Remove(callback);

                if (_subscribers.Count == 0)
                    StopFetching();
            }
        }

        private void StartFetching()
        {
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            Task.Run(() => FetchLoopAsync(token));
        }

        // A loop that is still finishing a request exits on its own once it sees the cancellation,
        // so a new loop can be started straight away if a label subscribes again.
        private void StopFetching()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task FetchLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _pauseGate.WaitWhilePausedAsync(token).ConfigureAwait(false);
                    await FetchAndNotifyAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log($"JSONDataSourceManager: Error fetching '{_id}': {ex.Message}");
                    NotifyError();
                }

                if (_refreshIntervalSeconds <= 0)
                    break;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_refreshIntervalSeconds), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task FetchAndNotifyAsync(CancellationToken token)
        {
            string data = await DownloadWithTimeout(_url, _timeoutSeconds, token).ConfigureAwait(false);

            if (data == null)
            {
                NotifyError();
                return;
            }

            JsonNode parsed;
            try
            {
                parsed = _format.Convert(data);
            }
            catch (Exception ex)
            {
                Logger.Log($"JSONDataSourceManager: Error parsing data for '{_id}': {ex.Message}");
                NotifyError();
                return;
            }

            _lastData = parsed;
            NotifySubscribers(parsed, false);
        }

        /// <summary>
        /// Reports a failed fetch. Once data has been received, a failed refresh keeps
        /// showing the last good data instead of replacing it with the fallback text.
        /// </summary>
        private void NotifyError()
        {
            if (_lastData == null)
                NotifySubscribers(null, true);
        }

        private void NotifySubscribers(JsonNode data, bool isError)
        {
            // Call subscribers outside the lock so a slow callback can't block Subscribe/Unsubscribe.
            Action<JsonNode, bool>[] subscribers;
            lock (_subscribers)
                subscribers = _subscribers.ToArray();

            foreach (var subscriber in subscribers)
                InvokeSubscriber(subscriber, data, isError);
        }

        private void InvokeSubscriber(Action<JsonNode, bool> subscriber, JsonNode data, bool isError)
        {
            try
            {
                subscriber(data, isError);
            }
            catch (Exception ex)
            {
                Logger.Log($"JSONDataSourceManager: Error notifying subscriber for '{_id}': {ex.Message}");
            }
        }

        /// <summary>
        /// Downloads the URL as a string, or returns null on timeout or error.
        /// Throws <see cref="OperationCanceledException"/> if <paramref name="token"/> is cancelled.
        /// </summary>
        private async Task<string> DownloadWithTimeout(string url, int timeoutSeconds, CancellationToken token)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                // GetAsync buffers the whole body by default, so the timeout covers the full download.
                using var response = await _httpClient.GetAsync(url, cts.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                Logger.Log($"JSONDataSourceManager: Timeout for '{_id}' ({url})");
                return null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.Log($"JSONDataSourceManager: Download error for '{_id}': {ex.Message}");
                return null;
            }
        }
    }
}
