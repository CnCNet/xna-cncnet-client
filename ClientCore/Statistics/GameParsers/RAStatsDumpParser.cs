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

        // Public entry point called by MatchStatistics
        public void ParseStats(string gamePath)
        {
            ParseStatistics(gamePath);
        }

        // Internal parsing logic
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

            /* ===============================
             *  Average FPS (GLOBAL MATCH)
             * =============================== */
            try
            {
                // Most RA dumps expose this
                Statistics.AverageFPS = dump.AverageFPS;
            }
            catch
            {
                Statistics.AverageFPS = 0; // Safe fallback
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

                /* ========= SCORE / ECONOMY ========= */

                int credits = dump.PlayerCredits[i];
                int harvested = dump.PlayerMoneyHarvested[i];

                ps.Score =
                    (credits > 0 ? credits : 0) +
                    (harvested > 0 ? harvested : 0);

                // Economy column (shown as "Built" in UI)
                ps.Economy = harvested > 0 ? harvested : 0;

                /* ========= KILLS ========= */

                int unitKills =
                    dump.PlayerVehiclesKilled[i].MammothTanks +
                    dump.PlayerVehiclesKilled[i].HeavyTanks +
                    dump.PlayerVehiclesKilled[i].MediumTanks +
                    dump.PlayerVehiclesKilled[i].LightTanks +
                    dump.PlayerVehiclesKilled[i].APCs +
                    dump.PlayerInfantryKilled[i].RifleInfantries +
                    dump.PlayerInfantryKilled[i].RocketSoldiers;

                int buildingKills =
                    dump.PlayerBuildingsKilled[i].ConstructionYards +
                    dump.PlayerBuildingsKilled[i].WarFactories +
                    dump.PlayerBuildingsKilled[i].Refineries +
                    dump.PlayerBuildingsKilled[i].PowerPlants;

                ps.Kills = unitKills + buildingKills;

                /* ========= LOSSES ========= */

                int unitLosses =
                    dump.PlayerVehiclesBought[i].MammothTanks +
                    dump.PlayerVehiclesBought[i].HeavyTanks +
                    dump.PlayerVehiclesBought[i].MediumTanks +
                    dump.PlayerVehiclesBought[i].LightTanks +
                    dump.PlayerVehiclesBought[i].APCs +
                    dump.PlayerInfantryBought[i].RifleInfantries +
                    dump.PlayerInfantryBought[i].RocketSoldiers;

                int buildingLosses =
                    dump.PlayerBuildingsBought[i].ConstructionYards +
                    dump.PlayerBuildingsBought[i].WarFactories +
                    dump.PlayerBuildingsBought[i].Refineries +
                    dump.PlayerBuildingsBought[i].PowerPlants;

                int unitRemaining =
                    dump.PlayerVehiclesLeft[i].MammothTanks +
                    dump.PlayerVehiclesLeft[i].HeavyTanks +
                    dump.PlayerVehiclesLeft[i].MediumTanks +
                    dump.PlayerVehiclesLeft[i].LightTanks +
                    dump.PlayerVehiclesLeft[i].APCs +
                    dump.PlayerInfantryLeft[i].RifleInfantries +
                    dump.PlayerInfantryLeft[i].RocketSoldiers;

                int buildingRemaining =
                    dump.PlayerBuildingsLeft[i].ConstructionYards +
                    dump.PlayerBuildingsLeft[i].WarFactories +
                    dump.PlayerBuildingsLeft[i].Refineries +
                    dump.PlayerBuildingsLeft[i].PowerPlants;

                ps.Losses = unitLosses + buildingLosses - (unitRemaining + buildingRemaining);

                /* ========= WIN STATE ========= */

                ps.Won =
                    dump.PlayerDeadStates[i] == 0 &&
                    dump.PlayersResigned[i] != 1;

                ps.SawEnd = true;

                Logger.Log(
                    $"RA Stats | {ps.Name} | Kills={ps.Kills} | Score={ps.Score} | Losses={ps.Losses} | Economy={ps.Economy} | Won={ps.Won}"
                );
            }

            Statistics.SawCompletion = true;
        }
    }
}