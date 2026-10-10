using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Rampastring.Tools;

namespace ClientCore.Statistics.GameParsers;

public sealed class Dune2000StatisticsParser : MatchParserBase
{
    private const string StatisticsFileName = "stats.dmp";
    private const int MaxPlayerCount = 8;

    private sealed class HouseResult
    {
        public string Name { get; set; }
        public bool HasScore { get; set; }
        public int FinishingPlace { get; set; }
        public int BuildingsLost { get; set; }
        public int BuildingsDestroyed { get; set; }
        public int UnitsLost { get; set; }
        public int UnitsKilled { get; set; }
        public int SpiceHarvested { get; set; }
        public bool? IsSpectator { get; set; }
    }

    private readonly HouseResult[] houseResults = Enumerable.Range(0, MaxPlayerCount).Select(_ => new HouseResult()).ToArray();

    private uint? endState;
    private bool suddenDisconnect;

    public Dune2000StatisticsParser(MatchStatistics statistics, bool isLoaded) : base(statistics, isLoaded)
    {
    }

    public override void ParseStats(string gamePath, string fileName) => ParseStatistics(gamePath);

    protected override void ParseStatistics(string gamePath)
    {
        FileInfo statisticsFile = SafePath.GetFile(gamePath, StatisticsFileName);
        if (!statisticsFile.Exists)
        {
            Logger.Log("Dune2000StatisticsParser: Failed to read statistics: stats.dmp does not exist.");
            return;
        }

        try
        {
            ReadRecords(statisticsFile.FullName);
            ApplyResults();
        }
        catch (Exception ex)
        {
            Logger.Log("Dune2000StatisticsParser: Error parsing stats.dmp: " + ex);
        }
    }

    private void ReadRecords(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        byte[] fileHeader = reader.ReadBytes(4);
        if (fileHeader.Length != 4)
            throw new InvalidDataException("stats.dmp is shorter than its four-byte header.");

        int declaredLength = BinaryPrimitives.ReadUInt16BigEndian(fileHeader.AsSpan(0, 2));
        if (declaredLength > stream.Length)
            throw new InvalidDataException("stats.dmp declares more data than the file contains.");

        while (stream.Position + 8 <= stream.Length)
        {
            string id = Encoding.ASCII.GetString(reader.ReadBytes(4));
            byte[] typeAndLength = reader.ReadBytes(4);
            if (typeAndLength.Length != 4)
                throw new EndOfStreamException("stats.dmp ended inside a field header.");

            int length = BinaryPrimitives.ReadUInt16BigEndian(typeAndLength.AsSpan(2, 2));
            if (length > stream.Length - stream.Position)
                throw new InvalidDataException($"stats.dmp field {id} exceeds the file length.");

            byte[] data = reader.ReadBytes(length);
            ReadRecord(id, data);

            long padding = (4 - stream.Position % 4) % 4;
            if (padding > stream.Length - stream.Position)
                break;

            stream.Position += padding;
        }
    }

    private void ReadRecord(string id, byte[] data)
    {
        if (TryGetHouseIndex(id, "PL_", out int houseIndex))
        {
            string[] fields = ReadString(data).Split('/');
            if (fields.Length > 0)
                houseResults[houseIndex].Name = fields[0];
            return;
        }

        if (TryGetHouseIndex(id, "SCR", out houseIndex))
        {
            string[] fields = ReadString(data).Split('/');
            if (fields.Length < 8)
                return;

            HouseResult result = houseResults[houseIndex];
            if (!TryParseInt(fields[0], out int finishingPlace)
                || !TryParseInt(fields[2], out int buildingsLost)
                || !TryParseInt(fields[3], out int buildingsDestroyed)
                || !TryParseInt(fields[5], out int unitsLost)
                || !TryParseInt(fields[6], out int unitsKilled)
                || !TryParseInt(fields[7], out int spiceHarvested))
            {
                return;
            }

            result.HasScore = true;
            result.FinishingPlace = finishingPlace;
            result.BuildingsLost = buildingsLost;
            result.BuildingsDestroyed = buildingsDestroyed;
            result.UnitsLost = unitsLost;
            result.UnitsKilled = unitsKilled;
            result.SpiceHarvested = spiceHarvested;
            return;
        }

        if (TryGetHouseIndex(id, "SPC", out houseIndex))
        {
            if (data.Length > 0)
                houseResults[houseIndex].IsSpectator = data[0] != 0;
            return;
        }

        switch (id)
        {
            case "TIME":
                if (TryReadUInt32(data, out uint seconds))
                    Statistics.LengthInSeconds = (int)Math.Min(seconds, int.MaxValue);
                break;
            case "ENDS":
                if (TryReadUInt32(data, out uint parsedEndState))
                    endState = parsedEndState;
                break;
            case "SDFX":
                suddenDisconnect = data.Length > 0 && data[0] != 0;
                break;
        }
    }

    private void ApplyResults()
    {
        var unmatchedPlayers = new List<PlayerStatistics>(Statistics.Players);
        bool hasAnyScore = false;

        for (int houseIndex = 0; houseIndex < houseResults.Length; houseIndex++)
        {
            HouseResult result = houseResults[houseIndex];
            if (!result.HasScore && string.IsNullOrWhiteSpace(result.Name) && !result.IsSpectator.HasValue)
            {
                continue;
            }

            PlayerStatistics player = null;
            if (!string.IsNullOrWhiteSpace(result.Name))
            {
                player = unmatchedPlayers.FirstOrDefault(candidate => string.Equals(candidate.Name, result.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (player == null && houseIndex < Statistics.Players.Count)
            {
                PlayerStatistics indexedPlayer = Statistics.Players[houseIndex];
                if (unmatchedPlayers.Contains(indexedPlayer))
                    player = indexedPlayer;
            }

            if (player == null)
                continue;

            unmatchedPlayers.Remove(player);

            if (result.IsSpectator.HasValue)
                player.WasSpectator = result.IsSpectator.Value;

            if (!result.HasScore)
                continue;

            hasAnyScore = true;
            player.SawEnd = true;
            player.Kills = result.BuildingsDestroyed + result.UnitsKilled;
            player.Losses = result.BuildingsLost + result.UnitsLost;
            player.Economy = result.SpiceHarvested;
            player.Won = result.FinishingPlace == 0 && !player.WasSpectator;
        }

        Statistics.SawCompletion =
            hasAnyScore
            && !suddenDisconnect
            && (!endState.HasValue || endState.Value is 0 or 1 or 2 or 5 or 6);
    }

    private static bool TryGetHouseIndex(
        string id,
        string prefix,
        out int houseIndex)
    {
        houseIndex = -1;
        if (id.Length != 4
            || !id.StartsWith(prefix, StringComparison.Ordinal)
            || !char.IsDigit(id[3]))
        {
            return false;
        }

        houseIndex = id[3] - '0';
        return houseIndex < MaxPlayerCount;
    }

    private static string ReadString(byte[] data) => Encoding.ASCII.GetString(data).TrimEnd('\0');

    private static bool TryParseInt(string value, out int result) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static bool TryReadUInt32(byte[] data, out uint value)
    {
        value = 0;
        if (data.Length < sizeof(uint))
            return false;

        value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, sizeof(uint)));
        return true;
    }
}
