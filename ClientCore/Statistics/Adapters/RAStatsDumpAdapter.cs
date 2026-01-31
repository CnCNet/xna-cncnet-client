using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientCore.Statistics.GameParsers;
using ClientCore.Statistics.Extensions;
using ClientCore.Enums;
using ClientCore.Statistics;

namespace ClientCore.Statistics.Adapters
{
    public static class RAStatsDumpAdapter
    {
        public static List<RAPlayerStatistics> BuildRAStats(
            StatsDumpParser dump,
            MatchStatistics match)
        {
            var result = new List<RAPlayerStatistics>();

            for (int i = 0; i < 8; i++)
            {
                // skip unused slots
                if (string.IsNullOrEmpty(dump.PlayerNames[i]))
                    continue;

                var ps = match.Players.Find(p => p.Name == dump.PlayerNames[i]);
                if (ps == null)
                    continue;

                var ra = new RAPlayerStatistics(ps)
                {
                    // Economy
                    MoneyHarvested = dump.PlayerMoneyHarvested[i],
                    Credits = dump.PlayerCredits[i],

                    // Kills
                    VehiclesKilled = Sum(dump.PlayerVehiclesKilled[i]),
                    InfantryKilled = Sum(dump.PlayerInfantryKilled[i]),
                    PlanesKilled = Sum(dump.PlayerPlanesKilled[i]),
                    BuildingsKilled = Sum(dump.PlayerBuildingsKilled[i]),
                    VesselsKilled = Sum(dump.PlayerVesselsKilled[i]),

                    // Left
                    VehiclesLeft = Sum(dump.PlayerVehiclesLeft[i]),
                    InfantryLeft = Sum(dump.PlayerInfantryLeft[i]),
                    PlanesLeft = Sum(dump.PlayerPlanesLeft[i]),
                    BuildingsLeft = Sum(dump.PlayerBuildingsLeft[i]),
                    VesselsLeft = Sum(dump.PlayerVesselsLeft[i]),

                    // Owned (bought)
                    VehiclesOwned = Sum(dump.PlayerVehiclesBought[i]),
                    InfantryOwned = Sum(dump.PlayerInfantryBought[i]),
                    PlanesOwned = Sum(dump.PlayerPlanesBought[i]),
                    BuildingsOwned = Sum(dump.PlayerBuildingsBought[i]),
                    VesselsOwned = Sum(dump.PlayerVesselsBought[i]),

                    // Captured
                    BuildingsCaptured = Sum(dump.PlayerBuildingsCaptured[i]),

                    // Crates
                    CratesCollected = Sum(dump.PlayerCratesCollected[i])
                };

                result.Add(ra);
            }

            return result;
        }

        private static int Sum<T>(T struc)
        {
            int total = 0;
            foreach (var f in typeof(T).GetFields())
            {
                if (f.GetValue(struc) is int v)
                    total += v;
            }
            return total;
        }
    }
}