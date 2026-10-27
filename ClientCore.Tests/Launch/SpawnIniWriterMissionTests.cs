using ClientCore.Launch;
using ClientCore.Launch.Spawner;

using Rampastring.Tools;

using Xunit;

namespace ClientCore.Tests.Launch;

public class SpawnIniWriterMissionTests
{
    private static MissionLaunchRequest CustomMission(bool requiresAddon) => new()
    {
        Scenario = "CUSTOM.MAP",
        CampaignID = -1,
        GameSpeed = 2,
        RequiresAddon = requiresAddon,
        CustomLoadScreen = "ls.shp",
        UIMapName = "Custom",
        Side = 1,
        HumanDifficulty = 1,
        ComputerDifficulty = 1,
        CustomMissionID = 12345,
    };

    [Fact]
    public void YurisRevengeCampaignMission()
    {
        var request = new MissionLaunchRequest
        {
            Scenario = ProgramConstants.SPAWNMAP_INI,
            CampaignID = -1,
            GameSpeed = 1,
            RequiresAddon = true,
            CustomLoadScreen = "ls.shp",
            UIMapName = "Allied 01",
            SidebarHack = false,
            Side = 0,
            BuildOffAlly = false,
            HumanDifficulty = 2,
            ComputerDifficulty = 0,
            MissionSection = new MissionSection("ALL01.MAP",
                [new SpawnOption("LS640BkgdName", "ls.shp"), new SpawnOption("LS800BkgdName", "ls.shp")]),
            OptionLayers =
            [
                new SpawnOptionLayer("Campaign options", [new SpawnOption("SkipScoreScreen", "True")]),
                new SpawnOptionLayer("GameOptions.ini [CampaignForcedSpawnIniOptions]", [new SpawnOption("AutoSaveInterval", "0")]),
            ],
            Recording = new ReplayRecording { Enabled = true, GamePackageVersion = "YR 1.0", FileOut = "Replays/x.yrpl" },
        };

        GoldenIni.AssertMatches("YurisRevengeCampaignMission",
            SpawnIniWriter.WriteMission(request, SpawnIniDialect.ForClientType(ClientType.YR)));
    }

    [Fact]
    public void AddonFlagDependsOnGame()
    {
        IniFile tiberianSun = SpawnIniWriter.WriteMission(CustomMission(true), SpawnIniDialect.ForClientType(ClientType.TS));
        Assert.Equal("True", tiberianSun.GetStringValue("Settings", "Firestorm", string.Empty));
        Assert.False(tiberianSun.KeyExists("Settings", "Ra2Mode"));
        Assert.Equal("CUSTOM.MAP", tiberianSun.GetStringValue("Settings", "Scenario", string.Empty));
        Assert.Equal("12345", tiberianSun.GetStringValue("Settings", "CustomMissionID", string.Empty));
        Assert.False(tiberianSun.KeyExists("Settings", "ReadMissionSection"));

        IniFile ares = SpawnIniWriter.WriteMission(CustomMission(false), SpawnIniDialect.ForClientType(ClientType.Ares));
        Assert.Equal("True", ares.GetStringValue("Settings", "Ra2Mode", string.Empty));

        IniFile redAlert = SpawnIniWriter.WriteMission(CustomMission(true), SpawnIniDialect.ForClientType(ClientType.RA));
        Assert.False(redAlert.KeyExists("Settings", "Ra2Mode"));
        Assert.False(redAlert.KeyExists("Settings", "Firestorm"));
        Assert.False(redAlert.KeyExists("Settings", "EnableReplayRecording"));
    }

    [Fact]
    public void SavedCustomMission()
    {
        var request = new SavedMissionLaunchRequest
        {
            SaveGameName = "SAVE0001.SAV",
            SidebarHack = true,
            CustomLoadScreen = "gls.shp",
            GameSpeed = 3,
            CustomMissionID = 777,
            MissionSection = new MissionSection("MY.MAP", [new SpawnOption("LS800BkgdPal", "custom.pal")]),
            OptionLayers = [new SpawnOptionLayer("GameOptions.ini [CampaignForcedSpawnIniOptions]", [new SpawnOption("AutoSaveInterval", "0")])],
        };

        GoldenIni.AssertMatches("SavedCustomMission", SpawnIniWriter.WriteSavedMission(request));
    }

    [Fact]
    public void SavedMissionWithoutMission()
    {
        IniFile spawnIni = SpawnIniWriter.WriteSavedMission(new SavedMissionLaunchRequest { SaveGameName = "SAVE0002.SAV", GameSpeed = 1 });

        // Parity (F5): Firestorm=No is written whatever the game.
        Assert.Equal("No", spawnIni.GetStringValue("Settings", "Firestorm", string.Empty));
        Assert.False(spawnIni.KeyExists("Settings", "CustomMissionID"));
        Assert.Single(spawnIni.GetSections());
    }
}
