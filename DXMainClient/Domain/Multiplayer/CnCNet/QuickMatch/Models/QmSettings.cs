using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmSettings
{
    public const string DefaultBaseUrl = "https://ladder.cncnet.org";
    public const string DefaultLoginUrl = "/api/v1/auth/login";
    public const string DefaultRefreshUrl = "/api/v1/auth/refresh";
    public const string DefaultServerStatusUrl = "/api/v1/ping";
    public const string DefaultGetUserAccountsUrl = "/api/v1/user/account";
    public const string DefaultGetLaddersUrl = "/api/v1/ladder";
    public const string DefaultGetLadderMapsUrl = "/api/v1/qm/ladder/{0}/maps";
    public const string DefaultGetLadderStatsUrl = "/api/v1/qm/ladder/{0}/stats";
    public const string DefaultQuickMatchUrl = "/api/v1/qm/{0}/{1}";
    public const int DefaultMatchFoundWaitSeconds = 20;

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public string LoginUrl { get; set; } = DefaultLoginUrl;

    public string RefreshUrl { get; set; } = DefaultRefreshUrl;

    public string ServerStatusUrl { get; set; } = DefaultServerStatusUrl;

    public string GetUserAccountsUrl { get; set; } = DefaultGetUserAccountsUrl;

    public string GetLaddersUrl { get; set; } = DefaultGetLaddersUrl;

    public string GetLadderMapsUrlFormat { get; set; } = DefaultGetLadderMapsUrl;

    public string GetLadderStatsUrlFormat { get; set; } = DefaultGetLadderStatsUrl;

    public string QuickMatchUrlFormat { get; set; } = DefaultQuickMatchUrl;

    public string MatchFoundSoundFile { get; set; }

    public List<string> AllowedLadders { get; set; } = new();

    public int MatchFoundWaitSeconds { get; set; } = DefaultMatchFoundWaitSeconds;

    public IDictionary<string, Texture2D> HeaderLogos = new Dictionary<string, Texture2D>();

    public Texture2D GetLadderHeaderLogo(string ladder)
        => !HeaderLogos.ContainsKey(ladder) ? null : HeaderLogos[ladder];
}