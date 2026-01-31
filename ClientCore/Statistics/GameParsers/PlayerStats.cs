using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Statistics.GameParsers
{
    public class PlayerStats
    {
        public string Name { get; set; }
        public string Side { get; set; }
        public int Credits { get; set; }
        public int MoneyHarvested { get; set; }
        public bool Quit { get; set; }
        public bool Dead { get; set; }
        public bool Spectator { get; set; }
        public int Color { get; set; }
        // Add more fields if needed
    }
}