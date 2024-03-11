using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Rampastring.Tools;

namespace ClientCore.Statistics.GameParsers;

public class LogFileStatisticsParser : GenericMatchParser
{
    public LogFileStatisticsParser(MatchStatistics ms, bool isLoadedGame) : base(ms)
    {
        this.isLoadedGame = isLoadedGame;
    }

    private string fileName = "DTA.log";
    private static string _economyString; // RA2/YR do not have economy stat, but a number of built objects.
    private static string EconomyString => _economyString ??= ClientConfiguration.Instance.UseBuiltStatistic ? "Built" : "Economy";
    private readonly bool isLoadedGame;

    public void ParseStats(string gamepath, string fileName)
    {
        this.fileName = fileName;
        ParseStatistics(gamepath);
    }

    protected override void ParseStatistics(string gamepath)
    {
        FileInfo statisticsFileInfo = SafePath.GetFile(gamepath, fileName);

        if (!statisticsFileInfo.Exists)
        {
            Logger.Log("DTAStatisticsParser: Failed to read statistics: the log file does not exist.");
            return;
        }

        Logger.Log("Attempting to read statistics from " + fileName);

        try
        {
            using StreamReader reader = new(statisticsFileInfo.OpenRead());

            string line;

            List<PlayerStatistics> takeoverAIs = new();
            PlayerStatistics currentPlayer = null;

            bool sawCompletion = false;
            int numPlayersFound = 0;

            while ((line = reader.ReadLine()) != null)
            {
                bool? winner = null;
                byte skipCharCount = 0;
                if (line.Contains(": Loser"))
                {
                    winner = false;
                    skipCharCount = 7;
                }
                else if (line.Contains(": Winner"))
                {
                    winner = true;
                    skipCharCount = 8;
                }

                if (winner.HasValue)
                {
                    // Player found, game saw completion
                    sawCompletion = true;

                    string playerName = line.Substring(0, line.Length - skipCharCount);
                    currentPlayer = Statistics.GetEmptyPlayerByName(playerName);

                    if (isLoadedGame && currentPlayer == null)
                        currentPlayer = Statistics.Players.Find(p => p.Name == playerName);

                    Logger.Log("Found player " + playerName);
                    numPlayersFound++;

                    if (currentPlayer == null && playerName == "Computer" && numPlayersFound <= Statistics.NumberOfHumanPlayers)
                    {
                        // The player has been taken over by an AI during the match
                        Logger.Log((winner.Value ? "Winning" : "Losing") + " take-over AI found");
                        takeoverAIs.Add(new PlayerStatistics("Computer", false, true, false, 0, 10, 255, 1));
                        currentPlayer = takeoverAIs[takeoverAIs.Count - 1];
                    }

                    if (currentPlayer != null)
                    {
                        currentPlayer.SawEnd = true;
                        currentPlayer.Won = winner.Value;
                    }
                }
                else if (line.Contains("Game loop finished. Average FPS"))
                {
                    // Game loop finished. Average FPS = <integer>
                    string fpsString = line.Substring(34);
                    Statistics.AverageFPS = int.Parse(fpsString, CultureInfo.InvariantCulture);
                }

                if (currentPlayer == null || line.Length < 1)
                    continue;

                line = line.Substring(1);

                if (line.StartsWith("Lost = ", StringComparison.CurrentCulture))
                    currentPlayer.Losses = int.Parse(line.Substring(7), CultureInfo.InvariantCulture);
                else if (line.StartsWith("Kills = ", StringComparison.CurrentCulture))
                    currentPlayer.Kills = int.Parse(line.Substring(8), CultureInfo.InvariantCulture);
                else if (line.StartsWith("Score = ", StringComparison.CurrentCulture))
                    currentPlayer.Score = int.Parse(line.Substring(8), CultureInfo.InvariantCulture);
                else if (line.StartsWith(EconomyString + " = ", StringComparison.CurrentCulture))
                    currentPlayer.Economy = int.Parse(line.Substring(EconomyString.Length + 2), CultureInfo.InvariantCulture);
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
            Logger.Log("DTAStatisticsParser: Error parsing statistics from match! Message: " + ex.Message);
        }
    }
}
