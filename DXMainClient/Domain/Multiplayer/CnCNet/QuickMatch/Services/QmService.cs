using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using ClientCore;
using ClientCore.Exceptions;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using DTAClient.Online;
using DTAClient.Services;
using JWT;
using JWT.Algorithms;
using JWT.Exceptions;
using JWT.Serializers;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;

public class QmService : IDisposable
{
    public const string QmVersion = "2.0";

    private readonly QmUserSettingsService qmUserSettingsService;
    private readonly ApiService apiService;
    private readonly SpawnService spawnService;
    private readonly QmSettingsService qmSettingsService;

    private readonly QmUserSettings qmUserSettings;
    private readonly QmSettings qmSettings;
    private readonly QmData qmData;

    private readonly Timer retryRequestmatchTimer;
    private QmUserAccount userAccount;
    private IEnumerable<int> mapSides;

    public QmService(
        QmSettingsService qmSettingsService,
        QmUserSettingsService qmUserSettingsService,
        ApiService apiService,
        SpawnService spawnService
    )
    {
        this.qmUserSettingsService = qmUserSettingsService;
        this.apiService = apiService;
        this.spawnService = spawnService;
        this.qmSettingsService = qmSettingsService;

        qmUserSettings = this.qmUserSettingsService.GetSettings();
        qmSettings = this.qmSettingsService.GetSettings();
        qmData = new QmData();

        retryRequestmatchTimer = new Timer();
        retryRequestmatchTimer.AutoReset = false;
        retryRequestmatchTimer.Elapsed += (_, _) => RetryRequestMatchAsync();
    }

    public event EventHandler<QmEvent> QmEvent;

    public IEnumerable<QmUserAccount> GetUserAccounts() => qmData?.UserAccounts;

    public QmLadder GetLadderForId(int ladderId) => qmData.Ladders.FirstOrDefault(l => l.Id == ladderId);

    public string GetCachedEmail() => qmUserSettings.Email;

    public string GetCachedLadder() => qmUserSettings.Ladder;

    /// <summary>
    /// Login process to cncnet.
    /// </summary>
    /// <param name="email">The email for login.</param>
    /// <param name="password">The password for login.</param>
    public void LoginAsync(string email, string password) =>
        ExecuteLoginRequest(async () =>
        {
            QmResponse<QmAuthData> response = await apiService.LoginAsync(email, password);
            return FinishLogin(response, email);
        });

    /// <summary>
    /// Attempts to refresh an existing auth tokenl.
    /// </summary>
    public void RefreshAsync() =>
        ExecuteLoginRequest(async () =>
        {
            QmResponse<QmAuthData> response = await apiService.RefreshAsync();
            return FinishLogin(response);
        });

    /// <summary>
    /// Simply clear all auth data from our settings.
    /// </summary>
    public void Logout()
    {
        ClearAuthData();
        QmEvent?.Invoke(this, new QmLogoutEvent());
    }

    public bool HasToken()
    {
        if (qmUserSettings.AuthData == null)
            return false;

        try
        {
            DecodeToken(qmUserSettings.AuthData.Token);
        }
        catch (TokenExpiredException)
        {
            Logger.Log(QmStrings.TokenExpiredError);
            return false;
        }
        catch (Exception e)
        {
            Logger.Log(e.ToString());
            return false;
        }

        apiService.SetToken(qmUserSettings.AuthData.Token);

        return true;
    }

    public void SetUserAccount(QmUserAccount userAccount)
    {
        string laddAbbr = userAccount?.Ladder?.Abbreviation;
        this.userAccount = userAccount;
        qmUserSettings.Ladder = laddAbbr;
        qmUserSettingsService.SaveSettings();
        QmEvent?.Invoke(this, new QmUserAccountSelectedEvent(userAccount));
    }

    public void SetMasterSide(QmSide side)
    {
        qmUserSettings.SideId = side?.LocalId ?? -1;
        qmUserSettingsService.SaveSettings();
        QmEvent?.Invoke(this, new QmMasterSideSelected(side));
    }

    public void SetMapSides(IEnumerable<int> mapSides)
    {
        this.mapSides = mapSides;
    }

    public void LoadLaddersAndUserAccountsAsync() =>
        ExecuteLoadingRequest(new QmLoadingLaddersAndUserAccountsEvent(), async () =>
        {
            Task<QmResponse<IEnumerable<QmLadder>>> loadLaddersTask = apiService.LoadLaddersAsync();
            Task<QmResponse<IEnumerable<QmUserAccount>>> loadUserAccountsTask = apiService.LoadUserAccountsAsync();

            await Task.WhenAll(loadLaddersTask, loadUserAccountsTask);

            QmResponse<IEnumerable<QmLadder>> loadLaddersResponse = loadLaddersTask.Result;
            if (!loadLaddersResponse.IsSuccess)
            {
                QmEvent?.Invoke(this, new QmErrorMessageEvent(string.Format(QmStrings.LoadingUserAccountsErrorFormat, loadLaddersResponse.ReasonPhrase)));
                return;
            }

            QmResponse<IEnumerable<QmUserAccount>> loadUserAccountsReponse = loadUserAccountsTask.Result;
            if (!loadUserAccountsReponse.IsSuccess)
            {
                QmEvent?.Invoke(this, new QmErrorMessageEvent(string.Format(QmStrings.LoadingUserAccountsErrorFormat, loadUserAccountsReponse.ReasonPhrase)));
                return;
            }

            qmData.Ladders = loadLaddersTask.Result.Data.ToList();
            qmData.UserAccounts = loadUserAccountsTask.Result.Data
                .Where(ua => qmSettings.AllowedLadders.Contains(ua.Ladder.Game))
                .GroupBy(ua => ua.Id) // remove possible duplicates
                .Select(g => g.First())
                .OrderBy(ua => ua.Ladder.Name)
                .ToList();

            if (!qmData.Ladders.Any())
            {
                QmEvent?.Invoke(this, new QmErrorMessageEvent(QmStrings.NoLaddersFoundError));
                return;
            }

            if (!qmData.UserAccounts.Any())
            {
                QmEvent?.Invoke(this, new QmErrorMessageEvent(QmStrings.NoUserAccountsFoundError));
                return;
            }

            QmEvent?.Invoke(this, new QmLaddersAndUserAccountsEvent(qmData.Ladders, qmData.UserAccounts));
        });

    public void LoadLadderMapsForAbbrAsync(string ladderAbbr) =>
        ExecuteLoadingRequest(new QmLoadingLadderMapsEvent(), async () =>
        {
            QmResponse<IEnumerable<QmLadderMap>> ladderMapsResponse = await apiService.LoadLadderMapsForAbbrAsync(ladderAbbr);
            if (!ladderMapsResponse.IsSuccess)
            {
                QmEvent?.Invoke(this, new QmErrorMessageEvent(string.Format(QmStrings.LoadingLadderMapsErrorFormat, ladderMapsResponse.ReasonPhrase)));
                return;
            }

            QmEvent?.Invoke(this, new QmLadderMapsEvent(ladderMapsResponse.Data));
        });

    public void LoadLadderStatsForAbbrAsync(string ladderAbbr) =>
        ExecuteLoadingRequest(new QmLoadingLadderStatsEvent(), async () =>
        {
            QmResponse<QmLadderStats> ladderStatsResponse = await apiService.LoadLadderStatsForAbbrAsync(ladderAbbr);

            if (!ladderStatsResponse.IsSuccess)
            {
                QmEvent?.Invoke(this, new QmErrorMessageEvent(string.Format(QmStrings.LoadingLadderStatsErrorFormat, ladderStatsResponse.ReasonPhrase)));
                return;
            }

            QmEvent?.Invoke(this, new QmLadderStatsEvent(ladderStatsResponse.Data));
        });

    /// <summary>
    /// This is called when the user clicks the button to begin searching for a match.
    /// </summary>
    public void RequestMatchAsync() =>
        ExecuteLoadingRequest(new QmRequestingMatchEvent(CancelRequestMatchAsync), async () =>
        {
            QmResponse<QmResponseMessage> response = await apiService.QuickMatchRequestAsync(userAccount.Ladder.Abbreviation, userAccount.Username, GetMatchRequest());
            HandleQuickMatchResponse(response);
        });

    /// <summary>
    /// This is called when the user clicks the "I'm Ready" button in the match found dialog.
    /// </summary>
    /// <param name="spawn">Spawn settings from the API.</param>
    public void AcceptMatchAsync(QmSpawn spawn)
    {
        ExecuteLoadingRequest(new QmReadyRequestMatchEvent(), async () =>
        {
            spawnService.WriteSpawnInfo(spawn);
            retryRequestmatchTimer.Stop();
            var readyRequest = new QmReadyRequest(spawn.Settings.Seed);
            QmResponse<QmResponseMessage> response = await apiService.QuickMatchRequestAsync(userAccount.Ladder.Abbreviation, userAccount.Username, readyRequest);
            HandleQuickMatchResponse(response);
        });
    }

    /// <summary>
    /// This is called when the user clicks the "Cancel" button in the match found dialog.
    /// </summary>
    /// <param name="spawn">Spawn settings from the API.</param>
    public void RejectMatchAsync(QmSpawn spawn)
    {
        ExecuteLoadingRequest(new QmNotReadyRequestMatchEvent(), async () =>
        {
            retryRequestmatchTimer.Stop();
            var notReadyRequest = new QmNotReadyRequest(spawn.Settings.Seed);
            QmResponse<QmResponseMessage> response = await apiService.QuickMatchRequestAsync(userAccount.Ladder.Abbreviation, userAccount.Username, notReadyRequest);
            HandleQuickMatchResponse(response);
        });
        CancelRequestMatchAsync();
    }

    public void Dispose()
    {
        apiService.Dispose();
    }

    private QmMatchRequest GetMatchRequest()
    {
        if (userAccount == null)
            throw new ClientException("No user account selected");

        if (userAccount.Ladder == null)
            throw new ClientException("No user account ladder selected");

        if (!qmUserSettings.SideId.HasValue)
            throw new ClientException("No side selected");

        var fileHashCalculator = new FileHashCalculator();

        return new QmMatchRequest()
        {
            IPv6Address = string.Empty,
            IPAddress = "98.111.198.94",
            IPPort = 51144,
            LanIP = "192.168.86.200",
            LanPort = 51144,
            Side = qmUserSettings.SideId.Value,
            MapSides = mapSides,
            ExeHash = fileHashCalculator.GetCompleteHash()
        };
    }

    private void RetryRequestMatchAsync() =>
        RequestMatchAsync();

    private void HandleQuickMatchResponse(QmResponse<QmResponseMessage> qmResponse)
    {
        switch (true)
        {
            case true when qmResponse.Data is QmWaitResponse waitResponse:
                retryRequestmatchTimer.Interval = waitResponse.CheckBack * 1000;
                retryRequestmatchTimer.Start();
                break;
        }

        QmEvent?.Invoke(this, new QmResponseEvent(qmResponse));
    }

    /// <summary>
    /// We only need to verify the expiration date of the token so that we can refresh or request a new one if it is expired.
    /// We do not need to worry about the signature. The API will handle that validation when the token is used.
    /// </summary>
    /// <param name="token">The token to be decoded.</param>
    private static void DecodeToken(string token)
    {
        IJsonSerializer serializer = new JsonNetSerializer();
        IDateTimeProvider provider = new UtcDateTimeProvider();
        ValidationParameters validationParameters = ValidationParameters.Default;
        validationParameters.ValidateSignature = false;
        IJwtValidator validator = new JwtValidator(serializer, provider, validationParameters);
        IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
        IJwtAlgorithm algorithm = new HMACSHA256Algorithm(); // symmetric
        IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);

        decoder.Decode(token, "nokey", verify: true);
    }

    private void ClearAuthData()
    {
        qmUserSettingsService.ClearAuthData();
        qmUserSettingsService.SaveSettings();
        apiService.SetToken(null);
    }

    private void CancelRequestMatchAsync() =>
        ExecuteLoadingRequest(new QmCancelingRequestMatchEvent(), async () =>
        {
            retryRequestmatchTimer.Stop();
            QmResponse<QmResponseMessage> response = await apiService.QuickMatchRequestAsync(userAccount.Ladder.Abbreviation, userAccount.Username, new QmQuitRequest());
            QmEvent?.Invoke(this, new QmResponseEvent(response));
        });

    private void ExecuteLoginRequest(Func<Task<bool>> loginFunction) =>
        ExecuteLoadingRequest(new QmLoggingInEvent(), async () =>
        {
            if (await loginFunction())
                QmEvent?.Invoke(this, new QmLoginEvent());
        });

    private void ExecuteLoadingRequest(QmEvent qmLoadingEvent, Func<Task> requestAction)
    {
        QmEvent?.Invoke(this, qmLoadingEvent);
        Task.Run(async () =>
        {
            try
            {
                await requestAction();
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                QmEvent?.Invoke(this, new QmErrorMessageEvent((e as ClientException)?.Message ?? QmStrings.UnknownError));
            }
        });
    }

    private bool FinishLogin(QmResponse<QmAuthData> response, string email = null)
    {
        if (!response.IsSuccess)
        {
            HandleFailedLogin(response);
            return false;
        }

        qmUserSettings.AuthData = response.Data;
        qmUserSettings.Email = email ?? qmUserSettings.Email;
        qmUserSettingsService.SaveSettings();

        apiService.SetToken(response.Data.Token);
        return true;
    }

    private void HandleFailedLogin(QmResponse<QmAuthData> response)
    {
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
                var responseBody = response.ReadContentAsStringAsync().Result;
                message = string.Format(QmStrings.LoggingInUnknownErrorFormat, response.ReasonPhrase, responseBody);
                break;
        }

        QmEvent?.Invoke(this, new QmErrorMessageEvent(message ?? QmStrings.UnknownError));
    }
}