using System.IO;
using System;
using Rampastring.Tools;

namespace ClientCore.Statistics.GameParsers
{
    public class RAStatsDumpParser : GenericMatchParser
    {
        public RAStatsDumpParser(MatchStatistics ms)
            : base(ms)
        {
        }

        public void ParseStats(string gamePath)
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

            var dump = new StatsDumpParser(statsPath);

            Statistics.AverageFPS = dump.AverageFPS;

            for (int i = 0; i < dump.PlayerNames.Length; i++)
            {
                string name = dump.PlayerNames[i];
                if (string.IsNullOrEmpty(name))
                    continue;

                var ps =
                    Statistics.GetEmptyPlayerByName(name) ??
                    Statistics.GetFirstEmptyPlayer();

                if (ps == null)
                    continue;

                ps.Name = name;

                /* ================= ECONOMY ================= */

                ps.Score = Math.Max(0, dump.PlayerMoneyHarvested[i]);
                //ps.Economy = 0; // RA score is meaningless

                /* ================= BUILT ================= */

                int built =
                    SumStructFields(dump.PlayerVehiclesBought[i]) +
                    SumStructFields(dump.PlayerInfantryBought[i]) +
                    SumStructFields(dump.PlayerPlanesBought[i]) +
                    SumStructFields(dump.PlayerVesselsBought[i]) +
                    SumStructFields(dump.PlayerBuildingsBought[i]);

                ps.Economy = built;

                /* ================= LEFT ================= */

                int left =
                    SumStructFields(dump.PlayerVehiclesLeft[i]) +
                    SumStructFields(dump.PlayerInfantryLeft[i]) +
                    SumStructFields(dump.PlayerPlanesLeft[i]) +
                    SumStructFields(dump.PlayerVesselsLeft[i]) +
                    SumStructFields(dump.PlayerBuildingsLeft[i]);

                /* ================= LOSSES ================= */

                ps.Losses = Math.Max(0, built - left);

                /* ================= KILLS ================= */

                ps.Kills =
                    SumStructFields(dump.PlayerVehiclesKilled[i]) +
                    SumStructFields(dump.PlayerInfantryKilled[i]) +
                    SumStructFields(dump.PlayerPlanesKilled[i]) +
                    SumStructFields(dump.PlayerVesselsKilled[i]) +
                    SumStructFields(dump.PlayerBuildingsKilled[i]);

                /* ================= WIN STATE ================= */

                ps.Won =
                    dump.PlayerDeadStates[i] == 0 &&
                    dump.PlayersResigned[i] != 1;

                ps.SawEnd = true;

                Logger.Log(
                    $"RA | {ps.Name} | Eco={ps.Economy} | Kills={ps.Kills} | Losses={ps.Losses} | Won={ps.Won}"
                );
            }

            Statistics.SawCompletion = true;
        }

        private static int SumStructFields<T>(T struc)
        {
            int sum = 0;
            var fields = typeof(T).GetFields();

            foreach (var f in fields)
            {
                if (f.FieldType == typeof(int))
                    sum += (int)f.GetValue(struc);
            }

            return sum;
        }
    }
}