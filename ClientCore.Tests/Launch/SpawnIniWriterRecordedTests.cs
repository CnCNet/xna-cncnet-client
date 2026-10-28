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

    private static IniFile SavedSpawnIni() => Parse(
        "[Settings]", "Name=Alice", "Seed=5", "GameID=99", "Port=1234", "Host=True", "",
        "[Other1]", "Name=Bob", "Ip=1.1.1.1", "Port=1234", "",
        "[Other2]", "Name=Carol", "Ip=2.2.2.2", "Port=1234", "",
        "[Tunnel]", "Ip=9.9.9.9", "Port=50000");

    [Fact]
    public void SavedMatchUsesCurrentEndpoints()
    {
        IniFile recorded = SavedSpawnIni();
        string recordedBefore = GoldenIni.Render(recorded);

        var request = new SavedMatchLaunchRequest
        {
            RecordedSpawnIni = recorded,
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

        GoldenIni.AssertMatches("SavedMatchUsesCurrentEndpoints", SpawnIniWriter.WriteSavedMatch(request));
        Assert.Equal(recordedBefore, GoldenIni.Render(recorded));
    }

    [Fact]
    public void SavedMatchOnlyPatchesAsManyRecordedPlayersAsAreInTheLobby()
    {
        var request = new SavedMatchLaunchRequest
        {
            RecordedSpawnIni = SavedSpawnIni(),
            SaveGameName = "SVGM_000.NET",
            LocalPort = 48000,
            Players = [new PlayerEndpoint("Alice", string.Empty, 48000), new PlayerEndpoint("Carol", "3.3.3.3", 47998)],
        };

        IniFile spawnIni = SpawnIniWriter.WriteSavedMatch(request);

        // Only [Other1] is looked at, and Bob isn't in the lobby, so both keep their recorded endpoints.
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
