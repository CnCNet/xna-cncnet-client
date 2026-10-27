using System;
using System.IO;

using ClientCore.Launch;

using Xunit;

namespace ClientCore.Tests.Launch;

/// <summary>Tests the saved match files in a temporary saved games directory.</summary>
public sealed class SavedMatchStoreFileTests : IDisposable
{
    private static readonly DateTime earlier = new(2026, 1, 1, 12, 0, 0);
    private static readonly DateTime matchStart = new(2026, 1, 2, 12, 0, 0);

    private readonly DirectoryInfo directory = new(Path.Combine(Path.GetTempPath(), "SavedMatchStoreTests-" + Guid.NewGuid().ToString("N")));

    public SavedMatchStoreFileTests() => directory.Create();

    public void Dispose() => directory.Delete(true);

    private string PathOf(string fileName) => Path.Combine(directory.FullName, fileName);

    private void WriteFile(string fileName, string content, DateTime lastWriteTime)
    {
        File.WriteAllText(PathOf(fileName), content);
        File.SetLastWriteTime(PathOf(fileName), lastWriteTime);
    }

    private void StoreSavedMatch(MatchLaunchRequest request, DateTime lastWriteTime)
        => WriteFile("match.json", SavedMatchStore.Serialize(request), lastWriteTime);

    private MatchLaunchRequest? LoadRequest() => SavedMatchStore.LoadRequest(directory);

    [Fact]
    public void UnsavedMatchKeepsThePreviousSavedMatch()
    {
        StoreSavedMatch(MatchScenarios.SkirmishWithTeams(), earlier);
        WriteFile("SVGM_000.NET", "old save", earlier);

        SavedMatchStore.RecordLaunch(MatchScenarios.LanHousesFollowColorOrder(), directory);
        SavedMatchStore.CompletePending(matchStart, directory);

        Assert.False(File.Exists(PathOf("match.pending.json")));
        Assert.Equal("Player", LoadRequest()?.LocalPlayerName);
    }

    [Fact]
    public void SavedMatchReplacesThePreviousSavedMatch()
    {
        StoreSavedMatch(MatchScenarios.SkirmishWithTeams(), earlier);

        SavedMatchStore.RecordLaunch(MatchScenarios.LanHousesFollowColorOrder(), directory);
        // The game saved the match itself, as OpenTS does.
        WriteFile("SVGM_000.NET", "new save", matchStart.AddMinutes(5));
        SavedMatchStore.CompletePending(matchStart, directory);

        Assert.False(File.Exists(PathOf("match.pending.json")));
        Assert.Equal("Alice", LoadRequest()?.LocalPlayerName);
    }

    [Fact]
    public void SaveHookPromotesRightAway()
    {
        SavedMatchStore.RecordLaunch(MatchScenarios.LanHousesFollowColorOrder(), directory);
        SavedMatchStore.PromotePending(directory);

        Assert.Equal("Alice", LoadRequest()?.LocalPlayerName);

        // Ending the match afterwards has nothing left to promote or discard.
        SavedMatchStore.CompletePending(matchStart, directory);
        Assert.Equal("Alice", LoadRequest()?.LocalPlayerName);
    }

    [Fact]
    public void UnreadableRequestIsIgnored()
    {
        WriteFile("match.json", "{ not json", earlier);

        Assert.Null(LoadRequest());
    }
}
