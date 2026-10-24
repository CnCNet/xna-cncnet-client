#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ClientCore.Statistics.GameParsers;

public sealed class Dune2000StatisticsParser : StatisticsDumpParserBase
{
    private sealed class HouseResult
    {
        public string? Name { get; set; }
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
    private uint? duration;
    private bool suddenDisconnect;

    public Dune2000StatisticsParser(MatchStatistics statistics, bool isLoaded) : base(statistics, isLoaded)
    {
    }

    protected override void ResetResults()
    {
        for (int houseIndex = 0; houseIndex < houseResults.Length; houseIndex++)
            houseResults[houseIndex] = new HouseResult();

        endState = null;
        duration = null;
        suddenDisconnect = false;
    }

    protected override void ReadRecord(string id, byte[] data)
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
                    duration = seconds;
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

    protected override void ApplyResults()
    {
        if (duration.HasValue)
            Statistics.LengthInSeconds = (int)Math.Min(duration.Value, int.MaxValue);

        var unmatchedPlayers = new List<PlayerStatistics>(Statistics.Players);
        bool hasAnyScore = false;

        for (int houseIndex = 0; houseIndex < houseResults.Length; houseIndex++)
        {
            HouseResult result = houseResults[houseIndex];
            if (!result.HasScore && string.IsNullOrWhiteSpace(result.Name) && !result.IsSpectator.HasValue)
            {
                continue;
            }

            PlayerStatistics? player = null;
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

    private static bool TryParseInt(string value, out int result) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
}