#nullable enable
using System;
using System.Buffers.Binary;

using ClientCore.Statistics.GameParsers;

using Xunit;

namespace Statistics.Tests;

public sealed class Dune2000StatisticsParserTests
{
    [Fact]
    public void ReadsDuneScoresAndMatchesNamesIgnoringCase()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("SCR0", StatisticsPacket.Text("0/9/3/7/4/5/11/12345")),
            ("????", new byte[] { 1, 2, 3 }),
            ("PL_0", StatisticsPacket.Text("alice/Atreides")),
            ("TIME", StatisticsPacket.Number(321)),
            ("ENDS", StatisticsPacket.Number(1)));
        var statistics = StatisticsPacket.Match("Bob", "Alice");

        new Dune2000StatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(321, statistics.LengthInSeconds);
        Assert.True(statistics.SawCompletion);
        Assert.Equal(18, statistics.Players[1].Kills);
        Assert.Equal(8, statistics.Players[1].Losses);
        Assert.Equal(12345, statistics.Players[1].Economy);
        Assert.True(statistics.Players[1].Won);
        Assert.True(statistics.Players[1].SawEnd);
        Assert.False(statistics.Players[0].SawEnd);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void DisconnectControlsCompletion(byte disconnected, bool expectedCompletion)
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("PL_0", StatisticsPacket.Text("Alice")),
            ("SCR0", StatisticsPacket.Text("0/0/0/0/0/0/0/0")),
            ("SDFX", new byte[] { disconnected }));
        var statistics = StatisticsPacket.Match("Alice");

        new Dune2000StatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(expectedCompletion, statistics.SawCompletion);
    }

    [Fact]
    public void SpectatorCannotWin()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("PL_0", StatisticsPacket.Text("Observer")),
            ("SCR0", StatisticsPacket.Text("0/0/0/0/0/0/0/0")),
            ("SPC0", new byte[] { 1 }));
        var statistics = StatisticsPacket.Match("Observer");

        new Dune2000StatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.True(statistics.Players[0].WasSpectator);
        Assert.False(statistics.Players[0].Won);
    }

    [Fact]
    public void ParserReuseDoesNotCarryDisconnectOrHouseResultsToAnotherMatch()
    {
        using var packet = new StatisticsPacket();
        packet.Write(
            ("PL_0", StatisticsPacket.Text("Alice")),
            ("SCR0", StatisticsPacket.Text("0/0/1/2/0/3/4/500")),
            ("SDFX", new byte[] { 1 }));
        var parser = new Dune2000StatisticsParser(StatisticsPacket.Match("Alice"), false);
        parser.ParseStats(packet.DirectoryPath, "unused.log");

        var nextMatch = StatisticsPacket.Match("Bob");
        parser.Statistics = nextMatch;
        packet.Write(("PL_0", StatisticsPacket.Text("Bob")), ("SCR0", StatisticsPacket.Text("1/0/0/0/0/0/0/0")));
        parser.ParseStats(packet.DirectoryPath, "unused.log");

        Assert.True(nextMatch.SawCompletion);
        Assert.False(nextMatch.Players[0].Won);
        Assert.Equal(0, nextMatch.Players[0].Kills);
        Assert.Equal(0, nextMatch.Players[0].Losses);
        Assert.Equal(0, nextMatch.Players[0].Economy);
    }

    [Theory]
    [InlineData("short-header")]
    [InlineData("short-declared-length")]
    [InlineData("long-declared-length")]
    [InlineData("partial-field-header")]
    [InlineData("partial-field-body")]
    [InlineData("missing-padding")]
    public void RejectsMalformedPacketsWithoutApplyingEarlierFields(string corruption)
    {
        using var packet = new StatisticsPacket();
        byte[] bytes = StatisticsPacket.Create(("TIME", StatisticsPacket.Number(999)), ("SDFX", new byte[] { 1 }));

        switch (corruption)
        {
            case "short-header":
                bytes = new byte[] { 0, 3, 0 };
                break;
            case "short-declared-length":
                BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), 3);
                break;
            case "long-declared-length":
                BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), checked((ushort)(bytes.Length + 1)));
                break;
            case "partial-field-header":
                Array.Resize(ref bytes, 19);
                BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), checked((ushort)bytes.Length));
                break;
            case "partial-field-body":
                BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(22, 2), 100);
                break;
            case "missing-padding":
                Array.Resize(ref bytes, 25);
                BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0, 2), checked((ushort)bytes.Length));
                break;
        }

        packet.WriteBytes(bytes);
        var statistics = StatisticsPacket.Match("Alice");
        statistics.LengthInSeconds = 42;

        new Dune2000StatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.Equal(42, statistics.LengthInSeconds);
        Assert.False(statistics.SawCompletion);
        Assert.False(statistics.Players[0].SawEnd);
    }

    [Fact]
    public void StopsAtDeclaredPacketBoundary()
    {
        using var packet = new StatisticsPacket();
        byte[] bytes = StatisticsPacket.Create(
            ("PL_0", StatisticsPacket.Text("Alice")),
            ("SCR0", StatisticsPacket.Text("0/0/0/1/0/0/2/3")));
        int packetLength = bytes.Length;
        Array.Resize(ref bytes, packetLength + 8);
        bytes[packetLength] = 255;
        packet.WriteBytes(bytes);
        var statistics = StatisticsPacket.Match("Alice");

        new Dune2000StatisticsParser(statistics, false).ParseStats(packet.DirectoryPath, "unused.log");

        Assert.True(statistics.SawCompletion);
        Assert.Equal(3, statistics.Players[0].Kills);
    }
}