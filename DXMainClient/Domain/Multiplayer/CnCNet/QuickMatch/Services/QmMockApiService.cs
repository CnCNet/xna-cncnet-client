using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;

public class QmMockApiService : QmApiService
{
    public override async Task<IEnumerable<QmLadderMap>> LoadLadderMapsForAbbrAsync(string ladderAbbreviation) => LoadMockData<IEnumerable<QmLadderMap>>($"qm_ladder_maps_{ladderAbbreviation}_response.json");

    public override async Task<QmLadderStats> LoadLadderStatsForAbbrAsync(string ladderAbbreviation) => LoadMockData<QmLadderStats>("qm_ladder_stats_response.json");

    public override async Task<IEnumerable<QmUserAccount>> LoadUserAccountsAsync() => LoadMockData<IEnumerable<QmUserAccount>>("qm_user_accounts_response.json");

    public override async Task<IEnumerable<QmLadder>> LoadLaddersAsync() => LoadMockData<IEnumerable<QmLadder>>("qm_ladders_response.json");

    public override async Task<QmAuthData> LoginAsync(string email, string password) => LoadMockData<QmAuthData>("qm_login_response.json");

    public override async Task<QmAuthData> RefreshAsync() => LoadMockData<QmAuthData>("qm_login_response.json");

    public override async Task<QmResponse> QuickMatchRequestAsync(string ladder, string playerName, QmRequest qmRequest)
    {
        return true switch
        {
            true when qmRequest.Type == QmRequestTypes.Quit => LoadMockData<QmResponse>("qm_find_match_quit_response.json"),
            true when qmRequest.Type == QmRequestTypes.MatchMeUp => LoadMockData<QmResponse>("qm_find_match_spawn_response.json"),
            // true when qmRequest.Type ==QmRequestTypes.Update && updateRequest?.Status == QmUpdateRequestStatuses.Ready => LoadMockData<QmRequestResponse>("qm_find_match_please_wait_response.json"),
            _ => new QmUpdateResponse() { Message = "default response" }
        };
    }

    public override bool IsServerAvailable() => true;

    private static T LoadMockData<T>(string mockDataFileName)
    {
        string content = File.ReadAllText($"MockData/QuickMatch/{mockDataFileName}");

        return JsonConvert.DeserializeObject<T>(content);
    }
}