using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ClientCore.Launch;
using ClientCore.Launch.Spawner;

using Rampastring.Tools;

using Xunit;

namespace ClientCore.Tests.Launch;

public class SavedMatchStoreTests
{
    private static readonly SpawnIniDialect tiberianSun = SpawnIniDialect.ForClientType(ClientType.TS);

    public static IEnumerable<object[]> Scenarios => MatchScenarios.All.Keys.Select(name => new object[] { name });

    private static IniFile ReadGolden(string scenario)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Launch", "Golden", scenario + ".ini");
        using var stream = new MemoryStream(File.ReadAllBytes(path));

        return new IniFile(stream, new UTF8Encoding(false));
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void StoredRequestRegeneratesTheSameSpawnIni(string scenario)
    {
        MatchLaunchRequest original = MatchScenarios.All[scenario]();

        string json = SavedMatchStore.Serialize(original);
        MatchLaunchRequest? restored = SavedMatchStore.Deserialize(json);

        Assert.NotNull(restored);
        Assert.Equal(GoldenIni.Render(SpawnIniWriter.WriteMatch(original, tiberianSun)),
            GoldenIni.Render(SpawnIniWriter.WriteMatch(restored, tiberianSun)));
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void InfoMatchesTheMatchSpawnIni(string scenario)
    {
        var fromRequest = new SavedMatchInfo(SpawnIniWriter.WriteMatch(MatchScenarios.All[scenario](), tiberianSun));
        var fromSpawnIni = new SavedMatchInfo(ReadGolden(scenario));

        Assert.Equal(fromSpawnIni.GetGameID("0"), fromRequest.GetGameID("0"));
        Assert.Equal(fromSpawnIni.GetGameIDNumber(-1), fromRequest.GetGameIDNumber(-1));
        Assert.Equal(fromSpawnIni.GetPlayerCount(0), fromRequest.GetPlayerCount(0));
        Assert.Equal(fromSpawnIni.LocalPlayerName, fromRequest.LocalPlayerName);
        Assert.Equal(fromSpawnIni.LocalPlayerColor, fromRequest.LocalPlayerColor);
        Assert.Equal(fromSpawnIni.IsHost, fromRequest.IsHost);
        Assert.Equal(fromSpawnIni.UsedTunnel, fromRequest.UsedTunnel);
        Assert.Equal(fromSpawnIni.MapSHA1, fromRequest.MapSHA1);
        Assert.Equal(fromSpawnIni.BroadcastedGameOptionValues, fromRequest.BroadcastedGameOptionValues);
        Assert.Equal(fromSpawnIni.UIMapName, fromRequest.UIMapName);
        Assert.Equal(fromSpawnIni.MapID, fromRequest.MapID);
        Assert.Equal(fromSpawnIni.UIGameMode, fromRequest.UIGameMode);
        Assert.Equal(fromSpawnIni.GetOtherPlayerName(1, "?"), fromRequest.GetOtherPlayerName(1, "?"));
        Assert.Equal(fromSpawnIni.GetOtherPlayerColor(1), fromRequest.GetOtherPlayerColor(1));
    }

    [Fact]
    public void SessionTypeSurvivesTheRoundTrip()
    {
        string json = SavedMatchStore.Serialize(MatchScenarios.CnCNetSpectatorHost());
        MatchLaunchRequest? restored = SavedMatchStore.Deserialize(json);

        TunnelMatchSession session = Assert.IsType<TunnelMatchSession>(restored?.Network?.Session);
        Assert.Equal(new TunnelEndpoint("127.0.0.1", 50000), session.Tunnel);
    }

    [Fact]
    public void MapIsNotStored()
    {
        MatchLaunchRequest original = MatchScenarios.SkirmishWithTeams();
        var withMap = new MatchLaunchRequest
        {
            SpawnMap = new IniFile(),
            LocalPlayerName = original.LocalPlayerName,
            Humans = original.Humans,
        };

        MatchLaunchRequest? restored = SavedMatchStore.Deserialize(SavedMatchStore.Serialize(withMap));

        Assert.Null(restored?.SpawnMap);
    }

    [Fact]
    public void UnknownSchemaVersionIsIgnored()
    {
        string json = SavedMatchStore.Serialize(MatchScenarios.SkirmishWithTeams())
            .Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 99");

        Assert.Null(SavedMatchStore.Deserialize(json));
    }
}
