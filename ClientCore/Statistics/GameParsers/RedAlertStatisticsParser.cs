#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClientCore.Statistics.GameParsers
{
    /// <summary>
    /// Reads the Westwood statistics packet written by the CnCNet Red Alert spawner.
    /// </summary>
    public sealed class RedAlertStatisticsParser : MatchParserBase
    {
        private sealed class HouseResult
        {
            public string? Name { get; set; }
            public bool HasData { get; set; }
            public bool? IsSpectator { get; set; }
            public bool? IsDefeated { get; set; }
            public bool? HasResigned { get; set; }
            public bool IsDisconnected { get; set; }
            public uint? Allies { get; set; }
            public uint? Harvested { get; set; }
            public Dictionary<string, uint[]> Built { get; } = new Dictionary<string, uint[]>();
            public Dictionary<string, uint[]> Remaining { get; } = new Dictionary<string, uint[]>();
            public Dictionary<string, uint[]> Killed { get; } = new Dictionary<string, uint[]>();

            public bool HasLost => IsDefeated == true || HasResigned == true;
        }

        private readonly HouseResult[] houseResults = new HouseResult[MaxPlayerCount];
        private uint? duration;
        private uint? averageFps;
        private byte? completion;
        private bool connectionLost;
        private bool outOfSync;

        public RedAlertStatisticsParser(MatchStatistics statistics, bool isLoadedGame)
            : base(statistics, isLoadedGame)
        {
        }

        protected override void ParseStatistics(string gamePath) => ParseStatisticsDump(gamePath, ResetResults, ReadRecord, ApplyResults);

        private void ResetResults()
        {
            for (int i = 0; i < houseResults.Length; i++)
                houseResults[i] = new HouseResult();

            duration = null;
            averageFps = null;
            completion = null;
            connectionLost = false;
            outOfSync = false;
        }

        private void ReadRecord(string id, byte[] data)
        {
            switch (id)
            {
                case "DURA":
                    duration = ReadUInt32(data);
                    return;
                case "AFPS":
                    averageFps = ReadUInt32(data);
                    return;
                case "CMPL":
                    if (data.Length != 1)
                        throw new InvalidDataException("Invalid Red Alert completion field.");

                    completion = data[0];
                    return;
                case "SDFX":
                    connectionLost = ReadFlag(data);
                    return;
                case "OOSY":
                    outOfSync = ReadFlag(data);
                    return;
            }

            string prefix = id.Substring(0, 3);
            if (!TryGetHouseIndex(id, prefix, out int houseIndex, firstHouseNumber: 1))
                return;

            HouseResult result = houseResults[houseIndex];
            switch (prefix)
            {
                case "NAM":
                    result.Name = ReadString(data);
                    break;
                case "SPC":
                    result.IsSpectator = ReadFlag(data);
                    break;
                case "DED":
                    result.IsDefeated = ReadFlag(data);
                    break;
                case "RSG":
                    result.HasResigned = ReadFlag(data);
                    break;
                case "CON":
                    result.IsDisconnected = ReadFlag(data);
                    break;
                case "ALY":
                    result.Allies = ReadUInt32(data);
                    break;
                case "HRV":
                    result.Harvested = ReadUInt32(data);
                    break;
                case "INB":
                case "UNB":
                case "PLB":
                case "BLB":
                case "VSB":
                    result.Built[prefix.Substring(0, 2)] = ReadCounters(data);
                    break;
                case "INL":
                case "UNL":
                case "PLL":
                case "BLL":
                case "VSL":
                    result.Remaining[prefix.Substring(0, 2)] = ReadCounters(data);
                    break;
                case "INK":
                case "UNK":
                case "PLK":
                case "BLK":
                case "VSK":
                    result.Killed[prefix.Substring(0, 2)] = ReadCounters(data);
                    break;
                default:
                    return;
            }

            result.HasData = true;
        }

        private void ApplyResults()
        {
            // Validate paired arrays before changing any match or player statistics.
            int[] losses = houseResults.Select(CountLosses).ToArray();
            var playersByHouse = new PlayerStatistics?[MaxPlayerCount];
            var unmatchedPlayers = new List<PlayerStatistics>(Statistics.Players);

            // Match names first so a missing or renamed house cannot claim another player's slot.
            for (int i = 0; i < houseResults.Length; i++)
            {
                string? name = houseResults[i].Name;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                PlayerStatistics? player = unmatchedPlayers.Find(candidate =>
                    string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
                if (player == null)
                    continue;

                playersByHouse[i] = player;
                unmatchedPlayers.Remove(player);
            }

            for (int i = 0; i < houseResults.Length && i < Statistics.Players.Count; i++)
            {
                if (playersByHouse[i] != null || !houseResults[i].HasData)
                    continue;

                PlayerStatistics player = Statistics.Players[i];
                if (unmatchedPlayers.Contains(player)
                    && (string.IsNullOrWhiteSpace(houseResults[i].Name) || player.IsAI))
                {
                    playersByHouse[i] = player;
                    unmatchedPlayers.Remove(player);
                }
            }

            for (int i = 0; i < houseResults.Length; i++)
            {
                // Older dumps may omit SPC; retain the spectator status recorded by the lobby.
                houseResults[i].IsSpectator ??= playersByHouse[i]?.WasSpectator;
            }

            int[] participants = Enumerable.Range(0, MaxPlayerCount)
                .Where(i => houseResults[i].HasData && houseResults[i].IsSpectator != true).ToArray();
            bool missingParticipants = Statistics.Players.Any(player => !player.WasSpectator && !playersByHouse.Contains(player));
            bool unreliable = connectionLost || outOfSync
                || completion is 0 or 3 or 6
                || participants.Any(i => houseResults[i].IsDisconnected);
            bool isDraw = completion == 64 && !unreliable;
            bool hasTwoPlayerResult = !missingParticipants && participants.Length == 2 && participants[0] == 0 && participants[1] == 1
                && playersByHouse[0] is { IsAI: false } && playersByHouse[1] is { IsAI: false }
                && completion is 1 or 2 or 4 or 5;
            bool[] winners = FindWinners(participants, unreliable || isDraw || missingParticipants, hasTwoPlayerResult);
            bool sawCompletion = false;

            for (int i = 0; i < houseResults.Length; i++)
            {
                HouseResult result = houseResults[i];
                PlayerStatistics? player = playersByHouse[i];
                if (player == null)
                    continue;

                if (result.IsSpectator.HasValue)
                    player.WasSpectator = result.IsSpectator.Value;

                player.Kills = ClampCounter(result.Killed.Values.Sum(counters => counters.Sum(value => (long)value)));
                player.Losses = losses[i];
                if (result.Harvested.HasValue)
                    player.Economy = ClampCounter(result.Harvested.Value);

                player.Won = winners[i] && !player.WasSpectator;
                player.SawEnd = !unreliable && !player.WasSpectator
                    && (result.HasLost || player.Won || isDraw || hasTwoPlayerResult);
                sawCompletion |= player.SawEnd;
            }

            // Some RA builds leave DURA at zero. Preserve the elapsed time measured
            // by MatchStatistics when the game does not provide a usable duration.
            if (duration is > 0)
                Statistics.LengthInSeconds = ClampCounter(duration.Value);

            if (averageFps.HasValue)
                Statistics.AverageFPS = ClampCounter(averageFps.Value);

            Statistics.SawCompletion = sawCompletion;
        }

        private bool[] FindWinners(int[] participants, bool unreliable, bool hasTwoPlayerResult)
        {
            var winners = new bool[MaxPlayerCount];
            if (unreliable)
                return winners;

            // The original CMPL codes describe a two-player game only.
            if (hasTwoPlayerResult)
            {
                int winner = completion is 1 or 2 ? 0 : 1;
                winners[winner] = !houseResults[winner].HasLost;

                return winners;
            }

            // CnCNet dumps commonly leave CMPL at 0xFF. RA ends the game when all
            // surviving houses are mutually allied. Defeated allies do not win.
            if (participants.Length < 2
                || participants.Any(i => !houseResults[i].IsDefeated.HasValue || !houseResults[i].HasResigned.HasValue)
                || !participants.Any(i => houseResults[i].HasLost))
            {
                return winners;
            }

            int[] survivors = participants.Where(i => !houseResults[i].HasLost).ToArray();
            foreach (int house in survivors)
            {
                foreach (int other in survivors)
                {
                    // Multiplayer house IDs start at HOUSE_MULTI1 (12).
                    if (house != other && (houseResults[house].Allies.GetValueOrDefault() & (1u << (12 + other))) == 0)
                        return winners;
                }
            }

            foreach (int house in survivors)
                winners[house] = true;

            return winners;
        }

        private static int CountLosses(HouseResult result)
        {
            long losses = 0;
            foreach (KeyValuePair<string, uint[]> category in result.Built)
            {
                if (!result.Remaining.TryGetValue(category.Key, out uint[]? remaining))
                    continue;

                if (category.Value.Length != remaining.Length)
                    throw new InvalidDataException("Red Alert built and remaining counters have different lengths.");

                for (int i = 0; i < remaining.Length; i++)
                    losses += Math.Max(0L, (long)category.Value[i] - remaining[i]);
            }

            // RA has no separate loss counter. Removals include deployments and sales.
            return ClampCounter(losses);
        }

        private static uint ReadUInt32(byte[] data)
        {
            if (data.Length != sizeof(uint) || !TryReadUInt32(data, out uint value))
                throw new InvalidDataException("Invalid Red Alert integer field.");

            return value;
        }

        private static bool ReadFlag(byte[] data)
        {
            // CnCNet's per-house flags are uint32; packet-level flags are bytes.
            if (data.Length == 1)
                return data[0] != 0;

            return ReadUInt32(data) != 0;
        }

        private static uint[] ReadCounters(byte[] data)
        {
            if (data.Length % sizeof(uint) != 0)
                throw new InvalidDataException("Invalid Red Alert counter array length.");

            var counters = new uint[data.Length / sizeof(uint)];
            for (int i = 0; i < counters.Length; i++)
                counters[i] = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(i * sizeof(uint), sizeof(uint)));

            return counters;
        }

        private static int ClampCounter(long value) => (int)Math.Min(value, int.MaxValue);
    }
}
