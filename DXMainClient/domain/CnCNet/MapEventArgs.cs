using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.CnCNet
{
    public class MapEventArgs : EventArgs
    {
        public MapEventArgs(Map map)
        {
            Map = map;
        }

        public Map Map { get; private set; }
    }
}
