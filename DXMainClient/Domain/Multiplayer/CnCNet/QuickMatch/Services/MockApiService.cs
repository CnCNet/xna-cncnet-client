using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using DTAClient.Services;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;

public class MockApiService : ApiService
{
    public override async Task<QmResponse<IEnumerable<QmLadderMap>>> LoadLadderMapsForAbbrAsync(string ladderAbbreviation) => LoadMockData<QmResponse<IEnumerable<QmLadderMap>>>($"qm_ladder_maps_{ladderAbbreviation}_response.json");

    public override async Task<QmResponse<QmLadderStats>> LoadLadderStatsForAbbrAsync(string ladderAbbreviation) => LoadMockData<QmResponse<QmLadderStats>>("qm_ladder_stats_response.json");

    public override async Task<QmResponse<IEnumerable<QmUserAccount>>> LoadUserAccountsAsync() => LoadMockData<QmResponse<IEnumerable<QmUserAccount>>>("qm_user_accounts_response.json");

    public override async Task<QmResponse<IEnumerable<QmLadder>>> LoadLaddersAsync() => LoadMockData<QmResponse<IEnumerable<QmLadder>>>("qm_ladders_response.json");

    public override async Task<QmResponse<QmAuthData>> LoginAsync(string email, string password) => LoadMockData<QmResponse<QmAuthData>>("qm_login_response.json");

    public override async Task<QmResponse<QmAuthData>> RefreshAsync() => LoadMockData<QmResponse<QmAuthData>>("qm_login_response.json");

    public override async Task<QmResponse<QmResponseMessage>> QuickMatchRequestAsync(string ladder, string playerName, QmRequest qmRequest)
    {
        return true switch
        {
            true when qmRequest.Type == QmRequestTypes.Quit => LoadMockData<QmResponse<QmResponseMessage>>("qm_find_match_quit_response.json"),
            true when qmRequest.Type == QmRequestTypes.MatchMeUp => LoadMockData<QmResponse<QmResponseMessage>>("qm_find_match_spawn_response.json"),
            // true when qmRequest.Type ==QmRequestTypes.Update && updateRequest?.Status == QmUpdateRequestStatuses.Ready => LoadMockData<QmRequestResponse>("qm_find_match_please_wait_response.json"),
            _ => new QmResponse<QmResponseMessage> { Data = new QmUpdateResponse { Message = "default response" } }
        };
    }

    private static T LoadMockData<T>(string mockDataFileName)
    {
        string content = File.ReadAllText($"MockData/QuickMatch/{mockDataFileName}");

        return JsonConvert.DeserializeObject<T>(content);
    }
}