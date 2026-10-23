#nullable enable
using System;
using System.IO;

using ClientCore.Statistics.GameParsers;

using Xunit;

namespace Statistics.Tests;

public sealed class RedAlertStatisticsParserTests
{
    [Fact]
    public void ReadsUploadedResignationDump()
    {
        using var packet = new StatisticsPacket();
        packet.WriteBytes(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "stats.dmp")));
        var statistics = StatisticsPacket.Match("mah_boi");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(0, statistics.LengthInSeconds);
        Assert.Equal(0, statistics.AverageFPS);
        Assert.Equal(0, statistics.Players[0].Kills);
        Assert.Equal(1, statistics.Players[0].Losses);
        Assert.Equal(0, statistics.Players[0].Economy);
        Assert.True(statistics.Players[0].SawEnd);
        Assert.False(statistics.Players[0].Won);
        Assert.False(statistics.Players[0].WasSpectator);
        Assert.True(statistics.SawCompletion);
    }

    [Fact]
    public void ReadsBigEndianCountersAndDurationIndependentlyOfRecordOrder()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("INK1", StatisticsPacket.Counts(1, 2)),
            ("UNK1", StatisticsPacket.Counts(3, 4)),
            ("PLK1", StatisticsPacket.Counts(5)),
            ("BLK1", StatisticsPacket.Counts(6)),
            ("VSK1", StatisticsPacket.Counts(7)),
            ("INB1", StatisticsPacket.Counts(10, 2)),
            ("INL1", StatisticsPacket.Counts(7, 5)),
            ("UNB1", StatisticsPacket.Counts(4)),
            ("UNL1", StatisticsPacket.Counts(2)),
            ("PLB1", StatisticsPacket.Counts(3)),
            ("PLL1", StatisticsPacket.Counts(1)),
            ("BLB1", StatisticsPacket.Counts(8)),
            ("BLL1", StatisticsPacket.Counts(4)),
            ("VSB1", StatisticsPacket.Counts(6)),
            ("VSL1", StatisticsPacket.Counts(5)),
            ("TIME", StatisticsPacket.Number(1792511247)),
            ("DURA", StatisticsPacket.Number(123)),
            ("AFPS", StatisticsPacket.Number(60)),
            ("HRV1", StatisticsPacket.Number(123456)),
            ("NAM1", StatisticsPacket.Text("aLiCe")),
            ("RSG1", StatisticsPacket.Number(1)),
            ("XTRA", new byte[] { 1, 2, 3 }));
        var statistics = StatisticsPacket.Match("Alice");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(123, statistics.LengthInSeconds);
        Assert.Equal(60, statistics.AverageFPS);
        Assert.Equal(28, statistics.Players[0].Kills);
        Assert.Equal(12, statistics.Players[0].Losses);
        Assert.Equal(123456, statistics.Players[0].Economy);
        Assert.False(statistics.Players[0].Won);
    }

    [Fact]
    public void ReadsSpectatorFlagAsBigEndianInteger()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Observer")),
            ("SPC1", StatisticsPacket.Number(1)),
            ("DED1", StatisticsPacket.Number(0)),
            ("RSG1", StatisticsPacket.Number(0)),
            ("CON1", StatisticsPacket.Number(0)));
        var statistics = StatisticsPacket.Match("Observer");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.True(statistics.Players[0].WasSpectator);
        Assert.False(statistics.Players[0].Won);
    }

    [Fact]
    public void MatchesNamedPlayersBeforeFallingBackToHouseOrderForComputers()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Computer")),
            ("NAM2", StatisticsPacket.Text("ALICE")),
            ("NAM3", StatisticsPacket.Text("Computer")),
            ("HRV1", StatisticsPacket.Number(100)),
            ("HRV2", StatisticsPacket.Number(200)),
            ("HRV3", StatisticsPacket.Number(300)));
        var statistics = StatisticsPacket.Match("Computer", "Alice", "Computer");
        statistics.Players[0].IsAI = true;
        statistics.Players[2].IsAI = true;

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(100, statistics.Players[0].Economy);
        Assert.Equal(200, statistics.Players[1].Economy);
        Assert.Equal(300, statistics.Players[2].Economy);
    }

    [Theory]
    [InlineData("DED1")]
    [InlineData("RSG1")]
    [InlineData("CON1")]
    public void EliminatedPlayerCannotWin(string eliminatedField)
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("DED1", StatisticsPacket.Number(0)),
            ("RSG1", StatisticsPacket.Number(0)),
            ("CON1", StatisticsPacket.Number(0)),
            (eliminatedField, StatisticsPacket.Number(1)));
        var statistics = StatisticsPacket.Match("Alice");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.False(statistics.Players[0].Won);
    }

    [Fact]
    public void ParserReuseDoesNotCarryScoresIntoAnotherMatch()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("HRV1", StatisticsPacket.Number(1234)),
            ("INK1", StatisticsPacket.Counts(7)),
            ("RSG1", StatisticsPacket.Number(1)));
        var parser = new RedAlertStatisticsParser(StatisticsPacket.Match("Alice"), false);
        parser.ParseStats(packet.DirectoryPath, "unused.log");

        var nextMatch = StatisticsPacket.Match("Bob");
        parser.Statistics = nextMatch;
        packet.Write(("NAM1", StatisticsPacket.Text("Bob")));
        parser.ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(0, nextMatch.Players[0].Kills);
        Assert.Equal(0, nextMatch.Players[0].Economy);
        Assert.False(nextMatch.Players[0].Won);
    }

    [Fact]
    public void LastSurvivingPlayerWinsAfterOpponentResigns()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("DED1", StatisticsPacket.Number(0)),
            ("RSG1", StatisticsPacket.Number(0)),
            ("NAM2", StatisticsPacket.Text("Bob")),
            ("DED2", StatisticsPacket.Number(0)),
            ("RSG2", StatisticsPacket.Number(1)),
            ("CMPL", new byte[] { 255 }));
        var statistics = StatisticsPacket.Match("Alice", "Bob");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.True(statistics.SawCompletion);
        Assert.True(statistics.Players[0].Won);
        Assert.True(statistics.Players[0].SawEnd);
        Assert.False(statistics.Players[1].Won);
        Assert.True(statistics.Players[1].SawEnd);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MultipleSurvivorsOnlyWinWithMutualAlliances(bool allied)
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("DED1", StatisticsPacket.Number(0)),
            ("RSG1", StatisticsPacket.Number(0)),
            ("ALY1", StatisticsPacket.Number(allied ? 1u << 13 : 0)),
            ("NAM2", StatisticsPacket.Text("Bob")),
            ("DED2", StatisticsPacket.Number(0)),
            ("RSG2", StatisticsPacket.Number(0)),
            ("ALY2", StatisticsPacket.Number(allied ? 1u << 12 : 0)),
            ("NAM3", StatisticsPacket.Text("Charlie")),
            ("DED3", StatisticsPacket.Number(1)),
            ("RSG3", StatisticsPacket.Number(0)));
        var statistics = StatisticsPacket.Match("Alice", "Bob", "Charlie");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(allied, statistics.Players[0].Won);
        Assert.Equal(allied, statistics.Players[1].Won);
        Assert.False(statistics.Players[2].Won);
    }

    [Theory]
    [InlineData("OOSY")]
    [InlineData("SDFX")]
    [InlineData("CON2")]
    public void NetworkFailuresDoNotAwardWinsOrCompletion(string failureField)
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("DED1", StatisticsPacket.Number(0)),
            ("RSG1", StatisticsPacket.Number(0)),
            ("NAM2", StatisticsPacket.Text("Bob")),
            ("DED2", StatisticsPacket.Number(1)),
            ("RSG2", StatisticsPacket.Number(0)),
            (failureField, failureField == "CON2" ? StatisticsPacket.Number(1) : new byte[] { 1 }));
        var statistics = StatisticsPacket.Match("Alice", "Bob");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.False(statistics.SawCompletion);
        Assert.False(statistics.Players[0].Won);
        Assert.False(statistics.Players[1].Won);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(4, 1)]
    [InlineData(5, 1)]
    public void LegacyTwoPlayerCompletionIdentifiesWinner(byte completion, int winnerIndex)
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("NAM2", StatisticsPacket.Text("Bob")),
            ("CMPL", new byte[] { completion }));
        var statistics = StatisticsPacket.Match("Alice", "Bob");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.True(statistics.SawCompletion);
        Assert.True(statistics.Players[winnerIndex].Won);
        Assert.False(statistics.Players[1 - winnerIndex].Won);
        Assert.True(statistics.Players[winnerIndex].SawEnd);
        Assert.True(statistics.Players[1 - winnerIndex].SawEnd);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(6)]
    public void LegacyDisconnectedCompletionDoesNotAwardWins(byte completion)
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("DED1", StatisticsPacket.Number(0)),
            ("RSG1", StatisticsPacket.Number(0)),
            ("NAM2", StatisticsPacket.Text("Bob")),
            ("DED2", StatisticsPacket.Number(1)),
            ("RSG2", StatisticsPacket.Number(0)),
            ("CMPL", new byte[] { completion }));
        var statistics = StatisticsPacket.Match("Alice", "Bob");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.False(statistics.SawCompletion);
        Assert.False(statistics.Players[0].Won);
        Assert.False(statistics.Players[1].Won);
    }

    [Fact]
    public void UnknownOpponentStateDoesNotImplyLocalWin()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("DED1", StatisticsPacket.Number(0)),
            ("RSG1", StatisticsPacket.Number(0)),
            ("NAM2", StatisticsPacket.Text("Bob")),
            ("NAM3", StatisticsPacket.Text("Charlie")),
            ("DED3", StatisticsPacket.Number(1)),
            ("RSG3", StatisticsPacket.Number(0)));
        var statistics = StatisticsPacket.Match("Alice", "Bob", "Charlie");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.False(statistics.Players[0].Won);
        Assert.False(statistics.Players[1].Won);
        Assert.False(statistics.Players[2].Won);
    }

    [Fact]
    public void MissingLobbyOpponentDoesNotImplyLocalWin()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("DED1", StatisticsPacket.Number(0)),
            ("RSG1", StatisticsPacket.Number(0)),
            ("NAM2", StatisticsPacket.Text("Bob")),
            ("DED2", StatisticsPacket.Number(1)),
            ("RSG2", StatisticsPacket.Number(0)));
        var statistics = StatisticsPacket.Match("Alice", "Bob", "Charlie");

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.False(statistics.Players[0].Won);
        Assert.False(statistics.Players[1].Won);
        Assert.False(statistics.Players[2].Won);
    }

    [Fact]
    public void MismatchedCounterArraysDoNotPartiallyApplyMatchOrEarlierHouseResults()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("DURA", StatisticsPacket.Number(999)),
            ("AFPS", StatisticsPacket.Number(60)),
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("INK1", StatisticsPacket.Counts(100)),
            ("HRV1", StatisticsPacket.Number(10000)),
            ("SPC1", StatisticsPacket.Number(1)),
            ("NAM2", StatisticsPacket.Text("Bob")),
            ("INB2", StatisticsPacket.Counts(3, 4)),
            ("INL2", StatisticsPacket.Counts(2)));
        var statistics = StatisticsPacket.Match("Alice", "Bob");
        statistics.LengthInSeconds = 42;
        statistics.AverageFPS = 30;
        statistics.SawCompletion = true;
        foreach (var player in statistics.Players)
        {
            player.Kills = 17;
            player.Losses = 18;
            player.Economy = 19;
            player.Score = 20;
            player.SawEnd = true;
            player.Won = true;
        }

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(42, statistics.LengthInSeconds);
        Assert.Equal(30, statistics.AverageFPS);
        Assert.True(statistics.SawCompletion);
        foreach (var player in statistics.Players)
        {
            Assert.Equal(17, player.Kills);
            Assert.Equal(18, player.Losses);
            Assert.Equal(19, player.Economy);
            Assert.Equal(20, player.Score);
            Assert.True(player.SawEnd);
            Assert.True(player.Won);
            Assert.False(player.WasSpectator);
        }
    }

    [Fact]
    public void ClampsUnsignedTotalsWithoutOverwritingLobbyMetadataOrScore()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("NAM1", StatisticsPacket.Text("Alice")),
            ("COL1", new byte[] { 7 }),
            ("SID1", StatisticsPacket.Text("USS")),
            ("HRV1", StatisticsPacket.Number(uint.MaxValue)),
            ("INK1", StatisticsPacket.Counts(uint.MaxValue, 7)),
            ("DURA", StatisticsPacket.Number(uint.MaxValue)),
            ("AFPS", StatisticsPacket.Number(uint.MaxValue)));
        var statistics = StatisticsPacket.Match("Alice");
        statistics.Players[0].Score = 123;
        statistics.Players[0].Color = 2;
        statistics.Players[0].Side = 3;

        new RedAlertStatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(int.MaxValue, statistics.LengthInSeconds);
        Assert.Equal(int.MaxValue, statistics.AverageFPS);
        Assert.Equal(int.MaxValue, statistics.Players[0].Economy);
        Assert.Equal(int.MaxValue, statistics.Players[0].Kills);
        Assert.Equal(123, statistics.Players[0].Score);
        Assert.Equal(2, statistics.Players[0].Color);
        Assert.Equal(3, statistics.Players[0].Side);
    }
}