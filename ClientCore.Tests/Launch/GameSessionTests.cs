using System.Collections.Generic;

using ClientCore.Launch;
using ClientCore.Launch.Spawner;

using Xunit;

namespace ClientCore.Tests.Launch;

public class GameSessionTests
{
    [Fact]
    public void HandlerAddedBeforeExitRunsOnExit()
    {
        var session = new GameSession(new MatchLaunchRequest());
        var results = new List<GameSessionResult>();

        session.WhenEnded(results.Add);
        Assert.True(session.IsRunning);
        Assert.Empty(results);

        session.End(3);

        Assert.False(session.IsRunning);
        Assert.Equal(3, Assert.Single(results).ExitCode);
    }

    [Fact]
    public void HandlerAddedAfterExitRunsImmediately()
    {
        // A game can exit before the launching code gets to subscribe.
        var session = new GameSession(new MatchLaunchRequest());
        session.End(null);

        GameSessionResult? result = null;
        session.WhenEnded(r => result = r);

        Assert.NotNull(result);
        Assert.Null(result.ExitCode);
    }

    [Fact]
    public void HandlersRunOnce()
    {
        var session = new GameSession(new MatchLaunchRequest());
        int calls = 0;

        session.WhenEnded(_ => calls++);
        session.End(0);
        session.End(0);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void SpawnerSupportsEveryLaunchKind()
    {
        var launcher = new SpawnerGameLauncher();

        Assert.True(launcher.Supports(new MatchLaunchRequest()));
        Assert.True(launcher.Supports(new MissionLaunchRequest()));
        Assert.True(launcher.Supports(new SavedMissionLaunchRequest()));
        Assert.True(launcher.Supports(new SavedMatchLaunchRequest()));
        Assert.True(launcher.Supports(new ReplayLaunchRequest()));
    }

    [Fact]
    public void FailedResultHasNoSession()
    {
        LaunchResult result = LaunchResult.Failed(LaunchFailureReason.ProcessStartFailed, "game.exe", "Access denied");

        Assert.False(result.Succeeded);
        Assert.Null(result.Session);
        Assert.Equal("game.exe", result.ExecutableName);
    }
}
