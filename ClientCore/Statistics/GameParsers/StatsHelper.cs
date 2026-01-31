using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Statistics.GameParsers
{
    public static class StatsHelper
    {
        public static List<PlayerStats> BuildPlayerStatsFromDump(string filePath)
        {
            // Create parser instance
            StatsDumpParser parser = new StatsDumpParser(filePath);

            // Convert parser fields into PlayerStats objects
            List<PlayerStats> result = new List<PlayerStats>();

            for (int i = 0; i < parser.NumberOfPlayers; i++)
            {
                PlayerStats ps = new PlayerStats
                {
                    Name = parser.PlayerNames[i],
                    Side = parser.PlayerSides[i],
                    Credits = parser.PlayerCredits[i],
                    MoneyHarvested = parser.PlayerMoneyHarvested[i],
                    Quit = parser.PlayerQuitStates[i] != 0,
                    Dead = parser.PlayerDeadStates[i] != 0,
                    Spectator = parser.PlayerSpectatorStates[i] != 0,
                    Color = parser.PlayerColors[i],
                    // Add more fields if you want
                };

                result.Add(ps);
            }

            return result;
        }
    }
}