using ClientCore.Statistics;

namespace ClientCore.Statistics.Extensions
{
    public class RAPlayerStatistics
    {
        public RAPlayerStatistics(PlayerStatistics baseStats)
        {
            BaseStats = baseStats;
        }

        public PlayerStatistics BaseStats { get; }

        // Example of RA-specific stats
        public int UnitsBuilt { get; set; }
        public int BuildingsDestroyed { get; set; }
        public int ResourcesCollected { get; set; }
    }
}