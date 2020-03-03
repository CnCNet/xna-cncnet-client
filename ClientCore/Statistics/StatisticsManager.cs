using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Rampastring.Tools;

namespace ClientCore.Statistics
{
    public class StatisticsManager : GenericStatisticsManager
    {
        private const string VERSION = "1.06";
        private const string SCORE_FILE_PATH = "Client\\dscore.dat";
        private const string OLD_SCORE_FILE_PATH = "dscore.dat";
        private static StatisticsManager _instance;

        public event EventHandler GameAdded;


        public static StatisticsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new StatisticsManager();
                return _instance;
            }
        }

        public override void ReadStatistics(string gamePath)
        {
            if (!File.Exists(gamePath + SCORE_FILE_PATH))
            {
                Logger.Log("Skipping reading statistics because the file doesn't exist!");
                return;
            }

            Logger.Log("Reading statistics.");

            Statistics.Clear();

            bool resave = ReadFile(gamePath + OLD_SCORE_FILE_PATH);
            bool resaveNew = ReadFile(gamePath + SCORE_FILE_PATH);

            PurgeStats();

            if (resave || resaveNew)
            {
                if (File.Exists(gamePath + OLD_SCORE_FILE_PATH))
                {
                    File.Copy(gamePath + OLD_SCORE_FILE_PATH, gamePath + "Client\\dscore_old.dat");
                    File.Delete(gamePath + OLD_SCORE_FILE_PATH);
                }

                SaveDatabase();
            }
        }

        /// <summary>
        /// Reads a statistics file.
        /// </summary>
        /// <param name="filePath">The path to the statistics file.</param>
        /// <returns>A bool that determines whether the database should be re-saved.</returns>
        private bool ReadFile(string filePath)
        {
            bool returnValue = false;

            try
            {
                string databaseVersion = GetStatDatabaseVersion(filePath);

                if (databaseVersion == null)
                    return false; // No score database exists

                switch (databaseVersion)
                {
                    case "1.00":
                    case "1.01":
                        ReadDatabase(filePath, 0);
                        returnValue = true;
                        break;
                    case "1.02":
                        ReadDatabase(filePath, 2);
                        returnValue = true;
                        break;
                    case "1.03":
                        ReadDatabase(filePath, 3);
                        returnValue = true;
                        break;
                    case "1.04":
                        ReadDatabase(filePath, 4);
                        returnValue = true;
                        break;
                    case "1.05":
                        ReadDatabase(filePath, 5);
                        returnValue = true;
                        break;
                    case "1.06":
                        ReadDatabase(filePath, 6);
                        break;
                    default:
                        throw new InvalidDataException("Invalid version for " + filePath + ": " + databaseVersion);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading statistics: " + ex.Message);
            }

            return returnValue;
        }

        private void ReadDatabase(string filePath, int version)
        {
            // TODO split this function with the MatchStatistics and PlayerStatistics classes

            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                {
                    fs.Position = 4; // Skip version
                    byte[] readBuffer = new byte[128];
                    fs.Read(readBuffer, 0, 4); // First 4 bytes following the version mean the amount of games
                    int gameCount = BitConverter.ToInt32(readBuffer, 0);

                    for (int i = 0; i < gameCount; i++)
                    {
                        MatchStatistics ms = new MatchStatistics();

                        // First 4 bytes of game info is the length in seconds
                        fs.Read(readBuffer, 0, 4);
                        int lengthInSeconds = BitConverter.ToInt32(readBuffer, 0);
                        ms.LengthInSeconds = lengthInSeconds;
                        // Next 8 are the game version
                        fs.Read(readBuffer, 0, 8);
                        ms.GameVersion = System.Text.Encoding.ASCII.GetString(readBuffer, 0, 8);
                        // Then comes the date and time, also 8 bytes
                        fs.Read(readBuffer, 0, 8);
                        long dateData = BitConverter.ToInt64(readBuffer, 0);
                        ms.DateAndTime = DateTime.FromBinary(dateData);
                        // Then one byte for SawCompletion
                        fs.Read(readBuffer, 0, 1);
                        ms.SawCompletion = Convert.ToBoolean(readBuffer[0]);
                        // Then 1 byte for the amount of players
                        fs.Read(readBuffer, 0, 1);
                        int playerCount = readBuffer[0];
                        if (version > 0)
                        {
                            // 4 bytes for average FPS
                            fs.Read(readBuffer, 0, 4);
                            ms.AverageFPS = BitConverter.ToInt32(readBuffer, 0);
                        }

                        int mapNameLength = 64;

                        if (version > 3)
                        {
                            mapNameLength = 128;
                        }

                        // Map name, 64 or 128 bytes of Unicode depending on version
                        fs.Read(readBuffer, 0, mapNameLength);
                        ms.MapName = Encoding.Unicode.GetString(readBuffer).Replace("\0", "");

                        // Game mode, 64 bytes
                        fs.Read(readBuffer, 0, 64);
                        ms.GameMode = Encoding.Unicode.GetString(readBuffer, 0, 64).Replace("\0", "");

                        if (version > 2)
                        {
                            // Unique game ID, 32 bytes (int32)
                            fs.Read(readBuffer, 0, 4);
                            ms.GameID = BitConverter.ToInt32(readBuffer, 0);
                        }

                        if (version > 5)
                        {
                            fs.Read(readBuffer, 0, 1);
                            ms.IsValidForStar = Convert.ToBoolean(readBuffer[0]);
                        }

                        // Player info comes right after the general match info
                        for (int j = 0; j < playerCount; j++)
                        {
                            PlayerStatistics ps = new PlayerStatistics();

                            if (version > 4)
                            {
                                // Economy is shared for the Built stat in YR
                                fs.Read(readBuffer, 0, 4);
                                ps.Economy = BitConverter.ToInt32(readBuffer, 0);
                            }
                            else
                            {
                                // Economy is between 0 and 100 in old versions, so it takes only one byte
                                fs.Read(readBuffer, 0, 1);
                                ps.Economy = readBuffer[0];
                            }

                            // IsAI is a bool, so obviously one byte
                            fs.Read(readBuffer, 0, 1);
                            ps.IsAI = Convert.ToBoolean(readBuffer[0]);
                            // IsLocalPlayer is also a bool
                            fs.Read(readBuffer, 0, 1);
                            ps.IsLocalPlayer = Convert.ToBoolean(readBuffer[0]);
                            // Kills take 4 bytes
                            fs.Read(readBuffer, 0, 4);
                            ps.Kills = BitConverter.ToInt32(readBuffer, 0);
                            // Losses also take 4 bytes
                            fs.Read(readBuffer, 0, 4);
                            ps.Losses = BitConverter.ToInt32(readBuffer, 0);
                            // 32 bytes for the name
                            fs.Read(readBuffer, 0, 32);
                            ps.Name = System.Text.Encoding.Unicode.GetString(readBuffer, 0, 32);
                            ps.Name = ps.Name.Replace("\0", String.Empty);
                            // 1 byte for SawEnd
                            fs.Read(readBuffer, 0, 1);
                            ps.SawEnd = Convert.ToBoolean(readBuffer[0]);
                            // 4 bytes for Score
                            fs.Read(readBuffer, 0, 4);
                            ps.Score = BitConverter.ToInt32(readBuffer, 0);
                            // 1 byte for Side
                            fs.Read(readBuffer, 0, 1);
                            ps.Side = readBuffer[0];
                            // 1 byte for Team
                            fs.Read(readBuffer, 0, 1);
                            ps.Team = readBuffer[0];
                            if (version > 2)
                            {
                                // 1 byte for Color
                                fs.Read(readBuffer, 0, 1);
                                ps.Color = readBuffer[0];
                            }
                            // 1 byte for WasSpectator
                            fs.Read(readBuffer, 0, 1);
                            ps.WasSpectator = Convert.ToBoolean(readBuffer[0]);
                            // 1 byte for Won
                            fs.Read(readBuffer, 0, 1);
                            ps.Won = Convert.ToBoolean(readBuffer[0]);
                            // 1 byte for AI level
                            fs.Read(readBuffer, 0, 1);
                            ps.AILevel = readBuffer[0];

                            ms.AddPlayer(ps);

                            if (!ps.IsAI)
                                ms.NumberOfHumanPlayers++;
                        }

                        if (ms.Players.Find(p => p.IsLocalPlayer && !p.IsAI) == null)
                            continue;

                        Statistics.Add(ms);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Reading the statistics file failed! Message: " + ex.Message);
            }
        }

        public void PurgeStats()
        {
            int removedCount = 0;

            for (int i = 0; i < Statistics.Count; i++)
            {
                if (Statistics[i].LengthInSeconds < 60)
                {
                    Logger.Log("Removing match on " + Statistics[i].MapName + " because it's too short.");
                    Statistics.RemoveAt(i);
                    i--;
                    removedCount++;
                }
            }

            if (removedCount > 0)
                SaveDatabase();
        }

        public void ClearDatabase()
        {
            Statistics.Clear();
            CreateDummyFile();
        }

        public void AddMatchAndSaveDatabase(bool addMatch, MatchStatistics ms)
        {
            // Skip adding stats if the game only had one player, make exception for co-op since it doesn't recognize pre-placed houses as players.
            if (ms.GetPlayerCount() <= 1 && !ms.MapIsCoop)
            {
                Logger.Log("Skipping adding match to statistics because game only had one player.");
                return;
            }

            if (ms.LengthInSeconds < 60)
            {
                Logger.Log("Skipping adding match to statistics because the game was cancelled.");
                return;
            }

            if (addMatch)
            {
                Statistics.Add(ms);
                GameAdded?.Invoke(this, EventArgs.Empty);
            }

            if (!File.Exists(ProgramConstants.GamePath + SCORE_FILE_PATH))
            {
                CreateDummyFile();
            }

            Logger.Log("Writing game info to statistics file.");

            using (FileStream fs = File.Open(ProgramConstants.GamePath + SCORE_FILE_PATH, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Position = 4; // First 4 bytes after the version mean the amount of games
                fs.WriteInt(Statistics.Count);

                fs.Position = fs.Length;
                ms.Write(fs);
            }

            Logger.Log("Finished writing statistics.");
        }

        private void CreateDummyFile()
        {
            Logger.Log("Creating empty statistics file.");

            StreamWriter sw = new StreamWriter(File.Create(ProgramConstants.GamePath + SCORE_FILE_PATH));
            sw.Write(VERSION);
            sw.Close();
        }

        /// <summary>
        /// Deletes the statistics file on the file system and rewrites it.
        /// </summary>
        public void SaveDatabase()
        {
            File.Delete(ProgramConstants.GamePath + SCORE_FILE_PATH);
            CreateDummyFile();

            using (FileStream fs = File.Open(ProgramConstants.GamePath + SCORE_FILE_PATH, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Position = 4; // First 4 bytes after the version mean the amount of games
                fs.WriteInt(Statistics.Count);

                foreach (MatchStatistics ms in Statistics)
                {
                    ms.Write(fs);
                }
            }
        }

        public bool HasBeatCoOpMap(string mapName, string gameMode)
        {
            List<MatchStatistics> matches = new List<MatchStatistics>();

            // Filter out unfitting games
            foreach (MatchStatistics ms in Statistics)
            {
                if (ms.SawCompletion &&
                    ms.MapName == mapName &&
                    ms.GameMode == gameMode)
                {
                    if (ms.Players[0].Won)
                        return true;
                }
            }

            return false;
        }

        public int GetCoopRankForDefaultMap(string mapName, int requiredPlayerCount)
        {
            List<MatchStatistics> matches = new List<MatchStatistics>();

            // Filter out unfitting games
            foreach (MatchStatistics ms in Statistics)
            {
                if (!ms.SawCompletion)
                    continue;

                if (!ms.IsValidForStar)
                    continue;

                if (ms.MapName != mapName)
                    continue;

                if (ms.Players.Count != requiredPlayerCount)
                    continue;

                if (ms.Players.Count(ps => !ps.IsAI && !ps.WasSpectator) > 1 &&
                    ms.Players.Find(ps => ps.IsAI) != null)
                    matches.Add(ms);
            }

            int rank = -1;

            foreach (MatchStatistics ms in matches)
            {
                rank = Math.Max(rank, GetRankForCoopMatch(ms));
            }

            return rank;
        }

        int GetRankForCoopMatch(MatchStatistics ms)
        {
            PlayerStatistics localPlayer = ms.Players.Find(p => p.IsLocalPlayer);

            if (localPlayer == null || !localPlayer.Won)
                return -1;

            if (ms.Players.Find(p => p.WasSpectator) != null)
                return -1; // Don't allow matches with spectators

            if (ms.Players.Count(p => !p.IsAI && p.Team != localPlayer.Team) > 0)
                return -1; // Don't allow matches with human players who were on a different team

            if (ms.Players.Find(p => p.Team == 0) != null)
                return -1; // Matches with non-allied players are discarded

            if (ms.Players.All(ps => ps.Team == localPlayer.Team))
                return -1; // Discard matches that had no enemies

            int[] teamMemberCounts = new int[5];
            int lowestEnemyAILevel = 2;
            int highestAllyAILevel = 0;

            for (int i = 0; i < ms.Players.Count; i++)
            {
                PlayerStatistics ps = ms.GetPlayer(i);

                teamMemberCounts[ps.Team]++;

                if (!ps.IsAI)
                {
                    continue;
                }

                if (ps.Team > 0 && ps.Team == localPlayer.Team)
                {
                    if (ps.AILevel > highestAllyAILevel)
                        highestAllyAILevel = ps.AILevel;
                }
                else
                {
                    if (ps.AILevel < lowestEnemyAILevel)
                        lowestEnemyAILevel = ps.AILevel;
                }
            }

            if (lowestEnemyAILevel < highestAllyAILevel)
            {
                // Check that the player's AI allies weren't stronger 
                return -1;
            }

            // Check that all teams had at least as many players
            // as the local player's team
            int allyCount = teamMemberCounts[localPlayer.Team];

            for (int i = 1; i < 5; i++)
            {
                if (i == localPlayer.Team)
                    continue;

                if (teamMemberCounts[i] > 0)
                {
                    if (teamMemberCounts[i] < allyCount)
                        return -1;
                }
            }

            return lowestEnemyAILevel;
        }

        public bool HasWonMapInPvP(string mapName, string gameMode, int requiredPlayerCount)
        {
            List<MatchStatistics> matches = new List<MatchStatistics>();

            foreach (MatchStatistics ms in Statistics)
            {
                if (!ms.SawCompletion)
                    continue;

                if (!ms.IsValidForStar)
                    continue;

                if (ms.MapName != mapName)
                    continue;

                if (ms.GameMode != gameMode)
                    continue;

                if (ms.Players.Count(ps => !ps.WasSpectator) != requiredPlayerCount)
                    continue;

                if (ms.Players.Find(ps => ps.IsAI) != null)
                    continue;

                PlayerStatistics localPlayer = ms.Players.Find(p => p.IsLocalPlayer);

                if (localPlayer == null)
                    continue;

                if (localPlayer.WasSpectator)
                    continue;

                if (!localPlayer.Won)
                    continue;

                int[] teamMemberCounts = new int[5];

                ms.Players.FindAll(ps => !ps.WasSpectator).ForEach(ps => teamMemberCounts[ps.Team]++);

                if (localPlayer.Team > 0)
                {
                    int lowestEnemyTeamMemberCount = int.MaxValue;

                    for (int i = 1; i < 5; i++)
                    {
                        if (i != localPlayer.Team && teamMemberCounts[i] > 0)
                        {
                            if (teamMemberCounts[i] < lowestEnemyTeamMemberCount)
                                lowestEnemyTeamMemberCount = teamMemberCounts[i];
                        }
                    }

                    if (lowestEnemyTeamMemberCount > teamMemberCounts[localPlayer.Team])
                        continue;

                    return true;
                }

                if (ms.Players.Count(ps => !ps.WasSpectator) > 1)
                    return true;
            }

            return false;
        }

        public int GetSkirmishRankForDefaultMap(string mapName, int requiredPlayerCount)
        {
            List<MatchStatistics> matches = new List<MatchStatistics>();

            // Filter out unfitting games
            foreach (MatchStatistics ms in Statistics)
            {
                if (ms.SawCompletion && 
                    ms.IsValidForStar &&
                    ms.MapName == mapName &&
                    ms.Players.Count == requiredPlayerCount &&
                    ms.Players.Count(p => !p.IsAI) == 1)
                    matches.Add(ms);
            }

            int rank = -1;

            foreach (MatchStatistics ms in matches)
            {
                // TODO This code turned out pretty ugly, should design it better

                PlayerStatistics localPlayer = ms.Players.Find(p => p.IsLocalPlayer);

                if (localPlayer == null || !localPlayer.Won)
                    continue;

                int[] teamMemberCounts = new int[5];
                int lowestEnemyAILevel = 2;
                int highestAllyAILevel = 0;

                for (int i = 0; i < ms.Players.Count; i++)
                {
                    PlayerStatistics ps = ms.GetPlayer(i);

                    teamMemberCounts[ps.Team]++;

                    if (ps.IsLocalPlayer)
                    {
                        continue;
                    }

                    if (ps.Team > 0 && ps.Team == localPlayer.Team)
                    {
                        if (ps.AILevel > highestAllyAILevel)
                            highestAllyAILevel = ps.AILevel;
                    }
                    else
                    {
                        if (ps.AILevel < lowestEnemyAILevel)
                            lowestEnemyAILevel = ps.AILevel;
                    }
                }

                if (lowestEnemyAILevel < highestAllyAILevel)
                {
                    // Check that the player's AI allies weren't stronger 
                    continue;
                }

                if (localPlayer.Team > 0)
                {
                    // Check that all teams had at least as many players as the human player's team

                    int allyCount = teamMemberCounts[localPlayer.Team];
                    bool pass = true;

                    for (int i = 1; i < 5; i++)
                    {
                        if (i == localPlayer.Team)
                            continue;

                        if (teamMemberCounts[i] > 0)
                        {
                            if (teamMemberCounts[i] < allyCount)
                            {
                                // The enemy team has fewer players than the player's team
                                pass = false;
                                break;
                            }
                        }
                    }

                    if (!pass)
                        continue;

                    // Check that there is a team other than the players' team that is at least as large
                    pass = false;
                    for (int i = 1; i < 5; i++)
                    {
                        if (i == localPlayer.Team)
                            continue;

                        if (teamMemberCounts[i] >= allyCount)
                        {
                            pass = true;
                            break;
                        }
                    }

                    if (!pass)
                        continue;
                }

                if (rank < lowestEnemyAILevel)
                {
                    rank = lowestEnemyAILevel;

                    if (rank == 2)
                        return rank; // Best possible rank
                }
            }

            return rank;
        }

        public bool IsGameIdUnique(int gameId)
        {
            return Statistics.Find(m => m.GameID == gameId) == null;
        }

        public MatchStatistics GetMatchWithGameID(int gameId)
        {
            return Statistics.Find(m => m.GameID == gameId);
        }

    }
}
