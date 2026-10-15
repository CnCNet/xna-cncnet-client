using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using Rampastring.Tools;

namespace ClientCore;

/// <summary>
/// Manages shared JSON data sources for XNAClientWebLabel.
/// Data sources are defined in INI files under [DataSources] section.
/// </summary>
public class JSONDataSourceManager
{
    private readonly Dictionary<string, DataSource> _dataSources = new Dictionary<string, DataSource>();

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

            var dataSource = new DataSource(dataSourceName, url, refreshInterval, timeout, format);
            _dataSources.Add(dataSourceName, dataSource);
        }
    }

    /// <summary>
    /// Subscribes to a data source and receives JSON updates.
    /// </summary>
    /// <param name="dataSourceId">The ID of the data source</param>
    /// <param name="callback">Callback invoked when new data is available (jToken, isError)</param>
    /// <returns>True if subscription was successful, false if data source not found</returns>
    public bool Subscribe(string dataSourceId, Action<JToken, bool> callback)
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
    public void Unsubscribe(string dataSourceId, Action<JToken, bool> callback)
    {
        if (string.IsNullOrEmpty(dataSourceId))
            return;

        if (_dataSources.TryGetValue(dataSourceId, out var dataSource))
            dataSource.Unsubscribe(callback);
    }

    private interface IDataSourceFormat
    {
        JToken Convert(string input);
    }

    private class JsonDataSourceFormat : IDataSourceFormat
    {
        public JToken Convert(string input) => JToken.Parse(input);
    }

    private class IniDataSourceFormat : IDataSourceFormat
    {
        public JToken Convert(string input)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var iniFile = new IniFile(stream, applyBaseIni: false);

            var result = new JObject();
            foreach (var sectionName in iniFile.GetSections())
            {
                var section = iniFile.GetSection(sectionName);
                var sectionObj = new JObject();
                foreach (var key in section.Keys)
                {
                    string value = section.GetStringValue(key.Key, string.Empty);
                    if (int.TryParse(value, out int intValue))
                        sectionObj[key.Key] = intValue;
                    else if (double.TryParse(value, out double doubleValue))
                        sectionObj[key.Key] = doubleValue;
                    else
                        sectionObj[key.Key] = value;
                }
                result[section.SectionName] = sectionObj;
            }
            return result;
        }
    }

    private class DataSource
    {
        private readonly string _id;
        private readonly string _url;
        private readonly int _refreshIntervalSeconds;
        private readonly int _timeoutSeconds;
        private readonly IDataSourceFormat _format;
        private readonly List<Action<JToken, bool>> _subscribers = new List<Action<JToken, bool>>();
        private CancellationTokenSource _cts;
        private Task _fetchTask;
        private JToken _lastJToken;

        public DataSource(string id, string url, int refreshIntervalSeconds, int timeoutSeconds, IDataSourceFormat format)
        {
            _id = id;
            _url = url;
            _refreshIntervalSeconds = refreshIntervalSeconds;
            _timeoutSeconds = timeoutSeconds;
            _format = format;
        }

        public void Subscribe(Action<JToken, bool> callback)
        {
            lock (_subscribers)
            {
                if (!_subscribers.Contains(callback))
                {
                    _subscribers.Add(callback);

                    if (_lastJToken != null) // cached
                        callback(_lastJToken, false);

                    if (_subscribers.Count == 1 && _fetchTask == null)
                        StartFetching();
                }
            }
        }

        public void Unsubscribe(Action<JToken, bool> callback)
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
            _fetchTask = Task.Run(() => FetchLoopAsync(_cts.Token));
        }

        private void StopFetching()
        {
            _cts?.Cancel();
            _fetchTask = null;
        }

        private async Task FetchLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await FetchAndNotifyAsync(token);
                }
                catch (Exception ex)
                {
                    Logger.Log($"JSONDataSourceManager: Error fetching '{_id}': {ex.Message}");
                    NotifySubscribers(null, true);
                }

                if (_refreshIntervalSeconds <= 0)
                    break;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_refreshIntervalSeconds), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task FetchAndNotifyAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            string data = await DownloadWithTimeout(_url, _timeoutSeconds, token);

            if (data == null)
            {
                NotifySubscribers(null, true);
                return;
            }

            JToken jToken;
            try
            {
                jToken = _format.Convert(data);
            }
            catch (Exception ex)
            {
                Logger.Log($"JSONDataSourceManager: Error parsing data for '{_id}': {ex.Message}");
                NotifySubscribers(null, true);
                return;
            }

            _lastJToken = jToken;
            NotifySubscribers(jToken, false);
        }

        private void NotifySubscribers(JToken jToken, bool isError)
        {
            lock (_subscribers)
            {
                foreach (var subscriber in _subscribers)
                {
                    try
                    {
                        subscriber(jToken, isError);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"JSONDataSourceManager: Error notifying subscriber for '{_id}': {ex.Message}");
                    }
                }
            }
        }

        private async Task<string> DownloadWithTimeout(string url, int timeoutSeconds, CancellationToken token)
        {
            using (var cts = new CancellationTokenSource())
            using (var webClient = new WebClient())
            {
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cts.Token);
                var downloadTask = webClient.DownloadStringTaskAsync(url);

                var completed = await Task.WhenAny(downloadTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    Logger.Log($"JSONDataSourceManager: Timeout for '{_id}' ({url})");
                    return null;
                }

                cts.Cancel();

                if (token.IsCancellationRequested)
                    return null;

                try
                {
                    return await downloadTask;
                }
                catch (Exception ex)
                {
                    Logger.Log($"JSONDataSourceManager: Download error for '{_id}': {ex.Message}");
                    return null;
                }
            }
        }

    }
}