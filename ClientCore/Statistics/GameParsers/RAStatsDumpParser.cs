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

            Logger.Log("Parsing Red Alert stats.dmp");

            var dump = new StatsDumpParser(statsPath);

            // ===== Average FPS (global) =====
            try
            {
                Statistics.AverageFPS = dump.AverageFPS;
            }
            catch
            {
                Statistics.AverageFPS = 0;
            }

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

                int harvested = dump.PlayerMoneyHarvested[i];
                ps.Economy = harvested > 0 ? harvested : 0;

                // Score is no longer meaningful in RA → zero it
                ps.Score = 0;

                /* ================= BUILT ================= */

                int vehiclesBuilt =
                    dump.PlayerVehiclesBought[i].MammothTanks +
                    dump.PlayerVehiclesBought[i].HeavyTanks +
                    dump.PlayerVehiclesBought[i].MediumTanks +
                    dump.PlayerVehiclesBought[i].LightTanks +
                    dump.PlayerVehiclesBought[i].APCs;

                int infantryBuilt =
                    dump.PlayerInfantryBought[i].RifleInfantries +
                    dump.PlayerInfantryBought[i].RocketSoldiers;

                int buildingsBuilt =
                    dump.PlayerBuildingsBought[i].ConstructionYards +
                    dump.PlayerBuildingsBought[i].WarFactories +
                    dump.PlayerBuildingsBought[i].Refineries +
                    dump.PlayerBuildingsBought[i].PowerPlants;

                /* ================= LEFT ================= */

                int vehiclesLeft =
                    dump.PlayerVehiclesLeft[i].MammothTanks +
                    dump.PlayerVehiclesLeft[i].HeavyTanks +
                    dump.PlayerVehiclesLeft[i].MediumTanks +
                    dump.PlayerVehiclesLeft[i].LightTanks +
                    dump.PlayerVehiclesLeft[i].APCs;

                int infantryLeft =
                    dump.PlayerInfantryLeft[i].RifleInfantries +
                    dump.PlayerInfantryLeft[i].RocketSoldiers;

                int buildingsLeft =
                    dump.PlayerBuildingsLeft[i].ConstructionYards +
                    dump.PlayerBuildingsLeft[i].WarFactories +
                    dump.PlayerBuildingsLeft[i].Refineries +
                    dump.PlayerBuildingsLeft[i].PowerPlants;

                /* ================= LOSSES ================= */

                int vehiclesLost = vehiclesBuilt - vehiclesLeft;
                int infantryLost = infantryBuilt - infantryLeft;
                int buildingsLost = buildingsBuilt - buildingsLeft;

                ps.Losses = vehiclesLost + infantryLost + buildingsLost;

                /* ================= KILLS ================= */

                int vehiclesKilled =
                    dump.PlayerVehiclesKilled[i].MammothTanks +
                    dump.PlayerVehiclesKilled[i].HeavyTanks +
                    dump.PlayerVehiclesKilled[i].MediumTanks +
                    dump.PlayerVehiclesKilled[i].LightTanks +
                    dump.PlayerVehiclesKilled[i].APCs;

                int infantryKilled =
                    dump.PlayerInfantryKilled[i].RifleInfantries +
                    dump.PlayerInfantryKilled[i].RocketSoldiers;

                int buildingsKilled =
                    dump.PlayerBuildingsKilled[i].ConstructionYards +
                    dump.PlayerBuildingsKilled[i].WarFactories +
                    dump.PlayerBuildingsKilled[i].Refineries +
                    dump.PlayerBuildingsKilled[i].PowerPlants;

                ps.Kills = vehiclesKilled + infantryKilled + buildingsKilled;

                /* ================= WIN STATE ================= */

                ps.Won =
                    dump.PlayerDeadStates[i] == 0 &&
                    dump.PlayersResigned[i] != 1;

                ps.SawEnd = true;

                Logger.Log(
                    $"RA Stats | {ps.Name} | Eco={ps.Economy} | Kills={ps.Kills} | Losses={ps.Losses} | Won={ps.Won}"
                );
            }

            Statistics.SawCompletion = true;
        }
    }
}