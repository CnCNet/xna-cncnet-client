using System.IO;
using System.Text;

using ClientCore.Launch;
using ClientCore.Launch.Spawner;

using Rampastring.Tools;

using Xunit;

namespace ClientCore.Tests.Launch;

public class SpawnIniWriterRecordedTests
{
    private static IniFile Parse(params string[] lines)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\r\n", lines)));

        return new IniFile(stream, new UTF8Encoding(false), applyBaseIni: false);
    }

    private static readonly SpawnIniDialect tiberianSun = SpawnIniDialect.ForClientType(ClientType.TS);

    private static MatchLaunchRequest SavedMatch() => new()
    {
        LocalPlayerName = "Alice",
        Humans =
        [
            new HumanHouse { Name = "Alice", ColorIndex = 0, Port = 1234 },
            new HumanHouse { Name = "Bob", ColorIndex = 1, IpAddress = "1.1.1.1", Port = 1234 },
            new HumanHouse { Name = "Carol", ColorIndex = 2, IpAddress = "2.2.2.2", Port = 1234 },
        ],
        ColorOrder = [0, 1, 2, 3],
        Seed = 5,
        Network = new MatchNetwork
        {
            FrameSendRate = 7,
            Protocol = 2,
            Session = new TunnelMatchSession { GameID = 99, IsHost = true, Port = 1234, Tunnel = new TunnelEndpoint("9.9.9.9", 50000) },
        },
    };

    [Fact]
    public void SavedMatchUsesCurrentEndpoints()
    {
        var request = new SavedMatchLaunchRequest
        {
            Match = SavedMatch(),
            SaveGameName = "SVGM_002.NET",
            LocalPort = 48000,
            Players =
            [
                new PlayerEndpoint("Alice", string.Empty, 48000),
                new PlayerEndpoint("Carol", "3.3.3.3", 47998),
                new PlayerEndpoint("Bob", "4.4.4.4", 47999),
            ],
            Tunnel = new TunnelEndpoint("127.0.0.1", 51000),
        };

        IniFile spawnIni = SpawnIniWriter.WriteSavedMatch(request, tiberianSun);

        Assert.Equal("SVGM_002.NET", spawnIni.GetStringValue("Settings", "SaveGameName", string.Empty));
        Assert.True(spawnIni.GetBooleanValue("Settings", "LoadSaveGame", false));
        Assert.Equal(48000, spawnIni.GetIntValue("Settings", "Port", 0));
        Assert.Equal(5, spawnIni.GetIntValue("Settings", "Seed", 0));
        Assert.Equal("Bob", spawnIni.GetStringValue("Other1", "Name", string.Empty));
        Assert.Equal("4.4.4.4", spawnIni.GetStringValue("Other1", "Ip", string.Empty));
        Assert.Equal(47999, spawnIni.GetIntValue("Other1", "Port", 0));
        Assert.Equal("Carol", spawnIni.GetStringValue("Other2", "Name", string.Empty));
        Assert.Equal("3.3.3.3", spawnIni.GetStringValue("Other2", "Ip", string.Empty));
        Assert.Equal(47998, spawnIni.GetIntValue("Other2", "Port", 0));
        Assert.Equal("127.0.0.1", spawnIni.GetStringValue("Tunnel", "Ip", string.Empty));
        Assert.Equal(51000, spawnIni.GetIntValue("Tunnel", "Port", 0));
    }

    [Fact]
    public void SavedMatchOnlyPatchesAsManyRecordedPlayersAsAreInTheLobby()
    {
        var request = new SavedMatchLaunchRequest
        {
            Match = SavedMatch(),
            SaveGameName = "SVGM_000.NET",
            LocalPort = 48000,
            Players = [new PlayerEndpoint("Alice", string.Empty, 48000), new PlayerEndpoint("Carol", "3.3.3.3", 47998)],
        };

        IniFile spawnIni = SpawnIniWriter.WriteSavedMatch(request, tiberianSun);

        // Only [Other1] is looked at, and Bob isn't in the lobby, so both keep their saved endpoints.
        Assert.Equal("1.1.1.1", spawnIni.GetStringValue("Other1", "Ip", string.Empty));
        Assert.Equal("2.2.2.2", spawnIni.GetStringValue("Other2", "Ip", string.Empty));
        Assert.Equal("9.9.9.9", spawnIni.GetStringValue("Tunnel", "Ip", string.Empty));
    }

    [Fact]
    public void ReplayPlayback()
    {
        IniFile recorded = Parse(
            "[Settings]", "Name=Alice", "EnableReplayRecording=True", "ReplayFileOut=Replays/a.yrpl", "CustomLoadScreen=old.shp", "",
            "[ReplayFileHashes]", "0=a|1", "",
            "[Other1]", "Name=Bob");
        string recordedBefore = GoldenIni.Render(recorded);

        var request = new ReplayLaunchRequest
        {
            RecordedSpawnIni = recorded,
            ReplayFile = "Replays/a.yrpl",
            ViewPlayer = 1,
            CustomLoadScreen = "new.shp",
            PlaybackSpeed = 60,
            ShroudEnabled = true,
            FreeCamera = false,
            ShowSelections = true,
            Spectator = false,
            ShowChatAndBeacons = true,
            KeyframeInterval = 300,
            KeyframeStorageLimitMB = 512,
        };

        GoldenIni.AssertMatches("ReplayPlayback", SpawnIniWriter.WriteReplay(request));
        Assert.Equal(recordedBefore, GoldenIni.Render(recorded));
    }

    [Fact]
    public void ReplayKeepsRecordedLoadScreenWithoutPerspective()
    {
        IniFile recorded = Parse("[Settings]", "Name=Alice", "CustomLoadScreen=old.shp");

        IniFile spawnIni = SpawnIniWriter.WriteReplay(new ReplayLaunchRequest { RecordedSpawnIni = recorded, Spectator = true });

        Assert.Equal("old.shp", spawnIni.GetStringValue("Settings", "CustomLoadScreen", string.Empty));
        Assert.Equal("True", spawnIni.GetStringValue("Settings", "ReplaySpectator", string.Empty));
    }
}
