using ClientCore.Statistics;
using ClientCore; 
using System.Collections.Generic;
using ClientCore.Enums;
namespace ClientCore.Statistics.Extensions
{
    public static class RAMatchStatisticsHelper
    {
        public static bool IsRA(this MatchStatistics match)
        {   //Only run if clientgametype=RA
            return  ClientConfiguration.Instance.ClientGameType == ClientType.RA;
        }
        public static List<RAPlayerStatistics> GetRAPlayers(this MatchStatistics match)
        {
            if (!match.IsRA())
                return null;

            var raPlayers = new List<RAPlayerStatistics>();
            foreach (var ps in match.Players)
            {
                raPlayers.Add(new RAPlayerStatistics(ps));
            }
            return raPlayers;
        }

        public static int GetTotalRAKills(this MatchStatistics match)
        {
            if (!match.IsRA())
                return 0;

            int total = 0;
            foreach (var player in match.Players)
            {
                if (!player.IsAI && !player.WasSpectator)
                    total += player.Kills;
            }
            return total;
        }

        public static int GetTotalRAEconomy(this MatchStatistics match)
        {
            if (!match.IsRA())
                return 0;

            int total = 0;
            foreach (var player in match.Players)
            {
                if (!player.IsAI && !player.WasSpectator)
                    total += player.Economy;
            }
            return total;
        }
    }
}