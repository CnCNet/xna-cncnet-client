using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClientCore.Exceptions;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Newtonsoft.Json;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;

public class QmApiService : IDisposable
{
    private static bool useMockService = true;
    private HttpClient _httpClient;
    private readonly QmSettings qmSettings;
    private string _token;

    private static QmApiService instance;

    protected QmApiService()
    {
        qmSettings = QmSettingsService.GetInstance().GetSettings();
    }

    public static QmApiService GetInstance() => instance ??= useMockService ? new QmMockApiService() : new QmApiService();

    public void SetToken(string token)
    {
        _token = token;
        HttpClient httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Clear();
        if (!string.IsNullOrEmpty(token))
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
    }

    public virtual async Task<IEnumerable<QmLadderMap>> LoadLadderMapsForAbbrAsync(string ladderAbbreviation)
    {
        HttpClient httpClient = GetHttpClient();
        string url = string.Format(qmSettings.GetLadderMapsUrlFormat, ladderAbbreviation);
        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new ClientException(string.Format(QmStrings.LoadingLadderMapsErrorFormat, response.ReasonPhrase));

        return JsonConvert.DeserializeObject<IEnumerable<QmLadderMap>>(await response.Content.ReadAsStringAsync());
    }

    public virtual async Task<QmLadderStats> LoadLadderStatsForAbbrAsync(string ladderAbbreviation)
    {
        HttpClient httpClient = GetHttpClient();
        string url = string.Format(qmSettings.GetLadderStatsUrlFormat, ladderAbbreviation);
        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new ClientException(string.Format(QmStrings.LoadingLadderStatsErrorFormat, response.ReasonPhrase));

        return JsonConvert.DeserializeObject<QmLadderStats>(await response.Content.ReadAsStringAsync());
    }

    public virtual async Task<IEnumerable<QmUserAccount>> LoadUserAccountsAsync()
    {
        HttpClient httpClient = GetHttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(qmSettings.GetUserAccountsUrl);
        if (!response.IsSuccessStatusCode)
            throw new ClientException(string.Format(QmStrings.LoadingUserAccountsErrorFormat, response.ReasonPhrase));

        return JsonConvert.DeserializeObject<IEnumerable<QmUserAccount>>(await response.Content.ReadAsStringAsync());
    }

    public virtual async Task<IEnumerable<QmLadder>> LoadLaddersAsync()
    {
        HttpClient httpClient = GetHttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(qmSettings.GetLaddersUrl);
        if (!response.IsSuccessStatusCode)
            throw new ClientException(string.Format(QmStrings.LoadingLaddersErrorFormat, response.ReasonPhrase));

        return JsonConvert.DeserializeObject<IEnumerable<QmLadder>>(await response.Content.ReadAsStringAsync());
    }

    public virtual async Task<QmAuthData> LoginAsync(string email, string password)
    {
        HttpClient httpClient = GetHttpClient();
        var postBodyContent = new StringContent(JsonConvert.SerializeObject(new QMLoginRequest() { Email = email, Password = password }), Encoding.Default, "application/json");
        var response = await httpClient.PostAsync(qmSettings.LoginUrl, postBodyContent);

        return await HandleLoginResponse(response, QmStrings.LoggingInUnknownErrorFormat);
    }

    public virtual async Task<QmAuthData> RefreshAsync()
    {
        HttpClient httpClient = GetHttpClient();
        HttpResponseMessage response = await httpClient.GetAsync(qmSettings.RefreshUrl);

        return await HandleLoginResponse(response, "Error refreshing token: {0}, {1}");
    }

    private async Task<QmAuthData> HandleLoginResponse(HttpResponseMessage response, string unknownErrorFormat)
    {
        if (!response.IsSuccessStatusCode)
            return await HandleFailedLoginResponse(response, unknownErrorFormat);

        string responseBody = await response.Content.ReadAsStringAsync();
        QmAuthData authData = JsonConvert.DeserializeObject<QmAuthData>(responseBody);
        if (authData == null)
            throw new ClientException(responseBody);

        return authData;
    }

    private async Task<QmAuthData> HandleFailedLoginResponse(HttpResponseMessage response, string unknownErrorFormat)
    {
        string responseBody = await response.Content.ReadAsStringAsync();
        string message;
        switch (response.StatusCode)
        {
            case HttpStatusCode.BadGateway:
                message = QmStrings.ServerUnreachableError;
                break;
            case HttpStatusCode.Unauthorized:
                message = QmStrings.InvalidUsernamePasswordError;
                break;
            default:
                message = string.Format(unknownErrorFormat, response.ReasonPhrase, responseBody);
                break;
        }

        throw new ClientRequestException(message, response.StatusCode);
    }

    public virtual bool IsServerAvailable()
    {
        HttpClient httpClient = GetHttpClient();
        HttpResponseMessage response = httpClient.GetAsync(qmSettings.ServerStatusUrl).Result;
        return response.IsSuccessStatusCode;
    }

    private HttpClient GetHttpClient() =>
        _httpClient ??= new HttpClient { BaseAddress = new Uri(qmSettings.BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

    public virtual async Task<QmRequestResponse> QuickMatchRequestAsync(string ladder, string playerName, QmRequest qmRequest)
    {
        HttpClient httpClient = GetHttpClient();
        string url = string.Format(qmSettings.QuickMatchUrlFormat, ladder, playerName);
        HttpResponseMessage response = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(qmRequest), Encoding.Default, "application/json"));

        string responseBody = await response.Content.ReadAsStringAsync();
        Logger.Log(responseBody);

        if (!response.IsSuccessStatusCode)
            throw new ClientException(string.Format(QmStrings.RequestingMatchErrorFormat, response.ReasonPhrase));

        QmRequestResponse matchRequestResponse = JsonConvert.DeserializeObject<QmRequestResponse>(responseBody);
        if (!(matchRequestResponse?.IsSuccessful ?? false))
            throw new ClientException(string.Format(QmStrings.RequestingMatchErrorFormat, matchRequestResponse?.Message ?? matchRequestResponse?.Description ?? "unknown"));

        return matchRequestResponse;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}