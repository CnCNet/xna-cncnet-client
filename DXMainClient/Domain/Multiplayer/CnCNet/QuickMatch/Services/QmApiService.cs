using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;

public class QmApiService : IDisposable
{
    private static bool useMockService = false;
    private static QmApiService instance;
    private readonly QmSettings qmSettings;
    private QmHttpClient _httpClient;
    private string token;

    protected QmApiService()
    {
        qmSettings = QmSettingsService.GetInstance().GetSettings();
    }

    public static QmApiService GetInstance() => instance ??= useMockService ? new QmMockApiService() : new QmApiService();

    public void SetToken(string token)
    {
        this.token = token;
        HttpClient httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Clear();
        if (!string.IsNullOrEmpty(token))
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {this.token}");
    }

    public virtual async Task<QmResponse<IEnumerable<QmLadderMap>>> LoadLadderMapsForAbbrAsync(string ladderAbbreviation)
        => await GetAsync<IEnumerable<QmLadderMap>>(string.Format(qmSettings.GetLadderMapsUrlFormat, ladderAbbreviation));

    public virtual async Task<QmResponse<QmLadderStats>> LoadLadderStatsForAbbrAsync(string ladderAbbreviation)
        => await GetAsync<QmLadderStats>(string.Format(qmSettings.GetLadderStatsUrlFormat, ladderAbbreviation));

    public virtual async Task<QmResponse<IEnumerable<QmUserAccount>>> LoadUserAccountsAsync()
        => await GetAsync<IEnumerable<QmUserAccount>>(qmSettings.GetUserAccountsUrl);

    public virtual async Task<QmResponse<IEnumerable<QmLadder>>> LoadLaddersAsync()
        => await GetAsync<IEnumerable<QmLadder>>(qmSettings.GetLaddersUrl);

    public virtual async Task<QmResponse<QmAuthData>> LoginAsync(string email, string password)
        => await PostAsync<QmAuthData>(qmSettings.LoginUrl, new QMLoginRequest { Email = email, Password = password });

    public virtual async Task<QmResponse<QmAuthData>> RefreshAsync()
        => await GetAsync<QmAuthData>(qmSettings.RefreshUrl);

    public virtual async Task<QmResponse<bool>> IsServerAvailable()
        => await GetAsync(new QmRequest { Url = qmSettings.ServerStatusUrl }, true, false);

    public virtual async Task<QmResponse<QmResponseMessage>> QuickMatchRequestAsync(string ladder, string playerName, QmRequest qmRequest)
    {
        qmRequest.Url = string.Format(qmSettings.QuickMatchUrlFormat, ladder, playerName);
        return await PostAsync<QmResponseMessage>(qmRequest, qmRequest);
    }

    private QmHttpClient GetHttpClient() =>
        _httpClient ??= new QmHttpClient { BaseAddress = new Uri(qmSettings.BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

    private async Task<QmResponse<T>> GetAsync<T>(string url)
        => await GetAsync<T>(new QmRequest { Url = url });

    private async Task<QmResponse<T>> GetAsync<T>(string url, T successDataValue, T failedDataValue)
        => await GetAsync(new QmRequest { Url = url }, successDataValue, failedDataValue);

    private async Task<QmResponse<T>> GetAsync<T>(QmRequest qmRequest)
    {
        QmHttpClient httpClient = GetHttpClient();
        return await httpClient.GetAsync<T>(qmRequest);
    }

    private async Task<QmResponse<T>> GetAsync<T>(QmRequest qmRequest, T successDataValue, T failedDataValue)
    {
        QmHttpClient httpClient = GetHttpClient();
        return await httpClient.GetAsync(qmRequest, successDataValue, failedDataValue);
    }

    private async Task<QmResponse<T>> PostAsync<T>(string url, object data)
        => await PostAsync<T>(new QmRequest { Url = url }, data);

    private async Task<QmResponse<T>> PostAsync<T>(QmRequest qmRequest, object data)
    {
        QmHttpClient httpClient = GetHttpClient();
        return await httpClient.PostAsync<T>(qmRequest, data);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}