using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rampastring.Tools;

namespace ClientCore.Statistics.GameParsers
{
    public class LogFileStatisticsParser : GenericMatchParser
    {
        public LogFileStatisticsParser(MatchStatistics ms, bool isLoadedGame)
            : base(ms)
        {
            this.isLoadedGame = isLoadedGame;
        }

        private string economyString = "Economy"; // RA2/YR do not have economy stat, but a number of built objects.
        private readonly bool isLoadedGame;

        public async ValueTask ParseStatisticsAsync(string gamepath, string fileName)
        {
            if (ClientConfiguration.Instance.UseBuiltStatistic)
                economyString = "Built";

            FileInfo statisticsFileInfo = SafePath.GetFile(gamepath, fileName);

            if (!statisticsFileInfo.Exists)
            {
                Logger.Log("DTAStatisticsParser: Failed to read statistics: the log file does not exist.");
                return;
            }

            Logger.Log("Attempting to read statistics from " + fileName);

            try
            {
                using var reader = new StreamReader(statisticsFileInfo.OpenRead());

                string line;

                List<PlayerStatistics> takeoverAIs = new List<PlayerStatistics>();
                PlayerStatistics currentPlayer = null;

                bool sawCompletion = false;
                int numPlayersFound = 0;

                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (line.Contains(": Loser"))
                    {
                        // Player found, game saw completion
                        sawCompletion = true;
                        string playerName = line[..^7];
                        currentPlayer = Statistics.GetEmptyPlayerByName(playerName);

                        if (isLoadedGame && currentPlayer == null)
                            currentPlayer = Statistics.Players.Find(p => p.Name == playerName);

                        Logger.Log("Found player " + playerName);
                        numPlayersFound++;

                        if (currentPlayer == null && playerName == "Computer" && numPlayersFound <= Statistics.NumberOfHumanPlayers)
                        {
                            // The player has been taken over by an AI during the match
                            Logger.Log("Losing take-over AI found");
                            takeoverAIs.Add(new PlayerStatistics("Computer", false, true, false, 0, 10, 255, 1));
                            currentPlayer = takeoverAIs[^1];
                        }

                        if (currentPlayer != null)
                            currentPlayer.SawEnd = true;
                    }
                    else if (line.Contains(": Winner"))
                    {
                        // Player found, game saw completion
                        sawCompletion = true;
                        string playerName = line[..^8];
                        currentPlayer = Statistics.GetEmptyPlayerByName(playerName);

                        if (isLoadedGame && currentPlayer == null)
                            currentPlayer = Statistics.Players.Find(p => p.Name == playerName);

                        Logger.Log("Found player " + playerName);
                        numPlayersFound++;

                        if (currentPlayer == null && playerName == "Computer" && numPlayersFound <= Statistics.NumberOfHumanPlayers)
                        {
                            // The player has been taken over by an AI during the match
                            Logger.Log("Winning take-over AI found");
                            takeoverAIs.Add(new PlayerStatistics("Computer", false, true, false, 0, 10, 255, 1));
                            currentPlayer = takeoverAIs[^1];
                        }

                        if (currentPlayer != null)
                        {
                            currentPlayer.SawEnd = true;
                            currentPlayer.Won = true;
                        }
                    }
                    else if (line.Contains("Game loop finished. Average FPS"))
                    {
                        // Game loop finished. Average FPS = <integer>
                        string fpsString = line[34..];
                        Statistics.AverageFPS = Int32.Parse(fpsString);
                    }

                    if (currentPlayer == null || line.Length < 1)
                        continue;

                    line = line[1..];

                    if (line.StartsWith("Lost = "))
                        currentPlayer.Losses = Int32.Parse(line[7..]);
                    else if (line.StartsWith("Kills = "))
                        currentPlayer.Kills = Int32.Parse(line[8..]);
                    else if (line.StartsWith("Score = "))
                        currentPlayer.Score = Int32.Parse(line[8..]);
                    else if (line.StartsWith(economyString + " = "))
                        currentPlayer.Economy = Int32.Parse(line[(economyString.Length + 2)..]);
                }

                // Check empty players for take-over by AIs
                if (takeoverAIs.Count == 1)
                {
                    PlayerStatistics ai = takeoverAIs[0];

                    PlayerStatistics ps = Statistics.GetFirstEmptyPlayer();

                    ps.Losses = ai.Losses;
                    ps.Kills = ai.Kills;
                    ps.Score = ai.Score;
                    ps.Economy = ai.Economy;
                }
                else if (takeoverAIs.Count > 1)
                {
                    // If there's multiple take-over AI players, we have no way of figuring out
                    // which AI represents which player, so let's just add the AIs into the player list
                    // (then the user viewing the statistics can figure it out themselves)
                    for (int i = 0; i < takeoverAIs.Count; i++)
                    {
                        takeoverAIs[i].SawEnd = false;
                        Statistics.AddPlayer(takeoverAIs[i]);
                    }
                }

                Statistics.SawCompletion = sawCompletion;
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "DTAStatisticsParser: Error parsing statistics from match!");
            }
        }
    }
}