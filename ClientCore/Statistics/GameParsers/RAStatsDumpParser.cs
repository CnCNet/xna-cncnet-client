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

            foreach (var ra in parser.Players)
            {
                var ps =
                    Statistics.GetEmptyPlayerByName(ra.Name) ??
                    Statistics.GetFirstEmptyPlayer();

                if (ps == null)
                    continue;

                ps.Score = ra.Credits + ra.MoneyHarvested;
                ps.Kills = ra.UnitsKilled + ra.BuildingsKilled;
                ps.UnitsBuilt = ra.UnitsBuilt;
                ps.BuildingsBuilt = ra.BuildingsBuilt;
                ps.Won = ra.Won;
                ps.SawEnd = true;
            }

            Statistics.SawCompletion = true;
        }
    }
}