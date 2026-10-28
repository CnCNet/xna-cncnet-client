#nullable enable

using System;
using System.Threading;

using ClientCore.INIProcessing;

using Rampastring.Tools;

namespace ClientCore.Launch.Spawner;

/// <summary>Launches the game by writing spawn.ini and spawnmap.ini and starting it with -SPAWN.</summary>
public sealed class SpawnerGameLauncher : IGameLauncher
{
    public bool Supports(LaunchRequest request) => request is MatchLaunchRequest or MissionLaunchRequest
        or SavedMissionLaunchRequest or SavedMatchLaunchRequest or ReplayLaunchRequest;

    public LaunchResult Launch(LaunchRequest request)
    {
        Logger.Log("About to launch main game executable.");

        if (!WaitForPreprocessor())
            return LaunchResult.Failed(LaunchFailureReason.PreprocessingIncomplete);

        WriteSpawnFiles(request);

        return GameProcessHost.Start(request);
    }

    /// <summary>
    /// Waits for INI preprocessing to finish, which it rarely hasn't by the time a game starts.
    /// Returns false if it is still running after about ten seconds.
    /// </summary>
    // TODO ideally this should be handled in the UI so the client doesn't appear just frozen for the user.
    private static bool WaitForPreprocessor()
    {
        int waitTimes = 0;
        while (PreprocessorBackgroundTask.Instance.IsRunning)
        {
            Logger.Log("The preprocessor background task is still running. Wait for it...");
            Thread.Sleep(1000);
            waitTimes++;
            if (waitTimes > 10)
                return false;
        }

        return true;
    }

    private static void WriteSpawnFiles(LaunchRequest request)
    {
        SpawnIniDialect dialect = SpawnIniDialect.ForClientType(ClientConfiguration.Instance.ClientGameType);

        switch (request)
        {
            case MatchLaunchRequest match:
                SpawnFiles.WriteSpawnIni(SpawnIniWriter.WriteMatch(match, dialect));
                SpawnFiles.WriteSpawnMap(match.SpawnMap ?? throw new ArgumentException("A match must have a map.", nameof(request)));
                break;
            case MissionLaunchRequest mission:
                SpawnFiles.WriteSpawnIni(SpawnIniWriter.WriteMission(mission, dialect));

                // Without a map of its own, the mission loads its scenario from the game's files.
                if (mission.SpawnMap != null)
                    SpawnFiles.WriteSpawnMap(mission.SpawnMap);
                break;
            case SavedMissionLaunchRequest savedMission:
                SpawnFiles.WriteSpawnIni(SpawnIniWriter.WriteSavedMission(savedMission));
                SpawnFiles.WriteStubSpawnMap();
                break;
            case SavedMatchLaunchRequest savedMatch:
                SpawnFiles.WriteSpawnIni(SpawnIniWriter.WriteSavedMatch(savedMatch));
                SpawnFiles.WriteStubSpawnMap();
                break;
            case ReplayLaunchRequest replay:
                SpawnFiles.WriteSpawnIni(SpawnIniWriter.WriteReplay(replay));
                // Campaign missions can load from game archives without an embedded map.
                SpawnFiles.WriteRecordedSpawnMap(replay.RecordedSpawnMap);
                break;
            default:
                throw new NotSupportedException("The spawner can't launch a " + request.GetType().Name + ".");
        }
    }
}
