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

            var dump = new StatsDumpParser(statsPath);

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

                int credits = dump.PlayerCredits[i];
                int harvested = dump.PlayerMoneyHarvested[i];

                ps.Score = (credits > 0 ? credits : 0)
                         + (harvested > 0 ? harvested : 0);

                // Units killed
                int unitKills =
                    dump.PlayerVehiclesKilled[i].MammothTanks +
                    dump.PlayerVehiclesKilled[i].HeavyTanks +
                    dump.PlayerVehiclesKilled[i].MediumTanks +
                    dump.PlayerVehiclesKilled[i].LightTanks +
                    dump.PlayerVehiclesKilled[i].APCs +
                    dump.PlayerInfantryKilled[i].RifleInfantries +
                    dump.PlayerInfantryKilled[i].RocketSoldiers;

                // Buildings killed (simple approximation)
                int buildingKills =
                    dump.PlayerBuildingsKilled[i].ConstructionYards +
                    dump.PlayerBuildingsKilled[i].WarFactories +
                    dump.PlayerBuildingsKilled[i].Refineries +
                    dump.PlayerBuildingsKilled[i].PowerPlants;

                ps.Kills = unitKills + buildingKills;

                ps.UnitsBuilt =
                    dump.PlayerVehiclesBought[i].HeavyTanks +
                    dump.PlayerVehiclesBought[i].MediumTanks +
                    dump.PlayerInfantryBought[i].RifleInfantries;

                ps.BuildingsBuilt =
                    dump.PlayerBuildingsBought[i].ConstructionYards +
                    dump.PlayerBuildingsBought[i].WarFactories +
                    dump.PlayerBuildingsBought[i].Refineries;

                // Determine win state
                ps.Won =
                    dump.PlayerDeadStates[i] == 0 &&
                    dump.PlayersResigned[i] != 1;

                ps.SawEnd = true;
            }

            Statistics.SawCompletion = true;
        }
    }
}
