using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Statistics
{
    public class RAPlayerStatistics
    {
        public string Name { get; internal set; }

        public int Credits { get; internal set; }
        public int MoneyHarvested { get; internal set; }
        public int CratesCollected { get; internal set; }

        public int VehiclesKilled { get; internal set; }
        public int InfantryKilled { get; internal set; }
        public int PlanesKilled { get; internal set; }
        public int BuildingsKilled { get; internal set; }
        public int VesselsKilled { get; internal set; }

        public int VehiclesLeft { get; internal set; }
        public int InfantryLeft { get; internal set; }
        public int PlanesLeft { get; internal set; }
        public int BuildingsLeft { get; internal set; }
        public int VesselsLeft { get; internal set; }

        public int VehiclesOwned { get; internal set; }
        public int InfantryOwned { get; internal set; }
        public int PlanesOwned { get; internal set; }
        public int BuildingsOwned { get; internal set; }
        public int VesselsOwned { get; internal set; }

        public int BuildingsCaptured { get; internal set; }

        public string QuitState { get; internal set; }

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