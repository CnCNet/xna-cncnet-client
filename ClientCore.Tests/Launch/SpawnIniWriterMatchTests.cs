using System.Collections.Generic;

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

    private static SpawnOptionLayer Layer(string name, params (string Key, string Value)[] options)
    {
        var spawnOptions = new List<SpawnOption>();

        foreach ((string key, string value) in options)
            spawnOptions.Add(new SpawnOption(key, value));

        return new SpawnOptionLayer(name, spawnOptions);
    }

    [Fact]
    public void SkirmishWithTeams()
    {
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "Player",
            Humans = [new HumanHouse { Name = "Player", InternalSideIndex = 1, ColorIndex = 2, StartingWaypoint = 0, TeamId = 1 }],
            AIs =
            [
                new AIHouse { InternalSideIndex = 0, ColorIndex = 0, StartingWaypoint = 1, TeamId = 1, HandicapLevel = 2 },
                new AIHouse { InternalSideIndex = 2, ColorIndex = 5, StartingWaypoint = -1, TeamId = 2, HandicapLevel = 0 },
            ],
            ColorOrder = colorOrder,
            Seed = 12345,
            UIGameMode = "Battle",
            UIMapName = "Test Map",
            CustomLoadScreen = "ls.shp",
            OptionLayers = [Layer("Game options", ("GameSpeed", "1"), ("ShortGame", "True"))],
            MapSHA1 = "abc",
            BroadcastedGameOptionValues = "1,2",
        };

        GoldenIni.AssertMatches("SkirmishWithTeams", SpawnIniWriter.WriteMatch(request, tiberianSun));
    }

    [Fact]
    public void LanHousesFollowColorOrder()
    {
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "Alice",
            Humans =
            [
                new HumanHouse { Name = "Alice", InternalSideIndex = 0, ColorIndex = 5, StartingWaypoint = 2, TeamId = 1 },
                new HumanHouse { Name = "Bob", InternalSideIndex = 1, ColorIndex = 1, StartingWaypoint = 3, TeamId = 1, IpAddress = "192.168.1.5", Port = 1234 },
            ],
            ColorOrder = colorOrder,
            Seed = 7,
            UIGameMode = "Battle",
            UIMapName = "Lan Map",
            MapID = "maps/official/lan",
            CustomLoadScreen = "ls.shp",
            Network = new MatchNetwork
            {
                FrameSendRate = 7,
                MaxAhead = 0,
                Protocol = 2,
                Session = new LanMatchSession { Port = 1234, GameID = 99, IsHost = true },
            },
            MapSHA1 = "def",
        };

        GoldenIni.AssertMatches("LanHousesFollowColorOrder", SpawnIniWriter.WriteMatch(request, tiberianSun));
    }

    [Fact]
    public void CnCNetSpectatorHost()
    {
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "Host",
            Humans =
            [
                new HumanHouse { Name = "Host", InternalSideIndex = 9, ColorIndex = 0, StartingWaypoint = 90, IsSpectator = true, Port = 48000 },
                new HumanHouse { Name = "Guest", InternalSideIndex = 1, ColorIndex = 3, StartingWaypoint = 1, IpAddress = "0.0.0.0", Port = 47999 },
            ],
            ColorOrder = colorOrder,
            Seed = 99,
            UIGameMode = "Battle",
            UIMapName = "Online Map",
            CustomLoadScreen = "ls.shp",
            CoachMode = true,
            Network = new MatchNetwork
            {
                FrameSendRate = 7,
                MaxAhead = 3,
                Protocol = 2,
                Session = new TunnelMatchSession
                {
                    Tunnel = new TunnelEndpoint("127.0.0.1", 50000),
                    GameID = 7,
                    IsHost = true,
                    Port = 48000,
                },
            },
            MapSHA1 = "ghi",
        };

        GoldenIni.AssertMatches("CnCNetSpectatorHost", SpawnIniWriter.WriteMatch(request, tiberianSun));
    }

    [Fact]
    public void CoopMapHouses()
    {
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "P",
            Humans = [new HumanHouse { Name = "P", InternalSideIndex = 1, ColorIndex = 0, StartingWaypoint = 0, TeamId = 1 }],
            AIs = [new AIHouse { InternalSideIndex = 0, ColorIndex = 1, StartingWaypoint = -1, TeamId = 1, HandicapLevel = 1 }],
            ColorOrder = colorOrder,
            Seed = 5,
            UIGameMode = "Co-op",
            UIMapName = "Coop Map",
            CustomLoadScreen = "ls.shp",
            DisableAutoSurrender = true,
            MapSettings = new MatchMapSettings { Credits = 10000, NeutralHouseColor = 8, SpecialHouseColor = 9, Bases = true },
            Coop = new CoopSetup
            {
                DifficultyLevel = 2,
                AllyHouses = [new CoopHouse(3, 4, 5)],
                EnemyHouses = [new CoopHouse(2, 6, 6), new CoopHouse(2, 7, 7)],
            },
            MapSHA1 = "jkl",
        };

        GoldenIni.AssertMatches("CoopMapHouses", SpawnIniWriter.WriteMatch(request, tiberianSun));
    }

    [Fact]
    public void LaterLayersOverrideEarlierOnesAndCoreKeys()
    {
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "Solo",
            Humans = [new HumanHouse { Name = "Solo", InternalSideIndex = 0, ColorIndex = 4, StartingWaypoint = -1 }],
            ColorOrder = colorOrder,
            Seed = 42,
            UIGameMode = "Battle",
            UIMapName = "Layer Map",
            CustomLoadScreen = "ls.shp",
            OptionLayers =
            [
                Layer("Game options", ("GameSpeed", "1"), ("Crates", "True")),
                Layer("GameOptions.ini [ForcedSpawnIniOptions]", ("Crates", "False"), ("Seed", "1")),
                Layer("Game mode Battle", ("GameSpeed", "3")),
                Layer("Map Layer Map"),
            ],
            MapSettings = new MatchMapSettings { UnitCount = 5 },
            MapSHA1 = "mno",
            Recording = new ReplayRecording
            {
                Enabled = true,
                GamePackageVersion = "TS 1.0",
                FileOut = "Replays/2026-01-01 00-00-00 Layer Map.yrpl",
                FileHashes = ["a.ini|1", "b.mix|2"],
            },
        };

        GoldenIni.AssertMatches("LaterLayersOverrideEarlierOnesAndCoreKeys", SpawnIniWriter.WriteMatch(request, tiberianSun));
    }

    [Fact]
    public void StackedStartUsesLobbyOrderSlot()
    {
        // Parity (F1): the stacked player's waypoint lands on the lobby-order slot, which here
        // belongs to Bob because houses are otherwise numbered by color.
        var request = new MatchLaunchRequest
        {
            LocalPlayerName = "A",
            Humans =
            [
                new HumanHouse { Name = "A", InternalSideIndex = 0, ColorIndex = 3, StartingWaypoint = -1, StackedStartingWaypoint = 4 },
                new HumanHouse { Name = "B", InternalSideIndex = 0, ColorIndex = 1, StartingWaypoint = 2, IpAddress = "10.0.0.2", Port = 1234 },
            ],
            ColorOrder = colorOrder,
            Seed = 1,
            UIGameMode = "Battle",
            UIMapName = "Stack Map",
            CustomLoadScreen = "ls.shp",
            Network = new MatchNetwork
            {
                FrameSendRate = 7,
                Protocol = 2,
                Session = new LanMatchSession { Port = 1234, GameID = 3, IsHost = true },
            },
            MapSHA1 = "pqr",
        };

        GoldenIni.AssertMatches("StackedStartUsesLobbyOrderSlot", SpawnIniWriter.WriteMatch(request, tiberianSun));
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
