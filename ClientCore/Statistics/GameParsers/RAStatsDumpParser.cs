using System.IO;
using Rampastring.Tools;

namespace ClientCore.Statistics.GameParsers
{
    public class RAStatsDumpParser : GenericMatchParser
    {
        public RAStatsDumpParser(MatchStatistics ms)
            : base(ms)
        {
        }

        // OPTIONAL public entry point if needed later
        public void Parse(string gamePath)
        {
            ParseStatistics(gamePath);
        }

        protected override void ParseStatistics(string gamePath)
        {
            string statsPath = Path.Combine(gamePath, "stats.dmp");

            if (!File.Exists(statsPath))
            {
                Logger.Log("RAStatsDumpParser: stats.dmp not found.");
                return;
            }

            Logger.Log("Parsing Red Alert stats.dmp");

            var parser = new StatsDumpParser(statsPath);

            int playerCount = parser.PlayerNames.Count;

            for (int i = 0; i < playerCount; i++)
            {
                string name = parser.PlayerNames[i];

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var ps =
                    Statistics.GetEmptyPlayerByName(name) ??
                    Statistics.GetFirstEmptyPlayer();

                if (ps == null)
                    continue;

                // Map RA stats → CnCNet stats
                ps.Score = parser.Credits[i] + parser.MoneyHarvested[i];
                ps.Kills = parser.UnitsKilled[i] + parser.BuildingsKilled[i];
                ps.Won   = parser.Won[i];

                ps.SawEnd = true;
            }

            Statistics.SawCompletion = true;
        }
    }
}
