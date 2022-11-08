using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClientCore.Exceptions;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Newtonsoft.Json;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;

public class QmMockApiService : QmApiService
{
    public override async Task<IEnumerable<QmLadderMap>> LoadLadderMapsForAbbrAsync(string ladderAbbreviation) => LoadMockData<IEnumerable<QmLadderMap>>($"qm_ladder_maps_{ladderAbbreviation}_response.json");

    public override async Task<QmLadderStats> LoadLadderStatsForAbbrAsync(string ladderAbbreviation) => LoadMockData<QmLadderStats>("qm_ladder_stats_response.json");

    public override async Task<IEnumerable<QmUserAccount>> LoadUserAccountsAsync() => LoadMockData<IEnumerable<QmUserAccount>>("qm_user_accounts_response.json");

    public override async Task<IEnumerable<QmLadder>> LoadLaddersAsync() => LoadMockData<IEnumerable<QmLadder>>("qm_ladders_response.json");

    public override async Task<QmAuthData> LoginAsync(string email, string password) => LoadMockData<QmAuthData>("qm_login_response.json");

    public override async Task<QmAuthData> RefreshAsync() => LoadMockData<QmAuthData>("qm_login_response.json");

    public override async Task<QmRequestResponse> QuickMatchRequestAsync(string ladder, string playerName, QmRequest qmRequest)
    {
        const string responseType = QmResponseTypes.Wait;
        return responseType switch
        {
            QmResponseTypes.Error => LoadMockData<QmRequestResponse>("qm_find_match_spawn_response.json"),
            QmResponseTypes.Spawn => LoadMockData<QmRequestResponse>("qm_find_match_spawn_response.json"),
            QmResponseTypes.Wait => LoadMockData<QmRequestResponse>("qm_find_match_please_wait_response.json"),
            _ => throw new NotImplementedException("unknown mock response type")
        };
    }
    
    public override bool IsServerAvailable() => true;

    private T LoadMockData<T>(string mockDataFileName)
    {
        string content = File.ReadAllText($"MockData/QuickMatch/{mockDataFileName}");

        return JsonConvert.DeserializeObject<T>(content);
    }
}