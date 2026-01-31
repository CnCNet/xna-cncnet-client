using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Statistics
{
    public class RAPlayerStatistics
    {
        public string Name { get; }

        public int Credits { get; }
        public int MoneyHarvested { get; }
        public int CratesCollected { get; }

        public int VehiclesKilled { get; }
        public int InfantryKilled { get; }
        public int PlanesKilled { get; }
        public int BuildingsKilled { get; }
        public int VesselsKilled { get; }

        public int VehiclesLeft { get; }
        public int InfantryLeft { get; }
        public int PlanesLeft { get; }
        public int BuildingsLeft { get; }
        public int VesselsLeft { get; }

        public int VehiclesOwned { get; }
        public int InfantryOwned { get; }
        public int PlanesOwned { get; }
        public int BuildingsOwned { get; }
        public int VesselsOwned { get; }

        public int BuildingsCaptured { get; }

        public string QuitState { get; }

        public RAPlayerStatistics(PlayerStatistics ps)
        {
            Name = ps.Name;

            Credits = ps.Economy;
            MoneyHarvested = ps.Economy; // adjust if separate later
            CratesCollected = 0;         // filled later from dump

            VehiclesKilled = ps.Kills;
            InfantryKilled = 0;
            PlanesKilled = 0;
            BuildingsKilled = 0;
            VesselsKilled = 0;

            VehiclesLeft = ps.Losses;
            InfantryLeft = 0;
            PlanesLeft = 0;
            BuildingsLeft = 0;
            VesselsLeft = 0;

            VehiclesOwned = 0;
            InfantryOwned = 0;
            PlanesOwned = 0;
            BuildingsOwned = 0;
            VesselsOwned = 0;

            BuildingsCaptured = 0;

            QuitState =
                ps.Won ? "Won" :
                ps.SawEnd ? "Finished" :
                "Quit";
        }
    }
}