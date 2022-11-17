using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;

namespace DTAClient.Domain.Multiplayer.CnCNet.Services;

public class ApiService : IDisposable
{
    public readonly ApiSettings ApiSettings;
    private QmHttpClient _httpClient;
    private string token;

    public ApiService()
    {
        ApiSettings = ApiSettingsService.GetInstance().GetSettings();
    }

    public void SetToken(string token)
    {
        this.token = token;
        HttpClient httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Clear();
        if (!string.IsNullOrEmpty(token))
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {this.token}");
    }

    public virtual async Task<QmResponse<IEnumerable<QmLadderMap>>> LoadLadderMapsForAbbrAsync(string ladderAbbreviation)
        => await GetAsync<IEnumerable<QmLadderMap>>(string.Format(ApiSettings.GetLadderMapsUrlFormat, ladderAbbreviation));

    public virtual async Task<QmResponse<QmLadderStats>> LoadLadderStatsForAbbrAsync(string ladderAbbreviation)
        => await GetAsync<QmLadderStats>(string.Format(ApiSettings.GetLadderStatsUrlFormat, ladderAbbreviation));

    public virtual async Task<QmResponse<IEnumerable<QmUserAccount>>> LoadUserAccountsAsync()
        => await GetAsync<IEnumerable<QmUserAccount>>(ApiSettings.GetUserAccountsUrl);

    public virtual async Task<QmResponse<IEnumerable<QmLadder>>> LoadLaddersAsync()
        => await GetAsync<IEnumerable<QmLadder>>(ApiSettings.GetLaddersUrl);

    public virtual async Task<QmResponse<QmAuthData>> LoginAsync(string email, string password)
        => await PostAsync<QmAuthData>(ApiSettings.LoginUrl, new QMLoginRequest { Email = email, Password = password });

    public virtual async Task<QmResponse<QmAuthData>> RefreshAsync()
        => await GetAsync<QmAuthData>(ApiSettings.RefreshUrl);

    public virtual async Task<QmResponse<bool>> IsServerAvailable()
        => await GetAsync(new QmRequest { Url = ApiSettings.ServerStatusUrl }, true, false);

    public virtual async Task<QmResponse<QmResponseMessage>> QuickMatchRequestAsync(string ladder, string playerName, QmRequest qmRequest)
    {
        qmRequest.Url = string.Format(ApiSettings.QuickMatchUrlFormat, ladder, playerName);
        return await PostAsync<QmResponseMessage>(qmRequest, qmRequest);
    }

    private QmHttpClient GetHttpClient() =>
        _httpClient ??= new QmHttpClient { BaseAddress = new Uri(ApiSettings.BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

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