using System.Collections.Generic;

namespace DTAClient.domain.Multiplayer
{
    public class CoopMapInfo
    {
        public List<CoopHouseInfo> EnemyHouses = new List<CoopHouseInfo>();
        public List<int> DisallowedPlayerSides = new List<int>();
        public List<int> DisallowedPlayerColors = new List<int>();
    }
}
