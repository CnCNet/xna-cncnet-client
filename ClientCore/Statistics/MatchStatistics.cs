using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ClientCore.Statistics.GameParsers;
using Rampastring.Tools;

namespace ClientCore.Statistics
{
    public class MatchStatistics
    {
        public MatchStatistics() { }

        public MatchStatistics(string gameVersion, int gameId, string mapName, string gameMode, int numHumans, bool mapIsCoop = false)
        {
            GameVersion = gameVersion;
            GameID = gameId;
            DateAndTime = DateTime.Now;
            MapName = mapName;
            GameMode = gameMode;
            NumberOfHumanPlayers = numHumans;
            MapIsCoop = mapIsCoop;
        }

        public List<PlayerStatistics> Players = new List<PlayerStatistics>();

        public int LengthInSeconds { get; set; }

        public DateTime DateAndTime { get; set; }

        public string GameVersion { get; set; }

        public string MapName { get; set; }

        public string GameMode { get; set; }

        public bool SawCompletion { get; set; }

        public int NumberOfHumanPlayers { get; set; }

        public int AverageFPS { get; set; }

        public int GameID { get; set; }

        public bool MapIsCoop { get; set; }

        public bool IsValidForStar { get; set; } = true;

        public void AddPlayer(string name, bool isLocal, bool isAI, bool isSpectator,
            int side, int team, int color, int aiLevel)
        {
            PlayerStatistics ps = new PlayerStatistics(name, isLocal, isAI, isSpectator,
                side, team, color, aiLevel);
            Players.Add(ps);
        }

        public void AddPlayer(PlayerStatistics ps)
        {
            Players.Add(ps);
        }

        public ValueTask ParseStatisticsAsync(string gamePath, bool isLoadedGame)
        {
            Logger.Log("Parsing game statistics.");

            LengthInSeconds = (int)(DateTime.Now - DateAndTime).TotalSeconds;

            var parser = new LogFileStatisticsParser(this, isLoadedGame);
            return parser.ParseStatisticsAsync(gamePath, ClientConfiguration.Instance.StatisticsLogFileName);
        }

        public PlayerStatistics GetEmptyPlayerByName(string playerName)
        {
            foreach (PlayerStatistics ps in Players)
            {
                if (ps.Name == playerName && ps.Losses == 0 && ps.Score == 0)
                    return ps;
            }

            return null;
        }

        public PlayerStatistics GetFirstEmptyPlayer()
        {
            foreach (PlayerStatistics ps in Players)
            {
                if (ps.Losses == 0 && ps.Score == 0)
                    return ps;
            }

            return null;
        }

        public int GetPlayerCount()
        {
            return Players.Count;
        }

        public PlayerStatistics GetPlayer(int index)
        {
            return Players[index];
        }

        public async ValueTask WriteAsync(Stream stream)
        {
            // Game length
            await stream.WriteIntAsync(LengthInSeconds).ConfigureAwait(false);

            // Game version, 8 bytes, ASCII
            await stream.WriteStringAsync(GameVersion, 8, Encoding.ASCII).ConfigureAwait(false);

            // Date and time, 8 bytes
            await stream.WriteLongAsync(DateAndTime.ToBinary()).ConfigureAwait(false);
            // SawCompletion, 1 byte
            await stream.WriteBoolAsync(SawCompletion).ConfigureAwait(false);
            // Number of players, 1 byte
            await stream.WriteAsync(new[] { Convert.ToByte(GetPlayerCount()) }, 0, 1).ConfigureAwait(false);
            // Average FPS, 4 bytes
            await stream.WriteIntAsync(AverageFPS).ConfigureAwait(false);
            // Map name, 128 bytes (64 chars), Unicode
            await stream.WriteStringAsync(MapName, 128).ConfigureAwait(false);
            // Game mode, 64 bytes (32 chars), Unicode
            await stream.WriteStringAsync(GameMode, 64).ConfigureAwait(false);
            // Unique game ID, 4 bytes
            await stream.WriteIntAsync(GameID).ConfigureAwait(false);
            // Whether game options were valid for earning a star, 1 byte
            await stream.WriteBoolAsync(IsValidForStar).ConfigureAwait(false);

            // Write player info
            for (int i = 0; i < GetPlayerCount(); i++)
            {
                PlayerStatistics ps = GetPlayer(i);
                await ps.WriteAsync(stream).ConfigureAwait(false);
            }
        }
    }
}