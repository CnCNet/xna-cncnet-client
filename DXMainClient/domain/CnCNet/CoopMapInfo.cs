using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.CnCNet
{
    public class CoopMapInfo
    {
        public List<CoopHouseInfo> EnemyHouses = new List<CoopHouseInfo>();
        public List<int> DisallowedPlayerSides = new List<int>();
        public List<int> DisallowedPlayerColors = new List<int>();
    }
}
