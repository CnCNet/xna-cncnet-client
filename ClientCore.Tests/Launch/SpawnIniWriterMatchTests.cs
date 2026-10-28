using System.Collections.Generic;
using System.Linq;

using ClientCore.Launch;
using ClientCore.Launch.Spawner;

using Rampastring.Tools;

using Xunit;

namespace ClientCore.Tests.Launch;

public class SpawnIniWriterMatchTests
{
    private static readonly SpawnIniDialect tiberianSun = SpawnIniDialect.ForClientType(ClientType.TS);
    private static readonly SpawnIniDialect redAlert = SpawnIniDialect.ForClientType(ClientType.RA);

    private static readonly int[] colorOrder = [0, 1, 2, 3, 4, 5, 6, 7];

    public static IEnumerable<object[]> GoldenScenarios => MatchScenarios.All.Keys.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(GoldenScenarios))]
    public void MatchesGoldenFile(string scenario)
        => GoldenIni.AssertMatches(scenario, SpawnIniWriter.WriteMatch(MatchScenarios.All[scenario](), tiberianSun));

    [Fact]
    public void EveryHumanGetsOneHouseWhenColorsShareAGameColor()
    {
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "Player",
            Humans = [new HumanHouse { Name = "Player", ColorIndex = 1, StartingWaypoint = 0, TeamId = 1 }],
            AIs = [new AIHouse { InternalSideIndex = 3, ColorIndex = 0, StartingWaypoint = 1, TeamId = 1 }],
            ColorOrder = [0, 1, 1, 2],
        };

        IniFile spawnIni = SpawnIniWriter.WriteMatch(request, tiberianSun);

        // The AI's house is Multi2 in every section.
        Assert.Equal(3, spawnIni.GetIntValue("HouseCountries", "Multi2", -1));
        Assert.Equal(1, spawnIni.GetIntValue("SpawnLocations", "Multi2", -1));
        Assert.Equal(0, spawnIni.GetIntValue("Multi2_Alliances", "HouseAllyOne", -1));
        Assert.False(spawnIni.KeyExists("HouseCountries", "Multi3"));
    }

    [Fact]
    public void HumanWithUnknownColorStillGetsAHouse()
    {
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "A",
            Humans =
            [
                new HumanHouse { Name = "A", ColorIndex = 99, StartingWaypoint = 0 },
                new HumanHouse { Name = "B", ColorIndex = 2, StartingWaypoint = 1 },
            ],
            ColorOrder = colorOrder,
        };

        IniFile spawnIni = SpawnIniWriter.WriteMatch(request, tiberianSun);

        Assert.Equal(1, spawnIni.GetIntValue("SpawnLocations", "Multi1", -1));
        Assert.Equal(0, spawnIni.GetIntValue("SpawnLocations", "Multi2", -1));
    }

    [Fact]
    public void RedAlertAllianceHouseIdsAreShifted()
    {
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "Player",
            Humans = [new HumanHouse { Name = "Player", InternalSideIndex = 0, ColorIndex = 0, StartingWaypoint = 0, TeamId = 1 }],
            AIs = [new AIHouse { InternalSideIndex = 1, ColorIndex = 1, StartingWaypoint = 1, TeamId = 1, HandicapLevel = 2 }],
            ColorOrder = colorOrder,
            UIGameMode = "Battle",
            UIMapName = "RA Map",
            CustomLoadScreen = "ls.shp",
        };

        IniFile spawnIni = SpawnIniWriter.WriteMatch(request, redAlert);

        Assert.Equal(13, spawnIni.GetIntValue("Multi1_Alliances", "HouseAllyOne", 0));
        Assert.Equal(12, spawnIni.GetIntValue("Multi2_Alliances", "HouseAllyOne", 0));
    }

    [Fact]
    public void OptionLayersCanTuneTheNetworkButNotTheSession()
    {
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "Player",
            Humans = [new HumanHouse { Name = "Player" }],
            ColorOrder = colorOrder,
            Network = new MatchNetwork
            {
                FrameSendRate = 7,
                Protocol = 2,
                Session = new TunnelMatchSession { GameID = 4, IsHost = true, Port = 48000, Tunnel = new TunnelEndpoint("127.0.0.1", 50000) },
            },
            OptionLayers =
            [
                MatchScenarios.Layer("GameOptions.ini [ForcedSpawnIniOptions]",
                    ("FrameSendRate", "3"), ("Port", "1"), ("Host", "False"), ("GameID", "0")),
            ],
        };

        IniFile spawnIni = SpawnIniWriter.WriteMatch(request, tiberianSun);

        Assert.Equal("3", spawnIni.GetStringValue("Settings", "FrameSendRate", string.Empty));
        Assert.Equal(48000, spawnIni.GetIntValue("Settings", "Port", 0));
        Assert.True(spawnIni.GetBooleanValue("Settings", "Host", false));
        Assert.Equal(4, spawnIni.GetIntValue("Settings", "GameID", 0));
        Assert.Equal(50000, spawnIni.GetIntValue("Tunnel", "Port", 0));
    }

    [Fact]
    public void RedAlertCoopAlliancesAreShifted()
    {
        IniFile spawnIni = SpawnIniWriter.WriteMatch(MatchScenarios.CoopMapHouses(), redAlert);

        // House IDs are 0-based in the other games and shifted by 12 in Red Alert.
        Assert.Equal(12, spawnIni.GetIntValue("Multi3_Alliances", "HouseAllyOne", -1));
        Assert.Equal(13, spawnIni.GetIntValue("Multi3_Alliances", "HouseAllyTwo", -1));
        Assert.Equal(14, spawnIni.GetIntValue("Multi1_Alliances", "HouseAllyTwo", -1));
        Assert.Equal(16, spawnIni.GetIntValue("Multi4_Alliances", "HouseAllyOne", -1));
        Assert.Equal(15, spawnIni.GetIntValue("Multi5_Alliances", "HouseAllyOne", -1));
    }

    [Fact]
    public void RecordingStates()
    {
        MatchLaunchRequest Request(ReplayRecording? recording) => new()
        {
            LocalPlayerName = "Player",
            Humans = [new HumanHouse { Name = "Player" }],
            ColorOrder = colorOrder,
            Recording = recording,
        };

        IniFile unsupported = SpawnIniWriter.WriteMatch(Request(null), tiberianSun);
        Assert.False(unsupported.KeyExists("Settings", "EnableReplayRecording"));

        IniFile disabled = SpawnIniWriter.WriteMatch(Request(new ReplayRecording { Enabled = false }), tiberianSun);
        Assert.Equal("False", disabled.GetStringValue("Settings", "EnableReplayRecording", string.Empty));
        Assert.False(disabled.KeyExists("Settings", "ReplayFileOut"));
        Assert.False(disabled.SectionExists(SpawnIniWriter.REPLAY_FILE_HASHES));

        IniFile noHashes = SpawnIniWriter.WriteMatch(Request(new ReplayRecording { Enabled = true, FileOut = "r.yrpl" }), tiberianSun);
        Assert.Equal("r.yrpl", noHashes.GetStringValue("Settings", "ReplayFileOut", string.Empty));
        Assert.False(noHashes.SectionExists(SpawnIniWriter.REPLAY_FILE_HASHES));
    }

    [Fact]
    public void NetworkWithoutTunnelOrSession()
    {
        MatchLaunchRequest Request(MatchSession? session) => new()
        {
            LocalPlayerName = "Player",
            Humans = [new HumanHouse { Name = "Player" }],
            ColorOrder = colorOrder,
            Network = new MatchNetwork { FrameSendRate = 7, Protocol = 2, Session = session },
        };

        IniFile noSession = SpawnIniWriter.WriteMatch(Request(null), tiberianSun);
        Assert.Equal(7, noSession.GetIntValue("Settings", "FrameSendRate", 0));
        Assert.False(noSession.KeyExists("Settings", "MaxAhead"));
        Assert.False(noSession.KeyExists("Settings", "GameID"));
        Assert.False(noSession.SectionExists("Tunnel"));

        IniFile noTunnel = SpawnIniWriter.WriteMatch(Request(new TunnelMatchSession { GameID = 4, Port = 48000 }), tiberianSun);
        Assert.False(noTunnel.SectionExists("Tunnel"));
        Assert.Equal(4, noTunnel.GetIntValue("Settings", "GameID", 0));
    }
}
