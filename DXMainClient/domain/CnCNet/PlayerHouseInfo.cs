using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.CnCNet
{
    public class PlayerHouseInfo
    {
        public int SideIndex { get; set; }
        public int ColorIndex { get; set; }
        public int StartingWaypoint { get; set; }

        public bool IsSpectator { get; set; }
    }
}
